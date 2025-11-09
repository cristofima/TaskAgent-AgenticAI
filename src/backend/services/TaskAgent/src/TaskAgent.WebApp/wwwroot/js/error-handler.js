/**
 * Error Handler Module
 * SRP: Single responsibility - handles error display and formatting
 * Specializes in Content Safety violations and other API errors
 */
class ChatErrorHandler {
    /**
     * Creates user-friendly error message from API error response
     * @param {object} error - Error object from API client
     * @returns {string} - Formatted error message for display
     */
    formatErrorMessage(error) {
        if (!error) {
            return 'An unknown error occurred. Please try again.';
        }

        const { status, data } = error;

        // Content Safety Violations (400 Bad Request from middleware)
        if (status === 400 && data?.error) {
            return this._formatContentSafetyError(data);
        }

        // Validation errors (422)
        if (status === 422) {
            return `⚠️ ${data?.message || 'Invalid input. Please check your message.'}`;
        }

        // Server errors (500)
        if (status === 500) {
            return '❌ Server error. Our team has been notified. Please try again later.';
        }

        // Network errors
        if (status === 0) {
            return '🔌 Connection error. Please check your internet connection.';
        }

        // Generic error
        return data?.message || 'Something went wrong. Please try again.';
    }

    /**
     * Formats Content Safety-specific error messages
     * Following industry best practices:
     * - No technical details exposed (security risk - enables attack iteration)
     * - Generic, professional message (no alert styling)
     * - Brief and conversational tone
     * @private
     */
    _formatContentSafetyError(data) {
        const errorType = data.error;

        switch (errorType) {
            case 'PromptInjectionDetected':
            case 'ContentPolicyViolation':
                // Generic response - don't expose attack details or violated categories
                // This prevents attackers from iterating and refining their attempts
                return "I'm sorry, but I can't assist with that request. Please try rephrasing your message to focus on task management.";

            case 'InvalidInput':
                return data.message || 'Please check your message and try again.';

            default:
                return data.message || 'I cannot process this request at the moment. Please try again.';
        }
    }

    /**
     * Determines error severity for styling
     * Following best practices: Content Safety violations use normal bot styling (not alerts)
     * Red/orange alerts only for technical errors (network, server)
     * @param {object} error - Error object
     * @returns {string} - CSS class name for severity
     */
    getErrorSeverity(error) {
        if (!error || !error.data) return 'error';

        const errorType = error.data.error;

        // Content Safety violations: Use normal bot message style
        // They respond naturally as if it's a conversational boundary
        if (errorType === 'PromptInjectionDetected' || errorType === 'ContentPolicyViolation') {
            return 'normal'; // Will render as regular bot message
        }

        // Technical errors use error styling
        return 'error';
    }

    /**
     * Checks if error is recoverable (user can retry with different input)
     * @param {object} error - Error object
     * @returns {boolean}
     */
    isRecoverableError(error) {
        if (!error) return true;

        const status = error.status;
        const errorType = error.data?.error;

        // Security and content violations are recoverable - user can rephrase
        if (status === 400 && (errorType === 'PromptInjectionDetected' || errorType === 'ContentPolicyViolation')) {
            return true;
        }

        // Validation errors are recoverable
        if (status === 422) {
            return true;
        }

        // Server errors and network errors are not recoverable (system issue)
        if (status === 500 || status === 0) {
            return false;
        }

        return true;
    }
}

// Export for use in other modules
window.ChatErrorHandler = ChatErrorHandler;
