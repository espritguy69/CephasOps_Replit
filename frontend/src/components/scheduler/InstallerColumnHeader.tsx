import React from 'react';
import { User } from 'lucide-react';
import { cn } from '../../lib/utils';
import type { ServiceInstaller } from '../../types/serviceInstallers';

export interface InstallerColumnHeaderProps {
  installer: ServiceInstaller;
  jobCount: number;
  availabilitySummary?: string;
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

/**
 * Installer column header: avatar/initials, name, optional badge, availability summary, job count.
 */
const InstallerColumnHeader: React.FC<InstallerColumnHeaderProps> = ({
  installer,
  jobCount,
  availabilitySummary,
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
        <div
          className={cn(
            'flex shrink-0 items-center justify-center rounded-full bg-primary/15 text-primary font-semibold',
            isCompact ? 'h-7 w-7 text-xs' : 'h-8 w-8 text-sm'
          )}
        >
          {initials ? initials : <User className={isCompact ? 'h-3.5 w-3.5' : 'h-4 w-4'} />}
        </div>
        <div className="min-w-0 flex-1">
          <div className="truncate text-sm font-medium text-foreground">{installer.name}</div>
          {badge && (
            <span className="inline-block truncate text-xs text-muted-foreground">{badge}</span>
          )}
        </div>
      </div>
      <div className="mt-1 flex items-center justify-between w-full gap-1 text-xs text-muted-foreground">
        <span className="truncate" title={availabilitySummary}>
          {availabilitySummary ?? '—'}
        </span>
        <span className="shrink-0 font-medium tabular-nums">{jobCount}</span>
      </div>
    </div>
  );
};

export default InstallerColumnHeader;
