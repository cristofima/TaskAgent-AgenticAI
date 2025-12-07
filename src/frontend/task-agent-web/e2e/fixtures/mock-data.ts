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
 * Includes STATUS_UPDATE events to simulate real backend behavior
 */
export function createMockSSEResponse(message: string, threadId: string = 'thread-mock-1'): string {
  const events = [
    // Status update events (simulating backend processing stages)
    `data: {"type":"STATUS_UPDATE","status":"Processing your request..."}\n\n`,
    // Message events
    `event: TEXT_MESSAGE_START\ndata: {"type":"TEXT_MESSAGE_START","messageId":"mock-msg-${Date.now()}","createdAt":"${new Date().toISOString()}"}\n\n`,
    `event: TEXT_MESSAGE_CONTENT\ndata: {"type":"TEXT_MESSAGE_CONTENT","delta":"${message}"}\n\n`,
    `event: TEXT_MESSAGE_END\ndata: {"type":"TEXT_MESSAGE_END"}\n\n`,
    `event: THREAD_STATE\ndata: {"type":"THREAD_STATE","serializedState":"${threadId}"}\n\n`,
    'data: [DONE]\n\n',
  ];
  return events.join('');
}

/**
 * Create a mock SSE stream response with a function call (for testing status updates)
 * Simulates backend calling a function tool like CreateTask
 * Now includes AG-UI standard STEP_STARTED/STEP_FINISHED events
 */
export function createMockSSEResponseWithFunctionCall(
  message: string, 
  functionName: string = 'CreateTask',
  threadId: string = 'thread-mock-1'
): string {
  // Status messages are now dynamically generated from [Description] attributes in backend
  // These are examples of what the backend generates
  const statusMessages: Record<string, string> = {
    'CreateTask': 'Creating task...',
    'ListTasks': 'Listing tasks...',
    'UpdateTask': 'Updating task...',
    'DeleteTask': 'Deleting task...',
    'GetTaskDetails': 'Getting task details...',
    'GetTaskSummary': 'Generating summary...',
  };
  
  const events = [
    // Initial status update
    `data: {"type":"STATUS_UPDATE","status":"Processing your request..."}\n\n`,
    // AG-UI standard STEP_STARTED event (function name for debugging/logging)
    `data: {"type":"STEP_STARTED","stepName":"${functionName}Async"}\n\n`,
    // Dynamic status update (user-friendly message from [Description] attribute)
    `data: {"type":"STATUS_UPDATE","status":"${statusMessages[functionName] || 'Processing...'}"}\n\n`,
    `data: {"type":"TOOL_CALL_START","toolName":"${functionName}Async","toolCallId":"call-1"}\n\n`,
    `data: {"type":"TOOL_CALL_RESULT","toolCallId":"call-1","result":"Success"}\n\n`,
    // AG-UI standard STEP_FINISHED event
    `data: {"type":"STEP_FINISHED","stepName":"${functionName}Async"}\n\n`,
    // Message events
    `data: {"type":"TEXT_MESSAGE_START","messageId":"mock-msg-${Date.now()}","createdAt":"${new Date().toISOString()}"}\n\n`,
    `data: {"type":"TEXT_MESSAGE_CONTENT","delta":"${message}"}\n\n`,
    `data: {"type":"TEXT_MESSAGE_END"}\n\n`,
    `data: {"type":"THREAD_STATE","serializedState":"${threadId}"}\n\n`,
    'data: [DONE]\n\n',
  ];
  return events.join('');
}
