import React, { useState } from 'react';
import { Upload, Download, FileSpreadsheet, CheckCircle, AlertCircle, Loader2, FileX } from 'lucide-react';
import { Button, Card, useToast } from '../../components/ui';
import { PageShell } from '../../components/layout';
import {
  downloadDeploymentTemplate,
  validateDeployment,
  importDeployment,
  exportCompany,
  type DeploymentValidationResult,
  type DeploymentImportResult,
  type DeploymentImportOptions,
} from '../../api/companyDeployment';

type DeploymentStep = 'format' | 'upload' | 'validate' | 'options' | 'import' | 'results';

const CompanyDeploymentPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [step, setStep] = useState<DeploymentStep>('format');
  const [format, setFormat] = useState<'single' | 'separate'>('single');
  const [files, setFiles] = useState<File[]>([]);
  const [validationResult, setValidationResult] = useState<DeploymentValidationResult | null>(null);
  const [importResult, setImportResult] = useState<DeploymentImportResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [importOptions, setImportOptions] = useState<DeploymentImportOptions>({
    duplicateHandling: 'Skip',
    skipSplittersIfNotGpon: true,
    createMissingDependencies: false,
    dataTypesToImport: [],
  });

  const handleDownloadTemplate = async (): Promise<void> => {
    try {
      setLoading(true);
      await downloadDeploymentTemplate(format);
      showSuccess('Template downloaded successfully');
    } catch (error: any) {
      showError(error.message || 'Failed to download template');
    } finally {
      setLoading(false);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>): void => {
    const selectedFiles = Array.from(e.target.files || []);
    setFiles(selectedFiles);
    setValidationResult(null);
    setImportResult(null);
  };

  const handleValidate = async (): Promise<void> => {
    if (files.length === 0) {
      showError('Please select files to validate');
      return;
    }

    try {
      setLoading(true);
      const result = await validateDeployment(files);
      setValidationResult(result);
      if (result.isValid) {
        showSuccess('Validation passed! Ready to import.');
        setStep('options');
      } else {
        showError(`Validation found ${result.errors.length} errors`);
      }
    } catch (error: any) {
      showError(error.message || 'Validation failed');
    } finally {
      setLoading(false);
    }
  };

  const handleImport = async (): Promise<void> => {
    if (files.length === 0) {
      showError('Please select files to import');
      return;
    }

    try {
      setLoading(true);
      const result = await importDeployment(files, importOptions);
      setImportResult(result);
      if (result.success) {
        showSuccess(`Import completed: ${result.successCount} records imported`);
        setStep('results');
      } else {
        showError(`Import completed with ${result.errorCount} errors`);
        setStep('results');
      }
    } catch (error: any) {
      showError(error.message || 'Import failed');
    } finally {
      setLoading(false);
    }
  };

  const handleExport = async (): Promise<void> => {
    try {
      setLoading(true);
      await exportCompany(undefined, format);
      showSuccess('Company data exported successfully');
    } catch (error: any) {
      showError(error.message || 'Export failed');
    } finally {
      setLoading(false);
    }
  };

  const renderFormatSelection = (): React.ReactNode => (
    <Card className="p-6">
      <h2 className="text-xl font-bold mb-4">Choose Deployment Format</h2>
      <div className="space-y-4">
        <div
          className={`p-4 border-2 rounded-lg cursor-pointer transition-colors ${
            format === 'single'
              ? 'border-primary bg-primary/5'
              : 'border-border hover:border-muted-foreground'
          }`}
          onClick={() => setFormat('single')}
        >
          <div className="flex items-start gap-3">
            <FileSpreadsheet className="h-6 w-6 text-primary mt-1" />
            <div>
              <h3 className="font-semibold">Single Master File</h3>
              <p className="text-sm text-muted-foreground">
                One Excel file with multiple sheets (Company Info, Departments, Partners, etc.)
              </p>
            </div>
          </div>
        </div>

        <div
          className={`p-4 border-2 rounded-lg cursor-pointer transition-colors ${
            format === 'separate'
              ? 'border-primary bg-primary/5'
              : 'border-border hover:border-muted-foreground'
          }`}
          onClick={() => setFormat('separate')}
        >
          <div className="flex items-start gap-3">
            <FileSpreadsheet className="h-6 w-6 text-primary mt-1" />
            <div>
              <h3 className="font-semibold">Separate Files</h3>
              <p className="text-sm text-muted-foreground">
                Multiple Excel files, one per data type (more flexible for incremental imports)
              </p>
            </div>
          </div>
        </div>

        <div className="flex gap-2 pt-4">
          <Button onClick={handleDownloadTemplate} disabled={loading}>
            {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Download Template
          </Button>
          <Button variant="outline" onClick={handleExport} disabled={loading}>
            Export Existing Company
          </Button>
          <Button onClick={() => setStep('upload')} className="ml-auto">
            Next: Upload Files
          </Button>
        </div>
      </div>
    </Card>
  );

  const renderUpload = (): React.ReactNode => (
    <Card className="p-6">
      <h2 className="text-xl font-bold mb-4">Upload Files</h2>
      <div className="space-y-4">
        <div className="border-2 border-dashed rounded-lg p-8 text-center">
          <input
            type="file"
            multiple
            accept=".xlsx,.xls"
            onChange={handleFileSelect}
            className="hidden"
            id="file-upload"
          />
          <label
            htmlFor="file-upload"
            className="cursor-pointer flex flex-col items-center gap-2"
          >
            <Upload className="h-12 w-12 text-muted-foreground" />
            <span className="text-sm font-medium">Click to select files</span>
            <span className="text-xs text-muted-foreground">
              {format === 'single' ? 'Select one Excel file' : 'Select multiple Excel files'}
            </span>
          </label>
        </div>

        {files.length > 0 && (
          <div className="space-y-2">
            <h3 className="font-semibold">Selected Files:</h3>
            {files.map((file, idx) => (
              <div key={idx} className="flex items-center gap-2 p-2 bg-muted rounded">
                <FileSpreadsheet className="h-4 w-4" />
                <span className="text-sm flex-1">{file.name}</span>
                <span className="text-xs text-muted-foreground">
                  {(file.size / 1024).toFixed(1)} KB
                </span>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setFiles(files.filter((_, i) => i !== idx))}
                >
                  <FileX className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        )}

        <div className="flex gap-2 pt-4">
          <Button variant="outline" onClick={() => setStep('format')}>
            Back
          </Button>
          <Button
            onClick={handleValidate}
            disabled={files.length === 0 || loading}
            className="ml-auto"
          >
            {loading ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                Validating...
              </>
            ) : (
              <>
                <CheckCircle className="h-4 w-4 mr-2" />
                Validate Files
              </>
            )}
          </Button>
        </div>
      </div>
    </Card>
  );

  const renderValidationResults = (): React.ReactNode => {
    if (!validationResult) return null;

    return (
      <Card className="p-6">
        <h2 className="text-xl font-bold mb-4">Validation Results</h2>
        <div className="space-y-4">
          {validationResult.isValid ? (
            <div className="flex items-start gap-3 p-4 bg-green-50 dark:bg-green-950/30 rounded-lg">
              <CheckCircle className="h-5 w-5 text-green-500 flex-shrink-0" />
              <div>
                <p className="font-semibold text-green-800 dark:text-green-200">Validation Passed</p>
                <p className="text-sm text-green-600 dark:text-green-300">
                  Total records: {validationResult.totalRecords}
                </p>
              </div>
            </div>
          ) : (
            <div className="flex items-start gap-3 p-4 bg-red-50 dark:bg-red-950/30 rounded-lg">
              <AlertCircle className="h-5 w-5 text-red-500 flex-shrink-0" />
              <div>
                <p className="font-semibold text-red-800 dark:text-red-200">Validation Failed</p>
                <p className="text-sm text-red-600 dark:text-red-300">
                  Found {validationResult.errors.length} errors
                </p>
              </div>
            </div>
          )}

          {validationResult.errors.length > 0 && (
            <div className="max-h-60 overflow-y-auto">
              <h3 className="font-semibold mb-2">Errors:</h3>
              {validationResult.errors.slice(0, 20).map((error, idx) => (
                <div key={idx} className="text-sm bg-muted p-2 rounded mb-1">
                  <span className="font-medium">
                    {error.sheetName || error.dataType} Row {error.rowNumber}:
                  </span>{' '}
                  {error.message}
                </div>
              ))}
              {validationResult.errors.length > 20 && (
                <p className="text-xs text-muted-foreground">
                  ... and {validationResult.errors.length - 20} more errors
                </p>
              )}
            </div>
          )}

          <div className="flex gap-2 pt-4">
            <Button variant="outline" onClick={() => setStep('upload')}>
              Back
            </Button>
            {validationResult.isValid && (
              <Button onClick={() => setStep('options')} className="ml-auto">
                Next: Configure Options
              </Button>
            )}
          </div>
        </div>
      </Card>
    );
  };

  const renderImportOptions = (): React.ReactNode => (
    <Card className="p-6">
      <h2 className="text-xl font-bold mb-4">Import Options</h2>
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-2">Duplicate Handling</label>
          <select
            value={importOptions.duplicateHandling}
            onChange={(e) =>
              setImportOptions({
                ...importOptions,
                duplicateHandling: e.target.value as 'Skip' | 'Update' | 'CreateNew',
              })
            }
            className="w-full p-2 border rounded"
          >
            <option value="Skip">Skip duplicates</option>
            <option value="Update">Update existing</option>
            <option value="CreateNew">Create new (force)</option>
          </select>
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="skipSplitters"
            checked={importOptions.skipSplittersIfNotGpon}
            onChange={(e) =>
              setImportOptions({
                ...importOptions,
                skipSplittersIfNotGpon: e.target.checked,
              })
            }
          />
          <label htmlFor="skipSplitters" className="text-sm">
            Skip Splitters if not GPON company
          </label>
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="createDeps"
            checked={importOptions.createMissingDependencies}
            onChange={(e) =>
              setImportOptions({
                ...importOptions,
                createMissingDependencies: e.target.checked,
              })
            }
          />
          <label htmlFor="createDeps" className="text-sm">
            Create missing dependencies automatically
          </label>
        </div>

        <div className="flex gap-2 pt-4">
          <Button variant="outline" onClick={() => setStep('validate')}>
            Back
          </Button>
          <Button onClick={handleImport} disabled={loading} className="ml-auto">
            {loading ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin mr-2" />
                Importing...
              </>
            ) : (
              <>
                <Upload className="h-4 w-4 mr-2" />
                Import Now
              </>
            )}
          </Button>
        </div>
      </div>
    </Card>
  );

  const renderImportResults = (): React.ReactNode => {
    if (!importResult) return null;

    return (
      <Card className="p-6">
        <h2 className="text-xl font-bold mb-4">Import Results</h2>
        <div className="space-y-4">
          {importResult.success ? (
            <div className="flex items-start gap-3 p-4 bg-green-50 dark:bg-green-950/30 rounded-lg">
              <CheckCircle className="h-5 w-5 text-green-500 flex-shrink-0" />
              <div>
                <p className="font-semibold text-green-800 dark:text-green-200">Import Successful</p>
                <p className="text-sm text-green-600 dark:text-green-300">
                  {importResult.successCount} of {importResult.totalRecords} records imported
                </p>
                {importResult.companyName && (
                  <p className="text-sm text-green-600 dark:text-green-300">
                    Company: {importResult.companyName}
                  </p>
                )}
              </div>
            </div>
          ) : (
            <div className="flex items-start gap-3 p-4 bg-yellow-50 dark:bg-yellow-950/30 rounded-lg">
              <AlertCircle className="h-5 w-5 text-yellow-500 flex-shrink-0" />
              <div>
                <p className="font-semibold text-yellow-800 dark:text-yellow-200">
                  Import Completed with Errors
                </p>
                <p className="text-sm text-yellow-600 dark:text-yellow-300">
                  {importResult.successCount} succeeded, {importResult.errorCount} failed
                </p>
              </div>
            </div>
          )}

          {importResult.errors.length > 0 && (
            <div className="max-h-60 overflow-y-auto">
              <h3 className="font-semibold mb-2">Errors:</h3>
              {importResult.errors.slice(0, 20).map((error, idx) => (
                <div key={idx} className="text-sm bg-muted p-2 rounded mb-1">
                  <span className="font-medium">
                    {error.sheetName || error.dataType} Row {error.rowNumber}:
                  </span>{' '}
                  {error.message}
                </div>
              ))}
            </div>
          )}

          <div className="flex gap-2 pt-4">
            <Button onClick={() => {
              setStep('format');
              setFiles([]);
              setValidationResult(null);
              setImportResult(null);
            }}>
              Start New Deployment
            </Button>
          </div>
        </div>
      </Card>
    );
  };

  return (
    <PageShell
      title="Company Deployment"
      subtitle="Deploy new company or update existing company data using Excel import/export"
    >
      <div className="space-y-6">
        {step === 'format' && renderFormatSelection()}
        {step === 'upload' && renderUpload()}
        {step === 'validate' && renderValidationResults()}
        {step === 'options' && renderImportOptions()}
        {step === 'import' && <div>Importing...</div>}
        {step === 'results' && renderImportResults()}
      </div>
    </PageShell>
  );
};

export default CompanyDeploymentPage;

