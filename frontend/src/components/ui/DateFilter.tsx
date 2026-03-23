import React from 'react';
import { X } from 'lucide-react';
import { Button } from './Button';
import { Input } from './input';
import { Label } from './label';
import { cn } from '@/lib/utils';
import {
  getTodayString,
  getYesterdayString,
  getTomorrowString,
  getThisWeekRange,
  getLast7DaysRange,
  getThisMonthRange,
  getLast30DaysRange,
} from '../../utils/dateHelpers';

// ============================================================================
// Types
// ============================================================================

export type DateFilterMode = 'single' | 'range';

export interface DateFilterValue {
  mode: DateFilterMode;
  singleDate: string;
  startDate: string;
  endDate: string;
}

export interface DateFilterProps {
  value: DateFilterValue;
  onChange: (value: DateFilterValue) => void;
  label?: string;
  showModeToggle?: boolean;
  showQuickShortcuts?: boolean;
  showRangePresets?: boolean;
  className?: string;
}

// ============================================================================
// Date Filter Component
// ============================================================================

/**
 * DateFilter component
 * Reusable date filter with single date and date range modes
 * Includes quick shortcuts (Yesterday, Today, Tomorrow) and range presets
 */
const DateFilter: React.FC<DateFilterProps> = ({
  value,
  onChange,
  label = 'Date',
  showModeToggle = true,
  showQuickShortcuts = true,
  showRangePresets = true,
  className,
}) => {
  const today = getTodayString();
  const yesterday = getYesterdayString();
  const tomorrow = getTomorrowString();

  const handleModeChange = (mode: DateFilterMode) => {
    onChange({ ...value, mode });
  };

  const handleSingleDateChange = (singleDate: string) => {
    onChange({ ...value, singleDate });
  };

  const handleStartDateChange = (startDate: string) => {
    onChange({ ...value, startDate });
  };

  const handleEndDateChange = (endDate: string) => {
    onChange({ ...value, endDate });
  };

  const handleClearSingleDate = () => {
    onChange({ ...value, singleDate: '' });
  };

  const handleClearDateRange = () => {
    onChange({ ...value, startDate: '', endDate: '' });
  };

  const handleQuickShortcut = (date: string) => {
    onChange({ ...value, singleDate: date });
  };

  const handleRangePreset = (range: { start: string; end: string }) => {
    onChange({ ...value, startDate: range.start, endDate: range.end });
  };

  const isDateActive = (date: string): boolean => {
    return value.singleDate === date;
  };

  return (
    <div className={cn('space-y-2', className)}>
      {/* Label and Mode Toggle */}
      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium">
          {label}
          {value.mode === 'single' && value.singleDate === today && (
            <span className="ml-2 text-xs text-primary font-normal">(Today)</span>
          )}
        </Label>
        
        {showModeToggle && (
          <div className="flex gap-1">
            <Button
              variant={value.mode === 'single' ? 'default' : 'outline'}
              size="sm"
              onClick={() => handleModeChange('single')}
              className="text-xs h-7"
            >
              Single Date
            </Button>
            <Button
              variant={value.mode === 'range' ? 'default' : 'outline'}
              size="sm"
              onClick={() => handleModeChange('range')}
              className="text-xs h-7"
            >
              Date Range
            </Button>
          </div>
        )}
      </div>

      {/* Single Date Mode */}
      {value.mode === 'single' && (
        <>
          {/* Quick Shortcuts */}
          {showQuickShortcuts && (
            <div className="flex gap-2">
              <Button
                variant={isDateActive(yesterday) ? 'default' : 'outline'}
                size="sm"
                onClick={() => handleQuickShortcut(yesterday)}
                className="text-xs"
              >
                Yesterday
              </Button>
              <Button
                variant={isDateActive(today) ? 'default' : 'outline'}
                size="sm"
                onClick={() => handleQuickShortcut(today)}
                className="text-xs"
              >
                Today
              </Button>
              <Button
                variant={isDateActive(tomorrow) ? 'default' : 'outline'}
                size="sm"
                onClick={() => handleQuickShortcut(tomorrow)}
                className="text-xs"
              >
                Tomorrow
              </Button>
            </div>
          )}

          {/* Date Input */}
          <div className="flex gap-2">
            <Input
              type="date"
              value={value.singleDate}
              onChange={(e) => handleSingleDateChange(e.target.value)}
              className="flex-1"
            />
            {value.singleDate && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleClearSingleDate}
                title="Clear date filter"
                className="px-2"
              >
                <X className="h-4 w-4" />
              </Button>
            )}
          </div>
        </>
      )}

      {/* Date Range Mode */}
      {value.mode === 'range' && (
        <>
          {/* Range Presets */}
          {showRangePresets && (
            <div className="flex gap-2 flex-wrap">
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleRangePreset(getThisWeekRange())}
                className="text-xs"
              >
                This Week
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleRangePreset(getLast7DaysRange())}
                className="text-xs"
              >
                Last 7 Days
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleRangePreset(getThisMonthRange())}
                className="text-xs"
              >
                This Month
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => handleRangePreset(getLast30DaysRange())}
                className="text-xs"
              >
                Last 30 Days
              </Button>
            </div>
          )}

          {/* Date Range Inputs */}
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="text-xs text-muted-foreground mb-1 block">Start Date</label>
              <Input
                type="date"
                value={value.startDate}
                onChange={(e) => handleStartDateChange(e.target.value)}
              />
            </div>
            <div>
              <label className="text-xs text-muted-foreground mb-1 block">End Date</label>
              <Input
                type="date"
                value={value.endDate}
                onChange={(e) => handleEndDateChange(e.target.value)}
              />
            </div>
          </div>

          {/* Clear Range Button */}
          {(value.startDate || value.endDate) && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleClearDateRange}
              className="text-xs w-full"
            >
              <X className="h-3 w-3 mr-1" />
              Clear Date Range
            </Button>
          )}
        </>
      )}
    </div>
  );
};

// ============================================================================
// Simple Date Filter (Single Date Only)
// ============================================================================

export interface SimpleDateFilterProps {
  value: string;
  onChange: (value: string) => void;
  label?: string;
  showQuickShortcuts?: boolean;
  className?: string;
}

/**
 * SimpleDateFilter component
 * Single date filter with quick shortcuts
 */
export const SimpleDateFilter: React.FC<SimpleDateFilterProps> = ({
  value,
  onChange,
  label = 'Date',
  showQuickShortcuts = true,
  className,
}) => {
  const today = getTodayString();
  const yesterday = getYesterdayString();
  const tomorrow = getTomorrowString();

  const isDateActive = (date: string): boolean => value === date;

  return (
    <div className={cn('space-y-2', className)}>
      <Label className="text-sm font-medium">
        {label}
        {value === today && (
          <span className="ml-2 text-xs text-blue-600 font-normal">(Today)</span>
        )}
      </Label>

      {showQuickShortcuts && (
        <div className="flex gap-2">
          <Button
            variant={isDateActive(yesterday) ? 'default' : 'outline'}
            size="sm"
            onClick={() => onChange(yesterday)}
            className="text-xs"
          >
            Yesterday
          </Button>
          <Button
            variant={isDateActive(today) ? 'default' : 'outline'}
            size="sm"
            onClick={() => onChange(today)}
            className="text-xs"
          >
            Today
          </Button>
          <Button
            variant={isDateActive(tomorrow) ? 'default' : 'outline'}
            size="sm"
            onClick={() => onChange(tomorrow)}
            className="text-xs"
          >
            Tomorrow
          </Button>
        </div>
      )}

      <div className="flex gap-2">
        <Input
          type="date"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className="flex-1"
        />
        {value && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onChange('')}
            title="Clear date filter"
            className="px-2"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>
    </div>
  );
};

// ============================================================================
// Date Range Filter (Range Only)
// ============================================================================

export interface DateRangeFilterProps {
  startDate: string;
  endDate: string;
  onStartDateChange: (value: string) => void;
  onEndDateChange: (value: string) => void;
  label?: string;
  showPresets?: boolean;
  className?: string;
}

/**
 * DateRangeFilter component
 * Date range filter with presets
 */
export const DateRangeFilter: React.FC<DateRangeFilterProps> = ({
  startDate,
  endDate,
  onStartDateChange,
  onEndDateChange,
  label = 'Date Range',
  showPresets = true,
  className,
}) => {
  const handlePreset = (range: { start: string; end: string }) => {
    onStartDateChange(range.start);
    onEndDateChange(range.end);
  };

  const handleClear = () => {
    onStartDateChange('');
    onEndDateChange('');
  };

  return (
    <div className={cn('space-y-2', className)}>
      <Label className="text-sm font-medium">{label}</Label>

      {showPresets && (
        <div className="flex gap-2 flex-wrap">
          <Button
            variant="outline"
            size="sm"
            onClick={() => handlePreset(getThisWeekRange())}
            className="text-xs"
          >
            This Week
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handlePreset(getLast7DaysRange())}
            className="text-xs"
          >
            Last 7 Days
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handlePreset(getThisMonthRange())}
            className="text-xs"
          >
            This Month
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handlePreset(getLast30DaysRange())}
            className="text-xs"
          >
            Last 30 Days
          </Button>
        </div>
      )}

      <div className="grid grid-cols-2 gap-2">
        <div>
          <label className="text-xs text-muted-foreground mb-1 block">Start Date</label>
          <Input
            type="date"
            value={startDate}
            onChange={(e) => onStartDateChange(e.target.value)}
          />
        </div>
        <div>
          <label className="text-xs text-muted-foreground mb-1 block">End Date</label>
          <Input
            type="date"
            value={endDate}
            onChange={(e) => onEndDateChange(e.target.value)}
          />
        </div>
      </div>

      {(startDate || endDate) && (
        <Button
          variant="ghost"
          size="sm"
          onClick={handleClear}
          className="text-xs w-full"
        >
          <X className="h-3 w-3 mr-1" />
          Clear Date Range
        </Button>
      )}
    </div>
  );
};

export default DateFilter;

