import React, { createContext, useContext, useEffect, useMemo, useState, useCallback, useRef, ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { getDepartments } from '../api/departments';
import { setDepartmentGetter, setCompanyIdGetter } from '../api/client';
import { useAuth } from './AuthContext';
import type { Department } from '../types/departments';

interface DepartmentContextType {
  departments: Department[];
  activeDepartment: Department | null;
  departmentId: string | null;
  selectDepartment: (departmentId: string) => void;
  refreshDepartments: () => Promise<void>;
  loading: boolean;
  error: string | null;
  landingPage: string;
  setLandingPage: (route: string) => void;
}

const DepartmentContext = createContext<DepartmentContextType | null>(null);

const STORAGE_KEYS = {
  activeDepartment: 'cephasops.activeDepartmentId',
  landingPage: 'cephasops.landingPageRoute'
} as const;

const DEFAULT_LANDING_PAGE = '/dashboard';

export const useDepartment = (): DepartmentContextType => {
  const context = useContext(DepartmentContext);
  if (!context) {
    throw new Error('useDepartment must be used within a DepartmentProvider');
  }
  return context;
};

interface DepartmentProviderProps {
  children: ReactNode;
}

export const DepartmentProvider: React.FC<DepartmentProviderProps> = ({ children }) => {
  const { isAuthenticated } = useAuth();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [loading, setLoading] = useState<boolean>(true);  // Start as true to prevent premature data fetches
  const [error, setError] = useState<string | null>(null);
  const [activeDepartmentId, setActiveDepartmentId] = useState<string>(
    localStorage.getItem(STORAGE_KEYS.activeDepartment) || ''
  );
  const [landingPage, setLandingPage] = useState<string>(
    localStorage.getItem(STORAGE_KEYS.landingPage) || DEFAULT_LANDING_PAGE
  );
  
  // Use ref to track current activeDepartmentId without causing dependency issues
  const activeDepartmentIdRef = useRef<string>(activeDepartmentId);
  activeDepartmentIdRef.current = activeDepartmentId;

  // Ref for effective company ID (SaaS: sent as X-Company-Id so SuperAdmin company switch works)
  const companyIdRef = useRef<string | null>(null);
  
  // Track if initial load is complete
  const hasLoadedRef = useRef<boolean>(false);

  const activeDepartment = useMemo<Department | null>(() => {
    if (!departments?.length) return null;
    return departments.find((dept) => dept.id === activeDepartmentId) || null;
  }, [departments, activeDepartmentId]);

  companyIdRef.current = activeDepartment?.companyId ?? departments[0]?.companyId ?? null;

  const persistDepartmentId = useCallback((departmentId: string) => {
    if (departmentId) {
      localStorage.setItem(STORAGE_KEYS.activeDepartment, departmentId);
    } else {
      localStorage.removeItem(STORAGE_KEYS.activeDepartment);
    }
    setActiveDepartmentId(departmentId || '');
  }, []);

  const persistLandingPage = useCallback((route: string) => {
    const value = route || DEFAULT_LANDING_PAGE;
    setLandingPage(value);
    localStorage.setItem(STORAGE_KEYS.landingPage, value);
  }, []);

  // refreshDepartments no longer depends on activeDepartmentId (uses ref instead)
  const refreshDepartments = useCallback(async () => {
    if (!isAuthenticated) {
      setDepartments([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const data = await getDepartments({ isActive: true });
      const list = Array.isArray(data) ? data : [];
      setDepartments(list);

      if (list.length > 0) {
        // Use ref to get current value without adding to dependencies
        const currentActiveDeptId = activeDepartmentIdRef.current;
        const stillValid = list.some((dept) => dept.id === currentActiveDeptId);
        
        if (!stillValid) {
          // Prioritize GPON department for default selection
          const gponDepartment = list.find(
            (dept) => dept.code === 'GPON' || dept.name === 'GPON' || dept.name?.toUpperCase().includes('GPON')
          );
          
          if (gponDepartment) {
            // Auto-select GPON as default (main department for this app)
            persistDepartmentId(gponDepartment.id);
          } else {
            // Fallback to first department if GPON not found
            persistDepartmentId(list[0].id);
          }
        }
        // If stillValid is true, keep the existing activeDepartmentId
      } else {
        persistDepartmentId('');
      }
      
      hasLoadedRef.current = true;
    } catch (err) {
      const error = err as Error;
      console.error('Failed to load departments', err);
      setDepartments([]);
      setError(error.message || 'Failed to load departments');
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated, persistDepartmentId]); // Removed activeDepartmentId from deps

  useEffect(() => {
    if (!isAuthenticated) {
      setDepartments([]);
      setLoading(false);
      hasLoadedRef.current = false;
      return;
    }
    
    // Only load once per auth session
    if (!hasLoadedRef.current) {
      refreshDepartments();
    }
  }, [isAuthenticated, refreshDepartments]);

  useEffect(() => {
    setDepartmentGetter(() => activeDepartment);
    setCompanyIdGetter(() => () => companyIdRef.current);
    return () => {
      setCompanyIdGetter(null);
    };
  }, [activeDepartment]);

  const queryClient = useQueryClient();
  const prevActiveDepartmentIdRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    if (!isAuthenticated) return;
    const prev = prevActiveDepartmentIdRef.current;
    prevActiveDepartmentIdRef.current = activeDepartmentId;
    if (prev !== undefined && prev !== activeDepartmentId) {
      queryClient.invalidateQueries();
    }
  }, [isAuthenticated, activeDepartmentId, queryClient]);

  const contextValue = useMemo<DepartmentContextType>(() => ({
    departments,
    activeDepartment,
    departmentId: activeDepartment?.id || null,
    selectDepartment: persistDepartmentId,
    refreshDepartments,
    loading,
    error,
    landingPage,
    setLandingPage: persistLandingPage
  }), [
    departments,
    activeDepartment,
    persistDepartmentId,
    refreshDepartments,
    loading,
    error,
    landingPage,
    persistLandingPage
  ]);

  return (
    <DepartmentContext.Provider value={contextValue}>
      {children}
    </DepartmentContext.Provider>
  );
};

