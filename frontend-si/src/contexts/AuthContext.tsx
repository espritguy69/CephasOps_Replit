import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { setAuthTokenGetter } from '../api/client';
import apiClient from '../api/client';
import type { AuthContextType } from '../types/auth';
import type { User, ServiceInstaller } from '../types/api';

const AuthContext = createContext<AuthContextType | null>(null);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [serviceInstaller, setServiceInstaller] = useState<ServiceInstaller | null>(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  // Register token getter with API client
  useEffect(() => {
    setAuthTokenGetter(() => {
      return localStorage.getItem('authToken');
    });
  }, []);

  // Check if user is authenticated on mount
  useEffect(() => {
    const token = localStorage.getItem('authToken');
    if (token) {
      // Verify token by fetching current user
      fetchCurrentUser();
    } else {
      setLoading(false);
    }
  }, []);

  const fetchCurrentUser = async () => {
    try {
      const response: any = await apiClient.get('/auth/me');
      // Handle both PascalCase and camelCase responses
      const userData = response.User || response.user || response;
      const userObj: User = {
        id: userData.Id || userData.id,
        name: userData.Name || userData.name,
        email: userData.Email || userData.email,
        phone: userData.Phone || userData.phone,
        roles: userData.Roles || userData.roles || [],
      };
      setUser(userObj);

      // Fetch service installer profile if user has one
      try {
        const siResponse: any = await apiClient.get('/service-installers');
        const siList = siResponse.data || siResponse || [];
        if (Array.isArray(siList) && siList.length > 0) {
          // Find SI where UserId matches current user's ID
          const si = siList.find((s: any) => {
            const siUserId = s.userId || s.UserId;
            return siUserId && siUserId.toString() === userObj.id.toString();
          });
          if (si) {
            setServiceInstaller(si as ServiceInstaller);
            // Store SI ID on user object for convenience
            (userObj as any).siId = si.id || si.Id;
          }
        }
      } catch (siError) {
        // If SI profile fetch fails, it's okay - user might not be an SI
        console.warn('Could not fetch service installer profile:', siError);
      }
      
      setUser(userObj);
    } catch (error) {
      // Token invalid, clear it
      localStorage.removeItem('authToken');
      localStorage.removeItem('refreshToken');
      setUser(null);
      setServiceInstaller(null);
    } finally {
      setLoading(false);
    }
  };

  const login = async (email: string, password: string): Promise<{ success: boolean; error?: string }> => {
    try {
      const response: any = await apiClient.post(
        '/auth/login',
        { Email: email, Password: password },
        { skipAuth: true }
      );

      // Handle both PascalCase and camelCase responses
      const accessToken = response.AccessToken || response.accessToken;
      const refreshToken = response.RefreshToken || response.refreshToken;
      const userData = response.User || response.user;

      if (!accessToken) {
        throw new Error('Invalid response: missing access token');
      }

      if (!userData) {
        throw new Error('Invalid response: missing user data');
      }

      // Store tokens
      localStorage.setItem('authToken', accessToken);
      if (refreshToken) {
        localStorage.setItem('refreshToken', refreshToken);
      }

      // Set user
      const userObj: User = {
        id: userData.Id || userData.id,
        name: userData.Name || userData.name,
        email: userData.Email || userData.email,
        phone: userData.Phone || userData.phone,
        roles: userData.Roles || userData.roles || [],
      };
      
      // Fetch service installer profile if user has one
      try {
        const siResponse: any = await apiClient.get('/service-installers');
        const siList = siResponse.data || siResponse || [];
        if (Array.isArray(siList) && siList.length > 0) {
          // Find SI where UserId matches current user's ID
          const si = siList.find((s: any) => {
            const siUserId = s.userId || s.UserId;
            return siUserId && siUserId.toString() === userObj.id.toString();
          });
          if (si) {
            setServiceInstaller(si as ServiceInstaller);
            // Store SI ID on user object for convenience
            (userObj as any).siId = si.id || si.Id;
          }
        }
      } catch (siError) {
        console.warn('Could not fetch service installer profile:', siError);
      }
      
      return { success: true };
    } catch (error: any) {
      return {
        success: false,
        error: error.message || 'Login failed',
      };
    }
  };

  const logout = () => {
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    setUser(null);
    navigate('/login');
  };

  // Check if user is a subcontractor
  const isSubcontractor = serviceInstaller?.isSubcontractor || 
                          (serviceInstaller as any)?.IsSubcontractor || 
                          serviceInstaller?.siLevel === 'Subcon' ||
                          (serviceInstaller as any)?.SiLevel === 'Subcon' ||
                          false;

  // Check if user is admin or super admin
  const isAdmin = user?.roles?.some(role => {
    const roleLower = role.toLowerCase();
    return roleLower.includes('admin') || roleLower === 'superadmin' || roleLower === 'super admin';
  }) ?? false;

  const value: AuthContextType = {
    user,
    serviceInstaller,
    loading,
    login,
    logout,
    isAuthenticated: !!user,
    isSubcontractor,
    isAdmin,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}

