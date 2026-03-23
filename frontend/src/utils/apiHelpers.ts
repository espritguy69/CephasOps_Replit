/**
 * API Helper Utilities
 * Common functions for handling API responses and errors
 */

interface ApiError extends Error {
  status?: number;
  message: string;
}

/**
 * Extract data from API response
 * Handles different response formats:
 * - { data: [...] }
 * - { data: { ... } }
 * - Direct array/object
 * @param response - API response
 * @returns Extracted data
 */
export const extractData = <T = unknown>(response: unknown): T | null => {
  if (!response) return null;
  
  // If response has a 'data' property, use it
  if (typeof response === 'object' && response !== null && 'data' in response) {
    return (response as { data: T }).data;
  }
  
  // Otherwise return the response itself
  return response as T;
};

/**
 * Handle API errors gracefully
 * @param error - Error object
 * @param defaultMessage - Default error message
 * @returns User-friendly error message
 */
export const handleApiError = (error: unknown, defaultMessage = 'An error occurred'): string => {
  const apiError = error as ApiError;
  
  // Handle 404 (endpoint not implemented)
  if (apiError.status === 404) {
    return 'This feature is not yet available. Backend endpoint required.';
  }
  
  // Handle 401 (unauthorized)
  if (apiError.status === 401) {
    return 'Authentication required. Please log in.';
  }
  
  // Handle 403 (forbidden)
  if (apiError.status === 403) {
    return 'You do not have permission to access this resource.';
  }
  
  // Handle 500 (server error)
  if (apiError.status === 500) {
    return 'Server error. Please try again later.';
  }
  
  // Use error message if available
  if (apiError.message) {
    return apiError.message;
  }
  
  // Fallback to default
  return defaultMessage;
};

/**
 * Check if endpoint is implemented (not 404)
 * @param error - Error object
 * @returns True if endpoint exists (not 404)
 */
export const isEndpointImplemented = (error: unknown): boolean => {
  const apiError = error as ApiError;
  return apiError.status !== 404;
};

/**
 * Format API response for display
 * @param response - API response
 * @param key - Optional key to extract from response
 * @returns Formatted data
 */
export const formatResponse = <T = unknown>(response: unknown, key: string | null = null): T | null => {
  const data = extractData<T>(response);
  
  if (key && data && typeof data === 'object' && data !== null && key in data) {
    return (data as Record<string, T>)[key];
  }
  
  return data;
};

