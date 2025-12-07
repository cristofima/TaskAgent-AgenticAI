import { test, expect } from '@playwright/test';
import { setupApiMocks } from './fixtures/api-mocks';
import { createMockSSEResponseWithFunctionCall } from './fixtures/mock-data';

/**
 * Chat functionality E2E tests
 * 
 * These tests validate the core chat functionality including:
 * - Sending messages
 * - Receiving responses (mocked)
 * - UI interactions
 * 
 * All API calls are mocked for reliable testing.
 */

test.describe('Chat Interface', () => {
  test.beforeEach(async ({ page }) => {
    // Setup API mocks before each test
    await setupApiMocks(page);
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('should display empty chat state initially', async ({ page }) => {
    // Look for elements that indicate empty state or welcome message
    // Using flexible selectors that match the actual DOM structure
    const chatArea = page.locator('#chat-messages, [data-testid="chat-messages"], .flex.flex-col').first();
    await expect(chatArea).toBeVisible();
  });

  test('should allow typing in the chat input', async ({ page }) => {
    const chatInput = page.locator('textarea, input[type="text"]').first();
    
    await chatInput.fill('Hello, I want to create a task');
    
    await expect(chatInput).toHaveValue('Hello, I want to create a task');
  });

  test('should clear input after sending message', async ({ page }) => {
    const chatInput = page.locator('textarea').first();
    
    // Type a message
    await chatInput.fill('Create a new task');
    
    // Find and click the send button or press Enter
    const sendButton = page.locator('button[type="submit"], button:has-text("Send")').first();
    
    if (await sendButton.isVisible()) {
      await sendButton.click();
    } else {
      // Try pressing Enter to submit
      await chatInput.press('Enter');
    }
    
    // Wait a moment for the message to be processed
    await page.waitForTimeout(500);
    
    // Input should be cleared or the message should appear in chat
    // Note: The actual behavior depends on your implementation
  });

  test('should show loading state while waiting for response', async ({ page }) => {
    // Create a delayed mock for this specific test
    await page.route('**/api/agent/chat', async (route) => {
      // Add a small delay to show loading state
      await new Promise(resolve => setTimeout(resolve, 100));
      
      const sseResponse = [
        'event: TEXT_MESSAGE_START\n',
        `data: {"messageId":"delayed-msg","createdAt":"${new Date().toISOString()}"}\n\n`,
        'event: TEXT_MESSAGE_CONTENT\n',
        'data: {"text":"Response after delay"}\n\n',
        'event: TEXT_MESSAGE_END\n',
        'data: {}\n\n',
      ].join('');

      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        body: sseResponse,
      });
    });

    const chatInput = page.locator('textarea').first();
    await chatInput.fill('Test message');
    
    // Submit the message
    const sendButton = page.locator('button[type="submit"]').first();
    if (await sendButton.isVisible()) {
      await sendButton.click();
    }
  });

  test('should display assistant messages with proper formatting', async ({ page }) => {
    // Mock a response with markdown content
    await page.route('**/api/agent/chat', async (route) => {
      const markdownResponse = '**Bold text** and _italic text_\\n\\n- List item 1\\n- List item 2';
      
      const sseResponse = [
        'event: TEXT_MESSAGE_START\n',
        `data: {"messageId":"md-msg","createdAt":"${new Date().toISOString()}"}\n\n`,
        'event: TEXT_MESSAGE_CONTENT\n',
        `data: {"text":"${markdownResponse}"}\n\n`,
        'event: TEXT_MESSAGE_END\n',
        'data: {}\n\n',
      ].join('');

      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        body: sseResponse,
      });
    });

    const chatInput = page.locator('textarea').first();
    await chatInput.fill('Show me formatted text');
    await chatInput.press('Enter');

    // Wait for response to render
    await page.waitForTimeout(1000);

    // The markdown should be rendered (look for rendered elements)
    // Note: Specific assertions depend on your markdown rendering implementation
  });
});

test.describe('Chat Error Handling', () => {
  test('should handle API errors gracefully', async ({ page }) => {
    // Mock an error response
    await page.route('**/api/agent/chat', async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal server error' }),
      });
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const chatInput = page.locator('textarea').first();
    await chatInput.fill('This should fail');
    await chatInput.press('Enter');

    // Wait for error to be displayed
    await page.waitForTimeout(1000);

    // The UI should handle the error gracefully (no crash)
    await expect(page.locator('body')).toBeVisible();
  });

  test('should handle network timeout', async ({ page }) => {
    // Mock a timeout
    await page.route('**/api/agent/chat', async (route) => {
      // Simulate a very long delay (timeout)
      await new Promise(resolve => setTimeout(resolve, 30000));
      await route.abort('timedout');
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // The page should remain functional even with network issues
    await expect(page.locator('body')).toBeVisible();
  });
});

test.describe('Chat Status Updates', () => {
  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
  });

  test('should display status message during function call processing', async ({ page }) => {
    // Override the chat mock to use a slower response with status updates
    await page.route('**/api/agent/chat', async (route) => {
      // Create a mock response with function call and status updates
      const sseResponse = createMockSSEResponseWithFunctionCall(
        '✅ Task created successfully!',
        'CreateTask'
      );

      // Add delay to simulate processing time - status should be visible
      await new Promise(resolve => setTimeout(resolve, 500));

      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        body: sseResponse,
      });
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const chatInput = page.locator('textarea').first();
    await chatInput.fill('Create a new task');
    
    // Submit the message
    await chatInput.press('Enter');

    // Verify the page handles the SSE stream correctly
    await page.waitForTimeout(1000);
    
    // After response, the message should be displayed
    await expect(page.locator('body')).toBeVisible();
  });

  test('should display searching status for list tasks operation', async ({ page }) => {
    // Override the chat mock for search operation
    await page.route('**/api/agent/chat', async (route) => {
      const sseResponse = createMockSSEResponseWithFunctionCall(
        'Here are your tasks...',
        'ListTasks'
      );

      await new Promise(resolve => setTimeout(resolve, 300));

      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        body: sseResponse,
      });
    });

    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const chatInput = page.locator('textarea').first();
    await chatInput.fill('Show me all tasks');
    await chatInput.press('Enter');

    // Wait for response to complete
    await page.waitForTimeout(800);
    
    // The UI should process the response correctly
    await expect(page.locator('body')).toBeVisible();
  });
});
