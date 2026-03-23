import { HTMLAttributes } from 'react';
import { cn } from '../../lib/utils';

/**
 * Loading rule: Use Skeleton when the final layout is known (list, table, cards, dashboard).
 * Use LoadingSpinner when layout is unknown or a simple centered spinner is preferred (e.g. full-page modal, async-heavy screen).
 */
export interface SkeletonProps extends HTMLAttributes<HTMLDivElement> {}

function Skeleton({ className, ...props }: SkeletonProps) {
  return (
    <div
      className={cn('animate-pulse rounded-md bg-muted', className)}
      {...props}
    />
  );
}

export { Skeleton };
