"use client";

/**
 * ChatInterface Component
 * Main chat interface using Vercel AI SDK with custom MVC-inspired styling
 * Supports streaming responses from .NET backend API
 */

import { useChat } from "@/hooks/use-chat";
import { ChatMessagesList } from "./ChatMessagesList";
import { ChatInput } from "./ChatInput";

/**
 * Main ChatInterface component with MVC-inspired design
 */
export function ChatInterface() {
  const {
    messages,
    input,
    isLoading,
    error,
    handleInputChange,
    handleSubmit,
    sendSuggestion,
  } = useChat({
    onError: (error) => {
      console.error("Chat error:", error);
    },
  });

  const hasMessages = messages.length > 0;

  return (
    <div className="h-screen flex flex-col bg-gray-50">
      {/* Header - Only show when there are messages */}
      {hasMessages && (
        <div className="flex-shrink-0 bg-white border-b border-gray-200 px-4 py-3 shadow-sm">
          <div className="flex items-center gap-2 max-w-4xl mx-auto">
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
          <div className="flex-shrink-0 border-t border-gray-200 bg-white">
            <ChatInput
              input={input}
              isLoading={isLoading}
              handleInputChange={handleInputChange}
              handleSubmit={handleSubmit}
            />
          </div>
        </>
      ) : (
        /* Empty state: centered welcome + input */
        <div className="flex-1 flex flex-col items-center justify-center px-4">
          <div className="w-full max-w-3xl">
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
              <svg className="h-6 w-6" fill="currentColor" viewBox="0 0 20 20">
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
  );
}
