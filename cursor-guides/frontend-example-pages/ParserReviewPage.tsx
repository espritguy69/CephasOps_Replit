import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Upload, FileText, Check, X, AlertTriangle } from 'lucide-react';
import { useDepartment } from '../../contexts/DepartmentContext';
import { useToast } from '../../components/ui';
import { Button, Card, LoadingSpinner, Modal } from '../../components/ui';
import { PageShell } from '../../components/layout';

/**
 * PATTERN: Parser Review Page
 * 
 * This page handles the workflow for reviewing and approving
 * parsed orders from uploaded files (Excel, PDF, Email).
 * 
 * Key conventions:
 * - Fetch parsed drafts for the active department
 * - Allow review, edit, approve, or reject
 * - On approval, create actual orders
 * - Handle file upload for manual parsing
 */

// ==================== TYPES ====================

interface ParsedOrderDraft {
  id: string;
  sessionId: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  orderTypeCode: string;
  orderTypeHint: string;
  serviceId?: string;
  ticketId?: string;
  customerName?: string;
  customerPhone?: string;
  customerEmail?: string;
  serviceAddress?: string;
  appointmentDate?: string;
  appointmentWindow?: string;
  confidenceScore: number;
  validationNotes?: string;
  sourceFileName?: string;
  createdAt: string;
}

interface ParseSession {
  id: string;
  sourceType: string;
  status: string;
  drafts: ParsedOrderDraft[];
  createdAt: string;
}

// ==================== API FUNCTIONS ====================

const getActiveParseSessions = async (departmentId?: string): Promise<ParseSession[]> => {
  // Implementation would call your API
  const response = await fetch(`/api/parser/sessions?status=PendingReview&departmentId=${departmentId || ''}`);
  const data = await response.json();
  return data;
};

const approveDraft = async (draftId: string, userId: string): Promise<{ orderId: string }> => {
  const response = await fetch(`/api/parser/drafts/${draftId}/approve`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId }),
  });
  return response.json();
};

const rejectDraft = async (draftId: string, reason: string): Promise<void> => {
  await fetch(`/api/parser/drafts/${draftId}/reject`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ reason }),
  });
};

// ==================== COMPONENT ====================

export const ParserReviewPage: React.FC = () => {
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const { departmentId, loading: departmentLoading } = useDepartment();

  const [sessions, setSessions] = useState<ParseSession[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedDraft, setSelectedDraft] = useState<ParsedOrderDraft | null>(null);
  const [showUploadModal, setShowUploadModal] = useState(false);

  // Load sessions when department is ready
  React.useEffect(() => {
    if (departmentLoading) return;
    loadSessions();
  }, [departmentId, departmentLoading]);

  const loadSessions = async () => {
    try {
      setLoading(true);
      const data = await getActiveParseSessions(departmentId || undefined);
      setSessions(data);
    } catch (err) {
      showError('Failed to load parse sessions');
    } finally {
      setLoading(false);
    }
  };

  // PATTERN: Approve draft and create order
  const handleApprove = useCallback(async (draft: ParsedOrderDraft) => {
    try {
      const result = await approveDraft(draft.id, 'current-user-id');
      showSuccess('Order created successfully');
      // Navigate to the created order
      navigate(`/orders/${result.orderId}`);
    } catch (err) {
      showError('Failed to approve draft');
    }
  }, [navigate, showSuccess, showError]);

  // PATTERN: Reject draft with reason
  const handleReject = useCallback(async (draft: ParsedOrderDraft, reason: string) => {
    try {
      await rejectDraft(draft.id, reason);
      showSuccess('Draft rejected');
      loadSessions(); // Refresh list
    } catch (err) {
      showError('Failed to reject draft');
    }
  }, [showSuccess, showError]);

  // PATTERN: File upload handler
  const handleFileUpload = useCallback(async (files: FileList) => {
    const formData = new FormData();
    Array.from(files).forEach((file) => {
      formData.append('files', file);
    });

    try {
      const response = await fetch('/api/parser/upload', {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        throw new Error('Upload failed');
      }

      showSuccess('Files uploaded and parsed');
      setShowUploadModal(false);
      loadSessions(); // Refresh to show new sessions
    } catch (err) {
      showError('Failed to upload files');
    }
  }, [showSuccess, showError]);

  // Get all drafts from all sessions
  const allDrafts = sessions.flatMap((s) => s.drafts);

  if (departmentLoading) {
    return (
      <PageShell title="Parser Review">
        <LoadingSpinner message="Loading department..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Parser Review"
      subtitle={`${allDrafts.filter((d) => d.status === 'Pending').length} drafts pending review`}
      actions={
        <Button onClick={() => setShowUploadModal(true)}>
          <Upload className="h-4 w-4 mr-2" />
          Upload Files
        </Button>
      }
    >
      {/* Stats Cards */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        <Card className="p-4">
          <div className="text-2xl font-bold">{allDrafts.filter((d) => d.status === 'Pending').length}</div>
          <div className="text-sm text-muted-foreground">Pending Review</div>
        </Card>
        <Card className="p-4">
          <div className="text-2xl font-bold text-green-600">
            {allDrafts.filter((d) => d.status === 'Approved').length}
          </div>
          <div className="text-sm text-muted-foreground">Approved Today</div>
        </Card>
        <Card className="p-4">
          <div className="text-2xl font-bold text-red-600">
            {allDrafts.filter((d) => d.status === 'Rejected').length}
          </div>
          <div className="text-sm text-muted-foreground">Rejected</div>
        </Card>
      </div>

      {/* Drafts List */}
      <Card>
        {loading && (
          <div className="p-8">
            <LoadingSpinner message="Loading drafts..." />
          </div>
        )}

        {!loading && allDrafts.length === 0 && (
          <div className="p-8 text-center text-muted-foreground">
            No pending drafts to review
          </div>
        )}

        {!loading && allDrafts.length > 0 && (
          <div className="divide-y">
            {allDrafts
              .filter((d) => d.status === 'Pending')
              .map((draft) => (
                <div key={draft.id} className="p-4 hover:bg-muted/40">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      {/* Order Type Badge */}
                      <div className="flex items-center gap-2 mb-2">
                        <span className="px-2 py-1 rounded text-xs bg-blue-100 text-blue-800">
                          {draft.orderTypeHint}
                        </span>
                        {/* Confidence Score */}
                        <span 
                          className={`px-2 py-1 rounded text-xs ${
                            draft.confidenceScore >= 0.8 
                              ? 'bg-green-100 text-green-800' 
                              : draft.confidenceScore >= 0.5
                              ? 'bg-yellow-100 text-yellow-800'
                              : 'bg-red-100 text-red-800'
                          }`}
                        >
                          {Math.round(draft.confidenceScore * 100)}% confidence
                        </span>
                      </div>

                      {/* Customer Info */}
                      <div className="font-medium">
                        {draft.customerName || 'Unknown Customer'}
                      </div>
                      <div className="text-sm text-muted-foreground">
                        {draft.serviceId || draft.ticketId || 'No ID'}
                      </div>
                      <div className="text-sm text-muted-foreground">
                        {draft.serviceAddress}
                      </div>

                      {/* Validation Notes */}
                      {draft.validationNotes && (
                        <div className="mt-2 flex items-center gap-2 text-sm text-amber-600">
                          <AlertTriangle className="h-4 w-4" />
                          {draft.validationNotes}
                        </div>
                      )}

                      {/* Source File */}
                      <div className="mt-2 text-xs text-muted-foreground">
                        <FileText className="h-3 w-3 inline mr-1" />
                        {draft.sourceFileName}
                      </div>
                    </div>

                    {/* Actions */}
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => setSelectedDraft(draft)}
                      >
                        Review
                      </Button>
                      <Button
                        size="sm"
                        variant="default"
                        onClick={() => handleApprove(draft)}
                      >
                        <Check className="h-4 w-4 mr-1" />
                        Approve
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        onClick={() => handleReject(draft, 'Manual rejection')}
                      >
                        <X className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
          </div>
        )}
      </Card>

      {/* Upload Modal */}
      <FileUploadModal
        isOpen={showUploadModal}
        onClose={() => setShowUploadModal(false)}
        onUpload={handleFileUpload}
      />

      {/* Review Modal */}
      {selectedDraft && (
        <DraftReviewModal
          draft={selectedDraft}
          onClose={() => setSelectedDraft(null)}
          onApprove={() => handleApprove(selectedDraft)}
          onReject={(reason) => handleReject(selectedDraft, reason)}
        />
      )}
    </PageShell>
  );
};

// ==================== SUB-COMPONENTS ====================

interface FileUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  onUpload: (files: FileList) => void;
}

const FileUploadModal: React.FC<FileUploadModalProps> = ({ isOpen, onClose, onUpload }) => {
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = React.useRef<HTMLInputElement>(null);

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      onUpload(e.dataTransfer.files);
    }
  };

  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Upload Files for Parsing">
      <div
        className={`border-2 border-dashed rounded-xl p-8 text-center ${
          dragActive ? 'border-primary bg-primary/5' : 'border-border'
        }`}
        onDragEnter={() => setDragActive(true)}
        onDragLeave={() => setDragActive(false)}
        onDragOver={(e) => e.preventDefault()}
        onDrop={handleDrop}
      >
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept=".pdf,.xls,.xlsx,.msg"
          onChange={(e) => e.target.files && onUpload(e.target.files)}
          className="hidden"
        />
        <Upload className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
        <p className="text-sm font-medium">Drag & drop files here</p>
        <p className="text-xs text-muted-foreground mt-1">
          Supported: PDF, Excel (.xls, .xlsx), Outlook (.msg)
        </p>
        <Button
          variant="outline"
          className="mt-4"
          onClick={() => fileInputRef.current?.click()}
        >
          Browse Files
        </Button>
      </div>
    </Modal>
  );
};

interface DraftReviewModalProps {
  draft: ParsedOrderDraft;
  onClose: () => void;
  onApprove: () => void;
  onReject: (reason: string) => void;
}

const DraftReviewModal: React.FC<DraftReviewModalProps> = ({
  draft,
  onClose,
  onApprove,
  onReject,
}) => {
  const [rejectReason, setRejectReason] = useState('');

  return (
    <Modal isOpen={true} onClose={onClose} title="Review Parsed Order">
      <div className="space-y-4">
        {/* Order Type */}
        <div>
          <label className="text-xs font-medium text-muted-foreground">Order Type</label>
          <div className="font-medium">{draft.orderTypeHint}</div>
        </div>

        {/* Customer Info */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-xs font-medium text-muted-foreground">Customer Name</label>
            <div>{draft.customerName || '-'}</div>
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground">Phone</label>
            <div>{draft.customerPhone || '-'}</div>
          </div>
        </div>

        {/* Service ID */}
        <div>
          <label className="text-xs font-medium text-muted-foreground">Service ID</label>
          <div className="font-mono">{draft.serviceId || draft.ticketId || '-'}</div>
        </div>

        {/* Address */}
        <div>
          <label className="text-xs font-medium text-muted-foreground">Address</label>
          <div>{draft.serviceAddress || '-'}</div>
        </div>

        {/* Appointment */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-xs font-medium text-muted-foreground">Appointment Date</label>
            <div>{draft.appointmentDate || '-'}</div>
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground">Time Window</label>
            <div>{draft.appointmentWindow || '-'}</div>
          </div>
        </div>

        {/* Validation Notes */}
        {draft.validationNotes && (
          <div className="p-3 bg-amber-50 rounded-lg">
            <div className="flex items-center gap-2 text-amber-800">
              <AlertTriangle className="h-4 w-4" />
              <span className="text-sm font-medium">Validation Notes</span>
            </div>
            <p className="text-sm text-amber-700 mt-1">{draft.validationNotes}</p>
          </div>
        )}

        {/* Reject Reason */}
        <div>
          <label className="text-xs font-medium text-muted-foreground">Rejection Reason (if rejecting)</label>
          <textarea
            className="w-full border rounded-md p-2 text-sm mt-1"
            rows={2}
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            placeholder="Enter reason for rejection..."
          />
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-2 pt-4 border-t">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={() => onReject(rejectReason || 'Rejected during review')}
            disabled={!rejectReason}
          >
            Reject
          </Button>
          <Button onClick={onApprove}>
            <Check className="h-4 w-4 mr-2" />
            Approve & Create Order
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default ParserReviewPage;

