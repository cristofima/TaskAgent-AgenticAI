"use client";

import { type UIMessage } from "ai";
import ReactMarkdown from "react-markdown";

interface TextPart {
  type: "text";
  text: string;
}

interface ChatMessageProps {
  message: UIMessage;
}

/**
 * Individual chat message bubble component with MVC-inspired styling
 */
export function ChatMessage({ message }: ChatMessageProps) {
  const isUser = message.role === "user";

  // Extract text content from message parts
  const textContent =
    message.parts
      ?.filter((part): part is TextPart => part.type === "text")
      .map((part) => part.text)
      .join("\n") || "";

  return (
    <div
      className={`flex animate-fadeIn ${
        isUser ? "justify-end" : "justify-start"
      }`}
    >
      <div
        className={`rounded-2xl px-5 py-3.5 max-w-[85%] shadow-md hover:shadow-lg transition-shadow ${
          isUser
            ? "bg-gradient-to-br from-blue-600 to-blue-700 text-white ml-auto text-right"
            : "bg-white border-2 border-gray-200 mr-auto"
        }`}
      >
        {!isUser && (
          <div className="font-bold text-gray-900 mb-2 flex items-center gap-2">
            <span className="text-lg">🤖</span>
            <span>Task Assistant</span>
          </div>
        )}
        <div
          className={`markdown-content leading-relaxed ${
            isUser ? "text-white" : "text-gray-800"
          }`}
        >
          <ReactMarkdown>{textContent}</ReactMarkdown>
        </div>
      </div>
    </div>
  );
}
