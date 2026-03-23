import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  executeTransition,
  getAllowedTransitions,
  canTransition,
  getWorkflowJob,
  getWorkflowJobs
} from './workflow';
import apiClient from './client';

// Mock the API client
vi.mock('./client', () => ({
  default: {
    post: vi.fn(),
    get: vi.fn()
  }
}));

describe('Workflow API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('executeTransition', () => {
    it('should execute a workflow transition successfully', async () => {
      const executeDto = {
        entityId: 'order-123',
        entityType: 'Order',
        targetStatus: 'Assigned',
        payload: { reason: 'Test transition', source: 'AdminPortal' }
      };
      const mockResponse = {
        id: 'job-123',
        entityId: 'order-123',
        status: 'Completed',
        result: { newStatus: 'Assigned' }
      };

      apiClient.post.mockResolvedValue(mockResponse);

      const result = await executeTransition(executeDto);

      expect(apiClient.post).toHaveBeenCalledWith('/workflow/execute', executeDto);
      expect(result).toEqual(mockResponse);
    });

    it('should handle API errors when executing transition', async () => {
      const executeDto = {
        entityId: 'order-123',
        entityType: 'Order',
        targetStatus: 'InvalidStatus',
        payload: {}
      };
      const error = new Error('Transition not allowed');
      error.status = 400;

      apiClient.post.mockRejectedValue(error);

      await expect(executeTransition(executeDto)).rejects.toThrow('Transition not allowed');
    });
  });

  describe('getAllowedTransitions', () => {
    it('should get allowed transitions for an entity', async () => {
      const params = {
        entityType: 'Order',
        entityId: 'order-123',
        currentStatus: 'New'
      };
      const mockResponse = [
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

      apiClient.get.mockResolvedValue(mockResponse);

      const result = await getAllowedTransitions(params);

      expect(apiClient.get).toHaveBeenCalledWith('/workflow/allowed-transitions', { params });
      expect(result).toEqual(mockResponse);
      expect(result).toHaveLength(2);
    });

    it('should return empty array when no transitions are allowed', async () => {
      const params = {
        entityType: 'Order',
        entityId: 'order-123',
        currentStatus: 'Completed'
      };

      apiClient.get.mockResolvedValue([]);

      const result = await getAllowedTransitions(params);

      expect(result).toEqual([]);
      expect(result).toHaveLength(0);
    });
  });

  describe('canTransition', () => {
    it('should check if a transition is allowed', async () => {
      const params = {
        entityType: 'Order',
        entityId: 'order-123',
        fromStatus: 'New',
        toStatus: 'Assigned'
      };

      apiClient.get.mockResolvedValue(true);

      const result = await canTransition(params);

      expect(apiClient.get).toHaveBeenCalledWith('/workflow/can-transition', { params });
      expect(result).toBe(true);
    });

    it('should return false when transition is not allowed', async () => {
      const params = {
        entityType: 'Order',
        entityId: 'order-123',
        fromStatus: 'Completed',
        toStatus: 'New'
      };

      apiClient.get.mockResolvedValue(false);

      const result = await canTransition(params);

      expect(result).toBe(false);
    });
  });

  describe('getWorkflowJob', () => {
    it('should get workflow job by ID', async () => {
      const jobId = 'job-123';
      const mockResponse = {
        id: 'job-123',
        entityId: 'order-123',
        entityType: 'Order',
        status: 'Completed',
        result: { newStatus: 'Assigned' },
        createdAt: '2024-01-01T00:00:00Z'
      };

      apiClient.get.mockResolvedValue(mockResponse);

      const result = await getWorkflowJob(jobId);

      expect(apiClient.get).toHaveBeenCalledWith(`/workflow/jobs/${jobId}`);
      expect(result).toEqual(mockResponse);
      expect(result.id).toBe(jobId);
    });
  });

  describe('getWorkflowJobs', () => {
    it('should get workflow jobs for an entity', async () => {
      const params = {
        entityType: 'Order',
        entityId: 'order-123',
        state: 'Completed'
      };
      const mockResponse = [
        {
          id: 'job-1',
          entityId: 'order-123',
          status: 'Completed',
          createdAt: '2024-01-01T00:00:00Z'
        },
        {
          id: 'job-2',
          entityId: 'order-123',
          status: 'Failed',
          createdAt: '2024-01-02T00:00:00Z'
        }
      ];

      apiClient.get.mockResolvedValue(mockResponse);

      const result = await getWorkflowJobs(params);

      expect(apiClient.get).toHaveBeenCalledWith('/workflow/jobs', { params });
      expect(result).toEqual(mockResponse);
      expect(result).toHaveLength(2);
    });

    it('should get all workflow jobs without filters', async () => {
      const mockResponse = [
        { id: 'job-1', entityId: 'order-123', status: 'Completed' },
        { id: 'job-2', entityId: 'order-456', status: 'Pending' }
      ];

      apiClient.get.mockResolvedValue(mockResponse);

      const result = await getWorkflowJobs({});

      expect(apiClient.get).toHaveBeenCalledWith('/workflow/jobs', { params: {} });
      expect(result).toEqual(mockResponse);
    });
  });
});
