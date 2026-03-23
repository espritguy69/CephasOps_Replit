import React from 'react';
import { Card } from '../ui';
import type { JobEarningRecord } from '../../types/payroll';

export interface InstallerEarningsSummaryPanelProps {
  /** Installer display name */
  installerName: string;
  /** Job earning records for this installer (e.g. from getJobEarningRecords({ siId })) */
  records: JobEarningRecord[];
  /** Currency for display (default MYR) */
  currency?: string;
}

export const InstallerEarningsSummaryPanel: React.FC<InstallerEarningsSummaryPanelProps> = ({
  installerName,
  records,
  currency = 'MYR'
}) => {
  const count = records.length;
  const total = records.reduce((sum, r) => sum + (r.finalPay ?? r.baseRate ?? r.amount ?? 0), 0);
  const average = count > 0 ? total / count : 0;

  return (
    <Card
      title="Installer earnings summary"
      subtitle="Total payout and average per job for the selected installer."
    >
      <div className="space-y-3 text-sm">
        <div className="flex justify-between items-center py-1">
          <span className="text-muted-foreground">Installer</span>
          <span className="font-medium">{installerName || '—'}</span>
        </div>
        <div className="flex justify-between items-center py-1">
          <span className="text-muted-foreground">Jobs completed</span>
          <span>{count}</span>
        </div>
        <div className="border-t border-border my-2" />
        <div className="flex justify-between items-center py-1">
          <span className="text-muted-foreground">Total payout</span>
          <span className="font-semibold">
            {currency} {total.toFixed(2)}
          </span>
        </div>
        <div className="flex justify-between items-center py-1">
          <span className="text-muted-foreground">Average payout</span>
          <span className="font-medium">
            {currency} {average.toFixed(2)}
          </span>
        </div>
      </div>
    </Card>
  );
};
