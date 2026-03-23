/**
 * Rate Grouping Utilities
 * Functions to group rates by various dimensions for better UI organization
 */

import type { GponPartnerJobRate, GponSiJobRate, GponSiCustomRate } from '../types/rates';

export type RateType = GponPartnerJobRate | GponSiJobRate | GponSiCustomRate;

export interface GroupedRateData<T extends RateType = RateType> {
  groupKey: string;
  groupLabel: string;
  rates: T[];
  subGroups?: {
    orderType: string;
    orderTypeName: string;
    rates: T[];
  }[];
}

/**
 * Get group key for a rate based on grouping strategy
 */
function getGroupKey<T extends RateType>(
  rate: T,
  groupBy: 'partnerGroup' | 'siLevel' | 'installer'
): string {
  switch (groupBy) {
    case 'partnerGroup':
      return (rate as GponPartnerJobRate | GponSiJobRate).partnerGroupId || 'no-group';
    case 'siLevel':
      return (rate as GponSiJobRate).siLevel || 'unknown';
    case 'installer':
      return (rate as GponSiCustomRate).serviceInstallerId || 'unknown';
    default:
      return 'unknown';
  }
}

/**
 * Get group label for display
 */
function getGroupLabel<T extends RateType>(
  key: string,
  groupBy: 'partnerGroup' | 'siLevel' | 'installer',
  rates: T[]
): string {
  if (rates.length === 0) return key;
  
  switch (groupBy) {
    case 'partnerGroup': {
      const rate = rates[0] as GponPartnerJobRate | GponSiJobRate;
      return rate.partnerGroupName || key || 'No Partner Group';
    }
    case 'siLevel':
      return key || 'Unknown Level';
    case 'installer': {
      const rate = rates[0] as GponSiCustomRate;
      return rate.serviceInstallerName || key || 'Unknown Installer';
    }
    default:
      return key;
  }
}

/**
 * Create sub-groups by Order Type
 */
function createSubGroups<T extends RateType>(rates: T[]): {
  orderType: string;
  orderTypeName: string;
  rates: T[];
}[] {
  const subGroupsMap = new Map<string, T[]>();
  
  rates.forEach(rate => {
    const orderTypeId = rate.orderTypeId || 'no-type';
    const orderTypeName = rate.orderTypeName || 'Unknown';
    const key = `${orderTypeId}|${orderTypeName}`;
    
    if (!subGroupsMap.has(key)) {
      subGroupsMap.set(key, []);
    }
    subGroupsMap.get(key)!.push(rate);
  });
  
  return Array.from(subGroupsMap.entries()).map(([key, groupRates]) => {
    const [orderTypeId, orderTypeName] = key.split('|');
    return {
      orderType: orderTypeId,
      orderTypeName,
      rates: groupRates
    };
  }).sort((a, b) => a.orderTypeName.localeCompare(b.orderTypeName));
}

/**
 * Group rates by primary dimension with optional sub-grouping
 */
export function groupRatesByDimension<T extends RateType>(
  rates: T[],
  groupBy: 'partnerGroup' | 'siLevel' | 'installer',
  subGroupBy?: 'orderType'
): GroupedRateData<T>[] {
  // Group rates by primary dimension
  const grouped = rates.reduce((acc, rate) => {
    const key = getGroupKey(rate, groupBy);
    if (!acc[key]) {
      acc[key] = [];
    }
    acc[key].push(rate);
    return acc;
  }, {} as Record<string, T[]>);

  // Convert to array and optionally sub-group
  return Object.entries(grouped)
    .map(([key, groupRates]) => ({
      groupKey: key,
      groupLabel: getGroupLabel(key, groupBy, groupRates),
      rates: groupRates,
      subGroups: subGroupBy === 'orderType' ? createSubGroups(groupRates) : undefined
    }))
    .sort((a, b) => a.groupLabel.localeCompare(b.groupLabel));
}

