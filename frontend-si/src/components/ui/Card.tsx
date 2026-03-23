import React, { ReactNode } from 'react';
import { cn } from '../../lib/utils';

interface CardProps extends Omit<React.HTMLAttributes<HTMLDivElement>, 'title'> {
  children: React.ReactNode;
  /** Optional card title (shown above content) */
  title?: string;
  /** Optional subtitle below title */
  subtitle?: string;
  /** Optional footer (e.g. actions); shown in a bordered section at bottom */
  footer?: ReactNode;
}

export function Card({ children, title, subtitle, footer, className, ...props }: CardProps) {
  return (
    <div
      className={cn(
        'rounded-lg border bg-card text-card-foreground shadow-sm',
        className
      )}
      {...props}
    >
      {(title || subtitle) && (
        <div className="p-3 md:p-4 pb-2 border-b border-border">
          {title && (
            <h3 className="text-sm font-semibold leading-none tracking-tight text-foreground">
              {title}
            </h3>
          )}
          {subtitle && (
            <p className="text-xs text-muted-foreground mt-0.5">
              {subtitle}
            </p>
          )}
        </div>
      )}
      <div className={cn('p-3 md:p-4', (title || subtitle) && 'pt-3')}>
        {children}
      </div>
      {footer != null && footer !== false && (
        <div className="flex items-center border-t border-border p-3 md:p-4 pt-2">
          {footer}
        </div>
      )}
    </div>
  );
}
