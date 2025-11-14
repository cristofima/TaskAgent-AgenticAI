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
            throw new ApiError(
                errorData.message || `HTTP ${response.status}: ${response.statusText}`,
                response.status,
                errorData
            );
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
 * Sends a message to the backend (non-streaming)
 * Uses /api/Chat/send endpoint
 */
export async function sendMessage(
    request: SendMessageRequest
): Promise<SendMessageResponse> {
    return apiFetch<SendMessageResponse>(
        "/api/Chat/send",
        {
            method: "POST",
            body: JSON.stringify(request),
        },
        "Failed to send message"
    );
}

/**
 * Gets conversation history for a thread
 * Uses /api/Chat/threads/{threadId}/messages endpoint
 */
export async function getConversation(
    request: GetConversationRequest
): Promise<GetConversationResponse> {
    const params = new URLSearchParams({
        ...(request.page && { page: request.page.toString() }),
        ...(request.pageSize && { pageSize: request.pageSize.toString() }),
    });

    return apiFetch<GetConversationResponse>(
        `/api/Chat/threads/${request.threadId}/messages?${params}`,
        { method: "GET" },
        "Failed to get conversation"
    );
}

/**
 * Lists all conversation threads
 * Uses /api/Chat/threads endpoint (GET)
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
        `/api/Chat/threads?${params}`,
        { method: "GET" },
        "Failed to list threads"
    );
}

/**
 * Creates a new conversation thread
 * Uses /api/Chat/threads endpoint (POST)
 */
export async function createThread(): Promise<{ threadId: string }> {
    return apiFetch<{ threadId: string }>(
        "/api/Chat/threads",
        { method: "POST" },
        "Failed to create thread"
    );
}

/**
 * Deletes a conversation thread
 * Uses /api/Chat/threads/{threadId} endpoint (DELETE)
 */
export async function deleteThread(threadId: string): Promise<void> {
    return apiFetch<void>(
        `/api/Chat/threads/${threadId}`,
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
