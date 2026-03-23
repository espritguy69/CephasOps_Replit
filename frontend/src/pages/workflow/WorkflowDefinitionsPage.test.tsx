import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import WorkflowDefinitionsPage from './WorkflowDefinitionsPage';
import * as workflowDefinitionsAPI from '../../api/workflowDefinitions';
import { renderWithProviders } from '../../test/utils';

// Mock the API
vi.mock('../../api/workflowDefinitions', () => ({
  getWorkflowDefinitions: vi.fn(),
  getWorkflowDefinition: vi.fn(),
  createWorkflowDefinition: vi.fn(),
  updateWorkflowDefinition: vi.fn(),
  deleteWorkflowDefinition: vi.fn(),
  getTransitions: vi.fn(),
  addTransition: vi.fn(),
  updateTransition: vi.fn(),
  deleteTransition: vi.fn(),
  getEffectiveWorkflowDefinition: vi.fn()
}));

vi.mock('../../api/departments', () => ({
  getDepartments: vi.fn().mockResolvedValue([{ id: 'dept-1', name: 'Test Department' }])
}));

vi.mock('../../api/partners', () => ({
  getPartners: vi.fn().mockResolvedValue([{ id: 'partner-1', name: 'Test Partner' }])
}));

vi.mock('../../api/orderTypes', () => ({
  getOrderTypeParents: vi.fn().mockResolvedValue([{ code: 'OT1', name: 'Order Type 1' }])
}));

describe('WorkflowDefinitionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Page Loading', () => {
    it('should load workflow definitions list on mount', async () => {
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true,
          transitions: []
        }
      ];

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockDefinitions);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(workflowDefinitionsAPI.getWorkflowDefinitions).toHaveBeenCalled();
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });
    });

    it('should show loading state while fetching', () => {
      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockImplementation(
        () => new Promise(() => {})
      );

      renderWithProviders(<WorkflowDefinitionsPage />);

      expect(screen.getByText(/Loading workflow definitions/i)).toBeInTheDocument();
    });
  });

  describe('Filtering', () => {
    it('should filter by entity type', async () => {
      const user = userEvent.setup();
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true
        },
        {
          id: 'def-2',
          name: 'Invoice Workflow',
          entityType: 'Invoice',
          isActive: true
        }
      ];

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockDefinitions)
        .mockResolvedValueOnce([mockDefinitions[0]]);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });

      const filterSelects = screen.getAllByRole('combobox');
      const entityFilter = filterSelects.find((el) => el.getAttribute('aria-label')?.includes('Entity') || el.closest('[class*="flex"]')?.querySelector('label')?.textContent?.includes('Entity')) ?? filterSelects[0];
      await user.selectOptions(entityFilter, 'Order');

      await waitFor(() => {
        expect(workflowDefinitionsAPI.getWorkflowDefinitions).toHaveBeenCalledWith(
          expect.objectContaining({ entityType: 'Order' })
        );
      });
    });

    it('should filter by active status', async () => {
      const user = userEvent.setup();
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true
        }
      ];

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockDefinitions);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });

      const checkbox = screen.getByRole('checkbox', { name: /Active Only/i });
      await user.click(checkbox);

      await waitFor(() => {
        const calls = (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mock.calls;
        expect(calls.length).toBeGreaterThanOrEqual(2);
      }, { timeout: 3000 });
    });
  });

  describe('Definition Selection', () => {
    it('should show definition details when clicking definition', async () => {
      const user = userEvent.setup();
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          description: 'Test description',
          isActive: true
        }
      ];
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          isActive: true
        }
      ];

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockDefinitions);
      (workflowDefinitionsAPI.getTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });

      const definitionCard = screen.getByRole('heading', { name: /Order Workflow/i }).closest('[class*="rounded-lg"]') ||
                            screen.getByRole('heading', { name: /Order Workflow/i });
      await user.click(definitionCard as HTMLElement);

      await waitFor(() => {
        expect(workflowDefinitionsAPI.getTransitions).toHaveBeenCalledWith('def-1');
      });
      expect(screen.getAllByText(/Test description/i).length).toBeGreaterThan(0);
    });
  });

  describe('Creating Workflow Definition', () => {
    it('should create new workflow definition', async () => {
      const user = userEvent.setup();
      const mockDefinitions: any[] = [];
      const newDefinition = {
        id: 'def-new',
        name: 'New Workflow',
        entityType: 'Order',
        isActive: true
      };

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockDefinitions)
        .mockResolvedValueOnce([newDefinition]);
      (workflowDefinitionsAPI.createWorkflowDefinition as ReturnType<typeof vi.fn>).mockResolvedValue(newDefinition);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        const createButtons = screen.getAllByRole('button', { name: /Create Workflow/i });
        expect(createButtons.length).toBeGreaterThan(0);
      });

      const createButtons = screen.getAllByRole('button', { name: /Create Workflow/i });
      await user.click(createButtons[createButtons.length - 1]);

      await waitFor(() => {
        expect(screen.getByText(/Create Workflow Definition/i)).toBeInTheDocument();
      });

      const modal = screen.getByRole('dialog');
      const nameInput = within(modal).getByLabelText(/Workflow Name/i) ||
                       within(modal).getByPlaceholderText(/e.g., ISP Order Workflow/i);
      await user.type(nameInput, 'New Workflow');
      const departmentSelect = within(modal).getByLabelText(/Department.*optional/i);
      await user.selectOptions(departmentSelect, ['dept-1']);

      const entitySelect = within(modal).getByLabelText(/Entity Type/i);
      await user.selectOptions(entitySelect, ['Order']);

      const submitButton = within(modal).getByRole('button', { name: /^Create$/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(workflowDefinitionsAPI.createWorkflowDefinition).toHaveBeenCalled();
      });
    });

    it('should validate required fields', async () => {
      const user = userEvent.setup();
      const mockDefinitions: any[] = [];

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockDefinitions);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        const createButtons = screen.getAllByRole('button', { name: /Create Workflow/i });
        expect(createButtons.length).toBeGreaterThan(0);
      });

      const createButtons = screen.getAllByRole('button', { name: /Create Workflow/i });
      await user.click(createButtons[createButtons.length - 1]);

      await waitFor(() => {
        expect(screen.getByText(/Create Workflow Definition/i)).toBeInTheDocument();
      });

      const modal = screen.getByRole('dialog');
      const submitButton = within(modal).getByRole('button', { name: /^Create$/ });
      await user.click(submitButton);

      // Form validation should prevent submission
      await waitFor(() => {
        expect(workflowDefinitionsAPI.createWorkflowDefinition).not.toHaveBeenCalled();
      });
    });

    it('should omit empty optional scope fields (partner, department, order type) from create payload', async () => {
      const user = userEvent.setup();
      const mockDefinitions: any[] = [];
      const newDefinition = {
        id: 'def-new',
        name: 'General Workflow',
        entityType: 'Order',
        isActive: true
      };

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockDefinitions)
        .mockResolvedValueOnce([newDefinition]);
      (workflowDefinitionsAPI.createWorkflowDefinition as ReturnType<typeof vi.fn>).mockResolvedValue(newDefinition);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        const createButtons = screen.getAllByRole('button', { name: /Create Workflow/i });
        expect(createButtons.length).toBeGreaterThan(0);
      });

      const createButtons = screen.getAllByRole('button', { name: /Create Workflow/i });
      await user.click(createButtons[createButtons.length - 1]);

      await waitFor(() => {
        expect(screen.getByText(/Create Workflow Definition/i)).toBeInTheDocument();
      });

      const modal = screen.getByRole('dialog');
      const nameInput = within(modal).getByLabelText(/Workflow Name/i) ||
                       within(modal).getByPlaceholderText(/e.g., ISP Order Workflow/i);
      await user.type(nameInput, 'General Workflow');
      const entitySelect = within(modal).getByLabelText(/Entity Type/i);
      await user.selectOptions(entitySelect, ['Order']);

      const submitButton = within(modal).getByRole('button', { name: /^Create$/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(workflowDefinitionsAPI.createWorkflowDefinition).toHaveBeenCalledTimes(1);
      });
      const payload = (workflowDefinitionsAPI.createWorkflowDefinition as ReturnType<typeof vi.fn>).mock.calls[0][0];
      expect(payload).toHaveProperty('name', 'General Workflow');
      expect(payload).toHaveProperty('entityType', 'Order');
      expect(payload).not.toHaveProperty('partnerId');
      expect(payload).not.toHaveProperty('departmentId');
      expect(payload).not.toHaveProperty('orderTypeCode');
    });
  });

  describe('Deleting Workflow Definition', () => {
    it('should delete workflow definition with confirmation', async () => {
      const user = userEvent.setup();
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true
        }
      ];

      // Mock window.confirm
      window.confirm = vi.fn(() => true) as any;

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockDefinitions)
        .mockResolvedValueOnce([]);
      (workflowDefinitionsAPI.deleteWorkflowDefinition as ReturnType<typeof vi.fn>).mockResolvedValue(null);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });

      const card = screen.getByRole('heading', { name: /Order Workflow/i }).closest('[class*="rounded-lg"]');
      const deleteButton = card ? within(card as HTMLElement).getByRole('button', { name: /Delete/i }) : screen.getByRole('button', { name: /Delete/i });
      await user.click(deleteButton);

      await waitFor(() => {
        expect(window.confirm).toHaveBeenCalled();
        expect(workflowDefinitionsAPI.deleteWorkflowDefinition).toHaveBeenCalledWith('def-1');
      });
    });
  });

  describe('Transitions Management', () => {
    it('should view transitions list', async () => {
      const user = userEvent.setup();
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true
        }
      ];
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          isActive: true
        }
      ];

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockDefinitions);
      (workflowDefinitionsAPI.getTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });

      const definitionCard = screen.getByRole('heading', { name: /Order Workflow/i }).closest('[class*="rounded-lg"]') ||
                            screen.getByRole('heading', { name: /Order Workflow/i });
      await user.click(definitionCard as HTMLElement);

      await waitFor(() => {
        expect(workflowDefinitionsAPI.getTransitions).toHaveBeenCalledWith('def-1');
        expect(screen.getByText(/Assigned/i)).toBeInTheDocument();
      });
    });

    it('should add new transition', async () => {
      const user = userEvent.setup();
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true
        }
      ];
      const mockTransitions: any[] = [];
      const newTransition = {
        id: 'trans-new',
        fromStatus: 'New',
        toStatus: 'Assigned',
        isActive: true
      };

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockDefinitions);
      (workflowDefinitionsAPI.getTransitions as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockTransitions)
        .mockResolvedValueOnce([newTransition]);
      (workflowDefinitionsAPI.addTransition as ReturnType<typeof vi.fn>).mockResolvedValue(newTransition);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });

      const definitionCard = screen.getByRole('heading', { name: /Order Workflow/i }).closest('[class*="rounded-lg"]') ||
                            screen.getByRole('heading', { name: /Order Workflow/i });
      await user.click(definitionCard as HTMLElement);

      await waitFor(() => {
        expect(workflowDefinitionsAPI.getTransitions).toHaveBeenCalledWith('def-1');
        expect(screen.getAllByRole('button', { name: /Add Transition/i }).length).toBeGreaterThan(0);
      });

      const addButtons = screen.getAllByRole('button', { name: /Add Transition/i });
      const addButton = addButtons[addButtons.length - 1];
      await user.click(addButton);

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument();
        expect(screen.getByLabelText(/To Status/i)).toBeInTheDocument();
      });

      const toStatusInput = screen.getByLabelText(/To Status/i) ||
                           screen.getByPlaceholderText(/e.g., Assigned, OrderCompleted/i);
      await user.type(toStatusInput, 'Assigned');

      const transitionModal = screen.getByRole('dialog');
      const submitButton = within(transitionModal).getByRole('button', { name: /Add.*Transition/i });
      await user.click(submitButton);

      await waitFor(() => {
        expect(workflowDefinitionsAPI.addTransition).toHaveBeenCalled();
      });
    });

    it('should edit existing transition', async () => {
      const user = userEvent.setup();
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true
        }
      ];
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          isActive: true
        }
      ];

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockDefinitions);
      (workflowDefinitionsAPI.getTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);
      (workflowDefinitionsAPI.updateTransition as ReturnType<typeof vi.fn>).mockResolvedValue({
        ...mockTransitions[0],
        toStatus: 'Updated'
      });

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });

      const definitionCard = screen.getByRole('heading', { name: /Order Workflow/i }).closest('[class*="rounded-lg"]') ||
                            screen.getByRole('heading', { name: /Order Workflow/i });
      await user.click(definitionCard as HTMLElement);

      await waitFor(() => {
        expect(screen.getByText(/Assigned/i)).toBeInTheDocument();
      });

      const transitionRow = screen.getByText(/Assigned/i).closest('.border');
      const rowButtons = transitionRow ? within(transitionRow).getAllByRole('button') : [];
      const editTransitionButton = rowButtons[0];
      await user.click(editTransitionButton);

      await waitFor(() => {
        expect(screen.getByText(/Edit Transition/i)).toBeInTheDocument();
      });

      const toStatusInput = screen.getByLabelText(/To Status/i);
      await user.clear(toStatusInput);
      await user.type(toStatusInput, 'Updated');

      const submitButton = screen.getByText(/Update.*Transition/i);
      await user.click(submitButton);

      await waitFor(() => {
        expect(workflowDefinitionsAPI.updateTransition).toHaveBeenCalled();
      });
    });

    it('should delete transition with confirmation', async () => {
      const user = userEvent.setup();
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true
        }
      ];
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          isActive: true
        }
      ];

      window.confirm = vi.fn(() => true) as any;

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockDefinitions);
      (workflowDefinitionsAPI.getTransitions as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockTransitions)
        .mockResolvedValueOnce([]);
      (workflowDefinitionsAPI.deleteTransition as ReturnType<typeof vi.fn>).mockResolvedValue(null);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });

      const definitionCard = screen.getByRole('heading', { name: /Order Workflow/i }).closest('[class*="rounded-lg"]') ||
                            screen.getByRole('heading', { name: /Order Workflow/i });
      await user.click(definitionCard as HTMLElement);

      await waitFor(() => {
        expect(screen.getByText(/Assigned/i)).toBeInTheDocument();
      });

      const transitionRow = screen.getByText(/Assigned/i).closest('.border');
      const rowButtons = transitionRow ? within(transitionRow).getAllByRole('button') : [];
      const transitionDeleteButton = rowButtons.length >= 2 ? rowButtons[1] : screen.getAllByText(/Delete/i)[0];

      await user.click(transitionDeleteButton);

      await waitFor(() => {
        expect(window.confirm).toHaveBeenCalled();
        expect(workflowDefinitionsAPI.deleteTransition).toHaveBeenCalledWith('trans-1');
      });
    });
  });

  describe('Effective workflow preview', () => {
    it('should send only non-empty trimmed params to getEffectiveWorkflowDefinition', async () => {
      const user = userEvent.setup();
      const mockDefinitions = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true
        }
      ];
      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockDefinitions);
      (workflowDefinitionsAPI.getEffectiveWorkflowDefinition as ReturnType<typeof vi.fn>).mockResolvedValue({
        id: 'def-1',
        name: 'Order Workflow',
        entityType: 'Order',
        isActive: true
      });

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /Order Workflow/i })).toBeInTheDocument();
      });

      const previewButton = screen.getByRole('button', { name: /Preview/i });
      await user.click(previewButton);

      await waitFor(() => {
        expect(workflowDefinitionsAPI.getEffectiveWorkflowDefinition).toHaveBeenCalled();
      });
      const params = (workflowDefinitionsAPI.getEffectiveWorkflowDefinition as ReturnType<typeof vi.fn>).mock.calls[0][0];
      expect(params).toHaveProperty('entityType');
      expect(params).not.toHaveProperty('partnerId');
      expect(params).not.toHaveProperty('departmentId');
      expect(params).not.toHaveProperty('orderTypeCode');
    });
  });

  describe('Error Handling', () => {
    it('should display error messages correctly', async () => {
      const error = new Error('Failed to load definitions');
      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>).mockRejectedValue(error);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        expect(screen.getByText(/Failed to load definitions/i)).toBeInTheDocument();
      });
    });

    it('should display success notifications', async () => {
      const user = userEvent.setup();
      const mockDefinitions: any[] = [];
      const newDefinition = {
        id: 'def-new',
        name: 'New Workflow',
        entityType: 'Order',
        isActive: true
      };

      (workflowDefinitionsAPI.getWorkflowDefinitions as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockDefinitions)
        .mockResolvedValueOnce([newDefinition]);
      (workflowDefinitionsAPI.createWorkflowDefinition as ReturnType<typeof vi.fn>).mockResolvedValue(newDefinition);

      renderWithProviders(<WorkflowDefinitionsPage />);

      await waitFor(() => {
        const createButtons = screen.getAllByRole('button', { name: /Create Workflow/i });
        expect(createButtons.length).toBeGreaterThan(0);
      });

      const createButtons = screen.getAllByRole('button', { name: /Create Workflow/i });
      await user.click(createButtons[createButtons.length - 1]);

      await waitFor(() => {
        expect(screen.getByLabelText(/Workflow Name/i)).toBeInTheDocument();
      });

      const modal = screen.getByRole('dialog');
      const nameInput = within(modal).getByLabelText(/Workflow Name/i) ||
                       within(modal).getByPlaceholderText(/e.g., ISP Order Workflow/i);
      await user.type(nameInput, 'New Workflow');

      const departmentSelect = within(modal).getByLabelText(/Department.*optional/i);
      await user.selectOptions(departmentSelect, 'dept-1');

      const submitButton = within(modal).getByRole('button', { name: /^Create$/ });
      await user.click(submitButton);

      await waitFor(() => {
        expect(screen.getByText(/Workflow definition created successfully/i)).toBeInTheDocument();
      }, { timeout: 3000 });
    });
  });
});

