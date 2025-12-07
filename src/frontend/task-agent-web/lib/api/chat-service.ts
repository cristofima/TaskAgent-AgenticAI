/**
 * Chat Service - API client for backend communication
 * Handles all HTTP requests to the TaskAgent backend
 */

import type {
    SendMessageRequest,
    SendMessageResponse,
    ListThreadsRequest,
    ListThreadsResponse,
    GetConversationRequest,
    GetConversationResponse,
    ErrorResponse,
} from "@/types/chat";
import { API } from "@/lib/constants";

const API_BASE_URL = API.BASE_URL;

/**
 * Error thrown when API request fails
 */
export class ApiError extends Error {
    constructor(
        message: string,
        public statusCode?: number,
        public response?: ErrorResponse
    ) {
        super(message);
        this.name = "ApiError";
    }
}

/**
 * Generic fetch wrapper with consistent error handling
 * Reduces code duplication across all API calls
 *
 * @param endpoint - API endpoint path (relative to base URL)
 * @param options - Fetch request options
 * @param errorMessage - Custom error message for this operation
 * @returns Parsed JSON response or void for DELETE requests
 */
async function apiFetch<T>(
    endpoint: string,
    options: RequestInit = {},
    errorMessage: string = "Request failed"
): Promise<T> {
    try {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            headers: {
                "Content-Type": "application/json",
                ...options.headers,
            },
            ...options,
        });

        if (!response.ok) {
            const errorData: ErrorResponse = await response.json().catch(() => ({
                error: "NetworkError",
                message: `HTTP ${response.status}: ${response.statusText}`,
            }));
            const apiError = new ApiError(
                errorData.message || `HTTP ${response.status}: ${response.statusText}`,
                response.status,
                errorData
            );
            throw apiError;
        }

        // Return void for DELETE requests (204 No Content)
        if (response.status === 204 || options.method === "DELETE") {
            return undefined as T;
        }

        return await response.json();
    } catch (error) {
        if (error instanceof ApiError) {
            throw error;
        }
        throw new ApiError(
            error instanceof Error ? error.message : errorMessage
        );
    }
}

/**
 * Callback options for streaming message responses
 * Enables progressive UI updates as text arrives
 */
export interface StreamingCallbacks {
    /** Called when message streaming starts */
    onStart?: (messageId: string, createdAt: string) => void;
    /** Called for each text chunk received */
    onTextChunk?: (delta: string, fullText: string) => void;
    /** Called when a tool/function call starts */
    onToolCallStart?: (toolName: string, toolCallId: string) => void;
    /** Called when a tool/function call completes */
    onToolCallResult?: (toolCallId: string, result: string) => void;
    /** Called when streaming completes */
    onComplete?: (serializedState: string | null) => void;
    /** Called on error */
    onError?: (error: ApiError) => void;
    /** Called when backend sends a status update (processing stages) */
    onStatusUpdate?: (status: string) => void;
}

/**
 * Sends a message to the backend via custom Agent endpoint with PostgreSQL persistence
 * Uses /api/agent/chat endpoint with Server-Sent Events (SSE) streaming
 * Backend maintains conversation state in PostgreSQL via serializedState (ThreadDbKey)
 */
export async function sendMessage(
    request: SendMessageRequest
): Promise<SendMessageResponse> {
    // Use streaming version without callbacks for backward compatibility
    return sendMessageWithStreaming(request, {});
}

/**
 * Sends a message with streaming callbacks for progressive UI rendering
 * Enables ChatGPT-like progressive text display
 */
export async function sendMessageWithStreaming(
    request: SendMessageRequest,
    callbacks: StreamingCallbacks = {}
): Promise<SendMessageResponse> {
    try {
        // Send current message with serializedState for conversation continuity
        // Backend loads full conversation history from PostgreSQL using ThreadDbKey
        const requestBody: {
            messages: Array<{ role: string; content: string }>;
            serializedState?: string;
        } = {
            messages: [
                {
                    role: "user",
                    content: request.message || "",
                },
            ],
        };

        // Add serializedState if available for conversation continuity
        if (request.serializedState) {
            requestBody.serializedState = request.serializedState;
        }

        const response = await fetch(`${API_BASE_URL}/api/agent/chat`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                Accept: "text/event-stream", // SSE format
            },
            body: JSON.stringify(requestBody),
        });

        if (!response.ok) {
            throw new ApiError(
                `HTTP ${response.status}: ${response.statusText}`,
                response.status
            );
        }

        // Parse SSE stream
        const reader = response.body?.getReader();
        const decoder = new TextDecoder();
        let buffer = "";
        let serializedState: string | null = null; // Captured from THREAD_STATE event
        let messageId: string | null = null;
        let fullMessage = "";
        let createdAt: string | null = null;

        if (!reader) {
            throw new ApiError("Response body is not readable");
        }

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, { stream: true });
            const lines = buffer.split("\n");
            buffer = lines.pop() || ""; // Keep incomplete line in buffer

            for (const line of lines) {
                if (line.startsWith("data: ")) {
                    const dataContent = line.slice(6); // Remove "data: " prefix
                    
                    // Handle [DONE] event - end of stream
                    if (dataContent === "[DONE]") {
                        break;
                    }

                    try {
                        const event = JSON.parse(dataContent);

                        // Handle TEXT_MESSAGE_START event
                        if (event.type === "TEXT_MESSAGE_START" && event.messageId) {
                            messageId = event.messageId;
                            createdAt = event.createdAt || new Date().toISOString();
                            // Callback: Streaming started
                            if (messageId && createdAt) {
                                callbacks.onStart?.(messageId, createdAt);
                            }
                        }

                        // Handle TEXT_MESSAGE_CONTENT event (streaming chunks)
                        if (event.type === "TEXT_MESSAGE_CONTENT" && event.delta) {
                            fullMessage += event.delta;
                            // Callback: Progressive text chunk for UI rendering
                            callbacks.onTextChunk?.(event.delta, fullMessage);
                        }

                        // Handle TEXT_MESSAGE_END event
                        if (event.type === "TEXT_MESSAGE_END") {
                            // Message complete - callback handled in onComplete
                        }

                        // Handle TOOL_CALL_START event (function execution started)
                        if (event.type === "TOOL_CALL_START") {
                            // Callback: Tool call started (for multi-agent scenarios)
                            callbacks.onToolCallStart?.(event.toolName || "unknown", event.toolCallId || "");
                        }

                        // Handle TOOL_CALL_RESULT event (function execution completed)
                        if (event.type === "TOOL_CALL_RESULT") {
                            // Callback: Tool call completed
                            callbacks.onToolCallResult?.(event.toolCallId || "", event.result || "");
                        }

                        // Handle STATUS_UPDATE event (processing stage updates from backend)
                        if (event.type === "STATUS_UPDATE" && event.status) {
                            // Callback: Status update for UI loading indicator
                            callbacks.onStatusUpdate?.(event.status);
                        }

                        // Handle CONTENT_FILTER event (Azure OpenAI content policy violation)
                        // This provides a ChatGPT-like UX where the blocked message appears in chat
                        if (event.type === "CONTENT_FILTER") {
                            // Update streaming message with the content filter message
                            const filterMessage = event.message || "I'm unable to assist with that request as it may violate content policies.";
                            fullMessage = filterMessage;
                            messageId = event.messageId || `cf-${Date.now()}`;
                            // Trigger text chunk callback so UI updates immediately
                            callbacks.onTextChunk?.(filterMessage, filterMessage);
                            // Don't throw - let stream continue to get thread state for persistence
                        }

                        // Handle THREAD_STATE event (serializedState for next request)
                        if (event.type === "THREAD_STATE" && event.serializedState) {
                            serializedState = event.serializedState;
                            // Callback: Stream complete with state
                            callbacks.onComplete?.(serializedState);
                        }

                        // Handle RUN_ERROR event
                        if (event.type === "RUN_ERROR") {
                            const apiError = new ApiError(
                                event.message || "Agent run failed",
                                500,
                                {
                                    error: event.error || "RunError",
                                    message: event.message || "Agent run failed",
                                }
                            );
                            // Callback: Error occurred
                            callbacks.onError?.(apiError);
                            throw apiError;
                        }
                    } catch (parseError) {
                        // Only log if it's a JSON parse error, not our thrown ApiError
                        if (!(parseError instanceof ApiError)) {
                            console.warn("Failed to parse SSE event:", line, parseError);
                        } else {
                            throw parseError;
                        }
                    }
                }
            }
        }

        // Return accumulated response with serializedState
        return {
            message: fullMessage || "No response received",
            serializedState: serializedState || undefined,
            messageId: messageId || `msg-${Date.now()}`,
            timestamp: createdAt || new Date().toISOString(),
            createdAt: createdAt || new Date().toISOString(),
        };
    } catch (error) {
        if (error instanceof ApiError) {
            throw error;
        }
        throw new ApiError(
            error instanceof Error ? error.message : "Failed to send message"
        );
    }
}

/**
 * Gets conversation history for a thread
 * Uses /api/conversations/{threadId}/messages endpoint
 */
export async function getConversation(
    request: GetConversationRequest
): Promise<GetConversationResponse> {
    const params = new URLSearchParams({
        ...(request.page && { page: request.page.toString() }),
        ...(request.pageSize && { pageSize: request.pageSize.toString() }),
    });

    const response = await apiFetch<GetConversationResponse>(
        `/api/conversations/${request.threadId}/messages?${params}`,
        { method: "GET" },
        "Failed to get conversation"
    );

    return response;
}

/**
 * Lists all conversation threads
 * Uses /api/conversations endpoint (GET)
 */
export async function listThreads(
    request: ListThreadsRequest = {}
): Promise<ListThreadsResponse> {
    const params = new URLSearchParams({
        ...(request.page && { page: request.page.toString() }),
        ...(request.pageSize && { pageSize: request.pageSize.toString() }),
        ...(request.sortBy && { sortBy: request.sortBy }),
        ...(request.sortOrder && { sortOrder: request.sortOrder }),
        ...(request.isActive !== undefined && { isActive: request.isActive.toString() }),
    });

    return apiFetch<ListThreadsResponse>(
        `/api/conversations?${params}`,
        { method: "GET" },
        "Failed to list threads"
    );
}

/**
 * Creates a new chat thread
 * Note: With AG UI, threads are created automatically on first message
 * This method is kept for API compatibility but may not be needed
 */
export async function createThread(): Promise<{ threadId: string }> {
    // With AG UI Protocol, threads are created implicitly on first message
    // Return a placeholder - actual threadId comes from first sendMessage response
    return Promise.resolve({ threadId: "" });
}

/**
 * Deletes a conversation thread
 * Uses /api/conversations/{threadId} endpoint (DELETE)
 */
export async function deleteThread(threadId: string): Promise<void> {
    return apiFetch<void>(
        `/api/conversations/${threadId}`,
        { method: "DELETE" },
        "Failed to delete thread"
    );
}

/**
 * Validates API connection
 */
export async function validateApiConnection(): Promise<boolean> {
    try {
        const response = await fetch(`${API_BASE_URL}/api/health`, {
            method: "GET",
        });
        return response.ok;
    } catch {
        return false;
    }
}
