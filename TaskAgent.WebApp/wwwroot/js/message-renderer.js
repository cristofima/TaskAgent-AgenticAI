/**
 * Message Renderer Module
 * SRP: Single responsibility - handles message rendering, markdown parsing, and syntax highlighting
 * Separates presentation logic from business logic
 */
class ChatMessageRenderer {
    constructor() {
        // Configure Marked.js with options
        marked.setOptions({
            breaks: true,        // Convert \n to <br>
            gfm: true,          // GitHub Flavored Markdown
            headerIds: true,
            mangle: false,
            highlight: function(code, lang) {
                if (lang && hljs.getLanguage(lang)) {
                    try {
                        return hljs.highlight(code, { language: lang }).value;
                    } catch (err) {
                        console.error('Highlight error:', err);
                    }
                }
                return hljs.highlightAuto(code).value;
            }
        });
    }

    /**
     * Renders a user message
     * @param {string} text - Message text
     * @returns {HTMLElement} - Rendered message element
     */
    renderUserMessage(text) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'message user-message';

        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';
        contentDiv.innerHTML = `<strong>You:</strong><p class="mb-0 mt-1">${this._escapeHtml(text).replace(/\n/g, '<br>')}</p>`;

        messageDiv.appendChild(contentDiv);
        return messageDiv;
    }

    /**
     * Renders a bot message with markdown support
     * @param {string} text - Message text (markdown)
     * @returns {HTMLElement} - Rendered message element
     */
    renderBotMessage(text) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'message bot-message';

        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';

        // Parse Markdown and sanitize HTML
        const markdownHtml = marked.parse(text);
        const sanitizedHtml = DOMPurify.sanitize(markdownHtml, {
            ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'u', 'code', 'pre', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 
                          'ul', 'ol', 'li', 'blockquote', 'a', 'table', 'thead', 'tbody', 'tr', 'th', 'td', 
                          'span', 'div', 'hr'],
            ALLOWED_ATTR: ['href', 'class', 'id', 'style', 'target', 'rel']
        });

        contentDiv.innerHTML = `<strong>Task Agent:</strong><div class="markdown-content mt-2">${sanitizedHtml}</div>`;
        
        // Apply syntax highlighting to code blocks
        contentDiv.querySelectorAll('pre code').forEach((block) => {
            hljs.highlightElement(block);
        });

        messageDiv.appendChild(contentDiv);
        return messageDiv;
    }

    /**
     * Renders an error message
     * @param {string} text - Error message text
     * @param {string} severity - Error severity (error, warning-error, security-error, info-error)
     * @returns {HTMLElement} - Rendered error message element
     */
    renderErrorMessage(text, severity = 'error') {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message bot-message ${severity}`;

        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';

        // Parse markdown in error messages (for formatting like **bold**)
        const markdownHtml = marked.parse(text);
        const sanitizedHtml = DOMPurify.sanitize(markdownHtml, {
            ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'code'],
            ALLOWED_ATTR: []
        });

        contentDiv.innerHTML = `<strong>⚠️ System:</strong><div class="mt-2">${sanitizedHtml}</div>`;

        messageDiv.appendChild(contentDiv);
        return messageDiv;
    }

    /**
     * Renders a PII warning message
     * @param {object} piiInfo - PII detection information
     * @returns {HTMLElement} - Rendered warning message element
     */
    renderPiiWarning(piiInfo) {
        const messageDiv = document.createElement('div');
        messageDiv.className = 'message bot-message info-error';

        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';

        const entityTypes = piiInfo.detectedTypes?.join(', ') || 'sensitive information';
        
        contentDiv.innerHTML = `
            <strong>🔒 Privacy Notice:</strong>
            <div class="mt-2">
                <p>We detected potential personal information in your message: <strong>${entityTypes}</strong></p>
                <p class="mb-0"><em>Note: Sensitive data has been redacted for your privacy. Your message was processed successfully.</em></p>
            </div>
        `;

        messageDiv.appendChild(contentDiv);
        return messageDiv;
    }

    /**
     * Escapes HTML to prevent XSS
     * @private
     */
    _escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}

// Export for use in other modules
window.ChatMessageRenderer = ChatMessageRenderer;
