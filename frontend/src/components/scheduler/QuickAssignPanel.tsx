import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { Search, Star, X, Check, Loader2 } from 'lucide-react';
import { cn } from '../../lib/utils';
import type { ServiceInstaller } from '../../types/serviceInstallers';
import type { CalendarSlot } from '../../types/scheduler';

export interface InstallerWorkload {
  installerId: string;
  jobCount: number;
  level: 'free' | 'medium' | 'overloaded';
}

export function computeWorkloads(
  installers: ServiceInstaller[],
  slots: CalendarSlot[]
): InstallerWorkload[] {
  return installers.map((inst) => {
    const jobCount = slots.filter(
      (s) => (s.serviceInstallerId || s.siId) === inst.id
    ).length;
    let level: 'free' | 'medium' | 'overloaded' = 'free';
    if (jobCount >= 6) level = 'overloaded';
    else if (jobCount >= 3) level = 'medium';
    return { installerId: inst.id, jobCount, level };
  });
}

export function getRecommendedInstaller(
  installers: ServiceInstaller[],
  workloads: InstallerWorkload[]
): { installer: ServiceInstaller; reason: string } | null {
  if (installers.length === 0) return null;

  const sorted = [...installers].sort((a, b) => {
    const wA = workloads.find((w) => w.installerId === a.id);
    const wB = workloads.find((w) => w.installerId === b.id);
    return (wA?.jobCount ?? 0) - (wB?.jobCount ?? 0);
  });

  const best = sorted[0];
  const bestWorkload = workloads.find((w) => w.installerId === best.id);
  const jobCount = bestWorkload?.jobCount ?? 0;

  let reason = 'Available';
  if (jobCount === 0) reason = 'Available - No jobs today';
  else if (jobCount <= 2) reason = 'Low workload';
  else reason = 'Lowest workload';

  return { installer: best, reason };
}

const WORKLOAD_COLORS = {
  free: 'bg-emerald-500',
  medium: 'bg-amber-500',
  overloaded: 'bg-red-500',
} as const;

const WORKLOAD_TEXT_COLORS = {
  free: 'text-emerald-600',
  medium: 'text-amber-600',
  overloaded: 'text-red-600',
} as const;

const WORKLOAD_BG_COLORS = {
  free: 'bg-emerald-50 dark:bg-emerald-950/30',
  medium: 'bg-amber-50 dark:bg-amber-950/30',
  overloaded: 'bg-red-50 dark:bg-red-950/30',
} as const;

interface NavigableItem {
  type: 'recommended' | 'installer';
  installer: ServiceInstaller;
  reason?: string;
}

interface QuickAssignPanelProps {
  installers: ServiceInstaller[];
  workloads: InstallerWorkload[];
  recommended: { installer: ServiceInstaller; reason: string } | null;
  onAssign: (installerId: string) => void;
  onClose: () => void;
  isSubmitting?: boolean;
  className?: string;
}

const QuickAssignPanel: React.FC<QuickAssignPanelProps> = ({
  installers,
  workloads,
  recommended,
  onAssign,
  onClose,
  isSubmitting = false,
  className,
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [highlightIndex, setHighlightIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(searchQuery), 200);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  const filtered = useMemo(() => {
    if (!debouncedQuery.trim()) return installers;
    const q = debouncedQuery.toLowerCase();
    return installers.filter(
      (i) =>
        i.name.toLowerCase().includes(q) ||
        (i.siLevel || '').toLowerCase().includes(q) ||
        (i.installerType || '').toLowerCase().includes(q)
    );
  }, [installers, debouncedQuery]);

  const navigableItems = useMemo((): NavigableItem[] => {
    const items: NavigableItem[] = [];
    const isSearching = debouncedQuery.trim().length > 0;
    if (!isSearching && recommended) {
      items.push({
        type: 'recommended',
        installer: recommended.installer,
        reason: recommended.reason,
      });
    }
    for (const inst of filtered) {
      items.push({ type: 'installer', installer: inst });
    }
    return items;
  }, [filtered, recommended, debouncedQuery]);

  useEffect(() => {
    setHighlightIndex(0);
  }, [navigableItems]);

  const workloadMapLocal = useMemo(() => {
    const map: Record<string, InstallerWorkload> = {};
    for (const w of workloads) map[w.installerId] = w;
    return map;
  }, [workloads]);

  const getWorkload = useCallback(
    (id: string) => workloadMapLocal[id],
    [workloadMapLocal]
  );

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setHighlightIndex((prev) => Math.min(prev + 1, navigableItems.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setHighlightIndex((prev) => Math.max(prev - 1, 0));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      const item = navigableItems[highlightIndex];
      if (item) onAssign(item.installer.id);
    } else if (e.key === 'Escape') {
      onClose();
    }
  };

  useEffect(() => {
    const el = listRef.current?.querySelector(`[data-nav-index="${highlightIndex}"]`) as HTMLElement | undefined;
    el?.scrollIntoView({ block: 'nearest' });
  }, [highlightIndex]);

  const isSearching = debouncedQuery.trim().length > 0;

  return (
    <div
      className={cn(
        'w-72 bg-popover border rounded-xl shadow-xl overflow-hidden animate-in fade-in-0 slide-in-from-top-2 duration-150',
        className
      )}
      role="listbox"
      aria-label="Select installer"
      onKeyDown={handleKeyDown}
    >
      <div className="flex items-center gap-2 px-3 py-2.5 border-b bg-muted/30">
        <Search className="h-4 w-4 text-muted-foreground shrink-0" />
        <input
          ref={inputRef}
          type="text"
          placeholder="Search installers..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="flex-1 bg-transparent text-sm outline-none placeholder:text-muted-foreground"
          role="combobox"
          aria-expanded="true"
          aria-autocomplete="list"
          aria-activedescendant={navigableItems[highlightIndex] ? `nav-item-${highlightIndex}` : undefined}
        />
        <button
          onClick={onClose}
          className="shrink-0 p-0.5 rounded hover:bg-muted transition-colors"
          aria-label="Close"
        >
          <X className="h-3.5 w-3.5 text-muted-foreground" />
        </button>
      </div>

      <div ref={listRef} className="max-h-64 overflow-y-auto">
        {isSubmitting && (
          <div className="flex items-center justify-center py-6 gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Assigning...
          </div>
        )}

        {!isSubmitting && navigableItems.length === 0 && (
          <div className="py-6 text-center text-sm text-muted-foreground">
            No installers found
          </div>
        )}

        {!isSubmitting &&
          navigableItems.map((item, idx) => {
            const w = getWorkload(item.installer.id);
            const isHighlighted = highlightIndex === idx;

            if (item.type === 'recommended') {
              return (
                <button
                  key={`rec-${item.installer.id}`}
                  id={`nav-item-${idx}`}
                  data-nav-index={idx}
                  type="button"
                  role="option"
                  aria-selected={isHighlighted}
                  onClick={() => onAssign(item.installer.id)}
                  className={cn(
                    'w-full text-left px-3 py-2.5 flex items-center gap-3 border-b-2 border-primary/20 transition-colors',
                    'bg-primary/5 hover:bg-primary/10',
                    isHighlighted && 'ring-2 ring-inset ring-primary/30'
                  )}
                >
                  <div className="shrink-0 flex items-center justify-center h-8 w-8 rounded-full bg-primary/15 text-primary">
                    <Star className="h-4 w-4 fill-current" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="text-sm font-semibold text-primary truncate">
                      {item.installer.name}
                    </div>
                    <div className="text-xs text-primary/70">{item.reason}</div>
                  </div>
                  {w && <WorkloadBadge workload={w} />}
                </button>
              );
            }

            const isRecommendedDuplicate =
              !isSearching && recommended?.installer.id === item.installer.id;

            return (
              <button
                key={item.installer.id}
                id={`nav-item-${idx}`}
                data-nav-index={idx}
                type="button"
                role="option"
                aria-selected={isHighlighted}
                onClick={() => onAssign(item.installer.id)}
                className={cn(
                  'w-full text-left px-3 py-2 flex items-center gap-3 transition-colors',
                  'hover:bg-accent',
                  isHighlighted && 'bg-accent',
                  isRecommendedDuplicate && 'opacity-50'
                )}
              >
                <div className="shrink-0 flex items-center justify-center h-7 w-7 rounded-full bg-muted text-muted-foreground text-xs font-semibold">
                  {item.installer.name
                    .split(/\s+/)
                    .map((s) => s[0])
                    .join('')
                    .toUpperCase()
                    .slice(0, 2)}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium truncate">{item.installer.name}</div>
                  {item.installer.siLevel && (
                    <div className="text-xs text-muted-foreground">{item.installer.siLevel}</div>
                  )}
                </div>
                {w && <WorkloadBadge workload={w} />}
              </button>
            );
          })}
      </div>

      <div className="px-3 py-2 border-t bg-muted/20 text-xs text-muted-foreground flex items-center gap-3">
        <span>
          <kbd className="px-1 py-0.5 rounded bg-muted border text-[10px]">&uarr;&darr;</kbd> navigate
        </span>
        <span>
          <kbd className="px-1 py-0.5 rounded bg-muted border text-[10px]">Enter</kbd> assign
        </span>
        <span>
          <kbd className="px-1 py-0.5 rounded bg-muted border text-[10px]">Esc</kbd> close
        </span>
      </div>
    </div>
  );
};

function WorkloadBadge({ workload }: { workload: InstallerWorkload }) {
  return (
    <div
      className={cn(
        'shrink-0 flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium',
        WORKLOAD_BG_COLORS[workload.level]
      )}
    >
      <div className={cn('h-2 w-2 rounded-full', WORKLOAD_COLORS[workload.level])} />
      <span className={WORKLOAD_TEXT_COLORS[workload.level]}>{workload.jobCount}</span>
    </div>
  );
}

export { WorkloadBadge };
export default QuickAssignPanel;
