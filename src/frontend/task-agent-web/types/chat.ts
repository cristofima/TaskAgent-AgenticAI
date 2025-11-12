/**
 * Chat API Contract Types
 * Based on NEXTJS_FRONTEND_PROPOSAL.md
 */

/**
 * Metadata for message context
 */
export interface MessageMetadata {
    functionCalls?: FunctionCallInfo[];
    citations?: Citation[];
    attachments?: Attachment[];
}

/**
 * Information about function calls made by the agent
 */
export interface FunctionCallInfo {
    name: string;
    arguments: string;
    result?: string;
}

/**
 * Citation information for sources
 */
export interface Citation {
    source: string;
    url?: string;
}

/**
 * Attachment information
 */
export interface Attachment {
    id: string;
    name: string;
    type: string;
    size: number;
    url: string;
}

/**
 * Base message structure for chat communication
 */
export interface ChatMessage {
    id: string;
    role: "user" | "assistant" | "system";
    content: string;
    timestamp: string;
    metadata?: MessageMetadata;
}

/**
 * Request to send a new message
 */
export interface SendMessageRequest {
    message: string;
    threadId?: string;
    attachments?: File[];
}

/**
 * Response from sending a message (non-streaming)
 */
export interface SendMessageResponse {
    threadId: string;
    message: ChatMessage;
    suggestions?: string[];
}

/**
 * Streaming chunk response types
 */
export type StreamChunkType =
    | "text"
    | "function_call"
    | "function_result"
    | "metadata"
    | "complete";

/**
 * Streaming chunk response
 */
export interface StreamChunk {
    type: StreamChunkType;
    content?: string;
    data?: unknown;
}

/**
 * Conversation thread info
 */
export interface ConversationThread {
    id: string;
    userId?: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    preview?: string;
}

/**
 * Request to list conversation threads
 */
export interface ListThreadsRequest {
    page?: number;
    pageSize?: number;
    sortBy?: "createdAt" | "updatedAt";
    sortOrder?: "asc" | "desc";
}

/**
 * Response with paginated threads
 */
export interface ListThreadsResponse {
    threads: ConversationThread[];
    totalCount: number;
    page: number;
    pageSize: number;
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
    threadId: string;
    messages: ChatMessage[];
    totalCount: number;
    page: number;
    pageSize: number;
}
