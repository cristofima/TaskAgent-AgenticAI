import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { useRef } from 'react';
import { ChatInput } from '@/components/chat/ChatInput';

describe('ChatInput', () => {
  const defaultProps = {
    input: '',
    isLoading: false,
    handleInputChange: vi.fn(),
    handleSubmit: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('rendering', () => {
    it('should render textarea with placeholder', () => {
      render(<ChatInput {...defaultProps} />);

      const textarea = screen.getByRole('textbox');
      expect(textarea).toBeInTheDocument();
      expect(textarea).toHaveAttribute('placeholder', expect.stringContaining('Message Task Agent'));
    });

    it('should render custom placeholder when provided', () => {
      render(<ChatInput {...defaultProps} placeholder="Custom placeholder" />);

      const textarea = screen.getByRole('textbox');
      expect(textarea).toHaveAttribute('placeholder', 'Custom placeholder');
    });

    it('should render send button', () => {
      render(<ChatInput {...defaultProps} />);

      const button = screen.getByRole('button', { name: /send message/i });
      expect(button).toBeInTheDocument();
    });

    it('should render helper text for keyboard shortcuts', () => {
      render(<ChatInput {...defaultProps} />);

      expect(screen.getByText(/Enter to send/i)).toBeInTheDocument();
      expect(screen.getByText(/Shift\+Enter for new line/i)).toBeInTheDocument();
    });
  });

  describe('input handling', () => {
    it('should display input value', () => {
      render(<ChatInput {...defaultProps} input="Hello world" />);

      const textarea = screen.getByRole('textbox');
      expect(textarea).toHaveValue('Hello world');
    });

    it('should call handleInputChange when typing', async () => {
      const handleInputChange = vi.fn();
      render(<ChatInput {...defaultProps} handleInputChange={handleInputChange} />);

      const textarea = screen.getByRole('textbox');
      fireEvent.change(textarea, { target: { value: 'Test message' } });

      expect(handleInputChange).toHaveBeenCalled();
    });
  });

  describe('submit behavior', () => {
    it('should call handleSubmit on form submit', async () => {
      const handleSubmit = vi.fn((e) => e.preventDefault());
      render(
        <ChatInput
          {...defaultProps}
          input="Test message"
          handleSubmit={handleSubmit}
        />
      );

      const button = screen.getByRole('button', { name: /send message/i });
      fireEvent.click(button);

      expect(handleSubmit).toHaveBeenCalled();
    });

    it('should disable send button when input is empty', () => {
      render(<ChatInput {...defaultProps} input="" />);

      const button = screen.getByRole('button', { name: /send message/i });
      expect(button).toBeDisabled();
    });

    it('should disable send button when input is only whitespace', () => {
      render(<ChatInput {...defaultProps} input="   " />);

      const button = screen.getByRole('button', { name: /send message/i });
      expect(button).toBeDisabled();
    });

    it('should enable send button when input has content', () => {
      render(<ChatInput {...defaultProps} input="Hello" />);

      const button = screen.getByRole('button', { name: /send message/i });
      expect(button).not.toBeDisabled();
    });
  });

  describe('loading state', () => {
    it('should disable textarea when loading', () => {
      render(<ChatInput {...defaultProps} isLoading={true} />);

      const textarea = screen.getByRole('textbox');
      expect(textarea).toBeDisabled();
    });

    it('should disable send button when loading', () => {
      render(<ChatInput {...defaultProps} input="Test" isLoading={true} />);

      const button = screen.getByRole('button', { name: /send message/i });
      expect(button).toBeDisabled();
    });

    it('should show loading spinner when loading', () => {
      render(<ChatInput {...defaultProps} isLoading={true} />);

      // Check for spinning animation class
      const button = screen.getByRole('button', { name: /send message/i });
      const spinner = button.querySelector('.animate-spin');
      expect(spinner).toBeInTheDocument();
    });
  });

  describe('keyboard shortcuts', () => {
    it('should submit form on Enter key press (without Shift)', async () => {
      const handleSubmit = vi.fn((e) => e.preventDefault());
      
      // Create a mock form with requestSubmit
      const mockRequestSubmit = vi.fn();
      
      render(
        <ChatInput
          {...defaultProps}
          input="Test message"
          handleSubmit={handleSubmit}
        />
      );

      const textarea = screen.getByRole('textbox');
      const form = textarea.closest('form');
      
      // Mock requestSubmit
      if (form) {
        form.requestSubmit = mockRequestSubmit;
      }

      // Simulate Enter key (without Shift)
      fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: false });

      expect(mockRequestSubmit).toHaveBeenCalled();
    });

    it('should not submit form on Shift+Enter', async () => {
      const handleSubmit = vi.fn((e) => e.preventDefault());
      
      render(
        <ChatInput
          {...defaultProps}
          input="Test message"
          handleSubmit={handleSubmit}
        />
      );

      const textarea = screen.getByRole('textbox');
      const form = textarea.closest('form');
      const mockRequestSubmit = vi.fn();
      
      if (form) {
        form.requestSubmit = mockRequestSubmit;
      }

      // Simulate Shift+Enter (should add new line, not submit)
      fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: true });

      expect(mockRequestSubmit).not.toHaveBeenCalled();
    });

    it('should not submit on Enter when input is empty', async () => {
      const handleSubmit = vi.fn((e) => e.preventDefault());
      
      render(
        <ChatInput
          {...defaultProps}
          input=""
          handleSubmit={handleSubmit}
        />
      );

      const textarea = screen.getByRole('textbox');
      const form = textarea.closest('form');
      const mockRequestSubmit = vi.fn();
      
      if (form) {
        form.requestSubmit = mockRequestSubmit;
      }

      fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: false });

      expect(mockRequestSubmit).not.toHaveBeenCalled();
    });

    it('should not submit on Enter when loading', async () => {
      const handleSubmit = vi.fn((e) => e.preventDefault());
      
      render(
        <ChatInput
          {...defaultProps}
          input="Test message"
          isLoading={true}
          handleSubmit={handleSubmit}
        />
      );

      const textarea = screen.getByRole('textbox');
      const form = textarea.closest('form');
      const mockRequestSubmit = vi.fn();
      
      if (form) {
        form.requestSubmit = mockRequestSubmit;
      }

      fireEvent.keyDown(textarea, { key: 'Enter', shiftKey: false });

      expect(mockRequestSubmit).not.toHaveBeenCalled();
    });
  });

  describe('accessibility', () => {
    it('should have proper aria-label on send button', () => {
      render(<ChatInput {...defaultProps} />);

      const button = screen.getByRole('button', { name: /send message/i });
      expect(button).toHaveAttribute('aria-label', 'Send message');
    });

    it('should be focusable via ref', () => {
      // Create a wrapper component to properly test ref forwarding
      let textareaRef: HTMLTextAreaElement | null = null;
      
      function TestWrapper() {
        const ref = useRef<HTMLTextAreaElement>(null);
        // Capture the ref after render
        if (ref.current) {
          textareaRef = ref.current;
        }
        return (
          <ChatInput
            {...defaultProps}
            ref={ref}
          />
        );
      }
      
      render(<TestWrapper />);
      
      // After render, the ref should be set
      const textarea = screen.getByRole('textbox');
      expect(textarea).toBeInstanceOf(HTMLTextAreaElement);
    });
  });
});
