import React from 'react';
import { ArrowUp, ArrowDown, ArrowUpDown } from 'lucide-react';
import { cn } from '@/lib/utils';

// ============================================================================
// Types
// ============================================================================

export type SortDirection = 'asc' | 'desc' | null;

export interface SortConfig {
  key: string;
  direction: SortDirection;
}

export interface SortableHeaderProps {
  label: string;
  sortKey: string;
  currentSort: SortConfig | null;
  onSort: (key: string, direction: SortDirection) => void;
  className?: string;
}

// ============================================================================
// Sortable Header Component
// ============================================================================

/**
 * SortableHeader component
 * Table header cell with sort functionality and visual indicators
 */
const SortableHeader: React.FC<SortableHeaderProps> = ({
  label,
  sortKey,
  currentSort,
  onSort,
  className,
}) => {
  const isActive = currentSort?.key === sortKey;
  const direction = isActive ? currentSort?.direction : null;

  const handleClick = () => {
    let newDirection: SortDirection;
    
    if (!isActive) {
      // First click: sort ascending
      newDirection = 'asc';
    } else if (direction === 'asc') {
      // Second click: sort descending
      newDirection = 'desc';
    } else {
      // Third click: clear sort
      newDirection = null;
    }
    
    onSort(sortKey, newDirection);
  };

  return (
    <th
      className={cn(
        'px-4 py-3 text-left text-sm font-semibold text-gray-700',
        'cursor-pointer select-none hover:bg-gray-100 transition-colors',
        isActive && 'bg-gray-50',
        className
      )}
      onClick={handleClick}
    >
      <div className="flex items-center gap-2">
        <span>{label}</span>
        <span className={cn('text-gray-400', isActive && 'text-primary')}>
          {direction === 'asc' && <ArrowUp className="h-4 w-4" />}
          {direction === 'desc' && <ArrowDown className="h-4 w-4" />}
          {!direction && <ArrowUpDown className="h-3 w-3" />}
        </span>
      </div>
    </th>
  );
};

// ============================================================================
// Sortable Table Header Row Component
// ============================================================================

export interface ColumnDef {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
  className?: string;
}

export interface SortableTableHeaderProps {
  columns: ColumnDef[];
  currentSort: SortConfig | null;
  onSort: (key: string, direction: SortDirection) => void;
  showCheckbox?: boolean;
  onSelectAll?: (checked: boolean) => void;
  allSelected?: boolean;
  someSelected?: boolean;
  className?: string;
}

/**
 * SortableTableHeader component
 * Complete table header row with sortable columns
 */
export const SortableTableHeader: React.FC<SortableTableHeaderProps> = ({
  columns,
  currentSort,
  onSort,
  showCheckbox = false,
  onSelectAll,
  allSelected = false,
  someSelected = false,
  className,
}) => {
  const alignClasses = {
    left: 'text-left',
    center: 'text-center',
    right: 'text-right',
  };

  return (
    <thead className={cn('bg-gray-50 border-b border-gray-200', className)}>
      <tr>
        {showCheckbox && (
          <th className="px-4 py-3 w-12">
            <input
              type="checkbox"
              checked={allSelected}
              ref={(input) => {
                if (input) {
                  input.indeterminate = someSelected && !allSelected;
                }
              }}
              onChange={(e) => onSelectAll?.(e.target.checked)}
              className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
          </th>
        )}
        {columns.map((column) => {
          if (column.sortable !== false) {
            return (
              <SortableHeader
                key={column.key}
                label={column.label}
                sortKey={column.key}
                currentSort={currentSort}
                onSort={onSort}
                className={cn(
                  column.width && `w-[${column.width}]`,
                  alignClasses[column.align || 'left'],
                  column.className
                )}
              />
            );
          }

          return (
            <th
              key={column.key}
              className={cn(
                'px-4 py-3 text-sm font-semibold text-gray-700',
                column.width && `w-[${column.width}]`,
                alignClasses[column.align || 'left'],
                column.className
              )}
            >
              {column.label}
            </th>
          );
        })}
      </tr>
    </thead>
  );
};

// ============================================================================
// Sort Utility Functions
// ============================================================================

/**
 * Sort array by a given key and direction
 * @param data - Array of objects to sort
 * @param sortConfig - Sort configuration
 * @returns Sorted array
 */
export function sortData<T extends Record<string, any>>(
  data: T[],
  sortConfig: SortConfig | null
): T[] {
  if (!sortConfig || !sortConfig.direction) {
    return data;
  }

  const { key, direction } = sortConfig;

  return [...data].sort((a, b) => {
    const aValue = getNestedValue(a, key);
    const bValue = getNestedValue(b, key);

    // Handle null/undefined
    if (aValue == null && bValue == null) return 0;
    if (aValue == null) return direction === 'asc' ? 1 : -1;
    if (bValue == null) return direction === 'asc' ? -1 : 1;

    // Handle different types
    if (typeof aValue === 'string' && typeof bValue === 'string') {
      const comparison = aValue.localeCompare(bValue, undefined, { sensitivity: 'base' });
      return direction === 'asc' ? comparison : -comparison;
    }

    if (typeof aValue === 'number' && typeof bValue === 'number') {
      return direction === 'asc' ? aValue - bValue : bValue - aValue;
    }

    if (aValue instanceof Date && bValue instanceof Date) {
      return direction === 'asc' 
        ? aValue.getTime() - bValue.getTime() 
        : bValue.getTime() - aValue.getTime();
    }

    // Handle date strings
    const dateA = new Date(aValue);
    const dateB = new Date(bValue);
    if (!isNaN(dateA.getTime()) && !isNaN(dateB.getTime())) {
      return direction === 'asc' 
        ? dateA.getTime() - dateB.getTime() 
        : dateB.getTime() - dateA.getTime();
    }

    // Default string comparison
    const strA = String(aValue);
    const strB = String(bValue);
    const comparison = strA.localeCompare(strB, undefined, { sensitivity: 'base' });
    return direction === 'asc' ? comparison : -comparison;
  });
}

/**
 * Get nested value from object using dot notation
 * @param obj - Object to get value from
 * @param path - Path to value (e.g., "customer.name")
 * @returns Value at path or undefined
 */
function getNestedValue(obj: Record<string, any>, path: string): any {
  return path.split('.').reduce((acc, key) => acc?.[key], obj);
}

// ============================================================================
// Hook for Sort State
// ============================================================================

/**
 * useSortState hook
 * Manages sort state for a table
 */
export function useSortState(
  initialSort: SortConfig | null = null
): [SortConfig | null, (key: string, direction: SortDirection) => void] {
  const [sortConfig, setSortConfig] = React.useState<SortConfig | null>(initialSort);

  const handleSort = React.useCallback((key: string, direction: SortDirection) => {
    if (direction === null) {
      setSortConfig(null);
    } else {
      setSortConfig({ key, direction });
    }
  }, []);

  return [sortConfig, handleSort];
}

export default SortableHeader;

