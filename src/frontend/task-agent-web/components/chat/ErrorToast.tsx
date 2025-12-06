"use client";

/**
 * ErrorToast Component
 * Displays error messages in a toast notification
 */

interface ErrorToastProps {
  error: Error | null;
}

export function ErrorToast({ error }: ErrorToastProps) {
  if (!error) return null;

  return (
    <div className="fixed top-4 right-4 max-w-md bg-red-50 dark:bg-red-900/30 border-2 border-red-200 dark:border-red-800 border-l-4 border-l-red-500 rounded-lg shadow-lg p-4 animate-fadeIn z-50">
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0 text-red-600 dark:text-red-400">
          <svg className="h-6 w-6" fill="currentColor" viewBox="0 0 20 20">
            <path
              fillRule="evenodd"
              d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
              clipRule="evenodd"
            />
          </svg>
        </div>
        <div className="flex-1">
          <h3 className="text-sm font-semibold text-red-900 dark:text-red-300 mb-1">⚠️ Error</h3>
          <p className="text-sm text-red-800 dark:text-red-400">{error.message}</p>
        </div>
      </div>
    </div>
  );
}
