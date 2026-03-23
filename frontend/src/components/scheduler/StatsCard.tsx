import React from 'react';
import { LucideIcon, TrendingUp, TrendingDown } from 'lucide-react';
import Card from '../ui/Card';
import { cn } from '../../lib/utils';

interface StatsCardProps {
  title: string;
  value: string | number;
  icon: LucideIcon;
  trend?: {
    value: number;
    positive: boolean;
  };
  className?: string;
}

/**
 * Modern Stats Card with animations and glass morphism
 * Uses Tailwind 4.0 features: backdrop blur, smooth transitions, hover effects
 */
export const StatsCard: React.FC<StatsCardProps> = ({
  title,
  value,
  icon: Icon,
  trend,
  className
}) => {
  return (
    <Card
      className={cn(
        "relative overflow-hidden backdrop-blur-sm bg-card/95",
        "hover:shadow-lg transition-spring group",
        className
      )}
    >
      {/* Animated background gradient */}
      <div className="absolute inset-0 bg-gradient-to-br from-primary/5 to-transparent opacity-0 group-hover:opacity-100 transition-smooth" />
      
      <div className="p-4 relative">
        <div className="flex items-center justify-between">
          <div className="space-y-1">
            <p className="text-xs font-medium text-muted-foreground">{title}</p>
            <p className="text-2xl font-bold tracking-tight group-hover:scale-105 transition-spring origin-left">
              {value}
            </p>
            {trend && (
              <div className="flex items-center gap-1 text-xs">
                {trend.positive ? (
                  <TrendingUp className="h-3 w-3 text-green-600 dark:text-green-400" />
                ) : (
                  <TrendingDown className="h-3 w-3 text-red-600 dark:text-red-400" />
                )}
                <span className={trend.positive ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}>
                  {Math.abs(trend.value)}%
                </span>
              </div>
            )}
          </div>
          <div className="rounded-lg bg-primary/10 p-2.5 group-hover:scale-110 group-hover:bg-primary/20 transition-spring">
            <Icon className="h-5 w-5 text-primary" />
          </div>
        </div>
      </div>
    </Card>
  );
};

