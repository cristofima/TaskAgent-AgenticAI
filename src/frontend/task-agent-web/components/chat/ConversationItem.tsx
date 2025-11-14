"use client";

/**
 * ConversationItem Component
 * Displays a single conversation in the sidebar with metadata
 */

import { useState } from "react";
import type { ConversationThread } from "@/types/chat";
import { formatDistanceToNow } from "@/lib/utils/date-utils";
import { DeleteConfirmModal } from "./DeleteConfirmModal";

interface ConversationItemProps {
  conversation: ConversationThread;
  isActive: boolean;
  onClick: () => void;
  onDelete: () => void;
}

export function ConversationItem({
  conversation,
  isActive,
  onClick,
  onDelete,
}: ConversationItemProps) {
  const [showDeleteModal, setShowDeleteModal] = useState(false);

  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation(); // Prevent triggering onClick
    setShowDeleteModal(true);
  };

  const confirmDelete = () => {
    setShowDeleteModal(false);
    onDelete();
  };

  // Generate title from conversation data
  const title =
    conversation.title ||
    conversation.preview?.substring(0, 50) ||
    "New conversation";

  // Format date
  const timeAgo = formatDistanceToNow(new Date(conversation.updatedAt));

  return (
    <div
      onClick={onClick}
      className={`
        group relative px-3 py-2.5 rounded-lg cursor-pointer transition-all
        ${
          isActive
            ? "bg-blue-50 border border-blue-200"
            : "hover:bg-gray-100 border border-transparent"
        }
      `}
    >
      {/* Active indicator */}
      {isActive && (
        <div className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-8 bg-blue-500 rounded-r" />
      )}

      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          {/* Title */}
          <h3
            className={`
            text-sm font-medium truncate mb-1
            ${isActive ? "text-blue-900" : "text-gray-900"}
          `}
          >
            {title}
          </h3>

          {/* Metadata */}
          <div className="flex items-center gap-2 text-xs text-gray-500">
            <span>{conversation.messageCount} messages</span>
            <span>•</span>
            <span>{timeAgo}</span>
          </div>
        </div>

        {/* Delete button */}
        <button
          onClick={handleDelete}
          className="
            opacity-0 group-hover:opacity-100 transition-opacity
            p-1 rounded hover:bg-red-50 text-gray-400 hover:text-red-600
            cursor-pointer
          "
          title="Delete conversation"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
            />
          </svg>
        </button>
      </div>

      {/* Delete confirmation modal */}
      <DeleteConfirmModal
        isOpen={showDeleteModal}
        title="Delete conversation?"
        message="This action cannot be undone. All messages in this conversation will be permanently deleted."
        onConfirm={confirmDelete}
        onCancel={() => setShowDeleteModal(false)}
      />
    </div>
  );
}
