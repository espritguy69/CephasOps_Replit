import React, { useState, useEffect, useMemo, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Search, Filter, RefreshCw, Calendar, Building2, User,
  FileText, FilePen, Eye, CheckCircle, XCircle, CheckSquare, Square,
  TrendingUp, AlertTriangle
} from 'lucide-react';
import { 
  getParsedOrderDraftsWithFilters, 
  getParsedOrderDraft,
  approveParsedOrderDraft,
  approveParsedOrderDraftsBulk,
  rejectParsedOrderDraft,
  updateParsedOrderDraft,
  checkOrderExistsByServiceId,
  createParsedMaterialAlias,
  type ParsedOrderDraft, 
  type ParsedOrderDraftFilters,
  type PagedParsedOrderDrafts,
  type UpdateParsedOrderDraftRequest
} from '../../api/parser';
import { getMaterials } from '../../api/inventory';
import { LoadingSpinner, Button, Card, Badge, Input, Select, Label, DataTable, useToast, Modal } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '../../lib/utils';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { formatLocalDateTime } from '../../utils/dateUtils';
import { getOrderCategories } from '../../api/orderCategories';
import type { ReferenceDataItem } from '../../types/referenceData';
import { MatchMaterialModal } from '../../components/parser/MatchMaterialModal';

/**
 * Parser Listing Page - Comprehensive view of all parsed order drafts
 * Features:
 * - Advanced filtering (status, source type, date range, search)
 * - Pagination
 * - Sorting
 * - Review action: Navigate to Create Order page for review/edit/approve/reject
 */
const ParserListingPage: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  // Filter state
  const [filters, setFilters] = useState<ParsedOrderDraftFilters>({
    page: 1,
    pageSize: 50,
    sortBy: 'createdAt',
    sortOrder: 'desc'
  });

  // UI state
  const [showFilters, setShowFilters] = useState(false);
  const [selectedDrafts, setSelectedDrafts] = useState<Set<string>>(new Set());
  const [bulkAction, setBulkAction] = useState<'approve' | 'reject' | null>(null);
  const [bulkActionNotes, setBulkActionNotes] = useState('');
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [editingDraft, setEditingDraft] = useState<ParsedOrderDraft | null>(null);
  const [editForm, setEditForm] = useState<UpdateParsedOrderDraftRequest>({});
  const [duplicateReviewDraft, setDuplicateReviewDraft] = useState<ParsedOrderDraft | null>(null);
  const [duplicateCheckOrderId, setDuplicateCheckOrderId] = useState<string | null>(null);
  const [duplicateCheckTicketId, setDuplicateCheckTicketId] = useState<string | null>(null);
  /** Match Material modal: which unmatched name is being mapped */
  const [matchMaterialName, setMatchMaterialName] = useState<string | null>(null);
  const [matchMaterialSelectedId, setMatchMaterialSelectedId] = useState<string>('');
  const [matchMaterialSaving, setMatchMaterialSaving] = useState(false);

  /** Tracks which draft id we are opening; used to ignore stale get-draft-by-id responses (race hardening). */
  const openingDraftIdRef = useRef<string | null>(null);

  /** Order categories for parser edit dropdown (parser-only lookup). */
  const [orderCategories, setOrderCategories] = useState<ReferenceDataItem[]>([]);

  // Add a refresh key to force cache invalidation
  const [refreshKey, setRefreshKey] = useState(0);

  // Fetch drafts with filters
  const { 
    data: pagedResult, 
    isLoading, 
    isError, 
    error,
    refetch 
  } = useQuery<PagedParsedOrderDrafts>({
    queryKey: ['parsedOrderDrafts', filters, refreshKey],
    queryFn: () => getParsedOrderDraftsWithFilters(filters),
    keepPreviousData: false, // Don't keep old data, show loading state instead
    refetchOnWindowFocus: true, // Refetch when user returns to tab so new drafts appear
    staleTime: 0, // Data is immediately stale - always refetch when requested
    refetchOnMount: true, // Always refetch when component mounts
    gcTime: 0, // Don't cache data - always fetch fresh
    refetchInterval: 60_000, // Poll every 60s while tab is visible so new parser drafts show without leaving the page
    refetchIntervalInBackground: false // Only poll when tab is visible
  });

  const drafts = pagedResult?.items || [];
  const totalCount = pagedResult?.totalCount || 0;
  const totalPages = pagedResult?.totalPages || 0;

  // Load order categories for parser edit dropdown (parser-only; narrowest lookup)
  useEffect(() => {
    getOrderCategories({ isActive: true })
      .then((data) => setOrderCategories(Array.isArray(data) ? data : []))
      .catch(() => setOrderCategories([]));
  }, []);

  // Parser edit: include saved category in options when it's inactive/missing from active lookup
  const orderCategoryOptions = useMemo(() => {
    const base = orderCategories.map((c) => ({ value: c.id, label: c.name + (c.code ? ` (${c.code})` : '') }));
    const currentId = editForm.orderCategoryId ?? editingDraft?.orderCategoryId;
    if (currentId && !orderCategories.some((c) => c.id === currentId)) {
      return [{ value: currentId, label: '(Saved category)' }, ...base];
    }
    return base;
  }, [orderCategories, editForm.orderCategoryId, editingDraft?.orderCategoryId]);

  // Auto-refresh when page becomes visible (user returns from another page)
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        // Refetch when page becomes visible
        refetch();
      }
    };

    const handleFocus = () => {
      // Also refetch when window regains focus
      refetch();
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    window.addEventListener('focus', handleFocus);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      window.removeEventListener('focus', handleFocus);
    };
  }, [refetch]);

  // Bulk action mutations (single-draft approve still used for row actions)
  const approveMutation = useMutation({
    mutationFn: (draftId: string) => approveParsedOrderDraft(draftId),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: ['parsedOrderDrafts'], exact: false });
      queryClient.invalidateQueries({ queryKey: ['parsedOrderDrafts'], exact: false });
      queryClient.invalidateQueries({ queryKey: ['parserStatistics'] });
      setRefreshKey(prev => prev + 1);
      refetch();
    }
  });

  const bulkApproveMutation = useMutation({
    mutationFn: (draftIds: string[]) => approveParsedOrderDraftsBulk(draftIds),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: ['parsedOrderDrafts'], exact: false });
      queryClient.invalidateQueries({ queryKey: ['parsedOrderDrafts'], exact: false });
      queryClient.invalidateQueries({ queryKey: ['parserStatistics'] });
      setRefreshKey(prev => prev + 1);
      refetch();
    }
  });

  const updateDraftMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateParsedOrderDraftRequest }) =>
      updateParsedOrderDraft(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['parsedOrderDrafts'], exact: false });
      queryClient.invalidateQueries({ queryKey: ['parserStatistics'] });
      setRefreshKey(prev => prev + 1);
      setEditingDraft(null);
      setEditForm({});
      refetch();
    }
  });

  const rejectMutation = useMutation({
    mutationFn: ({ draftId, notes }: { draftId: string; notes: string }) => 
      rejectParsedOrderDraft(draftId, { validationNotes: notes }),
    onSuccess: () => {
      // Clear cache and force refresh
      queryClient.removeQueries({ queryKey: ['parsedOrderDrafts'], exact: false });
      queryClient.invalidateQueries({ 
        queryKey: ['parsedOrderDrafts'],
        exact: false
      });
      queryClient.invalidateQueries({ queryKey: ['parserStatistics'] });
      // Update refresh key to force new query
      setRefreshKey(prev => prev + 1);
      // Also explicitly refetch to ensure immediate update
      refetch();
    }
  });

  // Selection handlers
  const toggleDraftSelection = (draftId: string) => {
    setSelectedDrafts(prev => {
      const newSet = new Set(prev);
      if (newSet.has(draftId)) {
        newSet.delete(draftId);
      } else {
        newSet.add(draftId);
      }
      return newSet;
    });
  };

  const toggleSelectAll = () => {
    if (selectedDrafts.size === drafts.length) {
      setSelectedDrafts(new Set());
    } else {
      setSelectedDrafts(new Set(drafts.map(d => d.id)));
    }
  };

  // Bulk action handlers (single API call for bulk approve)
  const handleBulkApprove = async () => {
    if (selectedDrafts.size === 0) {
      showError('No drafts selected');
      return;
    }

    const selected = drafts.filter(d => selectedDrafts.has(d.id)).filter(d => d.serviceId?.trim());
    if (selected.length > 0) {
      try {
        const checks = await Promise.all(
          selected.map(d => checkOrderExistsByServiceId(d.serviceId!.trim()))
        );
        const duplicateCount = checks.filter(c => c.exists).length;
        if (duplicateCount > 0 && !window.confirm(
          `${duplicateCount} of the selected draft(s) have Service IDs that already exist as orders. Proceeding may update those orders. Continue?`
        )) {
          return;
        }
      } catch {
        // On error, proceed with bulk approve
      }
    }

    try {
      const result = await bulkApproveMutation.mutateAsync(Array.from(selectedDrafts));
      const { succeededCount, failedCount, errors } = result;
      if (failedCount === 0) {
        showSuccess(`Successfully approved ${succeededCount} draft(s)`);
      } else if (succeededCount > 0) {
        showSuccess(`${succeededCount} approved, ${failedCount} failed. ${errors?.[0]?.message ?? ''}`);
      } else {
        showError(`All failed. ${errors?.[0]?.message ?? 'Could not approve drafts.'}`);
      }
      setSelectedDrafts(new Set());
      setBulkAction(null);
      setBulkActionNotes('');
    } catch (error: any) {
      showError(`Failed to approve drafts: ${error.message}`);
    }
  };

  const handleBulkReject = async () => {
    if (selectedDrafts.size === 0) {
      showError('No drafts selected');
      return;
    }

    if (!bulkActionNotes.trim()) {
      showError('Please provide rejection notes');
      return;
    }

    try {
      const promises = Array.from(selectedDrafts).map(draftId => 
        rejectMutation.mutateAsync({ draftId, notes: bulkActionNotes })
      );
      await Promise.all(promises);
      showSuccess(`Successfully rejected ${selectedDrafts.size} draft(s)`);
      setSelectedDrafts(new Set());
      setBulkAction(null);
      setBulkActionNotes('');
    } catch (error: any) {
      showError(`Failed to reject drafts: ${error.message}`);
    }
  };

  // Handler: Navigate to Create Order page for review/edit (with duplicate warning if ServiceId exists)
  const handleReview = async (draft: ParsedOrderDraft) => {
    if (draft.serviceId?.trim()) {
      try {
        const check = await checkOrderExistsByServiceId(draft.serviceId.trim());
        if (check.exists) {
          setDuplicateReviewDraft(draft);
          setDuplicateCheckOrderId(check.orderId ?? null);
          setDuplicateCheckTicketId(check.ticketId ?? null);
          return;
        }
      } catch {
        // On error, proceed to review anyway
      }
    }
    navigate(`/orders/create?draftId=${draft.id}`);
  };

  const handleConfirmReviewWithDuplicate = () => {
    if (duplicateReviewDraft) {
      navigate(`/orders/create?draftId=${duplicateReviewDraft.id}`);
      setDuplicateReviewDraft(null);
      setDuplicateCheckOrderId(null);
      setDuplicateCheckTicketId(null);
    }
  };

  const handleOpenEdit = async (draft: ParsedOrderDraft) => {
    const draftId = draft.id;
    openingDraftIdRef.current = draftId;
    try {
      const enriched = await getParsedOrderDraft(draftId);
      if (openingDraftIdRef.current !== draftId) return;
      setEditingDraft(enriched);
      setEditForm({
        serviceId: enriched.serviceId ?? '',
        ticketId: enriched.ticketId ?? '',
        awoNumber: enriched.awoNumber ?? '',
        customerName: enriched.customerName ?? '',
        customerPhone: enriched.customerPhone ?? '',
        customerEmail: enriched.customerEmail ?? '',
        addressText: enriched.addressText ?? '',
        oldAddress: enriched.oldAddress ?? '',
        appointmentDate: enriched.appointmentDate ?? '',
        appointmentWindow: enriched.appointmentWindow ?? '',
        orderTypeCode: enriched.orderTypeCode ?? '',
        orderCategoryId: enriched.orderCategoryId ?? undefined,
        packageName: enriched.packageName ?? '',
        bandwidth: enriched.bandwidth ?? '',
        onuSerialNumber: enriched.onuSerialNumber ?? '',
        onuPassword: enriched.onuPassword ?? '',
        voipServiceId: enriched.voipServiceId ?? '',
        remarks: enriched.remarks ?? '',
        buildingId: enriched.buildingId ?? undefined
      });
    } catch {
      if (openingDraftIdRef.current !== draftId) return;
      setEditingDraft(draft);
      setEditForm({
        serviceId: draft.serviceId ?? '',
        ticketId: draft.ticketId ?? '',
        awoNumber: draft.awoNumber ?? '',
        customerName: draft.customerName ?? '',
        customerPhone: draft.customerPhone ?? '',
        customerEmail: draft.customerEmail ?? '',
        addressText: draft.addressText ?? '',
        oldAddress: draft.oldAddress ?? '',
        appointmentDate: draft.appointmentDate ?? '',
        appointmentWindow: draft.appointmentWindow ?? '',
        orderTypeCode: draft.orderTypeCode ?? '',
        orderCategoryId: draft.orderCategoryId ?? undefined,
        packageName: draft.packageName ?? '',
        bandwidth: draft.bandwidth ?? '',
        onuSerialNumber: draft.onuSerialNumber ?? '',
        onuPassword: draft.onuPassword ?? '',
        voipServiceId: draft.voipServiceId ?? '',
        remarks: draft.remarks ?? '',
        buildingId: draft.buildingId ?? undefined
      });
    } finally {
      if (openingDraftIdRef.current === draftId) openingDraftIdRef.current = null;
    }
  };

  const handleSaveEdit = async () => {
    if (!editingDraft) return;
    try {
      await updateDraftMutation.mutateAsync({ id: editingDraft.id, data: editForm });
      showSuccess('Draft updated');
    } catch (err: unknown) {
      showError(err instanceof Error ? err.message : 'Failed to update draft');
    }
  };

  // Refresh when page becomes visible (after returning from review page)
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        // Refetch when page becomes visible (user returns from another page)
        refetch();
      }
    };

    const handleFocus = () => {
      // Also refetch when window regains focus
      refetch();
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    window.addEventListener('focus', handleFocus);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      window.removeEventListener('focus', handleFocus);
    };
  }, [refetch]);

  // Update filters
  const updateFilter = (key: keyof ParsedOrderDraftFilters, value: any) => {
    setFilters(prev => ({ ...prev, [key]: value, page: 1 }));
  };

  const clearFilters = () => {
    setFilters({
      page: 1,
      pageSize: 50,
      sortBy: 'createdAt',
      sortOrder: 'desc'
    });
  };

  // Get status badge - compact version
  const getStatusBadge = (status: string) => {
    const statusMap: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
      'Pending': { label: 'Pending', variant: 'outline' },
      'Valid': { label: 'Valid', variant: 'default' },
      'NeedsReview': { label: 'Review', variant: 'secondary' },
      'Rejected': { label: 'Rejected', variant: 'destructive' }
    };
    const config = statusMap[status] || { label: status, variant: 'outline' as const };
    return <Badge variant={config.variant} className="text-[10px] px-1.5 py-0.5 leading-tight">{config.label}</Badge>;
  };

  // Table columns with fixed widths for compact, no-scroll layout
  // Total width: ~940px (fits within standard viewport)
  const columns = [
    {
      key: 'select',
      label: drafts.length > 0 ? (
        <button
          onClick={(e) => {
            e.stopPropagation();
            toggleSelectAll();
          }}
          className="p-1 rounded hover:bg-muted transition-colors"
          title={selectedDrafts.size === drafts.length ? 'Deselect All' : 'Select All'}
        >
          {selectedDrafts.size === drafts.length ? (
            <CheckSquare className="h-4 w-4 text-primary" />
          ) : (
            <Square className="h-4 w-4 text-muted-foreground" />
          )}
        </button>
      ) : '',
      width: '40px',
      render: (_: any, row: ParsedOrderDraft) => (
        <div className="flex justify-center">
          <button
            onClick={(e) => {
              e.stopPropagation();
              toggleDraftSelection(row.id);
            }}
            className="p-1 rounded hover:bg-muted transition-colors"
            title={selectedDrafts.has(row.id) ? 'Deselect' : 'Select'}
          >
            {selectedDrafts.has(row.id) ? (
              <CheckSquare className="h-4 w-4 text-primary" />
            ) : (
              <Square className="h-4 w-4 text-muted-foreground" />
            )}
          </button>
        </div>
      )
    },
    {
      key: 'serviceId',
      label: 'Service ID',
      width: '120px',
      render: (value: string, row: ParsedOrderDraft) => (
        <div className="flex items-center gap-1 min-w-0">
          <span className="font-medium text-[11px] truncate block" title={value || 'N/A'}>
            {value || 'N/A'}
          </span>
          {row.existingOrderId && (
            <a
              href={`/orders/${row.existingOrderId}`}
              target="_blank"
              rel="noopener noreferrer"
              onClick={(e) => e.stopPropagation()}
              className="flex-shrink-0 text-amber-600 hover:text-amber-700 dark:text-amber-400"
              title="Order with this Service ID already exists — opens existing order"
            >
              <AlertTriangle className="h-3.5 w-3.5" />
            </a>
          )}
        </div>
      )
    },
    {
      key: 'customerName',
      label: 'Customer',
      width: '110px',
      render: (value: string) => (
        <div className="flex items-center gap-1 min-w-0">
          <User className="h-3 w-3 text-muted-foreground flex-shrink-0" />
          <span className="text-[11px] truncate block" title={value || 'N/A'}>
            {value || 'N/A'}
          </span>
        </div>
      )
    },
    {
      key: 'addressText',
      label: 'Address',
      width: '220px', // Flexible but constrained - largest column
      render: (value: string) => (
        <div className="flex items-center gap-1 min-w-0">
          <Building2 className="h-3 w-3 text-muted-foreground flex-shrink-0" />
          <span className="text-[11px] truncate block" title={value || 'N/A'}>
            {value || 'N/A'}
          </span>
        </div>
      )
    },
    {
      key: 'buildingStatus',
      label: 'Building',
      width: '80px',
      render: (value: string, row: ParsedOrderDraft) => {
        if (row.buildingId) {
          return <Badge variant="default" className="text-[10px] px-1 py-0.5 leading-tight">Existing</Badge>;
        }
        if (value === 'New') {
          return <Badge variant="secondary" className="text-[10px] px-1 py-0.5 leading-tight">New</Badge>;
        }
        return <Badge variant="outline" className="text-[10px] px-1 py-0.5 leading-tight">Not Matched</Badge>;
      }
    },
    {
      key: 'validationStatus',
      label: 'Status',
      width: '90px',
      render: (value: string) => {
        const badge = getStatusBadge(value || 'Pending');
        return <div className="text-[10px]">{badge}</div>;
      }
    },
    {
      key: 'confidenceScore',
      label: 'Confidence',
      width: '80px',
      render: (value: number) => {
        const percentage = Math.round((value || 0) * 100);
        const color = percentage >= 70 ? 'bg-green-500' : percentage >= 50 ? 'bg-yellow-500' : 'bg-red-500';
        return (
          <div className="flex items-center gap-1">
            <div className="w-10 h-1.5 bg-slate-200 dark:bg-slate-700 rounded-full overflow-hidden flex-shrink-0">
              <div 
                className={cn('h-full', color)} 
                style={{ width: `${percentage}%` }}
              />
            </div>
            <span className="text-[10px] text-muted-foreground whitespace-nowrap">{percentage}%</span>
          </div>
        );
      }
    },
    {
      key: 'sourceFileName',
      label: 'Source',
      width: '110px',
      render: (value: string) => (
        <div className="flex items-center gap-1 min-w-0">
          <FileText className="h-3 w-3 text-muted-foreground flex-shrink-0" />
          <span className="text-[10px] truncate block" title={value || 'N/A'}>
            {value || 'N/A'}
          </span>
        </div>
      )
    },
    {
      key: 'createdAt',
      label: 'Created',
      width: '110px',
      render: (value: string) => {
        return (
          <div className="flex items-center gap-1">
            <Calendar className="h-3 w-3 text-muted-foreground flex-shrink-0" />
            <span className="text-[10px] whitespace-nowrap">{formatLocalDateTime(value)}</span>
          </div>
        );
      }
    },
    {
      key: 'parseSessionId',
      label: 'Session',
      width: '100px',
      render: (value: string, row: ParsedOrderDraft) => {
        if (!value) return <span className="text-[10px] text-muted-foreground">N/A</span>;
        return (
          <Button
            variant="ghost"
            size="sm"
            className="h-6 text-[10px] px-2"
            onClick={(e) => {
              e.stopPropagation();
              navigate(`/orders/parser/sessions/${value}`);
            }}
          >
            <Eye className="h-3 w-3 mr-1" />
            View
          </Button>
        );
      }
    },
    {
      key: 'actions',
      label: '',
      width: '80px',
      render: (_: string, row: ParsedOrderDraft) => (
        <div className="flex justify-center gap-0.5">
          <Button
            size="sm"
            variant="ghost"
            onClick={(e) => {
              e.stopPropagation();
              handleOpenEdit(row);
            }}
            title="Edit parsed fields"
            className="h-6 w-6 p-0 text-amber-600 hover:text-amber-700 dark:text-amber-400"
          >
            <FilePen className="h-3.5 w-3.5" />
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={(e) => {
              e.stopPropagation();
              void handleReview(row);
            }}
            title="Review & create order"
            className="h-6 w-6 p-0 text-blue-600 hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
          >
            <Eye className="h-3.5 w-3.5" />
          </Button>
        </div>
      )
    }
  ];

  if (isLoading && !pagedResult) {
    return (
      <PageShell title="Parser Listing" subtitle="Manage all parsed order drafts">
        <LoadingSpinner message="Loading parsed order drafts..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Parser Listing"
      subtitle={`${totalCount} parsed order draft(s) found`}
      actions={
        <div className="flex items-center gap-2">
          <Button
            size="sm"
            onClick={() => navigate('/orders/parser/dashboard')}
            variant="default"
          >
            <TrendingUp className="h-4 w-4 mr-2" />
            Dashboard
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => setShowFilters(!showFilters)}
          >
            <Filter className="h-4 w-4 mr-2" />
            Filters
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={async () => {
              try {
                setIsRefreshing(true);
                // Clear the query cache to force fresh data
                queryClient.removeQueries({ queryKey: ['parsedOrderDrafts'], exact: false });
                // Update refresh key to force new query
                setRefreshKey(prev => prev + 1);
                // Invalidate and refetch with current filters (bypasses staleTime)
                await queryClient.invalidateQueries({ 
                  queryKey: ['parsedOrderDrafts'],
                  exact: false
                });
                await refetch();
                showSuccess('Data refreshed successfully');
              } catch (error) {
                showError('Failed to refresh data');
                console.error('Refresh error:', error);
              } finally {
                setIsRefreshing(false);
              }
            }}
            disabled={isRefreshing}
          >
            <RefreshCw className={cn("h-4 w-4 mr-2", isRefreshing && "animate-spin")} />
            {isRefreshing ? 'Refreshing...' : 'Refresh'}
          </Button>
        </div>
      }
    >
      {/* Bulk Action Toolbar */}
      {selectedDrafts.size > 0 && (
        <Card className="mb-4 p-4 bg-primary/5 border-primary/20">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <span className="text-sm font-medium">
                {selectedDrafts.size} draft{selectedDrafts.size !== 1 ? 's' : ''} selected
              </span>
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setSelectedDrafts(new Set())}
              >
                Clear Selection
              </Button>
            </div>
            <div className="flex items-center gap-2">
              <Button
                size="sm"
                variant="default"
                onClick={() => setBulkAction('approve')}
                disabled={bulkApproveMutation.isPending}
              >
                <CheckCircle className="h-4 w-4 mr-2" />
                Approve Selected
              </Button>
              <Button
                size="sm"
                variant="destructive"
                onClick={() => setBulkAction('reject')}
                disabled={rejectMutation.isPending}
              >
                <XCircle className="h-4 w-4 mr-2" />
                Reject Selected
              </Button>
            </div>
          </div>
        </Card>
      )}

      {/* Duplicate Service ID warning (Review & create order) */}
      <Modal
        isOpen={!!duplicateReviewDraft}
        onClose={() => {
          setDuplicateReviewDraft(null);
          setDuplicateCheckOrderId(null);
          setDuplicateCheckTicketId(null);
        }}
        title="Service ID already exists"
        size="md"
      >
        <div className="space-y-4">
          {duplicateReviewDraft && (
            <>
              <p className="text-sm text-muted-foreground">
                An order with Service ID <strong>{duplicateReviewDraft.serviceId}</strong> already exists
                {duplicateCheckTicketId ? ` (Ticket: ${duplicateCheckTicketId})` : ''}.
                Proceeding may update that order depending on order type.
              </p>
              {duplicateCheckOrderId && (
                <p className="text-sm">
                  <a
                    href={`/orders/${duplicateCheckOrderId}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-primary underline hover:no-underline"
                  >
                    View existing order
                  </a>
                </p>
              )}
              <p className="text-sm text-muted-foreground">
                Do you want to continue to review and create/update the order?
              </p>
            </>
          )}
          <div className="flex justify-end gap-2">
            <Button
              variant="outline"
              onClick={() => {
                setDuplicateReviewDraft(null);
                setDuplicateCheckOrderId(null);
                setDuplicateCheckTicketId(null);
              }}
            >
              Cancel
            </Button>
            <Button onClick={handleConfirmReviewWithDuplicate}>
              Continue to review
            </Button>
          </div>
        </div>
      </Modal>

      {/* Bulk Approve Confirmation Modal */}
      <Modal
        isOpen={bulkAction === 'approve'}
        onClose={() => {
          setBulkAction(null);
          setBulkActionNotes('');
        }}
        title="Bulk Approve Drafts"
        size="md"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Are you sure you want to approve <strong>{selectedDrafts.size}</strong> draft(s)? 
            This will create orders for all selected drafts.
          </p>
          <div className="flex justify-end gap-2">
            <Button
              variant="outline"
              onClick={() => {
                setBulkAction(null);
                setBulkActionNotes('');
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleBulkApprove}
              disabled={bulkApproveMutation.isPending}
            >
              {bulkApproveMutation.isPending ? (
                <>
                  <LoadingSpinner size="sm" className="mr-2" />
                  Approving...
                </>
              ) : (
                <>
                  <CheckCircle className="h-4 w-4 mr-2" />
                  Approve {selectedDrafts.size} Draft(s)
                </>
              )}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Bulk Reject Modal */}
      <Modal
        isOpen={bulkAction === 'reject'}
        onClose={() => {
          setBulkAction(null);
          setBulkActionNotes('');
        }}
        title="Bulk Reject Drafts"
        size="md"
      >
        <div className="space-y-4">
          <p className="text-sm text-muted-foreground">
            You are about to reject <strong>{selectedDrafts.size}</strong> draft(s). 
            Please provide rejection notes (required).
          </p>
          <div>
            <Label htmlFor="reject-notes">Rejection Notes</Label>
            <Input
              id="reject-notes"
              placeholder="Enter rejection reason..."
              value={bulkActionNotes}
              onChange={(e) => setBulkActionNotes(e.target.value)}
              className="mt-1"
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button
              variant="outline"
              onClick={() => {
                setBulkAction(null);
                setBulkActionNotes('');
              }}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleBulkReject}
              disabled={rejectMutation.isPending || !bulkActionNotes.trim()}
            >
              {rejectMutation.isPending ? (
                <>
                  <LoadingSpinner size="sm" className="mr-2" />
                  Rejecting...
                </>
              ) : (
                <>
                  <XCircle className="h-4 w-4 mr-2" />
                  Reject {selectedDrafts.size} Draft(s)
                </>
              )}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Edit Draft Modal - inline edit parsed fields before approving */}
      <Modal
        isOpen={editingDraft != null}
        onClose={() => {
          openingDraftIdRef.current = null;
          setEditingDraft(null);
          setEditForm({});
        }}
        title="Edit parsed draft"
        size="md"
      >
        {editingDraft && (
          <div className="space-y-4">
            {/* Unmatched parsed materials warning (backend truth) + Match Material */}
            {(editingDraft.unmatchedMaterialCount ?? 0) > 0 && (
              <div className="rounded-md border border-amber-500/60 bg-amber-500/10 p-3 text-sm text-amber-800 dark:text-amber-200">
                <span className="font-medium">⚠ {editingDraft.unmatchedMaterialCount} material(s) could not be matched</span>
                {Array.isArray(editingDraft.unmatchedMaterialNames) && editingDraft.unmatchedMaterialNames.length > 0 && (
                  <ul className="mt-1 list-none space-y-1">
                    {editingDraft.unmatchedMaterialNames.map((name, i) => (
                      <li key={i} className="flex items-center justify-between gap-2">
                        <span>{name}</span>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          className="shrink-0 h-7 text-xs"
                          onClick={() => setMatchMaterialName(name)}
                        >
                          Match Material
                        </Button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}
            {/* Partner (read-only, parser-derived) */}
            <div>
              <Label className="text-muted-foreground">Partner</Label>
              <Input
                readOnly
                disabled
                value={editingDraft.partnerCode?.trim() || '—'}
                className="bg-muted"
              />
            </div>
            {/* Building: resolved label (matches persisted buildingId) or parsed text; dropdown only when no BuildingId */}
            <div>
              <Label>Building</Label>
              {editForm.buildingId ? (
                <Input
                  readOnly
                  value={
                    editingDraft.suggestedBuildings?.find(s => s.building.id === editForm.buildingId)?.building.name ??
                    editingDraft.buildingName?.trim() ?? '—'
                  }
                  className="bg-muted"
                />
              ) : editingDraft.suggestedBuildings && editingDraft.suggestedBuildings.length > 0 ? (
                <Select
                  value={editForm.buildingId ?? ''}
                  onChange={(e) => {
                    const id = e.target.value || undefined;
                    const c = editingDraft.suggestedBuildings?.find(s => s.building.id === id);
                    setEditForm(f => ({ ...f, buildingId: id }));
                    if (c && editingDraft) setEditingDraft({ ...editingDraft, buildingId: id, buildingName: c.building.name });
                  }}
                  placeholder={editingDraft.buildingName ?? 'Select building...'}
                  options={editingDraft.suggestedBuildings.map((s) => ({
                    value: s.building.id,
                    label: `${s.building.name} (${Math.round(s.similarityScore * 100)}% match)`
                  }))}
                />
              ) : (
                <Input
                  readOnly
                  value={editingDraft.buildingName?.trim() ?? '—'}
                  className="bg-muted"
                  placeholder="Parsed building name (no match found)"
                />
              )}
            </div>
            {/* Order Sub-Type (bound to parser draft; persisted on save) */}
            <div>
              <Label>Order Sub-Type</Label>
              <Input
                value={editForm.orderTypeCode ?? ''}
                onChange={(e) => setEditForm(f => ({ ...f, orderTypeCode: e.target.value || undefined }))}
                placeholder="e.g. MODIFICATION_OUTDOOR"
              />
            </div>
            {/* Order Category (editable; persisted on save) */}
            <div>
              <Label>Order Category</Label>
              <Select
                value={editForm.orderCategoryId ?? ''}
                onChange={(e) => setEditForm(f => ({ ...f, orderCategoryId: e.target.value || undefined }))}
                placeholder="Select category (optional)"
                options={orderCategoryOptions}
              />
            </div>
            {/* Missing Material / Parsed materials (display-only from parser snapshot) */}
            {Array.isArray(editingDraft.materials) && editingDraft.materials.length > 0 && (
              <div>
                <Label>Missing Material / Parsed materials</Label>
                <div className="rounded border bg-muted/30 p-2 text-sm">
                  {editingDraft.materials.map((m, idx) => (
                    <div key={m?.id ?? `m-${idx}`} className="flex justify-between gap-2 py-0.5">
                      <span>{m?.name ?? '—'}</span>
                      {m?.quantity != null && <span className="text-muted-foreground">{m.quantity} {m.unitOfMeasure ?? ''}</span>}
                    </div>
                  ))}
                </div>
              </div>
            )}
            <div className="grid grid-cols-1 gap-3">
              <div>
                <Label>Service ID</Label>
                <Input
                  value={editForm.serviceId ?? ''}
                  onChange={(e) => setEditForm(f => ({ ...f, serviceId: e.target.value || undefined }))}
                  placeholder="Service ID"
                />
              </div>
              <div>
                <Label>Customer name</Label>
                <Input
                  value={editForm.customerName ?? ''}
                  onChange={(e) => setEditForm(f => ({ ...f, customerName: e.target.value || undefined }))}
                  placeholder="Customer name"
                />
              </div>
              <div>
                <Label>Customer phone</Label>
                <Input
                  value={editForm.customerPhone ?? ''}
                  onChange={(e) => setEditForm(f => ({ ...f, customerPhone: e.target.value || undefined }))}
                  placeholder="Phone"
                />
              </div>
              <div>
                <Label>Customer email</Label>
                <Input
                  type="email"
                  value={editForm.customerEmail ?? ''}
                  onChange={(e) => setEditForm(f => ({ ...f, customerEmail: e.target.value || undefined }))}
                  placeholder="Email"
                />
              </div>
              <div>
                <Label>Address</Label>
                <Input
                  value={editForm.addressText ?? ''}
                  onChange={(e) => setEditForm(f => ({ ...f, addressText: e.target.value || undefined }))}
                  placeholder="Address"
                />
              </div>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <Label>Appointment date</Label>
                  <Input
                    type="date"
                    value={editForm.appointmentDate ?? ''}
                    onChange={(e) => setEditForm(f => ({ ...f, appointmentDate: e.target.value || undefined }))}
                  />
                </div>
                <div>
                  <Label>Appointment window</Label>
                  <Input
                    value={editForm.appointmentWindow ?? ''}
                    onChange={(e) => setEditForm(f => ({ ...f, appointmentWindow: e.target.value || undefined }))}
                    placeholder="e.g. 09:00-17:00"
                  />
                </div>
              </div>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button
                variant="outline"
                onClick={() => { setEditingDraft(null); setEditForm({}); }}
              >
                Cancel
              </Button>
              <Button
                onClick={handleSaveEdit}
                disabled={updateDraftMutation.isPending}
              >
                {updateDraftMutation.isPending ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </div>
        )}
      </Modal>

      {/* Match Material modal: map unmatched parsed name to Material (creates alias, future drafts auto-resolve) */}
      <MatchMaterialModal
        open={matchMaterialName !== null}
        parsedName={matchMaterialName ?? ''}
        selectedMaterialId={matchMaterialSelectedId}
        onSelectedMaterialIdChange={setMatchMaterialSelectedId}
        onClose={() => {
          setMatchMaterialName(null);
          setMatchMaterialSelectedId('');
        }}
        onSave={async () => {
          if (!matchMaterialName || !matchMaterialSelectedId || !editingDraft) return;
          setMatchMaterialSaving(true);
          try {
            await createParsedMaterialAlias({ aliasText: matchMaterialName, materialId: matchMaterialSelectedId });
            showSuccess('Alias saved. Future drafts will resolve this name automatically.');
            const updated = await getParsedOrderDraft(editingDraft.id);
            setEditingDraft(updated);
            setMatchMaterialName(null);
            setMatchMaterialSelectedId('');
          } catch (e: unknown) {
            showError(e instanceof Error ? e.message : 'Failed to save alias');
          } finally {
            setMatchMaterialSaving(false);
          }
        }}
        saving={matchMaterialSaving}
      />

      {/* Quick filters */}
      <div className="mb-3 flex flex-wrap items-center gap-2">
        <span className="text-xs text-muted-foreground mr-1">Quick:</span>
        <Button
          size="sm"
          variant={filters.validationStatus === 'Pending' ? 'default' : 'outline'}
          onClick={() => updateFilter('validationStatus', filters.validationStatus === 'Pending' ? undefined : 'Pending')}
        >
          Pending review
        </Button>
        <Button
          size="sm"
          variant={filters.confidenceMin === 0.8 ? 'default' : 'outline'}
          onClick={() => updateFilter('confidenceMin', filters.confidenceMin === 0.8 ? undefined : 0.8)}
        >
          Confidence &gt; 80%
        </Button>
        <Button
          size="sm"
          variant={filters.buildingMatched === true ? 'default' : 'outline'}
          onClick={() => updateFilter('buildingMatched', filters.buildingMatched === true ? undefined : true)}
        >
          Building matched
        </Button>
        {(filters.validationStatus === 'Pending' || filters.confidenceMin === 0.8 || filters.buildingMatched === true) && (
          <Button
            size="sm"
            variant="ghost"
            onClick={() => {
              setFilters(prev => ({
                ...prev,
                validationStatus: undefined,
                confidenceMin: undefined,
                buildingMatched: undefined,
                page: 1
              }));
            }}
          >
            Clear quick
          </Button>
        )}
      </div>

      {/* Filters Panel */}
      {showFilters && (
        <Card className="mb-4 p-4">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div>
              <Select
                label="Validation Status"
                value={filters.validationStatus || 'all'}
                onChange={(e) => updateFilter('validationStatus', e.target.value === 'all' ? undefined : e.target.value)}
                options={[
                  { value: 'all', label: 'All' },
                  { value: 'Pending', label: 'Pending' },
                  { value: 'Valid', label: 'Valid' },
                  { value: 'NeedsReview', label: 'Needs Review' },
                  { value: 'Rejected', label: 'Rejected' }
                ]}
              />
            </div>
            <div>
              <Select
                label="Source Type"
                value={filters.sourceType || 'all'}
                onChange={(e) => updateFilter('sourceType', e.target.value === 'all' ? undefined : e.target.value)}
                options={[
                  { value: 'all', label: 'All' },
                  { value: 'Email', label: 'Email' },
                  { value: 'FileUpload', label: 'File Upload' }
                ]}
              />
            </div>
            <div>
              <Label>Service ID</Label>
              <Input
                placeholder="Search Service ID..."
                value={filters.serviceId || ''}
                onChange={(e) => updateFilter('serviceId', e.target.value || undefined)}
              />
            </div>
            <div>
              <Label>Customer Name</Label>
              <Input
                placeholder="Search Customer Name..."
                value={filters.customerName || ''}
                onChange={(e) => updateFilter('customerName', e.target.value || undefined)}
              />
            </div>
            <div>
              <Label>From Date</Label>
              <Input
                type="date"
                value={filters.fromDate || ''}
                onChange={(e) => updateFilter('fromDate', e.target.value || undefined)}
              />
            </div>
            <div>
              <Label>To Date</Label>
              <Input
                type="date"
                value={filters.toDate || ''}
                onChange={(e) => updateFilter('toDate', e.target.value || undefined)}
              />
            </div>
            <div>
              <Select
                label="Confidence min"
                value={filters.confidenceMin != null ? filters.confidenceMin.toString() : 'all'}
                onChange={(e) => updateFilter('confidenceMin', e.target.value === 'all' ? undefined : parseFloat(e.target.value))}
                options={[
                  { value: 'all', label: 'All' },
                  { value: '0.5', label: '≥ 50%' },
                  { value: '0.7', label: '≥ 70%' },
                  { value: '0.8', label: '≥ 80%' },
                  { value: '0.9', label: '≥ 90%' }
                ]}
              />
            </div>
            <div>
              <Select
                label="Building Status"
                value={filters.buildingStatus || 'all'}
                onChange={(e) => updateFilter('buildingStatus', e.target.value === 'all' ? undefined : e.target.value)}
                options={[
                  { value: 'all', label: 'All' },
                  { value: 'Existing', label: 'Existing' },
                  { value: 'New', label: 'New' }
                ]}
              />
            </div>
            <div>
              <Select
                label="Building matched"
                value={filters.buildingMatched === true ? 'yes' : filters.buildingMatched === false ? 'no' : 'all'}
                onChange={(e) => updateFilter('buildingMatched', e.target.value === 'all' ? undefined : e.target.value === 'yes')}
                options={[
                  { value: 'all', label: 'Any' },
                  { value: 'yes', label: 'Matched' },
                  { value: 'no', label: 'Not matched' }
                ]}
              />
            </div>
            <div className="flex items-end gap-2">
              <Button
                size="sm"
                variant="outline"
                onClick={clearFilters}
              >
                Clear
              </Button>
            </div>
          </div>
        </Card>
      )}

      {/* Data Table - Compact, no-scroll layout */}
      <Card className="overflow-hidden">
        <style>{`
          .parser-table-wrapper table {
            min-width: 0 !important;
            table-layout: fixed !important;
            width: 100% !important;
          }
          .parser-table-wrapper th {
            padding: 0.375rem 0.5rem !important;
            font-size: 0.625rem !important;
            font-weight: 600 !important;
            text-transform: uppercase;
            letter-spacing: 0.05em;
          }
          .parser-table-wrapper td {
            padding: 0.375rem 0.5rem !important;
            font-size: 0.6875rem !important;
            height: 2rem !important;
            white-space: nowrap !important;
            overflow: hidden !important;
          }
          .parser-table-wrapper .overflow-x-auto {
            overflow-x: visible !important;
          }
        `}</style>
        <div className="parser-table-wrapper">
          <DataTable
            data={drafts}
            columns={columns}
            loading={isLoading}
            className="w-full"
          />
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between p-4 border-t">
            <div className="text-sm text-muted-foreground">
              Showing {(filters.page! - 1) * filters.pageSize! + 1} to {Math.min(filters.page! * filters.pageSize!, totalCount)} of {totalCount} results
            </div>
            <div className="flex items-center gap-2">
              <Button
                size="sm"
                variant="outline"
                onClick={() => updateFilter('page', Math.max(1, filters.page! - 1))}
                disabled={filters.page === 1}
              >
                Previous
              </Button>
              <span className="text-sm">
                Page {filters.page} of {totalPages}
              </span>
              <Button
                size="sm"
                variant="outline"
                onClick={() => updateFilter('page', Math.min(totalPages, filters.page! + 1))}
                disabled={filters.page === totalPages}
              >
                Next
              </Button>
            </div>
          </div>
        )}
      </Card>

    </PageShell>
  );
};

export default ParserListingPage;

