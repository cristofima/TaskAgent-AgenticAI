"use client";

import { useEffect, useState } from "react";

interface LoadingIndicatorProps {
  contextMessage?: string;
}

/**
 * Enhanced loading indicator with contextual messages
 * Cycles through messages to provide better UX during loading
 */
export function LoadingIndicator({ contextMessage }: LoadingIndicatorProps) {
  const [messageIndex, setMessageIndex] = useState(0);

  const defaultMessages = [
    "🤔 Analyzing your request...",
    "🔍 Processing task information...",
    "⚡ Generating response...",
    "✨ Almost ready...",
  ];

  const messages = contextMessage ? [contextMessage] : defaultMessages;

  useEffect(() => {
    if (messages.length <= 1) return;

    const interval = setInterval(() => {
      setMessageIndex((prev) => (prev + 1) % messages.length);
    }, 2000);

    return () => clearInterval(interval);
  }, [messages.length]);

  return (
    <div className="flex justify-start animate-fadeIn">
      <div className="rounded-2xl px-5 py-3.5 max-w-[85%] shadow-md bg-white border-2 border-gray-200">
        <div className="font-bold text-gray-900 mb-2 flex items-center gap-2">
          <span className="text-lg">🤖</span>
          <span>Task Assistant</span>
        </div>
        <div className="flex items-center gap-3">
          {/* Animated dots */}
          <div className="flex gap-1">
            <div
              className="w-2.5 h-2.5 bg-blue-500 rounded-full animate-bounce"
              style={{ animationDelay: "0ms" }}
            />
            <div
              className="w-2.5 h-2.5 bg-blue-500 rounded-full animate-bounce"
              style={{ animationDelay: "150ms" }}
            />
            <div
              className="w-2.5 h-2.5 bg-blue-500 rounded-full animate-bounce"
              style={{ animationDelay: "300ms" }}
            />
          </div>
          {/* Contextual message */}
          <span className="text-sm text-gray-600 animate-pulse">
            {messages[messageIndex]}
          </span>
        </div>
      </div>
    </div>
  );
}
