import React from 'react';
import { Card, Badge } from '../ui';
import type { GponRateResolutionResult, ModifierTraceItemDto } from '../../types/rates';

export interface InstallerPayoutBreakdownPanelProps {
  /** Order/Job identifier for display */
  orderId?: string;
  /** Full rate resolution result from GET orders/{id}/payout-breakdown or payout-snapshot */
  result: GponRateResolutionResult | null;
  /** Optional order context (e.g. serviceId, ticketId) for header */
  orderLabel?: string;
  /** When from payout-snapshot endpoint: "Snapshot" = stored immutable, "Live" = resolved on demand */
  source?: 'Snapshot' | 'Live' | null;
}

function baseLabel(level: string | undefined, payoutPath: string | undefined): string {
  if (payoutPath === 'CustomOverride' || level === 'Custom') return 'Custom override';
  if (level === 'ExactCategory') return 'Base Work Rate (Exact Category)';
  if (level === 'ServiceProfile') return 'Base Work Rate (Service Profile)';
  if (level === 'BroadRateGroup') return 'Base Work Rate (Broad)';
  if (level === 'Legacy' || payoutPath === 'Legacy') return 'Legacy SI rate';
  return 'Base rate';
}

function modifierDisplayLabel(modifierType: string): string {
  switch (modifierType) {
    case 'InstallationMethod':
      return 'Installation Method modifier';
    case 'SITier':
      return 'SI Tier modifier';
    case 'Partner':
      return 'Partner modifier';
    default:
      return `${modifierType} modifier`;
  }
}

export const InstallerPayoutBreakdownPanel: React.FC<InstallerPayoutBreakdownPanelProps> = ({
  orderId,
  result,
  orderLabel,
  source
}) => {
  if (!result) {
    return (
      <Card
        title="Installer payout breakdown"
        subtitle="Enter an Order ID and load to see how the payout was calculated."
      >
        <p className="text-sm text-muted-foreground">No breakdown loaded.</p>
      </Card>
    );
  }

  if (!result.success) {
    return (
      <Card title="Installer payout breakdown" subtitle={orderId ? `Order ${orderId}` : undefined}>
        <p className="text-sm text-destructive">{result.errorMessage ?? 'Payout could not be resolved.'}</p>
      </Card>
    );
  }

  const currency = result.currency || 'MYR';
  const baseAmount = result.baseAmountBeforeModifiers;
  const modifierTrace: ModifierTraceItemDto[] = result.modifierTrace ?? [];
  const finalAmount = result.payoutAmount;
  const isCustom = result.resolutionMatchLevel === 'Custom' || result.payoutPath === 'CustomOverride';
  const hasBreakdown = isCustom || (baseAmount != null && finalAmount != null);

  return (
    <Card
      title="Installer payout breakdown"
      subtitle={orderLabel || orderId ? `Order ${orderId}` : 'How the payout was calculated'}
    >
      {source && (
        <div className="mb-3">
          <Badge variant={source === 'Snapshot' ? 'default' : 'secondary'}>
            {source === 'Snapshot' ? 'Snapshot' : 'Live calculation'}
          </Badge>
        </div>
      )}
      {!hasBreakdown ? (
        <p className="text-sm text-muted-foreground">No payout resolved for this order.</p>
      ) : (
        <>
          <div className="space-y-3 text-sm">
            {/* Base */}
            {isCustom ? (
              <div className="flex justify-between items-center py-1">
                <span className="text-muted-foreground">{baseLabel('Custom', result.payoutPath)}</span>
                <span className="font-medium">
                  {currency} {finalAmount != null ? Number(finalAmount).toFixed(2) : '—'}
                </span>
              </div>
            ) : (
              baseAmount != null && (
                <div className="flex justify-between items-center py-1">
                  <span className="text-muted-foreground">
                    {baseLabel(result.resolutionMatchLevel, result.payoutPath)}
                  </span>
                  <span>
                    {currency} {Number(baseAmount).toFixed(2)}
                  </span>
                </div>
              )
            )}

            {/* Modifiers */}
            {!isCustom &&
              modifierTrace.length > 0 &&
              modifierTrace.map((m, i) => (
                <div key={i} className="flex justify-between items-center py-0.5">
                  <span
                    className={
                      m.operation === 'Add'
                        ? 'text-emerald-600 dark:text-emerald-400'
                        : 'text-blue-600 dark:text-blue-400'
                    }
                  >
                    {modifierDisplayLabel(m.modifierType)}{' '}
                    {m.operation === 'Add' ? `+${currency}${Number(m.value).toFixed(2)}` : `×${m.value}`}
                  </span>
                  <span className="text-muted-foreground text-xs">
                    {Number(m.amountBefore).toFixed(2)} → {Number(m.amountAfter).toFixed(2)}
                  </span>
                </div>
              ))}

            {/* Divider and final */}
            {finalAmount != null && (
              <>
                <div className="border-t border-border my-2" />
                <div className="flex justify-between items-center py-1">
                  <span className="font-semibold">Final Installer Payout</span>
                  <span className="font-bold text-base">
                    {currency} {Number(finalAmount).toFixed(2)}
                  </span>
                </div>
              </>
            )}
          </div>

          {/* Job-level trace */}
          <div className="mt-4 pt-4 border-t border-border space-y-3 text-sm">
            <h4 className="font-medium text-muted-foreground">Resolution details</h4>
            <div className="rounded bg-muted/30 p-2 space-y-1 text-xs">
              <div className="flex flex-wrap items-center gap-2">
                <span>Matched rule:</span>
                <span className="font-medium">{result.payoutSource ?? '—'}</span>
                {result.payoutRateId && (
                  <Badge variant="outline" className="font-mono">
                    ID: {result.payoutRateId.slice(0, 8)}…
                  </Badge>
                )}
              </div>
              {result.matchedRuleDetails && (
                <div className="flex flex-wrap gap-x-4 gap-y-1 text-muted-foreground">
                  {result.matchedRuleDetails.rateGroupId && (
                    <span>Rate group: {result.matchedRuleDetails.rateGroupId.slice(0, 8)}…</span>
                  )}
                  {result.matchedRuleDetails.baseWorkRateId && (
                    <span>Base work rate: {result.matchedRuleDetails.baseWorkRateId.slice(0, 8)}…</span>
                  )}
                  {result.matchedRuleDetails.serviceProfileId && (
                    <span>Service profile: {result.matchedRuleDetails.serviceProfileId.slice(0, 8)}…</span>
                  )}
                  {result.matchedRuleDetails.customRateId && (
                    <span>Custom rate: {result.matchedRuleDetails.customRateId.slice(0, 8)}…</span>
                  )}
                  {result.matchedRuleDetails.legacyRateId && (
                    <span>Legacy rate: {result.matchedRuleDetails.legacyRateId.slice(0, 8)}…</span>
                  )}
                </div>
              )}
              {result.payoutPath && (
                <div>
                  <span className="text-muted-foreground">Path: </span>
                  <span className="font-mono">{result.payoutPath}</span>
                </div>
              )}
              {result.resolutionMatchLevel && (
                <div>
                  <span className="text-muted-foreground">Match level: </span>
                  <span>{result.resolutionMatchLevel}</span>
                </div>
              )}
            </div>
            {result.warnings && result.warnings.length > 0 && (
              <div className="rounded border border-amber-500/50 bg-amber-500/10 p-2 text-amber-800 dark:text-amber-200 text-xs">
                <span className="font-medium">Warnings:</span>
                <ul className="list-disc list-inside mt-1">
                  {result.warnings.map((w, i) => (
                    <li key={i}>{w}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </>
      )}
    </Card>
  );
};
