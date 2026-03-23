import React, { useState } from 'react';
import { AlertTriangle, Loader2 } from 'lucide-react';
import { Button, TextInput, useToast, Modal } from '../ui';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { markDeviceAsFaulty, type MarkFaultyRequest } from '../../api/si-app';

interface MarkFaultyModalProps {
  orderId: string;
  serialNumber: string;
  materialName?: string;
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

export function MarkFaultyModal({
  orderId,
  serialNumber,
  materialName,
  isOpen,
  onClose,
  onSuccess,
}: MarkFaultyModalProps) {
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();

  const [reason, setReason] = useState('');
  const [notes, setNotes] = useState('');

  const markFaultyMutation = useMutation({
    mutationFn: async (data: MarkFaultyRequest) => {
      return markDeviceAsFaulty(orderId, serialNumber, data);
    },
    onSuccess: (data) => {
      showSuccess(data.message || 'Device marked as faulty successfully!');
      setReason('');
      setNotes('');
      queryClient.invalidateQueries({ queryKey: ['jobDetails', orderId] });
      queryClient.invalidateQueries({ queryKey: ['materialUsage', orderId] });
      queryClient.invalidateQueries({ queryKey: ['orderMaterials', orderId] });
      onSuccess?.();
      onClose();
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to mark device as faulty.');
    },
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!reason.trim()) {
      showError('Please provide a reason for marking this device as faulty.');
      return;
    }

    markFaultyMutation.mutate({
      serialNumber,
      reason: reason.trim(),
      notes: notes.trim() || undefined,
    });
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Mark Device as Faulty" size="small">
      <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Serial Number</label>
            <div className="px-3 py-2 bg-muted rounded-md text-sm font-mono">
              {serialNumber}
            </div>
            {materialName && (
              <p className="text-xs text-muted-foreground mt-1">Material: {materialName}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">
              Reason <span className="text-red-500">*</span>
            </label>
            <TextInput
              placeholder="e.g., Device not powering on, No signal, Customer reported issue"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              disabled={markFaultyMutation.isPending}
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Notes (Optional)</label>
            <TextInput
              placeholder="Additional details..."
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={markFaultyMutation.isPending}
            />
          </div>

          <div className="flex gap-2 pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              disabled={markFaultyMutation.isPending}
              className="flex-1"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant="destructive"
              disabled={markFaultyMutation.isPending || !reason.trim()}
              className="flex-1"
            >
              {markFaultyMutation.isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin mr-2" />
                  Marking...
                </>
              ) : (
                'Mark as Faulty'
              )}
            </Button>
          </div>
        </form>
    </Modal>
  );
}

