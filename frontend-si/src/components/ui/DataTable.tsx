import React, { useState, useMemo, ReactNode } from 'react';
import { ArrowUpDown, ArrowUp, ArrowDown, ChevronLeft, ChevronRight } from 'lucide-react';
import { LoadingSpinner } from './LoadingSpinner';
import { EmptyState } from './EmptyState';
import { Button } from './Button';
import { cn } from '../../lib/utils';

export interface SortConfig {
  key: string | null;
  direction: 'asc' | 'desc';
}

export interface DataTableColumn<T = Record<string, unknown>> {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string | number;
  render?: (value: unknown, row: T, rowIndex: number) => ReactNode;
  sortValue?: (row: T) => unknown;
}

export interface DataTableProps<T = Record<string, unknown>> {
  columns?: DataTableColumn<T>[];
  data?: T[];
  loading?: boolean;
  pagination?: boolean;
  pageSize?: number;
  sortable?: boolean;
  onSort?: (config: SortConfig) => void;
  onRowClick?: (row: T, rowIndex: number) => void;
  emptyMessage?: string;
  className?: string;
}

export function DataTable<T extends Record<string, unknown> = Record<string, unknown>>({
  columns = [],
  data = [],
  loading = false,
  pagination = false,
  pageSize = 10,
  sortable = false,
  onSort,
  onRowClick,
  emptyMessage = 'No data available',
  className = '',
}: DataTableProps<T>) {
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [sortConfig, setSortConfig] = useState<SortConfig>({ key: null, direction: 'asc' });

  const handleSort = (columnKey: string): void => {
    if (!sortable || !columnKey) return;
    const direction: 'asc' | 'desc' =
      sortConfig.key === columnKey && sortConfig.direction === 'asc' ? 'desc' : 'asc';
    const newSortConfig: SortConfig = { key: columnKey, direction };
    setSortConfig(newSortConfig);
    onSort?.(newSortConfig);
  };

  const sortedData = useMemo(() => {
    if (!sortable || !sortConfig.key || onSort) return data;
    const column = columns.find((col) => col.key === sortConfig.key);
    const getSortValue = column?.sortValue ?? ((row: T) => row[sortConfig.key!]);
    return [...data].sort((a, b) => {
      const aVal = getSortValue(a);
      const bVal = getSortValue(b);
      if (aVal == null) return 1;
      if (bVal == null) return -1;
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        return sortConfig.direction === 'asc' ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal);
      }
      if (typeof aVal === 'boolean' && typeof bVal === 'boolean') {
        return sortConfig.direction === 'asc' ? (aVal === bVal ? 0 : aVal ? 1 : -1) : (aVal === bVal ? 0 : aVal ? -1 : 1);
      }
      const an = Number(aVal);
      const bn = Number(bVal);
      return sortConfig.direction === 'asc' ? an - bn : bn - an;
    });
  }, [data, sortConfig, sortable, onSort, columns]);

  const paginatedData = useMemo(() => {
    if (!pagination) return sortedData;
    const start = (currentPage - 1) * pageSize;
    return sortedData.slice(start, start + pageSize);
  }, [sortedData, pagination, currentPage, pageSize]);

  const totalPages = pagination ? Math.ceil(sortedData.length / pageSize) : 1;

  const getSortIcon = (columnKey: string): ReactNode => {
    if (!sortable || !columnKey) return null;
    if (sortConfig.key !== columnKey) {
      return <ArrowUpDown className="h-4 w-4 ml-2 text-muted-foreground" aria-hidden="true" />;
    }
    return sortConfig.direction === 'asc' ? (
      <ArrowUp className="h-4 w-4 ml-2 text-foreground" aria-hidden="true" />
    ) : (
      <ArrowDown className="h-4 w-4 ml-2 text-foreground" aria-hidden="true" />
    );
  };

  if (loading) {
    return <LoadingSpinner message="Loading data..." />;
  }

  if (data.length === 0) {
    return <EmptyState title={emptyMessage} />;
  }

  return (
    <div className={cn('w-full space-y-2', className)}>
      {/* Mobile card view */}
      <div className="md:hidden space-y-3">
        {paginatedData.map((row, rowIndex) => (
          <div
            key={(row as { id?: string })?.id ?? rowIndex}
            className={cn(
              'rounded border bg-card p-3 space-y-2 transition-colors',
              onRowClick && 'cursor-pointer hover:bg-muted/30'
            )}
            onClick={() => onRowClick?.(row as T, rowIndex)}
          >
            {columns.map((col) => {
              if (col.key.includes('actions') || col.key.includes('action')) return null;
              return (
                <div key={col.key} className="flex flex-col space-y-1">
                  <div className="text-[10px] font-medium text-muted-foreground uppercase tracking-wide">
                    {col.label}
                  </div>
                  <div className="text-xs text-foreground">
                    {col.render ? col.render((row as T)[col.key], row as T, rowIndex) : String((row as T)[col.key] ?? '')}
                  </div>
                </div>
              );
            })}
          </div>
        ))}
      </div>

      {/* Desktop table view */}
      <div className="hidden md:block rounded border overflow-x-auto">
        <table className="w-full border-collapse text-[9px] min-w-[600px]">
          <thead>
            <tr className="border-b bg-muted/50">
              {columns.map((col) => {
                const isSortable =
                  sortable && (col.sortable === true || (col.sortable !== false && !col.key.includes('actions')));
                return (
                  <th
                    key={col.key}
                    className={cn(
                      'h-8 px-2 md:px-3 py-1.5 text-left align-middle font-medium text-muted-foreground text-[9px]',
                      isSortable && !onRowClick && 'cursor-pointer hover:bg-muted/80 select-none'
                    )}
                    onClick={() => isSortable && handleSort(col.key)}
                    style={{ width: col.width }}
                  >
                    <div className="flex items-center">
                      {col.label}
                      {isSortable && getSortIcon(col.key)}
                    </div>
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {paginatedData.map((row, rowIndex) => (
              <tr
                key={(row as { id?: string })?.id ?? rowIndex}
                className={cn(
                  'border-b transition-colors min-h-[44px] items-center',
                  onRowClick && 'cursor-pointer hover:bg-muted/50'
                )}
                onClick={() => onRowClick?.(row as T, rowIndex)}
              >
                {columns.map((col) => (
                  <td key={col.key} className="px-2 md:px-3 py-2 md:py-1.5 align-middle text-[9px]">
                    {col.render
                      ? col.render((row as T)[col.key], row as T, rowIndex)
                      : String((row as T)[col.key] ?? '')}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {pagination && totalPages > 1 && (
        <div className="flex items-center justify-between px-2">
          <div className="text-xs text-muted-foreground">
            Showing {(currentPage - 1) * pageSize + 1} to{' '}
            {Math.min(currentPage * pageSize, sortedData.length)} of {sortedData.length} results
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
            >
              <ChevronLeft className="h-4 w-4 mr-1" />
              Previous
            </Button>
            <div className="text-xs font-medium">
              Page {currentPage} of {totalPages}
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
            >
              Next
              <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
