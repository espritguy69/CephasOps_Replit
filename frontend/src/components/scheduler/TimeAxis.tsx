import React from 'react';
import { SCHEDULER_START_HOUR, SCHEDULER_END_HOUR, SCHEDULER_HOUR_HEIGHT } from './schedulerConstants';
import { cn } from '../../lib/utils';

export interface TimeAxisProps {
  startHour?: number;
  endHour?: number;
  hourHeight?: number;
  /** When true, do not render the top spacer (use when grid has a separate header row) */
  noTopSpacer?: boolean;
  className?: string;
}

/**
 * Vertical time axis: sticky left column with hour labels.
 */
const TimeAxis: React.FC<TimeAxisProps> = ({
  startHour = SCHEDULER_START_HOUR,
  endHour = SCHEDULER_END_HOUR,
  hourHeight = SCHEDULER_HOUR_HEIGHT,
  noTopSpacer = false,
  className,
}) => {
  const hours: number[] = [];
  for (let h = startHour; h <= endHour; h++) {
    hours.push(h);
  }

  return (
    <div
      className={cn('sticky left-0 z-10 flex flex-col bg-muted/30 border-r shrink-0', className)}
      style={{ width: 52 }}
    >
      {!noTopSpacer && <div className="h-10 shrink-0 border-b bg-muted/50" aria-hidden />}
      {hours.map((h) => (
        <div
          key={h}
          className="flex items-start justify-end pr-2 pt-0.5 text-xs text-muted-foreground font-medium"
          style={{ height: hourHeight }}
        >
          {h === 12 ? '12' : h % 12}:00 {h < 12 ? 'AM' : 'PM'}
        </div>
      ))}
    </div>
  );
};

export default TimeAxis;
