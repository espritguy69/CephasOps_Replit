import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import App from './App';
import { AuthProvider } from './contexts/AuthContext';
import { DepartmentProvider } from './contexts/DepartmentContext';
import { NotificationProvider } from './contexts/NotificationContext';
import { ThemeProvider } from './contexts/ThemeContext';
import { CompanySettingsProvider } from './contexts/CompanySettingsContext';
import { ToastProvider, ErrorBoundary } from './components/ui';
import { initializeSyncfusion } from './utils/syncfusion';
import { queryClient } from './lib/queryClient';
import './index.css';

// Initialize Syncfusion license
initializeSyncfusion();

const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error('Root element not found');
}

ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter
          future={{
            v7_startTransition: true,
            v7_relativeSplatPath: true
          }}
        >
          <ThemeProvider>
            <AuthProvider>
              <CompanySettingsProvider>
                <DepartmentProvider>
                  <NotificationProvider>
                    <ToastProvider>
                      <App />
                    </ToastProvider>
                  </NotificationProvider>
                </DepartmentProvider>
              </CompanySettingsProvider>
            </AuthProvider>
          </ThemeProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </ErrorBoundary>
  </React.StrictMode>
);
