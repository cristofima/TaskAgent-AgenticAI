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
     * @private
     */
    _formatContentSafetyError(data) {
        const errorType = data.error;

        switch (errorType) {
            case 'SecurityViolation':
                return `🛡️ **Security Alert**\n\n${data.message}\n\n` +
                       `*Please rephrase your request for legitimate task management.*`;

            case 'ContentViolation':
                const categories = data.violatedCategories?.join(', ') || 'policy';
                return `⚠️ **Content Policy Violation**\n\n${data.message}\n\n` +
                       `**Violated Categories:** ${categories}\n\n` +
                       `*Your message contains content that violates our policy. ` +
                       `Please keep your messages professional and task-focused.*`;

            case 'InvalidInput':
                return `📝 ${data.message}`;

            default:
                return `❌ ${data.message || 'Your request could not be processed.'}`;
        }
    }

    /**
     * Determines error severity for styling
     * @param {object} error - Error object
     * @returns {string} - CSS class name for severity
     */
    getErrorSeverity(error) {
        if (!error || !error.data) return 'error';

        const errorType = error.data.error;

        if (errorType === 'SecurityViolation') {
            return 'security-error'; // Highest severity - red
        }

        if (errorType === 'ContentViolation') {
            return 'warning-error'; // Medium severity - orange
        }

        if (error.data.containsPii) {
            return 'info-error'; // Informational - blue
        }

        return 'error'; // Default - standard error styling
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
        if (status === 400 && (errorType === 'SecurityViolation' || errorType === 'ContentViolation')) {
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
