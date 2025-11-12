"use client";

import { type FormEvent } from "react";

interface ChatInputProps {
  input: string;
  isLoading: boolean;
  handleInputChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  handleSubmit: (e: FormEvent<HTMLFormElement>) => void;
}

/**
 * Chat input form with send button
 */
export function ChatInput({
  input,
  isLoading,
  handleInputChange,
  handleSubmit,
}: ChatInputProps) {
  return (
    <div className="border-t-2 border-gray-200 bg-gradient-to-r from-gray-800 via-gray-900 to-gray-800 p-5 shadow-inner">
      <form onSubmit={handleSubmit} className="flex items-center gap-3">
        <input
          type="text"
          value={input}
          onChange={handleInputChange}
          placeholder="What would you like to do with your tasks?"
          className="flex-1 rounded-full bg-white/95 border-2 border-gray-300 px-6 py-3.5 text-base text-gray-900 placeholder:text-gray-500 focus:border-blue-500 focus:ring-4 focus:ring-blue-200/50 transition-all outline-none shadow-sm hover:shadow-md"
          disabled={isLoading}
        />
        <button
          type="submit"
          disabled={isLoading || !input.trim()}
          className="bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white font-semibold px-8 py-3.5 rounded-full transition-all shadow-lg hover:shadow-xl flex-shrink-0 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-blue-600 transform hover:scale-105 active:scale-100"
        >
          {isLoading ? (
            <span className="flex items-center gap-2">
              <span className="inline-block h-4 w-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              Sending...
            </span>
          ) : (
            "Send"
          )}
        </button>
      </form>
    </div>
  );
}
