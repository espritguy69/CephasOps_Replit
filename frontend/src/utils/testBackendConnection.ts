/**
 * Backend Connection Test Utility
 * Tests connectivity to backend API endpoints
 */

import apiClient from '../api/client';

interface TestResult {
  success: boolean;
  message: string;
  status?: number;
  data?: unknown;
  error?: unknown;
  health?: unknown;
}

/**
 * Test backend health endpoint
 * @returns Promise with test result
 */
export const testBackendHealth = async (): Promise<TestResult> => {
  try {
    const response = await apiClient.get('/admin/health');
    return {
      success: true,
      message: 'Backend is healthy',
      data: response
    };
  } catch (error) {
    const err = error as { message?: string; status?: number };
    return {
      success: false,
      message: err.message || 'Backend health check failed',
      status: err.status,
      error
    };
  }
};

/**
 * Test backend connectivity
 * @returns Promise with test result
 */
export const testBackendConnection = async (): Promise<TestResult> => {
  try {
    // Test basic connectivity
    const healthResult = await testBackendHealth();
    
    if (healthResult.success) {
      return {
        success: true,
        message: 'Backend connection successful',
        health: healthResult.data
      };
    } else {
      return {
        success: false,
        message: `Backend connection failed: ${healthResult.message}`,
        status: healthResult.status
      };
    }
  } catch (error) {
    const err = error as { message?: string };
    return {
      success: false,
      message: `Connection test failed: ${err.message || 'Unknown error'}`,
      error
    };
  }
};

/**
 * Test authentication endpoint (if implemented)
 * @returns Promise with test result
 */
export const testAuthEndpoint = async (): Promise<TestResult> => {
  try {
    // Try to access auth endpoint (will fail if not implemented, but that's OK)
    await apiClient.get('/auth/me');
    return {
      success: true,
      message: 'Auth endpoint is available'
    };
  } catch (error) {
    const err = error as { status?: number; message?: string };
    if (err.status === 404) {
      return {
        success: false,
        message: 'Auth endpoint not yet implemented',
        status: 404
      };
    } else if (err.status === 401) {
      return {
        success: true,
        message: 'Auth endpoint exists (authentication required)',
        status: 401
      };
    } else {
      return {
        success: false,
        message: `Auth endpoint error: ${err.message || 'Unknown error'}`,
        status: err.status
      };
    }
  }
};

/**
 * Run all backend tests
 * @returns Promise with test results
 */
export const runBackendTests = async (): Promise<{
  connection: TestResult;
  auth: TestResult;
  timestamp: string;
}> => {
  const results = {
    connection: await testBackendConnection(),
    auth: await testAuthEndpoint(),
    timestamp: new Date().toISOString()
  };

  if (import.meta.env.DEV) {
    // eslint-disable-next-line no-console
    console.log('Backend Test Results:', results);
  }
  return results;
};

