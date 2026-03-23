import React from 'react';
import { Filter, Calendar as CalendarIcon } from 'lucide-react';
import { Select, Button } from '../ui';
import type { Partner } from '../../types/partners';

export interface SchedulerFiltersState {
  partnerId?: string | null;
  region?: string | null;
  fromDate?: string;
  toDate?: string;
}

interface SchedulerFiltersProps {
  partners: Partner[];
  filters: SchedulerFiltersState;
  onFilterChange: (filters: SchedulerFiltersState) => void;
  onReset: () => void;
  className?: string;
}

/**
 * SchedulerFilters component
 * Provides filtering options for the scheduler (Partner, Region, Date range)
 */
const SchedulerFilters: React.FC<SchedulerFiltersProps> = ({
  partners,
  filters,
  onFilterChange,
  onReset,
  className
}) => {
  const handlePartnerChange = (value: string): void => {
    onFilterChange({
      ...filters,
      partnerId: value && value !== '' && value !== 'undefined' && value !== 'null' ? value : null
    });
  };

  const handleRegionChange = (value: string): void => {
    onFilterChange({
      ...filters,
      region: value && value !== '' ? value : null
    });
  };

  const handleDateRangeChange = (type: 'from' | 'to', value: string): void => {
    onFilterChange({
      ...filters,
      [type === 'from' ? 'fromDate' : 'toDate']: value || undefined
    });
  };

  const hasActiveFilters = filters.partnerId || filters.region || filters.fromDate || filters.toDate;

  // Extract unique regions from partners (if available)
  const regions = React.useMemo(() => {
    const regionSet = new Set<string>();
    partners.forEach(partner => {
      // Assuming partner might have a region field - adjust based on actual Partner type
      if ((partner as any).region) {
        regionSet.add((partner as any).region);
      }
    });
    return Array.from(regionSet).sort();
  }, [partners]);

  return (
    <div className={`flex items-center gap-4 p-3 border-b bg-white ${className || ''}`}>
      <div className="flex items-center gap-2">
        <Filter className="h-4 w-4 text-muted-foreground" />
        <span className="text-sm font-medium">Filters:</span>
      </div>

      {/* Partner Filter */}
      <Select
        value={filters.partnerId || ''}
        onChange={(e) => handlePartnerChange(e.target.value)}
        options={[
          { value: '', label: 'All Partners' },
          ...partners.map(partner => ({ value: partner.id, label: partner.name }))
        ]}
        className="w-48"
      />

      {/* Region Filter */}
      {regions.length > 0 && (
        <Select
          value={filters.region || ''}
          onChange={(e) => handleRegionChange(e.target.value)}
          options={[
            { value: '', label: 'All Regions' },
            ...regions.map(region => ({ value: region, label: region }))
          ]}
          className="w-40"
        />
      )}

      {/* Date Range Filters */}
      <div className="flex items-center gap-2">
        <CalendarIcon className="h-4 w-4 text-muted-foreground" />
        <input
          type="date"
          value={filters.fromDate || ''}
          onChange={(e) => handleDateRangeChange('from', e.target.value)}
          className="h-9 px-3 rounded-md border border-input bg-background text-sm"
          placeholder="From date"
        />
        <span className="text-muted-foreground">to</span>
        <input
          type="date"
          value={filters.toDate || ''}
          onChange={(e) => handleDateRangeChange('to', e.target.value)}
          className="h-9 px-3 rounded-md border border-input bg-background text-sm"
          placeholder="To date"
        />
      </div>

      {/* Reset Button */}
      {hasActiveFilters && (
        <Button
          variant="outline"
          size="sm"
          onClick={onReset}
        >
          Clear Filters
        </Button>
      )}
    </div>
  );
};

export default SchedulerFilters;

