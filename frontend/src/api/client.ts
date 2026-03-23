// API client configuration
// Centralized HTTP client with authentication support

// Import shared API config
import { getApiBaseUrl } from './config';

// Use shared API base URL
const API_BASE_URL = getApiBaseUrl();

// Types for API client
export interface ApiClientConfig {
  params?: Record<string, any>;
  headers?: Record<string, string>;
  [key: string]: any;
}

export interface ApiError extends Error {
  status?: number;
  data?: any;
}

type AuthTokenGetter = () => string | null;
type DepartmentGetter = () => string | { id?: string; departmentId?: string } | null;
type CompanyIdGetter = () => string | null;

// Store reference to auth context getter (set by AuthContext)
let getAuthTokenFn: AuthTokenGetter | null = null;
let getDepartmentGetterFn: DepartmentGetter | null = null;
let getCompanyIdFn: CompanyIdGetter | null = null;

/**
 * Set the function to get auth token (called by AuthContext)
 * @param fn - Function that returns the auth token
 */
export const setAuthTokenGetter = (fn: AuthTokenGetter): void => {
  getAuthTokenFn = fn;
};

/**
 * Set the function to get the active department (called by DepartmentContext)
 * @param fn - Function that returns the active department or its ID
 */
export const setDepartmentGetter = (fn: DepartmentGetter): void => {
  getDepartmentGetterFn = fn;
};

/**
 * Set the function to get the effective company ID for this request (e.g. from department context).
 * When set, X-Company-Id is sent so SuperAdmin company switch works; ignored by backend for non-SuperAdmin.
 * @param fn - Function that returns the company ID string or null
 */
export const setCompanyIdGetter = (fn: CompanyIdGetter | null): void => {
  getCompanyIdFn = fn;
};

/**
 * Get authentication token from storage or context
 * @returns Auth token or null
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
 * Get department ID from context if available
 * @returns Department ID or null
 */
const getActiveDepartmentId = (): string | null => {
  if (!getDepartmentGetterFn) return null;

  try {
    const value = getDepartmentGetterFn();
    if (!value) return null;

    if (typeof value === 'string') {
      return value || null;
    }

    if (typeof value === 'object' && value !== null) {
      if ('id' in value && value.id) return value.id as string;
      if ('departmentId' in value && value.departmentId) return value.departmentId as string;
    }
  } catch (err) {
    console.warn('Error getting department from context:', err);
  }

  return null;
};

/**
 * Inject departmentId query param when absent
 * @param params - Query parameters
 * @returns Parameters with departmentId added if needed
 */
const ensureDepartmentParam = (params?: Record<string, any>): Record<string, any> => {
  const departmentId = getActiveDepartmentId();
  if (!departmentId) {
    return params || {};
  }

  if (params && Object.prototype.hasOwnProperty.call(params, 'departmentId')) {
    const existing = params.departmentId;
    if (existing === undefined || existing === null || existing === '') {
      return { ...params, departmentId };
    }
    return params;
  }

  return {
    ...(params || {}),
    departmentId
  };
};

/**
 * Build headers with authentication
 * @param customHeaders - Additional custom headers
 * @param skipAuth - Skip adding auth token (for login endpoints)
 * @returns Headers object
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

  const departmentId = getActiveDepartmentId();
  if (departmentId && !skipAuth) {
    headers['X-Department-Id'] = departmentId;
  }

  if (!skipAuth && getCompanyIdFn) {
    try {
      const companyId = getCompanyIdFn();
      if (companyId) {
        headers['X-Company-Id'] = companyId;
      }
    } catch (err) {
      console.warn('Error getting company ID from context:', err);
    }
  }

  return headers;
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
 * @param response - Fetch response object
 * @throws API error with status and message
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
        // Try to get text error message
        const text = await response.text();
        if (text) errorMessage = text;
      }
    } catch {
      // If response parsing fails, use default message
    }

    if (response.status === 401) {
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
      sessionStorage.setItem('sessionExpired', '1');
    }

    const error: ApiError = new Error(errorMessage);
    error.status = response.status;
    error.data = errorData;
    throw error;
  }
};

interface ApiClient {
  /**
   * GET request
   * @param url - API endpoint URL
   * @param config - Request configuration (params, headers, etc.)
   * @returns Response data
   */
  get<T = any>(url: string, config?: ApiClientConfig): Promise<T>;

  /**
   * POST request
   * @param url - API endpoint URL
   * @param data - Request body data
   * @param config - Request configuration (headers, etc.)
   * @returns Response data
   */
  post<T = any>(url: string, data?: any, config?: ApiClientConfig): Promise<T>;

  /**
   * PATCH request
   * @param url - API endpoint URL
   * @param data - Request body data
   * @param config - Request configuration (headers, etc.)
   * @returns Response data
   */
  patch<T = any>(url: string, data?: any, config?: ApiClientConfig): Promise<T>;

  /**
   * PUT request
   * @param url - API endpoint URL
   * @param data - Request body data
   * @param config - Request configuration (headers, etc.)
   * @returns Response data
   */
  put<T = any>(url: string, data?: any, config?: ApiClientConfig): Promise<T>;

  /**
   * DELETE request
   * @param url - API endpoint URL
   * @param config - Request configuration (headers, etc.)
   * @returns Response data or null
   */
  delete<T = any>(url: string, config?: ApiClientConfig): Promise<T | null>;
}

const apiClient: ApiClient = {
  async get<T = any>(url: string, config: ApiClientConfig = {}): Promise<T> {
    const { params, headers: customHeaders, ...restConfig } = config;
    const paramsWithDepartment = ensureDepartmentParam(params);
    
    // Filter out undefined and null values from query params
    const cleanParams: Record<string, any> = {};
    if (paramsWithDepartment) {
      Object.keys(paramsWithDepartment).forEach(key => {
        const value = paramsWithDepartment[key];
        if (value !== undefined && value !== null && value !== 'undefined' && value !== 'null') {
          cleanParams[key] = value;
        }
      });
    }
    
    // Build query string from params
    let queryString = '';
    if (Object.keys(cleanParams).length > 0) {
      queryString = '?' + new URLSearchParams(cleanParams).toString();
    }

    try {
      const fullUrl = `${API_BASE_URL}${url}${queryString}`;
      if (import.meta.env.DEV) {
        console.log('[API Client] GET request to:', fullUrl);
      }
      const response = await fetch(fullUrl, {
        method: 'GET',
        headers: buildHeaders(customHeaders),
        ...restConfig
      });

      await handleError(response);
      const json = await response.json();
      return unwrapResponse<T>(json);
    } catch (error) {
      // Handle network errors (backend not available, CORS, etc.)
      if (error instanceof TypeError && error.message.includes('fetch')) {
        throw new Error('Network error: Unable to connect to the server. Please check if the backend is running.');
      }
      throw error;
    }
  },

  async post<T = any>(url: string, data?: any, config: ApiClientConfig = {}): Promise<T> {
    const { headers: customHeaders, ...restConfig } = config;
    
    const skipAuth = url.includes('/auth/login') || url.includes('/auth/refresh');

    try {
      const response = await fetch(`${API_BASE_URL}${url}`, {
        method: 'POST',
        headers: buildHeaders(customHeaders, skipAuth),
        body: JSON.stringify(data),
        ...restConfig
      });

      await handleError(response);
      
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('application/json')) {
        const json = await response.json();
        return unwrapResponse<T>(json);
      }
      
      return {} as T;
    } catch (error) {
      if (error instanceof TypeError && error.message.includes('fetch')) {
        throw new Error('Network error: Unable to connect to the server. Please check if the backend is running.');
      }
      throw error;
    }
  },

  async patch<T = any>(url: string, data?: any, config: ApiClientConfig = {}): Promise<T> {
    const { headers: customHeaders, ...restConfig } = config;

    try {
      const response = await fetch(`${API_BASE_URL}${url}`, {
        method: 'PATCH',
        headers: buildHeaders(customHeaders),
        body: JSON.stringify(data),
        ...restConfig
      });

      await handleError(response);
      const json = await response.json();
      return unwrapResponse<T>(json);
    } catch (error) {
      if (error instanceof TypeError && error.message.includes('fetch')) {
        throw new Error('Network error: Unable to connect to the server. Please check if the backend is running.');
      }
      throw error;
    }
  },

  async put<T = any>(url: string, data?: any, config: ApiClientConfig = {}): Promise<T> {
    const { headers: customHeaders, ...restConfig } = config;

    try {
      const response = await fetch(`${API_BASE_URL}${url}`, {
        method: 'PUT',
        headers: buildHeaders(customHeaders),
        body: JSON.stringify(data),
        ...restConfig
      });

      await handleError(response);
      const json = await response.json();
      return unwrapResponse<T>(json);
    } catch (error) {
      if (error instanceof TypeError && error.message.includes('fetch')) {
        throw new Error('Network error: Unable to connect to the server. Please check if the backend is running.');
      }
      throw error;
    }
  },

  async delete<T = any>(url: string, config: ApiClientConfig = {}): Promise<T | null> {
    const { headers: customHeaders, ...restConfig } = config;

    try {
      const response = await fetch(`${API_BASE_URL}${url}`, {
        method: 'DELETE',
        headers: buildHeaders(customHeaders),
        ...restConfig
      });

      await handleError(response);
      
      const contentType = response.headers.get('content-type');
      const contentLength = response.headers.get('content-length');
      
      if (response.status === 204 || !contentLength || contentLength === '0') {
        return null;
      }
      
      if (contentType && contentType.includes('application/json')) {
        try {
          const text = await response.text();
          if (!text || text.trim() === '') {
            return null;
          }
          const json = JSON.parse(text);
          return unwrapResponse<T>(json);
        } catch (parseError) {
          console.warn('Failed to parse DELETE response as JSON:', parseError);
          return null;
        }
      }
      
      return null;
    } catch (error) {
      if (error instanceof TypeError && error.message.includes('fetch')) {
        throw new Error('Network error: Unable to connect to the server. Please check if the backend is running.');
      }
      throw error;
    }
  }
};

export default apiClient;

