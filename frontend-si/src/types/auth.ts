import type { User, ServiceInstaller } from './api';

export interface AuthContextType {
  user: User | null;
  serviceInstaller: ServiceInstaller | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  logout: () => void;
  isAuthenticated: boolean;
  isSubcontractor: boolean;
  isAdmin: boolean;
}

