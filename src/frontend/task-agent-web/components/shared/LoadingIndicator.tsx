"use client";

import { useEffect, useState } from "react";

interface LoadingIndicatorProps {
  /** Server-provided status message (takes priority over default rotation) */
  serverStatus?: string | null;
  /** Optional custom context message (fallback if no server status) */
  contextMessage?: string;
}

/**
 * Enhanced loading indicator with server-driven status messages
 * Shows backend processing stages when available, otherwise rotates default messages
 */
export function LoadingIndicator({ serverStatus, contextMessage }: LoadingIndicatorProps) {
  const [messageIndex, setMessageIndex] = useState(0);

  const defaultMessages = [
    "🤔 Analyzing your request...",
    "🔍 Processing task information...",
    "⚡ Generating response...",
    "✨ Almost ready...",
  ];

  // Use server status if available, otherwise use context message or default rotation
  const displayMessage = serverStatus || contextMessage || defaultMessages[messageIndex];
  const shouldRotate = !serverStatus && !contextMessage && defaultMessages.length > 1;

  useEffect(() => {
    if (!shouldRotate) return;

    const interval = setInterval(() => {
      setMessageIndex((prev) => (prev + 1) % defaultMessages.length);
    }, 2000);

    return () => clearInterval(interval);
  }, [shouldRotate, defaultMessages.length]);

  return (
    <div className="flex justify-start animate-fadeIn">
      <div className="rounded-2xl px-5 py-3.5 max-w-[85%] shadow-md bg-white dark:bg-gray-800 border-2 border-gray-200 dark:border-gray-700">
        <div className="font-bold text-gray-900 dark:text-white mb-2 flex items-center gap-2">
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
          <span className="text-sm text-gray-600 dark:text-gray-400 animate-pulse">
            {displayMessage}
          </span>
        </div>
      </div>
    </div>
  );
}
