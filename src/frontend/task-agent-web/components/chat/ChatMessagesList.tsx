"use client";

import { type UIMessage } from "ai";
import { ChatMessage } from "./ChatMessage";

interface ChatMessagesListProps {
  messages: UIMessage[];
  isLoading: boolean;
}

/**
 * Chat messages list container with empty state and loading indicator
 */
export function ChatMessagesList({
  messages,
  isLoading,
}: ChatMessagesListProps) {
  return (
    <div className="flex flex-col gap-4 p-6 bg-gray-50 overflow-auto h-full">
      {messages.length === 0 ? (
        <div className="flex flex-col items-center justify-center h-full text-center py-8">
          <div className="bg-white border-2 border-gray-200 rounded-2xl p-8 max-w-[650px] shadow-lg hover:shadow-xl transition-shadow">
            <div className="font-bold text-gray-900 mb-3 text-xl flex items-center justify-center gap-2">
              <span className="text-2xl">🤖</span>
              Task Assistant
            </div>
            <p className="mb-4 text-gray-700 text-base leading-relaxed">
              Hi! I&apos;m your task management assistant. I can help you
              create, organize, and track your tasks efficiently.
            </p>
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <p className="text-sm text-gray-700 font-medium mb-2">
                Try asking me:
              </p>
              <p className="text-sm text-blue-700 italic">
                &quot;Create a task to review project proposal&quot;
                <br />
                &quot;Show me all my high priority tasks&quot;
              </p>
            </div>
          </div>
        </div>
      ) : (
        <>
          {messages.map((message) => (
            <ChatMessage key={message.id} message={message} />
          ))}
          {isLoading && (
            <div className="flex items-center gap-2 text-blue-600 ml-4">
              <div className="h-2.5 w-2.5 bg-blue-600 rounded-full animate-bounce [animation-delay:-0.3s]"></div>
              <div className="h-2.5 w-2.5 bg-blue-600 rounded-full animate-bounce [animation-delay:-0.15s]"></div>
              <div className="h-2.5 w-2.5 bg-blue-600 rounded-full animate-bounce"></div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
