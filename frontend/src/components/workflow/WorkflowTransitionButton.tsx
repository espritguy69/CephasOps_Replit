import React, { useState, useEffect } from 'react';
import { ArrowRight, Lock, AlertCircle, Loader2, X } from 'lucide-react';
import { getAllowedTransitions, executeTransition } from '../../api/workflow';
import { Button, Modal, TextInput, LoadingSpinner, useToast } from '../ui';
import { cn } from '../../lib/utils';
import type { AllowedTransition, ExecuteTransitionRequest } from '../../types/workflow';

interface ExtendedTransition extends AllowedTransition {
  id?: string;
  guardConditions?: Record<string, any>;
}

interface WorkflowTransitionButtonProps {
  entityType: string;
  entityId: string;
  currentStatus: string;
  onTransitionExecuted?: () => void;
}

const WorkflowTransitionButton: React.FC<WorkflowTransitionButtonProps> = ({ 
  entityType, 
  entityId, 
  currentStatus, 
  onTransitionExecuted 
}) => {
  const { showSuccess, showError } = useToast();
  const [allowedTransitions, setAllowedTransitions] = useState<ExtendedTransition[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState<boolean>(false);
  const [selectedTransition, setSelectedTransition] = useState<ExtendedTransition | null>(null);
  const [transitionReason, setTransitionReason] = useState<string>('');
  const [executing, setExecuting] = useState<boolean>(false);

  useEffect(() => {
    if (entityType && entityId && currentStatus) {
      loadAllowedTransitions();
    }
  }, [entityType, entityId, currentStatus]);

  const loadAllowedTransitions = async (): Promise<void> => {
    try {
      setLoading(true);
      setError(null);
      const transitions = await getAllowedTransitions({
        entityType,
        entityId,
        currentStatus
      });
      setAllowedTransitions((transitions || []) as ExtendedTransition[]);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load allowed transitions';
      setError(errorMessage);
      console.error('Error loading transitions:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleExecuteTransition = async (): Promise<void> => {
    if (!selectedTransition) return;

    try {
      setExecuting(true);
      setError(null);

      const payload: ExecuteTransitionRequest = {
        entityId,
        entityType,
        targetStatus: selectedTransition.toStatus,
        payload: {
          reason: transitionReason || undefined,
          source: 'AdminPortal',
          userId: undefined // Will be set by backend from JWT
        }
      };

      await executeTransition(payload);

      setShowModal(false);
      setSelectedTransition(null);
      setTransitionReason('');
      
      // Reload transitions after successful execution
      await loadAllowedTransitions();
      showSuccess('Status transition executed successfully');
      
      if (onTransitionExecuted) {
        onTransitionExecuted();
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to execute transition';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Error executing transition:', err);
    } finally {
      setExecuting(false);
    }
  };

  if (allowedTransitions.length === 0 && !loading && !error) {
    return null; // No transitions available
  }

  return (
    <div className="flex flex-wrap gap-2">
      {error && (
        <div className="w-full mb-4 rounded-lg border border-red-200 bg-red-50 p-4 text-red-800 flex items-center gap-2" role="alert">
          <AlertCircle className="h-5 w-5" />
          {error}
          <button className="ml-auto hover:opacity-70" onClick={() => setError(null)} aria-label="Close">
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      {loading ? (
        <LoadingSpinner size="sm" message="Loading transitions..." />
      ) : (
        allowedTransitions.map((transition, index) => (
          <Button
            key={transition.id || `transition-${index}`}
            variant="outline"
            size="sm"
            onClick={() => {
              setSelectedTransition(transition);
              setShowModal(true);
              setTransitionReason('');
            }}
            title={transition.guardConditions ? 'This transition has guard conditions that must be met' : ''}
          >
            <span className="flex items-center gap-1">
              {transition.fromStatus || 'Start'} 
              <ArrowRight className="h-3 w-3" />
              {transition.toStatus}
            </span>
            {transition.guardConditions && (
              <Lock className="h-3 w-3 ml-1" />
            )}
          </Button>
        ))
      )}

      <Modal
        isOpen={showModal}
        onClose={() => !executing && setShowModal(false)}
        title="Execute Status Transition"
        size="small"
        closeOnOverlayClick={!executing}
        closeOnEscape={!executing}
      >
        {selectedTransition && (
          <div className="space-y-6">
            <div className="space-y-2">
              <div className="flex gap-4 text-sm">
                <span className="text-muted-foreground font-medium">From:</span>
                <span>{selectedTransition.fromStatus || 'Initial'}</span>
              </div>
              <div className="flex gap-4 text-sm">
                <span className="text-muted-foreground font-medium">To:</span>
                <span>{selectedTransition.toStatus}</span>
              </div>
            </div>
            
            {selectedTransition.guardConditions && (
              <div className="p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                <p className="text-sm font-medium text-yellow-800 mb-2">Guard Conditions:</p>
                <ul className="text-sm text-yellow-700 list-disc list-inside">
                  {Object.keys(selectedTransition.guardConditions).map((key) => (
                    <li key={key}>{key}</li>
                  ))}
                </ul>
              </div>
            )}
            
            <TextInput
              label="Reason / Notes (Optional)"
              value={transitionReason}
              onChange={(e) => setTransitionReason(e.target.value)}
              placeholder="Enter reason for this status change..."
              disabled={executing}
              as="textarea"
              rows="3"
            />
            
            <div className="flex gap-4 justify-end pt-4 border-t">
              <Button
                variant="outline"
                onClick={() => !executing && setShowModal(false)}
                disabled={executing}
              >
                Cancel
              </Button>
              <Button
                onClick={handleExecuteTransition}
                disabled={executing}
              >
                {executing ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Executing...
                  </>
                ) : (
                  'Execute Transition'
                )}
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default WorkflowTransitionButton;

