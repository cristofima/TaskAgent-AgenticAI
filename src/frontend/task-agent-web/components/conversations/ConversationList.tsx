"use client";

/**
 * ConversationList Component
 * Displays a list of chats with loading and empty states
 */

import type { ConversationThread } from "@/types/chat";
import { ConversationItem } from "./ConversationItem";

interface ConversationListProps {
  conversations: ConversationThread[];
  currentThreadId: string | null;
  isLoading: boolean;
  onConversationClick: (threadId: string) => void;
  onConversationDelete: (threadId: string) => void;
}

export function ConversationList({
  conversations,
  currentThreadId,
  isLoading,
  onConversationClick,
  onConversationDelete,
}: ConversationListProps) {
  // Loading state
  if (isLoading && conversations.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 px-4">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mb-3" />
        <p className="text-sm text-gray-500 dark:text-gray-400">Loading chats...</p>
      </div>
    );
  }

  // Empty state
  if (conversations.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 px-4 text-center">
        <div className="w-12 h-12 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center mb-3">
          <svg
            className="w-6 h-6 text-gray-400 dark:text-gray-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"
            />
          </svg>
        </div>
        <p className="text-sm text-gray-600 dark:text-gray-400 font-medium mb-1">
          No chats yet
        </p>
        <p className="text-xs text-gray-500 dark:text-gray-500">
          Start a new chat to get started
        </p>
      </div>
    );
  }

  // Conversation list
  return (
    <div className="space-y-1 px-2">
      {conversations.map((conversation) => (
        <ConversationItem
          key={conversation.id}
          conversation={conversation}
          isActive={currentThreadId === conversation.id}
          onClick={() => onConversationClick(conversation.id)}
          onDelete={() => onConversationDelete(conversation.id)}
        />
      ))}
    </div>
  );
}
