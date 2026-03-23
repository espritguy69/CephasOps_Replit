import React, { useState, useEffect, ChangeEvent } from 'react';
import { Upload, Download, Trash2, File } from 'lucide-react';
import { uploadFile, getFileMetadata, deleteFile, getFiles } from '../../api/files';
import { getApiBaseUrl } from '../../api/config';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, EmptyState, useToast, Button, Card, DataTable } from '../../components/ui';
import { formatLocalDateTime } from '../../utils/dateUtils';

interface FileItem {
  id: string;
  fileName: string;
  category?: string;
  fileSize?: number;
  uploadedAt?: string;
}

const FilesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [files, setFiles] = useState<FileItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [uploading, setUploading] = useState<boolean>(false);

  useEffect(() => {
    loadFiles();
  }, []);

  const loadFiles = async (): Promise<void> => {
    try {
      setLoading(true);
      const fileList = await getFiles();
      // Map FileMetadata to FileItem format
      const mappedFiles: FileItem[] = fileList.map(file => ({
        id: file.id,
        fileName: file.fileName,
        category: file.module || undefined,
        fileSize: file.sizeBytes,
        uploadedAt: file.createdAt
      }));
      setFiles(mappedFiles);
    } catch (err: any) {
      showError(err.message || 'Failed to load files');
      console.error('Error loading files:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleUpload = async (event: ChangeEvent<HTMLInputElement>): Promise<void> => {
    const file = event.target.files?.[0];
    if (!file) return;

    try {
      setUploading(true);
      await uploadFile(null, file, { module: 'general' });
      showSuccess('File uploaded successfully!');
      loadFiles();
    } catch (err: any) {
      showError(err.message || 'Failed to upload file');
    } finally {
      setUploading(false);
    }
  };

  const handleDownload = async (fileId: string): Promise<void> => {
    try {
      const fileUrl = `${getApiBaseUrl()}/files/${fileId}`;
      window.open(fileUrl, '_blank');
    } catch (err: any) {
      showError(err.message || 'Failed to download file');
    }
  };

  const handleDelete = async (fileId: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this file?')) return;
    
    try {
      await deleteFile(fileId);
      showSuccess('File deleted successfully!');
      loadFiles();
    } catch (err: any) {
      showError(err.message || 'Failed to delete file');
    }
  };

  const columns = [
    { key: 'fileName', label: 'File Name' },
    { key: 'category', label: 'Category' },
    { key: 'fileSize', label: 'Size', render: (value: unknown) => value ? `${((value as number) / 1024).toFixed(2)} KB` : '-' },
    { key: 'uploadedAt', label: 'Uploaded', render: (value: unknown) => value ? formatLocalDateTime(value as string) : '-' }
  ];

  if (loading) {
    return (
      <PageShell title="Files" breadcrumbs={[{ label: 'Files' }]}>
        <LoadingSpinner message="Loading files..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Files"
      breadcrumbs={[{ label: 'Files' }]}
      actions={
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="file"
            onChange={handleUpload}
            disabled={uploading}
            className="hidden"
          />
          <Button disabled={uploading} className="flex items-center gap-2">
            <Upload className="h-4 w-4" />
            {uploading ? 'Uploading...' : 'Upload File'}
          </Button>
        </label>
      }
    >
      <div className="flex-1 p-3 max-w-7xl mx-auto">
      <Card>
        {files.length > 0 ? (
          <DataTable
            data={files}
            columns={columns}
            onRowClick={(row) => handleDownload(row.id)}
          />
        ) : (
          <EmptyState
            title="No files found"
            message="Upload files to get started."
          />
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default FilesPage;

