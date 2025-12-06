"use client";

import { useState } from "react";

interface SuggestionsBarProps {
  suggestions: string[];
  onSuggestionClick: (suggestion: string) => void;
  disabled?: boolean;
}

/**
 * Suggestions bar component - displays clickable suggestion buttons
 * Based on agent's contextual recommendations
 */
export function SuggestionsBar({
  suggestions,
  onSuggestionClick,
  disabled = false,
}: SuggestionsBarProps) {
  const [clickedIndex, setClickedIndex] = useState<number | null>(null);

  if (!suggestions || suggestions.length === 0) {
    return null;
  }

  const handleClick = (suggestion: string, index: number) => {
    if (disabled) return;

    setClickedIndex(index);
    onSuggestionClick(suggestion);

    // Reset clicked state after animation
    setTimeout(() => setClickedIndex(null), 300);
  };

  return (
    <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700">
      <div className="flex items-start gap-2 mb-2">
        <span className="text-xs font-semibold text-gray-600 dark:text-gray-400 flex items-center gap-1">
          <svg
            className="w-3.5 h-3.5 sm:w-4 sm:h-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"
            />
          </svg>
          <span className="text-xs sm:text-sm">Suggestions:</span>
        </span>
      </div>
      <div className="flex flex-wrap gap-2">
        {suggestions.map((suggestion, index) => (
          <button
            key={index}
            onClick={() => handleClick(suggestion, index)}
            disabled={disabled}
            className={`
              group relative px-2.5 sm:px-3 py-1.5
              text-xs sm:text-sm font-medium
              bg-gradient-to-r from-blue-50 to-blue-100
              dark:from-blue-900/30 dark:to-blue-800/30
              hover:from-blue-100 hover:to-blue-200
              dark:hover:from-blue-800/40 dark:hover:to-blue-700/40
              active:from-blue-200 active:to-blue-300
              dark:active:from-blue-700/50 dark:active:to-blue-600/50
              text-blue-700 hover:text-blue-800
              dark:text-blue-400 dark:hover:text-blue-300
              border border-blue-200 hover:border-blue-300
              dark:border-blue-800 dark:hover:border-blue-700
              rounded-full
              transition-all duration-200
              cursor-pointer
              shadow-sm hover:shadow-md
              disabled:opacity-50 disabled:cursor-not-allowed
              touch-manipulation
              ${
                clickedIndex === index
                  ? "scale-95"
                  : "hover:scale-105 active:scale-95"
              }
            `}
          >
            <span className="flex items-center gap-1 sm:gap-1.5">
              <svg
                className="w-3 h-3 sm:w-3.5 sm:h-3.5 transition-transform group-hover:rotate-12"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M13 9l3 3m0 0l-3 3m3-3H8m13 0a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <span className="break-words">{suggestion}</span>
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
