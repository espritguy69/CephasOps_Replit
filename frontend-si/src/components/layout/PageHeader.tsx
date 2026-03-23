import React, { ReactNode } from 'react';
import { cn } from '../../lib/utils';

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  className?: string;
}

/**
 * PageHeader - Consistent page title and actions bar (SI app).
 * Aligns with Admin PageShell header style: same typography and padding.
 */
export function PageHeader({ title, subtitle, actions, className }: PageHeaderProps) {
  return (
    <div
      className={cn(
        'flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 border-b border-border bg-card/50 px-3 py-2 md:px-4 md:py-3 lg:px-6 lg:py-4',
        className
      )}
    >
      <div className="min-w-0 flex-1">
        <h1 className="text-base font-semibold text-foreground truncate md:text-lg">
          {title}
        </h1>
        {subtitle && (
          <p className="text-sm text-muted-foreground mt-0.5 truncate">{subtitle}</p>
        )}
      </div>
      {actions && <div className="flex items-center gap-2 flex-shrink-0">{actions}</div>}
    </div>
  );
}
