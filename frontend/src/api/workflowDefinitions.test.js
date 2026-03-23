import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  getWorkflowDefinitions,
  getWorkflowDefinition,
  createWorkflowDefinition,
  updateWorkflowDefinition,
  deleteWorkflowDefinition,
  getTransitions,
  addTransition,
  updateTransition,
  deleteTransition
} from './workflowDefinitions';
import apiClient from './client';

// Mock the API client
vi.mock('./client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn()
  }
}));

describe('Workflow Definitions API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getWorkflowDefinitions', () => {
    it('should get all workflow definitions', async () => {
      const mockResponse = [
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

      apiClient.get.mockResolvedValue(mockResponse);

      const result = await getWorkflowDefinitions();

      expect(apiClient.get).toHaveBeenCalledWith('/workflow-definitions', { params: {} });
      expect(result).toEqual(mockResponse);
      expect(result).toHaveLength(2);
    });

    it('should get workflow definitions with filters', async () => {
      const filters = { entityType: 'Order', isActive: true };
      const mockResponse = [
        {
          id: 'def-1',
          name: 'Order Workflow',
          entityType: 'Order',
          isActive: true
        }
      ];

      apiClient.get.mockResolvedValue(mockResponse);

      const result = await getWorkflowDefinitions(filters);

      expect(apiClient.get).toHaveBeenCalledWith('/workflow-definitions', { params: filters });
      expect(result).toEqual(mockResponse);
    });
  });

  describe('getWorkflowDefinition', () => {
    it('should get workflow definition by ID', async () => {
      const definitionId = 'def-123';
      const mockResponse = {
        id: 'def-123',
        name: 'Order Workflow',
        entityType: 'Order',
        description: 'Test workflow',
        isActive: true
      };

      apiClient.get.mockResolvedValue(mockResponse);

      const result = await getWorkflowDefinition(definitionId);

      expect(apiClient.get).toHaveBeenCalledWith(`/workflow-definitions/${definitionId}`);
      expect(result).toEqual(mockResponse);
      expect(result.id).toBe(definitionId);
    });
  });

  describe('createWorkflowDefinition', () => {
    it('should create a new workflow definition', async () => {
      const definitionData = {
        name: 'New Workflow',
        entityType: 'Order',
        description: 'Test workflow',
        isActive: true
      };
      const mockResponse = {
        id: 'def-new',
        ...definitionData
      };

      apiClient.post.mockResolvedValue(mockResponse);

      const result = await createWorkflowDefinition(definitionData);

      expect(apiClient.post).toHaveBeenCalledWith('/workflow-definitions', definitionData);
      expect(result).toEqual(mockResponse);
      expect(result.id).toBeDefined();
    });
  });

  describe('updateWorkflowDefinition', () => {
    it('should update an existing workflow definition', async () => {
      const definitionId = 'def-123';
      const updateData = {
        name: 'Updated Workflow',
        description: 'Updated description'
      };
      const mockResponse = {
        id: definitionId,
        ...updateData
      };

      apiClient.put.mockResolvedValue(mockResponse);

      const result = await updateWorkflowDefinition(definitionId, updateData);

      expect(apiClient.put).toHaveBeenCalledWith(
        `/workflow-definitions/${definitionId}`,
        updateData
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('deleteWorkflowDefinition', () => {
    it('should delete a workflow definition', async () => {
      const definitionId = 'def-123';

      apiClient.delete.mockResolvedValue(null);

      await deleteWorkflowDefinition(definitionId);

      expect(apiClient.delete).toHaveBeenCalledWith(`/workflow-definitions/${definitionId}`);
    });
  });

  describe('getTransitions', () => {
    it('should get transitions for a workflow definition', async () => {
      const definitionId = 'def-123';
      const mockResponse = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          isActive: true
        },
        {
          id: 'trans-2',
          fromStatus: 'Assigned',
          toStatus: 'Completed',
          isActive: true
        }
      ];

      apiClient.get.mockResolvedValue(mockResponse);

      const result = await getTransitions(definitionId);

      expect(apiClient.get).toHaveBeenCalledWith(`/workflow-definitions/${definitionId}/transitions`);
      expect(result).toEqual(mockResponse);
      expect(result).toHaveLength(2);
    });
  });

  describe('addTransition', () => {
    it('should add a transition to a workflow definition', async () => {
      const definitionId = 'def-123';
      const transitionData = {
        fromStatus: 'New',
        toStatus: 'Assigned',
        allowedRoles: ['Admin'],
        isActive: true
      };
      const mockResponse = {
        id: 'trans-new',
        ...transitionData
      };

      apiClient.post.mockResolvedValue(mockResponse);

      const result = await addTransition(definitionId, transitionData);

      expect(apiClient.post).toHaveBeenCalledWith(
        `/workflow-definitions/${definitionId}/transitions`,
        transitionData
      );
      expect(result).toEqual(mockResponse);
      expect(result.id).toBeDefined();
    });
  });

  describe('updateTransition', () => {
    it('should update a workflow transition', async () => {
      const transitionId = 'trans-123';
      const updateData = {
        toStatus: 'UpdatedStatus',
        allowedRoles: ['Admin', 'SI']
      };
      const mockResponse = {
        id: transitionId,
        ...updateData
      };

      apiClient.put.mockResolvedValue(mockResponse);

      const result = await updateTransition(transitionId, updateData);

      expect(apiClient.put).toHaveBeenCalledWith(
        `/workflow-definitions/transitions/${transitionId}`,
        updateData
      );
      expect(result).toEqual(mockResponse);
    });
  });

  describe('deleteTransition', () => {
    it('should delete a workflow transition', async () => {
      const transitionId = 'trans-123';

      apiClient.delete.mockResolvedValue(null);

      await deleteTransition(transitionId);

      expect(apiClient.delete).toHaveBeenCalledWith(`/workflow-definitions/transitions/${transitionId}`);
    });
  });
});
