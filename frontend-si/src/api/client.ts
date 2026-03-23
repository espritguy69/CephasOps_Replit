// API client configuration for SI App
// Centralized HTTP client with authentication support

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

// Store reference to auth token getter (set by AuthContext)
type AuthTokenGetter = () => string | null;
let getAuthTokenFn: AuthTokenGetter | null = null;

export interface ApiClientConfig {
  params?: Record<string, any>;
  headers?: Record<string, string>;
  skipAuth?: boolean;
  [key: string]: any;
}

export interface ApiError extends Error {
  status?: number;
  data?: any;
}

/**
 * Set the function to get auth token (called by AuthContext)
 */
export const setAuthTokenGetter = (fn: AuthTokenGetter): void => {
  getAuthTokenFn = fn;
};

/**
 * Get authentication token from storage or context
 */
const getAuthToken = (): string | null => {
  // Try context getter first (preferred)
  if (getAuthTokenFn) {
    try {
      const token = getAuthTokenFn();
      if (token) return token;
    } catch (err) {
      console.warn('Error getting token from context:', err);
    }
  }
  // Fallback to localStorage (always works as backup)
  return localStorage.getItem('authToken') || null;
};

/**
 * Build headers with authentication
 */
const buildHeaders = (customHeaders: Record<string, string> = {}, skipAuth: boolean = false): Record<string, string> => {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...customHeaders
  };

  // Only add auth token if not skipping (e.g., for login endpoint)
  if (!skipAuth) {
    const token = getAuthToken();
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
  }

  return headers;
};

/**
 * Build URL with query parameters
 */
const buildUrl = (url: string, params: Record<string, any> = {}): string => {
  if (!params || Object.keys(params).length === 0) {
    return url;
  }

  const searchParams = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') {
      searchParams.append(key, value.toString());
    }
  });

  const queryString = searchParams.toString();
  return queryString ? `${url}?${queryString}` : url;
};

/**
 * Standard API response envelope structure
 */
interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}

/**
 * Unwrap API response envelope if present, otherwise return data as-is
 * Supports both new envelope format and legacy direct responses
 */
const unwrapResponse = <T>(response: any): T => {
  // Check if response has ApiResponse structure
  if (response && typeof response === 'object' && 'success' in response) {
    const apiResponse = response as ApiResponse<T>;
    
    // If not successful, throw error with messages
    if (!apiResponse.success) {
      const errorMessage = apiResponse.message || apiResponse.errors?.join(', ') || 'API request failed';
      const error: ApiError = new Error(errorMessage);
      error.data = apiResponse;
      throw error;
    }
    
    // Return data from envelope
    return apiResponse.data as T;
  }
  
  // Legacy format: return response as-is
  return response as T;
};

/**
 * Handle API response errors
 */
const handleError = async (response: Response): Promise<void> => {
  if (!response.ok) {
    let errorMessage = `API Error: ${response.status} ${response.statusText}`;
    let errorData: any = null;
    
    try {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        errorData = await response.json();
        
        // Check if error is in ApiResponse format
        if (errorData && typeof errorData === 'object' && 'success' in errorData) {
          const apiResponse = errorData as ApiResponse<any>;
          errorMessage = apiResponse.message || apiResponse.errors?.join(', ') || errorMessage;
        } else {
          errorMessage = errorData.message || errorData.error || errorMessage;
        }
      } else {
        const text = await response.text();
        if (text) errorMessage = text;
      }
    } catch {
      // If response parsing fails, use default message
    }

    // Handle specific error codes
    if (response.status === 401) {
      // Unauthorized - clear auth
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
      // Redirect to login will be handled by ProtectedRoute
    }

    const error: ApiError = new Error(errorMessage);
    error.status = response.status;
    error.data = errorData;
    throw error;
  }
};

interface ApiClient {
  get<T = any>(url: string, config?: ApiClientConfig): Promise<T>;
  post<T = any>(url: string, data?: any, config?: ApiClientConfig): Promise<T>;
  patch<T = any>(url: string, data?: any, config?: ApiClientConfig): Promise<T>;
  put<T = any>(url: string, data?: any, config?: ApiClientConfig): Promise<T>;
  delete<T = any>(url: string, config?: ApiClientConfig): Promise<T>;
}

const apiClient: ApiClient = {
  /**
   * GET request
   */
  async get<T = any>(url: string, config: ApiClientConfig = {}): Promise<T> {
    const { params, headers, ...restConfig } = config;
    const fullUrl = buildUrl(`${API_BASE_URL}${url}`, params);
    
    const response = await fetch(fullUrl, {
      method: 'GET',
      headers: buildHeaders(headers),
      ...restConfig
    });

    await handleError(response);
    const json = await response.json();
    return unwrapResponse<T>(json);
  },

  /**
   * POST request
   */
  async post<T = any>(url: string, data?: any, config: ApiClientConfig = {}): Promise<T> {
    const { headers, skipAuth = false, ...restConfig } = config;
    
    const response = await fetch(`${API_BASE_URL}${url}`, {
      method: 'POST',
      headers: buildHeaders(headers, skipAuth),
      body: JSON.stringify(data),
      ...restConfig
    });

    await handleError(response);
    const json = await response.json();
    return unwrapResponse<T>(json);
  },

  /**
   * PATCH request
   */
  async patch<T = any>(url: string, data?: any, config: ApiClientConfig = {}): Promise<T> {
    const { headers, ...restConfig } = config;
    
    const response = await fetch(`${API_BASE_URL}${url}`, {
      method: 'PATCH',
      headers: buildHeaders(headers),
      body: JSON.stringify(data),
      ...restConfig
    });

    await handleError(response);
    const json = await response.json();
    return unwrapResponse<T>(json);
  },

  /**
   * PUT request
   */
  async put<T = any>(url: string, data?: any, config: ApiClientConfig = {}): Promise<T> {
    const { headers, ...restConfig } = config;
    
    const response = await fetch(`${API_BASE_URL}${url}`, {
      method: 'PUT',
      headers: buildHeaders(headers),
      body: JSON.stringify(data),
      ...restConfig
    });

    await handleError(response);
    const json = await response.json();
    return unwrapResponse<T>(json);
  },

  /**
   * DELETE request
   */
  async delete<T = any>(url: string, config: ApiClientConfig = {}): Promise<T> {
    const { headers, ...restConfig } = config;
    
    const response = await fetch(`${API_BASE_URL}${url}`, {
      method: 'DELETE',
      headers: buildHeaders(headers),
      ...restConfig
    });

    await handleError(response);
    // DELETE might return empty body
    const text = await response.text();
    if (!text) return null;
    const json = JSON.parse(text);
    return unwrapResponse<T>(json);
  }
};

export default apiClient;

