import React, { ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface CardProps {
  children: ReactNode;
  title?: string;
  subtitle?: string;
  header?: ReactNode;
  footer?: ReactNode;
  onClick?: () => void;
  className?: string;
  hoverable?: boolean;
  variant?: 'default' | 'bordered' | 'elevated' | 'frosted' | 'outlined';
  compact?: boolean;
}

const Card: React.FC<CardProps> = ({
  children,
  title,
  subtitle,
  header,
  footer,
  onClick,
  className = '',
  hoverable = false,
  variant = 'default',
  compact = false
}) => {
  const baseStyles = "rounded-lg border bg-card text-card-foreground shadow-sm transition-smooth";
  const variantStyles: Record<string, string> = {
    default: "border-border",
    bordered: "border-2 border-border",
    elevated: "shadow-md border-border",
    frosted: "frosted-glass border-border/50",
    outlined: "border-border"
  };
  
  const interactiveStyles = onClick || hoverable 
    ? "cursor-pointer hover-lift" 
    : "";

  const paddingClass = compact ? "p-3 md:p-4" : "p-3 md:p-4 lg:p-6";

  const cardContent = (
    <>
      {(title || subtitle || header) && (
        <div className={cn(
          "flex flex-col space-y-0.5",
          compact ? "p-3 md:p-4 pb-2 md:pb-3" : "p-3 md:p-4 lg:p-6 pb-3 md:pb-4 lg:pb-5"
        )}>
          {header || (
            <>
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
            </>
          )}
        </div>
      )}
      <div className={cn(
        paddingClass,
        !(title || subtitle || header) && "pt-0"
      )}>
        {children}
      </div>
      {footer && (
        <div className={cn(
          "flex items-center border-t border-border",
          compact ? "p-3 md:p-4 pt-2 md:pt-3" : "p-3 md:p-4 lg:p-6 pt-3 md:pt-4 lg:pt-5"
        )}>
          {footer}
        </div>
      )}
    </>
  );

  return (
    <div 
      className={cn(
        baseStyles,
        variantStyles[variant],
        interactiveStyles,
        className
      )} 
      onClick={onClick}
      role={onClick ? "button" : undefined}
      tabIndex={onClick ? 0 : undefined}
    >
      {cardContent}
    </div>
  );
};

export default Card;

