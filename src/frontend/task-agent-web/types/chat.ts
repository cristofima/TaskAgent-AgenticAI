/**
 * Chat API Contract Types
 * Based on backend Swagger API specification
 */

/**
 * Information about function calls made by the agent
 */
export interface FunctionCallInfo {
    name: string | null;
    arguments: string | null;
    result?: string | null;
    timestamp: string;
}

/**
 * Metadata for message context
 */
export interface MessageMetadata {
    functionCalls?: FunctionCallInfo[] | null;
    additionalData?: Record<string, unknown> | null;
    suggestions?: string[] | null;
    violations?: string[] | null;
    categoryScores?: Record<string, number> | null;
}

/**
 * Base message structure for chat communication
 */
export interface ChatMessage {
    id: string | null;
    content: string | null;
    role: string | null;
    createdAt: string;
    metadata?: MessageMetadata;
}

/**
 * Request to send a new message
 */
export interface SendMessageRequest {
    message: string | null;
    serializedState?: string | null; // ThreadDbKey (GUID) for PostgreSQL conversation persistence
}

/**
 * Response from sending a message via custom Agent endpoint
 * Accumulated from SSE stream events
 */
export interface SendMessageResponse {
    message: string | null;
    serializedState?: string | null; // ThreadDbKey (GUID) for next request - maintains conversation
    messageId: string | null;
    timestamp?: string; // For backward compatibility
    createdAt: string;
    metadata?: MessageMetadata;
    suggestions?: string[] | null;
}

/**
 * Conversation thread info
 */
export interface ConversationThread {
    id: string;
    title: string | null;
    preview: string | null;
    createdAt: string;
    updatedAt: string;
    messageCount: number;
    isActive: boolean;
    serializedState?: string | null; // AG-UI protocol: serialized thread state for resuming
}

/**
 * Request to list chat threads
 */
export interface ListThreadsRequest {
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortOrder?: string;
    isActive?: boolean;
}

/**
 * Response with paginated threads
 */
export interface ListThreadsResponse {
    threads: ConversationThread[] | null;
    totalCount: number;
    page: number;
    pageSize: number;
    hasMore: boolean;
}

/**
 * Request to get chat history
 */
export interface GetConversationRequest {
    threadId: string;
    page?: number;
    pageSize?: number;
}

/**
 * Response with conversation messages
 */
export interface GetConversationResponse {
    threadId: string | null;
    serializedState?: string | null; // ThreadDbKey (GUID) for PostgreSQL conversation persistence
    messages: ChatMessage[] | null;
    totalCount: number;
    page: number;
    pageSize: number;
    hasMore: boolean;
}

/**
 * Error response from API
 */
export interface ErrorResponse {
    error: string | null;
    message: string | null;
    details?: unknown;
    violations?: string[] | null;
    categoryScores?: Record<string, number> | null;
    threadId?: string | null;
    serializedState?: string | null; // ThreadDbKey (GUID) - preserved even when message blocked
    messageId?: string | null;
    createdAt?: string;
}
