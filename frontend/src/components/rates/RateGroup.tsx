import React, { useState } from 'react';
import { ChevronDown, ChevronRight, TrendingUp, TrendingDown, Users } from 'lucide-react';
import { RateRow } from './RateRow';
import type { GponPartnerJobRate, GponSiJobRate, GponSiCustomRate } from '../../types/rates';
import type { GroupedRateData } from '../../utils/rateGrouping';
import { cn } from '@/lib/utils';

type RateType = GponPartnerJobRate | GponSiJobRate | GponSiCustomRate;

interface RateGroupProps {
  group: GroupedRateData<RateType>;
  onEdit: (rate: RateType) => void;
  onDelete: (rate: RateType) => void;
  searchQuery?: string;
  defaultExpanded?: boolean;
  groupType: 'partner' | 'si' | 'custom';
  selectedRates?: Set<string>;
  onSelectRate?: (rate: RateType, selected: boolean) => void;
}

export const RateGroup: React.FC<RateGroupProps> = ({
  group,
  onEdit,
  onDelete,
  searchQuery = '',
  defaultExpanded = false,
  groupType,
  selectedRates = new Set(),
  onSelectRate
}) => {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);
  const [expandedSubGroups, setExpandedSubGroups] = useState<Set<string>>(new Set());
  
  const activeCount = group.rates.filter(r => r.isActive).length;
  const inactiveCount = group.rates.length - activeCount;
  
  const toggleSubGroup = (orderType: string) => {
    setExpandedSubGroups(prev => {
      const next = new Set(prev);
      if (next.has(orderType)) {
        next.delete(orderType);
      } else {
        next.add(orderType);
      }
      return next;
    });
  };
  
  const getGroupIcon = () => {
    switch (groupType) {
      case 'partner':
        return <TrendingUp className="h-4 w-4 text-green-500" />;
      case 'si':
        return <TrendingDown className="h-4 w-4 text-blue-500" />;
      case 'custom':
        return <Users className="h-4 w-4 text-purple-500" />;
      default:
        return null;
    }
  };
  
  return (
    <div className="border border-border rounded-lg overflow-hidden bg-card mb-2">
      {/* Group Header */}
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full flex items-center justify-between px-4 py-3 bg-muted/50 hover:bg-muted transition-colors text-left"
      >
        <div className="flex items-center gap-3 flex-1">
          {isExpanded ? (
            <ChevronDown className="h-4 w-4 text-muted-foreground flex-shrink-0" />
          ) : (
            <ChevronRight className="h-4 w-4 text-muted-foreground flex-shrink-0" />
          )}
          {getGroupIcon()}
          <div className="flex-1 min-w-0">
            <div className="font-medium text-sm text-foreground truncate">
              {group.groupLabel}
            </div>
            <div className="text-xs text-muted-foreground mt-0.5">
              {group.rates.length} rate{group.rates.length !== 1 ? 's' : ''}
              {activeCount > 0 && (
                <span className="ml-2">
                  • <span className="text-green-600 dark:text-green-400">{activeCount} active</span>
                </span>
              )}
              {inactiveCount > 0 && (
                <span className="ml-2">
                  • <span className="text-muted-foreground">{inactiveCount} inactive</span>
                </span>
              )}
            </div>
          </div>
        </div>
      </button>
      
      {/* Group Content */}
      {isExpanded && (
        <div className="bg-background">
          {/* Column Headers */}
          <div className="flex items-center gap-3 px-4 py-2 bg-muted/30 border-b border-border text-xs font-medium text-muted-foreground">
            <div className="flex-1 min-w-[120px]">Order Type</div>
            <div className="flex-1 min-w-[100px]">Category</div>
            <div className="flex-1 min-w-[120px]">Method</div>
            <div className="flex-1 min-w-[100px]">Amount</div>
            <div className="flex-1 min-w-[120px] hidden lg:block">
              {groupType === 'partner' ? 'Partner' : groupType === 'si' ? 'Partner Group' : 'Reason'}
            </div>
            <div className="w-[80px]">Status</div>
            <div className="w-[80px]">Actions</div>
          </div>
          
          {/* Sub-groups or direct rates */}
          {group.subGroups && group.subGroups.length > 0 ? (
            <div>
              {group.subGroups.map((subGroup) => {
                const isSubGroupExpanded = expandedSubGroups.has(subGroup.orderType);
                return (
                  <div key={subGroup.orderType} className="border-b border-border/50 last:border-b-0">
                    {/* Sub-group Header */}
                    <button
                      onClick={() => toggleSubGroup(subGroup.orderType)}
                      className="w-full flex items-center gap-2 px-6 py-2 bg-muted/20 hover:bg-muted/30 transition-colors text-left"
                    >
                      {isSubGroupExpanded ? (
                        <ChevronDown className="h-3 w-3 text-muted-foreground" />
                      ) : (
                        <ChevronRight className="h-3 w-3 text-muted-foreground" />
                      )}
                      <span className="text-xs font-medium text-foreground">
                        {subGroup.orderTypeName} ({subGroup.rates.length})
                      </span>
                    </button>
                    
                    {/* Sub-group Rates */}
                    {isSubGroupExpanded && (
                      <div>
                        {subGroup.rates.map((rate) => (
                          <RateRow
                            key={rate.id}
                            rate={rate}
                            onEdit={onEdit}
                            onDelete={onDelete}
                            searchQuery={searchQuery}
                            selected={selectedRates.has(rate.id)}
                            onSelect={onSelectRate}
                          />
                        ))}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          ) : (
            <div>
              {group.rates.map((rate) => (
                <RateRow
                  key={rate.id}
                  rate={rate}
                  onEdit={onEdit}
                  onDelete={onDelete}
                  searchQuery={searchQuery}
                  selected={selectedRates.has(rate.id)}
                  onSelect={onSelectRate}
                />
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};

