import React, { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import Card from '../ui/Card';
import { designTokens } from '@/lib/design-tokens';

interface SectionCardProps {
  title?: string;
  subtitle?: string;
  children: ReactNode;
  className?: string;
  compact?: boolean;
  headerActions?: ReactNode;
  footer?: ReactNode;
  variant?: 'default' | 'outlined' | 'elevated';
}

/**
 * SectionCard - Compact card for grouping related form fields or content
 * 
 * macOS-inspired section grouping with consistent spacing and styling
 */
const SectionCard: React.FC<SectionCardProps> = ({ 
  title,
  subtitle,
  children,
  className,
  compact = true,
  headerActions,
  footer,
  variant = 'default'
}) => {
  return (
    <Card 
      variant={variant}
      className={cn(
        compact ? 'p-3' : 'p-4',
        className
      )}
    >
      {/* Header */}
      {(title || subtitle || headerActions) && (
        <div className="flex items-start justify-between mb-3">
          <div className="flex-1 min-w-0">
            {title && (
              <h3 className={cn(
                designTokens.typography.sectionHeader,
                'mb-0.5'
              )}>
                {title}
              </h3>
            )}
            {subtitle && (
              <p className={cn(designTokens.typography.helper, 'mt-0.5')}>
                {subtitle}
              </p>
            )}
          </div>
          {headerActions && (
            <div className="ml-2 flex items-center gap-1">
              {headerActions}
            </div>
          )}
        </div>
      )}
      
      {/* Content */}
      <div className={cn(
        compact ? designTokens.spacing.compact : designTokens.spacing.standard,
        'flex flex-col'
      )}>
        {children}
      </div>
      
      {/* Footer */}
      {footer && (
        <div className="mt-3 pt-3 border-t border-border">
          {footer}
        </div>
      )}
    </Card>
  );
};

export default SectionCard;

