import React, { useState, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, Play, Download, ChevronLeft, ChevronRight } from 'lucide-react';
import { Card, Button, DataTable, Skeleton, useToast, Select } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '@/lib/utils';
import { designTokens } from '@/lib/design-tokens';
import { getReportDefinition, runReport, isForbiddenError, exportOrdersListReport, exportMaterialsReport, exportStockSummaryReport, exportLedgerReport, exportSchedulerUtilizationReport, type ExportFormat } from '../../api/reports';
import { getStockLocations } from '../../api/inventory';
import { getMaterials } from '../../api/inventory';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { useDepartment } from '../../contexts/DepartmentContext';
import { useAuth } from '../../contexts/AuthContext';
import type { RunReportRequestDto, RunReportResultDto } from '../../types/reports';

const ReportRunnerPage: React.FC = () => {
  const { reportKey } = useParams<{ reportKey: string }>();
  const navigate = useNavigate();
  const { showError } = useToast();
  const { user } = useAuth();
  const { departmentId, departments } = useDepartment();
  const roles = user?.roles ?? [];
  const permissions = user?.permissions ?? [];
  const canExport = Boolean(
    roles.includes('SuperAdmin') ||
    permissions.includes('reports.export') ||
    (permissions.length === 0 && roles.includes('Admin'))
  );
  const [filterValues, setFilterValues] = useState<Record<string, string | boolean | number>>({});
  const [result, setResult] = useState<RunReportResultDto | null>(null);
  const [running, setRunning] = useState(false);
  const [accessDenied, setAccessDenied] = useState(false);
  const [exportFormat, setExportFormat] = useState<ExportFormat>('csv');

  const { data: definition, isLoading: defLoading, error: defError } = useQuery({
    queryKey: ['reports', 'definition', reportKey],
    queryFn: () => getReportDefinition(reportKey!),
    enabled: !!reportKey
  });

  const hasLocationIdParam = definition?.parameterSchema.some((p) => p.name === 'locationId') ?? false;
  const hasMaterialIdParam = definition?.parameterSchema.some((p) => p.name === 'materialId') ?? false;
  const hasSiParam = definition?.parameterSchema.some((p) => p.name === 'siId' || p.name === 'assignedSiId') ?? false;

  const { data: locations = [] } = useQuery({
    queryKey: ['inventory', 'locations'],
    queryFn: () => getStockLocations(),
    enabled: hasLocationIdParam
  });

  const { data: materials = [] } = useQuery({
    queryKey: ['inventory', 'materials', departmentId],
    queryFn: () => getMaterials({ isActive: true }),
    enabled: hasMaterialIdParam && !!departmentId
  });

  const { data: serviceInstallers = [] } = useQuery({
    queryKey: ['serviceInstallers', departmentId],
    queryFn: () => getServiceInstallers({ isActive: true, ...(departmentId ? { departmentId } : {}) }),
    enabled: hasSiParam && !!departmentId
  });

  const initialisedFilters = useMemo(() => {
    if (!definition) return {};
    const init: Record<string, string | boolean | number> = {};
    definition.parameterSchema.forEach((p) => {
      if (p.name === 'departmentId' && departmentId) init[p.name] = departmentId;
      else if (p.type === 'bool') init[p.name] = false;
      else if (p.type === 'int') init[p.name] = p.name === 'page' ? 1 : p.name === 'pageSize' ? 50 : 0;
      else init[p.name] = '';
    });
    return init;
  }, [definition, departmentId]);

  const currentFilters = useMemo(() => {
    return { ...initialisedFilters, ...filterValues };
  }, [initialisedFilters, filterValues]);

  const updateFilter = (name: string, value: string | boolean | number) => {
    setFilterValues((prev) => ({ ...prev, [name]: value }));
  };

  const buildRequest = (overrides?: Partial<RunReportRequestDto>): RunReportRequestDto => {
    const request: RunReportRequestDto = {};
    Object.entries(currentFilters).forEach(([k, v]) => {
      if (v === '' || v === undefined) return;
      if (typeof v === 'boolean') request[k as keyof RunReportRequestDto] = v;
      else if (typeof v === 'number') request[k as keyof RunReportRequestDto] = v;
      else request[k as keyof RunReportRequestDto] = String(v).trim() || undefined;
    });
    if (departmentId && !request.departmentId) request.departmentId = departmentId;
    return { ...request, ...overrides };
  };

  const handleRun = async (overrides?: Partial<RunReportRequestDto>) => {
    if (!reportKey) return;
    setRunning(true);
    setAccessDenied(false);
    setResult(null);
    const request = buildRequest(overrides);
    try {
      const data = await runReport(reportKey, request);
      setResult(data);
      if (overrides?.page != null) setFilterValues((prev) => ({ ...prev, page: data.page ?? overrides.page }));
    } catch (err) {
      if (isForbiddenError(err)) {
        setAccessDenied(true);
        showError("You don't have access to this department.");
      } else {
        showError((err as Error).message ?? 'Failed to run report');
      }
    } finally {
      setRunning(false);
    }
  };

  const buildExportParams = (): Record<string, string | boolean | undefined> => {
    const params: Record<string, string | boolean | undefined> = { format: exportFormat };
    Object.entries(currentFilters).forEach(([k, v]) => {
      if (v === '' || v === undefined || k === 'page' || k === 'pageSize') return;
      if (typeof v === 'boolean') params[k] = v;
      else params[k] = String(v);
    });
    if (departmentId && !params.departmentId) params.departmentId = departmentId;
    return params;
  };

  const handleExport = async () => {
    try {
      const ep = buildExportParams();

      if (reportKey === 'orders-list') {
        await exportOrdersListReport({
          format: ep.format as ExportFormat,
          departmentId: ep.departmentId as string | undefined,
          keyword: ep.keyword as string | undefined,
          status: ep.status as string | undefined,
          fromDate: ep.fromDate as string | undefined,
          toDate: ep.toDate as string | undefined,
          assignedSiId: ep.assignedSiId as string | undefined,
        });
      } else if (reportKey === 'materials-list') {
        await exportMaterialsReport({
          format: ep.format as ExportFormat,
          departmentId: ep.departmentId as string | undefined,
          category: ep.category as string | undefined,
          isActive: typeof ep.isActive === 'boolean' ? ep.isActive : undefined,
        });
      } else if (reportKey === 'stock-summary') {
        await exportStockSummaryReport({
          format: ep.format as ExportFormat,
          departmentId: ep.departmentId as string | undefined,
          locationId: ep.locationId as string | undefined,
          materialId: ep.materialId as string | undefined,
        });
      } else if (reportKey === 'ledger') {
        await exportLedgerReport({
          format: ep.format as ExportFormat,
          departmentId: ep.departmentId as string | undefined,
          materialId: ep.materialId as string | undefined,
          locationId: ep.locationId as string | undefined,
          orderId: ep.orderId as string | undefined,
          entryType: ep.entryType as string | undefined,
          fromDate: ep.fromDate as string | undefined,
          toDate: ep.toDate as string | undefined,
        });
      } else if (reportKey === 'scheduler-utilization') {
        await exportSchedulerUtilizationReport({
          format: ep.format as ExportFormat,
          departmentId: ep.departmentId as string | undefined,
          fromDate: ep.fromDate as string | undefined,
          toDate: ep.toDate as string | undefined,
          siId: ep.siId as string | undefined,
        });
      } else {
        if (exportFormat !== 'csv') {
          showError('This report type only supports CSV export.');
          return;
        }
        if (!result || !result.items || result.items.length === 0) {
          showError('No data to export. Please run the report first.');
          return;
        }
        const firstItem = result.items[0] as Record<string, unknown>;
        const headers = Object.keys(firstItem);
        const csvRows = [
          headers.join(','),
          ...result.items.map((item) => {
            const row = item as Record<string, unknown>;
            return headers.map(h => `"${String(row[h] ?? '').replace(/"/g, '""')}"`).join(',');
          })
        ];
        const blob = new Blob([csvRows.join('\n')], { type: 'text/csv' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${reportKey}-export.csv`;
        document.body.appendChild(a);
        a.click();
        URL.revokeObjectURL(url);
        document.body.removeChild(a);
      }
    } catch (err) {
      showError((err as Error).message ?? 'Export failed');
    }
  };

  const columns = useMemo(() => {
    if (!result?.items?.length) return [];
    const first = result.items[0] as Record<string, unknown>;
    return Object.keys(first).map((key) => ({
      key,
      label: key.replace(/([A-Z])/g, ' $1').replace(/^./, (s) => s.toUpperCase()),
      sortable: true
    }));
  }, [result?.items]);

  const breadcrumbs = [{ label: 'Reports', path: '/reports' as const }];
  const backAction = (
    <Button variant="outline" size="sm" onClick={() => navigate('/reports')} className="gap-1">
      <ArrowLeft className="h-4 w-4" />
      Back to Reports
    </Button>
  );

  if (!reportKey) {
    return (
      <PageShell title="Report" breadcrumbs={breadcrumbs} actions={backAction}>
        <Card className="p-6">
          <p className="text-muted-foreground">Report key missing.</p>
          <Button className="mt-2" variant="outline" onClick={() => navigate('/reports')}>
            Back to Reports
          </Button>
        </Card>
      </PageShell>
    );
  }

  if (defError || (definition === null && !defLoading)) {
    return (
      <PageShell title="Report" breadcrumbs={breadcrumbs} actions={backAction}>
        <Card className="p-6">
          <p className="text-destructive">Report not found: {reportKey}</p>
          <Button className="mt-2" variant="outline" onClick={() => navigate('/reports')}>
            Back to Reports
          </Button>
        </Card>
      </PageShell>
    );
  }

  if (defLoading || !definition) {
    return (
      <PageShell title="Report" breadcrumbs={breadcrumbs} actions={backAction}>
        <Skeleton className="h-64 w-full rounded-lg" />
      </PageShell>
    );
  }

  return (
    <PageShell
      title={definition.name}
      breadcrumbs={[...breadcrumbs, { label: definition.name }]}
      actions={backAction}
    >
      <div className="space-y-4">
      {definition.description && (
        <p className="text-sm text-muted-foreground">{definition.description}</p>
      )}

      {!departmentId && definition.parameterSchema.some((p) => p.name === 'departmentId') && (
        <Card className="p-4 border-amber-500/50 bg-amber-500/5">
          <p className="text-amber-600 dark:text-amber-400">Please select a department from the header to run this report.</p>
        </Card>
      )}

      {accessDenied && (
        <Card className="p-4 border-destructive/50 bg-destructive/5">
          <p className="text-destructive">No access to this department.</p>
        </Card>
      )}

      <Card className="p-4">
        <h2 className={cn(designTokens.typography.sectionHeader, 'mb-3')}>Filters</h2>
        {definition.parameterSchema.length === 0 ? (
          <p className="text-sm text-muted-foreground mb-3">This report has no configurable filters. Click "Run report" to generate results.</p>
        ) : null}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
          {definition.parameterSchema.map((p) => (
            <div key={p.name} className="flex flex-col gap-1">
              <label className="text-sm font-medium">
                {p.label ?? p.name}
                {p.required && <span className="text-destructive ml-1">*</span>}
              </label>
              {p.type === 'bool' ? (
                <input
                  type="checkbox"
                  checked={Boolean(currentFilters[p.name])}
                  onChange={(e) => updateFilter(p.name, e.target.checked)}
                  className="rounded border"
                />
              ) : p.type === 'datetime' ? (
                <input
                  type="date"
                  className="rounded border px-2 py-1.5 bg-background"
                  value={String(currentFilters[p.name] ?? '')}
                  onChange={(e) => updateFilter(p.name, e.target.value)}
                />
              ) : p.type === 'int' ? (
                <input
                  type="number"
                  className="rounded border px-2 py-1.5 bg-background"
                  value={currentFilters[p.name] ?? ''}
                  onChange={(e) => updateFilter(p.name, e.target.value ? Number(e.target.value) : '')}
                />
              ) : p.type === 'guid' ? (
                (() => {
                  const value = String(currentFilters[p.name] ?? '');
                  let options: { value: string; label: string }[] = [];
                  if (p.name === 'departmentId') {
                    options = departments.map((d) => ({ value: d.id, label: d.name ?? d.id }));
                  } else if (p.name === 'locationId') {
                    options = locations.map((l) => ({ value: l.id, label: l.name ?? l.id }));
                  } else if (p.name === 'materialId') {
                    options = materials.map((m) => ({ value: m.id, label: (m.code ?? m.itemCode ?? m.description ?? m.name) || m.id }));
                  } else if (p.name === 'assignedSiId' || p.name === 'siId') {
                    options = serviceInstallers.map((si) => ({ value: si.id, label: si.name ?? si.id }));
                  }
                  if (options.length > 0) {
                    return (
                      <Select
                        value={value}
                        onChange={(e) => updateFilter(p.name, e.target.value)}
                        options={[{ value: '', label: p.required ? 'Select…' : 'All' }, ...options]}
                        placeholder={p.required ? 'Required' : ''}
                        className="mb-0"
                      />
                    );
                  }
                  return (
                    <input
                      type="text"
                      className="rounded border px-2 py-1.5 bg-background"
                      placeholder={p.required ? 'Required (GUID)' : 'Optional GUID'}
                      value={value}
                      onChange={(e) => updateFilter(p.name, e.target.value)}
                    />
                  );
                })()
              ) : (
                <input
                  type="text"
                  className="rounded border px-2 py-1.5 bg-background"
                  placeholder={p.required ? 'Required' : ''}
                  value={String(currentFilters[p.name] ?? '')}
                  onChange={(e) => updateFilter(p.name, e.target.value)}
                />
              )}
            </div>
          ))}
        </div>
        <div className="flex gap-2 mt-4">
          <Button onClick={handleRun} disabled={running || !departmentId} className="gap-2">
            <Play className="h-4 w-4" />
            {running ? 'Running…' : 'Run report'}
          </Button>
          {definition.supportsExport && canExport && (
            <div className="flex items-center gap-2">
              <Select
                value={exportFormat}
                onChange={(e) => setExportFormat((e.target.value || 'csv') as ExportFormat)}
                options={[
                  { value: 'csv', label: 'CSV' },
                  { value: 'xlsx', label: 'Excel (.xlsx)' },
                  { value: 'pdf', label: 'PDF' }
                ]}
                className="mb-0 w-[140px]"
              />
              <Button variant="outline" onClick={handleExport} className="gap-2">
                <Download className="h-4 w-4" />
                Export
              </Button>
            </div>
          )}
        </div>
      </Card>

      {result && (
        <Card className="p-4">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mb-2">
            <h2 className={cn(designTokens.typography.sectionHeader)}>
              Results {result.totalCount != null && `(${result.totalCount})`}
            </h2>
            {result.page != null && result.pageSize != null && result.totalCount != null && result.totalCount > 0 && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <span>
                  {result.pageSize * (result.page - 1) + 1}–
                  {Math.min(result.page * result.pageSize, result.totalCount)} of {result.totalCount}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={result.page <= 1 || running}
                  onClick={() => handleRun({ page: result.page! - 1 })}
                  aria-label="Previous page"
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={
                    result.page >= Math.ceil(result.totalCount / result.pageSize) || running
                  }
                  onClick={() => handleRun({ page: result.page! + 1 })}
                  aria-label="Next page"
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            )}
          </div>
          {result.items.length === 0 ? (
            <p className="text-muted-foreground py-4">No rows returned.</p>
          ) : (
            <DataTable
              columns={columns}
              data={result.items as Record<string, unknown>[]}
              loading={false}
              emptyMessage="No data"
            />
          )}
        </Card>
      )}
      </div>
    </PageShell>
  );
};

export default ReportRunnerPage;
