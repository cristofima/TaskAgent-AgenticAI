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
}

/**
 * Individual chat message bubble component with MVC-inspired styling
 * Memoized to prevent unnecessary re-renders when parent updates
 */
export const ChatMessage = memo(function ChatMessage({
  message,
  onSuggestionClick,
  isLoading = false,
}: ChatMessageProps) {
  const isUser = message.role === "user";
  const [isHovered, setIsHovered] = useState(false);

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
        className={`rounded-2xl px-3 sm:px-5 py-3 sm:py-3.5 max-w-[90%] sm:max-w-[85%] shadow-md hover:shadow-lg transition-shadow ${
          isUser
            ? "bg-gradient-to-br from-blue-600 to-blue-700 text-white ml-auto text-right"
            : "bg-white border-2 border-gray-200 mr-auto"
        }`}
      >
        {!isUser && (
          <div className="font-bold text-gray-900 mb-2 flex items-center gap-2">
            <span className="text-base sm:text-lg">🤖</span>
            <span className="text-sm sm:text-base">Task Assistant</span>
          </div>
        )}
        <div
          className={`markdown-content leading-relaxed text-sm sm:text-base ${
            isUser ? "text-white" : "text-gray-800"
          }`}
        >
          <ReactMarkdown
            remarkPlugins={[remarkGfm]}
            rehypePlugins={[rehypeRaw]}
          >
            {textContent}
          </ReactMarkdown>
        </div>

        {/* Show suggestions for assistant messages */}
        {!isUser && suggestions.length > 0 && onSuggestionClick && (
          <SuggestionsBar
            suggestions={suggestions}
            onSuggestionClick={onSuggestionClick}
            disabled={isLoading}
          />
        )}

        {/* Show action buttons for assistant messages on hover */}
        {!isUser && (
          <MessageActions content={textContent} isVisible={isHovered} />
        )}
      </div>
    </div>
  );
});
