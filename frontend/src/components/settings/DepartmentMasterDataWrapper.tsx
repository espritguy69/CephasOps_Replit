import React, { useEffect, useState, useRef } from 'react';
import { Link } from 'react-router-dom';
import { useDepartment } from '../../contexts/DepartmentContext';
import { LoadingSpinner } from '../ui';
import { AlertTriangle, Plus, ArrowRight } from 'lucide-react';

interface DepartmentMasterDataWrapperProps {
  children: React.ReactNode;
  /** The department code to filter by (e.g., 'gpon', 'cwo', 'nwo') */
  departmentCode: string;
}

// Department display names and descriptions
const DEPARTMENT_INFO: Record<string, { name: string; description: string }> = {
  GPON: {
    name: 'GPON (Fibre Installation)',
    description: 'Gigabit Passive Optical Network - Residential and commercial fibre installations'
  },
  CWO: {
    name: 'CWO (Customer Work Orders - Enterprise)',
    description: 'Enterprise customer work orders - Core Pull, Rack Setup, etc.'
  },
  NWO: {
    name: 'NWO (Network Work Orders)',
    description: 'Network infrastructure work orders - Fibre Pull, Chamber, Manhole, etc.'
  }
};

/**
 * Wrapper component that sets the active department context
 * for department-specific master data pages (GPON, CWO, NWO).
 * 
 * IMPORTANT: This wrapper ensures the child component only renders
 * AFTER the correct department context is set. It uses a key to force
 * remount of children when the department changes, ensuring fresh data.
 */
export const DepartmentMasterDataWrapper: React.FC<DepartmentMasterDataWrapperProps> = ({
  children,
  departmentCode
}) => {
  const { departments, selectDepartment, activeDepartment, loading } = useDepartment();
  const [targetDepartment, setTargetDepartment] = useState<{ id: string; name: string; code?: string } | null>(null);
  const [isReady, setIsReady] = useState(false);
  const hasSetDepartment = useRef(false);

  const upperCode = departmentCode.toUpperCase();
  const deptInfo = DEPARTMENT_INFO[upperCode] || { name: upperCode, description: '' };

  useEffect(() => {
    // Reset state when department code changes
    setIsReady(false);
    hasSetDepartment.current = false;
    setTargetDepartment(null);
  }, [departmentCode]);

  useEffect(() => {
    if (loading || !departments.length) return;

    // Find the department by code (case-insensitive)
    const dept = departments.find(
      d => d.code?.toUpperCase() === upperCode ||
           d.name?.toUpperCase() === upperCode
    );

    if (dept) {
      setTargetDepartment({ id: dept.id, name: dept.name, code: dept.code });
      
      // Check if we need to switch department
      const needsSwitch = !activeDepartment || activeDepartment.id !== dept.id;
      
      if (needsSwitch && !hasSetDepartment.current) {
        hasSetDepartment.current = true;
        selectDepartment(dept.id);
        // Don't set ready yet - wait for the context to update
      } else if (!needsSwitch) {
        // Already on the correct department
        setIsReady(true);
      }
    } else {
      // Department not found
      setTargetDepartment(null);
      setIsReady(true);
    }
  }, [departments, upperCode, loading, activeDepartment, selectDepartment]);

  // Watch for activeDepartment changes to know when switch is complete
  useEffect(() => {
    if (targetDepartment && activeDepartment && activeDepartment.id === targetDepartment.id) {
      // Department switch complete - now render children
      setIsReady(true);
    }
  }, [activeDepartment, targetDepartment]);

  if (loading || !isReady) {
    return (
      <div className="flex items-center justify-center h-64">
        <LoadingSpinner size="lg" />
        <span className="ml-3 text-gray-600">Loading {deptInfo.name} data...</span>
      </div>
    );
  }

  // If department not found, show helpful message with action
  if (!targetDepartment) {
    return (
      <div className="p-6 max-w-2xl mx-auto">
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-6">
          <div className="flex items-start gap-4">
            <div className="flex-shrink-0">
              <AlertTriangle className="h-8 w-8 text-amber-500" />
            </div>
            <div className="flex-1">
              <h3 className="text-lg font-semibold text-amber-800">
                {deptInfo.name} Department Not Found
              </h3>
              <p className="mt-2 text-amber-700">
                {deptInfo.description}
              </p>
              <p className="mt-3 text-amber-600 text-sm">
                This department needs to be created before you can manage its master data.
                Each department (GPON, CWO, NWO) has its own separate set of:
              </p>
              <ul className="mt-2 text-amber-600 text-sm list-disc list-inside space-y-1">
                <li>Service Installers</li>
                <li>Rate Engine / Rate Cards</li>
                <li>Order Types</li>
                <li>Installation Types</li>
                <li>Installation Methods</li>
                <li>Splitter Types</li>
              </ul>
              
              <div className="mt-6 flex flex-wrap gap-3">
                <Link
                  to="/settings/department/deployment"
                  className="inline-flex items-center gap-2 px-4 py-2 bg-amber-600 text-white rounded-md hover:bg-amber-700 transition-colors"
                >
                  <Plus className="h-4 w-4" />
                  Deploy {upperCode} Department
                </Link>
                <Link
                  to="/settings/company/departments"
                  className="inline-flex items-center gap-2 px-4 py-2 bg-white border border-amber-300 text-amber-700 rounded-md hover:bg-amber-50 transition-colors"
                >
                  Manage Departments
                  <ArrowRight className="h-4 w-4" />
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Department found and context is set - render children with key to force remount
  // The key ensures that when navigating between GPON/CWO/NWO, the child component
  // completely remounts and fetches fresh data for the new department
  return (
    <React.Fragment key={`dept-${targetDepartment.id}`}>
      {children}
    </React.Fragment>
  );
};

export default DepartmentMasterDataWrapper;

