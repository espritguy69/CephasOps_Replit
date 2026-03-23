import React, { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { LoadingSpinner } from '../ui/LoadingSpinner';

interface SubconRouteProps {
  children: ReactNode;
}

export function SubconRoute({ children }: SubconRouteProps) {
  const { isSubcontractor, loading } = useAuth();

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <LoadingSpinner />
      </div>
    );
  }

  if (!isSubcontractor) {
    // Redirect non-subcontractors to the jobs list
    return <Navigate to="/jobs" replace />;
  }

  return <>{children}</>;
}

