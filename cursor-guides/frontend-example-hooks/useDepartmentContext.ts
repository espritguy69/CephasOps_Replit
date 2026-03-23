import React, { 
  createContext, 
  useContext, 
  useEffect, 
  useMemo, 
  useState, 
  useCallback, 
  useRef 
} from 'react';
import { getDepartments } from '../api/departments';
import { setDepartmentGetter } from '../api/client';
import { useAuth } from './AuthContext';

/**
 * PATTERN: Department Context for Multi-Department Data Isolation
 * 
 * Key conventions:
 * - Start with loading: true to prevent premature data fetches
 * - Auto-select default department (GPON) on login
 * - Use refs to avoid dependency loops in callbacks
 * - Persist active department in localStorage
 * - Expose departmentId for use in API calls
 */

// ==================== TYPES ====================

interface Department {
  id: string;
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
}

interface DepartmentContextValue {
  departments: Department[];
  activeDepartment: Department | null;
  departmentId: string | null;
  selectDepartment: (departmentId: string) => void;
  refreshDepartments: () => Promise<void>;
  loading: boolean;
  error: string | null;
}

// ==================== CONSTANTS ====================

const STORAGE_KEY = 'cephasops.activeDepartmentId';

// ==================== CONTEXT ====================

const DepartmentContext = createContext<DepartmentContextValue | null>(null);

/**
 * PATTERN: Custom hook to access department context
 * - Throws if used outside provider
 */
export const useDepartment = (): DepartmentContextValue => {
  const context = useContext(DepartmentContext);
  if (!context) {
    throw new Error('useDepartment must be used within a DepartmentProvider');
  }
  return context;
};

// ==================== PROVIDER ====================

export const DepartmentProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated } = useAuth();
  
  const [departments, setDepartments] = useState<Department[]>([]);
  // IMPORTANT: Start with loading: true to prevent premature data fetches
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeDepartmentId, setActiveDepartmentId] = useState(
    localStorage.getItem(STORAGE_KEY) || ''
  );

  // PATTERN: Use refs to avoid dependency loops
  const activeDepartmentIdRef = useRef(activeDepartmentId);
  activeDepartmentIdRef.current = activeDepartmentId;
  
  const hasLoadedRef = useRef(false);

  // Derived state: active department object
  const activeDepartment = useMemo(() => {
    if (!departments?.length) return null;
    return departments.find((dept) => dept.id === activeDepartmentId) || null;
  }, [departments, activeDepartmentId]);

  // PATTERN: Persist department selection to localStorage
  const persistDepartmentId = useCallback((departmentId: string) => {
    if (departmentId) {
      localStorage.setItem(STORAGE_KEY, departmentId);
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
    setActiveDepartmentId(departmentId || '');
  }, []);

  // PATTERN: Refresh departments with auto-selection
  // NOTE: Uses ref to avoid adding activeDepartmentId to dependencies (prevents loops)
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
        // Use ref to get current value without causing dependency loop
        const currentActiveDeptId = activeDepartmentIdRef.current;
        const stillValid = list.some((dept) => dept.id === currentActiveDeptId);
        
        if (!stillValid) {
          // PATTERN: Auto-select default department (GPON for this app)
          const defaultDepartment = list.find(
            (dept) => 
              dept.code === 'GPON' || 
              dept.name === 'GPON' || 
              dept.name?.toUpperCase().includes('GPON')
          );
          
          if (defaultDepartment) {
            persistDepartmentId(defaultDepartment.id);
            console.log('Auto-selected default department:', defaultDepartment.name);
          } else {
            // Fallback to first department
            persistDepartmentId(list[0].id);
          }
        }
      } else {
        persistDepartmentId('');
      }
      
      hasLoadedRef.current = true;
    } catch (err) {
      console.error('Failed to load departments', err);
      setDepartments([]);
      setError(err instanceof Error ? err.message : 'Failed to load departments');
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated, persistDepartmentId]); // NOTE: No activeDepartmentId here!

  // Load departments when authenticated
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

  // PATTERN: Register department getter with API client
  // This allows apiClient to automatically inject departmentId
  useEffect(() => {
    setDepartmentGetter(() => activeDepartment);
  }, [activeDepartment]);

  // Context value
  const contextValue = useMemo(() => ({
    departments,
    activeDepartment,
    departmentId: activeDepartment?.id || null,
    selectDepartment: persistDepartmentId,
    refreshDepartments,
    loading,
    error,
  }), [
    departments,
    activeDepartment,
    persistDepartmentId,
    refreshDepartments,
    loading,
    error,
  ]);

  return (
    <DepartmentContext.Provider value={contextValue}>
      {children}
    </DepartmentContext.Provider>
  );
};

/**
 * USAGE IN PAGES:
 * 
 * ```tsx
 * const MyPage = () => {
 *   const { departmentId, loading: departmentLoading } = useDepartment();
 *   
 *   // Wait for department to load before fetching data
 *   useEffect(() => {
 *     if (departmentLoading) return;
 *     loadData();
 *   }, [departmentId, departmentLoading]);
 *   
 *   // Or use TanStack Query with enabled flag
 *   const { data } = useQuery({
 *     queryKey: ['myData', departmentId],
 *     queryFn: () => fetchData({ departmentId }),
 *     enabled: !departmentLoading,
 *   });
 * };
 * ```
 */

