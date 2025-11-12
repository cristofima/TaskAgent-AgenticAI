"use client";

/**
 * Custom hook for chat functionality using Vercel AI SDK
 * Configured to work with external .NET backend API with streaming support
 */

import { useChat as useVercelChat } from "@ai-sdk/react";
import { DefaultChatTransport } from "ai";
import type { UIMessage } from "ai";
import { useState, useCallback, FormEvent } from "react";

export interface UseChatOptions {
    onError?: (error: Error) => void;
}

export interface UseChatReturn {
    messages: UIMessage[];
    input: string;
    isLoading: boolean;
    error: Error | undefined;
    handleInputChange: (
        e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
    ) => void;
    handleSubmit: (e: FormEvent<HTMLFormElement>) => Promise<void>;
    clearMessages: () => void;
    setInput: (input: string) => void;
}

/**
 * Hook for managing chat state and interactions with streaming support
 * Wraps Vercel AI SDK's useChat for compatibility with .NET backend
 */
export function useChat(options: UseChatOptions = {}): UseChatReturn {
    const [input, setInput] = useState("");

    const chat = useVercelChat({
        transport: new DefaultChatTransport({
            api: `${process.env.NEXT_PUBLIC_API_URL}/api/chat`,
        }),
        onError: options.onError,
    });

    const handleInputChange = useCallback(
        (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
            setInput(e.target.value);
        },
        []
    );

    const handleSubmit = useCallback(
        async (e: FormEvent<HTMLFormElement>) => {
            e.preventDefault();
            if (!input.trim()) return;

            const message = input.trim();
            setInput(""); // Clear input immediately

            await chat.sendMessage({ text: message });
        },
        [input, chat]
    );

    return {
        messages: chat.messages,
        input,
        isLoading: chat.status === "streaming" || chat.status === "submitted",
        error: chat.error,
        handleInputChange,
        handleSubmit,
        clearMessages: () => chat.setMessages([]),
        setInput,
    };
}




