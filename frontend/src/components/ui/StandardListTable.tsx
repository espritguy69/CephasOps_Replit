import React, { useState, useMemo, ReactNode } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import EmptyState from './EmptyState';
import LoadingSpinner from './LoadingSpinner';
import ActionIcons from './ActionIcons';
import { TooltipProvider } from './Tooltip';

interface StandardListTableColumn<T = any> {
  key: string;
  label: string;
  align?: 'left' | 'center' | 'right';
  render?: (value: unknown, row: T) => ReactNode;
}

interface StandardListTableActions {
  onView?: (row: any) => void;
  onEdit?: (row: any) => void;
  onDownload?: (row: any) => void;
  onDeactivate?: (row: any) => void;
  onDelete?: (row: any) => void;
  viewPath?: string;
  editPath?: string;
  downloadPath?: string;
}

interface StandardListTableProps<T = any> {
  // Data
  data?: T[];
  columns?: StandardListTableColumn<T>[];
  
  // Selection
  selectedRows?: string[];
  onSelectionChange?: (selectedIds: string[]) => void;
  
  // Navigation
  onRowClick?: (row: T) => void;
  
  // Actions
  actions?: StandardListTableActions;
  
  // Pagination
  pagination?: boolean;
  pageSize?: number;
  onPageSizeChange?: (size: number) => void;
  currentPage?: number;
  onPageChange?: (page: number) => void;
  
  // Loading & Empty
  loading?: boolean;
  emptyMessage?: string;
  
  // Styling
  className?: string;
  rowIdKey?: string;
}

/**
 * StandardListTable - A consistent, compact table component for all list views
 * 
 * Features:
 * - Checkbox selection (leftmost column)
 * - Primary name column (left-aligned, medium weight)
 * - 1-3 secondary columns (short text, center/right aligned)
 * - Action column (last column) with role-based icons
 * - Compact rows (20 rows visible on 1366×768)
 * - Row hover highlight
 * - Click row to navigate (checkbox/actions only select/perform action)
 * - Compact pagination (default 20 rows per page)
 */
const StandardListTable = <T extends Record<string, any> = any>({
  // Data
  data = [],
  columns = [],
  
  // Selection
  selectedRows = [],
  onSelectionChange,
  
  // Navigation
  onRowClick,
  
  // Actions
  actions,
  
  // Pagination
  pagination = true,
  pageSize: controlledPageSize,
  onPageSizeChange,
  currentPage: controlledPage,
  onPageChange,
  
  // Loading & Empty
  loading = false,
  emptyMessage = 'No items found',
  
  // Styling
  className = '',
  rowIdKey = 'id'
}: StandardListTableProps<T>) => {
  const [internalPage, setInternalPage] = useState<number>(1);
  const [internalPageSize, setInternalPageSize] = useState<number>(20);
  
  const currentPage = controlledPage ?? internalPage;
  const setCurrentPage = onPageChange ?? setInternalPage;
  const pageSize = controlledPageSize ?? internalPageSize;
  const setPageSize = onPageSizeChange ?? setInternalPageSize;
  
  const [selectAll, setSelectAll] = useState<boolean>(false);
  
  // Get row ID
  const getRowId = (row: T): string => {
    return (row as any)[rowIdKey] || (row as any).id || '';
  };
  
  // Pagination
  const totalPages = Math.ceil(data.length / pageSize);
  const paginatedData = useMemo(() => {
    if (!pagination) return data;
    const start = (currentPage - 1) * pageSize;
    const end = start + pageSize;
    return data.slice(start, end);
  }, [data, pagination, currentPage, pageSize]);
  
  // Selection handlers
  const handleSelectAll = (checked: boolean): void => {
    setSelectAll(checked);
    if (checked) {
      const allIds = paginatedData.map(getRowId);
      onSelectionChange?.(allIds);
    } else {
      onSelectionChange?.([]);
    }
  };
  
  const handleRowSelect = (rowId: string, checked: boolean): void => {
    if (checked) {
      onSelectionChange?.([...selectedRows, rowId]);
    } else {
      onSelectionChange?.(selectedRows.filter(id => id !== rowId));
    }
    setSelectAll(false);
  };
  
  const handleRowClick = (row: T, e: React.MouseEvent): void => {
    // Don't navigate if clicking checkbox or action icons
    const target = e.target as HTMLElement;
    if (
      (target as HTMLInputElement).type === 'checkbox' || 
      target.closest('input[type="checkbox"]') ||
      target.closest('button') ||
      target.closest('[role="button"]')
    ) {
      return;
    }
    onRowClick?.(row);
  };
  
  // Check if all visible rows are selected
  const allSelected = paginatedData.length > 0 && 
    paginatedData.every(row => selectedRows.includes(getRowId(row)));
  
  if (loading) {
    return <LoadingSpinner message="Loading..." />;
  }
  
  if (data.length === 0) {
    return <EmptyState title={emptyMessage} />;
  }
  
  return (
    <TooltipProvider>
      <div className={cn("w-full", className)}>
        {/* Table */}
        <div className="border border-border rounded-lg overflow-hidden">
          <table className="w-full border-collapse">
            <thead>
              <tr className="border-b border-border bg-muted/30">
                {/* Checkbox column */}
                <th className="w-10 px-3 py-1.5">
                  <input
                    type="checkbox"
                    checked={allSelected}
                    onChange={(e) => handleSelectAll(e.target.checked)}
                    className="h-3.5 w-3.5 rounded border-border text-primary focus:ring-2 focus:ring-ring"
                    onClick={(e) => e.stopPropagation()}
                  />
                </th>
                
                {/* Data columns */}
                {columns.map((column, idx) => (
                  <th
                    key={column.key}
                    className={cn(
                      "px-3 py-1.5 text-xs font-semibold text-muted-foreground uppercase tracking-wide",
                      idx === 0 ? "text-left" : column.align === 'right' ? "text-right" : "text-center"
                    )}
                  >
                    {column.label}
                  </th>
                ))}
                
                {/* Action column */}
                {actions && (
                  <th className="w-24 px-3 py-1.5 text-xs font-semibold text-muted-foreground uppercase tracking-wide text-center">
                    Action
                  </th>
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {paginatedData.map((row) => {
                const rowId = getRowId(row);
                const isSelected = selectedRows.includes(rowId);
                
                return (
                  <tr
                    key={rowId}
                    onClick={(e) => handleRowClick(row, e)}
                    className={cn(
                      "transition-colors cursor-pointer",
                      isSelected && "bg-primary/5",
                      !isSelected && "hover:bg-muted/30"
                    )}
                  >
                    {/* Checkbox */}
                    <td className="px-3 py-1.5" onClick={(e) => e.stopPropagation()}>
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={(e) => handleRowSelect(rowId, e.target.checked)}
                        className="h-3.5 w-3.5 rounded border-border text-primary focus:ring-2 focus:ring-ring"
                        onClick={(e) => e.stopPropagation()}
                      />
                    </td>
                    
                    {/* Data cells */}
                    {columns.map((column, idx) => (
                      <td
                        key={column.key}
                        className={cn(
                          "px-3 py-1.5 text-xs",
                          idx === 0 
                            ? "text-left font-medium text-foreground" // Primary column
                            : column.align === 'right' 
                              ? "text-right text-muted-foreground"
                              : "text-center text-muted-foreground"
                        )}
                      >
                        {column.render 
                          ? column.render((row as any)[column.key], row)
                          : (row as any)[column.key] || '-'}
                      </td>
                    ))}
                    
                    {/* Action column */}
                    {actions && (
                      <td className="px-3 py-1.5 text-center" onClick={(e) => e.stopPropagation()}>
                        <ActionIcons
                          row={row}
                          {...actions}
                        />
                      </td>
                    )}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        
        {/* Compact Pagination */}
        {pagination && totalPages > 1 && (
          <div className="flex items-center justify-end gap-3 mt-2 px-1">
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <span>Rows per page:</span>
              <select
                value={pageSize}
                onChange={(e) => {
                  const newSize = parseInt(e.target.value);
                  setPageSize(newSize);
                  setCurrentPage(1);
                }}
                className="h-6 px-2 text-xs border border-border rounded bg-background"
                onClick={(e) => e.stopPropagation()}
              >
                <option value={10}>10</option>
                <option value={15}>15</option>
                <option value={20}>20</option>
                <option value={25}>25</option>
                <option value={50}>50</option>
              </select>
            </div>
            
            <div className="text-xs text-muted-foreground">
              {((currentPage - 1) * pageSize) + 1}–{Math.min(currentPage * pageSize, data.length)} of {data.length}
            </div>
            
            <div className="flex items-center gap-1">
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setCurrentPage(p => Math.max(1, p - 1));
                }}
                disabled={currentPage === 1}
                className="p-1.5 rounded hover:bg-muted disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setCurrentPage(p => Math.min(totalPages, p + 1));
                }}
                disabled={currentPage === totalPages}
                className="p-1.5 rounded hover:bg-muted disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}
      </div>
    </TooltipProvider>
  );
};

export default StandardListTable;

