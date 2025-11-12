"use client";

/**
 * ChatInterface Component
 * Main chat interface using Vercel AI SDK with custom MVC-inspired styling
 * Supports streaming responses from .NET backend API
 */

import Link from "next/link";
import { useChat } from "@/hooks/use-chat";
import { ChatMessagesList } from "./ChatMessagesList";
import { ChatInput } from "./ChatInput";

/**
 * Main ChatInterface component with MVC-inspired design
 */
export function ChatInterface() {
  const { messages, input, isLoading, error, handleInputChange, handleSubmit } =
    useChat({
      onError: (error) => {
        console.error("Chat error:", error);
      },
    });

  return (
    <div className="min-h-screen bg-gradient-radial from-blue-500/80 via-blue-800 to-slate-950">
      {/* Header/Navbar */}
      <nav className="bg-gray-900/95 backdrop-blur-sm shadow-lg border-b border-gray-700/50">
        <div className="container mx-auto px-4">
          <div className="flex items-center h-16">
            <Link
              href="/"
              className="flex items-center gap-2 text-white text-xl font-semibold hover:text-blue-400 transition-colors"
            >
              <span className="text-2xl">📋</span>
              <span>Task Agent</span>
            </Link>
          </div>
        </div>
      </nav>

      {/* Chat Container */}
      <div className="container mx-auto px-2 sm:px-4 py-4 sm:py-6 lg:py-8">
        <div className="flex justify-center">
          <div className="w-full max-w-sm sm:max-w-2xl md:max-w-3xl lg:max-w-4xl xl:max-w-5xl">
            <div
              className="bg-white shadow-2xl rounded-xl sm:rounded-2xl flex flex-col overflow-hidden border border-gray-100"
              style={{ height: "calc(100vh - 160px)" }}
            >
              {/* Card Header */}
              <div className="bg-gradient-to-r from-blue-600 via-blue-600 to-blue-700 text-white px-4 sm:px-6 py-4 sm:py-5 shadow-md">
                <h5 className="text-lg sm:text-xl font-bold mb-1 flex items-center gap-2">
                  <span className="text-xl sm:text-2xl">📋</span>
                  <span className="hidden sm:inline">
                    Task Management Assistant
                  </span>
                  <span className="sm:hidden">Task Assistant</span>
                </h5>
                <small className="text-blue-100 text-xs sm:text-sm">
                  Your AI-powered task organizer
                </small>
              </div>

              {/* Chat Messages Area */}
              <div className="flex-1 overflow-hidden bg-gray-50">
                <ChatMessagesList messages={messages} isLoading={isLoading} />
              </div>

              {/* Input Area */}
              <ChatInput
                input={input}
                isLoading={isLoading}
                handleInputChange={handleInputChange}
                handleSubmit={handleSubmit}
              />
            </div>
          </div>
        </div>
      </div>

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
