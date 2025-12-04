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
  handleInputChange: (
    e: React.ChangeEvent<HTMLTextAreaElement | HTMLInputElement>
  ) => void;
  handleSubmit: (e: React.FormEvent<HTMLFormElement>) => void;
  sendSuggestion: (suggestion: string) => void;
  inputRef?: React.RefObject<HTMLTextAreaElement | null>;
}

export function EmptyChatState({
  messages,
  input,
  isLoading,
  handleInputChange,
  handleSubmit,
  sendSuggestion,
  inputRef,
}: EmptyChatStateProps) {
  return (
    <>
      {/* Desktop: Content positioned higher (not perfectly centered) - Mobile: Centered welcome only */}
      <div className="flex-1 flex flex-col md:justify-start md:pt-24 justify-center items-center px-4 py-8 overflow-y-auto">
        <div className="w-full max-w-3xl">
          <ChatMessagesList
            messages={messages}
            isLoading={isLoading}
            onSuggestionClick={sendSuggestion}
          />
          {/* Input centered in desktop */}
          <div className="mt-8 hidden md:block">
            <ChatInput
              ref={inputRef}
              input={input}
              isLoading={isLoading}
              handleInputChange={handleInputChange}
              handleSubmit={handleSubmit}
            />
          </div>
        </div>
      </div>

      {/* Fixed input at bottom - mobile only */}
      <div className="md:hidden flex-shrink-0 bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700 px-4 py-4">
        <div className="max-w-4xl mx-auto">
          <ChatInput
            ref={inputRef}
            input={input}
            isLoading={isLoading}
            handleInputChange={handleInputChange}
            handleSubmit={handleSubmit}
          />
        </div>
      </div>
    </>
  );
}
