import { test as base, type Page } from '@playwright/test';
import { mockConversations, mockMessages, createMockSSEResponse } from './mock-data';

/**
 * Custom test fixtures with API mocking
 * 
 * This provides a centralized way to mock all API endpoints
 * ensuring consistent behavior across all E2E tests.
 */

/**
 * Setup API mocks for a page
 * Call this before navigating to mock all backend endpoints
 */
export async function setupApiMocks(page: Page) {
  // Mock conversations list endpoint
  await page.route('**/api/conversations', async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockConversations),
      });
    } else {
      await route.continue();
    }
  });

  // Mock conversation messages endpoint
  await page.route('**/api/conversations/*/messages', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockMessages),
    });
  });

  // Mock chat endpoint with SSE streaming
  await page.route('**/api/agent/chat', async (route) => {
    const sseResponse = createMockSSEResponse('✅ I understand your request. Let me help you with that!');

    await route.fulfill({
      status: 200,
      contentType: 'text/event-stream',
      body: sseResponse,
    });
  });

  // Mock delete conversation endpoint
  await page.route('**/api/conversations/*', async (route) => {
    if (route.request().method() === 'DELETE') {
      await route.fulfill({
        status: 204,
      });
    } else {
      await route.continue();
    }
  });
}

/**
 * Extended test with API mocking enabled by default
 */
export const test = base.extend<{ mockApi: void }>({
  mockApi: async ({ page }, use) => {
    await setupApiMocks(page);
    await use();
  },
});

export { expect } from '@playwright/test';
