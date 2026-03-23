import React from 'react';
import { Card, Badge } from '../ui';
import type { GponRateResolutionResult, ResolutionContextDto, ModifierTraceItemDto } from '../../types/rates';

interface RequestContextSnapshot {
  orderTypeId?: string;
  orderCategoryId?: string;
  installationMethodId?: string;
  siLevel?: string;
  partnerGroupId?: string;
  serviceInstallerId?: string;
}

interface DebugTracePanelProps {
  result: GponRateResolutionResult | null;
  requestContext?: RequestContextSnapshot | null;
}

function resolutionLevelLabel(level: string | undefined): string {
  switch (level) {
    case 'Custom':
      return 'Custom override';
    case 'ExactCategory':
      return 'Exact category';
    case 'ServiceProfile':
      return 'Service profile';
    case 'BroadRateGroup':
      return 'Broad rate';
    case 'Legacy':
      return 'Legacy fallback';
    default:
      return level ?? '—';
  }
}

function resolutionPathSummary(result: GponRateResolutionResult): string {
  if (result.resolutionMatchLevel === 'Custom') return 'CustomOverride (no modifiers)';
  const base = result.payoutPath === 'Legacy' ? 'Legacy' : `BaseWorkRate (${result.resolutionMatchLevel ?? result.payoutPath ?? 'BaseWorkRate'})`;
  const hasModifiers = result.modifierTrace && result.modifierTrace.length > 0;
  return hasModifiers ? `${base} → Modifiers` : base;
}

export const DebugTracePanel: React.FC<DebugTracePanelProps> = ({ result, requestContext }) => {
  if (!result) {
    return (
      <Card
        title="Debug trace"
        subtitle="Request context, matched rule, path, and warnings appear here after you run the calculator."
      >
        <p className="text-sm text-muted-foreground">Run the calculator to see the debug trace.</p>
      </Card>
    );
  }

  const ctx: ResolutionContextDto | null = result.resolutionContext ?? null;
  const hasWarnings = result.warnings && result.warnings.length > 0;
  const isLegacy = result.resolutionMatchLevel === 'Legacy';
  const modifierTrace: ModifierTraceItemDto[] = result.modifierTrace ?? [];

  return (
    <Card
      title="Debug trace"
      subtitle="Support view: why this payout happened, which rule matched, and what modifiers applied."
    >
      <div className="space-y-4 text-sm">
        {/* Request context: prefer API echo, else UI snapshot */}
        <section>
          <h4 className="font-medium text-muted-foreground mb-1">Request context</h4>
          <div className="rounded bg-muted/30 p-2 font-mono text-xs space-y-0.5">
            {(ctx ?? requestContext) ? (
              <>
                {(ctx?.orderTypeId ?? requestContext?.orderTypeId) && <div>Order type: {ctx?.orderTypeId ?? requestContext?.orderTypeId}</div>}
                {(ctx?.orderCategoryId ?? requestContext?.orderCategoryId) && <div>Order category: {ctx?.orderCategoryId ?? requestContext?.orderCategoryId}</div>}
                {(ctx?.orderSubtypeId) && <div>Order subtype: {ctx.orderSubtypeId}</div>}
                {(ctx?.installationMethodId ?? requestContext?.installationMethodId) && <div>Installation method: {ctx?.installationMethodId ?? requestContext?.installationMethodId}</div>}
                {(ctx?.siTier ?? requestContext?.siLevel) && <div>SI tier: {ctx?.siTier ?? requestContext?.siLevel}</div>}
                {(ctx?.partnerGroupId ?? requestContext?.partnerGroupId) && <div>Partner group: {ctx?.partnerGroupId ?? requestContext?.partnerGroupId}</div>}
                {(ctx?.companyId) && <div>Company: {ctx.companyId}</div>}
                {(ctx?.effectiveDateUsed) && <div>Effective date: {typeof ctx.effectiveDateUsed === 'string' ? ctx.effectiveDateUsed.slice(0, 19) : String(ctx.effectiveDateUsed)}</div>}
                {requestContext?.serviceInstallerId && <div>Service installer: {requestContext.serviceInstallerId}</div>}
              </>
            ) : (
              <span className="text-muted-foreground">—</span>
            )}
          </div>
        </section>

        {/* Matched rule */}
        <section>
          <h4 className="font-medium text-muted-foreground mb-1">Matched rule</h4>
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-medium">{result.payoutSource ?? '—'}</span>
            {result.payoutRateId && (
              <Badge variant="outline" className="font-mono text-xs">
                ID: {result.payoutRateId.slice(0, 8)}…
              </Badge>
            )}
          </div>
          {result.matchedRuleDetails && (
            <div className="mt-1 font-mono text-xs text-muted-foreground">
              {result.matchedRuleDetails.rateGroupId && <span>Rate group: {result.matchedRuleDetails.rateGroupId.slice(0, 8)}… </span>}
              {result.matchedRuleDetails.baseWorkRateId && <span>BaseWorkRate: {result.matchedRuleDetails.baseWorkRateId.slice(0, 8)}… </span>}
              {result.matchedRuleDetails.serviceProfileId && <span>Service profile: {result.matchedRuleDetails.serviceProfileId.slice(0, 8)}… </span>}
              {result.matchedRuleDetails.legacyRateId && <span>Legacy: {result.matchedRuleDetails.legacyRateId.slice(0, 8)}… </span>}
              {result.matchedRuleDetails.customRateId && <span>Custom: {result.matchedRuleDetails.customRateId.slice(0, 8)}… </span>}
            </div>
          )}
        </section>

        {/* Resolution path with level badge */}
        <section>
          <h4 className="font-medium text-muted-foreground mb-1">Resolution path</h4>
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant={isLegacy ? 'warning' : 'secondary'}>
              {resolutionLevelLabel(result.resolutionMatchLevel)}
            </Badge>
            <span className="text-muted-foreground">{resolutionPathSummary(result)}</span>
          </div>
          {result.baseAmountBeforeModifiers != null && result.payoutAmount !== result.baseAmountBeforeModifiers && (
            <p className="mt-1 text-muted-foreground">
              Base {result.currency} {Number(result.baseAmountBeforeModifiers).toFixed(2)} → after modifiers {result.currency} {Number(result.payoutAmount).toFixed(2)}
            </p>
          )}
        </section>

        {/* Modifier execution */}
        <section>
          <h4 className="font-medium text-muted-foreground mb-1">Modifier execution</h4>
          {modifierTrace.length === 0 ? (
            <p className="text-muted-foreground">No modifiers applied.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr className="border-b border-border">
                    <th className="text-left py-1 pr-2 font-medium">Type</th>
                    <th className="text-left py-1 pr-2 font-medium">Operation</th>
                    <th className="text-right py-1 pr-2 font-medium">Value</th>
                    <th className="text-right py-1 font-medium">Before → After</th>
                  </tr>
                </thead>
                <tbody>
                  {modifierTrace.map((m, i) => (
                    <tr key={i} className="border-b border-border/50">
                      <td className="py-1 pr-2">{m.modifierType}</td>
                      <td className="py-1 pr-2">{m.operation}</td>
                      <td className="py-1 pr-2 text-right">{m.operation === 'Multiply' ? `×${m.value}` : `+${m.value}`}</td>
                      <td className="py-1 text-right">{Number(m.amountBefore).toFixed(2)} → {Number(m.amountAfter).toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        {/* Warnings */}
        {hasWarnings && (
          <section>
            <h4 className="font-medium text-amber-600 dark:text-amber-500 mb-1">Warnings</h4>
            <ul className="list-disc pl-4 space-y-0.5 text-amber-700 dark:text-amber-400">
              {result.warnings!.map((w, i) => (
                <li key={i}>{w}</li>
              ))}
            </ul>
          </section>
        )}

        {/* Final result */}
        {result.success && result.payoutAmount != null && (
          <section>
            <h4 className="font-medium text-muted-foreground mb-1">Final result</h4>
            <div className="rounded bg-muted/30 px-3 py-2 font-medium">
              Payout: {result.currency} {Number(result.payoutAmount).toFixed(2)}
            </div>
          </section>
        )}

        {/* Resolution steps (full log) */}
        <section>
          <h4 className="font-medium text-muted-foreground mb-1">Resolution steps</h4>
          {result.resolutionSteps.length === 0 ? (
            <p className="text-muted-foreground">No steps recorded.</p>
          ) : (
            <ul className="list-none pl-0 font-mono text-xs bg-muted/20 rounded p-3 max-h-40 overflow-y-auto space-y-1">
              {result.resolutionSteps.map((step, i) => (
                <li key={i} className="border-l-2 border-primary/30 pl-2 py-0.5">
                  {step}
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </Card>
  );
};
