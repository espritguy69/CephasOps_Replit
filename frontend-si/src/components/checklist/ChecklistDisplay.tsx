import React, { useState, useEffect } from 'react';
import { CheckCircle, XCircle, ChevronDown, ChevronRight, AlertCircle } from 'lucide-react';
import { Card, Button, LoadingSpinner, EmptyState } from '../ui';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getOrderChecklist, submitChecklistAnswers } from '../../api/orders';
import { useAuth } from '../../contexts/AuthContext';
import type { ChecklistItem, ChecklistAnswer } from '../../types/api';

interface ChecklistDisplayProps {
  orderId: string;
  statusCode: string;
  readonly?: boolean;
}

export function ChecklistDisplay({ orderId, statusCode, readonly = false }: ChecklistDisplayProps) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [expandedItems, setExpandedItems] = useState<Set<string>>(new Set());
  const [answers, setAnswers] = useState<Record<string, { answer: boolean; remarks?: string }>>({});

  const { data: checklistData, isLoading, error } = useQuery({
    queryKey: ['checklistWithAnswers', orderId, statusCode],
    queryFn: () => getOrderChecklist(orderId, statusCode),
    enabled: !!user?.id && !!orderId && !!statusCode,
  });

  const submitMutation = useMutation({
    mutationFn: (answersToSubmit: ChecklistAnswer[]) => 
      submitChecklistAnswers(orderId, statusCode, answersToSubmit),
    onSuccess: () => {
      queryClient.invalidateQueries(['checklistWithAnswers', orderId, statusCode]);
    },
  });

  // Initialize answers from loaded data
  useEffect(() => {
    if (checklistData?.items) {
      const initialAnswers: Record<string, { answer: boolean; remarks?: string }> = {};
      const collectAnswers = (items: ChecklistItem[]) => {
        items.forEach((item) => {
          const existingAnswer = checklistData.answers?.find(a => a.checklistItemId === item.id);
          if (existingAnswer) {
            initialAnswers[item.id] = {
              answer: existingAnswer.answer,
              remarks: existingAnswer.remarks,
            };
          }
          if (item.subSteps) {
            collectAnswers(item.subSteps);
          }
        });
      };
      collectAnswers(checklistData.items);
      setAnswers(initialAnswers);
    }
  }, [checklistData]);

  const toggleExpanded = (itemId: string) => {
    setExpandedItems((prev) => {
      const newExpanded = new Set(prev);
      if (newExpanded.has(itemId)) {
        newExpanded.delete(itemId);
      } else {
        newExpanded.add(itemId);
      }
      return newExpanded;
    });
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
    const answersToSubmit: ChecklistAnswer[] = Object.entries(answers).map(([checklistItemId, data]) => ({
      checklistItemId,
      answer: data.answer,
      remarks: data.remarks,
    }));
    submitMutation.mutate(answersToSubmit);
  };

  const renderChecklistItem = (item: ChecklistItem, level = 0): React.ReactNode => {
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
        <div
          className={`flex items-start gap-3 p-3 hover:bg-muted/50 transition-colors ${
            level > 0 ? 'pl-8' : ''
          }`}
        >
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
          {readonly && (
            <div className="flex items-center gap-2 mt-0.5">
              {isAnswered ? (
                <CheckCircle className="h-5 w-5 text-green-600" title="Yes" />
              ) : isNo ? (
                <XCircle className="h-5 w-5 text-red-600" title="No" />
              ) : (
                <AlertCircle className="h-5 w-5 text-gray-400" title="Not Answered" />
              )}
            </div>
          )}

          <div className="flex-1 flex flex-col">
            <p className={`font-medium ${item.isRequired ? 'text-red-600' : ''}`}>
              {item.description || item.text} {item.isRequired && <span className="text-red-500">*</span>}
            </p>
            {!readonly && (
              <textarea
                placeholder="Add remarks (optional)"
                value={currentAnswer?.remarks || ''}
                onChange={(e) => handleRemarksChange(item.id, e.target.value)}
                className="mt-2 text-sm p-2 border rounded-md resize-none"
                rows={2}
              />
            )}
            {readonly && currentAnswer?.remarks && (
              <p className="text-sm text-muted-foreground mt-2 italic">Remarks: {currentAnswer.remarks}</p>
            )}
          </div>
        </div>

        {hasSubSteps && isExpanded && (
          <div className="pl-4 pr-2 py-2 bg-muted/50">
            {item.subSteps?.map((subItem) => renderChecklistItem(subItem, level + 1))}
          </div>
        )}
      </div>
    );
  };

  if (isLoading) {
    return <LoadingSpinner />;
  }

  if (error) {
    return <EmptyState title="Error" description="Failed to load checklist." />;
  }

  if (!checklistData?.items || checklistData.items.length === 0) {
    return <EmptyState title="No Checklist Items" description="No checklist items configured for this status." />;
  }

  return (
    <Card className="p-0">
      <div className="p-4 border-b border-border">
        <h3 className="font-semibold text-lg">Checklist</h3>
        <p className="text-sm text-muted-foreground">Complete the required steps for this job status.</p>
      </div>
      <div className="divide-y divide-border">
        {checklistData.items.map((item) => renderChecklistItem(item))}
      </div>
      {!readonly && (
        <div className="p-4 border-t border-border flex justify-end">
          <Button onClick={handleSubmit} disabled={submitMutation.isPending}>
            {submitMutation.isPending ? 'Saving...' : 'Save Checklist'}
          </Button>
        </div>
      )}
    </Card>
  );
}

