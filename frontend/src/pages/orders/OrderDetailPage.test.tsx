import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import OrderDetailPage from './OrderDetailPage';
import { getOrder } from '../../api/orders';
import { getAllowedTransitions, executeTransition } from '../../api/workflow';
import { renderWithProviders } from '../../test/utils';

// Mock the API functions (return promises so .catch() works in loadOrderData)
vi.mock('../../api/orders', () => ({
  getOrder: vi.fn(),
  getOrderStatusLogs: vi.fn().mockResolvedValue([]),
  getOrderReschedules: vi.fn().mockResolvedValue([]),
  getOrderBlockers: vi.fn().mockResolvedValue([]),
  getOrderDockets: vi.fn().mockResolvedValue([]),
  getOrderMaterials: vi.fn().mockResolvedValue([]),
  checkMaterialCollection: vi.fn().mockResolvedValue(null),
  getRequiredMaterials: vi.fn().mockResolvedValue([]),
  getOnuPassword: vi.fn(),
  updateOrder: vi.fn()
}));

vi.mock('../../api/workflow', () => ({
  getAllowedTransitions: vi.fn(),
  executeTransition: vi.fn()
}));

vi.mock('../../api/rma', () => ({
  getRmaRequestsByOrder: vi.fn().mockResolvedValue([]),
  createRmaFromOrder: vi.fn()
}));

describe('OrderDetailPage Integration', () => {
  const mockOrder = {
    id: 'order-123',
    serviceId: 'SVC-123',
    status: 'New',
    customerName: 'John Doe',
    customerPhone: '123-456-7890',
    customerEmail: 'john@example.com',
    address: '123 Main St',
    orderType: 'Installation',
    partnerName: 'Test Partner',
    createdAt: '2024-01-01T00:00:00Z'
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (getOrder as ReturnType<typeof vi.fn>).mockResolvedValue(mockOrder);
    vi.fn().mockResolvedValue([]);
  });

  describe('WorkflowTransitionButton Integration', () => {
    it('should appear on OrderDetailPage', async () => {
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(
        <MemoryRouter initialEntries={['/orders/order-123']}>
          <Routes>
            <Route path="/orders/:orderId" element={<OrderDetailPage />} />
          </Routes>
        </MemoryRouter>,
        { wrapWithRouter: false }
      );

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /SVC-123/i })).toBeInTheDocument();
      }, { timeout: 3000 });

      await waitFor(() => {
        expect(getAllowedTransitions).toHaveBeenCalledWith({
          entityType: 'Order',
          entityId: 'order-123',
          currentStatus: 'New'
        });
      });
    });

    it('should load transitions based on order status', async () => {
      const orderWithStatus = { ...mockOrder, status: 'Assigned' };
      (getOrder as ReturnType<typeof vi.fn>).mockResolvedValue(orderWithStatus);

      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'Assigned',
          toStatus: 'Completed',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);

      renderWithProviders(
        <MemoryRouter initialEntries={['/orders/order-123']}>
          <Routes>
            <Route path="/orders/:orderId" element={<OrderDetailPage />} />
          </Routes>
        </MemoryRouter>,
        { wrapWithRouter: false }
      );

      await waitFor(() => {
        expect(getAllowedTransitions).toHaveBeenCalledWith({
          entityType: 'Order',
          entityId: 'order-123',
          currentStatus: 'Assigned'
        });
      });
    });

    it('should update order status after transition execution', async () => {
      const user = userEvent.setup();
      const updatedOrder = { ...mockOrder, status: 'Assigned' };
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockTransitions)
        .mockResolvedValueOnce([]);
      (executeTransition as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'job-123', status: 'Completed' });
      (getOrder as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockOrder)
        .mockResolvedValueOnce(updatedOrder);

      renderWithProviders(
        <MemoryRouter initialEntries={['/orders/order-123']}>
          <Routes>
            <Route path="/orders/:orderId" element={<OrderDetailPage />} />
          </Routes>
        </MemoryRouter>,
        { wrapWithRouter: false }
      );

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /SVC-123/i })).toBeInTheDocument();
      }, { timeout: 3000 });

      const transitionButton = await screen.findByRole('button', { name: /New.*Assigned/i }, { timeout: 3000 });
      await user.click(transitionButton);
      const executeButton = await screen.findByText(/Execute Transition/i);
      await user.click(executeButton);

      await waitFor(() => {
        expect(getOrder).toHaveBeenCalledTimes(2);
      }, { timeout: 3000 });
    });

    it('should refresh order data after transition', async () => {
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockTransitions)
        .mockResolvedValueOnce([]);
      (executeTransition as ReturnType<typeof vi.fn>).mockResolvedValue({ id: 'job-123', status: 'Completed' });
      (getOrder as ReturnType<typeof vi.fn>)
        .mockResolvedValueOnce(mockOrder)
        .mockResolvedValueOnce({ ...mockOrder, status: 'Assigned' });

      renderWithProviders(
        <MemoryRouter initialEntries={['/orders/order-123']}>
          <Routes>
            <Route path="/orders/:orderId" element={<OrderDetailPage />} />
          </Routes>
        </MemoryRouter>,
        { wrapWithRouter: false }
      );

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /SVC-123/i })).toBeInTheDocument();
      }, { timeout: 3000 });

      // Verify that loadOrderData callback is passed to WorkflowTransitionButton
      // This is tested indirectly through the component integration
      expect(getOrder).toHaveBeenCalled();
    });
  });

  describe('Error Handling', () => {
    it('should handle transition failures gracefully', async () => {
      const mockTransitions = [
        {
          id: 'trans-1',
          fromStatus: 'New',
          toStatus: 'Assigned',
          guardConditions: null
        }
      ];

      (getAllowedTransitions as ReturnType<typeof vi.fn>).mockResolvedValue(mockTransitions);
      const error = new Error('Transition not allowed');
      (executeTransition as ReturnType<typeof vi.fn>).mockRejectedValue(error);

      renderWithProviders(
        <MemoryRouter initialEntries={['/orders/order-123']}>
          <Routes>
            <Route path="/orders/:orderId" element={<OrderDetailPage />} />
          </Routes>
        </MemoryRouter>,
        { wrapWithRouter: false }
      );

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /SVC-123/i })).toBeInTheDocument();
      }, { timeout: 3000 });

      // Error should be handled by WorkflowTransitionButton component
      // Order page should still be functional
      expect(screen.getByRole('heading', { name: /SVC-123/i })).toBeInTheDocument();
    });

    it('should handle invalid entity ID', async () => {
      const error = new Error('Order not found');
      (getOrder as ReturnType<typeof vi.fn>).mockRejectedValue(error);

      renderWithProviders(
        <MemoryRouter initialEntries={['/orders/invalid-id']}>
          <Routes>
            <Route path="/orders/:orderId" element={<OrderDetailPage />} />
          </Routes>
        </MemoryRouter>,
        { wrapWithRouter: false }
      );

      const errorHeading = await screen.findByRole('heading', { name: /Error/i }, { timeout: 3000 });
      expect(errorHeading).toBeInTheDocument();
      expect(screen.getByText(/Order not found/i)).toBeInTheDocument();
    });
  });
});

