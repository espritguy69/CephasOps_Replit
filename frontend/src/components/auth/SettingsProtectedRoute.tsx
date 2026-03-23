import React, { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';

/**
 * SettingsProtectedRoute component
 * Protects all /settings routes - only SuperAdmin, Director, HeadOfDepartment, and Supervisor can access
 */
interface SettingsProtectedRouteProps {
  children: ReactNode;
}

const SettingsProtectedRoute: React.FC<SettingsProtectedRouteProps> = ({ children }) => {
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

  // Check if user has access to settings (RBAC v2: settings.view or legacy role)
  if (user) {
    const userRoles = user.roles || [];
    const userPermissions = user.permissions || [];
    const hasSettingsPermission = userPermissions.includes('settings.view');
    const hasLegacyRole = userRoles.some(r =>
      r === 'SuperAdmin' || r === 'Admin' || r === 'Director' || r === 'HeadOfDepartment' || r === 'Supervisor'
    );
    const hasSettingsAccess = hasSettingsPermission || (userPermissions.length === 0 && hasLegacyRole);

    if (!hasSettingsAccess) {
      return (
        <div className="flex items-center justify-center min-h-screen">
          <div className="text-center">
            <h1 className="text-2xl font-bold text-red-600 mb-2">Access Denied</h1>
            <p className="text-gray-600 mb-4">You don't have permission to access Settings.</p>
            <p className="text-sm text-muted-foreground">
              You need the settings.view permission or an allowed role (SuperAdmin, Admin, Director, HeadOfDepartment, Supervisor).
            </p>
            <button
              onClick={() => window.history.back()}
              className="mt-4 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
            >
              Go Back
            </button>
          </div>
        </div>
      );
    }
  }

  return <>{children}</>;
};

export default SettingsProtectedRoute;

