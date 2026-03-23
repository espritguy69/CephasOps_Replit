import { expect, afterEach, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
import * as matchers from '@testing-library/jest-dom/matchers';

// Mock Syncfusion diagrams so jsdom does not need HTMLCanvasElement.getContext
vi.mock('@syncfusion/ej2-react-diagrams', () => ({
  DiagramComponent: () => null,
  Diagram: () => null,
  Node: () => null,
  Connector: () => null,
  default: () => null
}));
vi.mock('@syncfusion/ej2-diagrams', () => ({}));

// Mock DepartmentContext so App/MainLayout/TopNav don't throw
vi.mock('@/contexts/DepartmentContext', () => ({
  useDepartment: () => ({
    departments: [],
    activeDepartment: null,
    departmentId: null,
    selectDepartment: vi.fn(),
    refreshDepartments: vi.fn().mockResolvedValue(undefined),
    loading: false,
    error: null,
    landingPage: '/dashboard',
    setLandingPage: vi.fn()
  }),
  DepartmentProvider: ({ children }) => children
}));

// Mock ThemeContext so App/TopNav don't throw
vi.mock('@/contexts/ThemeContext', () => ({
  useTheme: () => ({
    theme: 'light',
    isDark: false,
    toggleTheme: vi.fn(),
    setTheme: vi.fn()
  }),
  ThemeProvider: ({ children }) => children
}));

// Mock NotificationContext so App/MainLayout don't throw
vi.mock('@/contexts/NotificationContext', () => ({
  useNotifications: () => ({
    notifications: [],
    unreadCount: 0,
    markAsRead: vi.fn(),
    markAllAsRead: vi.fn(),
    refresh: vi.fn().mockResolvedValue(undefined),
    loading: false
  }),
  NotificationProvider: ({ children }) => children
}));

// Extend Vitest's expect with jest-dom matchers
expect.extend(matchers);

// Cleanup after each test
afterEach(() => {
  cleanup();
});

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

// Ensure localStorage is available and has getItem (jsdom / test env)
const storage = {};
try {
  if (typeof global.localStorage === 'undefined' || typeof global.localStorage.getItem !== 'function') {
    global.localStorage = {
      getItem: (key) => storage[key] ?? null,
      setItem: (key, value) => { storage[key] = String(value); },
      removeItem: (key) => { delete storage[key]; },
      clear: () => { for (const k of Object.keys(storage)) delete storage[k]; },
      get length() { return Object.keys(storage).length; },
      key: (i) => Object.keys(storage)[i] ?? null
    };
  }
} catch (_) {}

// Mock IntersectionObserver
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  takeRecords() {
    return [];
  }
  unobserve() {}
};
