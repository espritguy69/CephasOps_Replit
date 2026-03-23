import React from 'react';
import { Calculator } from 'lucide-react';
import { Card, Button } from '../ui';
import type { GponRateResolutionResult } from '../../types/rates';

interface RateCalculatorPanelProps {
  onCalculate: () => void;
  result: GponRateResolutionResult | null;
  loading?: boolean;
  canCalculate: boolean;
}

export const RateCalculatorPanel: React.FC<RateCalculatorPanelProps> = ({
  onCalculate,
  result,
  loading,
  canCalculate
}) => {
  const payoutSourceLabel =
    result?.payoutSource === 'GponSiCustomRate'
      ? 'Custom SI override'
      : result?.payoutSource === 'BaseWorkRate'
        ? 'Base work rate'
        : result?.payoutSource === 'GponSiJobRate'
          ? 'Legacy SI rate'
          : result?.payoutSource ?? '—';

  return (
    <Card
      title="Payout calculator"
      subtitle="Preview payout (and revenue) for the selected context using the live resolution engine"
    >
      <div className="space-y-4">
        <Button
          onClick={onCalculate}
          disabled={!canCalculate || loading}
          className="w-full sm:w-auto flex items-center gap-2"
        >
          <Calculator className="h-4 w-4" />
          {loading ? 'Calculating…' : 'Calculate payout'}
        </Button>

        {result && (
          <div className="rounded-lg border border-border bg-muted/30 p-4 space-y-3">
            {!result.success ? (
              <p className="text-sm text-destructive">{result.errorMessage ?? 'Resolution failed'}</p>
            ) : (
              <>
                <div className="grid grid-cols-2 gap-2 text-sm">
                  <span className="text-muted-foreground">Revenue</span>
                  <span className="font-medium text-right">
                    {result.revenueAmount != null ? `${result.currency} ${Number(result.revenueAmount).toFixed(2)}` : '—'}
                  </span>
                  <span className="text-muted-foreground">Payout (base)</span>
                  <span className="font-medium text-right">
                    {result.payoutAmount != null ? `${result.currency} ${Number(result.payoutAmount).toFixed(2)}` : '—'}
                  </span>
                  <span className="text-muted-foreground">Payout source</span>
                  <span className="text-right font-medium">{payoutSourceLabel}</span>
                  {result.grossMargin != null && (
                    <>
                      <span className="text-muted-foreground">Gross margin</span>
                      <span className="text-right">{result.currency} {Number(result.grossMargin).toFixed(2)}</span>
                    </>
                  )}
                  {result.marginPercentage != null && (
                    <>
                      <span className="text-muted-foreground">Margin %</span>
                      <span className="text-right">{Number(result.marginPercentage).toFixed(1)}%</span>
                    </>
                  )}
                </div>
              </>
            )}
          </div>
        )}
      </div>
    </Card>
  );
};
