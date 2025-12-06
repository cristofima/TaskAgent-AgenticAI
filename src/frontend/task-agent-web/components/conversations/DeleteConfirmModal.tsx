"use client";

/**
 * DeleteConfirmModal Component
 * Custom confirmation modal for delete actions
 * Uses React Portal to render outside the component hierarchy
 */

import { createPortal } from "react-dom";

interface DeleteConfirmModalProps {
  isOpen: boolean;
  title: string;
  chatTitle: string;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteConfirmModal({
  isOpen,
  title,
  chatTitle,
  onConfirm,
  onCancel,
}: DeleteConfirmModalProps) {
  // Only render on client side
  if (!isOpen || typeof window === "undefined") return null;

  return createPortal(
    <div className="fixed inset-0 z-[9999] flex items-center justify-center">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/50 animate-fadeIn"
        onClick={onCancel}
      />

      {/* Modal */}
      <div className="relative z-10 p-4 w-full max-w-md">
        <div
          className="bg-white dark:bg-gray-800 rounded-xl shadow-2xl w-full p-6 animate-scaleIn"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Title */}
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">{title}</h3>

          {/* Message with chat name inline */}
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
            This will delete{" "}
            <span className="font-semibold text-gray-900 dark:text-white">{chatTitle}</span>.
          </p>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3">
            <button
              onClick={onCancel}
              className="
                px-4 py-2 rounded-lg
                text-sm font-medium
                text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white
                bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600
                transition-colors
                cursor-pointer
              "
            >
              Cancel
            </button>
            <button
              onClick={onConfirm}
              className="
                px-4 py-2 rounded-lg
                text-sm font-medium
                text-white
                bg-red-600 hover:bg-red-700
                transition-colors
                cursor-pointer
              "
            >
              Delete
            </button>
          </div>
        </div>
      </div>
    </div>,
    document.body
  );
}
