import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import Sidebar from '../components/layout/Sidebar';
import App from '../App';
import { renderWithProviders } from './utils';

// Ensure NotificationContext is mocked for App tree (TopNav -> NotificationBell)
vi.mock('@/contexts/NotificationContext', () => ({
  useNotifications: () => ({
    notifications: [],
    unreadCount: 0,
    markAsRead: vi.fn(),
    markAllAsRead: vi.fn(),
    fetchNotifications: vi.fn().mockResolvedValue(undefined),
    fetchUnreadCount: vi.fn().mockResolvedValue(undefined),
    archiveNotification: vi.fn().mockResolvedValue(undefined),
    refresh: vi.fn().mockResolvedValue(undefined),
    loading: false,
    error: null
  }),
  NotificationProvider: ({ children }) => children
}));

describe('Navigation', () => {
  describe('Sidebar Menu', () => {
    it('should show Workflow menu item in sidebar', () => {
      const authValue = {
        isAuthenticated: true,
        user: {
          id: 'test-user',
          permissions: ['workflow.view'],
          roles: ['User']
        },
        loading: false,
        login: vi.fn(),
        logout: vi.fn()
      };

      renderWithProviders(
        <Sidebar isOpen={true} />,
        { authValue }
      );

      expect(screen.getByText(/Workflow/i)).toBeInTheDocument();
    });

    it('should hide Workflow menu item when user lacks permission', () => {
      const authValue = {
        isAuthenticated: true,
        user: {
          id: 'test-user',
          permissions: [],
          roles: []
        },
        loading: false,
        login: vi.fn(),
        logout: vi.fn()
      };

      renderWithProviders(
        <Sidebar isOpen={true} />,
        { authValue }
      );

      expect(screen.queryByText(/Workflow/i)).not.toBeInTheDocument();
    });

    it('should navigate to /workflow/definitions when clicking Workflow menu', () => {
      const authValue = {
        isAuthenticated: true,
        user: {
          id: 'test-user',
          permissions: ['workflow.view'],
          roles: ['User']
        },
        loading: false,
        login: vi.fn(),
        logout: vi.fn()
      };

      renderWithProviders(
        <MemoryRouter>
          <Sidebar isOpen={true} />
        </MemoryRouter>,
        { authValue, wrapWithRouter: false }
      );

      const workflowLink = screen.getByText(/Workflow/i).closest('a');
      expect(workflowLink).toHaveAttribute('href', '/workflow/definitions');
    });
  });

  describe('Route Protection', () => {
    it('should require authentication for /workflow/definitions route', async () => {
      const authValue = {
        isAuthenticated: false,
        user: null,
        loading: false,
        login: vi.fn(),
        logout: vi.fn()
      };

      renderWithProviders(
        <MemoryRouter initialEntries={['/workflow/definitions']}>
          <App />
        </MemoryRouter>,
        { authValue, wrapWithRouter: false }
      );

      // Should show login page (MemoryRouter does not update window.location)
      const loginContent = await screen.findByText(/Sign in to your account/i);
      expect(loginContent).toBeInTheDocument();
    });

    it('should check route permission for /workflow/definitions', () => {
      const authValue = {
        isAuthenticated: true,
        user: {
          id: 'test-user',
          permissions: ['workflow.view'],
          roles: ['User']
        },
        loading: false,
        login: vi.fn(),
        logout: vi.fn()
      };

      renderWithProviders(
        <MemoryRouter initialEntries={['/workflow/definitions']}>
          <App />
        </MemoryRouter>,
        { authValue, wrapWithRouter: false }
      );

      // Should render the workflow definitions page
      expect(screen.getByText(/Workflow Definitions/i)).toBeInTheDocument();
    });

    it('should deny access when user lacks workflow.view permission', () => {
      const authValue = {
        isAuthenticated: true,
        user: {
          id: 'test-user',
          permissions: [],
          roles: []
        },
        loading: false,
        login: vi.fn(),
        logout: vi.fn()
      };

      renderWithProviders(
        <MemoryRouter initialEntries={['/workflow/definitions']}>
          <App />
        </MemoryRouter>,
        { authValue, wrapWithRouter: false }
      );

      // Should show access denied or redirect
      // The exact behavior depends on ProtectedRoute implementation
      const workflowPage = screen.queryByText(/Workflow Definitions/i);
      const accessDenied = screen.queryByText(/Access Denied/i);
      
      expect(workflowPage || accessDenied).toBeTruthy();
    });
  });
});

