import { test, expect } from '@playwright/test';

test('simple connectivity test', async ({ page }) => {
  console.log('Going to homepage...');
  await page.goto('/');
  
  console.log('Waiting for page load...');
  await page.waitForLoadState('domcontentloaded');
  
  console.log('Taking screenshot...');
  await page.screenshot({ path: 'test-results/homepage.png', fullPage: true });
  
  console.log('Getting page title...');
  const title = await page.title();
  console.log('Title:', title);
  
  expect(title).toBe('KunstButikken');
  
  console.log('✅ Test completed successfully');
});

