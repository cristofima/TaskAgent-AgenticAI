"use client";

/**
 * ChatHeader Component
 * Displays the chat header with sidebar toggle and title
 * Always visible in both desktop and mobile
 */

interface ChatHeaderProps {
  onToggleSidebar: () => void;
  isSidebarOpen?: boolean;
}

export function ChatHeader({
  onToggleSidebar,
  isSidebarOpen,
}: ChatHeaderProps) {
  return (
    <header className="flex-shrink-0 bg-white border-b border-gray-200 px-3 sm:px-4 py-3 sticky top-0 z-30">
      <div className="flex items-center gap-2 sm:gap-3">
        {/* Sidebar toggle button - only visible on mobile */}
        <button
          onClick={onToggleSidebar}
          className="md:hidden p-2 rounded-lg hover:bg-gray-100 text-gray-700 transition-colors cursor-pointer"
          aria-label={isSidebarOpen ? "Close sidebar" : "Open sidebar"}
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

        {/* Logo and Title */}
        <div className="flex items-center gap-2">
          <span className="text-xl sm:text-2xl">📋</span>
          <h1 className="font-semibold text-gray-900 text-base sm:text-lg">
            Task Agent
          </h1>
        </div>

        {/* Spacer */}
        <div className="flex-1"></div>

        {/* Optional: Add more actions here like settings, user menu, etc */}
      </div>
    </header>
  );
}
