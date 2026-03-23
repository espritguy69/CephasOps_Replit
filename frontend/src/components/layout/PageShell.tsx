import React, { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { ChevronRight, Home } from 'lucide-react';
import { cn } from '@/lib/utils';
import { designTokens } from '@/lib/design-tokens';

interface Breadcrumb {
  label: string;
  path?: string;
}

interface PageShellProps {
  title?: string;
  breadcrumbs?: Breadcrumb[];
  actions?: ReactNode;
  children: ReactNode;
  className?: string;
  compact?: boolean;
}

/**
 * PageShell - Standard page wrapper with title, breadcrumbs, and actions
 * 
 * Provides consistent layout structure for all pages with macOS-inspired styling
 */
const PageShell: React.FC<PageShellProps> = ({ 
  title, 
  breadcrumbs = [], 
  actions,
  children,
  className,
  compact = false 
}) => {
  return (
    <div className={cn('flex flex-col h-full', className)}>
      {/* Header Section */}
      {(title || breadcrumbs.length > 0 || actions) && (
        <div className={cn(
          'flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 sm:gap-0',
          compact ? 'px-3 py-2 md:px-4 md:py-3' : 'px-3 py-2 md:px-4 md:py-3 lg:px-6 lg:py-4',
          'border-b border-border bg-card/50'
        )}>
          {/* Left: Breadcrumbs + Title */}
          <div className="flex items-center gap-2 min-w-0 flex-1">
            {breadcrumbs.length > 0 && (
              <nav className="flex items-center gap-1.5 text-[10px] md:text-xs text-muted-foreground">
                <Link 
                  to="/dashboard" 
                  className="hover:text-foreground transition-colors flex items-center min-h-[44px] min-w-[44px] justify-center"
                >
                  <Home className="h-3 w-3" />
                </Link>
                {breadcrumbs.map((crumb, index) => (
                  <React.Fragment key={index}>
                    <ChevronRight className="h-3 w-3 flex-shrink-0" />
                    {crumb.path ? (
                      <Link 
                        to={crumb.path}
                        className="hover:text-foreground transition-colors truncate"
                      >
                        {crumb.label}
                      </Link>
                    ) : (
                      <span className="text-foreground font-medium truncate">
                        {crumb.label}
                      </span>
                    )}
                  </React.Fragment>
                ))}
              </nav>
            )}
            {title && (
              <h1 className={cn(
                designTokens.typography.pageTitle,
                breadcrumbs.length > 0 && 'ml-2',
                'truncate text-sm md:text-base lg:text-lg'
              )}>
                {title}
              </h1>
            )}
          </div>
          
          {/* Right: Actions */}
          {actions && (
            <div className="flex items-center gap-2 sm:ml-4 flex-wrap">
              {actions}
            </div>
          )}
        </div>
      )}
      
      {/* Content Area */}
      <div className={cn(
        'flex-1 overflow-y-auto',
        compact ? 'p-3 md:p-4' : 'p-3 md:p-4 lg:p-6'
      )}>
        {children}
      </div>
    </div>
  );
};

export default PageShell;

