import React, { useState, useEffect, useRef } from 'react';
import { Eye, Download, X, ZoomIn, ZoomOut, RotateCw, FileText, Image, Mail, Calendar, User, CheckCircle, AlertTriangle, Clock, ChevronLeft, ChevronRight, Search } from 'lucide-react';
import { PdfViewerComponent, Toolbar, Magnification, Navigation, LinkAnnotation, BookmarkView, ThumbnailView, Print, TextSelection, TextSearch, FormDesigner, FormFields, Annotation, Inject } from '@syncfusion/ej2-react-pdfviewer';
import { LoadingSpinner, EmptyState, useToast, Button, Card, Modal, TextInput, StatusBadge } from '../../components/ui';
import { PageShell } from '../../components/layout';
import apiClient from '../../api/client';
import { getApiBaseUrl } from '../../api/config';
import { formatLocalDateTime } from '../../utils/dateUtils';

interface Guide {
  number: number;
  title: string;
  content: string;
}

interface CollapsibleGuideProps {
  title: string;
  description: string;
  guides: Guide[];
}

const CollapsibleGuide: React.FC<CollapsibleGuideProps> = ({ title, description, guides }) => {
  const [isOpen, setIsOpen] = useState<boolean>(false);
  
  return (
    <div className="mb-4 border border-slate-200 rounded-lg overflow-hidden bg-slate-50">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="w-full flex items-center justify-between px-3 py-2 text-left hover:bg-slate-100 transition-colors"
      >
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-slate-700">{title}</span>
          <span className="text-xs text-slate-500">— {description}</span>
        </div>
        <span className="text-xs text-slate-400">{isOpen ? '▲ Hide' : '▼ Show Guide'}</span>
      </button>
      
      {isOpen && (
        <div className="px-3 py-2 border-t border-slate-200 bg-white">
          <div className="grid grid-cols-4 gap-2">
            {guides.map((guide, idx) => (
              <div key={idx} className="flex items-start gap-2">
                <div className="flex-shrink-0 w-4 h-4 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-bold">
                  {guide.number}
                </div>
                <div>
                  <h4 className="text-xs font-semibold text-slate-800">{guide.title}</h4>
                  <p className="text-xs text-slate-600 leading-tight">{guide.content}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

interface ParseSession {
  id: string;
  status: string;
  snapshotFileId?: string;
  emailMessage?: {
    subject?: string;
    fromAddress?: string;
  };
  parsedOrdersCount?: number;
  isVip?: boolean;
  errorMessage?: string;
  createdAt: string;
  completedAt?: string;
}

interface Filters {
  status: string;
  dateFrom: string;
  dateTo: string;
  search: string;
}

interface Pagination {
  page: number;
  pageSize: number;
  total: number;
}

const ParserSnapshotViewerPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [sessions, setSessions] = useState<ParseSession[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedSession, setSelectedSession] = useState<ParseSession | null>(null);
  const [snapshotUrl, setSnapshotUrl] = useState<string | null>(null);
  const [snapshotLoading, setSnapshotLoading] = useState<boolean>(false);
  const [isPdf, setIsPdf] = useState<boolean>(true);
  const pdfViewerRef = useRef<any>(null);
  const [filters, setFilters] = useState<Filters>({
    status: '',
    dateFrom: '',
    dateTo: '',
    search: ''
  });
  const [pagination, setPagination] = useState<Pagination>({
    page: 1,
    pageSize: 20,
    total: 0
  });

  useEffect(() => {
    loadSessions();
  }, [filters, pagination.page]);

  const loadSessions = async (): Promise<void> => {
    try {
      setLoading(true);
      setError(null);
      const params: Record<string, unknown> = {
        page: pagination.page,
        pageSize: pagination.pageSize,
        ...filters
      };
      const response = await apiClient.get('/parser/sessions', { params });
      if (response && (response as any).items) {
        setSessions((response as any).items);
        setPagination(prev => ({ ...prev, total: (response as any).total }));
      } else if (Array.isArray(response)) {
        setSessions(response);
      }
    } catch (err) {
      setError((err as Error).message || 'Failed to load parse sessions');
      console.error('Error loading sessions:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadSnapshot = async (session: ParseSession): Promise<void> => {
    if (!session.snapshotFileId) {
      showError('No snapshot available for this session');
      return;
    }
    
    try {
      setSnapshotLoading(true);
      setSelectedSession(session);
      // Get the snapshot file URL
      const apiBaseUrl = getApiBaseUrl();
      const url = `${apiBaseUrl}/files/${session.snapshotFileId}/download`;
      
      // Check if it's a PDF or image
      try {
        const response = await fetch(url, { method: 'HEAD' });
        const contentType = response.headers.get('content-type') || '';
        setIsPdf(contentType.includes('pdf') || url.toLowerCase().endsWith('.pdf'));
      } catch {
        // Default to PDF if we can't determine
        setIsPdf(true);
      }
      
      setSnapshotUrl(url);
    } catch (err) {
      showError('Failed to load snapshot');
      console.error('Error loading snapshot:', err);
    } finally {
      setSnapshotLoading(false);
    }
  };

  const handleApprove = async (sessionId: string): Promise<void> => {
    try {
      await apiClient.post(`/parser/sessions/${sessionId}/approve`);
      showSuccess('Parse session approved');
      await loadSessions();
    } catch (err) {
      showError((err as Error).message || 'Failed to approve session');
    }
  };

  const handleReject = async (sessionId: string, reason: string): Promise<void> => {
    try {
      await apiClient.post(`/parser/sessions/${sessionId}/reject`, { reason });
      showSuccess('Parse session rejected');
      await loadSessions();
    } catch (err) {
      showError((err as Error).message || 'Failed to reject session');
    }
  };

  const getStatusColor = (status?: string): string => {
    switch (status?.toLowerCase()) {
      case 'success': return 'bg-green-100 text-green-800';
      case 'failed': return 'bg-red-100 text-red-800';
      case 'pending': return 'bg-yellow-100 text-yellow-800';
      case 'running': return 'bg-blue-100 text-blue-800';
      default: return 'bg-slate-100 text-slate-800';
    }
  };

  const closeViewer = (): void => {
    setSelectedSession(null);
    setSnapshotUrl(null);
    setIsPdf(true);
    if (pdfViewerRef.current) {
      pdfViewerRef.current.destroy();
    }
  };

  if (loading && sessions.length === 0) {
    return (
      <PageShell title="Parser Snapshot Viewer" breadcrumbs={[{ label: 'Parser', path: '/parser' }, { label: 'Snapshot Viewer' }]}>
        <LoadingSpinner message="Loading parse sessions..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Parser Snapshot Viewer"
      breadcrumbs={[{ label: 'Parser', path: '/parser' }, { label: 'Snapshot Viewer' }]}
      actions={
        <Button variant="outline" onClick={loadSessions}>
          <RotateCw className="h-4 w-4 mr-1" />
          Refresh
        </Button>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      {/* How-to Guide */}
      <CollapsibleGuide
        title="How to Use Parser Snapshot Viewer"
        description="View and verify parsed emails before approving orders. Snapshots are PDF/image copies of the original email or Excel attachment."
        guides={[
          {
            number: 1,
            title: "Why Snapshots?",
            content: "Snapshots let you verify parser accuracy without accessing the original email. They're stored for 7 days by default for audit purposes."
          },
          {
            number: 2,
            title: "Approve or Reject",
            content: "Review the snapshot against parsed data. If correct, Approve to create Order. If wrong, Reject with a reason for manual entry."
          },
          {
            number: 3,
            title: "Zoom & Rotate",
            content: "Use zoom controls to examine details. Rotate if the snapshot was captured sideways. Download for offline review."
          },
          {
            number: 4,
            title: "Filter by Status",
            content: "Filter by Pending (needs review), Success (auto-approved), Failed (parsing errors). Focus on Pending first."
          }
        ]}
      />

      {/* Filters */}
      <Card className="p-4 mb-4">
        <div className="flex flex-wrap items-center gap-4">
          <div className="flex-1 min-w-[200px]">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                placeholder="Search by email subject, sender..."
                value={filters.search}
                onChange={(e) => setFilters({ ...filters, search: e.target.value })}
                className="w-full pl-10 pr-4 py-2 border border-slate-200 rounded-md text-sm"
              />
            </div>
          </div>
          <div>
            <select
              value={filters.status}
              onChange={(e) => setFilters({ ...filters, status: e.target.value })}
              className="px-3 py-2 border border-slate-200 rounded-md text-sm"
            >
              <option value="">All Statuses</option>
              <option value="Pending">Pending Review</option>
              <option value="Success">Success</option>
              <option value="Failed">Failed</option>
              <option value="Running">Running</option>
            </select>
          </div>
          <div className="flex items-center gap-2">
            <input
              type="date"
              value={filters.dateFrom}
              onChange={(e) => setFilters({ ...filters, dateFrom: e.target.value })}
              className="px-3 py-2 border border-slate-200 rounded-md text-sm"
            />
            <span className="text-slate-400">to</span>
            <input
              type="date"
              value={filters.dateTo}
              onChange={(e) => setFilters({ ...filters, dateTo: e.target.value })}
              className="px-3 py-2 border border-slate-200 rounded-md text-sm"
            />
          </div>
        </div>
      </Card>

      {/* Error Banner */}
      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-red-800 flex items-center gap-2 text-sm" role="alert">
          <AlertTriangle className="h-4 w-4" />
          {error}
          <button className="ml-auto hover:opacity-70" onClick={() => setError(null)} aria-label="Close">
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      {/* Sessions List */}
      {sessions.length === 0 ? (
        <EmptyState
          title="No parse sessions found"
          description="Parse sessions will appear here when emails are processed."
        />
      ) : (
        <div className="space-y-3">
          {sessions.map((session) => (
            <Card key={session.id} className="p-4 hover:shadow-md transition-shadow">
              <div className="flex items-start justify-between">
                <div className="flex items-start gap-4">
                  {/* Icon based on type */}
                  <div className="w-12 h-12 rounded-lg bg-slate-100 flex items-center justify-center">
                    {session.snapshotFileId ? (
                      <Image className="h-6 w-6 text-slate-500" />
                    ) : (
                      <Mail className="h-6 w-6 text-slate-500" />
                    )}
                  </div>
                  
                  {/* Session Info */}
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <h3 className="font-semibold text-sm">
                        {session.emailMessage?.subject || `Session #${session.id.substring(0, 8)}`}
                      </h3>
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${getStatusColor(session.status)}`}>
                        {session.status}
                      </span>
                      {session.isVip && (
                        <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
                          VIP
                        </span>
                      )}
                    </div>
                    <div className="flex flex-wrap items-center gap-3 text-xs text-slate-500">
                      {session.emailMessage?.fromAddress && (
                        <span className="flex items-center gap-1">
                          <User className="h-3 w-3" />
                          {session.emailMessage.fromAddress}
                        </span>
                      )}
                      <span className="flex items-center gap-1">
                        <Calendar className="h-3 w-3" />
                        {formatLocalDateTime(session.createdAt)}
                      </span>
                      <span className="flex items-center gap-1">
                        <FileText className="h-3 w-3" />
                        {session.parsedOrdersCount || 0} orders parsed
                      </span>
                    </div>
                    {session.errorMessage && (
                      <p className="mt-2 text-xs text-red-600 bg-red-50 px-2 py-1 rounded">
                        {session.errorMessage}
                      </p>
                    )}
                  </div>
                </div>

                {/* Actions */}
                <div className="flex items-center gap-2">
                  {session.snapshotFileId && (
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => loadSnapshot(session)}
                    >
                      <Eye className="h-4 w-4 mr-1" />
                      View Snapshot
                    </Button>
                  )}
                  {session.status === 'Pending' && (
                    <>
                      <Button
                        size="sm"
                        onClick={() => handleApprove(session.id)}
                      >
                        <CheckCircle className="h-4 w-4 mr-1" />
                        Approve
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          const reason = prompt('Enter rejection reason:');
                          if (reason) handleReject(session.id, reason);
                        }}
                      >
                        <X className="h-4 w-4 mr-1" />
                        Reject
                      </Button>
                    </>
                  )}
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Pagination */}
      {pagination.total > pagination.pageSize && (
        <div className="mt-4 flex items-center justify-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPagination(prev => ({ ...prev, page: prev.page - 1 }))}
            disabled={pagination.page === 1}
          >
            <ChevronLeft className="h-4 w-4" />
            Previous
          </Button>
          <span className="text-sm text-slate-600">
            Page {pagination.page} of {Math.ceil(pagination.total / pagination.pageSize)}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPagination(prev => ({ ...prev, page: prev.page + 1 }))}
            disabled={pagination.page >= Math.ceil(pagination.total / pagination.pageSize)}
          >
            Next
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      )}

      {/* Snapshot Viewer Modal */}
      {selectedSession && (
        <div className="fixed inset-0 bg-black/80 z-50 flex items-center justify-center">
          <div className="absolute inset-4 bg-white rounded-lg flex flex-col">
            {/* Viewer Header */}
            <div className="flex items-center justify-between px-4 py-3 border-b">
              <div>
                <h2 className="font-semibold">
                  {selectedSession.emailMessage?.subject || 'Snapshot Viewer'}
                </h2>
                <p className="text-sm text-slate-500">
                  Session ID: {selectedSession.id.substring(0, 8)}... • 
                  Parsed {selectedSession.parsedOrdersCount} orders
                </p>
              </div>
              <div className="flex items-center gap-2">
                {/* Download */}
                {snapshotUrl && (
                  <a
                    href={snapshotUrl}
                    download
                    className="p-2 hover:bg-slate-100 rounded"
                    title="Download"
                  >
                    <Download className="h-4 w-4" />
                  </a>
                )}
                {/* Close */}
                <button
                  onClick={closeViewer}
                  className="p-2 hover:bg-slate-100 rounded"
                  title="Close"
                >
                  <X className="h-5 w-5" />
                </button>
              </div>
            </div>

            {/* Viewer Content */}
            <div className="flex-1 overflow-hidden bg-slate-100">
              {snapshotLoading ? (
                <div className="flex items-center justify-center h-full">
                  <LoadingSpinner message="Loading snapshot..." />
                </div>
              ) : snapshotUrl ? (
                isPdf ? (
                  <PdfViewerComponent
                    ref={pdfViewerRef}
                    id="pdf-viewer-snapshot"
                    serviceUrl=""
                    documentPath={snapshotUrl}
                    enableToolbar={true}
                    enableNavigation={true}
                    enableThumbnail={true}
                    enableBookmark={true}
                    enableTextSearch={true}
                    enableTextSelection={true}
                    enablePrint={true}
                    enableDownload={true}
                    style={{ height: '100%', width: '100%' }}
                  >
                    <Inject services={[
                      Toolbar,
                      Magnification,
                      Navigation,
                      LinkAnnotation,
                      BookmarkView,
                      ThumbnailView,
                      Print,
                      TextSelection,
                      TextSearch,
                      FormDesigner,
                      FormFields,
                      Annotation
                    ]} />
                  </PdfViewerComponent>
                ) : (
                  <div className="flex items-center justify-center h-full p-4">
                    <img
                      src={snapshotUrl}
                      alt="Parser Snapshot"
                      className="max-w-full max-h-full rounded shadow-lg"
                      onError={(e) => {
                        (e.target as HTMLImageElement).onerror = null;
                        (e.target as HTMLImageElement).src = 'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" width="400" height="300"><rect fill="%23f1f5f9" width="100%" height="100%"/><text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle" fill="%2394a3b8">Failed to load image</text></svg>';
                      }}
                    />
                  </div>
                )
              ) : (
                <div className="flex items-center justify-center h-full text-center text-slate-500">
                  <div>
                    <Image className="h-16 w-16 mx-auto mb-4 opacity-30" />
                    <p>No snapshot available</p>
                  </div>
                </div>
              )}
            </div>

            {/* Viewer Footer with Actions */}
            <div className="flex items-center justify-between px-4 py-3 border-t bg-white">
              <div className="flex items-center gap-4 text-sm">
                <span className={`px-2 py-1 rounded ${getStatusColor(selectedSession.status)}`}>
                  {selectedSession.status}
                </span>
                {selectedSession.completedAt && (
                  <span className="text-slate-500">
                    Completed: {formatLocalDateTime(selectedSession.completedAt)}
                  </span>
                )}
              </div>
              <div className="flex items-center gap-2">
                {selectedSession.status === 'Pending' && (
                  <>
                    <Button onClick={() => handleApprove(selectedSession.id)}>
                      <CheckCircle className="h-4 w-4 mr-1" />
                      Approve & Create Order
                    </Button>
                    <Button
                      variant="outline"
                      onClick={() => {
                        const reason = prompt('Enter rejection reason:');
                        if (reason) {
                          handleReject(selectedSession.id, reason);
                          closeViewer();
                        }
                      }}
                    >
                      <X className="h-4 w-4 mr-1" />
                      Reject
                    </Button>
                  </>
                )}
                <Button variant="outline" onClick={closeViewer}>
                  Close
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
      </div>
    </PageShell>
  );
};

export default ParserSnapshotViewerPage;

