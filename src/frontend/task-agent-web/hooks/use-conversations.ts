"use client";

/**
 * Custom hook for conversation/thread management
 * Handles listing, loading, and deleting conversations
 */

import { useState, useEffect, useCallback } from "react";
import { listThreads, deleteThread, getConversation } from "@/lib/api/chat-service";
import type { ConversationThread, GetConversationResponse } from "@/types/chat";

export interface UseConversationsOptions {
    autoLoad?: boolean;
    pageSize?: number;
}

export interface UseConversationsReturn {
    conversations: ConversationThread[];
    isLoading: boolean;
    error: Error | undefined;
    currentThreadId: string | null;
    hasMore: boolean;
    loadConversations: () => Promise<void>;
    loadConversation: (threadId: string) => Promise<GetConversationResponse>;
    deleteConversation: (threadId: string) => Promise<void>;
    setCurrentThreadId: (threadId: string | null) => void;
    createNewConversation: () => void;
}

/**
 * Hook for managing conversation list and operations
 */
export function useConversations(
    options: UseConversationsOptions = {}
): UseConversationsReturn {
    const { autoLoad = true, pageSize = 20 } = options;

    const [conversations, setConversations] = useState<ConversationThread[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<Error | undefined>();
    const [currentThreadId, setCurrentThreadIdState] = useState<string | null>(null);
    const [hasMore, setHasMore] = useState(false);

    // Load conversations from backend
    const loadConversations = useCallback(async () => {
        setIsLoading(true);
        setError(undefined);

        try {
            const response = await listThreads({
                page: 1,
                pageSize,
                sortBy: "UpdatedAt",
                sortOrder: "desc",
            });

            setConversations(response.threads ?? []);
            setHasMore(response.hasMore);
        } catch (err) {
            const errorObj =
                err instanceof Error ? err : new Error("Failed to load conversations");
            setError(errorObj);
            console.error("Failed to load conversations:", err);
        } finally {
            setIsLoading(false);
        }
    }, [pageSize]);

    // Load a specific conversation's history
    const loadConversation = useCallback(
        async (threadId: string) => {
            setIsLoading(true);
            setError(undefined);

            try {
                // Get conversation history
                const response = await getConversation({
                    threadId,
                    page: 1,
                    pageSize: 50,
                });

                // Set as current thread
                setCurrentThreadIdState(threadId);

                // Store in localStorage for persistence
                if (typeof window !== "undefined") {
                    localStorage.setItem("taskagent_current_thread", threadId);
                }

                return response;
            } catch (err) {
                const errorObj =
                    err instanceof Error ? err : new Error("Failed to load conversation");
                setError(errorObj);
                console.error("Failed to load conversation:", err);
                throw errorObj;
            } finally {
                setIsLoading(false);
            }
        },
        []
    );

    // Delete a conversation
    const deleteConversation = useCallback(
        async (threadId: string) => {
            setError(undefined);

            try {
                await deleteThread(threadId);

                // Remove from local state
                setConversations((prev) => prev.filter((c) => c.id !== threadId));

                // Clear current thread if it was deleted
                if (currentThreadId === threadId) {
                    setCurrentThreadIdState(null);
                    if (typeof window !== "undefined") {
                        localStorage.removeItem("taskagent_current_thread");
                    }
                }
            } catch (err) {
                const errorObj =
                    err instanceof Error ? err : new Error("Failed to delete conversation");
                setError(errorObj);
                console.error("Failed to delete conversation:", err);
                throw errorObj;
            }
        },
        [currentThreadId]
    );

    // Set current thread ID
    const setCurrentThreadId = useCallback((threadId: string | null) => {
        setCurrentThreadIdState(threadId);

        // Persist to localStorage
        if (typeof window !== "undefined") {
            if (threadId) {
                localStorage.setItem("taskagent_current_thread", threadId);
            } else {
                localStorage.removeItem("taskagent_current_thread");
            }
        }
    }, []);

    // Create new conversation
    const createNewConversation = useCallback(() => {
        setCurrentThreadIdState(null);
        if (typeof window !== "undefined") {
            localStorage.removeItem("taskagent_current_thread");
        }
    }, []);

    // Auto-load on mount
    useEffect(() => {
        if (autoLoad) {
            loadConversations();
        }

        // Restore current thread from localStorage
        if (typeof window !== "undefined") {
            const savedThreadId = localStorage.getItem("taskagent_current_thread");
            if (savedThreadId) {
                setCurrentThreadIdState(savedThreadId);
            }
        }
    }, [autoLoad, loadConversations]);

    return {
        conversations,
        isLoading,
        error,
        currentThreadId,
        hasMore,
        loadConversations,
        loadConversation,
        deleteConversation,
        setCurrentThreadId,
        createNewConversation,
    };
}
