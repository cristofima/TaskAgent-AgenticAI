"use client";

import { memo, useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeRaw from "rehype-raw";
import type { ChatMessage as ChatMessageType } from "@/types/chat";
import { SuggestionsBar } from "./SuggestionsBar";
import { MessageActions } from "./MessageActions";

interface ChatMessageProps {
  message: ChatMessageType;
  onSuggestionClick?: (suggestion: string) => void;
  isLoading?: boolean;
  isStreaming?: boolean; // New: shows blinking cursor while streaming
  statusMessage?: string | null; // Status message from backend (shown when streaming with no content)
}

/**
 * Individual chat message bubble component with MVC-inspired styling
 * Memoized to prevent unnecessary re-renders when parent updates
 */
/**
 * Detects if a message content is an internal function call JSON
 * These are internal agent messages that should not be shown to users
 */
function isFunctionCallMessage(content: string | null): boolean {
  if (!content) return false;
  const trimmed = content.trim();
  // Check if it's JSON starting with {"$type":"functionCall" or {"$type":"functionResult"
  if (trimmed.startsWith("{") && trimmed.includes('"$type"')) {
    try {
      const parsed = JSON.parse(trimmed);
      return (
        parsed.$type === "functionCall" || parsed.$type === "functionResult"
      );
    } catch {
      // Not valid JSON, not a function call
      return false;
    }
  }
  return false;
}

export const ChatMessage = memo(function ChatMessage({
  message,
  onSuggestionClick,
  isLoading = false,
  isStreaming = false, // Streaming cursor state
  statusMessage, // Status from backend
}: ChatMessageProps) {
  const isUser = message.role === "user";
  const [isHovered, setIsHovered] = useState(false);

  // Filter out internal function call messages - don't render them
  if (isFunctionCallMessage(message.content)) {
    return null;
  }

  // Process content: convert \n to actual newlines for proper Markdown rendering
  const textContent = (message.content || "").replace(/\\n/g, "\n");

  // Extract suggestions from metadata
  const suggestions = message.metadata?.suggestions || [];

  return (
    <div
      className={`flex animate-fadeIn ${
        isUser ? "justify-end" : "justify-start"
      }`}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div
        className={`relative rounded-2xl px-3 sm:px-5 py-3 sm:py-3.5 max-w-[90%] sm:max-w-[85%] shadow-md hover:shadow-lg transition-shadow ${
          isUser
            ? "bg-gradient-to-br from-blue-600 to-blue-700 text-white ml-auto text-right"
            : "bg-white dark:bg-gray-800 border-2 border-gray-200 dark:border-gray-700 mr-auto"
        }`}
      >
        {/* Copy button - top right corner */}
        {!isUser && (
          <MessageActions content={textContent} isVisible={isHovered} />
        )}

        {!isUser && (
          <div className="font-bold text-gray-900 dark:text-white mb-2 flex items-center gap-2">
            <span className="text-base sm:text-lg">🤖</span>
            <span className="text-sm sm:text-base">Task Assistant</span>
          </div>
        )}
        <div
          className={`markdown-content leading-relaxed text-sm sm:text-base ${
            isUser ? "text-white" : "text-gray-800 dark:text-gray-200"
          }`}
        >
          <ReactMarkdown
            remarkPlugins={[remarkGfm]}
            rehypePlugins={[rehypeRaw]}
          >
            {textContent}
          </ReactMarkdown>
          {/* Streaming state: show status message or blinking cursor */}
          {isStreaming && (
            <span className="inline-flex items-center gap-2">
              {statusMessage && !textContent.trim() && (
                <span className="text-sm text-gray-500 dark:text-gray-400 animate-pulse">
                  {statusMessage}
                </span>
              )}
              <span className="inline-block w-2 h-4 bg-gray-800 dark:bg-gray-200 animate-pulse" />
            </span>
          )}
        </div>

        {/* Show suggestions for assistant messages */}
        {!isUser && suggestions.length > 0 && onSuggestionClick && (
          <SuggestionsBar
            suggestions={suggestions}
            onSuggestionClick={onSuggestionClick}
            disabled={isLoading}
          />
        )}
      </div>
    </div>
  );
});
