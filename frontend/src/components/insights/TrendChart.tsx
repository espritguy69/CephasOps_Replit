import React from 'react';
import { Card } from '../ui';

export interface TrendDataPoint {
  label: string;
  value: number;
}

interface TrendChartProps {
  title: string;
  data: TrendDataPoint[];
  loading?: boolean;
}

export const TrendChart: React.FC<TrendChartProps> = ({ title, data, loading = false }) => (
  <Card className="p-4">
    <h3 className="text-sm font-medium text-muted-foreground mb-3">{title}</h3>
    {loading ? (
      <div className="h-48 bg-muted animate-pulse rounded" />
    ) : data.length === 0 ? (
      <p className="text-sm text-muted-foreground">No trend data</p>
    ) : (
      <div className="flex items-end gap-1 h-48">
        {data.map((d, i) => {
          const max = Math.max(...data.map((x) => x.value), 1);
          const pct = max > 0 ? (d.value / max) * 100 : 0;
          return (
            <div
              key={d.label}
              className="flex-1 min-w-0 flex flex-col items-center gap-1"
              title={`${d.label}: ${d.value}`}
            >
              <div
                className="w-full bg-primary/60 rounded-t min-h-[4px] transition-all"
                style={{ height: `${Math.max(pct, 4)}%` }}
              />
              <span className="text-xs text-muted-foreground truncate w-full text-center">
                {d.label}
              </span>
            </div>
          );
        })}
      </div>
    )}
  </Card>
);
