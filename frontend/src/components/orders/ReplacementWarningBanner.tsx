import React from 'react';
import { AlertTriangle, CheckCircle, X } from 'lucide-react';
import { Card, Button } from '../ui';
import { cn } from '@/lib/utils';

export interface IncompleteReplacement {
  id: string;
  oldMaterialName?: string;
  oldSerialNumber: string;
  newMaterialName?: string;
  newSerialNumber: string;
  replacementReason?: string;
  approvedBy?: string;
  approvalNotes?: string;
  approvedAt?: string;
  recordedAt: string;
  issues: string[];
}

interface ReplacementWarningBannerProps {
  incompleteReplacements: IncompleteReplacement[];
  onCompleteReplacement?: (replacementId: string) => void;
  onDismiss?: () => void;
  className?: string;
}

/**
 * ReplacementWarningBanner Component
 * 
 * Displays a warning banner when there are incomplete material replacements on an order.
 * A replacement is considered incomplete if:
 * - Missing TIME approval (ApprovedBy or ApprovalNotes)
 * - Missing new serial number
 * - Missing old serial number
 */
export function ReplacementWarningBanner({
  incompleteReplacements,
  onCompleteReplacement,
  onDismiss,
  className
}: ReplacementWarningBannerProps) {
  if (incompleteReplacements.length === 0) {
    return null;
  }

  const getIssueIcon = (issue: string) => {
    if (issue.includes('approval')) {
      return <AlertTriangle className="h-4 w-4 text-yellow-500" />;
    }
    if (issue.includes('serial')) {
      return <AlertTriangle className="h-4 w-4 text-orange-500" />;
    }
    return <AlertTriangle className="h-4 w-4 text-red-500" />;
  };

  const getIssueColor = (issue: string) => {
    if (issue.includes('approval')) {
      return 'text-yellow-700 bg-yellow-50 border-yellow-200';
    }
    if (issue.includes('serial')) {
      return 'text-orange-700 bg-orange-50 border-orange-200';
    }
    return 'text-red-700 bg-red-50 border-red-200';
  };

  return (
    <Card className={cn("p-4 border-2 border-yellow-500 bg-yellow-50/50", className)}>
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-2">
          <AlertTriangle className="h-5 w-5 text-yellow-600" />
          <h3 className="font-semibold text-yellow-900">
            Incomplete Material Replacements ({incompleteReplacements.length})
          </h3>
        </div>
        {onDismiss && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onDismiss}
            className="h-6 w-6 p-0 text-yellow-700 hover:text-yellow-900 hover:bg-yellow-100"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      <p className="text-sm text-yellow-800 mb-4">
        The following material replacements require attention before the order can proceed to Invoice/Docket Verified status.
      </p>

      <div className="space-y-3">
        {incompleteReplacements.map((replacement) => (
          <div
            key={replacement.id}
            className={cn(
              "p-3 rounded-md border",
              getIssueColor(replacement.issues[0] || '')
            )}
          >
            <div className="flex items-start justify-between mb-2">
              <div className="flex-1">
                <div className="flex items-center gap-2 mb-1">
                  {getIssueIcon(replacement.issues[0] || '')}
                  <span className="font-medium text-sm">
                    Replacement #{replacement.id.slice(0, 8)}
                  </span>
                </div>
                <div className="text-xs space-y-1 ml-6">
                  {replacement.oldMaterialName && (
                    <p>
                      <span className="font-medium">Old:</span> {replacement.oldMaterialName}
                      {replacement.oldSerialNumber && ` (SN: ${replacement.oldSerialNumber})`}
                    </p>
                  )}
                  {replacement.newMaterialName && (
                    <p>
                      <span className="font-medium">New:</span> {replacement.newMaterialName}
                      {replacement.newSerialNumber && ` (SN: ${replacement.newSerialNumber})`}
                    </p>
                  )}
                  {replacement.replacementReason && (
                    <p className="text-muted-foreground">
                      Reason: {replacement.replacementReason}
                    </p>
                  )}
                  <p className="text-muted-foreground">
                    Recorded: {new Date(replacement.recordedAt).toLocaleString()}
                  </p>
                </div>
              </div>
              {onCompleteReplacement && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => onCompleteReplacement(replacement.id)}
                  className="ml-2 h-8 text-xs"
                >
                  Complete
                </Button>
              )}
            </div>

            {/* Issues List */}
            <div className="ml-6 mt-2 space-y-1">
              {replacement.issues.map((issue, index) => (
                <div key={index} className="flex items-center gap-2 text-xs">
                  {getIssueIcon(issue)}
                  <span className="font-medium">{issue}</span>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      {incompleteReplacements.length > 0 && (
        <div className="mt-4 pt-3 border-t border-yellow-200">
          <p className="text-xs text-yellow-700">
            <strong>Note:</strong> All replacements must have TIME approval (ApprovedBy and ApprovalNotes) 
            before the order can proceed to Invoice/Docket Verified status.
          </p>
        </div>
      )}
    </Card>
  );
}

/**
 * Helper function to check if a replacement is incomplete
 */
export function isReplacementIncomplete(replacement: any): boolean {
  // Check for missing TIME approval
  const missingApproval = !replacement.approvedBy || !replacement.approvalNotes;
  
  // Check for missing serial numbers
  const missingNewSerial = !replacement.newSerialNumber || replacement.newSerialNumber.trim() === '';
  const missingOldSerial = !replacement.oldSerialNumber || replacement.oldSerialNumber.trim() === '';

  return missingApproval || missingNewSerial || missingOldSerial;
}

/**
 * Helper function to get list of issues for a replacement
 */
export function getReplacementIssues(replacement: any): string[] {
  const issues: string[] = [];

  if (!replacement.approvedBy) {
    issues.push('Missing TIME approval (ApprovedBy)');
  }
  if (!replacement.approvalNotes) {
    issues.push('Missing approval notes (ApprovalNotes)');
  }
  if (!replacement.newSerialNumber || replacement.newSerialNumber.trim() === '') {
    issues.push('Missing new serial number');
  }
  if (!replacement.oldSerialNumber || replacement.oldSerialNumber.trim() === '') {
    issues.push('Missing old serial number');
  }

  return issues;
}

