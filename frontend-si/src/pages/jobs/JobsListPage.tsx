import React from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { format } from 'date-fns';
import { Card, Skeleton, EmptyState, StatusBadge, getOrderStatusVariant, Button, useToast } from '../../components/ui';
import { PageHeader } from '../../components/layout/PageHeader';
import { getAssignedJobs } from '../../api/si-app';
import { useAuth } from '../../contexts/AuthContext';

export function JobsListPage() {
  const { user, serviceInstaller } = useAuth();
  const { showInfo } = useToast();
  const siId = ((user as any)?.siId || serviceInstaller?.id || (serviceInstaller as any)?.Id) as string | undefined;

  const { data: jobs, isLoading, error, refetch, isRefetching } = useQuery({
    queryKey: ['assignedJobs', siId],
    queryFn: () => getAssignedJobs(null, siId || '', {}),
    enabled: !!siId,
  });

  if (isLoading) {
    return (
      <>
        <PageHeader title="Assigned Jobs" />
        <div className="p-4 md:p-6 space-y-4">
          {[1, 2, 3].map((i) => (
            <Card key={i} className="p-4">
              <div className="flex justify-between items-start mb-2">
                <Skeleton className="h-6 w-40" />
                <Skeleton className="h-5 w-16 rounded-full" />
              </div>
              <Skeleton className="h-4 w-full mb-1" />
              <Skeleton className="h-4 w-3/4" />
            </Card>
          ))}
        </div>
      </>
    );
  }

  const handleRetry = () => {
    showInfo('Retrying…');
    refetch();
  };

  if (error) {
    return (
      <>
        <PageHeader title="Assigned Jobs" />
        <div className="p-4">
          <EmptyState
            title="Error loading jobs"
            description={(error as Error).message || 'Failed to fetch assigned jobs.'}
            action={
              <Button variant="outline" onClick={handleRetry} disabled={isRefetching}>
                {isRefetching ? 'Retrying…' : 'Retry'}
              </Button>
            }
          />
        </div>
      </>
    );
  }

  if (!jobs || jobs.length === 0) {
    return (
      <>
        <PageHeader title="Assigned Jobs" />
        <div className="p-4">
          <EmptyState title="No Jobs Assigned" description="You currently have no assigned jobs." />
        </div>
      </>
    );
  }

  return (
    <>
      <PageHeader title="Assigned Jobs" />
      <div className="p-4 md:p-6 space-y-4">
        {jobs.map((job: any) => (
          <Link to={`/jobs/${job.id}`} key={job.id}>
            <Card className="p-4">
              <div className="flex justify-between items-start mb-2">
                <h3 className="text-lg font-semibold">{job.customerName}</h3>
                <StatusBadge variant={getOrderStatusVariant(job.status)} size="sm">
                  {job.status}
                </StatusBadge>
              </div>
            <p className="text-muted-foreground text-sm mb-1">{job.addressLine1}, {job.city}</p>
            {job.appointmentDate && (
              <p className="text-muted-foreground text-sm">
                Appointment: {format(new Date(job.appointmentDate), 'MMM dd, yyyy')} {job.appointmentWindowFrom} - {job.appointmentWindowTo}
              </p>
            )}
          </Card>
        </Link>
      ))}
      </div>
    </>
  );
}

