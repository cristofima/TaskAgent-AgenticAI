"use client";

/**
 * ChatInterface Component
 * Main chat interface with conversation sidebar
 * Supports conversation management (list, load, delete, create new)
 * Includes keyboard shortcuts for improved productivity
 */

import { useState, useRef, useCallback } from "react";
import { useChat } from "@/hooks/use-chat";
import { useKeyboardShortcuts } from "@/hooks/use-keyboard-shortcuts";
import { ChatMessagesList } from "./ChatMessagesList";
import { ChatInput } from "./ChatInput";
import { ChatHeader } from "./ChatHeader";
import { EmptyChatState } from "./EmptyChatState";
import { ErrorToast } from "./ErrorToast";
import { ConversationSidebar } from "@/components/conversations/ConversationSidebar";

/**
 * Main ChatInterface component with sidebar and chat area
 */
export function ChatInterface() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  // Ref to hold the loadConversations function from ConversationSidebar
  const loadConversationsRef = useRef<(() => Promise<void>) | null>(null);
  // Ref to the chat input for focus management
  const inputRef = useRef<HTMLTextAreaElement>(null);

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
      // Also called when first valid message updates blocked thread's title
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
      console.error("Failed to load chat:", error);
    }
  };

  const handleNewConversation = useCallback(() => {
    clearMessages();
    setThreadId(null);
    // Focus the input after creating new conversation
    setTimeout(() => inputRef.current?.focus(), 100);
  }, [clearMessages, setThreadId]);

  const toggleSidebar = useCallback(() => {
    setIsSidebarOpen((prev) => !prev);
  }, []);

  const focusInput = useCallback(() => {
    inputRef.current?.focus();
  }, []);

  // Register keyboard shortcuts
  useKeyboardShortcuts({
    onFocusInput: focusInput,
    onNewConversation: handleNewConversation,
    onToggleSidebar: toggleSidebar,
    enabled: true,
  });

  return (
    <div className="h-screen flex bg-gray-50 dark:bg-gray-900">
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
        onConversationDeleted={(deletedThreadId) => {
          // Clear messages when current conversation is deleted
          if (deletedThreadId === threadId) {
            clearMessages();
            setThreadId(null);
          }
        }}
      />

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Header - Always visible */}
        <ChatHeader
          onToggleSidebar={toggleSidebar}
          isSidebarOpen={isSidebarOpen}
        />

        {/* Main content area */}
        {hasMessages ? (
          /* With messages: scrollable messages + fixed input */
          <>
            <div className="flex-1 overflow-y-auto">
              <div className="max-w-4xl mx-auto px-4">
                <ChatMessagesList
                  messages={messages}
                  isLoading={isLoading}
                  onSuggestionClick={sendSuggestion}
                />
              </div>
            </div>

            {/* Fixed Input at bottom */}
            <div className="flex-shrink-0 bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700 px-4 py-4">
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
        ) : (
          /* Empty state: centered welcome + input */
          <EmptyChatState
            messages={messages}
            input={input}
            isLoading={isLoading}
            handleInputChange={handleInputChange}
            handleSubmit={handleSubmit}
            sendSuggestion={sendSuggestion}
            inputRef={inputRef}
          />
        )}

        {/* Error Toast */}
        <ErrorToast error={error ?? null} />
      </div>
    </div>
  );
}
