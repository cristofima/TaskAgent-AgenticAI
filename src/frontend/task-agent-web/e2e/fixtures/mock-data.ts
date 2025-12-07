/**
 * Mock data for E2E tests
 * All API responses are mocked to ensure reliable, fast tests
 */

/**
 * Mock conversation type for tests
 */
export interface MockConversation {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messageCount: number;
}

/**
 * Mock message type for tests
 */
export interface MockMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
}

/**
 * Mock conversations for testing
 */
export const mockConversations: MockConversation[] = [
  {
    id: 'conv-1',
    title: 'Task Management Discussion',
    createdAt: '2025-12-06T10:00:00Z',
    updatedAt: '2025-12-06T10:30:00Z',
    messageCount: 5,
  },
  {
    id: 'conv-2',
    title: 'Project Planning',
    createdAt: '2025-12-05T14:00:00Z',
    updatedAt: '2025-12-05T15:00:00Z',
    messageCount: 3,
  },
];

/**
 * Mock chat messages for testing
 */
export const mockMessages: MockMessage[] = [
  {
    id: 'msg-1',
    role: 'user',
    content: 'Create a new task for testing',
    createdAt: '2025-12-06T10:00:00Z',
  },
  {
    id: 'msg-2',
    role: 'assistant',
    content: '✅ **Task Created Successfully!**\n\nI\'ve created a new task:\n- **Title:** Testing Task\n- **Priority:** Medium\n- **Status:** Pending',
    createdAt: '2025-12-06T10:00:05Z',
  },
];

/**
 * Create a mock SSE stream response for chat
 * Returns events in the format expected by the frontend
 */
export function createMockSSEResponse(message: string, threadId: string = 'thread-mock-1'): string {
  const events = [
    `event: TEXT_MESSAGE_START\ndata: {"messageId":"mock-msg-${Date.now()}","createdAt":"${new Date().toISOString()}"}\n\n`,
    `event: TEXT_MESSAGE_CONTENT\ndata: {"text":"${message}"}\n\n`,
    `event: TEXT_MESSAGE_END\ndata: {}\n\n`,
    `event: THREAD_STATE\ndata: {"threadId":"${threadId}","serializedState":"mock-state"}\n\n`,
  ];
  return events.join('');
}
