import React, { useState, useEffect } from 'react';
import { X, Filter } from 'lucide-react';
import { ORDER_STATUS, ORDER_TYPE } from '../../constants/orders';
import { getPartners } from '../../api/partners';
import type { Partner } from '../../types/partners';
import { Button, Card, Select, DatePicker } from '../ui';

interface OrderFiltersProps {
  filters?: Record<string, string>;
  onFilterChange: (filters: Record<string, string | undefined>) => void;
  onReset?: () => void;
}

const OrderFilters: React.FC<OrderFiltersProps> = ({ filters, onFilterChange, onReset }) => {
  const [localFilters, setLocalFilters] = useState<Record<string, string>>(filters || {});
  const [partners, setPartners] = useState<Partner[]>([]);

  useEffect(() => {
    getPartners({ isActive: true }).then(setPartners).catch(() => setPartners([]));
  }, []);

  const handleChange = (key: string, value: string): void => {
    const newFilters = { ...localFilters, [key]: value || undefined };
    if (!value) delete newFilters[key];
    setLocalFilters(newFilters);
    onFilterChange(newFilters);
  };

  const handleReset = (): void => {
    setLocalFilters({});
    onFilterChange({});
    if (onReset) onReset();
  };

  return (
    <Card className="mb-2">
      <div className="flex justify-between items-center mb-2">
        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-muted-foreground" />
          <h3 className="text-xs font-semibold">Filters</h3>
        </div>
        <Button variant="outline" size="sm" onClick={handleReset}>
          <X className="h-3 w-3 mr-1" />
          Reset
        </Button>
      </div>

      <div className="flex flex-wrap items-end gap-2">
        <div className="flex-shrink-0" style={{ minWidth: '140px' }}>
          <Select
            label="Status"
            value={localFilters.status || ''}
            onChange={(e) => handleChange('status', e.target.value)}
            options={[
              { value: '', label: 'All Statuses' },
              ...Object.values(ORDER_STATUS).map((status) => ({ value: status, label: status }))
            ]}
          />
        </div>

        <div className="flex-shrink-0" style={{ minWidth: '140px' }}>
          <Select
            label="Type"
            value={localFilters.orderType || ''}
            onChange={(e) => handleChange('orderType', e.target.value)}
            options={[
              { value: '', label: 'All Types' },
              ...Object.values(ORDER_TYPE).map((type) => ({ value: type, label: type }))
            ]}
          />
        </div>

        <div className="flex-shrink-0" style={{ minWidth: '140px' }}>
          <Select
            label="Partner"
            value={localFilters.partnerId || ''}
            onChange={(e) => handleChange('partnerId', e.target.value)}
            options={[
              { value: '', label: 'All Partners' },
              ...partners.map((p) => ({ value: p.id, label: p.name + (p.code ? ` (${p.code})` : '') }))
            ]}
          />
        </div>

        <div className="flex-shrink-0" style={{ minWidth: '140px' }}>
          <DatePicker
            label="Start Date"
            value={localFilters.startDate || ''}
            onChange={(e) => handleChange('startDate', e.target.value)}
          />
        </div>

        <div className="flex-shrink-0" style={{ minWidth: '140px' }}>
          <DatePicker
            label="End Date"
            value={localFilters.endDate || ''}
            onChange={(e) => handleChange('endDate', e.target.value)}
          />
        </div>

        <div className="flex-1 min-w-[200px]">
          <div className="relative">
            <label className="block text-xs font-medium mb-0.5">Search</label>
            <input
              type="text"
              className="w-full h-9 px-2 py-1 border border-input rounded bg-background text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
              placeholder="Service ID, Customer Name, Address..."
              value={localFilters.search || ''}
              onChange={(e) => handleChange('search', e.target.value)}
            />
          </div>
        </div>
      </div>
    </Card>
  );
};

export default OrderFilters;

