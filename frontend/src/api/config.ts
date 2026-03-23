// API configuration
// Centralized API base URL that works with Vite proxy in development

/**
 * Get the API base URL
 * - In development: uses relative URL '/api' (Vite proxy handles routing)
 * - In production: uses VITE_API_BASE_URL env var or falls back to absolute URL
 */
export const getApiBaseUrl = (): string => {
  // If VITE_API_BASE_URL is explicitly set, use it
  if (import.meta.env.VITE_API_BASE_URL) {
    console.log('[API Config] Using VITE_API_BASE_URL:', import.meta.env.VITE_API_BASE_URL);
    return import.meta.env.VITE_API_BASE_URL;
  }
  
  // In development mode, use relative URL (Vite proxy will handle it)
  // import.meta.env.DEV is true when running 'vite' or 'vite dev'
  // import.meta.env.MODE === 'development' is an alternative check
  const isDev = import.meta.env.DEV || import.meta.env.MODE === 'development';
  
  if (isDev) {
    console.log('[API Config] Development mode detected, using relative URL: /api');
    return '/api';
  }
  
  // In production, use relative URL /api (requires reverse proxy or same-origin backend)
  console.log('[API Config] Production mode, using relative URL: /api');
  return '/api';
};

/**
 * API base URL constant
 */
export const API_BASE_URL = getApiBaseUrl();

