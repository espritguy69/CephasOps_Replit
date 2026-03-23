import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { login as apiLogin, logout as apiLogout, getCurrentUser, refreshToken as apiRefreshToken } from '../api/auth';
import { setAuthTokenGetter } from '../api/client';
import type { User, AuthResponse } from '../types/auth';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  error: string | null;
  /** Set when /me returns mustChangePassword; app should redirect to change-password. Cleared after redirect or logout. */
  pendingPasswordChangeEmail: string | null;
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string; requiresPasswordChange?: boolean; accountLocked?: boolean; email?: string }>;
  logout: () => Promise<void>;
  clearError: () => void;
  clearPendingPasswordChange: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('authToken'));
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [pendingPasswordChangeEmail, setPendingPasswordChangeEmail] = useState<string | null>(null);

  // Register token getter with API client - always read from localStorage to get latest value
  useEffect(() => {
    setAuthTokenGetter(() => {
      // Always read from localStorage to ensure we get the latest token
      // This handles cases where token is updated in another tab/window
      return localStorage.getItem('authToken');
    });
  }, []);

  // Initialize auth state on mount
  useEffect(() => {
    const initAuth = async () => {
      const storedToken = localStorage.getItem('authToken');
      if (storedToken) {
        try {
          // Set token in state first so the getter can use it
          setToken(storedToken);
          
          // Add timeout to prevent hanging if backend is not available
          const timeoutPromise = new Promise<never>((_, reject) => 
            setTimeout(() => reject(new Error('Request timeout')), 5000)
          );
          
          const userDataPromise = getCurrentUser();
          const userData = await Promise.race([userDataPromise, timeoutPromise]);
          
          if (userData.mustChangePassword) {
            localStorage.removeItem('authToken');
            localStorage.removeItem('refreshToken');
            setToken(null);
            setUser(null);
            setPendingPasswordChangeEmail(userData.email ?? null);
          } else {
            setUser(userData);
          }
        } catch (err) {
          // Token invalid or backend unavailable, clear storage
          const error = err as Error;
          console.warn('Auth initialization failed:', error.message);
          localStorage.removeItem('authToken');
          localStorage.removeItem('refreshToken');
          setToken(null);
          setUser(null);
        }
      } else {
        setToken(null);
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  // Auto-refresh token before expiry
  useEffect(() => {
    if (!token) return;

    const refreshInterval = setInterval(async () => {
      try {
        const refreshTokenValue = localStorage.getItem('refreshToken');
        if (refreshTokenValue) {
          const response = await apiRefreshToken(refreshTokenValue);
          setToken(response.token);
          localStorage.setItem('authToken', response.token);
          if (response.refreshToken) {
            localStorage.setItem('refreshToken', response.refreshToken);
          }
        }
      } catch (err) {
        console.error('Token refresh failed:', err);
        // Logout on refresh failure
        handleLogout();
      }
    }, 15 * 60 * 1000); // Refresh every 15 minutes

    return () => clearInterval(refreshInterval);
  }, [token]);

  const handleLogin = async (email: string, password: string): Promise<{ success: boolean; error?: string; requiresPasswordChange?: boolean; accountLocked?: boolean; email?: string }> => {
    try {
      setError(null);
      setLoading(true);
      const response = await apiLogin(email, password);
      
      const { token: newToken, refreshToken, user: userData } = response;
      
      setToken(newToken);
      setUser(userData);
      localStorage.setItem('authToken', newToken);
      if (refreshToken) {
        localStorage.setItem('refreshToken', refreshToken);
      }
      
      return { success: true };
    } catch (err) {
      const error = err as Error & { requiresPasswordChange?: boolean; accountLocked?: boolean };
      if (error.requiresPasswordChange) {
        return { success: false, error: error.message, requiresPasswordChange: true, email };
      }
      if (error.accountLocked) {
        return { success: false, error: error.message, accountLocked: true };
      }
      const errorMessage = error.message || 'Login failed. Please check your credentials.';
      setError(errorMessage);
      return { success: false, error: errorMessage };
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = async (): Promise<void> => {
    try {
      await apiLogout();
    } catch (err) {
      console.error('Logout error:', err);
    } finally {
      setToken(null);
      setUser(null);
      setPendingPasswordChangeEmail(null);
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
    }
  };

  const value: AuthContextType = {
    user,
    token,
    isAuthenticated: !!token && !!user,
    loading,
    error,
    pendingPasswordChangeEmail,
    login: handleLogin,
    logout: handleLogout,
    clearError: () => setError(null),
    clearPendingPasswordChange: () => setPendingPasswordChangeEmail(null)
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

