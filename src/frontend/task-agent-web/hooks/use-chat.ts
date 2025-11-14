"use client";

/**
 * Custom hook for chat functionality
 * Manages chat state and API communication with .NET backend
 */

import { useState, useCallback, FormEvent } from "react";
import { sendMessage, getConversation } from "@/lib/api/chat-service";
import type { ChatMessage } from "@/types/chat";
import { PAGINATION } from "@/lib/constants";

export interface UseChatOptions {
    onError?: (error: Error) => void;
    onThreadCreated?: (threadId: string) => void;
}

export interface UseChatReturn {
    messages: ChatMessage[];
    input: string;
    isLoading: boolean;
    error: Error | undefined;
    threadId: string | null;
    handleInputChange: (
        e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
    ) => void;
    handleSubmit: (e: FormEvent<HTMLFormElement>) => Promise<void>;
    clearMessages: () => void;
    setInput: (input: string) => void;
    sendSuggestion: (suggestion: string) => Promise<void>;
    loadConversation: (threadId: string) => Promise<void>;
    setThreadId: (threadId: string | null) => void;
}

/**
 * Hook for managing chat state and interactions (non-streaming)
 * Uses /api/Chat/send endpoint
 */
export function useChat(options: UseChatOptions = {}): UseChatReturn {
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<Error | undefined>();
    const [threadId, setThreadId] = useState<string | null>(null);

    const handleInputChange = useCallback(
        (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>): void => {
            setInput(e.target.value);
        },
        []
    );

    /**
     * Internal helper to send a message and handle the response
     */
    const sendMessageInternal = useCallback(
        async (message: string): Promise<void> => {
            if (!message.trim() || isLoading) return;

            setError(undefined);

            // Add user message to UI immediately (optimistic update)
            const userChatMessage: ChatMessage = {
                id: `temp-${Date.now()}`,
                role: "user",
                content: message,
                createdAt: new Date().toISOString(),
            };
            setMessages((prev) => [...prev, userChatMessage]);
            setIsLoading(true);

            try {
                // Send message to backend
                const response = await sendMessage({
                    message,
                    threadId: threadId,
                });

                // Update threadId if new conversation
                if (response.threadId && !threadId) {
                    setThreadId(response.threadId);
                    // Notify that a new thread was created
                    options.onThreadCreated?.(response.threadId);
                }

                // Add assistant response to messages
                const assistantMessage: ChatMessage = {
                    id: response.messageId || `msg-${Date.now()}`,
                    role: "assistant",
                    content: response.message || "",
                    createdAt: response.createdAt,
                    metadata: {
                        ...response.metadata,
                        suggestions: response.suggestions || [],
                    },
                };
                setMessages((prev) => [...prev, assistantMessage]);
            } catch (err) {
                const validatedError = err instanceof Error ? err : new Error("Failed to send message");
                setError(validatedError);
                options.onError?.(validatedError);

                // Remove the user message on error
                setMessages((prev) => prev.slice(0, -1));
            } finally {
                setIsLoading(false);
            }
        },
        [isLoading, threadId, options]
    );

    const handleSubmit = useCallback(
        async (e: FormEvent<HTMLFormElement>): Promise<void> => {
            e.preventDefault();
            const userMessage = input.trim();
            setInput(""); // Clear input immediately
            await sendMessageInternal(userMessage);
        },
        [input, sendMessageInternal]
    );

    const sendSuggestion = useCallback(
        async (suggestion: string): Promise<void> => {
            await sendMessageInternal(suggestion);
        },
        [sendMessageInternal]
    );

    const clearMessages = useCallback((): void => {
        setMessages([]);
        setThreadId(null);
        setError(undefined);
    }, []);

    /**
     * Loads a conversation's message history and updates the chat state
     *
     * @param newThreadId - The thread ID to load
     * @throws {Error} When conversation cannot be loaded
     */
    const loadConversation = useCallback(
        async (newThreadId: string): Promise<void> => {
            setIsLoading(true);
            setError(undefined);

            try {
                // Get conversation history
                const response = await getConversation({
                    threadId: newThreadId,
                    page: 1,
                    pageSize: PAGINATION.CONVERSATION_PAGE_SIZE,
                });

                // Update thread ID
                setThreadId(newThreadId);

                // Convert backend messages to frontend format
                const loadedMessages: ChatMessage[] = (response.messages || []).map((msg) => ({
                    id: msg.id || `msg-${Date.now()}-${Math.random()}`,
                    role: msg.role || "assistant",
                    content: msg.content || "",
                    createdAt: msg.createdAt,
                    metadata: msg.metadata,
                }));

                setMessages(loadedMessages);
            } catch (err) {
                const validatedError =
                    err instanceof Error ? err : new Error("Failed to load conversation");
                setError(validatedError);
                options.onError?.(validatedError);
            } finally {
                setIsLoading(false);
            }
        },
        [options]
    );

    const setThreadIdManually = useCallback((newThreadId: string | null): void => {
        setThreadId(newThreadId);
    }, []);

    return {
        messages,
        input,
        isLoading,
        error,
        threadId,
        handleInputChange,
        handleSubmit,
        clearMessages,
        setInput,
        sendSuggestion,
        loadConversation,
        setThreadId: setThreadIdManually,
    };
}



