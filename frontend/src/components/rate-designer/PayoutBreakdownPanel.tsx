import React from 'react';
import { Card } from '../ui';
import type { GponRateResolutionResult, ModifierTraceItemDto } from '../../types/rates';

interface PayoutBreakdownPanelProps {
  result: GponRateResolutionResult | null;
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

export const PayoutBreakdownPanel: React.FC<PayoutBreakdownPanelProps> = ({ result }) => {
  if (!result || !result.success) {
    return (
      <Card
        title="Payout breakdown"
        subtitle="How the payout was calculated. Run the calculator to see the breakdown."
      >
        <p className="text-sm text-muted-foreground">Run the calculator to see the breakdown.</p>
      </Card>
    );
  }

  const currency = result.currency || 'MYR';
  const baseAmount = result.baseAmountBeforeModifiers;
  const modifierTrace: ModifierTraceItemDto[] = result.modifierTrace ?? [];
  const finalAmount = result.payoutAmount;
  const isCustom = result.resolutionMatchLevel === 'Custom' || result.payoutPath === 'CustomOverride';

  const hasBreakdown = isCustom || (baseAmount != null && finalAmount != null);

  if (!hasBreakdown) {
    return (
      <Card
        title="Payout breakdown"
        subtitle="How the payout was calculated."
      >
        <p className="text-sm text-muted-foreground">No payout resolved for this context.</p>
      </Card>
    );
  }

  return (
    <Card
      title="Payout breakdown"
      subtitle="How the payout was calculated from base rate and modifiers."
    >
      <div className="space-y-3 text-sm">
        {/* Base rate */}
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
        {!isCustom && modifierTrace.length > 0 && (
          <div className="space-y-1 pl-0">
            {modifierTrace.map((m, i) => (
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
          </div>
        )}

        {/* Divider and final payout */}
        {finalAmount != null && (
          <>
            <div className="border-t border-border my-2" />
            <div className="flex justify-between items-center py-1">
              <span className="font-semibold">Final payout</span>
              <span className="font-bold text-base">
                {currency} {Number(finalAmount).toFixed(2)}
              </span>
            </div>
          </>
        )}
      </div>
    </Card>
  );
};
