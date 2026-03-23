import React from 'react';
import { ChevronLeft, ChevronRight, Calendar, RefreshCw, Plus, LayoutGrid, CalendarDays } from 'lucide-react';
import { Button, Select } from '../ui';
import type { Department } from '../../types/departments';
import type { ServiceInstaller } from '../../types/serviceInstallers';
import { cn } from '../../lib/utils';

export type SchedulerViewMode = 'day' | 'week';

export interface SchedulerToolbarProps {
  selectedDate: Date;
  onDateChange: (date: Date) => void;
  onToday: () => void;
  onRefresh: () => void;
  onAssignJob?: () => void;
  viewMode: SchedulerViewMode;
  onViewModeChange: (mode: SchedulerViewMode) => void;
  departments: Department[];
  departmentId: string | null;
  onDepartmentChange: (id: string | null) => void;
  installers: ServiceInstaller[];
  installerId: string | null;
  onInstallerFilterChange: (id: string | null) => void;
  isLoading?: boolean;
  className?: string;
}

/**
 * Scheduler toolbar: date navigation, today, day/week toggle, department/installer filter, assign job, refresh.
 */
const SchedulerToolbar: React.FC<SchedulerToolbarProps> = ({
  selectedDate,
  onDateChange,
  onToday,
  onRefresh,
  onAssignJob,
  viewMode,
  onViewModeChange,
  departments,
  departmentId,
  onDepartmentChange,
  installers,
  installerId,
  onInstallerFilterChange,
  isLoading = false,
  className,
}) => {
  const handlePrev = () => {
    const d = new Date(selectedDate);
    if (viewMode === 'week') d.setDate(d.getDate() - 7);
    else d.setDate(d.getDate() - 1);
    onDateChange(d);
  };

  const handleNext = () => {
    const d = new Date(selectedDate);
    if (viewMode === 'week') d.setDate(d.getDate() + 7);
    else d.setDate(d.getDate() + 1);
    onDateChange(d);
  };

  const dateLabel =
    viewMode === 'week'
      ? (() => {
          const end = new Date(selectedDate);
          end.setDate(end.getDate() + 6);
          return `${selectedDate.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })} – ${end.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}`;
        })()
      : selectedDate.toLocaleDateString('en-US', {
          weekday: 'short',
          month: 'short',
          day: 'numeric',
          year: 'numeric',
        });

  return (
    <div
      className={cn(
        'sticky top-0 z-10 flex flex-wrap items-center gap-2 border-b bg-background px-3 py-2 shadow-sm',
        className
      )}
    >
      <div className="flex items-center gap-1">
        <Button variant="outline" size="sm" onClick={handlePrev} aria-label="Previous">
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <Button variant="outline" size="sm" onClick={onToday}>
          <Calendar className="h-4 w-4 mr-1" />
          Today
        </Button>
        <Button variant="outline" size="sm" onClick={handleNext} aria-label="Next">
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
      <div className="text-base font-semibold text-foreground min-w-[200px]">{dateLabel}</div>

      <div className="flex items-center gap-2 ml-2">
        <Button
          variant={viewMode === 'day' ? 'default' : 'outline'}
          size="sm"
          onClick={() => onViewModeChange('day')}
        >
          <CalendarDays className="h-4 w-4 mr-1" />
          Day
        </Button>
        <Button
          variant={viewMode === 'week' ? 'default' : 'outline'}
          size="sm"
          onClick={() => onViewModeChange('week')}
        >
          <LayoutGrid className="h-4 w-4 mr-1" />
          Week
        </Button>
      </div>

      <Select
        value={departmentId ?? ''}
        onChange={(e) => onDepartmentChange(e.target.value ? e.target.value : null)}
        options={[
          { value: '', label: 'All departments' },
          ...departments.map((d) => ({ value: d.id, label: d.name })),
        ]}
        className="w-44"
      />

      <Select
        value={installerId ?? ''}
        onChange={(e) => onInstallerFilterChange(e.target.value ? e.target.value : null)}
        options={[
          { value: '', label: 'All installers' },
          ...installers.map((i) => ({ value: i.id, label: i.name })),
        ]}
        className="w-44"
      />

      {onAssignJob && (
        <Button size="sm" onClick={onAssignJob}>
          <Plus className="h-4 w-4 mr-2" />
          Assign job
        </Button>
      )}

      <Button variant="outline" size="sm" onClick={onRefresh} disabled={isLoading} aria-label="Refresh">
        <RefreshCw className={cn('h-4 w-4', isLoading && 'animate-spin')} />
      </Button>
    </div>
  );
};

export default SchedulerToolbar;
