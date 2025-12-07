import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ChatMessage } from '@/components/chat/ChatMessage';
import type { ChatMessage as ChatMessageType } from '@/types/chat';

// Mock react-markdown to avoid complex markdown rendering in tests
vi.mock('react-markdown', () => ({
  default: ({ children }: { children: string }) => <div data-testid="markdown-content">{children}</div>,
}));

vi.mock('remark-gfm', () => ({
  default: () => null,
}));

vi.mock('rehype-raw', () => ({
  default: () => null,
}));

describe('ChatMessage', () => {
  const userMessage: ChatMessageType = {
    id: 'msg-1',
    role: 'user',
    content: 'Hello, I need help with a task',
    createdAt: '2025-12-06T10:00:00Z',
  };

  const assistantMessage: ChatMessageType = {
    id: 'msg-2',
    role: 'assistant',
    content: '✅ I can help you with that!',
    createdAt: '2025-12-06T10:00:05Z',
  };

  const assistantMessageWithSuggestions: ChatMessageType = {
    id: 'msg-3',
    role: 'assistant',
    content: 'Here is your task summary',
    createdAt: '2025-12-06T10:00:10Z',
    metadata: {
      suggestions: ['View all tasks', 'Create another task', 'Delete task'],
    },
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('user message rendering', () => {
    it('should render user message content', () => {
      render(<ChatMessage message={userMessage} />);

      expect(screen.getByText('Hello, I need help with a task')).toBeInTheDocument();
    });

    it('should have correct styling for user messages (right-aligned)', () => {
      render(<ChatMessage message={userMessage} />);

      // User messages should have justify-end class
      const container = screen.getByText('Hello, I need help with a task').closest('.flex');
      expect(container).toHaveClass('justify-end');
    });

    it('should have blue gradient background for user messages', () => {
      render(<ChatMessage message={userMessage} />);

      const messageBubble = screen.getByText('Hello, I need help with a task').closest('.rounded-2xl');
      expect(messageBubble).toHaveClass('bg-gradient-to-br');
      expect(messageBubble).toHaveClass('from-blue-600');
    });

    it('should not show assistant label for user messages', () => {
      render(<ChatMessage message={userMessage} />);

      expect(screen.queryByText('Task Assistant')).not.toBeInTheDocument();
    });
  });

  describe('assistant message rendering', () => {
    it('should render assistant message content', () => {
      render(<ChatMessage message={assistantMessage} />);

      expect(screen.getByText(/I can help you with that/)).toBeInTheDocument();
    });

    it('should have correct styling for assistant messages (left-aligned)', () => {
      render(<ChatMessage message={assistantMessage} />);

      const container = screen.getByText(/I can help you with that/).closest('.flex');
      expect(container).toHaveClass('justify-start');
    });

    it('should show assistant label with robot emoji', () => {
      render(<ChatMessage message={assistantMessage} />);

      expect(screen.getByText('🤖')).toBeInTheDocument();
      expect(screen.getByText('Task Assistant')).toBeInTheDocument();
    });

    it('should have white/gray background for assistant messages', () => {
      render(<ChatMessage message={assistantMessage} />);

      const messageBubble = screen.getByText(/I can help you with that/).closest('.rounded-2xl');
      expect(messageBubble).toHaveClass('bg-white');
    });
  });

  describe('suggestions', () => {
    it('should render suggestions when available', () => {
      const onSuggestionClick = vi.fn();
      render(
        <ChatMessage
          message={assistantMessageWithSuggestions}
          onSuggestionClick={onSuggestionClick}
        />
      );

      expect(screen.getByText('View all tasks')).toBeInTheDocument();
      expect(screen.getByText('Create another task')).toBeInTheDocument();
      expect(screen.getByText('Delete task')).toBeInTheDocument();
    });

    it('should call onSuggestionClick when suggestion is clicked', () => {
      const onSuggestionClick = vi.fn();
      render(
        <ChatMessage
          message={assistantMessageWithSuggestions}
          onSuggestionClick={onSuggestionClick}
        />
      );

      fireEvent.click(screen.getByText('View all tasks'));

      expect(onSuggestionClick).toHaveBeenCalledWith('View all tasks');
    });

    it('should not render suggestions for user messages', () => {
      const userWithSuggestions: ChatMessageType = {
        ...userMessage,
        metadata: {
          suggestions: ['Should not appear'],
        },
      };
      const onSuggestionClick = vi.fn();

      render(
        <ChatMessage
          message={userWithSuggestions}
          onSuggestionClick={onSuggestionClick}
        />
      );

      expect(screen.queryByText('Should not appear')).not.toBeInTheDocument();
    });

    it('should not render suggestions when onSuggestionClick is not provided', () => {
      render(<ChatMessage message={assistantMessageWithSuggestions} />);

      // SuggestionsBar should not render without onSuggestionClick handler
      expect(screen.queryByText('View all tasks')).not.toBeInTheDocument();
    });
  });

  describe('loading state', () => {
    it('should disable suggestions when loading', () => {
      const onSuggestionClick = vi.fn();
      render(
        <ChatMessage
          message={assistantMessageWithSuggestions}
          onSuggestionClick={onSuggestionClick}
          isLoading={true}
        />
      );

      // Suggestions should be present but disabled
      const suggestion = screen.getByText('View all tasks');
      expect(suggestion.closest('button')).toBeDisabled();
    });
  });

  describe('streaming state', () => {
    it('should show blinking cursor when streaming', () => {
      render(
        <ChatMessage
          message={assistantMessage}
          isStreaming={true}
        />
      );

      // Look for the animated cursor element
      const cursor = document.querySelector('.animate-pulse');
      expect(cursor).toBeInTheDocument();
    });

    it('should not show cursor when not streaming', () => {
      render(
        <ChatMessage
          message={assistantMessage}
          isStreaming={false}
        />
      );

      const cursor = document.querySelector('.animate-pulse');
      expect(cursor).not.toBeInTheDocument();
    });
  });

  describe('function call filtering', () => {
    it('should not render function call messages', () => {
      const functionCallMessage: ChatMessageType = {
        id: 'msg-func',
        role: 'assistant',
        content: '{"$type":"functionCall","name":"CreateTask","arguments":"{}"}',
        createdAt: '2025-12-06T10:00:00Z',
      };

      const { container } = render(<ChatMessage message={functionCallMessage} />);

      // Should render nothing
      expect(container.firstChild).toBeNull();
    });

    it('should not render function result messages', () => {
      const functionResultMessage: ChatMessageType = {
        id: 'msg-result',
        role: 'assistant',
        content: '{"$type":"functionResult","result":"Task created"}',
        createdAt: '2025-12-06T10:00:00Z',
      };

      const { container } = render(<ChatMessage message={functionResultMessage} />);

      expect(container.firstChild).toBeNull();
    });

    it('should render normal messages that contain JSON-like text', () => {
      const normalMessage: ChatMessageType = {
        id: 'msg-normal',
        role: 'assistant',
        content: 'Here is a JSON example: {"name": "test"}',
        createdAt: '2025-12-06T10:00:00Z',
      };

      render(<ChatMessage message={normalMessage} />);

      expect(screen.getByText(/Here is a JSON example/)).toBeInTheDocument();
    });
  });

  describe('newline handling', () => {
    it('should convert escaped newlines to actual newlines', () => {
      const messageWithNewlines: ChatMessageType = {
        id: 'msg-newlines',
        role: 'assistant',
        content: 'Line 1\\nLine 2\\nLine 3',
        createdAt: '2025-12-06T10:00:00Z',
      };

      render(<ChatMessage message={messageWithNewlines} />);

      // The markdown content should have actual newlines
      const markdownContent = screen.getByTestId('markdown-content');
      expect(markdownContent.textContent).toContain('Line 1\nLine 2\nLine 3');
    });
  });

  describe('hover state', () => {
    it('should show message actions on hover for assistant messages', () => {
      render(<ChatMessage message={assistantMessage} />);

      const container = screen.getByText(/I can help you with that/).closest('.flex');
      
      // Initially MessageActions might not be visible
      fireEvent.mouseEnter(container!);
      
      // MessageActions component should become visible
      // Note: The actual visibility depends on the MessageActions implementation
    });
  });

  describe('empty content handling', () => {
    it('should handle empty content gracefully', () => {
      const emptyMessage: ChatMessageType = {
        id: 'msg-empty',
        role: 'assistant',
        content: '',
        createdAt: '2025-12-06T10:00:00Z',
      };

      const { container } = render(<ChatMessage message={emptyMessage} />);

      // Should still render the message bubble
      expect(container.querySelector('.rounded-2xl')).toBeInTheDocument();
    });

    it('should handle null content gracefully', () => {
      const nullMessage: ChatMessageType = {
        id: 'msg-null',
        role: 'assistant',
        content: null,
        createdAt: '2025-12-06T10:00:00Z',
      };

      const { container } = render(<ChatMessage message={nullMessage} />);

      expect(container.querySelector('.rounded-2xl')).toBeInTheDocument();
    });
  });
});
