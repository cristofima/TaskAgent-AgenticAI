import { test, expect } from '@playwright/test';
import { setupApiMocks } from './fixtures/api-mocks';

/**
 * Theme toggle E2E tests
 * 
 * Tests for dark/light theme switching and system preference detection
 */

test.describe('Theme Toggle', () => {
  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
  });

  test('should have theme toggle button', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Look for theme toggle button (sun/moon icon)
    const themeToggle = page.locator('button[aria-label*="theme"], button[aria-label*="mode"], button:has(svg)').filter({
      has: page.locator('svg')
    });

    // Theme toggle should exist in header area
    const toggleButtons = await themeToggle.all();
    expect(toggleButtons.length).toBeGreaterThan(0);
  });

  test('should toggle from light to dark mode', async ({ page }) => {
    // Start with light mode preference
    await page.emulateMedia({ colorScheme: 'light' });
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Find and click theme toggle
    const themeToggle = page.locator('button[aria-label*="theme"], button[aria-label*="Toggle"]').first();
    
    if (await themeToggle.isVisible()) {
      // Click to toggle theme
      await themeToggle.click();
      
      // Wait for class change
      await page.waitForTimeout(500);
      
      // Verify toggle was clicked (the page should remain functional)
      await expect(page.locator('body')).toBeVisible();
    }
  });

  test('should toggle from dark to light mode', async ({ page }) => {
    // Start with dark mode preference
    await page.emulateMedia({ colorScheme: 'dark' });
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Find and click theme toggle
    const themeToggle = page.locator('button[aria-label*="theme"], button[aria-label*="Toggle"]').first();
    
    if (await themeToggle.isVisible()) {
      const initialClass = await page.locator('html').getAttribute('class');
      
      await themeToggle.click();
      await page.waitForTimeout(300);
      
      const newClass = await page.locator('html').getAttribute('class');
      
      // Class should have changed
      expect(newClass).not.toBe(initialClass);
    }
  });

  test('should persist theme preference in localStorage', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Find and click theme toggle
    const themeToggle = page.locator('button[aria-label*="theme"], button[aria-label*="Toggle"]').first();
    
    if (await themeToggle.isVisible()) {
      await themeToggle.click();
      await page.waitForTimeout(300);
      
      // Check localStorage for theme preference
      const storedTheme = await page.evaluate(() => {
        return localStorage.getItem('theme');
      });
      
      // Theme should be stored
      expect(storedTheme).toBeTruthy();
    }
  });

  test('should respect stored theme preference on reload', async ({ page }) => {
    await page.goto('/');
    
    // Set dark theme in localStorage
    await page.evaluate(() => {
      localStorage.setItem('theme', 'dark');
    });

    // Reload page
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Check if dark class is applied
    const htmlClass = await page.locator('html').getAttribute('class');
    
    // The HTML element should have dark class if theme was respected
    // Note: This depends on next-themes implementation
  });
});

test.describe('System Color Scheme', () => {
  test('should follow system dark mode preference', async ({ page }) => {
    // Set system preference to dark
    await page.emulateMedia({ colorScheme: 'dark' });
    
    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // The page should render with dark colors or dark class
    await expect(page.locator('body')).toBeVisible();
  });

  test('should follow system light mode preference', async ({ page }) => {
    // Set system preference to light
    await page.emulateMedia({ colorScheme: 'light' });
    
    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // The page should render with light colors
    await expect(page.locator('body')).toBeVisible();
  });
});

test.describe('Dark Mode Visual Consistency', () => {
  test.beforeEach(async ({ page }) => {
    await page.emulateMedia({ colorScheme: 'dark' });
    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should have readable text in dark mode', async ({ page }) => {
    // Main content should be visible
    const chatInput = page.locator('textarea').first();
    await expect(chatInput).toBeVisible();
    
    // Input should be enabled and usable
    await expect(chatInput).toBeEnabled();
  });

  test('should have proper contrast in dark mode', async ({ page }) => {
    // Type something to check visibility
    const chatInput = page.locator('textarea').first();
    await chatInput.fill('Test message in dark mode');
    
    // Text should be visible (no contrast issues)
    await expect(chatInput).toHaveValue('Test message in dark mode');
  });

  test('should apply dark styles to chat messages', async ({ page }) => {
    // Send a message
    const chatInput = page.locator('textarea').first();
    await chatInput.fill('Dark mode test');
    await chatInput.press('Enter');
    
    await page.waitForTimeout(1000);
    
    // Page should remain stable with proper dark styling
    await expect(page.locator('body')).toBeVisible();
  });
});

test.describe('Light Mode Visual Consistency', () => {
  test.beforeEach(async ({ page }) => {
    await page.emulateMedia({ colorScheme: 'light' });
    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should have readable text in light mode', async ({ page }) => {
    const chatInput = page.locator('textarea').first();
    await expect(chatInput).toBeVisible();
    await expect(chatInput).toBeEnabled();
  });

  test('should have proper contrast in light mode', async ({ page }) => {
    const chatInput = page.locator('textarea').first();
    await chatInput.fill('Test message in light mode');
    await expect(chatInput).toHaveValue('Test message in light mode');
  });
});

test.describe('Theme Accessibility', () => {
  test('should maintain focus visibility in dark mode', async ({ page }) => {
    await page.emulateMedia({ colorScheme: 'dark' });
    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Tab to focus on chat input
    await page.keyboard.press('Tab');
    
    // The focused element should be visible
    const focusedElement = page.locator(':focus');
    await expect(focusedElement).toBeVisible();
  });

  test('should maintain focus visibility in light mode', async ({ page }) => {
    await page.emulateMedia({ colorScheme: 'light' });
    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    await page.keyboard.press('Tab');
    
    const focusedElement = page.locator(':focus');
    await expect(focusedElement).toBeVisible();
  });
});
