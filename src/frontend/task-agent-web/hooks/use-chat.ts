"use client";

/**
 * Custom hook for chat functionality
 * Manages chat state and API communication with .NET backend
 * Supports progressive streaming rendering (ChatGPT-like UX)
 */

import { useState, useCallback, useRef, FormEvent } from "react";
import { sendMessageWithStreaming, getConversation, ApiError, type StreamingCallbacks } from "@/lib/api/chat-service";
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
    isStreaming: boolean; // New: indicates text is being streamed
    statusMessage: string | null; // Status message from backend (processing stages)
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
 * Hook for managing chat state and interactions with progressive streaming
 * Renders assistant responses progressively as text arrives (like ChatGPT)
 */
export function useChat(options: UseChatOptions = {}): UseChatReturn {
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [isStreaming, setIsStreaming] = useState(false); // New: tracks active streaming
    const [statusMessage, setStatusMessage] = useState<string | null>(null); // Backend status updates
    const [error, setError] = useState<Error | undefined>();
    const [currentThreadId, setCurrentThreadId] = useState<string | null>(null); // Active conversation ThreadDbKey
    const [serializedState, setSerializedState] = useState<string | null>(null); // ThreadDbKey from backend
    
    // Ref to track the streaming assistant message ID for updates
    const streamingMessageIdRef = useRef<string | null>(null);

    const handleInputChange = useCallback(
        (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>): void => {
            setInput(e.target.value);
        },
        []
    );

    /**
     * Internal helper to send a message and handle the streaming response
     * Uses callbacks for progressive UI rendering
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

            // Create placeholder for streaming assistant message
            const streamingMsgId = `streaming-${Date.now()}`;
            streamingMessageIdRef.current = streamingMsgId;
            
            const streamingPlaceholder: ChatMessage = {
                id: streamingMsgId,
                role: "assistant",
                content: "", // Will be updated progressively
                createdAt: new Date().toISOString(),
            };
            setMessages((prev) => [...prev, streamingPlaceholder]);
            setIsStreaming(true);

            // Define streaming callbacks for progressive rendering
            const streamingCallbacks: StreamingCallbacks = {
                onStart: (messageId, createdAt) => {
                    // Clear status message when actual text starts streaming
                    setStatusMessage(null);
                    // Update the streaming message with actual ID and timestamp
                    setMessages((prev) => 
                        prev.map((msg) => 
                            msg.id === streamingMsgId 
                                ? { ...msg, id: messageId, createdAt } 
                                : msg
                        )
                    );
                    streamingMessageIdRef.current = messageId;
                },
                
                onTextChunk: (_delta, fullText) => {
                    // Clear status message when text arrives (in case onStart wasn't called)
                    setStatusMessage(null);
                    // Progressive UI update - this is the magic!
                    const currentMsgId = streamingMessageIdRef.current;
                    setMessages((prev) => 
                        prev.map((msg) => 
                            msg.id === currentMsgId 
                                ? { ...msg, content: fullText } 
                                : msg
                        )
                    );
                },

                onStatusUpdate: (status) => {
                    // Update status message from backend (processing stages)
                    setStatusMessage(status);
                },
                
                onComplete: (newSerializedState) => {
                    setIsStreaming(false);
                    setStatusMessage(null); // Clear status on completion
                    
                    // Update serializedState for next request
                    if (newSerializedState) {
                        const isNewConversation = !serializedState;
                        setSerializedState(newSerializedState);
                        setCurrentThreadId(newSerializedState);
                        
                        if (isNewConversation) {
                            options.onThreadCreated?.(newSerializedState);
                        }
                    }
                },
                
                onError: () => {
                    setIsStreaming(false);
                    setStatusMessage(null); // Clear status on error
                    // Error will be handled in catch block
                }
            };

            try {
                // Send message with streaming callbacks
                const response = await sendMessageWithStreaming(
                    {
                        message,
                        serializedState: serializedState || undefined,
                    },
                    streamingCallbacks
                );

                // Final update with complete response data (metadata, suggestions)
                const currentMsgId = streamingMessageIdRef.current;
                setMessages((prev) => 
                    prev.map((msg) => 
                        msg.id === currentMsgId 
                            ? { 
                                ...msg, 
                                content: response.message || msg.content,
                                metadata: {
                                    ...response.metadata,
                                    suggestions: response.suggestions || [],
                                },
                              } 
                            : msg
                    )
                );

            } catch (err) {
                setIsStreaming(false);
                
                // Check if it's a Content Safety or Prompt Injection error (400 status)
                if (err instanceof ApiError && err.statusCode === 400 && err.response) {
                    const errorResponse = err.response;
                    const isContentSafetyError = errorResponse.error === "ContentPolicyViolation" ||
                        errorResponse.error === "PromptInjectionDetected";

                    if (isContentSafetyError) {
                        // Replace streaming placeholder with error message
                        const currentMsgId = streamingMessageIdRef.current;
                        setMessages((prev) => 
                            prev.map((msg) => 
                                msg.id === currentMsgId 
                                    ? {
                                        ...msg,
                                        id: errorResponse.messageId || `error-${Date.now()}`,
                                        content: errorResponse.message || "Your message was blocked due to safety concerns.",
                                        createdAt: errorResponse.createdAt || new Date().toISOString(),
                                        metadata: {
                                            violations: errorResponse.violations,
                                            categoryScores: errorResponse.categoryScores,
                                        },
                                      }
                                    : msg
                            )
                        );
                        return; // Exit early, don't show toast
                    }
                }

                // For other errors, show toast and remove both messages
                const validatedError = err instanceof Error ? err : new Error("Failed to send message");
                setError(validatedError);
                options.onError?.(validatedError);

                // Remove user message and streaming placeholder on error
                setMessages((prev) => prev.slice(0, -2));
            } finally {
                setIsLoading(false);
                streamingMessageIdRef.current = null;
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

                // Update current thread ID and serializedState from backend response
                // CRITICAL: Use serializedState from response, NOT the threadId directly
                // The serializedState is a full AgentThread JSON required by the streaming service
                setCurrentThreadId(threadId);
                setSerializedState(response.serializedState ?? null);

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
        isStreaming, // New: indicates progressive text rendering in progress
        statusMessage, // Backend status updates (processing stages)
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

