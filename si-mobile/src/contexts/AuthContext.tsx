import React, { createContext, useContext, useState, useEffect, useCallback, ReactNode } from 'react';
import { setAuthTokenGetter } from '../api/client';
import { getItem, setItem, removeItem } from '../lib/storage';
import {
  login as apiLogin,
  getCurrentUser,
  getServiceInstallers,
  type LoginResponse,
} from '../api';
import type { User, ServiceInstaller } from '../types/api';

const TOKEN_KEY = 'authToken';
const REFRESH_KEY = 'refreshToken';

export interface AuthContextType {
  user: User | null;
  serviceInstaller: ServiceInstaller | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  isAuthenticated: boolean;
  siId: string | null;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [serviceInstaller, setServiceInstaller] = useState<ServiceInstaller | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setAuthTokenGetter(async () => getItem(TOKEN_KEY));
  }, []);

  const resolveUser = useCallback((data: unknown): User => {
    const d = data as { Id?: string; id?: string; Name?: string; name?: string; Email?: string; email?: string; Phone?: string; phone?: string; Roles?: string[]; roles?: string[] };
    return {
      id: String(d.Id ?? d.id ?? ''),
      name: d.Name ?? d.name ?? '',
      email: d.Email ?? d.email ?? '',
      phone: d.Phone ?? d.phone,
      roles: d.Roles ?? d.roles ?? [],
    };
  }, []);

  const fetchUser = useCallback(async () => {
    try {
      const u = await getCurrentUser();
      setUser(resolveUser(u));
      try {
        const list = await getServiceInstallers();
        const myId = (u as User).id ?? (u as { Id?: string }).Id;
        const si = list.find(
          (s) => String(s.userId ?? (s as { UserId?: string }).UserId) === String(myId)
        );
        if (si) {
          setServiceInstaller(si);
        }
      } catch {
        // ignore
      }
    } catch {
      await removeItem(TOKEN_KEY);
      await removeItem(REFRESH_KEY);
      setUser(null);
      setServiceInstaller(null);
    } finally {
      setLoading(false);
    }
  }, [resolveUser]);

  useEffect(() => {
    getItem(TOKEN_KEY).then((token) => {
      if (token) fetchUser();
      else setLoading(false);
    });
  }, [fetchUser]);

  const login = useCallback(
    async (email: string, password: string): Promise<{ success: boolean; error?: string }> => {
      try {
        const res = (await apiLogin(email, password)) as LoginResponse;
        const token = res.AccessToken ?? res.accessToken;
        const refresh = res.RefreshToken ?? res.refreshToken;
        const userData = res.User ?? res.user;
        if (!token || !userData) {
          return { success: false, error: 'Invalid response from server' };
        }
        await setItem(TOKEN_KEY, token);
        if (refresh) await setItem(REFRESH_KEY, refresh);
        setUser(resolveUser(userData));
        try {
          const list = await getServiceInstallers();
          const myId = (userData as { id?: string; Id?: string }).id ?? (userData as { Id?: string }).Id;
          const si = list.find(
            (s) => String(s.userId ?? (s as { UserId?: string }).UserId) === String(myId)
          );
          if (si) setServiceInstaller(si);
        } catch {
          // ignore
        }
        return { success: true };
      } catch (e: unknown) {
        const message = e instanceof Error ? e.message : 'Login failed';
        return { success: false, error: message };
      }
    },
    [resolveUser]
  );

  const logout = useCallback(async () => {
    await removeItem(TOKEN_KEY);
    await removeItem(REFRESH_KEY);
    setUser(null);
    setServiceInstaller(null);
  }, []);

  const siId = serviceInstaller?.id ?? (serviceInstaller as { Id?: string })?.Id ?? null;

  const value: AuthContextType = {
    user,
    serviceInstaller,
    loading,
    login,
    logout,
    refreshUser: fetchUser,
    isAuthenticated: !!user,
    siId: siId ? String(siId) : null,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextType {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
