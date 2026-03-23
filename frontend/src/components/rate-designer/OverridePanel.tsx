import React from 'react';
import { Link } from 'react-router-dom';
import { Card } from '../ui';
import type { GponSiCustomRate } from '../../types/rates';

interface OverridePanelProps {
  overrides: GponSiCustomRate[];
  loading?: boolean;
}

export const OverridePanel: React.FC<OverridePanelProps> = ({ overrides, loading }) => {
  return (
    <Card
      title="Installer overrides"
      subtitle="Custom payout rates for specific installers. These take precedence over base and legacy rates."
      footer={
        <Link to="/settings/gpon/rate-engine" className="text-sm text-primary hover:underline">
          Manage SI custom rates →
        </Link>
      }
    >
      {loading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : overrides.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          No installer-specific overrides match the current context.
        </p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border">
                <th className="text-left py-2 pr-2 font-medium">Installer</th>
                <th className="text-left py-2 pr-2 font-medium">Category / Method</th>
                <th className="text-right py-2 pr-2 font-medium">Amount</th>
                <th className="text-left py-2 font-medium">Active</th>
              </tr>
            </thead>
            <tbody>
              {overrides.map((row) => (
                <tr key={row.id} className="border-b border-border/50">
                  <td className="py-2 pr-2">{row.serviceInstallerName ?? row.serviceInstallerId}</td>
                  <td className="py-2 pr-2">
                    {[row.orderCategoryName ?? row.orderCategoryId, row.installationMethodName ?? row.installationMethodId]
                      .filter(Boolean)
                      .join(' / ') || '—'}
                  </td>
                  <td className="py-2 pr-2 text-right font-medium">
                    {row.currency} {Number(row.customPayoutAmount).toFixed(2)}
                  </td>
                  <td className="py-2">{row.isActive ? 'Yes' : 'No'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
};
