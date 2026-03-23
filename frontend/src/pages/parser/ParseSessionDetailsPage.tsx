import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft, Clock, CheckCircle, XCircle, AlertTriangle,
  FileText, Mail, RefreshCw, Eye, ExternalLink, User,
  Building2, Calendar, Package, TrendingUp, FilePen
} from 'lucide-react';
import {
  getParseSession,
  getParsedOrderDrafts,
  retryParseSession,
  type ParseSession,
  type ParsedOrderDraft
} from '../../api/parser';
import {
  Button,
  Card,
  LoadingSpinner,
  Badge,
  EmptyState,
  DataTable,
  useToast
} from '../../components/ui';
import { PageShell } from '../../components/layout';
import { formatLocalDateTime } from '../../utils/dateUtils';
import { cn } from '../../lib/utils';

const ParseSessionDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();
  const queryClient = useQueryClient();

  // Fetch session details
  const {
    data: session,
    isLoading: sessionLoading,
    error: sessionError
  } = useQuery<ParseSession>({
    queryKey: ['parseSession', id],
    queryFn: () => getParseSession(id!),
    enabled: !!id
  });

  // Fetch drafts for this session
  const {
    data: drafts = [],
    isLoading: draftsLoading
  } = useQuery<ParsedOrderDraft[]>({
    queryKey: ['parseSessionDrafts', id],
    queryFn: () => getParsedOrderDrafts(id!),
    enabled: !!id
  });

  // Retry parse session mutation
  const retryMutation = useMutation({
    mutationFn: (sessionId: string) => retryParseSession(sessionId),
    onSuccess: (newSession) => {
      showSuccess('Parse session retry initiated successfully');
      // Invalidate queries to refresh data
      queryClient.invalidateQueries({ queryKey: ['parseSession', id] });
      queryClient.invalidateQueries({ queryKey: ['parseSessionDrafts', id] });
      queryClient.invalidateQueries({ queryKey: ['parseSessions'] });
      // Navigate to the new session
      navigate(`/orders/parser/sessions/${newSession.id}`);
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to retry parse session');
    }
  });

  if (sessionLoading) {
    return (
      <PageShell title="Parse Session Details">
        <div className="flex items-center justify-center py-12">
          <LoadingSpinner size="lg" />
        </div>
      </PageShell>
    );
  }

  if (sessionError || !session) {
    return (
      <PageShell title="Parse Session Details">
        <EmptyState
          icon={AlertTriangle}
          title="Session Not Found"
          description="The parse session you're looking for doesn't exist or has been deleted."
          action={
            <Button onClick={() => navigate('/orders/parser')}>
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Parser
            </Button>
          }
        />
      </PageShell>
    );
  }

  const getStatusBadge = (status: string) => {
    const statusMap: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
      'Completed': { label: 'Completed', variant: 'default' },
      'Success': { label: 'Success', variant: 'default' },
      'Failed': { label: 'Failed', variant: 'destructive' },
      'Error': { label: 'Error', variant: 'destructive' },
      'Running': { label: 'Running', variant: 'secondary' },
      'Pending': { label: 'Pending', variant: 'outline' }
    };
    const config = statusMap[status] || { label: status, variant: 'outline' as const };
    return <Badge variant={config.variant}>{config.label}</Badge>;
  };

  const getValidationStatusBadge = (status: string) => {
    const statusMap: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
      'Pending': { label: 'Pending', variant: 'outline' },
      'Valid': { label: 'Valid', variant: 'default' },
      'NeedsReview': { label: 'Needs Review', variant: 'secondary' },
      'Rejected': { label: 'Rejected', variant: 'destructive' }
    };
    const config = statusMap[status] || { label: status, variant: 'outline' as const };
    return <Badge variant={config.variant} className="text-xs">{config.label}</Badge>;
  };

  // Table columns for drafts
  const draftColumns = [
    {
      key: 'serviceId',
      label: 'Service ID',
      width: '120px',
      render: (value: string) => (
        <span className="font-medium text-sm truncate block" title={value || 'N/A'}>
          {value || 'N/A'}
        </span>
      )
    },
    {
      key: 'customerName',
      label: 'Customer',
      width: '150px',
      render: (value: string) => (
        <div className="flex items-center gap-1 min-w-0">
          <User className="h-3 w-3 text-muted-foreground flex-shrink-0" />
          <span className="text-sm truncate block" title={value || 'N/A'}>
            {value || 'N/A'}
          </span>
        </div>
      )
    },
    {
      key: 'addressText',
      label: 'Address',
      width: '200px',
      render: (value: string) => (
        <div className="flex items-center gap-1 min-w-0">
          <Building2 className="h-3 w-3 text-muted-foreground flex-shrink-0" />
          <span className="text-sm truncate block" title={value || 'N/A'}>
            {value || 'N/A'}
          </span>
        </div>
      )
    },
    {
      key: 'validationStatus',
      label: 'Status',
      width: '120px',
      render: (value: string) => getValidationStatusBadge(value || 'Pending')
    },
    {
      key: 'confidenceScore',
      label: 'Confidence',
      width: '100px',
      render: (value: number) => (
        <div className="flex items-center gap-1">
          <TrendingUp className="h-3 w-3 text-muted-foreground" />
          <span className="text-sm font-medium">{Math.round(value)}%</span>
        </div>
      )
    },
    {
      key: 'actions',
      label: 'Actions',
      width: '100px',
      render: (_: any, row: ParsedOrderDraft) => (
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate(`/orders/create?draftId=${row.id}`)}
        >
          <Eye className="h-4 w-4 mr-1" />
          Review
        </Button>
      )
    }
  ];

  const duration = session.completedAt && session.createdAt
    ? Math.round((new Date(session.completedAt).getTime() - new Date(session.createdAt).getTime()) / 1000)
    : null;

  // Get first reviewable draft (prioritize NeedsReview, otherwise first draft)
  const getFirstReviewableDraft = (): ParsedOrderDraft | null => {
    // First, try to find a draft that needs review
    const needsReview = drafts.find(d => d.validationStatus === 'NeedsReview');
    if (needsReview) return needsReview;
    
    // Otherwise, return the first draft
    return drafts.length > 0 ? drafts[0] : null;
  };

  return (
    <PageShell
      title="Parse Session Details"
      description={`Session ID: ${session.id.substring(0, 8)}...`}
      actions={
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate('/orders/parser')}
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Parser
          </Button>
          {session.status === 'Failed' || session.status === 'Error' ? (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                if (id) {
                  retryMutation.mutate(id);
                }
              }}
              disabled={retryMutation.isPending}
            >
              <RefreshCw className={`h-4 w-4 mr-2 ${retryMutation.isPending ? 'animate-spin' : ''}`} />
              {retryMutation.isPending ? 'Retrying...' : 'Retry Parse'}
            </Button>
          ) : null}
          {getFirstReviewableDraft() && (
            <Button
              variant="default"
              size="sm"
              onClick={() => {
                const firstDraft = getFirstReviewableDraft();
                if (firstDraft) {
                  navigate(`/orders/create?draftId=${firstDraft.id}`);
                }
              }}
            >
              <FilePen className="h-4 w-4 mr-2" />
              Review Draft
            </Button>
          )}
          {session.emailMessageId ? (
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate(`/email?messageId=${session.emailMessageId}`)}
            >
              <Mail className="h-4 w-4 mr-2" />
              View Email
            </Button>
          ) : null}
        </div>
      }
    >
      <div className="space-y-6">
        {/* Session Information */}
        <Card className="p-6">
          <h3 className="text-lg font-semibold mb-4">Session Information</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div>
              <p className="text-sm font-medium text-muted-foreground mb-1">Status</p>
              <div className="flex items-center gap-2">
                {getStatusBadge(session.status)}
              </div>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground mb-1">Source Type</p>
              <div className="flex items-center gap-2">
                <FileText className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{session.sourceType || 'Unknown'}</span>
              </div>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground mb-1">Parsed Orders</p>
              <div className="flex items-center gap-2">
                <Package className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm font-semibold">{session.parsedOrdersCount || 0}</span>
              </div>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground mb-1">Created At</p>
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm">{formatLocalDateTime(session.createdAt)}</span>
              </div>
            </div>
            {session.completedAt && (
              <div>
                <p className="text-sm font-medium text-muted-foreground mb-1">Completed At</p>
                <div className="flex items-center gap-2">
                  <CheckCircle className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm">{formatLocalDateTime(session.completedAt)}</span>
                </div>
              </div>
            )}
            {duration !== null && (
              <div>
                <p className="text-sm font-medium text-muted-foreground mb-1">Duration</p>
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm">{duration}s</span>
                </div>
              </div>
            )}
          </div>

          {session.sourceDescription && (
            <div className="mt-4">
              <p className="text-sm font-medium text-muted-foreground mb-1">Source Description</p>
              <p className="text-sm bg-muted p-2 rounded">{session.sourceDescription}</p>
            </div>
          )}

          {session.errorMessage && (
            <div className="mt-4">
              <p className="text-sm font-medium text-red-600 mb-1">Error Message</p>
              <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-3 rounded">
                <p className="text-sm text-red-800 dark:text-red-200">{session.errorMessage}</p>
              </div>
            </div>
          )}
        </Card>

        {/* Parsed Order Drafts */}
        <Card className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold">Parsed Order Drafts</h3>
            <Badge variant="outline">{drafts.length} draft{drafts.length !== 1 ? 's' : ''}</Badge>
          </div>

          {draftsLoading ? (
            <div className="flex items-center justify-center py-8">
              <LoadingSpinner />
            </div>
          ) : drafts.length === 0 ? (
            <EmptyState
              icon={FileText}
              title="No Drafts Found"
              description="This parse session did not produce any order drafts."
            />
          ) : (
            <DataTable
              data={drafts}
              columns={draftColumns}
              keyField="id"
            />
          )}
        </Card>

        {/* Quick Actions */}
        {drafts.length > 0 && (
          <Card className="p-6">
            <h3 className="text-lg font-semibold mb-4">Quick Actions</h3>
            <div className="flex flex-wrap gap-2">
              <Button
                variant="outline"
                onClick={() => navigate('/orders/parser?validationStatus=NeedsReview')}
              >
                <Eye className="h-4 w-4 mr-2" />
                View All Needs Review
              </Button>
              <Button
                variant="outline"
                onClick={() => navigate('/orders/parser?validationStatus=Valid')}
              >
                <CheckCircle className="h-4 w-4 mr-2" />
                View All Valid
              </Button>
              <Button
                variant="outline"
                onClick={() => navigate('/orders/parser/dashboard')}
              >
                <TrendingUp className="h-4 w-4 mr-2" />
                View Dashboard
              </Button>
            </div>
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default ParseSessionDetailsPage;

