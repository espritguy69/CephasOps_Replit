import React, { useState } from 'react';
import { Search, User } from 'lucide-react';
import { getOrderPayoutSnapshot } from '../../api/orders';
import { getJobEarningRecords } from '../../api/payroll';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { InstallerPayoutBreakdownPanel, InstallerEarningsSummaryPanel } from '../../components/installer-payout';
import { Button, Card, TextInput, useToast, LoadingSpinner } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { GponRateResolutionResult } from '../../types/rates';
import type { JobEarningRecord } from '../../types/payroll';
import type { ServiceInstaller } from '../../types/serviceInstallers';

/**
 * Installer Payout Breakdown – Operations
 * Route: /operations/installer-payout-breakdown
 * View how installer payouts are calculated for a given order and optional earnings summary by installer.
 */
const InstallerPayoutBreakdownPage: React.FC = () => {
  const { showError, showSuccess } = useToast();
  const [orderIdInput, setOrderIdInput] = useState('');
  const [breakdownResult, setBreakdownResult] = useState<GponRateResolutionResult | null>(null);
  const [payoutSource, setPayoutSource] = useState<'Snapshot' | 'Live' | null>(null);
  const [orderLabel, setOrderLabel] = useState<string | undefined>();
  const [loadingBreakdown, setLoadingBreakdown] = useState(false);

  const [installers, setInstallers] = useState<ServiceInstaller[]>([]);
  const [selectedSiId, setSelectedSiId] = useState<string | null>(null);
  const [earningsRecords, setEarningsRecords] = useState<JobEarningRecord[]>([]);
  const [loadingEarnings, setLoadingEarnings] = useState(false);
  const [earningsLoaded, setEarningsLoaded] = useState(false);

  const loadBreakdown = async () => {
    const id = orderIdInput.trim();
    if (!id) {
      showError('Enter an Order ID');
      return;
    }
    setLoadingBreakdown(true);
    setBreakdownResult(null);
    setPayoutSource(null);
    setOrderLabel(undefined);
    try {
      const { source, result } = await getOrderPayoutSnapshot(id);
      setBreakdownResult(result);
      setPayoutSource(source);
      setOrderLabel(`Order ${id}`);
      if (result.success) {
        showSuccess(source === 'Snapshot' ? 'Payout snapshot loaded' : 'Live payout breakdown loaded');
      }
    } catch (err: any) {
      showError(err?.message ?? 'Failed to load payout');
      setBreakdownResult(null);
      setPayoutSource(null);
    } finally {
      setLoadingBreakdown(false);
    }
  };

  const loadInstallers = async () => {
    if (installers.length > 0) return;
    try {
      const list = await getServiceInstallers({ isActive: true });
      setInstallers(list ?? []);
    } catch {
      setInstallers([]);
    }
  };

  const loadEarningsSummary = async () => {
    if (!selectedSiId) return;
    setLoadingEarnings(true);
    setEarningsLoaded(false);
    try {
      const records = await getJobEarningRecords({ siId: selectedSiId });
      setEarningsRecords(records ?? []);
      setEarningsLoaded(true);
    } catch (err: any) {
      showError(err?.message ?? 'Failed to load earnings');
      setEarningsRecords([]);
    } finally {
      setLoadingEarnings(false);
    }
  };

  const selectedInstaller = selectedSiId ? installers.find((i) => i.id === selectedSiId) : null;

  return (
    <PageShell
      title="Installer Payout Breakdown"
      breadcrumbs={[
        { label: 'Operations', path: '/orders' },
        { label: 'Installer Payout Breakdown', path: '/operations/installer-payout-breakdown' }
      ]}
    >
      <div className="space-y-6 max-w-4xl">
        <Card title="Look up payout by order" subtitle="Enter Order ID or Job ID to see how the installer payout was calculated.">
          <div className="flex flex-wrap items-end gap-3">
            <div className="min-w-[200px] flex-1">
              <TextInput
                label="Order ID / Job ID"
                value={orderIdInput}
                onChange={(e) => setOrderIdInput(e.target.value)}
                placeholder="e.g. 550e8400-e29b-41d4-a716-446655440000"
              />
            </div>
            <Button onClick={loadBreakdown} disabled={loadingBreakdown}>
              {loadingBreakdown ? <LoadingSpinner className="w-4 h-4" /> : <Search className="w-4 h-4" />}
              {loadingBreakdown ? ' Loading…' : ' Load breakdown'}
            </Button>
          </div>
        </Card>

        <InstallerPayoutBreakdownPanel
          orderId={orderIdInput.trim() || undefined}
          result={breakdownResult}
          orderLabel={orderLabel}
          source={payoutSource}
        />

        <section>
          <h2 className="text-lg font-semibold mb-2">Installer earnings summary</h2>
          <p className="text-sm text-muted-foreground mb-4">
            Optional: view total jobs and total payout for an installer.
          </p>
          <div className="flex flex-wrap items-end gap-3 mb-4">
            <div className="min-w-[200px]">
              <label className="block text-sm font-medium mb-1">Installer</label>
              <select
                className="flex min-h-[44px] w-full rounded-lg border border-input bg-background px-4 py-3 text-base"
                value={selectedSiId ?? ''}
                onChange={(e) => {
                  const v = e.target.value || null;
                  setSelectedSiId(v);
                  setEarningsLoaded(false);
                  setEarningsRecords([]);
                }}
                onFocus={loadInstallers}
              >
                <option value="">Select installer…</option>
                {installers.map((si) => (
                  <option key={si.id} value={si.id}>
                    {si.name ?? si.id}
                  </option>
                ))}
              </select>
            </div>
            <Button
              onClick={loadEarningsSummary}
              disabled={!selectedSiId || loadingEarnings}
              variant="outline"
            >
              {loadingEarnings ? <LoadingSpinner className="w-4 h-4" /> : <User className="w-4 h-4" />}
              {loadingEarnings ? ' Loading…' : ' Load summary'}
            </Button>
          </div>
          {earningsLoaded && selectedInstaller && (
            <InstallerEarningsSummaryPanel
              installerName={selectedInstaller.name ?? selectedSiId ?? '—'}
              records={earningsRecords}
              currency="MYR"
            />
          )}
        </section>
      </div>
    </PageShell>
  );
};

export default InstallerPayoutBreakdownPage;
