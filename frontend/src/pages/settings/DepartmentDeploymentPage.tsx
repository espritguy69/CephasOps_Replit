import React, { useState, useEffect } from 'react';
import { Upload, Download, FileSpreadsheet, CheckCircle, AlertCircle, Loader2, FileX, Building2 } from 'lucide-react';
import { Button, Card, useToast, SelectInput } from '../../components/ui';
import { PageShell } from '../../components/layout';
import {
  getDeploymentConfigurations,
  downloadDeploymentTemplate,
  validateDeployment,
  importDeployment,
  exportDepartmentData,
  type DepartmentDeploymentConfig,
  type DepartmentDeploymentValidationResult,
  type DepartmentDeploymentImportResult,
  type DepartmentDeploymentImportOptions,
} from '../../api/departmentDeployment';

type DeploymentStep = 'select' | 'upload' | 'validate' | 'options' | 'import' | 'results';

const DepartmentDeploymentPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [step, setStep] = useState<DeploymentStep>('select');
  const [configurations, setConfigurations] = useState<DepartmentDeploymentConfig[]>([]);
  const [selectedDepartment, setSelectedDepartment] = useState<string>('');
  const [selectedConfig, setSelectedConfig] = useState<DepartmentDeploymentConfig | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [validationResult, setValidationResult] = useState<DepartmentDeploymentValidationResult | null>(null);
  const [importResult, setImportResult] = useState<DepartmentDeploymentImportResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [importOptions, setImportOptions] = useState<DepartmentDeploymentImportOptions>({
    departmentCode: '',
    duplicateHandling: 'Skip',
    createMissingDependencies: false,
    createDepartmentIfNotExists: true,
    dataTypesToImport: [],
  });

  useEffect(() => {
    loadConfigurations();
  }, []);

  useEffect(() => {
    if (selectedDepartment && configurations.length > 0) {
      const config = configurations.find((c) => c.departmentCode.toUpperCase() === selectedDepartment.toUpperCase());
      setSelectedConfig(config || null);
      setImportOptions((prev) => ({ ...prev, departmentCode: selectedDepartment }));
    }
  }, [selectedDepartment, configurations]);

  const loadConfigurations = async (): Promise<void> => {
    try {
      setLoading(true);
      const configs = await getDeploymentConfigurations();
      setConfigurations(configs);
      if (configs.length > 0 && !selectedDepartment) {
        setSelectedDepartment(configs[0].departmentCode);
      }
    } catch (error: any) {
      showError(error.message || 'Failed to load department configurations');
    } finally {
      setLoading(false);
    }
  };

  const handleDownloadTemplate = async (): Promise<void> => {
    if (!selectedDepartment) {
      showError('Please select a department first');
      return;
    }

    try {
      setLoading(true);
      await downloadDeploymentTemplate(selectedDepartment);
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

    if (!selectedDepartment) {
      showError('Please select a department first');
      return;
    }

    try {
      setLoading(true);
      const result = await validateDeployment(files, selectedDepartment);
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

    if (!selectedDepartment) {
      showError('Please select a department first');
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
    if (!selectedDepartment) {
      showError('Please select a department first');
      return;
    }

    try {
      setLoading(true);
      await exportDepartmentData(selectedDepartment);
      showSuccess('Department data exported successfully');
    } catch (error: any) {
      showError(error.message || 'Export failed');
    } finally {
      setLoading(false);
    }
  };

  const renderDepartmentSelection = (): React.ReactNode => (
    <Card className="p-6">
      <h2 className="text-xl font-bold mb-4">Select Department</h2>
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-2">Department</label>
          <select
            value={selectedDepartment}
            onChange={(e) => setSelectedDepartment(e.target.value)}
            className="w-full p-2 border rounded"
          >
            <option value="">-- Select Department --</option>
            {configurations.map((config) => (
              <option key={config.departmentCode} value={config.departmentCode}>
                {config.departmentName} ({config.departmentCode})
              </option>
            ))}
          </select>
        </div>

        {selectedConfig && (
          <div className="p-4 bg-muted rounded-lg">
            <h3 className="font-semibold mb-2">{selectedConfig.departmentName}</h3>
            <div className="space-y-2">
              <div>
                <p className="text-sm font-medium">Required Data Types:</p>
                <ul className="text-sm text-muted-foreground list-disc list-inside">
                  {selectedConfig.requiredDataTypes.map((type) => (
                    <li key={type}>
                      {type} - {selectedConfig.dataTypeDescriptions[type] || ''}
                    </li>
                  ))}
                </ul>
              </div>
              {selectedConfig.optionalDataTypes.length > 0 && (
                <div>
                  <p className="text-sm font-medium">Optional Data Types:</p>
                  <ul className="text-sm text-muted-foreground list-disc list-inside">
                    {selectedConfig.optionalDataTypes.map((type) => (
                      <li key={type}>
                        {type} - {selectedConfig.dataTypeDescriptions[type] || ''}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          </div>
        )}

        <div className="flex gap-2 pt-4">
          <Button onClick={handleDownloadTemplate} disabled={loading || !selectedDepartment}>
            {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
            Download Template
          </Button>
          <Button variant="outline" onClick={handleExport} disabled={loading || !selectedDepartment}>
            Export Existing Data
          </Button>
          <Button
            onClick={() => setStep('upload')}
            disabled={!selectedDepartment}
            className="ml-auto"
          >
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
        {selectedConfig && (
          <div className="p-3 bg-blue-50 dark:bg-blue-950/30 rounded-lg">
            <p className="text-sm">
              <strong>Deploying:</strong> {selectedConfig.departmentName} ({selectedConfig.departmentCode})
            </p>
          </div>
        )}

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
            <span className="text-sm font-medium">Click to select Excel file</span>
            <span className="text-xs text-muted-foreground">
              Select the deployment Excel file with multiple sheets
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
          <Button variant="outline" onClick={() => setStep('select')}>
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

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="createDept"
            checked={importOptions.createDepartmentIfNotExists}
            onChange={(e) =>
              setImportOptions({
                ...importOptions,
                createDepartmentIfNotExists: e.target.checked,
              })
            }
          />
          <label htmlFor="createDept" className="text-sm">
            Create department if it doesn't exist
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
                {importResult.departmentId && (
                  <p className="text-sm text-green-600 dark:text-green-300">
                    Department ID: {importResult.departmentId}
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
            <Button
              onClick={() => {
                setStep('select');
                setFiles([]);
                setValidationResult(null);
                setImportResult(null);
              }}
            >
              Start New Deployment
            </Button>
          </div>
        </div>
      </Card>
    );
  };

  return (
    <PageShell
      title="Department Deployment"
      subtitle="Deploy new department (GPON, CWO, NWO) or update existing department data using Excel import/export"
    >
      <div className="space-y-6">
        {step === 'select' && renderDepartmentSelection()}
        {step === 'upload' && renderUpload()}
        {step === 'validate' && renderValidationResults()}
        {step === 'options' && renderImportOptions()}
        {step === 'import' && <div>Importing...</div>}
        {step === 'results' && renderImportResults()}
      </div>
    </PageShell>
  );
};

export default DepartmentDeploymentPage;

