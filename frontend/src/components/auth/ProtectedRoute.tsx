import React, { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';

/**
 * ProtectedRoute component
 * Wraps routes that require authentication
 * Redirects to login if user is not authenticated
 */
interface ProtectedRouteProps {
  children: ReactNode;
  requiredPermission?: string | null;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredPermission = null }) => {
  const { isAuthenticated, loading, user } = useAuth();

  // Show loading state while checking authentication
  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Check permission if required
  // Current RBAC model: SuperAdmin/Director/HeadOfDepartment/Supervisor have elevated access
  // Permissions are implied by roles - granular permissions can be added when backend supports it
  if (requiredPermission && user) {
    const userRoles = user.roles || [];
    
    // SuperAdmin, Director, HeadOfDepartment, and Supervisor have elevated access
    const hasElevatedAccess = userRoles.some(r => 
      r === 'SuperAdmin' || r === 'Director' || r === 'HeadOfDepartment' || r === 'Supervisor'
    );
    
    if (!hasElevatedAccess) {
      // For now, any authenticated user with a role can access routes
      // This can be refined when backend implements granular permissions
      const hasRole = userRoles.length > 0;
      
      if (!hasRole) {
        return (
          <div className="flex items-center justify-center min-h-screen">
            <div className="text-center">
              <h1 className="text-2xl font-bold text-red-600 mb-2">Access Denied</h1>
              <p className="text-gray-600">You don't have permission to access this page.</p>
            </div>
          </div>
        );
      }
    }
  }

  return <>{children}</>;
};

export default ProtectedRoute;

