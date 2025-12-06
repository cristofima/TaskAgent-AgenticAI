"use client";

/**
 * MessageActions Component
 * Action buttons for chat messages (copy, etc.)
 * Only visible on hover for assistant messages
 */

import { useState, useCallback } from "react";

interface MessageActionsProps {
  content: string;
  isVisible: boolean;
}

/**
 * Copy message content to clipboard
 */
async function copyToClipboard(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch (error) {
    // Fallback for older browsers or when clipboard API is not available
    try {
      const textArea = document.createElement("textarea");
      textArea.value = text;
      textArea.style.position = "fixed";
      textArea.style.left = "-999999px";
      textArea.style.top = "-999999px";
      document.body.appendChild(textArea);
      textArea.focus();
      textArea.select();
      const success = document.execCommand("copy");
      document.body.removeChild(textArea);
      return success;
    } catch {
      console.error("Failed to copy to clipboard:", error);
      return false;
    }
  }
}

/**
 * Strip markdown formatting for plain text copy
 */
function stripMarkdown(text: string): string {
  return text
    // Remove code blocks
    .replace(/```[\s\S]*?```/g, (match) => {
      const code = match.replace(/```\w*\n?/g, "").replace(/```/g, "");
      return code.trim();
    })
    // Remove inline code
    .replace(/`([^`]+)`/g, "$1")
    // Remove bold
    .replace(/\*\*([^*]+)\*\*/g, "$1")
    // Remove italic
    .replace(/\*([^*]+)\*/g, "$1")
    .replace(/_([^_]+)_/g, "$1")
    // Remove headers
    .replace(/^#{1,6}\s+/gm, "")
    // Remove blockquotes
    .replace(/^>\s+/gm, "")
    // Remove horizontal rules
    .replace(/^[-*_]{3,}$/gm, "")
    // Clean up extra whitespace
    .replace(/\n{3,}/g, "\n\n")
    .trim();
}

export function MessageActions({ content, isVisible }: MessageActionsProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = useCallback(async () => {
    const plainText = stripMarkdown(content);
    const success = await copyToClipboard(plainText);

    if (success) {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  }, [content]);

  if (!isVisible) return null;

  return (
    <div className="absolute top-2 right-2 flex items-center gap-1">
      <button
        onClick={handleCopy}
        className="
          flex items-center gap-1.5 px-2 py-1 rounded-md
          text-xs font-medium
          text-gray-400 hover:text-gray-600
          dark:text-gray-500 dark:hover:text-gray-300
          bg-gray-100/80 hover:bg-gray-200/90
          dark:bg-gray-700/80 dark:hover:bg-gray-600/90
          backdrop-blur-sm
          transition-all duration-200
          cursor-pointer
        "
        title="Copy message"
        aria-label={copied ? "Copied!" : "Copy message"}
      >
        {copied ? (
          <>
            <svg
              className="w-3.5 h-3.5 text-green-500"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M5 13l4 4L19 7"
              />
            </svg>
            <span className="text-green-600">Copied!</span>
          </>
        ) : (
          <>
            <svg
              className="w-3.5 h-3.5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
              />
            </svg>
            <span>Copy</span>
          </>
        )}
      </button>
    </div>
  );
}
