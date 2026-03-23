import React from 'react';
import { Card } from '../ui';
import { Select } from '../ui';
import type { RateDesignerContext, RateDesignerContextKey } from './types';

const SI_LEVELS = ['', 'Junior', 'Senior', 'Subcon'];

interface Option {
  id: string;
  name: string;
  code?: string;
}

interface RateContextPanelProps {
  context: RateDesignerContext;
  onChange: (key: RateDesignerContextKey, value: string) => void;
  orderTypes: Option[];
  orderSubtypes: Option[];
  orderCategories: Option[];
  installationMethods: Option[];
  partnerGroups: Option[];
  serviceInstallers: Option[];
  derivedRateGroupName: string;
  derivedServiceProfileName: string;
}

export const RateContextPanel: React.FC<RateContextPanelProps> = ({
  context,
  onChange,
  orderTypes,
  orderSubtypes,
  orderCategories,
  installationMethods,
  partnerGroups,
  serviceInstallers,
  derivedRateGroupName,
  derivedServiceProfileName
}) => {
  const select = (key: RateDesignerContextKey) => (e: React.ChangeEvent<HTMLSelectElement>) =>
    onChange(key, e.target.value || '');

  return (
    <Card title="Pricing context" subtitle="Select a job scenario to see matching rates and run the calculator">
      <div className="space-y-3">
        <Select
          label="Order type"
          value={context.orderTypeId}
          onChange={select('orderTypeId')}
          options={[{ value: '', label: '— Select —' }, ...orderTypes.map((o) => ({ value: o.id, label: `${o.name} (${o.code ?? o.id})` }))]}
        />
        <Select
          label="Order subtype (optional)"
          value={context.orderSubtypeId}
          onChange={select('orderSubtypeId')}
          options={[{ value: '', label: '— Any —' }, ...orderSubtypes.map((o) => ({ value: o.id, label: `${o.name} (${o.code ?? ''})` }))]}
        />
        <Select
          label="Order category"
          value={context.orderCategoryId}
          onChange={select('orderCategoryId')}
          options={[{ value: '', label: '— Select —' }, ...orderCategories.map((o) => ({ value: o.id, label: `${o.name} (${o.code ?? ''})` }))]}
        />
        <Select
          label="Installation method (optional)"
          value={context.installationMethodId}
          onChange={select('installationMethodId')}
          options={[{ value: '', label: '— Any —' }, ...installationMethods.map((o) => ({ value: o.id, label: `${o.name} (${o.code ?? ''})` }))]}
        />
        <div className="text-sm">
          <span className="text-muted-foreground">Rate group (from order type): </span>
          <span className="font-medium">{derivedRateGroupName || '—'}</span>
        </div>
        <div className="text-sm">
          <span className="text-muted-foreground">Service profile (from category): </span>
          <span className="font-medium">{derivedServiceProfileName || '—'}</span>
        </div>
        <Select
          label="SI tier (optional)"
          value={context.siLevel}
          onChange={select('siLevel')}
          options={SI_LEVELS.map((l) => ({ value: l, label: l || '— Any —' }))}
        />
        <Select
          label="Partner group (optional)"
          value={context.partnerGroupId}
          onChange={select('partnerGroupId')}
          options={[{ value: '', label: '— Any —' }, ...partnerGroups.map((o) => ({ value: o.id, label: o.name }))]}
        />
        <Select
          label="Service installer override (optional)"
          value={context.serviceInstallerId}
          onChange={select('serviceInstallerId')}
          options={[{ value: '', label: '— None —' }, ...serviceInstallers.map((o) => ({ value: o.id, label: o.name }))]}
        />
      </div>
    </Card>
  );
};
