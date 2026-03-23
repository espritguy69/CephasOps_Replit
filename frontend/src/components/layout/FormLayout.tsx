import React, { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import { designTokens } from '@/lib/design-tokens';

interface FormLayoutProps {
  children: ReactNode;
  columns?: 1 | 2 | 3 | 4;
  gap?: 'tight' | 'compact' | 'standard';
  className?: string;
  fullWidth?: boolean;
}

/**
 * FormLayout - Grid-based form wrapper with consistent spacing
 * 
 * Provides flexible grid layouts for forms with macOS-inspired compact spacing
 */
const FormLayout: React.FC<FormLayoutProps> = ({ 
  children,
  columns = 1,
  gap = 'compact',
  className,
  fullWidth = false
}) => {
  const gridCols: Record<number, string> = {
    1: 'grid-cols-1',
    2: 'grid-cols-1 md:grid-cols-2',
    3: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3',
    4: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-4',
  };
  
  const gapClass = gap === 'tight' 
    ? designTokens.spacing.tight 
    : gap === 'standard' 
    ? designTokens.spacing.standard 
    : designTokens.spacing.compact;
  
  return (
    <div className={cn(
      'grid',
      gridCols[columns] || gridCols[1],
      gapClass,
      fullWidth ? 'w-full' : '',
      className
    )}>
      {children}
    </div>
  );
};

/**
 * FormField - Standardized form field wrapper
 */
interface FormFieldProps {
  label?: string;
  error?: string;
  helper?: string;
  required?: boolean;
  children: ReactNode;
  className?: string;
  labelClassName?: string;
}

export const FormField: React.FC<FormFieldProps> = ({ 
  label,
  error,
  helper,
  required = false,
  children,
  className,
  labelClassName
}) => {
  return (
    <div className={cn('flex flex-col gap-1', className)}>
      {label && (
        <label className={cn(
          designTokens.typography.fieldLabel,
          required && "after:content-['*'] after:ml-0.5 after:text-destructive",
          labelClassName
        )}>
          {label}
        </label>
      )}
      {children}
      {error && (
        <span className={cn(designTokens.typography.helper, 'text-destructive')}>
          {error}
        </span>
      )}
      {helper && !error && (
        <span className={designTokens.typography.helper}>
          {helper}
        </span>
      )}
    </div>
  );
};

/**
 * FormRow - Horizontal row for grouping related fields
 */
interface FormRowProps {
  children: ReactNode;
  gap?: 'tight' | 'compact' | 'standard';
  className?: string;
}

export const FormRow: React.FC<FormRowProps> = ({ 
  children,
  gap = 'compact',
  className
}) => {
  const gapClass = gap === 'tight' 
    ? designTokens.spacing.tight 
    : gap === 'standard' 
    ? designTokens.spacing.standard 
    : designTokens.spacing.compact;
  
  return (
    <div className={cn('flex flex-col sm:flex-row items-start', gapClass, className)}>
      {children}
    </div>
  );
};

export default FormLayout;

