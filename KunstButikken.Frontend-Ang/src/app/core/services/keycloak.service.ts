
import { isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID, signal } from '@angular/core';

import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class KeycloakService {
  private keycloak: import('keycloak-js').default | null = null;
  private readonly isBrowser: boolean;
  private readonly authenticatedInternal = signal<boolean>(false);
  readonly authenticated = this.authenticatedInternal.asReadonly();

  constructor(@Inject(PLATFORM_ID) platformId: object) {
    // Use multiple methods to detect browser - sometimes isPlatformBrowser fails with dev server SSR
    this.isBrowser = isPlatformBrowser(platformId) || (typeof window !== 'undefined');
    console.log('[KeycloakService] Constructor - isPlatformBrowser:', isPlatformBrowser(platformId));
    console.log('[KeycloakService] Constructor - window exists:', typeof window !== 'undefined');
    console.log('[KeycloakService] Constructor - isBrowser (final):', this.isBrowser);
  }

  async boot(): Promise<void> {
    console.log('[KeycloakService] boot() called');
    console.log('[KeycloakService] isBrowser:', this.isBrowser);

    if (!this.isBrowser) {
      console.log('[KeycloakService] Not in browser environment - skipping init');
      return;
    }

    if (this.keycloak) {
      console.log('[KeycloakService] Already initialized - skipping');
      return;
    }

    console.log('[KeycloakService] Environment config:', environment);
    const { keycloak, frontend } = environment;

    console.log('[KeycloakService] Keycloak baseUrl:', keycloak?.baseUrl);
    console.log('[KeycloakService] Keycloak realm:', keycloak?.realm);
    console.log('[KeycloakService] Keycloak clientId:', keycloak?.clientId);
    console.log('[KeycloakService] Frontend selfUrl:', frontend?.selfUrl);

    if (!keycloak.baseUrl || !keycloak.realm || !keycloak.clientId) {
      console.error('[KeycloakService] Missing Keycloak configuration – skipping init');
      console.error('[KeycloakService] This is why Keycloak is not initialized!');
      return;
    }

    console.log('[KeycloakService] Importing keycloak-js...');
    const { default: Keycloak } = await import('keycloak-js');

    console.log('[KeycloakService] Creating Keycloak instance...');
    this.keycloak = new Keycloak({
      url: keycloak.baseUrl,
      realm: keycloak.realm,
      clientId: keycloak.clientId
    });

    console.log('[KeycloakService] Initializing Keycloak with check-sso...');
    try {
      const authenticated = await this.keycloak.init({
        onLoad: 'check-sso',
        silentCheckSsoRedirectUri: `${frontend.selfUrl}/assets/silent-check-sso.html`,
        checkLoginIframe: false,
        enableLogging: !environment.production
      });

      console.log('[KeycloakService] Keycloak initialized successfully!');
      console.log('[KeycloakService] User authenticated:', authenticated);

      this.authenticatedInternal.set(Boolean(authenticated));
      this.registerCallbacks();
    } catch (error) {
      console.error('[KeycloakService] Failed to initialise Keycloak:', error);
      this.authenticatedInternal.set(false);
    }
  }

  login(): void {
    if (!this.keycloak) {
      this.logMissingClient();
      return;
    }
    this.keycloak.login({ redirectUri: environment.frontend.selfUrl });
  }

  logout(): void {
    if (!this.keycloak) {
      this.logMissingClient();
      return;
    }
    this.authenticatedInternal.set(false);
    this.keycloak.logout({ redirectUri: environment.frontend.selfUrl });
  }

  getToken(): string | undefined {
    const token = this.keycloak?.token;
    console.log('[KeycloakService] getToken called, token exists:', !!token);
    if (token) {
      console.log('[KeycloakService] Token (first 50 chars):', token.substring(0, 50) + '...');
    } else {
      console.warn('[KeycloakService] NO TOKEN AVAILABLE');
      console.log('[KeycloakService] Keycloak instance exists:', !!this.keycloak);
      console.log('[KeycloakService] Authenticated signal:', this.authenticated());
    }
    return token;
  }

  isAuthenticated(): boolean {
    return this.authenticated();
  }

  isLoggedIn(): boolean {
    return !!this.keycloak?.token;
  }

  private registerCallbacks(): void {
    if (!this.keycloak) {
      return;
    }
    this.keycloak.onAuthSuccess = () => {
      this.authenticatedInternal.set(true);
    };
    this.keycloak.onAuthLogout = () => {
      this.authenticatedInternal.set(false);
    };
    this.keycloak.onTokenExpired = async () => {
      try {
        await this.keycloak?.updateToken(30);
      } catch (error) {
        console.warn('[KeycloakService] Token refresh failed, forcing logout', error);
        this.authenticatedInternal.set(false);
      }
    };
  }

  private logMissingClient() {
    console.warn('[KeycloakService] Keycloak client not ready');
  }
}
