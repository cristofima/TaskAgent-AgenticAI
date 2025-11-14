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
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
}

export function DeleteConfirmModal({
  isOpen,
  title,
  message,
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
          className="bg-white rounded-xl shadow-2xl w-full p-6 animate-scaleIn"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="flex items-start gap-4 mb-4">
            <div className="flex-shrink-0 w-12 h-12 rounded-full bg-red-100 flex items-center justify-center">
              <svg
                className="w-6 h-6 text-red-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                />
              </svg>
            </div>
            <div className="flex-1">
              <h3 className="text-lg font-semibold text-gray-900 mb-1">
                {title}
              </h3>
              <p className="text-sm text-gray-600">{message}</p>
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3">
            <button
              onClick={onCancel}
              className="
                px-4 py-2 rounded-lg
                text-sm font-medium
                text-gray-700 hover:text-gray-900
                bg-gray-100 hover:bg-gray-200
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
