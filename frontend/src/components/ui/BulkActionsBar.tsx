import React from 'react';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { LucideIcon } from 'lucide-react';

interface BulkAction {
  label: string;
  icon?: LucideIcon;
  onClick: () => void;
  variant?: 'default' | 'destructive';
}

interface BulkActionsBarProps {
  selectedCount: number;
  onClearSelection: () => void;
  actions?: BulkAction[];
}

/**
 * BulkActionsBar - Shows when rows are selected
 * Displays selected count and action buttons
 */
const BulkActionsBar: React.FC<BulkActionsBarProps> = ({
  selectedCount,
  onClearSelection,
  actions = []
}) => {
  if (selectedCount === 0) return null;
  
  return (
    <div className="flex items-center justify-between px-4 py-2 bg-primary/5 border-b border-border mb-2 rounded-t-lg">
      <div className="flex items-center gap-3">
        <span className="text-xs font-medium text-foreground">
          {selectedCount} {selectedCount === 1 ? 'item' : 'items'} selected
        </span>
        <button
          onClick={onClearSelection}
          className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1 transition-colors"
        >
          <X className="h-3 w-3" />
          Clear
        </button>
      </div>
      
      <div className="flex items-center gap-2">
        {actions.map((action, idx) => {
          const Icon = action.icon;
          return (
            <button
              key={idx}
              onClick={action.onClick}
              className={cn(
                "px-2 py-1 text-xs font-medium rounded transition-colors",
                action.variant === 'destructive'
                  ? "bg-red-100 text-red-700 hover:bg-red-200 dark:bg-red-900/30 dark:text-red-400"
                  : "bg-background border border-border hover:bg-muted",
                "flex items-center gap-1.5"
              )}
            >
              {Icon && <Icon className="h-3.5 w-3.5" />}
              {action.label}
            </button>
          );
        })}
      </div>
    </div>
  );
};

export default BulkActionsBar;

