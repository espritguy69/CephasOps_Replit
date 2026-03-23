import React, { useState, useEffect } from 'react';
import { Calendar, Clock, AlertCircle } from 'lucide-react';
import { Button, TextInput, Textarea, useToast, Modal } from '../ui';
import { requestReschedule } from '../../api/scheduler';
import type { RequestRescheduleRequest } from '../../api/scheduler';

interface RescheduleRequestModalProps {
  isOpen: boolean;
  onClose: () => void;
  slotId: string;
  currentDate: string;
  currentWindowFrom: string;
  currentWindowTo: string;
  onSuccess?: () => void;
}

export const RescheduleRequestModal: React.FC<RescheduleRequestModalProps> = ({
  isOpen,
  onClose,
  slotId,
  currentDate,
  currentWindowFrom,
  currentWindowTo,
  onSuccess
}) => {
  const { showSuccess, showError } = useToast();
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState<RequestRescheduleRequest>({
    newDate: '',
    newWindowFrom: '',
    newWindowTo: '',
    reason: '',
    notes: ''
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isOpen) {
      // Initialize form with current date/time
      const today = new Date().toISOString().split('T')[0];
      setFormData({
        newDate: today,
        newWindowFrom: currentWindowFrom || '09:00:00',
        newWindowTo: currentWindowTo || '10:00:00',
        reason: '',
        notes: ''
      });
      setErrors({});
    }
  }, [isOpen, currentDate, currentWindowFrom, currentWindowTo]);

  const handleChange = (field: keyof RequestRescheduleRequest, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    setErrors(prev => ({ ...prev, [field]: '' }));
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.newDate) {
      newErrors.newDate = 'New date is required';
    } else {
      const newDate = new Date(formData.newDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      if (newDate < today) {
        newErrors.newDate = 'New date cannot be in the past';
      }
    }

    if (!formData.newWindowFrom) {
      newErrors.newWindowFrom = 'Start time is required';
    }

    if (!formData.newWindowTo) {
      newErrors.newWindowTo = 'End time is required';
    } else if (formData.newWindowFrom && formData.newWindowTo) {
      // Basic time validation
      const from = formData.newWindowFrom.split(':').map(Number);
      const to = formData.newWindowTo.split(':').map(Number);
      const fromMinutes = from[0] * 60 + from[1];
      const toMinutes = to[0] * 60 + to[1];
      if (toMinutes <= fromMinutes) {
        newErrors.newWindowTo = 'End time must be after start time';
      }
    }

    if (!formData.reason.trim()) {
      newErrors.reason = 'Reason is required';
    } else if (formData.reason.trim().length < 10) {
      newErrors.reason = 'Reason must be at least 10 characters';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validateForm()) {
      return;
    }

    try {
      setLoading(true);
      await requestReschedule(slotId, formData);
      showSuccess('Reschedule request submitted successfully. Waiting for admin approval.');
      onSuccess?.();
      onClose();
    } catch (error: any) {
      console.error('Failed to request reschedule:', error);
      showError(error.message || 'Failed to submit reschedule request');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Request Reschedule" size="medium">
      <div className="space-y-4">
          {/* Current Appointment Info */}
          <div className="bg-muted/50 rounded-lg p-3 border border-border">
            <p className="text-sm font-medium mb-2">Current Appointment</p>
            <div className="space-y-1 text-sm text-muted-foreground">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                <span>{new Date(currentDate).toLocaleDateString()}</span>
              </div>
              <div className="flex items-center gap-2">
                <Clock className="h-4 w-4" />
                <span>
                  {currentWindowFrom.substring(0, 5)} - {currentWindowTo.substring(0, 5)}
                </span>
              </div>
            </div>
          </div>

          {/* New Date */}
          <div>
            <label className="block text-sm font-medium mb-1">
              New Date <span className="text-destructive">*</span>
            </label>
            <input
              type="date"
              value={formData.newDate}
              onChange={(e) => handleChange('newDate', e.target.value)}
              min={new Date().toISOString().split('T')[0]}
              className={`w-full px-3 py-2 rounded-md border ${
                errors.newDate ? 'border-destructive' : 'border-border'
              } bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary`}
              disabled={loading}
            />
            {errors.newDate && (
              <p className="text-xs text-destructive mt-1">{errors.newDate}</p>
            )}
          </div>

          {/* Time Range */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">
                Start Time <span className="text-destructive">*</span>
              </label>
              <input
                type="time"
                value={formData.newWindowFrom.substring(0, 5)}
                onChange={(e) => handleChange('newWindowFrom', `${e.target.value}:00`)}
                className={`w-full px-3 py-2 rounded-md border ${
                  errors.newWindowFrom ? 'border-destructive' : 'border-border'
                } bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary`}
                disabled={loading}
              />
              {errors.newWindowFrom && (
                <p className="text-xs text-destructive mt-1">{errors.newWindowFrom}</p>
              )}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">
                End Time <span className="text-destructive">*</span>
              </label>
              <input
                type="time"
                value={formData.newWindowTo.substring(0, 5)}
                onChange={(e) => handleChange('newWindowTo', `${e.target.value}:00`)}
                className={`w-full px-3 py-2 rounded-md border ${
                  errors.newWindowTo ? 'border-destructive' : 'border-border'
                } bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary`}
                disabled={loading}
              />
              {errors.newWindowTo && (
                <p className="text-xs text-destructive mt-1">{errors.newWindowTo}</p>
              )}
            </div>
          </div>

          {/* Reason */}
          <div>
            <label className="block text-sm font-medium mb-1">
              Reason <span className="text-destructive">*</span>
            </label>
            <Textarea
              value={formData.reason}
              onChange={(e) => handleChange('reason', e.target.value)}
              placeholder="Explain why you need to reschedule (e.g., customer requested, conflict with another appointment, etc.)"
              rows={3}
              className={errors.reason ? 'border-destructive' : ''}
              disabled={loading}
            />
            {errors.reason && (
              <p className="text-xs text-destructive mt-1">{errors.reason}</p>
            )}
            <p className="text-xs text-muted-foreground mt-1">
              Minimum 10 characters required
            </p>
          </div>

          {/* Notes (Optional) */}
          <div>
            <label className="block text-sm font-medium mb-1">Additional Notes (Optional)</label>
            <Textarea
              value={formData.notes || ''}
              onChange={(e) => handleChange('notes', e.target.value)}
              placeholder="Any additional information for the admin..."
              rows={2}
              disabled={loading}
            />
          </div>

          {/* Info Alert */}
          <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
            <div className="flex items-start gap-2">
              <AlertCircle className="h-4 w-4 text-blue-600 dark:text-blue-400 mt-0.5 shrink-0" />
              <div className="text-xs text-blue-800 dark:text-blue-200">
                <p className="font-medium mb-1">Reschedule Request Process</p>
                <ul className="list-disc list-inside space-y-0.5">
                  <li>Your request will be sent to the admin for approval</li>
                  <li>The order status will change to "Reschedule Pending Approval"</li>
                  <li>You'll be notified once the admin responds</li>
                  <li>The appointment will remain as scheduled until approved</li>
                </ul>
              </div>
            </div>
          </div>
        </div>

      <div className="flex items-center justify-end gap-2 pt-4 border-t border-border">
        <Button
          variant="outline"
          onClick={onClose}
          disabled={loading}
        >
          Cancel
        </Button>
        <Button
          onClick={handleSubmit}
          disabled={loading}
        >
          {loading ? 'Submitting...' : 'Submit Request'}
        </Button>
      </div>
    </Modal>
  );
};

