let threadId = null;

const messageInput = document.getElementById('messageInput');
const sendButton = document.getElementById('sendButton');
const chatMessages = document.getElementById('chatMessages');
const sendIcon = document.getElementById('sendIcon');
const loadingIcon = document.getElementById('loadingIcon');

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

// Send message on button click
sendButton.addEventListener('click', sendMessage);

// Send message on Enter key
messageInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

async function sendMessage() {
    const message = messageInput.value.trim();

    if (!message) return;

    // Add user message to chat
    addMessage(message, 'user');
    messageInput.value = '';

    // Disable input while processing
    setLoading(true);

    try {
        const response = await fetch('/api/chat/send', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                message: message,
                threadId: threadId
            })
        });

        if (!response.ok) {
            throw new Error('Failed to send message');
        }

        const data = await response.json();

        // Store thread ID for conversation continuity
        if (!threadId) {
            threadId = data.threadId;
        }

        // Add bot response to chat
        addMessage(data.message, 'bot');
    } catch (error) {
        console.error('Error:', error);
        addMessage('Sorry, something went wrong. Please try again.', 'bot');
    } finally {
        setLoading(false);
    }
}

function addMessage(text, sender) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${sender}-message`;

    const contentDiv = document.createElement('div');
    contentDiv.className = 'message-content';

    if (sender === 'bot') {
        // Parse Markdown and sanitize HTML for bot messages
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
    } else {
        // User messages - escape HTML but preserve line breaks
        contentDiv.innerHTML = `<strong>You:</strong><p class="mb-0 mt-1">${escapeHtml(text).replace(/\n/g, '<br>')}</p>`;
    }

    messageDiv.appendChild(contentDiv);
    chatMessages.appendChild(messageDiv);

    // Scroll to bottom
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

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

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Focus input on load
messageInput.focus();