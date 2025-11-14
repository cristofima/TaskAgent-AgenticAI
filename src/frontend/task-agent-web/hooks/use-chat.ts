"use client";

/**
 * Custom hook for chat functionality
 * Manages chat state and API communication with .NET backend
 */

import { useState, useCallback, FormEvent } from "react";
import { sendMessage, getConversation } from "@/lib/api/chat-service";
import type { ChatMessage } from "@/types/chat";

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
        (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
            setInput(e.target.value);
        },
        []
    );

    const handleSubmit = useCallback(
        async (e: FormEvent<HTMLFormElement>) => {
            e.preventDefault();
            if (!input.trim() || isLoading) return;

            const userMessage = input.trim();
            setInput(""); // Clear input immediately
            setError(undefined);

            // Add user message to UI immediately
            const userChatMessage: ChatMessage = {
                id: `temp-${Date.now()}`,
                role: "user",
                content: userMessage,
                createdAt: new Date().toISOString(),
            };
            setMessages((prev) => [...prev, userChatMessage]);
            setIsLoading(true);

            try {
                // Send message to backend
                const response = await sendMessage({
                    message: userMessage,
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
                const errorObj = err instanceof Error ? err : new Error("Failed to send message");
                setError(errorObj);
                options.onError?.(errorObj);

                // Remove the user message on error
                setMessages((prev) => prev.slice(0, -1));
            } finally {
                setIsLoading(false);
            }
        },
        [input, isLoading, threadId, options]
    );

    const sendSuggestion = useCallback(
        async (suggestion: string) => {
            if (!suggestion.trim() || isLoading) return;

            setError(undefined);

            // Add user message to UI immediately
            const userChatMessage: ChatMessage = {
                id: `temp-${Date.now()}`,
                role: "user",
                content: suggestion,
                createdAt: new Date().toISOString(),
            };
            setMessages((prev) => [...prev, userChatMessage]);
            setIsLoading(true);

            try {
                // Send message to backend
                const response = await sendMessage({
                    message: suggestion,
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
                const errorObj = err instanceof Error ? err : new Error("Failed to send message");
                setError(errorObj);
                options.onError?.(errorObj);

                // Remove the user message on error
                setMessages((prev) => prev.slice(0, -1));
            } finally {
                setIsLoading(false);
            }
        },
        [isLoading, threadId, options]
    );

    const clearMessages = useCallback(() => {
        setMessages([]);
        setThreadId(null);
        setError(undefined);
    }, []);

    const loadConversation = useCallback(
        async (newThreadId: string) => {
            setIsLoading(true);
            setError(undefined);

            try {
                // Get conversation history
                const response = await getConversation({
                    threadId: newThreadId,
                    page: 1,
                    pageSize: 50,
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
                const errorObj =
                    err instanceof Error ? err : new Error("Failed to load conversation");
                setError(errorObj);
                options.onError?.(errorObj);
            } finally {
                setIsLoading(false);
            }
        },
        [options]
    );

    const setThreadIdManually = useCallback((newThreadId: string | null) => {
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



