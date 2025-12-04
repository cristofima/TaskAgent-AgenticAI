"use client";

import { type FormEvent, forwardRef, useCallback, type KeyboardEvent } from "react";

interface ChatInputProps {
  input: string;
  isLoading: boolean;
  handleInputChange: (e: React.ChangeEvent<HTMLTextAreaElement>) => void;
  handleSubmit: (e: FormEvent<HTMLFormElement>) => void;
  /** Placeholder text to show when input is empty */
  placeholder?: string;
}

/**
 * Chat input form with send button
 * Supports keyboard shortcuts and multiline input
 */
export const ChatInput = forwardRef<HTMLTextAreaElement, ChatInputProps>(
  function ChatInput(
    {
      input,
      isLoading,
      handleInputChange,
      handleSubmit,
      placeholder = "Message Task Agent... (⌘K to focus)",
    },
    ref
  ) {
    // Handle Enter key to submit (Shift+Enter for new line)
    const handleKeyDown = useCallback(
      (e: KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === "Enter" && !e.shiftKey && !isLoading && input.trim()) {
          e.preventDefault();
          // Create a synthetic form event
          const form = e.currentTarget.form;
          if (form) {
            form.requestSubmit();
          }
        }
      },
      [isLoading, input]
    );

    return (
      <form onSubmit={handleSubmit} className="w-full">
        <div className="flex items-end gap-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-xl shadow-sm hover:shadow-md transition-shadow focus-within:border-blue-500 focus-within:ring-2 focus-within:ring-blue-200 dark:focus-within:ring-blue-800">
          <textarea
            ref={ref}
            value={input}
            onChange={handleInputChange}
            onKeyDown={handleKeyDown}
            placeholder={placeholder}
            className="flex-1 bg-transparent px-3 sm:px-4 py-3 text-sm sm:text-base text-gray-900 dark:text-gray-100 placeholder:text-gray-400 dark:placeholder:text-gray-500 outline-none min-w-0 resize-none max-h-32 overflow-y-auto"
            disabled={isLoading}
            rows={1}
            style={{
              height: "auto",
              minHeight: "44px",
            }}
          />
          <button
            type="submit"
            disabled={isLoading || !input.trim()}
            className="mr-2 mb-2 p-2 rounded-lg bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white transition-colors cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed disabled:hover:bg-blue-600 touch-manipulation"
            aria-label="Send message"
          >
            {isLoading ? (
              <svg
                className="w-5 h-5 animate-spin"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                />
              </svg>
            ) : (
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
                  d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"
                />
              </svg>
            )}
          </button>
        </div>
        <p className="text-xs text-gray-400 dark:text-gray-500 mt-1 text-center">
          Press Enter to send, Shift+Enter for new line
        </p>
      </form>
    );
  }
);
