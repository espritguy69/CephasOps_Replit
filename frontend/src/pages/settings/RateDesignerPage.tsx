import React, { useState, useEffect, useCallback } from 'react';
import { PageShell } from '../../components/layout';
import {
  RateContextPanel,
  BaseRatePanel,
  ModifierPanel,
  OverridePanel,
  RateCalculatorPanel,
  PayoutBreakdownPanel,
  SimulationSandboxPanel,
  DebugTracePanel,
  emptyRateDesignerContext,
  type RateDesignerContext,
  type RateDesignerContextKey
} from '../../components/rate-designer';
import { getOrderTypeParents, getOrderTypeSubtypes, getOrderTypes } from '../../api/orderTypes';
import { getOrderCategories } from '../../api/orderCategories';
import { getInstallationMethods } from '../../api/installationMethods';
import { getRateGroups, getRateGroupMappings } from '../../api/rateGroups';
import { getServiceProfileMappings } from '../../api/serviceProfiles';
import { getPartnerGroups } from '../../api/partnerGroups';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { getBaseWorkRates } from '../../api/rateGroups';
import { getGponSiCustomRates, resolveGponRates } from '../../api/rates';
import { useToast } from '../../components/ui';
import type { OrderTypeDto } from '../../types/orderTypes';
import type { ReferenceDataItem } from '../../types/referenceData';
import type { InstallationMethod } from '../../types/installationMethods';
import type { RateGroupDto, OrderTypeSubtypeRateGroupMappingDto } from '../../types/rateGroups';
import type { OrderCategoryServiceProfileDto } from '../../api/serviceProfiles';
import type { BaseWorkRateDto } from '../../types/rateGroups';
import type { GponSiCustomRate, GponRateResolutionResult } from '../../types/rates';
import type { PartnerGroup } from '../../types/partnerGroups';
import type { ServiceInstaller } from '../../types/serviceInstallers';

export const RateDesignerPage: React.FC = () => {
  const { showError } = useToast();
  const [context, setContext] = useState<RateDesignerContext>(emptyRateDesignerContext);

  const [orderTypeParents, setOrderTypeParents] = useState<OrderTypeDto[]>([]);
  const [orderSubtypes, setOrderSubtypes] = useState<OrderTypeDto[]>([]);
  const [orderCategories, setOrderCategories] = useState<ReferenceDataItem[]>([]);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [rateGroupMappings, setRateGroupMappings] = useState<OrderTypeSubtypeRateGroupMappingDto[]>([]);
  const [serviceProfileMappings, setServiceProfileMappings] = useState<OrderCategoryServiceProfileDto[]>([]);
  const [partnerGroups, setPartnerGroups] = useState<PartnerGroup[]>([]);
  const [serviceInstallers, setServiceInstallers] = useState<ServiceInstaller[]>([]);

  const [baseWorkRates, setBaseWorkRates] = useState<BaseWorkRateDto[]>([]);
  const [customRates, setCustomRates] = useState<GponSiCustomRate[]>([]);
  const [loadingBwr, setLoadingBwr] = useState(false);
  const [loadingOverrides, setLoadingOverrides] = useState(false);

  const [calcResult, setCalcResult] = useState<GponRateResolutionResult | null>(null);
  const [loadingCalc, setLoadingCalc] = useState(false);
  const [lastRequestContext, setLastRequestContext] = useState<{
    orderTypeId?: string;
    orderCategoryId?: string;
    installationMethodId?: string;
    siLevel?: string;
    partnerGroupId?: string;
    serviceInstallerId?: string;
  } | null>(null);

  const [loadingRef, setLoadingRef] = useState(true);

  const derivedRateGroup = ((): { id: string; name: string } | null => {
    if (!context.orderTypeId) return null;
    const withSub = context.orderSubtypeId
      ? rateGroupMappings.find(
          (m) => m.orderTypeId === context.orderTypeId && m.orderSubtypeId === context.orderSubtypeId
        )
      : rateGroupMappings.find(
          (m) => m.orderTypeId === context.orderTypeId && !m.orderSubtypeId
        );
    if (withSub)
      return {
        id: withSub.rateGroupId,
        name: withSub.rateGroupName ?? withSub.rateGroupCode ?? withSub.rateGroupId
      };
    return null;
  })();

  const derivedServiceProfile = ((): { id: string; name: string } | null => {
    if (!context.orderCategoryId) return null;
    const m = serviceProfileMappings.find((m) => m.orderCategoryId === context.orderCategoryId);
    if (m) return { id: m.serviceProfileId, name: m.serviceProfileName ?? m.serviceProfileCode ?? m.serviceProfileId };
    return null;
  })();

  const handleContextChange = useCallback((key: RateDesignerContextKey, value: string) => {
    setContext((prev) => ({ ...prev, [key]: value }));
    if (key === 'orderTypeId') setContext((prev) => ({ ...prev, orderSubtypeId: '' }));
  }, []);

  useEffect(() => {
    const load = async () => {
      try {
        setLoadingRef(true);
        const [parents, categories, methods, mappings, profileMappings, partners, installers] = await Promise.all([
          getOrderTypeParents({}),
          getOrderCategories({}),
          getInstallationMethods({}),
          getRateGroupMappings({}),
          getServiceProfileMappings({}),
          getPartnerGroups(),
          getServiceInstallers({})
        ]);
        setOrderTypeParents(Array.isArray(parents) ? parents : []);
        setOrderCategories(Array.isArray(categories) ? categories : []);
        setInstallationMethods(Array.isArray(methods) ? methods : []);
        setRateGroupMappings(Array.isArray(mappings) ? mappings : []);
        setServiceProfileMappings(Array.isArray(profileMappings) ? profileMappings : []);
        setPartnerGroups(Array.isArray(partners) ? partners : []);
        setServiceInstallers(Array.isArray(installers) ? installers : []);
      } catch (e) {
        showError((e as Error).message ?? 'Failed to load reference data');
      } finally {
        setLoadingRef(false);
      }
    };
    load();
  }, [showError]);

  useEffect(() => {
    if (!context.orderTypeId) {
      setOrderSubtypes([]);
      return;
    }
    getOrderTypeSubtypes(context.orderTypeId)
      .then((sub) => setOrderSubtypes(Array.isArray(sub) ? sub : []))
      .catch(() => setOrderSubtypes([]));
  }, [context.orderTypeId]);

  useEffect(() => {
    if (!derivedRateGroup?.id) {
      setBaseWorkRates([]);
      return;
    }
    setLoadingBwr(true);
    getBaseWorkRates({
      rateGroupId: derivedRateGroup.id,
      isActive: true
    })
      .then((list) => setBaseWorkRates(Array.isArray(list) ? list : []))
      .catch(() => setBaseWorkRates([]))
      .finally(() => setLoadingBwr(false));
  }, [derivedRateGroup?.id]);

  useEffect(() => {
    if (!context.orderTypeId || !context.orderCategoryId) {
      setCustomRates([]);
      return;
    }
    setLoadingOverrides(true);
    getGponSiCustomRates({
      orderTypeId: context.orderTypeId,
      orderCategoryId: context.orderCategoryId,
      serviceInstallerId: context.serviceInstallerId || undefined
    })
      .then((list) => setCustomRates(Array.isArray(list) ? list : []))
      .catch(() => setCustomRates([]))
      .finally(() => setLoadingOverrides(false));
  }, [context.orderTypeId, context.orderCategoryId, context.serviceInstallerId]);

  const handleCalculate = useCallback(async () => {
    if (!context.orderTypeId || !context.orderCategoryId) {
      showError('Select order type and order category to run the calculator.');
      return;
    }
    setLoadingCalc(true);
    setCalcResult(null);
    setLastRequestContext({
      orderTypeId: context.orderTypeId,
      orderCategoryId: context.orderCategoryId,
      installationMethodId: context.installationMethodId || undefined,
      siLevel: context.siLevel || undefined,
      partnerGroupId: context.partnerGroupId || undefined,
      serviceInstallerId: context.serviceInstallerId || undefined
    });
    try {
      const result = await resolveGponRates({
        orderTypeId: context.orderTypeId,
        orderCategoryId: context.orderCategoryId,
        installationMethodId: context.installationMethodId || undefined,
        partnerGroupId: context.partnerGroupId || undefined,
        serviceInstallerId: context.serviceInstallerId || undefined,
        siLevel: context.siLevel || undefined
      });
      setCalcResult(result);
    } catch (e) {
      showError((e as Error).message ?? 'Calculation failed');
      setCalcResult({ success: false, errorMessage: (e as Error).message, currency: 'MYR', resolutionSteps: [] });
    } finally {
      setLoadingCalc(false);
    }
  }, [context, showError]);

  const orderTypesForSelect = orderTypeParents;
  const options = (arr: { id: string; name: string; code?: string }[]) =>
    arr.map((o) => ({ id: o.id, name: o.name, code: (o as { code?: string }).code }));

  if (loadingRef) {
    return (
      <PageShell
        title="Rate Designer"
        breadcrumbs={[{ label: 'Settings' }, { label: 'GPON' }, { label: 'Rate Designer' }]}
      >
        <p className="text-muted-foreground">Loading…</p>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Rate Designer"
      breadcrumbs={[{ label: 'Settings' }, { label: 'GPON' }, { label: 'Rate Designer' }]}
    >
      <div className="flex flex-col lg:flex-row gap-4 p-4 max-w-[1600px] mx-auto">
        <aside className="w-full lg:w-72 shrink-0">
          <RateContextPanel
            context={context}
            onChange={handleContextChange}
            orderTypes={options(orderTypesForSelect)}
            orderSubtypes={options(orderSubtypes)}
            orderCategories={options(orderCategories)}
            installationMethods={options(installationMethods)}
            partnerGroups={partnerGroups.map((p) => ({ id: p.id, name: p.name }))}
            serviceInstallers={serviceInstallers.map((s) => ({ id: s.id, name: s.name }))}
            derivedRateGroupName={derivedRateGroup?.name ?? ''}
            derivedServiceProfileName={derivedServiceProfile?.name ?? ''}
          />
        </aside>

        <main className="flex-1 min-w-0 space-y-4">
          <section>
            <BaseRatePanel baseWorkRates={baseWorkRates} loading={loadingBwr} />
          </section>
          <section>
            <ModifierPanel />
          </section>
          <section>
            <OverridePanel overrides={customRates} loading={loadingOverrides} />
          </section>
        </main>

        <aside className="w-full lg:w-96 shrink-0 space-y-4">
          <RateCalculatorPanel
            onCalculate={handleCalculate}
            result={calcResult}
            loading={loadingCalc}
            canCalculate={!!context.orderTypeId && !!context.orderCategoryId}
          />
          <PayoutBreakdownPanel result={calcResult} />
          <SimulationSandboxPanel result={calcResult} />
          <DebugTracePanel result={calcResult} requestContext={lastRequestContext} />
        </aside>
      </div>
    </PageShell>
  );
};

export default RateDesignerPage;
