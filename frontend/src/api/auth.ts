import apiClient, { type ApiError } from './client';
import type { LoginCredentials, AuthResponse, RefreshTokenRequest, ChangePasswordRequest, User } from '../types/auth';

/**
 * Authentication API
 * Handles user authentication, token management
 */

/**
 * User login
 * @param email - User email address
 * @param password - User password
 * @returns Authentication response with token and user info
 */
export const login = async (email: string, password: string): Promise<AuthResponse> => {
  try {
    // Backend expects PascalCase: Email and Password
    const response = await apiClient.post<{
      AccessToken?: string;
      accessToken?: string;
      RefreshToken?: string;
      refreshToken?: string;
      ExpiresAt?: string | Date;
      expiresAt?: string | Date;
      User?: {
        Id?: string;
        id?: string;
        Name?: string;
        name?: string;
        Email?: string;
        email?: string;
        Phone?: string;
        phone?: string;
        Roles?: string[];
        roles?: string[];
      };
      user?: {
        Id?: string;
        id?: string;
        Name?: string;
        name?: string;
        Email?: string;
        email?: string;
        Phone?: string;
        phone?: string;
        Roles?: string[];
        roles?: string[];
        Permissions?: string[];
        permissions?: string[];
      };
    }>('/auth/login', {
      Email: email,
      Password: password
    });
    
    // Backend returns PascalCase (AccessToken, RefreshToken, ExpiresAt, User)
    const accessToken = response.AccessToken || response.accessToken;
    const refreshToken = response.RefreshToken || response.refreshToken;
    const expiresAt = response.ExpiresAt || response.expiresAt;
    const userData = response.User || response.user;
    
    if (!accessToken) {
      throw new Error('Invalid response: missing access token');
    }

    if (!userData) {
      throw new Error('Invalid response: missing user data');
    }

    // Map user DTO to frontend User type (handle both PascalCase and camelCase)
    const mappedUser: User = {
      id: userData.Id || userData.id || '',
      name: userData.Name || userData.name || '',
      email: userData.Email || userData.email || '',
      phone: userData.Phone || userData.phone,
      roles: userData.Roles || userData.roles || [],
      permissions: userData.Permissions || userData.permissions,
      mustChangePassword: userData.MustChangePassword ?? userData.mustChangePassword
    };

    // Convert ExpiresAt to ISO string if it's a Date object
    let expiresAtString: string | undefined;
    if (expiresAt) {
      if (typeof expiresAt === 'string') {
        expiresAtString = expiresAt;
      } else if (expiresAt instanceof Date) {
        expiresAtString = expiresAt.toISOString();
      } else {
        expiresAtString = new Date(expiresAt).toISOString();
      }
    }

    return {
      token: accessToken,
      refreshToken: refreshToken,
      expiresAt: expiresAtString,
      user: mappedUser
    };
  } catch (error: any) {
    const apiError = error as ApiError;
    if (apiError.status === 403 && apiError.data?.data?.requiresPasswordChange) {
      const e = new Error(apiError.message || 'You must change your password before signing in.') as Error & { requiresPasswordChange?: boolean };
      e.requiresPasswordChange = true;
      throw e;
    }
    if (apiError.status === 423 || apiError.data?.data?.accountLocked) {
      const e = new Error(apiError.message || 'Your account is temporarily locked due to repeated failed sign-in attempts. Please try again later.') as Error & { accountLocked?: boolean };
      e.accountLocked = true;
      throw e;
    }
    if (apiError.status === 401) {
      throw new Error('Invalid email or password');
    }
    if (error.message) {
      throw error;
    }
    throw new Error('Login failed. Please check your credentials and try again.');
  }
};

/**
 * Refresh authentication token
 * @param refreshToken - Refresh token
 * @returns New authentication tokens
 */
export const refreshToken = async (refreshToken: string): Promise<AuthResponse> => {
  try {
    // Backend expects PascalCase: RefreshToken
    const response = await apiClient.post<{
      AccessToken?: string;
      accessToken?: string;
      RefreshToken?: string;
      refreshToken?: string;
      ExpiresAt?: string | Date;
      expiresAt?: string | Date;
      User?: {
        Id?: string;
        id?: string;
        Name?: string;
        name?: string;
        Email?: string;
        email?: string;
        Phone?: string;
        phone?: string;
        Roles?: string[];
        roles?: string[];
      };
      user?: {
        Id?: string;
        id?: string;
        Name?: string;
        name?: string;
        Email?: string;
        email?: string;
        Phone?: string;
        phone?: string;
        Roles?: string[];
        roles?: string[];
        Permissions?: string[];
        permissions?: string[];
      };
    }>('/auth/refresh', {
      RefreshToken: refreshToken
    });
    
    // Backend returns PascalCase (AccessToken, RefreshToken, ExpiresAt, User)
    const accessToken = response.AccessToken || response.accessToken;
    const refreshTokenValue = response.RefreshToken || response.refreshToken;
    const expiresAt = response.ExpiresAt || response.expiresAt;
    const userData = response.User || response.user;
    
    if (!accessToken) {
      throw new Error('Invalid response: missing access token');
    }

    if (!userData) {
      throw new Error('Invalid response: missing user data');
    }

    // Map user DTO to frontend User type
    const mappedUser: User = {
      id: userData.Id || userData.id || '',
      name: userData.Name || userData.name || '',
      email: userData.Email || userData.email || '',
      phone: userData.Phone || userData.phone,
      roles: userData.Roles || userData.roles || [],
      permissions: userData.Permissions || userData.permissions,
      mustChangePassword: userData.MustChangePassword ?? userData.mustChangePassword
    };

    // Convert ExpiresAt to ISO string if it's a Date object
    let expiresAtString: string | undefined;
    if (expiresAt) {
      if (typeof expiresAt === 'string') {
        expiresAtString = expiresAt;
      } else if (expiresAt instanceof Date) {
        expiresAtString = expiresAt.toISOString();
      } else {
        expiresAtString = new Date(expiresAt).toISOString();
      }
    }

    return {
      token: accessToken,
      refreshToken: refreshTokenValue,
      expiresAt: expiresAtString,
      user: mappedUser
    };
  } catch (error: any) {
    const apiError = error as ApiError;
    if (apiError.status === 403 && apiError.data?.data?.requiresPasswordChange) {
      const e = new Error(apiError.message || 'You must change your password.') as Error & { requiresPasswordChange?: boolean };
      e.requiresPasswordChange = true;
      throw e;
    }
    if (error.status === 401) {
      throw new Error('Invalid refresh token');
    }
    if (error.message) {
      throw error;
    }
    throw new Error('Token refresh failed. Please login again.');
  }
};

/**
 * Get current user information
 * @returns Current user details, roles, and departments
 */
export const getCurrentUser = async (): Promise<User> => {
  try {
    // Backend returns UserDto with PascalCase properties
    const response = await apiClient.get<{
      Id?: string;
      id?: string;
      Name?: string;
      name?: string;
      Email?: string;
      email?: string;
      Phone?: string;
      phone?: string;
      Roles?: string[];
      roles?: string[];
      Permissions?: string[];
      permissions?: string[];
    }>('/auth/me');
    
    // Map backend UserDto (PascalCase) to frontend User type (camelCase)
    return {
      id: response.Id || response.id || '',
      name: response.Name || response.name || '',
      email: response.Email || response.email || '',
      phone: response.Phone || response.phone,
      roles: response.Roles || response.roles || [],
      permissions: response.Permissions || response.permissions,
      mustChangePassword: response.MustChangePassword ?? response.mustChangePassword
    };
  } catch (error: any) {
    if (error.status === 401) {
      // Clear invalid tokens
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
      throw new Error('Session expired. Please login again.');
    }
    throw error;
  }
};

/**
 * Logout current user
 * @returns Promise that resolves when logout is complete
 */
export const logout = async (): Promise<void> => {
  await apiClient.post('/auth/logout');
};

/**
 * Change user password (authenticated user).
 * @param request - Password change request (currentPassword and newPassword)
 * @returns Promise that resolves when password is changed
 */
export const changePassword = async (request: ChangePasswordRequest): Promise<void> => {
  await apiClient.post('/auth/change-password', {
    CurrentPassword: request.currentPassword,
    NewPassword: request.newPassword
  });
};

/**
 * Change password when login is blocked by MustChangePassword (no token).
 * @param email - User email
 * @param currentPassword - Current password
 * @param newPassword - New password
 */
export const changePasswordRequired = async (
  email: string,
  currentPassword: string,
  newPassword: string
): Promise<void> => {
  await apiClient.post('/auth/change-password-required', {
    Email: email.trim().toLowerCase(),
    CurrentPassword: currentPassword,
    NewPassword: newPassword
  });
};

/**
 * Request a password reset email. Always returns success to avoid account enumeration.
 * @param email - User email
 */
export const forgotPassword = async (email: string): Promise<void> => {
  await apiClient.post('/auth/forgot-password', {
    Email: email?.trim().toLowerCase() ?? ''
  });
};

/**
 * Reset password using the token from the email link.
 * @param token - Token from reset link
 * @param newPassword - New password
 * @param confirmPassword - Confirm new password
 */
export const resetPasswordWithToken = async (
  token: string,
  newPassword: string,
  confirmPassword: string
): Promise<void> => {
  await apiClient.post('/auth/reset-password-with-token', {
    Token: token,
    NewPassword: newPassword,
    ConfirmPassword: confirmPassword
  });
};

