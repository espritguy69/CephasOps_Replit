import React, { useRef, useState } from 'react';
import { Download, Upload, FileSpreadsheet, Loader2, CheckCircle, AlertCircle } from 'lucide-react';
import Button from './Button';
import Modal from './Modal';
import { cn } from '@/lib/utils';

interface ImportResult {
  success: boolean;
  successCount?: number;
  totalRows?: number;
  error?: string;
  errors?: Array<{ rowNumber: number; message: string }>;
  errorCount?: number;
}

interface ImportExportButtonsProps {
  entityName?: string;
  onExport?: () => Promise<void>;
  onImport?: (file: File) => Promise<ImportResult>;
  onDownloadTemplate?: () => Promise<void>;
  disabled?: boolean;
  className?: string;
}

/**
 * Reusable Import/Export buttons component for CSV data management
 */
export const ImportExportButtons: React.FC<ImportExportButtonsProps> = ({
  entityName = 'Data',
  onExport,
  onImport,
  onDownloadTemplate,
  disabled = false,
  className = ''
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [importing, setImporting] = useState<boolean>(false);
  const [exporting, setExporting] = useState<boolean>(false);
  const [showImportModal, setShowImportModal] = useState<boolean>(false);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const handleExport = async (): Promise<void> => {
    if (!onExport) return;
    setExporting(true);
    try {
      await onExport();
    } catch (err) {
      console.error('Export error:', err);
    } finally {
      setExporting(false);
    }
  };

  const handleTemplateDownload = async (): Promise<void> => {
    if (!onDownloadTemplate) return;
    try {
      await onDownloadTemplate();
    } catch (err) {
      console.error('Template download error:', err);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>): void => {
    const file = e.target.files?.[0];
    if (file) {
      setSelectedFile(file);
      setImportResult(null);
    }
  };

  const handleImport = async (): Promise<void> => {
    if (!onImport || !selectedFile) return;
    setImporting(true);
    try {
      const result = await onImport(selectedFile);
      setImportResult(result);
    } catch (err: any) {
      console.error('Import error:', err);
      setImportResult({
        success: false,
        error: err.message || 'Import failed'
      });
    } finally {
      setImporting(false);
    }
  };

  const closeImportModal = (): void => {
    setShowImportModal(false);
    setSelectedFile(null);
    setImportResult(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <>
      <div className={cn("flex items-center gap-2", className)}>
        {/* Export Button */}
        {onExport && (
          <Button
            variant="outline"
            size="sm"
            onClick={handleExport}
            disabled={disabled || exporting}
            className="gap-1.5"
            title={`Export ${entityName} to CSV`}
          >
            {exporting ? (
              <Loader2 className="h-3 w-3 animate-spin" />
            ) : (
              <Download className="h-3 w-3" />
            )}
            <span className="hidden md:inline">Export</span>
          </Button>
        )}

        {/* Import Button */}
        {onImport && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowImportModal(true)}
            disabled={disabled}
            className="gap-1.5"
            title={`Import ${entityName} from CSV`}
          >
            <Upload className="h-3 w-3" />
            <span className="hidden md:inline">Import</span>
          </Button>
        )}

        {/* Template Button */}
        {onDownloadTemplate && (
          <Button
            variant="outline"
            size="sm"
            onClick={handleTemplateDownload}
            disabled={disabled}
            className="gap-1.5"
            title="Download CSV template"
          >
            <FileSpreadsheet className="h-3 w-3" />
            <span className="hidden md:inline">Template</span>
          </Button>
        )}
      </div>

      {/* Import Modal */}
      <Modal isOpen={showImportModal} onClose={closeImportModal}>
        <div className="bg-card rounded-lg shadow-xl max-w-md w-full p-4">
          <h2 className="text-sm font-bold mb-4">Import {entityName}</h2>

          {!importResult ? (
            <>
              <div className="space-y-4">
                {/* File Upload Area */}
                <div
                  className={cn(
                    "border-2 border-dashed rounded-lg p-6 text-center transition-colors",
                    selectedFile
                      ? "border-primary bg-primary/5"
                      : "border-border hover:border-muted-foreground"
                  )}
                >
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept=".csv"
                    onChange={handleFileSelect}
                    className="hidden"
                  />
                  
                  {selectedFile ? (
                    <div className="space-y-2">
                      <FileSpreadsheet className="h-8 w-8 mx-auto text-primary" />
                      <p className="text-sm font-medium">{selectedFile.name}</p>
                      <p className="text-xs text-muted-foreground">
                        {(selectedFile.size / 1024).toFixed(1)} KB
                      </p>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => fileInputRef.current?.click()}
                      >
                        Change file
                      </Button>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      <Upload className="h-8 w-8 mx-auto text-muted-foreground" />
                      <p className="text-sm text-muted-foreground">
                        Click to select a CSV file
                      </p>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => fileInputRef.current?.click()}
                      >
                        Browse Files
                      </Button>
                    </div>
                  )}
                </div>

                {/* Download Template Link */}
                {onDownloadTemplate && (
                  <p className="text-xs text-center text-muted-foreground">
                    Need a template?{' '}
                    <button
                      onClick={handleTemplateDownload}
                      className="text-primary hover:underline"
                    >
                      Download CSV template
                    </button>
                  </p>
                )}
              </div>

              <div className="flex justify-end gap-2 mt-4 pt-4 border-t">
                <Button variant="outline" size="sm" onClick={closeImportModal}>
                  Cancel
                </Button>
                <Button
                  size="sm"
                  onClick={handleImport}
                  disabled={!selectedFile || importing}
                  className="gap-1.5"
                >
                  {importing ? (
                    <>
                      <Loader2 className="h-3 w-3 animate-spin" />
                      Importing...
                    </>
                  ) : (
                    <>
                      <Upload className="h-3 w-3" />
                      Import
                    </>
                  )}
                </Button>
              </div>
            </>
          ) : (
            <>
              {/* Import Results */}
              <div className="space-y-4">
                {importResult.error ? (
                  <div className="flex items-start gap-3 p-4 bg-red-50 dark:bg-red-950/30 rounded-lg">
                    <AlertCircle className="h-5 w-5 text-red-500 flex-shrink-0" />
                    <div>
                      <p className="text-sm font-medium text-red-800 dark:text-red-200">
                        Import Failed
                      </p>
                      <p className="text-xs text-red-600 dark:text-red-300 mt-1">
                        {importResult.error}
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-start gap-3 p-4 bg-green-50 dark:bg-green-950/30 rounded-lg">
                    <CheckCircle className="h-5 w-5 text-green-500 flex-shrink-0" />
                    <div>
                      <p className="text-sm font-medium text-green-800 dark:text-green-200">
                        Import Successful
                      </p>
                      <p className="text-xs text-green-600 dark:text-green-300 mt-1">
                        {importResult.successCount || 0} of {importResult.totalRows || 0} records imported
                      </p>
                    </div>
                  </div>
                )}

                {/* Error Details */}
                {importResult.errors && importResult.errors.length > 0 && (
                  <div className="max-h-40 overflow-y-auto">
                    <p className="text-xs font-medium mb-2 text-muted-foreground">
                      Errors ({importResult.errorCount}):
                    </p>
                    <div className="space-y-1">
                      {importResult.errors.slice(0, 10).map((err, idx) => (
                        <div key={idx} className="text-xs bg-muted p-2 rounded">
                          <span className="font-medium">Row {err.rowNumber}:</span>{' '}
                          {err.message}
                        </div>
                      ))}
                      {importResult.errors.length > 10 && (
                        <p className="text-xs text-muted-foreground">
                          ... and {importResult.errors.length - 10} more errors
                        </p>
                      )}
                    </div>
                  </div>
                )}
              </div>

              <div className="flex justify-end mt-4 pt-4 border-t">
                <Button size="sm" onClick={closeImportModal}>
                  Done
                </Button>
              </div>
            </>
          )}
        </div>
      </Modal>
    </>
  );
};

export default ImportExportButtons;

