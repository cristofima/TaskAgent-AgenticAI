"use client";

/**
 * ChatHeader Component
 * Displays the chat header with sidebar toggle and title
 */

interface ChatHeaderProps {
  onToggleSidebar: () => void;
}

export function ChatHeader({ onToggleSidebar }: ChatHeaderProps) {
  return (
    <div className="flex-shrink-0 bg-gray-50 border-b border-gray-200 px-4 py-3 shadow-sm">
      <div className="flex items-center gap-3 max-w-4xl mx-auto">
        {/* Sidebar toggle button (mobile) */}
        <button
          onClick={onToggleSidebar}
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
  );
}
