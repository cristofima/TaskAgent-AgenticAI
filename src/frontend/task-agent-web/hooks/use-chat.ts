"use client";

/**
 * Custom hook for chat functionality
 * Manages chat state and API communication with .NET backend
 */

import { useState, useCallback, FormEvent } from "react";
import { sendMessage, getConversation, ApiError } from "@/lib/api/chat-service";
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
    const [isFirstValidMessage, setIsFirstValidMessage] = useState(false);

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
                const isNewThread = response.threadId && !threadId;
                if (isNewThread) {
                    setThreadId(response.threadId);
                    // Notify that a new thread was created
                    if (response.threadId) {
                        options.onThreadCreated?.(response.threadId);
                    }
                }

                // If this is the first valid message after a blocked one, reload sidebar
                // This updates the title from "New chat" to the actual message
                if (isFirstValidMessage && response.threadId) {
                    options.onThreadCreated?.(response.threadId);
                    setIsFirstValidMessage(false);
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
                // Check if it's a Content Safety or Prompt Injection error (400 status)
                if (err instanceof ApiError && err.statusCode === 400 && err.response) {
                    // Show content safety/prompt injection errors as assistant messages
                    const errorResponse = err.response;
                    const isContentSafetyError = errorResponse.error === "ContentPolicyViolation" ||
                        errorResponse.error === "PromptInjectionDetected";

                    if (isContentSafetyError) {
                        // ChatGPT behavior: Update threadId if new conversation (even for blocked messages)
                        if (errorResponse.threadId && !threadId) {
                            setThreadId(errorResponse.threadId);
                            if (errorResponse.threadId) {
                                options.onThreadCreated?.(errorResponse.threadId);
                            }
                            // Mark that next valid message should trigger sidebar reload
                            setIsFirstValidMessage(true);
                        }

                        // Show as assistant message in the chat (restore previous behavior)
                        const errorMessage: ChatMessage = {
                            id: errorResponse.messageId || `error-${Date.now()}`,
                            role: "assistant",
                            content: errorResponse.message || "Your message was blocked due to safety concerns.",
                            createdAt: errorResponse.createdAt || new Date().toISOString(),
                            metadata: {
                                violations: errorResponse.violations,
                                categoryScores: errorResponse.categoryScores,
                            },
                        };
                        setMessages((prev) => [...prev, errorMessage]);
                        // Don't set error state, so no toast is shown
                        return; // Exit early
                    }
                }

                // For other errors, show toast and remove user message
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



