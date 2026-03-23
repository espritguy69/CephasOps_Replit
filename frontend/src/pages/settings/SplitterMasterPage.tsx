import React, { useState, useEffect } from 'react';
import { 
  Network, Filter, Search, RefreshCw, AlertTriangle, 
  ChevronDown, Building2, Layers, MapPin
} from 'lucide-react';
import { getAllSplitters, SplitterStatuses, SplitterStatusLabels } from '../../api/infrastructure';
import { getBuildings } from '../../api/buildings';
import { PageShell } from '../../components/layout';
import { LoadingSpinner, EmptyState, useToast, Button, Card } from '../../components/ui';
import { cn } from '@/lib/utils';
import type { Building } from '../../types/buildings';

interface Splitter {
  id: string;
  name: string;
  buildingId?: string;
  buildingName?: string;
  blockId?: string;
  blockName?: string;
  floor?: string;
  locationDescription?: string;
  serialNumber?: string;
  status: string;
  isFull?: boolean;
  needsAttention?: boolean;
  portsTotal: number;
  portsUsed: number;
  splitterTypeName?: string;
  updatedAt?: string;
}

interface CapacityBarProps {
  used: number;
  total: number;
}

const CapacityBar: React.FC<CapacityBarProps> = ({ used, total }) => {
  const percent = total > 0 ? (used / total) * 100 : 0;
  const getColor = (): string => {
    if (percent >= 100) return 'bg-red-500';
    if (percent >= 80) return 'bg-amber-500';
    return 'bg-emerald-500';
  };
  
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden min-w-[60px]">
        <div className={cn("h-full transition-all", getColor())} style={{ width: `${Math.min(percent, 100)}%` }} />
      </div>
      <span className={cn("text-xs font-medium whitespace-nowrap", percent >= 100 ? "text-red-500" : "text-muted-foreground")}>
        {used}/{total}
      </span>
    </div>
  );
};

interface StatusBadgeProps {
  status: string;
}

const StatusBadge: React.FC<StatusBadgeProps> = ({ status }) => {
  const colors: Record<string, string> = {
    Active: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
    Full: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
    Faulty: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
    MaintenanceRequired: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
    Decommissioned: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
  };
  return (
    <span className={cn("px-2 py-0.5 rounded text-[10px] font-medium whitespace-nowrap", colors[status] || colors.Active)}>
      {SplitterStatusLabels[status] || status}
    </span>
  );
};

interface SplitterFilters {
  buildingId?: string;
  status?: string;
  needsAttention?: boolean;
}

interface FiltersState {
  buildingId: string;
  status: string;
  needsAttention: string;
}

const SplitterMasterPage: React.FC = () => {
  const { showError } = useToast();
  
  const [splitters, setSplitters] = useState<Splitter[]>([]);
  const [buildings, setBuildings] = useState<Building[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [showFilters, setShowFilters] = useState<boolean>(false);
  const [filters, setFilters] = useState<FiltersState>({
    buildingId: '',
    status: '',
    needsAttention: ''
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const [splittersData, buildingsData] = await Promise.all([
        getAllSplitters(getActiveFilters()),
        getBuildings({ isActive: true })
      ]);
      setSplitters(Array.isArray(splittersData) ? splittersData : []);
      setBuildings(Array.isArray(buildingsData) ? buildingsData : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load splitters');
    } finally {
      setLoading(false);
    }
  };

  const getActiveFilters = (): SplitterFilters => {
    const activeFilters: SplitterFilters = {};
    if (filters.buildingId) activeFilters.buildingId = filters.buildingId;
    if (filters.status) activeFilters.status = filters.status;
    if (filters.needsAttention === 'true') activeFilters.needsAttention = true;
    return activeFilters;
  };

  const handleApplyFilters = (): void => {
    loadData();
    setShowFilters(false);
  };

  const handleClearFilters = (): void => {
    setFilters({ buildingId: '', status: '', needsAttention: '' });
    setSearchTerm('');
  };

  const hasActiveFilters = Object.values(filters).some(v => v !== '');

  // Filter by search term
  const filteredSplitters = splitters.filter(s => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return (
      s.name?.toLowerCase().includes(term) ||
      s.buildingName?.toLowerCase().includes(term) ||
      s.blockName?.toLowerCase().includes(term) ||
      s.locationDescription?.toLowerCase().includes(term) ||
      s.serialNumber?.toLowerCase().includes(term)
    );
  });

  // Stats
  const totalSplitters = filteredSplitters.length;
  const activeSplitters = filteredSplitters.filter(s => s.status === 'Active').length;
  const fullSplitters = filteredSplitters.filter(s => s.isFull).length;
  const needsAttentionCount = filteredSplitters.filter(s => s.needsAttention).length;
  const totalPorts = filteredSplitters.reduce((sum, s) => sum + (s.portsTotal || 0), 0);
  const usedPorts = filteredSplitters.reduce((sum, s) => sum + (s.portsUsed || 0), 0);

  if (loading) {
    return (
      <PageShell title="Splitter Master" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Splitters' }]}>
        <LoadingSpinner message="Loading splitters..." />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Splitter Master"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Splitters' }]}
      actions={
        <Button variant="outline" size="sm" onClick={loadData} className="gap-2">
          <RefreshCw className="h-3 w-3" />
          Refresh
        </Button>
      }
    >
      <div className="flex-1 p-2 max-w-7xl mx-auto space-y-3">
      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-2">
        <Card className="p-2">
          <p className="text-[10px] text-muted-foreground">Total Splitters</p>
          <p className="text-lg font-bold">{totalSplitters}</p>
        </Card>
        <Card className="p-2">
          <p className="text-[10px] text-muted-foreground">Active</p>
          <p className="text-lg font-bold text-emerald-600">{activeSplitters}</p>
        </Card>
        <Card className="p-2">
          <p className="text-[10px] text-muted-foreground">Full</p>
          <p className="text-lg font-bold text-amber-600">{fullSplitters}</p>
        </Card>
        <Card className="p-2">
          <p className="text-[10px] text-muted-foreground flex items-center gap-1">
            <AlertTriangle className="h-3 w-3" />
            Needs Attention
          </p>
          <p className="text-lg font-bold text-red-600">{needsAttentionCount}</p>
        </Card>
        <Card className="p-2">
          <p className="text-[10px] text-muted-foreground">Port Usage</p>
          <p className="text-lg font-bold">{usedPorts}/{totalPorts}</p>
          <CapacityBar used={usedPorts} total={totalPorts} />
        </Card>
      </div>

      {/* Search and Filters */}
      <Card className="p-2">
        <div className="flex flex-col sm:flex-row gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3 w-3 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search by name, building, block, location..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full h-8 pl-8 pr-3 text-xs bg-background border border-input rounded focus:outline-none focus:ring-1 focus:ring-ring"
            />
          </div>
          <Button
            variant={hasActiveFilters ? "default" : "outline"}
            size="sm"
            onClick={() => setShowFilters(!showFilters)}
            className="gap-1 text-xs"
          >
            <Filter className="h-3 w-3" />
            Filters
            {hasActiveFilters && (
              <span className="ml-1 px-1 py-0.5 bg-primary-foreground text-primary rounded text-[10px]">
                {Object.values(filters).filter(v => v !== '').length}
              </span>
            )}
            <ChevronDown className={cn("h-3 w-3 transition-transform", showFilters && "rotate-180")} />
          </Button>
        </div>

        {showFilters && (
          <div className="mt-2 pt-2 border-t border-border">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-2">
              <div>
                <label className="text-[10px] font-medium text-muted-foreground mb-0.5 block">Building</label>
                <select
                  value={filters.buildingId}
                  onChange={(e) => setFilters({ ...filters, buildingId: e.target.value })}
                  className="w-full h-7 px-2 text-xs bg-background border border-input rounded focus:outline-none focus:ring-1 focus:ring-ring"
                >
                  <option value="">All Buildings</option>
                  {buildings.map(b => (
                    <option key={b.id} value={b.id}>{b.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-[10px] font-medium text-muted-foreground mb-0.5 block">Status</label>
                <select
                  value={filters.status}
                  onChange={(e) => setFilters({ ...filters, status: e.target.value })}
                  className="w-full h-7 px-2 text-xs bg-background border border-input rounded focus:outline-none focus:ring-1 focus:ring-ring"
                >
                  <option value="">All Statuses</option>
                  {Object.entries(SplitterStatusLabels).map(([k, v]) => (
                    <option key={k} value={k}>{v}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-[10px] font-medium text-muted-foreground mb-0.5 block">Attention</label>
                <select
                  value={filters.needsAttention}
                  onChange={(e) => setFilters({ ...filters, needsAttention: e.target.value })}
                  className="w-full h-7 px-2 text-xs bg-background border border-input rounded focus:outline-none focus:ring-1 focus:ring-ring"
                >
                  <option value="">All</option>
                  <option value="true">Needs Attention Only</option>
                </select>
              </div>
            </div>
            <div className="flex justify-end gap-2 mt-2">
              <Button variant="ghost" size="sm" onClick={handleClearFilters} className="text-xs h-7">Clear</Button>
              <Button size="sm" onClick={handleApplyFilters} className="text-xs h-7">Apply</Button>
            </div>
          </div>
        )}
      </Card>

      {/* Results */}
      <div className="flex items-center justify-between text-xs text-muted-foreground">
        <span>
          Showing <span className="font-medium text-foreground">{filteredSplitters.length}</span> splitters
        </span>
      </div>

      {/* Splitters Table */}
      <Card>
        {filteredSplitters.length === 0 ? (
          <EmptyState
            title="No splitters found"
            message={hasActiveFilters || searchTerm 
              ? "Try adjusting your filters or search term."
              : "No splitters have been added yet."
            }
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-border bg-muted/30">
                  <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Splitter</th>
                  <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Building</th>
                  <th className="text-left py-2 px-3 font-semibold text-muted-foreground hidden md:table-cell">Block</th>
                  <th className="text-left py-2 px-3 font-semibold text-muted-foreground hidden lg:table-cell">Floor</th>
                  <th className="text-left py-2 px-3 font-semibold text-muted-foreground hidden lg:table-cell">Location</th>
                  <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Capacity</th>
                  <th className="text-left py-2 px-3 font-semibold text-muted-foreground">Status</th>
                  <th className="text-left py-2 px-3 font-semibold text-muted-foreground hidden xl:table-cell">Updated</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {filteredSplitters.map(splitter => (
                  <tr key={splitter.id} className="hover:bg-muted/50">
                    <td className="py-2 px-3">
                      <div className="flex items-center gap-2">
                        {splitter.needsAttention && (
                          <AlertTriangle className="h-3 w-3 text-amber-500 flex-shrink-0" />
                        )}
                        <div>
                          <p className="font-medium text-foreground">{splitter.name}</p>
                          {splitter.splitterTypeName && (
                            <p className="text-[10px] text-muted-foreground">{splitter.splitterTypeName}</p>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="py-2 px-3">
                      <div className="flex items-center gap-1">
                        <Building2 className="h-3 w-3 text-muted-foreground flex-shrink-0" />
                        <span className="text-muted-foreground truncate max-w-[150px]">{splitter.buildingName || '-'}</span>
                      </div>
                    </td>
                    <td className="py-2 px-3 hidden md:table-cell">
                      <div className="flex items-center gap-1">
                        <Layers className="h-3 w-3 text-muted-foreground flex-shrink-0" />
                        <span className="text-muted-foreground">{splitter.blockName || '-'}</span>
                      </div>
                    </td>
                    <td className="py-2 px-3 text-muted-foreground hidden lg:table-cell">
                      {splitter.floor || '-'}
                    </td>
                    <td className="py-2 px-3 hidden lg:table-cell">
                      <div className="flex items-center gap-1">
                        <MapPin className="h-3 w-3 text-muted-foreground flex-shrink-0" />
                        <span className="text-muted-foreground truncate max-w-[150px]">{splitter.locationDescription || '-'}</span>
                      </div>
                    </td>
                    <td className="py-2 px-3 min-w-[120px]">
                      <CapacityBar used={splitter.portsUsed} total={splitter.portsTotal} />
                    </td>
                    <td className="py-2 px-3">
                      <StatusBadge status={splitter.status} />
                    </td>
                    <td className="py-2 px-3 text-muted-foreground hidden xl:table-cell">
                      {splitter.updatedAt 
                        ? new Date(splitter.updatedAt).toLocaleDateString('en-MY', { day: 'numeric', month: 'short' })
                        : '-'
                      }
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default SplitterMasterPage;

