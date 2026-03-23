import { Loader2 } from 'lucide-react';
import { cn } from '../../lib/utils';

/** Loading rule: Use when layout is unknown or a simple centered spinner is preferred. Use Skeleton when the final layout is known (list, table, cards). */
interface LoadingSpinnerProps {
  size?: 'sm' | 'default' | 'lg' | 'md';
  message?: string;
  fullPage?: boolean;
  className?: string;
}

export function LoadingSpinner({
  size = 'default',
  message = 'Loading...',
  fullPage = false,
  className,
}: LoadingSpinnerProps) {
  const resolvedSize = size === 'md' ? 'default' : size;
  const sizeClasses: Record<string, string> = {
    sm: 'h-4 w-4',
    default: 'h-8 w-8',
    lg: 'h-12 w-12',
  };
  const containerStyles = fullPage
    ? 'fixed inset-0 flex flex-col items-center justify-center bg-background/80 backdrop-blur-sm z-50'
    : 'flex flex-col items-center justify-center p-8';
  return (
    <div className={cn(containerStyles, className)}>
      <Loader2 className={cn('animate-spin text-primary', sizeClasses[resolvedSize])} />
      {message && <p className="mt-4 text-sm text-muted-foreground">{message}</p>}
    </div>
  );
}

