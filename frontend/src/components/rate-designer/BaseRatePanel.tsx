import React from 'react';
import { Link } from 'react-router-dom';
import { Card, Badge } from '../ui';
import type { BaseWorkRateDto } from '../../types/rateGroups';

interface BaseRatePanelProps {
  baseWorkRates: BaseWorkRateDto[];
  loading?: boolean;
}

function scopeBadge(row: BaseWorkRateDto): { label: string; variant: 'default' | 'secondary' | 'outline' } {
  if (row.orderCategoryId)
    return { label: 'Exact category', variant: 'default' };
  if (row.serviceProfileId)
    return { label: 'Shared profile', variant: 'secondary' };
  return { label: 'Broad', variant: 'outline' };
}

export const BaseRatePanel: React.FC<BaseRatePanelProps> = ({ baseWorkRates, loading }) => {
  return (
    <Card
      title="Base work rates"
      subtitle="Rates that match the selected context. Exact category wins over shared profile over broad."
      footer={
        <Link
          to="/settings/gpon/rate-groups"
          className="text-sm text-primary hover:underline"
        >
          Manage base work rates →
        </Link>
      }
    >
      {loading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : baseWorkRates.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          No base work rates match the current context. Select a rate group and category, or add rates in Rate Groups.
        </p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border">
                <th className="text-left py-2 pr-2 font-medium">Scope</th>
                <th className="text-left py-2 pr-2 font-medium">Rate group</th>
                <th className="text-left py-2 pr-2 font-medium">Installation / Subtype</th>
                <th className="text-right py-2 pr-2 font-medium">Amount</th>
                <th className="text-left py-2 font-medium">Active</th>
              </tr>
            </thead>
            <tbody>
              {baseWorkRates.map((row) => {
                const scope = scopeBadge(row);
                return (
                  <tr key={row.id} className="border-b border-border/50">
                    <td className="py-2 pr-2">
                      <Badge variant={scope.variant}>{scope.label}</Badge>
                    </td>
                    <td className="py-2 pr-2">{row.rateGroupCode ?? row.rateGroupName ?? '—'}</td>
                    <td className="py-2 pr-2">
                      {[row.installationMethodCode ?? row.installationMethodName, row.orderSubtypeCode ?? row.orderSubtypeName]
                        .filter(Boolean)
                        .join(' / ') || '—'}
                    </td>
                    <td className="py-2 pr-2 text-right font-medium">
                      {row.currency} {Number(row.amount).toFixed(2)}
                    </td>
                    <td className="py-2">{row.isActive ? 'Yes' : 'No'}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
};
