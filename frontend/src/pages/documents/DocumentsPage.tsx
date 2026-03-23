import React, { useState, useEffect } from 'react';
import { FileText, Download, Eye } from 'lucide-react';
import { getGeneratedDocuments, getGeneratedDocument } from '../../api/documents';
import { LoadingSpinner, EmptyState, useToast, Card, DataTable, Button } from '../../components/ui';
import { PageShell } from '../../components/layout';

interface Document {
  id: string;
  documentType: string;
  referenceEntity: string;
  referenceId: string;
  generatedAt?: string;
  status: string;
  fileUrl?: string;
}

const DocumentsPage: React.FC = () => {
  const { showError } = useToast();
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [filters, setFilters] = useState<Record<string, any>>({});

  useEffect(() => {
    loadDocuments();
  }, [filters]);

  const loadDocuments = async (): Promise<void> => {
    try {
      setLoading(true);
      const data = await getGeneratedDocuments(filters);
      setDocuments(Array.isArray(data) ? data : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load documents');
      console.error('Error loading documents:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleDownload = async (documentId: string): Promise<void> => {
    try {
      const document = await getGeneratedDocument(documentId);
      if (document.fileUrl) {
        window.open(document.fileUrl, '_blank');
      } else {
        showError('Document file not available');
      }
    } catch (err: any) {
      showError(err.message || 'Failed to download document');
    }
  };

  const columns = [
    { key: 'documentType', label: 'Type' },
    { key: 'referenceEntity', label: 'Entity' },
    { key: 'referenceId', label: 'Reference ID' },
    { key: 'generatedAt', label: 'Generated', render: (value: unknown) => value ? new Date(value as string).toLocaleString() : '-' },
    { key: 'status', label: 'Status' }
  ];

  if (loading) {
    return (
      <PageShell title="Generated Documents" breadcrumbs={[{ label: 'Documents' }]}>
        <LoadingSpinner message="Loading documents..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell title="Generated Documents" breadcrumbs={[{ label: 'Documents' }]}>
      <div className="max-w-7xl mx-auto">
      <Card>
        {documents.length > 0 ? (
          <DataTable
            data={documents}
            columns={columns}
            onRowClick={(row) => handleDownload(row.id)}
          />
        ) : (
          <EmptyState
            title="No documents found"
            message="Generated documents will appear here."
          />
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default DocumentsPage;

