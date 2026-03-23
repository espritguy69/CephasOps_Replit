import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import WorkflowTransitionButton from './WorkflowTransitionButton';
import { getAllowedTransitions, executeTransition } from '../../api/workflow';
import { renderWithProviders } from '../../test/utils';

// Mock the API functions
vi.mock('../../api/workflow', () => ({
  getAllowedTransitions: vi.fn(),
  executeTransition: vi.fn()
}));

describe('WorkflowTransitionButton', () => {
  const defaultProps = {
    entityType: 'Order',
    entityId: 'order-123',
    currentStatus: 'New',
    onTransitionExecuted: vi.fn()
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render correctly with allowed transitions', async () => {
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });
    });

    it('should not render when no transitions are available', async () => {
      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue([]);

      renderWithProviders(
        <WorkflowTransitionButton {...defaultProps} />
      );

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /New.*Assigned/i })).not.toBeInTheDocument();
      });
    });

    it('should show loading state while fetching transitions', () => {
      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockImplementation(() => new Promise(() => {}));

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      expect(screen.getByText(/Loading transitions/i)).toBeInTheDocument();
    });

    it('should display transition buttons for allowed transitions', async () => {
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        },
        {
          id: 'trans-2',
          fromStatus: 'New',
          toStatus: 'Cancelled',
          guardConditions: { requiresApproval: true }
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /New.*Cancelled/i })).toBeInTheDocument();
      });
    });

    it('should show guard condition indicator when conditions exist', async () => {
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: { requiresApproval: true }
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        const button = screen.getByRole('button', { name: /New.*Assigned/i });
        expect(button).toHaveAttribute('title', 'This transition has guard conditions that must be met');
      });
    });
  });

  describe('Modal Interaction', () => {
    it('should open modal when clicking transition button', async () => {
      const user = userEvent.setup();
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });

      const button = screen.getByRole('button', { name: /New.*Assigned/i });
      if (button) {
        await user.click(button);
      }

      expect(screen.getByText(/Execute Status Transition/i)).toBeInTheDocument();
      expect(screen.getByText(/From:/i)).toBeInTheDocument();
      expect(screen.getByText(/To:/i)).toBeInTheDocument();
    });

    it('should allow input of reason/notes in modal', async () => {
      const user = userEvent.setup();
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });

      const button = screen.getByRole('button', { name: /New.*Assigned/i });
      if (button) {
        await user.click(button);
      }

      const textarea = screen.getByPlaceholderText(/Enter reason for this status change/i);
      await user.type(textarea, 'Test reason for transition');

      expect(textarea).toHaveValue('Test reason for transition');
    });

    it('should close modal when clicking cancel', async () => {
      const user = userEvent.setup();
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });

      const button = screen.getByRole('button', { name: /New.*Assigned/i });
      if (button) {
        await user.click(button);
      }

      expect(screen.getByText(/Execute Status Transition/i)).toBeInTheDocument();

      const cancelButton = screen.getByText(/Cancel/i);
      await user.click(cancelButton);

      await waitFor(() => {
        expect(screen.queryByText(/Execute Status Transition/i)).not.toBeInTheDocument();
      });
    });
  });

  describe('Transition Execution', () => {
    it('should execute transition on submit', async () => {
      const user = userEvent.setup();
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);
      (executeTransition as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'job-123', status: 'Completed' });
      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValueOnce(mockTransitions).mockResolvedValueOnce([]);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });

      const button = screen.getByRole('button', { name: /New.*Assigned/i });
      if (button) {
        await user.click(button);
      }

      const executeButton = screen.getByText(/Execute Transition/i);
      await user.click(executeButton);

      await waitFor(() => {
        expect(executeTransition).toHaveBeenCalledWith({
          entityId: 'order-123',
          entityType: 'Order',
          targetStatus: 'Assigned',
          payload: {
            reason: undefined,
            source: 'AdminPortal',
            userId: undefined
          }
        });
      });
    });

    it('should include reason in payload when provided', async () => {
      const user = userEvent.setup();
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);
      (executeTransition as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'job-123', status: 'Completed' });
      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValueOnce(mockTransitions).mockResolvedValueOnce([]);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });

      const button = screen.getByRole('button', { name: /New.*Assigned/i });
      if (button) {
        await user.click(button);
      }

      const textarea = screen.getByPlaceholderText(/Enter reason for this status change/i);
      await user.type(textarea, 'Test reason');

      const executeButton = screen.getByText(/Execute Transition/i);
      await user.click(executeButton);

      await waitFor(() => {
        expect(executeTransition).toHaveBeenCalledWith(
          expect.objectContaining({
            payload: expect.objectContaining({
              reason: 'Test reason'
            })
          })
        );
      });
    });

    it('should call onTransitionExecuted callback after successful execution', async () => {
      const user = userEvent.setup();
      const onTransitionExecuted = vi.fn();
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);
      (executeTransition as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'job-123', status: 'Completed' });
      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValueOnce(mockTransitions).mockResolvedValueOnce([]);

      renderWithProviders(
        <WorkflowTransitionButton
          {...defaultProps}
          onTransitionExecuted={onTransitionExecuted}
        />
      );

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });

      const button = screen.getByRole('button', { name: /New.*Assigned/i });
      if (button) {
        await user.click(button);
      }

      const executeButton = screen.getByText(/Execute Transition/i);
      await user.click(executeButton);

      await waitFor(() => {
        expect(onTransitionExecuted).toHaveBeenCalled();
      });
    });

    it('should display error on failure', async () => {
      const user = userEvent.setup();
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);
      const error = new Error('Transition failed');
      (executeTransition as ReturnType<typeof vi.fn>).mockRejectedValue(error);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });

      const button = screen.getByRole('button', { name: /New.*Assigned/i });
      if (button) {
        await user.click(button);
      }

      const executeButton = screen.getByText(/Execute Transition/i);
      await user.click(executeButton);

      await waitFor(() => {
        expect(screen.getAllByText(/Transition failed/i).length).toBeGreaterThan(0);
      });
    });

    it('should show loading state during execution', async () => {
      const user = userEvent.setup();
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);
      (executeTransition as ReturnType<typeof vi.fn>).mockImplementation(() => new Promise(() => {}));

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });

      const button = screen.getByRole('button', { name: /New.*Assigned/i });
      if (button) {
        await user.click(button);
      }

      const executeButton = screen.getByText(/Execute Transition/i);
      await user.click(executeButton);

      expect(screen.getByText(/Executing.../i)).toBeInTheDocument();
      expect(executeButton).toBeDisabled();
    });

    it('should disable buttons during execution', async () => {
      const user = userEvent.setup();
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);
      (executeTransition as ReturnType<typeof vi.fn>).mockImplementation(() => new Promise(() => {}));

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /New.*Assigned/i })).toBeInTheDocument();
      });

      const button = screen.getByRole('button', { name: /New.*Assigned/i });
      if (button) {
        await user.click(button);
      }

      const executeButton = screen.getByText(/Execute Transition/i);
      await user.click(executeButton);

      const cancelButton = screen.getByText(/Cancel/i);
      expect(cancelButton).toBeDisabled();
    });
  });

  describe('Error Handling', () => {
    it('should display error message when loading transitions fails', async () => {
      const error = new Error('Failed to load transitions');
      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockRejectedValue(error);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText(/Failed to load transitions/i)).toBeInTheDocument();
      });
    });

    it('should allow dismissing error message', async () => {
      const user = userEvent.setup();
      const error = new Error('Failed to load transitions');
      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockRejectedValue(error);

      renderWithProviders(<WorkflowTransitionButton {...defaultProps} />);

      await waitFor(() => {
        expect(screen.getByText(/Failed to load transitions/i)).toBeInTheDocument();
      });

      const closeButton = screen.getByRole('button', { name: /close/i });
      await user.click(closeButton);

      await waitFor(() => {
        expect(screen.queryByText(/Failed to load transitions/i)).not.toBeInTheDocument();
      });
    });
  });
});

