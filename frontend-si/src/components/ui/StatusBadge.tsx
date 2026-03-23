import { ReactNode } from 'react';
import { cn } from '../../lib/utils';

interface StatusBadgeProps {
  status?: string;
  variant?: 'default' | 'success' | 'error' | 'warning' | 'info' | 'secondary';
  size?: 'sm' | 'default' | 'lg';
  className?: string;
  children?: ReactNode;
}

/**
 * StatusBadge - Aligned with Admin StatusBadge API for UI consistency.
 */
export function StatusBadge({
  status,
  variant = 'default',
  size = 'default',
  className,
  children,
}: StatusBadgeProps) {
  const variantStyles: Record<string, string> = {
    default: 'bg-gray-100 text-gray-800',
    success: 'bg-green-100 text-green-800',
    error: 'bg-red-100 text-red-800',
    warning: 'bg-yellow-100 text-yellow-800',
    info: 'bg-blue-100 text-blue-800',
    secondary: 'bg-gray-100 text-gray-600',
  };
  const sizeStyles: Record<string, string> = {
    sm: 'px-1.5 py-0.5 text-xs',
    default: 'px-2 py-0.5 text-xs',
    lg: 'px-2.5 py-0.5 text-xs',
  };
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full font-medium',
        variantStyles[variant],
        sizeStyles[size],
        className
      )}
    >
      {children ?? status}
    </span>
  );
}

/** Map order status to StatusBadge variant for SI (aligned with scheduler colors). */
export function getOrderStatusVariant(status: string): StatusBadgeProps['variant'] {
  const s = status || '';
  if (['Completed', 'OrderCompleted'].includes(s)) return 'success';
  if (['Blocker', 'Rejected', 'Overdue', 'Cancelled', 'OrderCancelled'].some((x) => s.includes(x))) return 'error';
  if (['Pending', 'Assigned', 'ReschedulePendingApproval'].includes(s)) return 'warning';
  if (['OnTheWay', 'MetCustomer', 'Installing', 'InProgress'].includes(s)) return 'info';
  return 'secondary';
}
