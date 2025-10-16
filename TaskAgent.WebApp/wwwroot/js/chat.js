/**
 * Main Chat Application
 * SRP: Single responsibility - coordinates chat UI interactions
 * Uses specialized modules for API, rendering, and error handling
 */

// Initialize services
const apiClient = new ChatApiClient();
const errorHandler = new ChatErrorHandler();
const messageRenderer = new ChatMessageRenderer();

// State
let threadId = null;

// DOM elements
const messageInput = document.getElementById('messageInput');
const sendButton = document.getElementById('sendButton');
const chatMessages = document.getElementById('chatMessages');
const sendIcon = document.getElementById('sendIcon');
const loadingIcon = document.getElementById('loadingIcon');

// Event listeners
sendButton.addEventListener('click', sendMessage);
messageInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

/**
 * Sends a message to the chat API
 */
async function sendMessage() {
    const message = messageInput.value.trim();

    if (!message) return;

    // Display user message
    addMessageToChat(messageRenderer.renderUserMessage(message));
    messageInput.value = '';

    // Disable input while processing
    setLoading(true);

    try {
        const result = await apiClient.sendMessage(message, threadId);

        if (!result.success) {
            // Handle error (including Content Safety violations)
            handleError(result.error);
            return;
        }

        // Success - store thread ID and display response
        if (!threadId) {
            threadId = result.data.threadId;
        }

        addMessageToChat(messageRenderer.renderBotMessage(result.data.message));

        // Check for PII warnings in response headers (if middleware adds them)
        // This would require accessing response headers, which fetch API supports
        // For now, we handle PII at the middleware level

    } catch (error) {
        console.error('Unexpected error:', error);
        addMessageToChat(messageRenderer.renderErrorMessage(
            'An unexpected error occurred. Please refresh the page and try again.',
            'error'
        ));
    } finally {
        setLoading(false);
    }
}

/**
 * Handles API errors and displays appropriate messages
 */
function handleError(error) {
    const errorMessage = errorHandler.formatErrorMessage(error);
    const severity = errorHandler.getErrorSeverity(error);
    
    addMessageToChat(messageRenderer.renderErrorMessage(errorMessage, severity));

    // Log to console for debugging
    if (error.status === 400 && error.data?.error === 'SecurityViolation') {
        console.warn('Security violation detected:', error.data);
    } else if (error.status === 400 && error.data?.error === 'ContentViolation') {
        console.warn('Content policy violation:', error.data);
    }
}

/**
 * Adds a message element to the chat
 */
function addMessageToChat(messageElement) {
    chatMessages.appendChild(messageElement);
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

/**
 * Sets loading state for input controls
 */
function setLoading(isLoading) {
    messageInput.disabled = isLoading;
    sendButton.disabled = isLoading;

    if (isLoading) {
        sendIcon.classList.add('d-none');
        loadingIcon.classList.remove('d-none');
    } else {
        sendIcon.classList.remove('d-none');
        loadingIcon.classList.add('d-none');
        messageInput.focus();
    }
}

// Focus input on load
messageInput.focus();