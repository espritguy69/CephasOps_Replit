import React, { useState, useMemo, useEffect } from 'react';
import { Card, TextInput } from '../ui';
import type { GponRateResolutionResult, ModifierTraceItemDto } from '../../types/rates';

interface SimulationSandboxPanelProps {
  result: GponRateResolutionResult | null;
}

function modifierLabel(modifierType: string): string {
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

/**
 * Compute simulated payout from base + modifier overrides (same formula as engine: base then Add/Multiply in order).
 * Used only for display; no production data is changed.
 */
function computeSimulatedPayout(
  baseAmount: number | undefined,
  modifierTrace: ModifierTraceItemDto[],
  draftBase: string | undefined,
  modifierOverrides: Record<number, string>
): { simulated: number; steps: string[] } {
  const steps: string[] = [];
  let current = baseAmount;
  if (current == null) return { simulated: 0, steps: [] };

  const baseOverride = draftBase != null && draftBase.trim() !== '' ? parseFloat(draftBase.trim()) : null;
  if (baseOverride != null && !Number.isNaN(baseOverride)) {
    steps.push('Base amount override applied');
    current = baseOverride;
  }

  modifierTrace.forEach((m, i) => {
    const raw = modifierOverrides[i];
    const overrideVal = raw != null && raw.trim() !== '' ? parseFloat(raw.trim()) : null;
    const val = overrideVal != null && !Number.isNaN(overrideVal) ? overrideVal : m.value;
    if (overrideVal != null && !Number.isNaN(overrideVal)) steps.push(`${modifierLabel(m.modifierType)} override applied`);
    current = m.operation === 'Add' ? current + val : current * val;
  });

  return { simulated: current, steps };
}

export const SimulationSandboxPanel: React.FC<SimulationSandboxPanelProps> = ({ result }) => {
  const [draftBaseAmount, setDraftBaseAmount] = useState<string>('');
  const [modifierOverrides, setModifierOverrides] = useState<Record<number, string>>({});
  const [draftPayoutOverride, setDraftPayoutOverride] = useState<string>('');

  // Clear draft overrides when result changes (e.g. after new Calculate) so simulation doesn't apply to stale breakdown
  useEffect(() => {
    setDraftBaseAmount('');
    setModifierOverrides({});
    setDraftPayoutOverride('');
  }, [result]);

  const currency = result?.currency || 'MYR';
  const currentPayout = result?.success && result.payoutAmount != null ? result.payoutAmount : null;
  const baseAmount = result?.baseAmountBeforeModifiers;
  const modifierTrace: ModifierTraceItemDto[] = result?.modifierTrace ?? [];
  const isCustom = result?.resolutionMatchLevel === 'Custom' || result?.payoutPath === 'CustomOverride';

  const { simulated, steps: draftSteps } = useMemo(() => {
    if (isCustom) {
      const draft = draftPayoutOverride.trim() !== '' ? parseFloat(draftPayoutOverride.trim()) : null;
      const sim = draft != null && !Number.isNaN(draft) ? draft : (currentPayout ?? 0);
      const steps = draft != null && !Number.isNaN(draft) ? ['Custom payout override applied'] : [];
      return { simulated: sim, steps };
    }
    return computeSimulatedPayout(baseAmount ?? undefined, modifierTrace, draftBaseAmount, modifierOverrides);
  }, [isCustom, currentPayout, draftPayoutOverride, baseAmount, modifierTrace, draftBaseAmount, modifierOverrides]);

  const hasAnyDraft =
    (draftBaseAmount.trim() !== '' && !Number.isNaN(parseFloat(draftBaseAmount))) ||
    (isCustom && draftPayoutOverride.trim() !== '' && !Number.isNaN(parseFloat(draftPayoutOverride))) ||
    modifierTrace.some((_, i) => {
      const v = modifierOverrides[i];
      return v != null && v.trim() !== '' && !Number.isNaN(parseFloat(v));
    });

  const delta = currentPayout != null ? simulated - currentPayout : null;

  if (!result || !result.success || currentPayout == null) {
    return (
      <Card
        title="Simulation sandbox"
        subtitle="Test hypothetical pricing. Run the calculator first, then adjust draft values below."
      >
        <p className="text-sm text-muted-foreground">Run the calculator to enable simulation.</p>
      </Card>
    );
  }

  return (
    <Card
      title="Simulation sandbox"
      subtitle="Test hypothetical base and modifier values. Changes here are not saved; compare current vs simulated."
    >
      <div className="space-y-4 text-sm">
        {/* Current payout */}
        <section>
          <h4 className="font-medium text-muted-foreground mb-1">Current payout</h4>
          <div className="rounded bg-muted/30 px-3 py-2 font-medium">
            {currency} {Number(currentPayout).toFixed(2)}
          </div>
        </section>

        {/* Draft adjustments */}
        <section>
          <h4 className="font-medium text-muted-foreground mb-2">Draft adjustments</h4>
          <p className="text-xs text-muted-foreground mb-2">
            Override values to see simulated payout. Leave blank to use current value.
          </p>

          {isCustom ? (
            <div className="space-y-2">
              <TextInput
                label="Draft payout (override custom amount)"
                type="number"
                value={draftPayoutOverride}
                onChange={(e) => setDraftPayoutOverride(e.target.value)}
                placeholder={String(currentPayout)}
              />
            </div>
          ) : (
            <div className="space-y-2">
              <TextInput
                label="Draft base amount"
                type="number"
                value={draftBaseAmount}
                onChange={(e) => setDraftBaseAmount(e.target.value)}
                placeholder={baseAmount != null ? String(baseAmount) : '—'}
              />
              {modifierTrace.map((m, i) => (
                <TextInput
                  key={i}
                  label={`${modifierLabel(m.modifierType)} (${m.operation === 'Add' ? '+' : '×'})`}
                  type="number"
                  value={modifierOverrides[i] ?? ''}
                  onChange={(e) => setModifierOverrides((prev) => ({ ...prev, [i]: e.target.value }))}
                  placeholder={String(m.value)}
                />
              ))}
            </div>
          )}
        </section>

        {/* Simulated payout + Difference */}
        <section>
          <h4 className="font-medium text-muted-foreground mb-1">Simulated payout</h4>
          <div className="rounded bg-muted/30 px-3 py-2 font-medium">
            {currency} {Number(simulated).toFixed(2)}
          </div>
          {delta != null && hasAnyDraft && (
            <>
              <h4 className="font-medium text-muted-foreground mt-2 mb-1">Difference</h4>
              <div
                className={`rounded px-3 py-2 font-medium ${
                  delta > 0
                    ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300'
                    : delta < 0
                      ? 'bg-rose-100 text-rose-800 dark:bg-rose-900/30 dark:text-rose-300'
                      : 'bg-muted/30'
                }`}
              >
                {delta >= 0 ? '+' : ''}{currency} {Number(delta).toFixed(2)}
              </div>
            </>
          )}
        </section>

        {/* Which draft caused the difference */}
        {hasAnyDraft && draftSteps.length > 0 && (
          <section>
            <h4 className="font-medium text-muted-foreground mb-1">Simulation trace</h4>
            <ul className="text-xs text-muted-foreground list-disc pl-4 space-y-0.5">
              {draftSteps.map((s, i) => (
                <li key={i}>{s}</li>
              ))}
            </ul>
          </section>
        )}
      </div>
    </Card>
  );
};
