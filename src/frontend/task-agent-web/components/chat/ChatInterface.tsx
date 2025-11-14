"use client";

/**
 * ChatInterface Component
 * Main chat interface with conversation sidebar
 * Supports conversation management (list, load, delete, create new)
 */

import { useState, useRef } from "react";
import { useChat } from "@/hooks/use-chat";
import { ChatMessagesList } from "./ChatMessagesList";
import { ChatInput } from "./ChatInput";
import { ConversationSidebar } from "./ConversationSidebar";

/**
 * Main ChatInterface component with sidebar and chat area
 */
export function ChatInterface() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  // Ref to hold the loadConversations function from ConversationSidebar
  const loadConversationsRef = useRef<(() => Promise<void>) | null>(null);

  const {
    messages,
    input,
    isLoading,
    error,
    threadId,
    handleInputChange,
    handleSubmit,
    sendSuggestion,
    clearMessages,
    loadConversation,
    setThreadId,
  } = useChat({
    onError: (error) => {
      console.error("Chat error:", error);
    },
    onThreadCreated: async () => {
      // Refresh conversation list when new thread is created
      if (loadConversationsRef.current) {
        await loadConversationsRef.current();
      }
    },
  });

  const hasMessages = messages.length > 0;

  const handleConversationSelect = async (selectedThreadId: string) => {
    try {
      await loadConversation(selectedThreadId);
    } catch (error) {
      console.error("Failed to load conversation:", error);
    }
  };

  const handleNewConversation = () => {
    clearMessages();
    setThreadId(null);
  };

  const toggleSidebar = () => {
    setIsSidebarOpen(!isSidebarOpen);
  };

  return (
    <div className="h-screen flex bg-gray-50">
      {/* Conversation Sidebar */}
      <ConversationSidebar
        isOpen={isSidebarOpen}
        isCollapsed={isSidebarCollapsed}
        onClose={() => setIsSidebarOpen(false)}
        onToggleCollapse={() => setIsSidebarCollapsed(!isSidebarCollapsed)}
        onConversationSelect={handleConversationSelect}
        onNewConversation={handleNewConversation}
        currentThreadId={threadId}
        onLoadConversationsReady={(loadFn) => {
          loadConversationsRef.current = loadFn;
        }}
      />

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        {hasMessages && (
          <div className="flex-shrink-0 bg-gray-50 border-b border-gray-200 px-4 py-3 shadow-sm">
            <div className="flex items-center gap-3 max-w-4xl mx-auto">
              {/* Sidebar toggle button (mobile) */}
              <button
                onClick={toggleSidebar}
                className="md:hidden p-2 rounded hover:bg-gray-100 text-gray-600 cursor-pointer"
                aria-label="Toggle sidebar"
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
              </button>

              <span className="text-xl">📋</span>
              <span className="font-semibold text-gray-800">Task Agent</span>
            </div>
          </div>
        )}

        {/* Main content area */}
        {hasMessages ? (
          /* With messages: scrollable messages + fixed input */
          <>
            <div className="flex-1 overflow-y-auto">
              <div className="max-w-4xl mx-auto">
                <ChatMessagesList
                  messages={messages}
                  isLoading={isLoading}
                  onSuggestionClick={sendSuggestion}
                />
              </div>
            </div>

            {/* Fixed Input at bottom */}
            <div className="flex-shrink-0 bg-gray-50 px-4 py-4">
              <div className="max-w-4xl mx-auto">
                <ChatInput
                  input={input}
                  isLoading={isLoading}
                  handleInputChange={handleInputChange}
                  handleSubmit={handleSubmit}
                />
              </div>
            </div>
          </>
        ) : (
          /* Empty state: centered welcome + input */
          <div className="flex-1 flex flex-col items-center justify-center px-4">
            <div className="w-full max-w-3xl">
              {/* Sidebar toggle in empty state (mobile) */}
              <div className="md:hidden flex justify-start mb-4">
                <button
                  onClick={toggleSidebar}
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
        )}

        {/* Error Toast */}
        {error && (
          <div className="fixed top-4 right-4 max-w-md bg-red-50 border-2 border-red-200 border-l-4 border-l-red-500 rounded-lg shadow-lg p-4 animate-fadeIn z-50">
            <div className="flex items-start gap-3">
              <div className="flex-shrink-0 text-red-600">
                <svg
                  className="h-6 w-6"
                  fill="currentColor"
                  viewBox="0 0 20 20"
                >
                  <path
                    fillRule="evenodd"
                    d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                    clipRule="evenodd"
                  />
                </svg>
              </div>
              <div className="flex-1">
                <h3 className="text-sm font-semibold text-red-900 mb-1">
                  ⚠️ Error
                </h3>
                <p className="text-sm text-red-800">{error.message}</p>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
