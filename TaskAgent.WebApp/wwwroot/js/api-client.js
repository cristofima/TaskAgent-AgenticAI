/**
 * API Client Module
 * SRP: Single responsibility - handles HTTP communication with backend
 * Separates network logic from UI logic
 */
class ChatApiClient {
    constructor(baseUrl = '/api/chat') {
        this.baseUrl = baseUrl;
    }

    /**
     * Sends a message to the chat API
     * @param {string} message - User message
     * @param {string|null} threadId - Optional conversation thread ID
     * @returns {Promise<{success: boolean, data?: any, error?: any}>}
     */
    async sendMessage(message, threadId) {
        try {
            const response = await fetch(`${this.baseUrl}/send`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    message: message,
                    threadId: threadId
                })
            });

            const data = await response.json();

            if (!response.ok) {
                return {
                    success: false,
                    error: {
                        status: response.status,
                        data: data
                    }
                };
            }

            return {
                success: true,
                data: data
            };
        } catch (error) {
            return {
                success: false,
                error: {
                    status: 0,
                    data: {
                        error: 'NetworkError',
                        message: 'Failed to connect to server. Please check your connection.',
                        details: error.message
                    }
                }
            };
        }
    }

    /**
     * Creates a new conversation thread
     * @returns {Promise<{success: boolean, threadId?: string, error?: any}>}
     */
    async createNewThread() {
        try {
            const response = await fetch(`${this.baseUrl}/new-thread`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            const data = await response.json();

            if (!response.ok) {
                return { success: false, error: data };
            }

            return {
                success: true,
                threadId: data.threadId
            };
        } catch (error) {
            return {
                success: false,
                error: { message: error.message }
            };
        }
    }
}

// Export for use in other modules
window.ChatApiClient = ChatApiClient;
