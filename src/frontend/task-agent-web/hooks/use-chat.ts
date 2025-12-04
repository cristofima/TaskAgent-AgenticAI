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
    const [currentThreadId, setCurrentThreadId] = useState<string | null>(null); // Active conversation ThreadDbKey
    const [serializedState, setSerializedState] = useState<string | null>(null); // ThreadDbKey from backend

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
                // Send message with serializedState for conversation continuity
                const response = await sendMessage({
                    message,
                    serializedState: serializedState || undefined,
                });

                // Update serializedState for next request (thread continuity)
                if (response.serializedState) {
                    const isNewConversation = !serializedState; // First message creates new chat
                    setSerializedState(response.serializedState);
                    setCurrentThreadId(response.serializedState); // ThreadDbKey is the thread ID
                    
                    // Notify parent component to reload chats list
                    if (isNewConversation) {
                        options.onThreadCreated?.(response.serializedState);
                    }
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
                        // Show as assistant message in the chat
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
        [isLoading, serializedState, options]
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
        setCurrentThreadId(null);
        setSerializedState(null);
        setError(undefined);
    }, []);

    /**
     * Loads a conversation's message history and updates the chat state
     *
     * @param threadId - The ThreadDbKey (GUID) to load from database
     * @throws {Error} When conversation cannot be loaded
     */
    const loadConversation = useCallback(
        async (threadId: string): Promise<void> => {
            setIsLoading(true);
            setError(undefined);

            try {
                // Get conversation history
                const response = await getConversation({
                    threadId: threadId,
                    page: 1,
                    pageSize: PAGINATION.CONVERSATION_PAGE_SIZE,
                });

                // Update current thread ID and serializedState
                setCurrentThreadId(threadId);
                setSerializedState(threadId); // ThreadDbKey is used as serializedState

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
                    err instanceof Error ? err : new Error("Failed to load chat");
                setError(validatedError);
                options.onError?.(validatedError);
            } finally {
                setIsLoading(false);
            }
        },
        [options]
    );

    return {
        messages,
        input,
        isLoading,
        error,
        threadId: currentThreadId,
        handleInputChange,
        handleSubmit,
        clearMessages,
        setInput,
        sendSuggestion,
        loadConversation,
        setThreadId: setCurrentThreadId,
    };
}



