import React, { useState } from 'react';
import { CheckCircle, XCircle, ChevronDown, ChevronRight, AlertCircle } from 'lucide-react';
import { Card, Button, LoadingSpinner, EmptyState, useToast, Textarea } from '../ui';
import {
  useChecklistWithAnswers,
  useSubmitChecklistAnswers,
  type OrderStatusChecklistWithAnswers,
} from '../../hooks/useOrderStatusChecklists';

interface OrderStatusChecklistDisplayProps {
  orderId: string;
  statusCode: string;
  readonly?: boolean;
}

const OrderStatusChecklistDisplay: React.FC<OrderStatusChecklistDisplayProps> = ({
  orderId,
  statusCode,
  readonly = false,
}) => {
  const { showSuccess, showError } = useToast();
  const { data: checklistItems, isLoading } = useChecklistWithAnswers(orderId, statusCode);
  const submitMutation = useSubmitChecklistAnswers(orderId);

  const [expandedItems, setExpandedItems] = useState<Set<string>>(new Set());
  const [answers, setAnswers] = useState<Record<string, { answer: boolean; remarks?: string }>>({});
  const [saving, setSaving] = useState(false);

  // Initialize answers from loaded data
  React.useEffect(() => {
    if (checklistItems) {
      const initialAnswers: Record<string, { answer: boolean; remarks?: string }> = {};
      const collectAnswers = (items: OrderStatusChecklistWithAnswers[]) => {
        items.forEach((item) => {
          if (item.answer) {
            initialAnswers[item.id] = {
              answer: item.answer.answer,
              remarks: item.answer.remarks || undefined,
            };
          }
          if (item.subSteps) {
            collectAnswers(item.subSteps);
          }
        });
      };
      collectAnswers(checklistItems);
      setAnswers(initialAnswers);
    }
  }, [checklistItems]);

  const toggleExpanded = (itemId: string) => {
    const newExpanded = new Set(expandedItems);
    if (newExpanded.has(itemId)) {
      newExpanded.delete(itemId);
    } else {
      newExpanded.add(itemId);
    }
    setExpandedItems(newExpanded);
  };

  const handleAnswerChange = (itemId: string, answer: boolean) => {
    setAnswers((prev) => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        answer,
      },
    }));
  };

  const handleRemarksChange = (itemId: string, remarks: string) => {
    setAnswers((prev) => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        remarks,
      },
    }));
  };

  const handleSubmit = async () => {
    try {
      setSaving(true);
      const answersToSubmit = Object.entries(answers).map(([checklistItemId, data]) => ({
        checklistItemId,
        answer: data.answer,
        remarks: data.remarks,
      }));

      await submitMutation.mutateAsync({
        answers: answersToSubmit,
      });

      showSuccess('Checklist answers saved successfully');
    } catch (error) {
      showError('Failed to save checklist answers');
    } finally {
      setSaving(false);
    }
  };

  const renderChecklistItem = (item: OrderStatusChecklistWithAnswers, level: number = 0) => {
    const isMainStep = !item.parentChecklistItemId;
    const hasSubSteps = item.subSteps && item.subSteps.length > 0;
    const isExpanded = expandedItems.has(item.id);
    const currentAnswer = answers[item.id];
    const isAnswered = currentAnswer?.answer === true;
    const isNo = currentAnswer?.answer === false;

    return (
      <div
        key={item.id}
        className={`border-b border-border last:border-b-0 ${level > 0 ? 'bg-muted/30' : ''}`}
      >
        {/* Main Row */}
        <div
          className={`flex items-start gap-3 p-3 hover:bg-muted/50 transition-colors ${
            level > 0 ? 'pl-8' : ''
          }`}
        >
          {/* Expand/Collapse Icon */}
          {hasSubSteps && (
            <button
              onClick={() => toggleExpanded(item.id)}
              className="p-1 hover:bg-muted rounded transition-colors mt-0.5"
            >
              {isExpanded ? (
                <ChevronDown className="h-4 w-4" />
              ) : (
                <ChevronRight className="h-4 w-4" />
              )}
            </button>
          )}
          {!hasSubSteps && <div className="w-6" />}

          {/* Answer Control */}
          {!readonly && (
            <div className="flex items-center gap-2 mt-0.5">
              <button
                onClick={() => handleAnswerChange(item.id, true)}
                className={`p-1.5 rounded transition-colors ${
                  isAnswered
                    ? 'bg-green-100 text-green-700 hover:bg-green-200'
                    : 'bg-gray-100 text-gray-400 hover:bg-gray-200'
                }`}
                title="Yes"
              >
                <CheckCircle className="h-5 w-5" />
              </button>
              <button
                onClick={() => handleAnswerChange(item.id, false)}
                className={`p-1.5 rounded transition-colors ${
                  isNo
                    ? 'bg-red-100 text-red-700 hover:bg-red-200'
                    : 'bg-gray-100 text-gray-400 hover:bg-gray-200'
                }`}
                title="No"
              >
                <XCircle className="h-5 w-5" />
              </button>
            </div>
          )}

          {/* Read-only Answer Display */}
          {readonly && (
            <div className="mt-0.5">
              {item.answer ? (
                item.answer.answer ? (
                  <CheckCircle className="h-5 w-5 text-green-600" />
                ) : (
                  <XCircle className="h-5 w-5 text-red-600" />
                )
              ) : (
                <div className="h-5 w-5 rounded-full border-2 border-gray-300" />
              )}
            </div>
          )}

          {/* Item Details */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <span
                className={`font-medium ${isMainStep ? 'text-base' : 'text-sm text-muted-foreground'}`}
              >
                {item.name}
              </span>
              {item.isRequired && (
                <span className="px-1.5 py-0.5 text-xs font-medium bg-red-100 text-red-800 rounded">
                  Required
                </span>
              )}
              {!item.isActive && (
                <span className="px-1.5 py-0.5 text-xs bg-gray-100 text-gray-600 rounded">
                  Inactive
                </span>
              )}
            </div>
            {item.description && (
              <p className="text-xs text-muted-foreground mt-0.5">{item.description}</p>
            )}

            {/* Remarks Input */}
            {!readonly && (
              <div className="mt-2">
                <Textarea
                  placeholder="Add remarks (optional)"
                  value={currentAnswer?.remarks || ''}
                  onChange={(e) => handleRemarksChange(item.id, e.target.value)}
                  rows={2}
                  className="text-sm"
                />
              </div>
            )}

            {/* Read-only Remarks Display */}
            {readonly && item.answer?.remarks && (
              <div className="mt-2 p-2 bg-muted rounded text-sm text-muted-foreground">
                <strong>Remarks:</strong> {item.answer.remarks}
              </div>
            )}
          </div>
        </div>

        {/* Sub-Steps */}
        {hasSubSteps && isExpanded && (
          <div className="ml-6 border-l-2 border-border">
            {item.subSteps?.map((subStep) => renderChecklistItem(subStep, level + 1))}
          </div>
        )}
      </div>
    );
  };

  if (isLoading) {
    return (
      <Card className="p-4">
        <LoadingSpinner />
      </Card>
    );
  }

  if (!checklistItems || checklistItems.length === 0) {
    return (
      <Card className="p-4">
        <EmptyState
          title="No checklist defined"
          description="No process checklist has been configured for this status yet."
        />
      </Card>
    );
  }

  // Check if all required items are completed
  const checkCompletion = (items: OrderStatusChecklistWithAnswers[]): {
    allComplete: boolean;
    incompleteItems: string[];
  } => {
    let allComplete = true;
    const incompleteItems: string[] = [];

    const checkItem = (item: OrderStatusChecklistWithAnswers) => {
      if (!item.isRequired || !item.isActive) return;

      const hasSubSteps = item.subSteps && item.subSteps.length > 0;
      if (hasSubSteps) {
        // Check all required sub-steps
        item.subSteps?.forEach((subStep) => {
          if (subStep.isRequired && subStep.isActive) {
            const subAnswer = answers[subStep.id];
            if (!subAnswer || !subAnswer.answer) {
              allComplete = false;
              incompleteItems.push(subStep.name);
            }
          }
        });
        // Optionally check main step itself if required
        if (item.isRequired) {
          const mainAnswer = answers[item.id];
          if (!mainAnswer || !mainAnswer.answer) {
            allComplete = false;
            incompleteItems.push(item.name);
          }
        }
      } else {
        // Check main step
        const answer = answers[item.id];
        if (!answer || !answer.answer) {
          allComplete = false;
          incompleteItems.push(item.name);
        }
      }
    };

    items.forEach((item) => {
      checkItem(item);
      if (item.subSteps) {
        item.subSteps.forEach(checkItem);
      }
    });

    return { allComplete, incompleteItems };
  };

  const { allComplete, incompleteItems } = checkCompletion(checklistItems);

  return (
    <Card className="p-4">
      <div className="space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold">Process Checklist</h2>
            <p className="text-sm text-muted-foreground">
              Complete all required steps before proceeding to the next status
            </p>
          </div>
          {!readonly && (
            <Button onClick={handleSubmit} disabled={saving} size="sm">
              {saving ? 'Saving...' : 'Save Answers'}
            </Button>
          )}
        </div>

        {/* Completion Status */}
        {!allComplete && incompleteItems.length > 0 && (
          <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-lg flex items-start gap-2">
            <AlertCircle className="h-5 w-5 text-yellow-600 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <p className="text-sm font-medium text-yellow-800">
                Incomplete Required Steps:
              </p>
              <ul className="text-sm text-yellow-700 mt-1 list-disc list-inside">
                {incompleteItems.map((name, idx) => (
                  <li key={idx}>{name}</li>
                ))}
              </ul>
            </div>
          </div>
        )}

        {allComplete && (
          <div className="p-3 bg-green-50 border border-green-200 rounded-lg flex items-center gap-2">
            <CheckCircle className="h-5 w-5 text-green-600" />
            <p className="text-sm font-medium text-green-800">
              All required steps are completed
            </p>
          </div>
        )}

        {/* Checklist Items */}
        <div className="border border-border rounded-lg divide-y divide-border">
          {checklistItems.map((item) => renderChecklistItem(item))}
        </div>
      </div>
    </Card>
  );
};

export default OrderStatusChecklistDisplay;

