import React from 'react';
import { Card } from '../ui';

export interface StatusItem {
  status: string;
  count: number;
}

interface StatusDistributionProps {
  title: string;
  items: StatusItem[];
  loading?: boolean;
}

export const StatusDistribution: React.FC<StatusDistributionProps> = ({
  title,
  items,
  loading = false
}) => {
  const total = items.reduce((s, i) => s + i.count, 0);
  return (
    <Card className="p-4">
      <h3 className="text-sm font-medium text-muted-foreground mb-3">{title}</h3>
      {loading ? (
        <div className="space-y-2">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-6 bg-muted animate-pulse rounded" />
          ))}
        </div>
      ) : items.length === 0 ? (
        <p className="text-sm text-muted-foreground">No data</p>
      ) : (
        <div className="space-y-2">
          {items.map((item) => (
            <div key={item.status} className="flex items-center justify-between gap-2">
              <span className="text-sm capitalize">{item.status}</span>
              <span className="text-sm font-medium">
                {item.count}
                {total > 0 && (
                  <span className="text-muted-foreground ml-1">
                    ({Math.round((item.count / total) * 100)}%)
                  </span>
                )}
              </span>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
};
