import React from 'react';
import { LucideIcon } from 'lucide-react';

interface KpiCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon?: LucideIcon;
  trend?: 'up' | 'down' | 'neutral';
  trendValue?: string;
}

const KpiCard: React.FC<KpiCardProps> = ({ title, value, subtitle, icon: Icon, trend, trendValue }) => {
  return (
    <div className="rounded-xl bg-layout-card shadow-card p-5">
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          {Icon && (
            <div className="h-10 w-10 rounded-lg bg-brand-50 flex items-center justify-center">
              <Icon className="h-5 w-5 text-brand-600" />
            </div>
          )}
          <div>
            <h3 className="text-sm font-semibold text-slate-900">{title}</h3>
            {subtitle && (
              <p className="text-xs text-slate-600 mt-0.5">{subtitle}</p>
            )}
          </div>
        </div>
      </div>
      
      <div className="flex items-baseline gap-2">
        <div className="text-2xl font-bold text-slate-900">{value}</div>
        {trend && trendValue && (
          <div className={`flex items-center gap-1 text-xs font-medium ${
            trend === 'up' ? 'text-green-600' : trend === 'down' ? 'text-red-600' : 'text-slate-500'
          }`}>
            {trend === 'up' ? (
              <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
              </svg>
            ) : trend === 'down' ? (
              <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 17h8m0 0V9m0 8l-8-8-4 4-6-6" />
              </svg>
            ) : null}
            {trendValue}
          </div>
        )}
      </div>
    </div>
  );
};

export default KpiCard;

