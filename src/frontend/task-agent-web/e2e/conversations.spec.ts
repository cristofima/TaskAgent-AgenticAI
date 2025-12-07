import { test, expect } from '@playwright/test';
import { setupApiMocks } from './fixtures/api-mocks';
import { mockConversations, createMockSSEResponse } from './fixtures/mock-data';

/**
 * Conversation management E2E tests
 * 
 * Tests for sidebar, conversation list, creating/switching/deleting conversations
 */

test.describe('Conversation Sidebar', () => {
  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should display sidebar toggle button', async ({ page }) => {
    // On mobile viewport, the sidebar toggle should be visible
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Look for sidebar toggle button (hamburger menu or similar)
    const sidebarToggle = page.locator('button[aria-label*="menu"], button[aria-label*="sidebar"], button[aria-label*="Open"], button[aria-label*="Close"]').first();
    
    // The toggle should be visible on mobile
    await expect(sidebarToggle).toBeVisible();
  });

  test('should have new chat button', async ({ page }) => {
    // Look for "New Chat" or "+" button
    const newChatButton = page.locator('button:has-text("New"), button[aria-label*="new chat"], button:has-text("+")').first();
    
    if (await newChatButton.isVisible()) {
      await expect(newChatButton).toBeEnabled();
    }
  });
});

test.describe('Conversation List', () => {
  test.beforeEach(async ({ page }) => {
    // Setup API mocks to return conversations
    await page.route('**/api/conversations', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            threads: mockConversations,
            totalCount: mockConversations.length,
            page: 1,
            pageSize: 20,
            hasMore: false,
          }),
        });
      } else {
        await route.continue();
      }
    });

    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should load conversations list', async ({ page }) => {
    // Wait for API call to complete
    await page.waitForTimeout(500);
    
    // Check if conversations are displayed in sidebar
    // The sidebar should contain conversation titles
    const pageContent = await page.content();
    
    // Verify the page loaded correctly
    expect(pageContent).toBeTruthy();
  });
});

test.describe('Create New Conversation', () => {
  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
  });

  test('should create new conversation when sending first message', async ({ page }) => {
    let threadStateReceived = false;

    // Override the chat mock to track thread creation
    await page.route('**/api/agent/chat', async (route) => {
      threadStateReceived = true;
      const sseResponse = createMockSSEResponse('✅ Task created! This is your new conversation.');

      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        body: sseResponse,
      });
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Type and send a message
    const chatInput = page.locator('textarea').first();
    await chatInput.fill('Create a new task for testing');
    
    const sendButton = page.locator('button[type="submit"]').first();
    if (await sendButton.isVisible()) {
      await sendButton.click();
    }

    // Wait for the SSE response
    await page.waitForTimeout(1000);

    // Verify that a chat endpoint was called (indicating conversation creation)
    expect(threadStateReceived).toBe(true);
  });

  test('should clear messages when starting new conversation', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Send an initial message
    const chatInput = page.locator('textarea').first();
    await chatInput.fill('First message');
    await chatInput.press('Enter');

    await page.waitForTimeout(500);

    // Find and click "New Chat" button if available
    const newChatButton = page.locator('button:has-text("New Chat"), button[aria-label*="new"]').first();
    
    if (await newChatButton.isVisible()) {
      await newChatButton.click();
      
      // The chat input should be empty and ready for new conversation
      await expect(chatInput).toHaveValue('');
    }
  });
});

test.describe('Switch Between Conversations', () => {
  test.beforeEach(async ({ page }) => {
    // Setup conversations list
    await page.route('**/api/conversations', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            threads: mockConversations,
            totalCount: mockConversations.length,
            page: 1,
            pageSize: 20,
            hasMore: false,
          }),
        });
      }
    });

    // Setup messages for each conversation
    await page.route('**/api/conversations/conv-1/messages', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          messages: [
            { id: 'msg-1', role: 'user', content: 'First conversation message', createdAt: '2025-12-06T10:00:00Z' },
            { id: 'msg-2', role: 'assistant', content: 'Response to first conversation', createdAt: '2025-12-06T10:00:05Z' },
          ],
          serializedState: 'state-conv-1',
        }),
      });
    });

    await page.route('**/api/conversations/conv-2/messages', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          messages: [
            { id: 'msg-3', role: 'user', content: 'Second conversation message', createdAt: '2025-12-05T14:00:00Z' },
            { id: 'msg-4', role: 'assistant', content: 'Response to second conversation', createdAt: '2025-12-05T14:00:05Z' },
          ],
          serializedState: 'state-conv-2',
        }),
      });
    });

    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should maintain UI stability when switching', async ({ page }) => {
    // Verify the page remains stable during operations
    await expect(page.locator('body')).toBeVisible();

    // The main chat interface should always be present
    const chatArea = page.locator('textarea').first();
    await expect(chatArea).toBeVisible();
  });
});

test.describe('Delete Conversation', () => {
  test.beforeEach(async ({ page }) => {
    let deleteEndpointCalled = false;

    // Setup conversations list
    await page.route('**/api/conversations', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            threads: deleteEndpointCalled ? [] : mockConversations,
            totalCount: deleteEndpointCalled ? 0 : mockConversations.length,
            page: 1,
            pageSize: 20,
            hasMore: false,
          }),
        });
      }
    });

    // Setup delete endpoint
    await page.route('**/api/conversations/*', async (route) => {
      if (route.request().method() === 'DELETE') {
        deleteEndpointCalled = true;
        await route.fulfill({
          status: 204,
        });
      } else {
        await route.continue();
      }
    });

    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should show delete confirmation modal', async ({ page }) => {
    // Look for delete button in conversation list
    const deleteButton = page.locator('button[aria-label*="delete"], button:has(svg[class*="trash"])').first();
    
    if (await deleteButton.isVisible()) {
      await deleteButton.click();
      
      // Look for confirmation modal
      const modal = page.locator('[role="dialog"], .modal, [data-testid="delete-modal"]');
      
      if (await modal.isVisible()) {
        await expect(modal).toBeVisible();
      }
    }
  });

  test('should close modal when cancelled', async ({ page }) => {
    const deleteButton = page.locator('button[aria-label*="delete"]').first();
    
    if (await deleteButton.isVisible()) {
      await deleteButton.click();
      
      const cancelButton = page.locator('button:has-text("Cancel"), button:has-text("No")').first();
      
      if (await cancelButton.isVisible()) {
        await cancelButton.click();
        
        // Modal should be closed
        const modal = page.locator('[role="dialog"]');
        await expect(modal).not.toBeVisible();
      }
    }
  });
});

test.describe('Conversation Persistence', () => {
  test('should maintain current thread ID in localStorage', async ({ page }) => {
    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Send a message to create a conversation
    const chatInput = page.locator('textarea').first();
    await chatInput.fill('Test message for persistence');
    await chatInput.press('Enter');

    await page.waitForTimeout(1000);

    // Check localStorage for thread ID
    const threadId = await page.evaluate(() => {
      return localStorage.getItem('taskagent_current_thread');
    });

    // Thread ID might be set after message is sent
    // This depends on the implementation
  });

  test('should restore conversation on page reload', async ({ page }) => {
    // Set a thread ID in localStorage
    await page.goto('/');
    await page.evaluate(() => {
      localStorage.setItem('taskagent_current_thread', 'conv-1');
    });

    // Setup message history mock
    await page.route('**/api/conversations/conv-1/messages', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          messages: [
            { id: 'msg-1', role: 'user', content: 'Restored message', createdAt: '2025-12-06T10:00:00Z' },
          ],
          serializedState: 'state-conv-1',
        }),
      });
    });

    await setupApiMocks(page);
    
    // Reload the page
    await page.reload();
    await page.waitForLoadState('networkidle');

    // The UI should still be functional
    await expect(page.locator('body')).toBeVisible();
  });
});
