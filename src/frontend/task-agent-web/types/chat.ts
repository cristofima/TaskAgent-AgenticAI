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
    threadId?: string | null;
}

/**
 * Response from sending a message (non-streaming)
 * Maps to backend ChatResponse from /api/Chat/send
 */
export interface SendMessageResponse {
    message: string | null;
    threadId: string | null;
    messageId: string | null;
    createdAt: string;
    metadata?: MessageMetadata;
    suggestions?: string[] | null;
}

// TODO: Implement streaming support
// See STREAMING_ROADMAP.md for implementation plan

/**
 * Conversation thread info
 */
export interface ConversationThread {
    id: string | null;
    title: string | null;
    preview: string | null;
    createdAt: string;
    updatedAt: string;
    messageCount: number;
    isActive: boolean;
}

/**
 * Request to list conversation threads
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
 * Request to get conversation history
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
}
