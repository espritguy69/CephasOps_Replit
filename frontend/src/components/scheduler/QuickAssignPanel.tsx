import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { Search, Star, X, Loader2, ShieldAlert, TrendingUp } from 'lucide-react';
import { cn } from '../../lib/utils';
import type { ServiceInstaller } from '../../types/serviceInstallers';
import type { InstallerWorkload, ScoringResult } from '../../lib/scheduler/scoringEngine';

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
  type: 'top-pick' | 'installer';
  installer: ServiceInstaller;
  scoringResult?: ScoringResult;
  rank?: number;
}

interface QuickAssignPanelProps {
  installers: ServiceInstaller[];
  workloads: InstallerWorkload[];
  rankedScores?: ScoringResult[];
  onAssign: (installerId: string) => void;
  onClose: () => void;
  isSubmitting?: boolean;
  mode?: 'single' | 'bulk-distribute';
  className?: string;
}

const QuickAssignPanel: React.FC<QuickAssignPanelProps> = ({
  installers,
  workloads,
  rankedScores,
  onAssign,
  onClose,
  isSubmitting = false,
  mode = 'single',
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

  const scoreMap = useMemo(() => {
    const map = new Map<string, ScoringResult>();
    if (rankedScores) {
      for (const s of rankedScores) map.set(s.installerId, s);
    }
    return map;
  }, [rankedScores]);

  const topPicks = useMemo((): ScoringResult[] => {
    if (!rankedScores || debouncedQuery.trim()) return [];
    return rankedScores.filter((r) => !r.blocked && r.score > 0).slice(0, 3);
  }, [rankedScores, debouncedQuery]);

  const navigableItems = useMemo((): NavigableItem[] => {
    const items: NavigableItem[] = [];
    const isSearching = debouncedQuery.trim().length > 0;

    if (!isSearching) {
      topPicks.forEach((pick, idx) => {
        const inst = installers.find((i) => i.id === pick.installerId);
        if (inst) {
          items.push({ type: 'top-pick', installer: inst, scoringResult: pick, rank: idx + 1 });
        }
      });
    }

    for (const inst of filtered) {
      items.push({
        type: 'installer',
        installer: inst,
        scoringResult: scoreMap.get(inst.id),
      });
    }
    return items;
  }, [filtered, topPicks, installers, scoreMap, debouncedQuery]);

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
      if (item && !(item.scoringResult?.blocked)) onAssign(item.installer.id);
    } else if (e.key === 'Escape') {
      onClose();
    }
  };

  useEffect(() => {
    const el = listRef.current?.querySelector(`[data-nav-index="${highlightIndex}"]`) as HTMLElement | undefined;
    el?.scrollIntoView({ block: 'nearest' });
  }, [highlightIndex]);

  const isSearching = debouncedQuery.trim().length > 0;
  const topPickIds = new Set(topPicks.map((p) => p.installerId));

  return (
    <div
      className={cn(
        'w-80 bg-popover border rounded-xl shadow-xl overflow-hidden animate-in fade-in-0 slide-in-from-top-2 duration-150',
        'max-sm:fixed max-sm:inset-x-0 max-sm:bottom-0 max-sm:top-auto max-sm:w-full max-sm:rounded-t-2xl max-sm:rounded-b-none max-sm:border-t max-sm:border-x-0 max-sm:border-b-0 max-sm:shadow-2xl max-sm:max-h-[70vh] max-sm:animate-in max-sm:slide-in-from-bottom-4 max-sm:z-[100]',
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

      <div ref={listRef} className="max-h-72 overflow-y-auto">
        {isSubmitting && (
          <div className="flex items-center justify-center py-6 gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            {mode === 'bulk-distribute' ? 'Distributing...' : 'Assigning...'}
          </div>
        )}

        {!isSubmitting && navigableItems.length === 0 && (
          <div className="py-6 text-center text-sm text-muted-foreground">
            No installers found
          </div>
        )}

        {!isSubmitting && topPicks.length > 0 && !isSearching && (
          <div className="px-3 py-1.5 bg-primary/5 border-b">
            <div className="flex items-center gap-1.5 text-xs font-medium text-primary">
              <TrendingUp className="h-3 w-3" />
              <span>Smart Recommendations</span>
            </div>
          </div>
        )}

        {!isSubmitting &&
          navigableItems.map((item, idx) => {
            const w = getWorkload(item.installer.id);
            const isHighlighted = highlightIndex === idx;
            const isBlocked = item.scoringResult?.blocked;
            const isTopPick = item.type === 'top-pick';
            const isTopPickDuplicate = !isSearching && topPickIds.has(item.installer.id) && item.type === 'installer';

            if (isTopPick && !isSearching) {
              const isBest = item.rank === 1;
              return (
                <button
                  key={`top-${item.installer.id}`}
                  id={`nav-item-${idx}`}
                  data-nav-index={idx}
                  type="button"
                  role="option"
                  aria-selected={isHighlighted}
                  onClick={() => onAssign(item.installer.id)}
                  className={cn(
                    'w-full text-left px-3 py-2.5 flex items-center gap-3 transition-colors',
                    isBest
                      ? 'bg-primary/5 hover:bg-primary/10 border-b border-primary/10'
                      : 'hover:bg-accent border-b border-transparent',
                    isHighlighted && (isBest ? 'ring-2 ring-inset ring-primary/30' : 'bg-accent'),
                    idx === topPicks.length - 1 && 'border-b-2 border-b-border'
                  )}
                >
                  <div className="relative shrink-0">
                    <div
                      className={cn(
                        'flex items-center justify-center rounded-full font-semibold',
                        isBest
                          ? 'h-8 w-8 bg-primary/15 text-primary'
                          : 'h-7 w-7 bg-muted text-muted-foreground text-xs'
                      )}
                    >
                      {isBest ? (
                        <Star className="h-4 w-4 fill-current" />
                      ) : (
                        <span className="text-xs font-bold">#{item.rank}</span>
                      )}
                    </div>
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className={cn('text-sm font-medium truncate', isBest && 'font-semibold text-primary')}>
                      {item.installer.name}
                    </div>
                    <div className="text-xs text-muted-foreground truncate">
                      {item.scoringResult?.reasons.slice(0, 3).join(' · ') || 'Available'}
                    </div>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <ScoreBadge score={item.scoringResult?.score ?? 0} />
                    {w && <UtilizationMini workload={w} />}
                  </div>
                </button>
              );
            }

            return (
              <button
                key={item.installer.id}
                id={`nav-item-${idx}`}
                data-nav-index={idx}
                type="button"
                role="option"
                aria-selected={isHighlighted}
                aria-disabled={isBlocked}
                onClick={() => !isBlocked && onAssign(item.installer.id)}
                className={cn(
                  'w-full text-left px-3 py-2 flex items-center gap-3 transition-colors',
                  isBlocked
                    ? 'opacity-40 cursor-not-allowed'
                    : 'hover:bg-accent cursor-pointer',
                  isHighlighted && !isBlocked && 'bg-accent',
                  isTopPickDuplicate && 'opacity-40'
                )}
              >
                {isBlocked ? (
                  <div className="shrink-0 flex items-center justify-center h-7 w-7 rounded-full bg-red-100 dark:bg-red-900/30 text-red-500">
                    <ShieldAlert className="h-3.5 w-3.5" />
                  </div>
                ) : (
                  <div className="shrink-0 flex items-center justify-center h-7 w-7 rounded-full bg-muted text-muted-foreground text-xs font-semibold">
                    {item.installer.name
                      .split(/\s+/)
                      .map((s) => s[0])
                      .join('')
                      .toUpperCase()
                      .slice(0, 2)}
                  </div>
                )}
                <div className="flex-1 min-w-0">
                  <div className="text-sm font-medium truncate">{item.installer.name}</div>
                  {isBlocked ? (
                    <div className="text-xs text-red-500 truncate">{item.scoringResult?.blockReason}</div>
                  ) : item.scoringResult ? (
                    <div className="text-xs text-muted-foreground truncate">
                      {item.scoringResult.reasons.slice(0, 2).join(' · ')}
                    </div>
                  ) : (
                    item.installer.siLevel && (
                      <div className="text-xs text-muted-foreground">{item.installer.siLevel}</div>
                    )
                  )}
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  {item.scoringResult && !isBlocked && (
                    <ScoreBadge score={item.scoringResult.score} />
                  )}
                  {w && <UtilizationMini workload={w} />}
                </div>
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

function ScoreBadge({ score }: { score: number }) {
  const color =
    score >= 70
      ? 'text-emerald-600 bg-emerald-50 dark:bg-emerald-950/30'
      : score >= 40
        ? 'text-amber-600 bg-amber-50 dark:bg-amber-950/30'
        : 'text-red-600 bg-red-50 dark:bg-red-950/30';

  return (
    <span className={cn('text-[10px] font-bold px-1.5 py-0.5 rounded-md tabular-nums', color)}>
      {Math.round(score)}
    </span>
  );
}

function UtilizationMini({ workload }: { workload: InstallerWorkload }) {
  return (
    <div className="flex items-center gap-1">
      <div className="w-8 h-1.5 bg-muted rounded-full overflow-hidden">
        <div
          className={cn(
            'h-full rounded-full',
            WORKLOAD_COLORS[workload.level]
          )}
          style={{ width: `${Math.min(workload.utilizationPct, 100)}%` }}
        />
      </div>
      <span className={cn('text-[10px] tabular-nums', WORKLOAD_TEXT_COLORS[workload.level])}>
        {workload.utilizationPct}%
      </span>
    </div>
  );
}

export function WorkloadBadge({ workload }: { workload: InstallerWorkload }) {
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

export default QuickAssignPanel;
