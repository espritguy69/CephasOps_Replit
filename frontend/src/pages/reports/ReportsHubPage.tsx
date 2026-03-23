import React, { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Search } from 'lucide-react';
import { Card, Skeleton } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { getReportDefinitions } from '../../api/reports';
import type { ReportDefinitionHubDto } from '../../types/reports';

const ReportsHubPage: React.FC = () => {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<string>('');

  const { data: definitions = [], isLoading, error } = useQuery({
    queryKey: ['reports', 'definitions'],
    queryFn: getReportDefinitions
  });

  const categories = useMemo(() => {
    const set = new Set<string>();
    definitions.forEach((d) => {
      if (d.category) set.add(d.category);
    });
    return Array.from(set).sort();
  }, [definitions]);

  const filtered = useMemo(() => {
    let list = definitions as ReportDefinitionHubDto[];
    const q = search.trim().toLowerCase();
    if (q) {
      list = list.filter(
        (d) =>
          d.name.toLowerCase().includes(q) ||
          (d.description ?? '').toLowerCase().includes(q) ||
          d.tags.some((t) => t.toLowerCase().includes(q))
      );
    }
    if (categoryFilter) {
      list = list.filter((d) => d.category === categoryFilter);
    }
    return list;
  }, [definitions, search, categoryFilter]);

  if (error) {
    return (
      <PageShell title="Reports Hub" breadcrumbs={[{ label: 'Reports', path: '/reports' }]}>
        <div data-testid="reports-hub-root">
          <Card className="p-6 border-destructive/50 bg-destructive/5">
            <p className="text-destructive">Failed to load reports: {(error as Error).message}</p>
          </Card>
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell title="Reports Hub" breadcrumbs={[{ label: 'Reports', path: '/reports' }]}>
      <div data-testid="reports-hub-root">
      <p className="text-sm text-muted-foreground mb-4">Search and run reports by department, filters, and export when available.</p>

      <div className="mb-6">
        <h2 className="text-sm font-medium text-muted-foreground mb-2">Dashboards</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <Card
            className="p-4 cursor-pointer hover:border-primary/50 transition-colors"
            onClick={() => navigate('/reports/payout-health')}
          >
            <div className="flex flex-col gap-2">
              <h2 className="font-semibold">Payout Health</h2>
              <p className="text-sm text-muted-foreground line-clamp-2">
                Snapshot coverage, legacy/custom override counts, warnings, zero payout, and top unusual payouts.
              </p>
              <span className="text-xs text-muted-foreground">Operations · GPON</span>
            </div>
          </Card>
        </div>
      </div>

      <div className="flex flex-col sm:flex-row gap-3 mb-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            type="text"
            placeholder="Search by name, description, or tags..."
            className="w-full pl-9 pr-4 py-2 rounded-md border bg-background text-foreground"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        {categories.length > 0 && (
          <select
            className="px-4 py-2 rounded-md border bg-background text-foreground min-w-[160px]"
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
          >
            <option value="">All categories</option>
            {categories.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        )}
      </div>

      <div className="space-y-4">
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} className="h-32 rounded-lg" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <Card className="p-6">
          <p className="text-muted-foreground">No reports match your search.</p>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map((def) => (
            <Card
              key={def.reportKey}
              className="p-4 cursor-pointer hover:border-primary/50 transition-colors"
              onClick={() => navigate(`/reports/${def.reportKey}`)}
            >
              <div className="flex flex-col gap-2">
                <h2 className="font-semibold">{def.name}</h2>
                {def.description && (
                  <p className="text-sm text-muted-foreground line-clamp-2">{def.description}</p>
                )}
                {def.tags.length > 0 && (
                  <div className="flex flex-wrap gap-1">
                    {def.tags.slice(0, 5).map((t) => (
                      <span
                        key={t}
                        className="text-xs px-2 py-0.5 rounded-full bg-muted text-muted-foreground"
                      >
                        {t}
                      </span>
                    ))}
                  </div>
                )}
                {def.category && (
                  <span className="text-xs text-muted-foreground">{def.category}</span>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}
      </div>
      </div>
    </PageShell>
  );
};

export default ReportsHubPage;
