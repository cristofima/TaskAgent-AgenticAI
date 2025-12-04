"use client";

/**
 * useKeyboardShortcuts Hook
 * Provides keyboard shortcuts functionality for the chat interface
 */

import { useEffect, useCallback } from "react";

interface KeyboardShortcutsOptions {
  /** Focus the chat input */
  onFocusInput?: () => void;
  /** Start a new conversation */
  onNewConversation?: () => void;
  /** Toggle the sidebar */
  onToggleSidebar?: () => void;
  /** Submit the current message */
  onSubmit?: () => void;
  /** Whether shortcuts are enabled */
  enabled?: boolean;
}

interface KeyboardShortcut {
  key: string;
  ctrlKey?: boolean;
  metaKey?: boolean;
  shiftKey?: boolean;
  description: string;
  action: () => void;
}

/**
 * Hook to handle keyboard shortcuts throughout the application
 * 
 * Shortcuts:
 * - Ctrl/Cmd + K: Focus chat input
 * - Ctrl/Cmd + Shift + N: New conversation
 * - Ctrl/Cmd + B: Toggle sidebar
 * - Escape: Blur input / close modals
 */
export function useKeyboardShortcuts({
  onFocusInput,
  onNewConversation,
  onToggleSidebar,
  onSubmit,
  enabled = true,
}: KeyboardShortcutsOptions) {
  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      if (!enabled) return;

      // Don't trigger shortcuts when typing in inputs (except for specific ones)
      const target = event.target as HTMLElement;
      const isInputElement =
        target.tagName === "INPUT" ||
        target.tagName === "TEXTAREA" ||
        target.isContentEditable;

      const isMac = navigator.platform.toUpperCase().indexOf("MAC") >= 0;
      const ctrlOrCmd = isMac ? event.metaKey : event.ctrlKey;

      // Ctrl/Cmd + K: Focus chat input (works anywhere)
      if (ctrlOrCmd && event.key.toLowerCase() === "k") {
        event.preventDefault();
        onFocusInput?.();
        return;
      }

      // Ctrl/Cmd + Shift + N: New conversation (works anywhere)
      if (ctrlOrCmd && event.shiftKey && event.key.toLowerCase() === "n") {
        event.preventDefault();
        onNewConversation?.();
        return;
      }

      // Ctrl/Cmd + B: Toggle sidebar (works anywhere)
      if (ctrlOrCmd && event.key.toLowerCase() === "b") {
        event.preventDefault();
        onToggleSidebar?.();
        return;
      }

      // Escape: Blur current input
      if (event.key === "Escape" && isInputElement) {
        event.preventDefault();
        (target as HTMLInputElement | HTMLTextAreaElement).blur();
        return;
      }

      // Enter without shift in textarea: Submit (handled by input component, but can trigger here too)
      if (
        event.key === "Enter" &&
        !event.shiftKey &&
        isInputElement &&
        target.tagName === "TEXTAREA"
      ) {
        // Let the input handle this, but can add additional logic here
        // Note: Don't prevent default here as the input handles submission
      }
    },
    [enabled, onFocusInput, onNewConversation, onToggleSidebar, onSubmit]
  );

  useEffect(() => {
    if (!enabled) return;

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [enabled, handleKeyDown]);

  // Return available shortcuts for documentation/help
  const shortcuts: KeyboardShortcut[] = [
    {
      key: "K",
      ctrlKey: true,
      description: "Focus chat input",
      action: () => onFocusInput?.(),
    },
    {
      key: "N",
      ctrlKey: true,
      shiftKey: true,
      description: "New conversation",
      action: () => onNewConversation?.(),
    },
    {
      key: "B",
      ctrlKey: true,
      description: "Toggle sidebar",
      action: () => onToggleSidebar?.(),
    },
    {
      key: "Escape",
      description: "Blur input / Close modal",
      action: () => {},
    },
  ];

  return { shortcuts };
}

/**
 * Format a keyboard shortcut for display
 * @param shortcut - The shortcut to format
 * @returns Formatted string like "⌘K" or "Ctrl+K"
 */
export function formatShortcut(shortcut: KeyboardShortcut): string {
  const isMac =
    typeof navigator !== "undefined" &&
    navigator.platform.toUpperCase().indexOf("MAC") >= 0;

  const parts: string[] = [];

  if (shortcut.ctrlKey || shortcut.metaKey) {
    parts.push(isMac ? "⌘" : "Ctrl");
  }
  if (shortcut.shiftKey) {
    parts.push(isMac ? "⇧" : "Shift");
  }
  parts.push(shortcut.key);

  return isMac ? parts.join("") : parts.join("+");
}
