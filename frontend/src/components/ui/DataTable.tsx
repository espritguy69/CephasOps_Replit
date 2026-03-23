import React, { useState, useMemo, ReactNode } from 'react';
import { ArrowUpDown, ArrowUp, ArrowDown, ChevronLeft, ChevronRight } from 'lucide-react';
import LoadingSpinner from './LoadingSpinner';
import EmptyState from './EmptyState';
import Button from './Button';
import { cn } from '@/lib/utils';

interface SortConfig {
  key: string | null;
  direction: 'asc' | 'desc';
}

interface DataTableColumn<T = any> {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string | number;
  render?: (value: unknown, row: T, rowIndex: number) => ReactNode;
  sortValue?: (row: T) => any;
}

interface DataTableProps<T = any> {
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

const DataTable = <T extends Record<string, any> = any>({
  columns = [],
  data = [],
  loading = false,
  pagination = false,
  pageSize = 10,
  sortable = false,
  onSort,
  onRowClick,
  emptyMessage = 'No data available',
  className = ''
}: DataTableProps<T>) => {
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [sortConfig, setSortConfig] = useState<SortConfig>({ key: null, direction: 'asc' });

  const handleSort = (columnKey: string): void => {
    if (!sortable || !columnKey) return;

    let direction: 'asc' | 'desc' = 'asc';
    if (sortConfig.key === columnKey && sortConfig.direction === 'asc') {
      direction = 'desc';
    }

    const newSortConfig: SortConfig = { key: columnKey, direction };
    setSortConfig(newSortConfig);

    if (onSort) {
      onSort(newSortConfig);
    }
  };

  const sortedData = useMemo(() => {
    if (!sortable || !sortConfig.key || onSort) {
      return data;
    }

    // Find the column definition to check for custom sortValue
    const column = columns.find(col => col.key === sortConfig.key);
    const getSortValue = column?.sortValue || ((row: T) => row[sortConfig.key!]);

    const sorted = [...data].sort((a, b) => {
      const aValue = getSortValue(a);
      const bValue = getSortValue(b);

      if (aValue === null || aValue === undefined) return 1;
      if (bValue === null || bValue === undefined) return -1;

      if (typeof aValue === 'string' && typeof bValue === 'string') {
        return sortConfig.direction === 'asc'
          ? aValue.localeCompare(bValue)
          : bValue.localeCompare(aValue);
      }

      if (typeof aValue === 'boolean' && typeof bValue === 'boolean') {
        return sortConfig.direction === 'asc' 
          ? (aValue === bValue ? 0 : aValue ? 1 : -1)
          : (aValue === bValue ? 0 : aValue ? -1 : 1);
      }

      return sortConfig.direction === 'asc' ? (aValue as number) - (bValue as number) : (bValue as number) - (aValue as number);
    });

    return sorted;
  }, [data, sortConfig, sortable, onSort, columns]);

  const paginatedData = useMemo(() => {
    if (!pagination) return sortedData;
    const start = (currentPage - 1) * pageSize;
    const end = start + pageSize;
    return sortedData.slice(start, end);
  }, [sortedData, pagination, currentPage, pageSize]);

  const totalPages = pagination ? Math.ceil(sortedData.length / pageSize) : 1;

  const getSortIcon = (columnKey: string): ReactNode => {
    if (!sortable || !columnKey) return null;
    if (sortConfig.key !== columnKey) {
      return <ArrowUpDown className="h-4 w-4 ml-2 text-muted-foreground" aria-hidden="true" />;
    }
    return sortConfig.direction === 'asc' 
      ? <ArrowUp className="h-4 w-4 ml-2 text-foreground" aria-hidden="true" />
      : <ArrowDown className="h-4 w-4 ml-2 text-foreground" aria-hidden="true" />;
  };

  if (loading) {
    return <LoadingSpinner message="Loading data..." />;
  }

  if (data.length === 0) {
    return <EmptyState title={emptyMessage} />;
  }

  return (
    <div className={cn("w-full space-y-2", className)}>
      {/* Mobile Card View */}
      <div className="md:hidden space-y-3">
        {paginatedData.map((row, rowIndex) => (
          <div
            key={(row as any).id || rowIndex}
            className={cn(
              "rounded border bg-card p-3 space-y-2 transition-colors",
              onRowClick && "cursor-pointer hover:bg-muted/30"
            )}
            onClick={() => onRowClick && onRowClick(row, rowIndex)}
          >
            {columns.map((column) => {
              // Hide action columns on mobile cards (they can be shown as buttons)
              if (column.key.includes('actions') || column.key.includes('action')) {
                return null;
              }
              return (
                <div key={column.key} className="flex flex-col space-y-1">
                  <div className="text-[10px] font-medium text-muted-foreground uppercase tracking-wide">
                    {column.label}
                  </div>
                  <div className="text-xs text-foreground">
                    {column.render
                      ? column.render(row[column.key], row, rowIndex)
                      : row[column.key]}
                  </div>
                </div>
              );
            })}
          </div>
        ))}
      </div>

      {/* Desktop Table View */}
      <div className="hidden md:block rounded border overflow-x-auto">
        <table className="w-full border-collapse text-[9px] min-w-[600px]">
          <thead>
            <tr className="border-b bg-muted/50">
              {columns.map((column) => {
                const isColumnSortable = sortable && (column.sortable === true || (column.sortable !== false && !column.key.includes('actions')));
                return (
                  <th
                    key={column.key}
                    className={cn(
                      "h-8 px-2 md:px-3 py-1.5 text-left align-middle font-medium text-muted-foreground text-[9px]",
                      isColumnSortable && onRowClick === undefined && "cursor-pointer hover:bg-muted/80",
                      onRowClick === undefined && isColumnSortable && "select-none"
                    )}
                    onClick={() => isColumnSortable && handleSort(column.key)}
                    style={{ width: column.width }}
                  >
                    <div className="flex items-center">
                      {column.label}
                      {isColumnSortable && getSortIcon(column.key)}
                    </div>
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {paginatedData.map((row, rowIndex) => (
              <tr
                key={(row as any).id || rowIndex}
                className={cn(
                  "border-b transition-colors min-h-[44px] items-center",
                  onRowClick && "cursor-pointer hover:bg-muted/50"
                )}
                onClick={() => onRowClick && onRowClick(row, rowIndex)}
              >
                {columns.map((column) => (
                  <td key={column.key} className="px-2 md:px-3 py-2 md:py-1.5 align-middle text-[9px]">
                    {column.render
                      ? column.render(row[column.key], row, rowIndex)
                      : row[column.key]}
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
            Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, sortedData.length)} of {sortedData.length} results
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
};

export default DataTable;

