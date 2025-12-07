import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { LoadingIndicator } from '@/components/shared/LoadingIndicator';

describe('LoadingIndicator', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  describe('rendering', () => {
    it('should render the loading indicator with assistant label', () => {
      render(<LoadingIndicator />);

      expect(screen.getByText('🤖')).toBeInTheDocument();
      expect(screen.getByText('Task Assistant')).toBeInTheDocument();
    });

    it('should render animated bounce dots', () => {
      render(<LoadingIndicator />);

      // There should be 3 bouncing dots
      const dots = document.querySelectorAll('.animate-bounce');
      expect(dots).toHaveLength(3);
    });
  });

  describe('server status message', () => {
    it('should display server status when provided', () => {
      render(<LoadingIndicator serverStatus="Creating task..." />);

      expect(screen.getByText('Creating task...')).toBeInTheDocument();
    });

    it('should prioritize server status over context message', () => {
      render(
        <LoadingIndicator 
          serverStatus="Searching tasks..." 
          contextMessage="This should not appear" 
        />
      );

      expect(screen.getByText('Searching tasks...')).toBeInTheDocument();
      expect(screen.queryByText('This should not appear')).not.toBeInTheDocument();
    });

    it('should not rotate messages when server status is provided', async () => {
      render(<LoadingIndicator serverStatus="Processing your request..." />);

      // Wait for rotation interval
      act(() => {
        vi.advanceTimersByTime(5000);
      });

      // Should still show server status
      expect(screen.getByText('Processing your request...')).toBeInTheDocument();
    });

    it('should display different server statuses correctly', () => {
      const { rerender } = render(<LoadingIndicator serverStatus="Creating task..." />);
      expect(screen.getByText('Creating task...')).toBeInTheDocument();

      rerender(<LoadingIndicator serverStatus="Searching tasks..." />);
      expect(screen.getByText('Searching tasks...')).toBeInTheDocument();

      rerender(<LoadingIndicator serverStatus="Updating task..." />);
      expect(screen.getByText('Updating task...')).toBeInTheDocument();
    });
  });

  describe('context message', () => {
    it('should display context message when no server status', () => {
      render(<LoadingIndicator contextMessage="Custom loading message" />);

      expect(screen.getByText('Custom loading message')).toBeInTheDocument();
    });

    it('should not rotate messages when context message is provided', async () => {
      render(<LoadingIndicator contextMessage="Static message" />);

      // Wait for rotation interval
      act(() => {
        vi.advanceTimersByTime(5000);
      });

      // Should still show context message
      expect(screen.getByText('Static message')).toBeInTheDocument();
    });
  });

  describe('default rotation', () => {
    it('should show default message when no props provided', () => {
      render(<LoadingIndicator />);

      // Should show one of the default messages
      expect(screen.getByText('🤔 Analyzing your request...')).toBeInTheDocument();
    });

    it('should rotate through default messages', async () => {
      render(<LoadingIndicator />);

      // Initial message
      expect(screen.getByText('🤔 Analyzing your request...')).toBeInTheDocument();

      // Advance timer to trigger rotation
      act(() => {
        vi.advanceTimersByTime(2000);
      });

      // Second message
      expect(screen.getByText('🔍 Processing task information...')).toBeInTheDocument();

      // Advance timer again
      act(() => {
        vi.advanceTimersByTime(2000);
      });

      // Third message
      expect(screen.getByText('⚡ Generating response...')).toBeInTheDocument();
    });
  });

  describe('transitions', () => {
    it('should switch from server status to default when server status becomes null', () => {
      const { rerender } = render(<LoadingIndicator serverStatus="Creating task..." />);
      expect(screen.getByText('Creating task...')).toBeInTheDocument();

      // Server status cleared
      rerender(<LoadingIndicator serverStatus={null} />);
      
      // Should now show default message
      expect(screen.getByText('🤔 Analyzing your request...')).toBeInTheDocument();
    });
  });
});
