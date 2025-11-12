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
        public response?: unknown
    ) {
        super(message);
        this.name = "ApiError";
    }
}

/**
 * Sends a message to the backend (non-streaming)
 */
export async function sendMessage(
    request: SendMessageRequest
): Promise<SendMessageResponse> {
    try {
        const response = await fetch(`${API_BASE_URL}/api/chat`, {
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
            const errorData = await response.json().catch(() => ({}));
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
            error instanceof Error ? error.message : "Failed to send message"
        );
    }
}

/**
 * Gets conversation history for a thread
 */
export async function getConversation(
    request: GetConversationRequest
): Promise<GetConversationResponse> {
    try {
        const params = new URLSearchParams({
            threadId: request.threadId,
            ...(request.page && { page: request.page.toString() }),
            ...(request.pageSize && { pageSize: request.pageSize.toString() }),
        });

        const response = await fetch(
            `${API_BASE_URL}/api/chat/history?${params}`,
            {
                method: "GET",
                headers: {
                    "Content-Type": "application/json",
                },
            }
        );

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new ApiError(
                errorData.message ||
                `HTTP ${response.status}: ${response.statusText}`,
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
            error instanceof Error ? error.message : "Failed to get conversation"
        );
    }
}

/**
 * Lists all conversation threads
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
        });

        const response = await fetch(
            `${API_BASE_URL}/api/chat/threads?${params}`,
            {
                method: "GET",
                headers: {
                    "Content-Type": "application/json",
                },
            }
        );

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new ApiError(
                errorData.message ||
                `HTTP ${response.status}: ${response.statusText}`,
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
            error instanceof Error ? error.message : "Failed to list threads"
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
