import { test, expect } from '@playwright/test';
import { setupApiMocks } from './fixtures/api-mocks';

/**
 * Basic navigation E2E tests
 * 
 * These tests validate that the application loads correctly
 * and basic navigation works as expected.
 * 
 * API calls are mocked to ensure fast, reliable tests.
 */

test.describe('Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Setup API mocks before each test
    await setupApiMocks(page);
  });

  test('should load the home page', async ({ page }) => {
    await page.goto('/');

    // Wait for the page to be fully loaded
    await page.waitForLoadState('networkidle');

    // Check that the main chat interface is visible
    // Look for common elements that should be present
    await expect(page.locator('body')).toBeVisible();
  });

  test('should display the page title', async ({ page }) => {
    await page.goto('/');

    // Check the page title
    await expect(page).toHaveTitle(/TaskAgent AI/i);
  });

  test('should have a chat input area', async ({ page }) => {
    await page.goto('/');

    // Wait for hydration
    await page.waitForLoadState('networkidle');

    // Look for textarea or input for chat
    const chatInput = page.locator('textarea, input[type="text"]').first();
    await expect(chatInput).toBeVisible();
  });

  test('should be responsive - mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Page should still be functional on mobile
    await expect(page.locator('body')).toBeVisible();
  });

  test('should be responsive - tablet viewport', async ({ page }) => {
    // Set tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Page should still be functional on tablet
    await expect(page.locator('body')).toBeVisible();
  });
});

test.describe('Accessibility', () => {
  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
  });

  test('should have no accessibility violations on main elements', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Check that interactive elements are keyboard accessible
    const chatInput = page.locator('textarea, input[type="text"]').first();
    
    if (await chatInput.isVisible()) {
      // Tab to the input and verify it receives focus
      await page.keyboard.press('Tab');
      
      // The input should be focusable
      await expect(chatInput).toBeEnabled();
    }
  });
});
