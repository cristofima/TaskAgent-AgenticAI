"use client";

import dynamic from "next/dynamic";

// Load ChatInterface only on client side to prevent hydration issues
const ChatInterface = dynamic(
  () =>
    import("./ChatInterface").then((mod) => ({
      default: mod.ChatInterface,
    })),
  {
    ssr: false,
    loading: () => (
      <div className="flex items-center justify-center min-h-screen bg-gradient-radial from-blue-500/80 via-blue-800 to-slate-950">
        <div className="flex flex-col items-center gap-4">
          <div className="inline-block h-12 w-12 border-4 border-white border-t-transparent rounded-full animate-spin"></div>
          <div className="text-white text-xl font-semibold">Loading...</div>
        </div>
      </div>
    ),
  }
);

export function ChatInterfaceClient() {
  return <ChatInterface />;
}
