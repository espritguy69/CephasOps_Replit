import React, { ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface StatusBadgeProps {
  status?: string;
  variant?: 'default' | 'success' | 'error' | 'warning' | 'info' | 'secondary';
  size?: 'sm' | 'default' | 'lg';
  className?: string;
  children?: ReactNode;
}

const StatusBadge: React.FC<StatusBadgeProps> = ({ 
  status, 
  variant = 'default',
  size = 'default',
  className = '',
  children
}) => {
  const baseStyles = "inline-flex items-center rounded-full font-medium transition-colors";
  
  const variantStyles: Record<string, string> = {
    default: "bg-gray-100 text-gray-800",
    success: "bg-green-100 text-green-800",
    error: "bg-red-100 text-red-800",
    warning: "bg-yellow-100 text-yellow-800",
    info: "bg-brand-100 text-brand-800",
    secondary: "bg-gray-100 text-gray-600"
  };
  
  const sizeStyles: Record<string, string> = {
    sm: "px-1.5 py-0.5 text-xs",
    default: "px-2 py-0.5 text-xs",
    lg: "px-2.5 py-0.5 text-xs"
  };

  return (
    <span className={cn(baseStyles, variantStyles[variant], sizeStyles[size], className)}>
      {children || status}
    </span>
  );
};

export default StatusBadge;

