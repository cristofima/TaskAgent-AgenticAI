"use client";

/**
 * ConversationSidebar Component
 * Sidebar for managing conversations (list, create, delete)
 * Responsive: Drawer on mobile, persistent sidebar on desktop
 */

import { useEffect } from "react";
import { useConversations } from "@/hooks/use-conversations";
import { ConversationList } from "./ConversationList";

interface ConversationSidebarProps {
  isOpen: boolean;
  isCollapsed: boolean;
  onClose: () => void;
  onToggleCollapse: () => void;
  onConversationSelect: (threadId: string) => void;
  onNewConversation: () => void;
  currentThreadId: string | null;
  onLoadConversationsReady?: (loadFn: () => Promise<void>) => void;
}

export function ConversationSidebar({
  isOpen,
  isCollapsed,
  onClose,
  onToggleCollapse,
  onConversationSelect,
  onNewConversation,
  currentThreadId,
  onLoadConversationsReady,
}: ConversationSidebarProps) {
  const {
    conversations,
    isLoading,
    error,
    loadConversations,
    deleteConversation,
  } = useConversations({
    autoLoad: true,
    pageSize: 50,
  });

  // Expose loadConversations to parent component
  useEffect(() => {
    if (onLoadConversationsReady) {
      onLoadConversationsReady(loadConversations);
    }
  }, [loadConversations, onLoadConversationsReady]);

  const handleConversationClick = async (threadId: string) => {
    onConversationSelect(threadId);
    // Close sidebar on mobile after selection
    if (window.innerWidth < 768) {
      onClose();
    }
  };

  const handleNewConversation = () => {
    onNewConversation();
    // Close sidebar on mobile
    if (window.innerWidth < 768) {
      onClose();
    }
  };

  const handleDelete = async (threadId: string) => {
    try {
      await deleteConversation(threadId);
      await loadConversations(); // Refresh list
    } catch (error) {
      console.error("Failed to delete conversation:", error);
    }
  };

  return (
    <>
      {/* Backdrop (mobile only) */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-40 md:hidden"
          onClick={onClose}
        />
      )}

      {/* Sidebar */}
      <aside
        className={`
          fixed md:static inset-y-0 left-0 z-50
          ${isCollapsed ? "md:w-16" : "md:w-64"} w-64
          bg-white border-r border-gray-200
          flex flex-col
          transition-all duration-300 ease-in-out
          ${isOpen ? "translate-x-0" : "-translate-x-full md:translate-x-0"}
        `}
      >
        {/* Header */}
        <div
          className={`flex-shrink-0 p-4 ${
            isCollapsed ? "md:px-2" : ""
          } border-b border-gray-200`}
        >
          <div className="flex items-center justify-between mb-3">
            {!isCollapsed && (
              <h2 className="text-lg font-semibold text-gray-900">
                Conversations
              </h2>
            )}
            <div className="flex items-center gap-1">
              {/* Collapse button (desktop only) */}
              <button
                onClick={onToggleCollapse}
                className="hidden md:block p-1.5 rounded hover:bg-gray-100 text-gray-500 cursor-pointer"
                title={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
              >
                <svg
                  className={`w-5 h-5 transition-transform ${
                    isCollapsed ? "rotate-180" : ""
                  }`}
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M15 19l-7-7 7-7"
                  />
                </svg>
              </button>
              {/* Close button (mobile only) */}
              <button
                onClick={onClose}
                className="md:hidden p-1 rounded hover:bg-gray-100 text-gray-500 cursor-pointer"
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
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>
          </div>

          {/* New conversation button */}
          {!isCollapsed && (
            <button
              onClick={handleNewConversation}
              className="
                w-full flex items-center justify-center gap-2
                px-4 py-2.5 rounded-lg
                bg-blue-600 hover:bg-blue-700
                text-white text-sm font-medium
                transition-colors
                cursor-pointer
              "
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
                  d="M12 4v16m8-8H4"
                />
              </svg>
              New conversation
            </button>
          )}
          {isCollapsed && (
            <button
              onClick={handleNewConversation}
              className="
                w-full flex items-center justify-center
                p-2.5 rounded-lg
                bg-blue-600 hover:bg-blue-700
                text-white
                transition-colors
                cursor-pointer
              "
              title="New conversation"
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
                  d="M12 4v16m8-8H4"
                />
              </svg>
            </button>
          )}
        </div>

        {/* Conversation list */}
        {!isCollapsed && (
          <div className="flex-1 overflow-y-auto py-2">
            {error && (
              <div className="mx-2 mb-2 p-3 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-800">{error.message}</p>
              </div>
            )}
            <ConversationList
              conversations={conversations}
              currentThreadId={currentThreadId}
              isLoading={isLoading}
              onConversationClick={handleConversationClick}
              onConversationDelete={handleDelete}
            />
          </div>
        )}

        {/* Footer - refresh button */}
        {!isCollapsed && (
          <div className="flex-shrink-0 px-4 py-4 bg-white border-t border-gray-200">
            <button
              onClick={loadConversations}
              disabled={isLoading}
              className="
                w-full flex items-center justify-center gap-2
                px-4 py-2 rounded-lg
                text-sm font-medium
                bg-gray-100 hover:bg-gray-200
                text-gray-700
                transition-colors
                cursor-pointer
                disabled:opacity-50 disabled:cursor-not-allowed
              "
            >
              <svg
                className={`w-4 h-4 ${isLoading ? "animate-spin" : ""}`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                />
              </svg>
              {isLoading ? "Refreshing..." : "Refresh"}
            </button>
          </div>
        )}
      </aside>
    </>
  );
}
