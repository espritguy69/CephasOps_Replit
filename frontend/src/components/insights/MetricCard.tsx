import React from 'react';
import type { LucideIcon } from 'lucide-react';
import { Card } from '../ui';

interface MetricCardProps {
  title: string;
  value: number | string;
  subtitle?: string;
  icon?: LucideIcon;
  iconBg?: string;
  loading?: boolean;
}

export const MetricCard: React.FC<MetricCardProps> = ({
  title,
  value,
  subtitle,
  icon: Icon,
  iconBg = 'bg-primary/10',
  loading = false
}) => (
  <Card className="p-4">
    <div className="flex items-start justify-between gap-3">
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-muted-foreground">{title}</p>
        {loading ? (
          <div className="h-8 w-20 bg-muted animate-pulse rounded mt-1" />
        ) : (
          <p className="text-2xl font-bold tracking-tight mt-1">{value}</p>
        )}
        {subtitle != null && subtitle !== '' && (
          <p className="text-xs text-muted-foreground mt-1">{subtitle}</p>
        )}
      </div>
      {Icon && (
        <div className={`h-10 w-10 rounded-lg flex items-center justify-center flex-shrink-0 ${iconBg}`}>
          <Icon className="h-5 w-5 text-muted-foreground" />
        </div>
      )}
    </div>
  </Card>
);
