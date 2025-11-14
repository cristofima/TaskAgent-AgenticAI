"use client";

/**
 * EmptyChatState Component
 * Displays the welcome state when no messages are present
 */

import { ChatMessagesList } from "./ChatMessagesList";
import { ChatInput } from "./ChatInput";
import type { ChatMessage } from "@/types/chat";

interface EmptyChatStateProps {
  messages: ChatMessage[];
  input: string;
  isLoading: boolean;
  onToggleSidebar: () => void;
  handleInputChange: (
    e:
      | React.ChangeEvent<HTMLTextAreaElement>
      | React.ChangeEvent<HTMLInputElement>
  ) => void;
  handleSubmit: (e: React.FormEvent<HTMLFormElement>) => void;
  sendSuggestion: (suggestion: string) => void;
}

export function EmptyChatState({
  messages,
  input,
  isLoading,
  onToggleSidebar,
  handleInputChange,
  handleSubmit,
  sendSuggestion,
}: EmptyChatStateProps) {
  return (
    <div className="flex-1 flex flex-col items-center justify-center px-4">
      <div className="w-full max-w-3xl">
        {/* Sidebar toggle in empty state (mobile) */}
        <div className="md:hidden flex justify-start mb-4">
          <button
            onClick={onToggleSidebar}
            className="flex items-center gap-2 px-4 py-2 rounded-lg bg-white border border-gray-200 hover:bg-gray-50 text-gray-700 text-sm font-medium cursor-pointer"
          >
            <svg
              className="w-5 h-5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 6h16M4 12h16M4 18h16"
              />
            </svg>
            Conversations
          </button>
        </div>

        <ChatMessagesList
          messages={messages}
          isLoading={isLoading}
          onSuggestionClick={sendSuggestion}
        />
        <div className="mt-8">
          <ChatInput
            input={input}
            isLoading={isLoading}
            handleInputChange={handleInputChange}
            handleSubmit={handleSubmit}
          />
        </div>
      </div>
    </div>
  );
}
