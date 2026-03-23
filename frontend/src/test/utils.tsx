import React from 'react';
import { render } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import * as AuthContextModule from '@/contexts/AuthContext';
import { ToastProvider } from '@/components/ui';

/**
 * Custom render function that includes all providers
 * Uses mocked context values for testing
 */
export const renderWithProviders = (
  ui,
  {
    authValue = {
      isAuthenticated: true,
      user: {
        id: 'test-user-id',
        email: 'test@example.com',
        permissions: ['workflow.view', 'workflow.edit'],
        roles: ['User']
      },
      token: 'test-token',
      loading: false,
      error: null,
      login: vi.fn(),
      logout: vi.fn(),
      clearError: vi.fn()
    },
    wrapWithRouter = true,
    ...renderOptions
  } = {}
) => {
  vi.spyOn(AuthContextModule, 'useAuth').mockReturnValue(authValue);

  const Wrapper = ({ children }: { children: React.ReactNode }) =>
    wrapWithRouter ? (
      <BrowserRouter>
        <ToastProvider>{children}</ToastProvider>
      </BrowserRouter>
    ) : (
      <ToastProvider>{children}</ToastProvider>
    );

  return render(ui, { wrapper: Wrapper, ...renderOptions });
};

/**
 * Mock API client responses
 */
export const mockApiResponse = (data, status = 200) => {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(data),
    text: () => Promise.resolve(JSON.stringify(data)),
    headers: new Headers({
      'Content-Type': 'application/json'
    })
  });
};

/**
 * Mock API error
 */
export const mockApiError = (message, status = 400) => {
  return Promise.reject({
    message,
    status,
    response: {
      ok: false,
      status,
      json: () => Promise.resolve({ message, error: message })
    }
  });
};
