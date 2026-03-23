import React, { useState, useEffect } from 'react';
import { ChevronsDown, ChevronsUp } from 'lucide-react';
import { Button, EmptyState } from '../ui';
import { RateGroup } from './RateGroup';
import { groupRatesByDimension, type GroupedRateData } from '../../utils/rateGrouping';
import type { GponPartnerJobRate, GponSiJobRate, GponSiCustomRate } from '../../types/rates';

type RateType = GponPartnerJobRate | GponSiJobRate | GponSiCustomRate;

interface GroupedRateTableProps {
  rates: RateType[];
  onEdit: (rate: RateType) => void;
  onDelete: (rate: RateType) => void;
  searchQuery?: string;
  groupType: 'partner' | 'si' | 'custom';
  emptyMessage?: string;
  selectedRates?: Set<string>;
  onSelectRate?: (rate: RateType, selected: boolean) => void;
}

export const GroupedRateTable: React.FC<GroupedRateTableProps> = ({
  rates,
  onEdit,
  onDelete,
  searchQuery = '',
  groupType,
  emptyMessage = 'No rates found',
  selectedRates = new Set(),
  onSelectRate
}) => {
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set());
  const [allExpanded, setAllExpanded] = useState(false);
  
  // Determine grouping strategy
  const groupBy = groupType === 'partner' ? 'partnerGroup' : groupType === 'si' ? 'siLevel' : 'installer';
  
  // Group rates
  const groupedRates = groupRatesByDimension(rates, groupBy, 'orderType');
  
  // Expand all groups by default if there are few groups
  useEffect(() => {
    if (groupedRates.length <= 5 && expandedGroups.size === 0) {
      const allKeys = new Set(groupedRates.map(g => g.groupKey));
      setExpandedGroups(allKeys);
      setAllExpanded(true);
    }
  }, [groupedRates.length]);
  
  const toggleAllGroups = () => {
    if (allExpanded) {
      setExpandedGroups(new Set());
      setAllExpanded(false);
    } else {
      const allKeys = new Set(groupedRates.map(g => g.groupKey));
      setExpandedGroups(allKeys);
      setAllExpanded(true);
    }
  };
  
  if (groupedRates.length === 0) {
    return <EmptyState title={emptyMessage} />;
  }
  
  return (
    <div className="space-y-2">
      {/* Controls */}
      <div className="flex items-center justify-between mb-3">
        <div className="text-xs text-muted-foreground">
          {groupedRates.length} group{groupedRates.length !== 1 ? 's' : ''} • {rates.length} total rate{rates.length !== 1 ? 's' : ''}
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={toggleAllGroups}
          className="text-xs"
        >
          {allExpanded ? (
            <>
              <ChevronsUp className="h-3 w-3 mr-1" />
              Collapse All
            </>
          ) : (
            <>
              <ChevronsDown className="h-3 w-3 mr-1" />
              Expand All
            </>
          )}
        </Button>
      </div>
      
      {/* Groups */}
      <div>
        {groupedRates.map((group) => (
          <RateGroup
            key={group.groupKey}
            group={group}
            onEdit={onEdit}
            onDelete={onDelete}
            searchQuery={searchQuery}
            defaultExpanded={expandedGroups.has(group.groupKey)}
            groupType={groupType}
            selectedRates={selectedRates}
            onSelectRate={onSelectRate}
          />
        ))}
      </div>
    </div>
  );
};

