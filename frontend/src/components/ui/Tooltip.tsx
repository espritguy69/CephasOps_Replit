import React, { useState, useRef, useEffect, ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { cn } from '@/lib/utils';

type TooltipSide = 'top' | 'bottom' | 'left' | 'right';

interface TooltipProps {
  children: ReactNode;
  content?: ReactNode;
  side?: TooltipSide;
  className?: string;
}

/**
 * Tooltip component - shadcn style
 * Simple tooltip that appears on hover
 */
const Tooltip: React.FC<TooltipProps> = ({ children, content, side = 'top', className }) => {
  const [isVisible, setIsVisible] = useState<boolean>(false);
  const [position, setPosition] = useState<{ top: number; left: number }>({ top: 0, left: 0 });
  const triggerRef = useRef<HTMLDivElement>(null);
  const tooltipRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isVisible && triggerRef.current && tooltipRef.current) {
      const updatePosition = (): void => {
        if (!triggerRef.current || !tooltipRef.current) return;
        
        const triggerRect = triggerRef.current.getBoundingClientRect();
        const tooltipRect = tooltipRef.current.getBoundingClientRect();
        
        let top = 0;
        let left = 0;

        switch (side) {
          case 'top':
            top = triggerRect.top - tooltipRect.height - 6;
            left = triggerRect.left + (triggerRect.width / 2) - (tooltipRect.width / 2);
            break;
          case 'bottom':
            top = triggerRect.bottom + 6;
            left = triggerRect.left + (triggerRect.width / 2) - (tooltipRect.width / 2);
            break;
          case 'left':
            top = triggerRect.top + (triggerRect.height / 2) - (tooltipRect.height / 2);
            left = triggerRect.left - tooltipRect.width - 6;
            break;
          case 'right':
            top = triggerRect.top + (triggerRect.height / 2) - (tooltipRect.height / 2);
            left = triggerRect.right + 6;
            break;
        }

        // Keep tooltip within viewport
        const padding = 8;
        if (left < padding) left = padding;
        if (left + tooltipRect.width > window.innerWidth - padding) {
          left = window.innerWidth - tooltipRect.width - padding;
        }
        if (top < padding) top = padding;
        if (top + tooltipRect.height > window.innerHeight - padding) {
          top = window.innerHeight - tooltipRect.height - padding;
        }

        setPosition({ top, left });
      };

      updatePosition();
      window.addEventListener('scroll', updatePosition, true);
      window.addEventListener('resize', updatePosition);

      return () => {
        window.removeEventListener('scroll', updatePosition, true);
        window.removeEventListener('resize', updatePosition);
      };
    }
  }, [isVisible, side]);

  if (!content) return <>{children}</>;

  return (
    <>
      <div
        ref={triggerRef}
        onMouseEnter={() => setIsVisible(true)}
        onMouseLeave={() => setIsVisible(false)}
        className="inline-block"
      >
        {children}
      </div>
      {isVisible && typeof document !== 'undefined' && createPortal(
        <div
          ref={tooltipRef}
          className={cn(
            "fixed z-50 px-2 py-1 text-xs font-medium text-white bg-slate-900 dark:bg-slate-800 rounded-md shadow-lg pointer-events-none",
            className
          )}
          style={{
            top: `${position.top}px`,
            left: `${position.left}px`
          }}
        >
          {content}
          <div
            className={cn(
              "absolute w-2 h-2 bg-slate-900 dark:bg-slate-800 rotate-45",
              side === 'top' && "bottom-[-4px] left-1/2 -translate-x-1/2",
              side === 'bottom' && "top-[-4px] left-1/2 -translate-x-1/2",
              side === 'left' && "right-[-4px] top-1/2 -translate-y-1/2",
              side === 'right' && "left-[-4px] top-1/2 -translate-y-1/2"
            )}
          />
        </div>,
        document.body
      )}
    </>
  );
};

/**
 * TooltipProvider - Wrapper for tooltip positioning context
 */
interface TooltipProviderProps {
  children: ReactNode;
}

export const TooltipProvider: React.FC<TooltipProviderProps> = ({ children }) => {
  return <>{children}</>;
};

export default Tooltip;

