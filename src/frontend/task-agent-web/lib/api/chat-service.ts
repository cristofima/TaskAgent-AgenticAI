/**
 * Chat Service - API client for backend communication
 * Handles all HTTP requests to the TaskAgent backend
 *
 * TODO: Implement streaming support - see STREAMING_ROADMAP.md
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

const API_BASE_URL =
    process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

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
 * Sends a message to the backend (non-streaming)
 * Uses /api/Chat/send endpoint
 */
export async function sendMessage(
    request: SendMessageRequest
): Promise<SendMessageResponse> {
    try {
        const response = await fetch(`${API_BASE_URL}/api/Chat/send`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                message: request.message,
                threadId: request.threadId,
            }),
        });

        if (!response.ok) {
            const errorData: ErrorResponse = await response.json().catch(() => ({
                error: "NetworkError",
                message: `HTTP ${response.status}: ${response.statusText}`,
            }));
            throw new ApiError(
                errorData.message || `HTTP ${response.status}: ${response.statusText}`,
                response.status,
                errorData
            );
        }

        const data: SendMessageResponse = await response.json();
        return data;
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
 * Uses /api/Chat/threads/{threadId}/messages endpoint
 */
export async function getConversation(
    request: GetConversationRequest
): Promise<GetConversationResponse> {
    try {
        const params = new URLSearchParams({
            ...(request.page && { page: request.page.toString() }),
            ...(request.pageSize && { pageSize: request.pageSize.toString() }),
        });

        const response = await fetch(
            `${API_BASE_URL}/api/Chat/threads/${request.threadId}/messages?${params}`,
            {
                method: "GET",
                headers: {
                    "Content-Type": "application/json",
                },
            }
        );

        if (!response.ok) {
            const errorData: ErrorResponse = await response.json().catch(() => ({
                error: "NetworkError",
                message: `HTTP ${response.status}: ${response.statusText}`,
            }));
            throw new ApiError(
                errorData.message || `HTTP ${response.status}: ${response.statusText}`,
                response.status,
                errorData
            );
        }

        const data: GetConversationResponse = await response.json();
        return data;
    } catch (error) {
        if (error instanceof ApiError) {
            throw error;
        }
        throw new ApiError(
            error instanceof Error ? error.message : "Failed to get conversation"
        );
    }
}

/**
 * Lists all conversation threads
 * Uses /api/Chat/threads endpoint (GET)
 */
export async function listThreads(
    request: ListThreadsRequest = {}
): Promise<ListThreadsResponse> {
    try {
        const params = new URLSearchParams({
            ...(request.page && { page: request.page.toString() }),
            ...(request.pageSize && { pageSize: request.pageSize.toString() }),
            ...(request.sortBy && { sortBy: request.sortBy }),
            ...(request.sortOrder && { sortOrder: request.sortOrder }),
            ...(request.isActive !== undefined && { isActive: request.isActive.toString() }),
        });

        const response = await fetch(
            `${API_BASE_URL}/api/Chat/threads?${params}`,
            {
                method: "GET",
                headers: {
                    "Content-Type": "application/json",
                },
            }
        );

        if (!response.ok) {
            const errorData: ErrorResponse = await response.json().catch(() => ({
                error: "NetworkError",
                message: `HTTP ${response.status}: ${response.statusText}`,
            }));
            throw new ApiError(
                errorData.message || `HTTP ${response.status}: ${response.statusText}`,
                response.status,
                errorData
            );
        }

        const data: ListThreadsResponse = await response.json();
        return data;
    } catch (error) {
        if (error instanceof ApiError) {
            throw error;
        }
        throw new ApiError(
            error instanceof Error ? error.message : "Failed to list threads"
        );
    }
}

/**
 * Creates a new conversation thread
 * Uses /api/Chat/threads endpoint (POST)
 */
export async function createThread(): Promise<{ threadId: string }> {
    try {
        const response = await fetch(`${API_BASE_URL}/api/Chat/threads`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
        });

        if (!response.ok) {
            const errorData: ErrorResponse = await response.json().catch(() => ({
                error: "NetworkError",
                message: `HTTP ${response.status}: ${response.statusText}`,
            }));
            throw new ApiError(
                errorData.message || `HTTP ${response.status}: ${response.statusText}`,
                response.status,
                errorData
            );
        }

        const data = await response.json();
        return data;
    } catch (error) {
        if (error instanceof ApiError) {
            throw error;
        }
        throw new ApiError(
            error instanceof Error ? error.message : "Failed to create thread"
        );
    }
}

/**
 * Deletes a conversation thread
 * Uses /api/Chat/threads/{threadId} endpoint (DELETE)
 */
export async function deleteThread(threadId: string): Promise<void> {
    try {
        const response = await fetch(`${API_BASE_URL}/api/Chat/threads/${threadId}`, {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
            },
        });

        if (!response.ok) {
            const errorData: ErrorResponse = await response.json().catch(() => ({
                error: "NetworkError",
                message: `HTTP ${response.status}: ${response.statusText}`,
            }));
            throw new ApiError(
                errorData.message || `HTTP ${response.status}: ${response.statusText}`,
                response.status,
                errorData
            );
        }
    } catch (error) {
        if (error instanceof ApiError) {
            throw error;
        }
        throw new ApiError(
            error instanceof Error ? error.message : "Failed to delete thread"
        );
    }
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
