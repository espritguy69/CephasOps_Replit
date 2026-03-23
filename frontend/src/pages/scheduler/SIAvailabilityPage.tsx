import React, { useState, useEffect, ChangeEvent } from 'react';
import { Calendar, Clock } from 'lucide-react';
import { getSIAvailability, setSIAvailability } from '../../api/scheduler';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, EmptyState, useToast, Card, StatusBadge, DatePicker } from '../../components/ui';

interface AvailabilityItem {
  id?: string;
  siId: string;
  siName?: string;
  name?: string;
  isAvailable: boolean;
  startTime?: string;
  endTime?: string;
  notes?: string;
}

const SIAvailabilityPage: React.FC = () => {
  const { showError } = useToast();
  const [availability, setAvailability] = useState<AvailabilityItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedDate, setSelectedDate] = useState<string>(new Date().toISOString().split('T')[0]);

  useEffect(() => {
    loadAvailability();
  }, [selectedDate]);

  const loadAvailability = async (): Promise<void> => {
    try {
      setLoading(true);
      setError(null);
      const data = await getSIAvailability({
        startDate: selectedDate,
        endDate: selectedDate
      });
      setAvailability(Array.isArray(data) ? data : []);
    } catch (err: any) {
      const errorMessage = err.message || 'Failed to load SI availability';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Error loading availability:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && availability.length === 0) {
    return (
      <PageShell title="SI Availability" breadcrumbs={[{ label: 'Scheduler', path: '/scheduler' }, { label: 'SI Availability' }]}>
        <LoadingSpinner message="Loading availability..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="SI Availability"
      breadcrumbs={[{ label: 'Scheduler', path: '/scheduler' }, { label: 'SI Availability' }]}
      actions={
        <div className="flex items-center gap-2">
          <Calendar className="h-4 w-4 text-muted-foreground" />
          <input
            type="date"
            value={selectedDate}
            onChange={(e: ChangeEvent<HTMLInputElement>) => setSelectedDate(e.target.value)}
            className="px-3 py-2 border border-input rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
      }
    >
      <div className="flex-1 p-3 max-w-7xl mx-auto">
      {/* Error Banner */}
      {error && (
        <div className="mb-2 rounded border border-red-200 bg-red-50 p-2 text-xs text-red-800" role="alert">
          {error}
        </div>
      )}

      {/* Availability List */}
      {availability.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2">
          {availability.map((item) => (
            <Card key={item.id || item.siId}>
              <div className="flex justify-between items-start mb-1">
                <div>
                  <h3 className="font-semibold text-xs mb-0.5">{item.siName || item.name || `SI ${item.siId}`}</h3>
                  <p className="text-xs text-muted-foreground">ID: {item.siId}</p>
                </div>
                <StatusBadge
                  status={item.isAvailable ? 'Available' : 'Unavailable'}
                  variant={item.isAvailable ? 'success' : 'error'}
                />
              </div>
              {item.startTime && item.endTime && (
                <div className="flex items-center gap-2 text-sm mb-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <span>{item.startTime} - {item.endTime}</span>
                </div>
              )}
              {item.notes && (
                <div className="mt-3 pt-3 border-t">
                  <p className="text-sm text-muted-foreground">{item.notes}</p>
                </div>
              )}
            </Card>
          ))}
        </div>
      ) : (
        <EmptyState
          title="No availability data for selected date"
          description="Select a different date to view availability."
        />
      )}
      </div>
    </PageShell>
  );
};

export default SIAvailabilityPage;

