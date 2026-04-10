import React from 'react';
import { User } from 'lucide-react';
import { cn } from '../../lib/utils';
import type { ServiceInstaller } from '../../types/serviceInstallers';

export interface InstallerColumnHeaderProps {
  installer: ServiceInstaller;
  jobCount: number;
  availabilitySummary?: string;
  workloadLevel?: 'free' | 'medium' | 'overloaded';
  isCompact?: boolean;
  className?: string;
}

function getInitials(name: string): string {
  return name
    .trim()
    .split(/\s+/)
    .map((s) => s[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

const WORKLOAD_DOT = {
  free: 'bg-emerald-500',
  medium: 'bg-amber-500',
  overloaded: 'bg-red-500',
} as const;

const WORKLOAD_RING = {
  free: 'ring-emerald-500/30',
  medium: 'ring-amber-500/30',
  overloaded: 'ring-red-500/30',
} as const;

const WORKLOAD_LABEL = {
  free: 'text-emerald-600 dark:text-emerald-400',
  medium: 'text-amber-600 dark:text-amber-400',
  overloaded: 'text-red-600 dark:text-red-400',
} as const;

const InstallerColumnHeader: React.FC<InstallerColumnHeaderProps> = ({
  installer,
  jobCount,
  availabilitySummary,
  workloadLevel = 'free',
  isCompact = false,
  className,
}) => {
  const initials = getInitials(installer.name);
  const badge = installer.siLevel || (installer.installerType === 'Subcontractor' ? 'Subcon' : undefined);

  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center border-b bg-muted/30 px-2 py-2 text-center',
        isCompact && 'py-1.5',
        className
      )}
    >
      <div className="flex items-center gap-2 w-full justify-center">
        <div className="relative shrink-0">
          <div
            className={cn(
              'flex items-center justify-center rounded-full bg-primary/15 text-primary font-semibold',
              isCompact ? 'h-7 w-7 text-xs' : 'h-8 w-8 text-sm'
            )}
          >
            {initials ? initials : <User className={isCompact ? 'h-3.5 w-3.5' : 'h-4 w-4'} />}
          </div>
          <div
            className={cn(
              'absolute -bottom-0.5 -right-0.5 h-3 w-3 rounded-full border-2 border-background ring-2',
              WORKLOAD_DOT[workloadLevel],
              WORKLOAD_RING[workloadLevel]
            )}
            title={`${workloadLevel} - ${jobCount} jobs`}
          />
        </div>
        <div className="min-w-0 flex-1">
          <div className="truncate text-sm font-medium text-foreground">{installer.name}</div>
          {badge && (
            <span className="inline-block truncate text-xs text-muted-foreground">{badge}</span>
          )}
        </div>
      </div>
      <div className="mt-1 flex items-center justify-between w-full gap-1 text-xs">
        <span className="truncate text-muted-foreground" title={availabilitySummary}>
          {availabilitySummary ?? '—'}
        </span>
        <span
          className={cn(
            'shrink-0 font-semibold tabular-nums',
            WORKLOAD_LABEL[workloadLevel]
          )}
        >
          {jobCount} {jobCount === 1 ? 'job' : 'jobs'}
        </span>
      </div>
    </div>
  );
};

export default InstallerColumnHeader;
