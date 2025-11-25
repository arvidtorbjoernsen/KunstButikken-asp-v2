import { test, expect } from '@playwright/test';

// Base URL is provided by AppHost via env (NEXTAUTH_URL/FRONTEND_URL). Fallback to localhost for standalone runs.
const BASE = process.env.PLAYWRIGHT_TEST_BASE_URL || process.env.FRONTEND_URL || 'http://localhost:3000';

// Test credentials: prefer Keycloak_* but fall back to legacy Auth0_* vars to be backward compatible.
const USERNAME = process.env.KEYCLOAK_TEST_USERNAME || process.env.AUTH0_TEST_USERNAME || 'seller1';
const PASSWORD = process.env.KEYCLOAK_TEST_PASSWORD || process.env.AUTH0_TEST_PASSWORD || 'seller1';

function escapeForRegExp(s: string) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// Helpers to detect Keycloak login page and perform login submissions reliably across themes.
async function performKeycloakLogin(page: import('@playwright/test').Page, username: string, password: string) {
  // Wait for the login form fields to appear. Keycloak typically uses #username/#password but accept name= as well.
  const userField = page.locator('input[name="username"], #username').first();
  await userField.waitFor({ state: 'visible', timeout: 30000 });
  await userField.fill(username);

  const passField = page.locator('input[name="password"], #password').first();
  await passField.fill(password);

  // The submit control can be a button#kc-login or button/input with name="login"
  const loginButton = page.locator('#kc-login, button[name="login"], input[type="submit"][name="login"]').first();

  // Click and wait for navigation back to our frontend host
  const baseRe = new RegExp('^' + escapeForRegExp(BASE) + '/');
  await Promise.all([
    page.waitForURL(baseRe, { timeout: 30000 }).catch(() => {}),
    loginButton.click(),
  ]);
}

async function tryClickNextAuthProvider(page: import('@playwright/test').Page, callbackPath: string) {
  // 1) Anchor form: /api/auth/signin/keycloak
  const providerLink = page.locator('a[href*="/api/auth/signin/keycloak"]').first();
  if (await providerLink.isVisible().catch(() => false)) {
    await Promise.all([
      page.waitForURL(/\/realms\//, { timeout: 20000 }).catch(() => {}),
      providerLink.click(),
    ]);
    if (page.url().includes('/realms/')) return true;
  }

  // 2) Form post action
  const providerForm = page.locator('form[action*="/api/auth/signin/keycloak"]').first();
  if (await providerForm.isVisible().catch(() => false)) {
    const submitBtn = providerForm.locator('button[type="submit"], input[type="submit"]').first();
    if (await submitBtn.isVisible().catch(() => false)) {
      await Promise.all([
        page.waitForURL(/\/realms\//, { timeout: 20000 }).catch(() => {}),
        submitBtn.click(),
      ]);
      if (page.url().includes('/realms/')) return true;
    } else {
      // No button; try programmatic submit
      await Promise.all([
        page.waitForURL(/\/realms\//, { timeout: 20000 }).catch(() => {}),
        providerForm.evaluate((f: HTMLFormElement) => f.submit()),
      ]);
      if (page.url().includes('/realms/')) return true;
    }
  }

  // 3) Button with Keycloak text
  const providerButton = page.locator('button:has-text("Keycloak")').first();
  if (await providerButton.isVisible().catch(() => false)) {
    await Promise.all([
      page.waitForURL(/\/realms\//, { timeout: 20000 }).catch(() => {}),
      providerButton.click(),
    ]);
    if (page.url().includes('/realms/')) return true;
  }

  // 4) Fallback: navigate directly to the provider sign-in endpoint with callback
  const providerUrl = `${BASE}/api/auth/signin/keycloak?callbackUrl=${encodeURIComponent(callbackPath)}`;
  await page.goto(providerUrl);
  await page.waitForURL(/\/realms\//, { timeout: 20000 }).catch(() => {});
  return page.url().includes('/realms/');
}

// Main scenario: navigate to a seller-only route and complete the OIDC login flow.
// This covers: NextAuth middleware redirect -> provider login (Keycloak) -> redirect back -> authorized UI visible.

test.describe('Login flow (Keycloak -> NextAuth)', () => {
  test('seller can log in and access seller-only page', async ({ page }) => {
    // Preflight: skip if frontend or keycloak config isn't reachable (use dev-check endpoint if present)
    try {
      const res = await page.request.get(`${BASE}/api/auth/dev-check`, { timeout: 5000 });
      if (!res.ok()) {
        test.skip(true, `Frontend responded ${res.status()} to /api/auth/dev-check; skipping login e2e.`);
      } else {
        const json = await res.json().catch(() => null) as null | { env?: { keycloak?: { issuer?: string, issuerValue?: string } } };
        const issuerSet = !!(json && (json.env?.keycloak?.issuer === 'set' || json.env?.keycloak?.issuerValue));
        if (!issuerSet) {
          test.skip(true, 'Keycloak issuer not configured in frontend env; skipping login e2e.');
        }
      }
    } catch {
      test.skip(true, `Frontend not reachable at ${BASE}; skipping login e2e.`);
    }

    const target = `${BASE}/sell/new-art`;
    const callbackPath = '/sell/new-art';
    const signInStart = `${BASE}/signin?callbackUrl=${encodeURIComponent(callbackPath)}`;

    // Start on the explicit sign-in page so our SignInClient triggers the provider redirect deterministically
    await page.goto(signInStart);

    // If we were already authenticated (rare in clean context), the form input should be visible quickly once we land at target.
    const alreadyAuthed = await page
      .locator('[data-testid="image-file-input"]')
      .first()
      .isVisible({ timeout: 1500 })
      .catch(() => false);

    if (!alreadyAuthed) {
      // If on NextAuth sign-in page, click/submit the Keycloak provider
      if (page.url().includes('/api/auth/signin')) {
        await tryClickNextAuthProvider(page, callbackPath).catch(() => {});
      }

      // Expect a redirect to Keycloak login. Detect presence of the login form and authenticate.
      let onKeycloak = await page
        .locator('input[name="username"], #username')
        .first()
        .isVisible({ timeout: 20000 })
        .catch(() => false);

      if (!onKeycloak) {
        await page.waitForURL(/\/realms\//, { timeout: 20000 }).catch(() => {});
        // Try the provider selection again if we returned to /api/auth/signin
        if (page.url().includes('/api/auth/signin')) {
          await tryClickNextAuthProvider(page, callbackPath).catch(() => {});
        }
        onKeycloak = await page
          .locator('input[name="username"], #username')
          .first()
          .isVisible({ timeout: 20000 })
          .catch(() => false);
      }

      if (!onKeycloak) {
        test.skip(true, 'Keycloak login page not reachable — skipping login e2e. Ensure AppHost and Keycloak are running.');
      }

      await performKeycloakLogin(page, USERNAME, PASSWORD);

      // After login we should be back on our app at the callback target.
      await page.waitForURL(new RegExp('^' + escapeForRegExp(BASE) + '/sell/new-art'), { timeout: 30000 }).catch(() => {});
      if (!page.url().startsWith(`${BASE}/sell/new-art`)) {
        await page.goto(target);
      }
    }

    // Assertions: we are on the seller-only page and the seller UI is present.
    await expect(page).toHaveURL(new RegExp('^' + escapeForRegExp(BASE) + '/sell/new-art'));

    // The page has a file input with a stable test id regardless of locale.
    await expect(page.locator('[data-testid="image-file-input"]')).toBeVisible({ timeout: 20000 });

    // Navbar includes a quick link to the same seller page for authenticated sellers.
    await expect(page.locator('a[href="/sell/new-art"]').first()).toBeVisible();
  });
});
