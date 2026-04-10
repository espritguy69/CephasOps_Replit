import React from 'react';
import { Briefcase, Clock, AlertTriangle, Users, Zap } from 'lucide-react';
import { cn } from '../../lib/utils';
import type { InstallerWorkload } from '../../lib/scheduler/scoringEngine';

interface SchedulerSummaryBarProps {
  totalJobs: number;
  unassignedCount: number;
  overloadedInstallers: number;
  totalInstallers: number;
  avgUtilization: number;
  className?: string;
}

const SchedulerSummaryBar: React.FC<SchedulerSummaryBarProps> = ({
  totalJobs,
  unassignedCount,
  overloadedInstallers,
  totalInstallers,
  avgUtilization,
  className,
}) => {
  return (
    <div
      className={cn(
        'flex items-center gap-1 px-3 py-2 bg-muted/30 border rounded-lg text-sm flex-wrap',
        className
      )}
    >
      <SummaryChip
        icon={<Briefcase className="h-3.5 w-3.5" />}
        label="Total Jobs"
        value={totalJobs}
        className="text-foreground"
      />
      <Divider />
      <SummaryChip
        icon={<Clock className="h-3.5 w-3.5" />}
        label="Unassigned"
        value={unassignedCount}
        className={unassignedCount > 0 ? 'text-amber-600 dark:text-amber-400' : 'text-foreground'}
        highlight={unassignedCount > 0}
      />
      <Divider />
      <SummaryChip
        icon={<AlertTriangle className="h-3.5 w-3.5" />}
        label="Overloaded"
        value={overloadedInstallers}
        className={overloadedInstallers > 0 ? 'text-red-600 dark:text-red-400' : 'text-foreground'}
        highlight={overloadedInstallers > 0}
      />
      <Divider />
      <SummaryChip
        icon={<Users className="h-3.5 w-3.5" />}
        label="Installers"
        value={totalInstallers}
        className="text-foreground"
      />
      <Divider />
      <div className="flex items-center gap-2 px-2 py-1">
        <Zap className="h-3.5 w-3.5 text-muted-foreground" />
        <span className="text-xs text-muted-foreground">Avg Load</span>
        <div className="flex items-center gap-1.5">
          <div className="w-16 h-2 bg-muted rounded-full overflow-hidden">
            <div
              className={cn(
                'h-full rounded-full transition-all duration-300',
                avgUtilization >= 85 ? 'bg-red-500' : avgUtilization >= 50 ? 'bg-amber-500' : 'bg-emerald-500'
              )}
              style={{ width: `${Math.min(avgUtilization, 100)}%` }}
            />
          </div>
          <span
            className={cn(
              'text-xs font-semibold tabular-nums',
              avgUtilization >= 85
                ? 'text-red-600 dark:text-red-400'
                : avgUtilization >= 50
                  ? 'text-amber-600 dark:text-amber-400'
                  : 'text-emerald-600 dark:text-emerald-400'
            )}
          >
            {avgUtilization}%
          </span>
        </div>
      </div>
    </div>
  );
};

function SummaryChip({
  icon,
  label,
  value,
  className,
  highlight,
}: {
  icon: React.ReactNode;
  label: string;
  value: number;
  className?: string;
  highlight?: boolean;
}) {
  return (
    <div
      className={cn(
        'flex items-center gap-1.5 px-2 py-1 rounded-md transition-colors',
        highlight && 'bg-background shadow-sm',
        className
      )}
    >
      <span className="text-muted-foreground">{icon}</span>
      <span className="text-xs text-muted-foreground hidden sm:inline">{label}</span>
      <span className="text-sm font-semibold tabular-nums">{value}</span>
    </div>
  );
}

function Divider() {
  return <div className="h-4 w-px bg-border mx-0.5 hidden sm:block" />;
}

export function computeSummaryStats(
  workloads: InstallerWorkload[],
  totalJobs: number,
  unassignedCount: number
) {
  const overloadedInstallers = workloads.filter((w) => w.level === 'overloaded').length;
  const avgUtilization =
    workloads.length > 0
      ? Math.round(workloads.reduce((sum, w) => sum + w.utilizationPct, 0) / workloads.length)
      : 0;
  return {
    totalJobs,
    unassignedCount,
    overloadedInstallers,
    totalInstallers: workloads.length,
    avgUtilization,
  };
}

export default SchedulerSummaryBar;
