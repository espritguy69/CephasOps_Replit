import React from 'react';
import { Edit, Trash2 } from 'lucide-react';
import { StatusBadge } from '../ui';
import { cn } from '@/lib/utils';
import type { GponPartnerJobRate, GponSiJobRate, GponSiCustomRate } from '../../types/rates';

type RateType = GponPartnerJobRate | GponSiJobRate | GponSiCustomRate;

interface RateRowProps {
  rate: RateType;
  onEdit: (rate: RateType) => void;
  onDelete: (rate: RateType) => void;
  searchQuery?: string;
  selected?: boolean;
  onSelect?: (rate: RateType, selected: boolean) => void;
}

/**
 * Highlight matching text in search results
 */
const highlightText = (text: string | null | undefined, query: string): React.ReactNode => {
  if (!query || !text) return text || '-';
  
  const lowerText = text.toLowerCase();
  const lowerQuery = query.toLowerCase();
  const index = lowerText.indexOf(lowerQuery);
  
  if (index === -1) return text;
  
  const before = text.substring(0, index);
  const match = text.substring(index, index + query.length);
  const after = text.substring(index + query.length);
  
  return (
    <>
      {before}
      <mark className="bg-yellow-200 dark:bg-yellow-800 px-0.5 rounded">{match}</mark>
      {after}
    </>
  );
};

export const RateRow: React.FC<RateRowProps> = ({ rate, onEdit, onDelete, searchQuery = '', selected = false, onSelect }) => {
  const isPartnerRate = 'revenueAmount' in rate;
  const isSiRate = 'siLevel' in rate && 'payoutAmount' in rate;
  const isCustomRate = 'serviceInstallerId' in rate && 'customPayoutAmount' in rate;
  
  return (
    <div className={cn(
      "flex items-center gap-3 px-4 py-2 hover:bg-muted/50 transition-colors border-b border-border/50 last:border-b-0",
      selected && "bg-primary/5"
    )}>
      {/* Selection Checkbox */}
      {onSelect && (
        <div className="w-4">
          <input
            type="checkbox"
            checked={selected}
            onChange={(e) => onSelect(rate, e.target.checked)}
            onClick={(e) => e.stopPropagation()}
            className="h-4 w-4 rounded border-input cursor-pointer"
          />
        </div>
      )}
      
      {/* Order Type */}
      <div className="flex-1 min-w-[120px]">
        <div className="text-xs font-medium text-foreground">
          {highlightText(rate.orderTypeName, searchQuery)}
        </div>
      </div>
      
      {/* Order Category */}
      <div className="flex-1 min-w-[100px]">
        <div className="text-xs text-muted-foreground">
          {highlightText(rate.orderCategoryName, searchQuery)}
        </div>
      </div>
      
      {/* Installation Method */}
      <div className="flex-1 min-w-[120px]">
        <div className="text-xs text-muted-foreground">
          {rate.installationMethodName ? (
            highlightText(rate.installationMethodName, searchQuery)
          ) : (
            <span className="text-muted-foreground text-xs">All</span>
          )}
        </div>
      </div>
      
      {/* Amount */}
      <div className="flex-1 min-w-[100px]">
        <div className="text-xs font-semibold text-foreground">
          {isPartnerRate && `RM ${((rate as GponPartnerJobRate).revenueAmount || 0).toFixed(2)}`}
          {isSiRate && `RM ${((rate as GponSiJobRate).payoutAmount || 0).toFixed(2)}`}
          {isCustomRate && `RM ${((rate as GponSiCustomRate).customPayoutAmount || 0).toFixed(2)}`}
        </div>
      </div>
      
      {/* Additional Info */}
      {isPartnerRate && (rate as GponPartnerJobRate).partnerName && (
        <div className="flex-1 min-w-[120px] hidden lg:block">
          <div className="text-xs text-muted-foreground">
            {highlightText((rate as GponPartnerJobRate).partnerName, searchQuery)}
          </div>
        </div>
      )}
      
      {isSiRate && (rate as GponSiJobRate).partnerGroupName && (
        <div className="flex-1 min-w-[120px] hidden lg:block">
          <div className="text-xs text-muted-foreground">
            {highlightText((rate as GponSiJobRate).partnerGroupName, searchQuery)}
          </div>
        </div>
      )}
      
      {isCustomRate && (rate as GponSiCustomRate).reason && (
        <div className="flex-1 min-w-[120px] hidden lg:block">
          <div className="text-xs text-muted-foreground">
            {(rate as GponSiCustomRate).reason}
          </div>
        </div>
      )}
      
      {/* Status */}
      <div className="w-[80px]">
        <StatusBadge variant={rate.isActive ? 'success' : 'default'}>
          {rate.isActive ? 'Active' : 'Inactive'}
        </StatusBadge>
      </div>
      
      {/* Actions */}
      <div className="flex items-center gap-2 w-[80px]">
        <button
          onClick={(e) => {
            e.stopPropagation();
            onEdit(rate);
          }}
          title="Edit"
          className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
        >
          <Edit className="h-4 w-4" />
        </button>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete(rate);
          }}
          title="Delete"
          className="p-1 rounded text-red-600 hover:text-red-700 hover:bg-muted transition-colors"
        >
          <Trash2 className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
};

