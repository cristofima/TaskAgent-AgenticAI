"use client";

import { ChatMessage } from "./ChatMessage";
import { LoadingIndicator } from "@/components/shared/LoadingIndicator";
import type { ChatMessage as ChatMessageType } from "@/types/chat";

interface ChatMessagesListProps {
  messages: ChatMessageType[];
  isLoading: boolean;
  onSuggestionClick?: (suggestion: string) => void;
}

/**
 * Chat messages list container with empty state and loading indicator
 */
export function ChatMessagesList({
  messages,
  isLoading,
  onSuggestionClick,
}: ChatMessagesListProps) {
  return (
    <>
      {messages.length === 0 ? (
        <div className="flex flex-col items-center justify-center text-center px-4">
          <div className="bg-white border border-gray-200 rounded-2xl p-6 sm:p-8 max-w-2xl shadow-sm">
            <div className="font-bold text-gray-900 mb-3 text-xl sm:text-2xl flex items-center justify-center gap-2">
              <span className="text-2xl sm:text-3xl">🤖</span>
              <span>Task Assistant</span>
            </div>
            <p className="mb-4 text-gray-600 text-sm sm:text-base leading-relaxed">
              Hi! I&apos;m your task management assistant. I can help you
              create, organize, and track your tasks efficiently.
            </p>
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 sm:p-4">
              <p className="text-xs sm:text-sm text-gray-700 font-medium mb-2">
                Try asking me:
              </p>
              <p className="text-xs sm:text-sm text-blue-700 italic">
                &quot;Create a task to review project proposal&quot;
                <br />
                &quot;Show me all my high priority tasks&quot;
              </p>
            </div>
          </div>
        </div>
      ) : (
        <div className="flex flex-col gap-3 sm:gap-4 p-3 sm:p-4 py-4 sm:py-6">
          {messages.map((message) => (
            <ChatMessage
              key={message.id}
              message={message}
              onSuggestionClick={onSuggestionClick}
              isLoading={isLoading}
            />
          ))}
          {isLoading && <LoadingIndicator />}
        </div>
      )}
    </>
  );
}
