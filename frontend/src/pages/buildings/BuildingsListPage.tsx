import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Plus, Building2, Filter, Search, RefreshCw, 
  MapPin, Calendar, ChevronDown, X, Download, Trash2
} from 'lucide-react';
import { getBuildings, PropertyTypes, PropertyTypeLabels, exportBuildings, importBuildings, downloadBuildingsTemplate, deleteBuilding } from '../../api/buildings';
import { getInstallationMethods } from '../../api/installationMethods';
import { LoadingSpinner, EmptyState, useToast, Button, Card, ImportExportButtons, StandardListTable, BulkActionsBar } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '@/lib/utils';
import { useAuth } from '../../contexts/AuthContext';
import type { Building, PropertyType, BuildingFilters } from '../../types/buildings';
import type { InstallationMethod } from '../../types/installationMethods';

// Property type badge
interface PropertyTypeBadgeProps {
  type: PropertyType | string;
}

const PropertyTypeBadge: React.FC<PropertyTypeBadgeProps> = ({ type }) => {
  const colors: Record<string, string> = {
    MDU: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
    SDU: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
    Shoplot: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
    Factory: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',
    Office: 'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-400',
    Other: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
  };
  
  return (
    <span className={cn("px-2 py-0.5 rounded text-[10px] font-medium", colors[type] || colors.Other)}>
      {PropertyTypeLabels[type as PropertyType] || type || 'Unknown'}
    </span>
  );
};

interface BuildingFiltersState {
  propertyType: string;
  installationMethodId: string;
  state: string;
  isActive: string;
}

const BuildingsListPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();
  const { user } = useAuth();
  
  const [buildings, setBuildings] = useState<Building[]>([]);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [showFilters, setShowFilters] = useState<boolean>(false);
  const [selectedRows, setSelectedRows] = useState<string[]>([]);
  const [filters, setFilters] = useState<BuildingFiltersState>({
    propertyType: '',
    installationMethodId: '',
    state: '',
    isActive: ''
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const [buildingsData, methodsData] = await Promise.all([
        getBuildings(getActiveFilters()),
        getInstallationMethods({ isActive: true })
      ]);
      setBuildings(Array.isArray(buildingsData) ? buildingsData : []);
      setInstallationMethods(Array.isArray(methodsData) ? methodsData : []);
    } catch (err: any) {
      showError(err.message || 'Failed to load buildings');
    } finally {
      setLoading(false);
    }
  };

  const getActiveFilters = (): BuildingFilters => {
    const activeFilters: BuildingFilters = {};
    if (filters.propertyType) activeFilters.propertyType = filters.propertyType as PropertyType;
    if (filters.installationMethodId) activeFilters.installationMethodId = filters.installationMethodId;
    if (filters.state) activeFilters.state = filters.state;
    if (filters.isActive !== '') activeFilters.isActive = filters.isActive === 'true';
    return activeFilters;
  };

  const handleApplyFilters = (): void => {
    loadData();
    setShowFilters(false);
  };

  const handleClearFilters = (): void => {
    setFilters({
      propertyType: '',
      installationMethodId: '',
      state: '',
      isActive: ''
    });
    setSearchTerm('');
  };

  const hasActiveFilters = Object.values(filters).some(v => v !== '');

  // Get unique states for filter
  const uniqueStates = [...new Set(buildings.map(b => b.state).filter(Boolean))].sort();

  // Filter buildings by search term
  const filteredBuildings = buildings.filter(b => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return (
      b.name?.toLowerCase().includes(term) ||
      b.code?.toLowerCase().includes(term) ||
      b.city?.toLowerCase().includes(term) ||
      b.state?.toLowerCase().includes(term) ||
      (b as any).area?.toLowerCase().includes(term)
    );
  });

  const canManage = user?.roles?.some((r: string) => ['Admin', 'Manager'].includes(r)) || false;

  const handleExportBuildings = async (): Promise<void> => {
    try {
      await exportBuildings(getActiveFilters());
      showSuccess('Buildings exported successfully');
    } catch (err: any) {
      showError(err.message || 'Failed to export buildings');
    }
  };

  const handleDeleteBuildings = async (): Promise<void> => {
    if (!window.confirm(`Are you sure you want to delete ${selectedRows.length} building(s)?`)) return;
    try {
      await Promise.all(selectedRows.map(id => deleteBuilding(id)));
      showSuccess(`${selectedRows.length} building(s) deleted successfully`);
      setSelectedRows([]);
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to delete buildings');
    }
  };

  const handleDeactivateBuilding = async (id: string): Promise<void> => {
    // This would typically call an updateBuilding API with isActive: false
    showError('Deactivate functionality not implemented');
  };

  const handleDeleteBuilding = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this building?')) return;
    try {
      await deleteBuilding(id);
      showSuccess('Building deleted successfully');
      loadData();
    } catch (err: any) {
      showError(err.message || 'Failed to delete building');
    }
  };

  const handleDownloadTemplate = async (): Promise<void> => {
    try {
      await downloadBuildingsTemplate();
      showSuccess('Template downloaded successfully');
    } catch (err: any) {
      showError(err.message || 'Failed to download template');
    }
  };

  if (loading) {
    return (
      <PageShell title="Buildings" breadcrumbs={[{ label: 'Buildings' }]}>
        <LoadingSpinner message="Loading buildings..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Buildings"
      breadcrumbs={[{ label: 'Buildings' }]}
      actions={
        <div className="flex items-center gap-2">
          <ImportExportButtons
            entityName="Buildings"
            onExport={handleExportBuildings}
            onImport={async (file: File) => {
              const result = await importBuildings(file);
              await loadData();
              return result;
            }}
            onDownloadTemplate={handleDownloadTemplate}
          />
          <Button
            variant="outline"
            size="sm"
            onClick={loadData}
            className="gap-2"
          >
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          <Button
            size="sm"
            onClick={() => navigate('/buildings/new')}
            className="gap-2"
          >
            <Plus className="h-4 w-4" />
            Add Building
          </Button>
        </div>
      }
    >
      <div className="space-y-4">
      {/* Search and Filters */}
      <Card className="p-3">
        <div className="flex flex-col sm:flex-row gap-3">
          {/* Search */}
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search by name, code, city, state..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full h-9 pl-10 pr-4 text-sm bg-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>

          {/* Filter Toggle */}
          <Button
            variant={hasActiveFilters ? "default" : "outline"}
            size="sm"
            onClick={() => setShowFilters(!showFilters)}
            className="gap-2"
          >
            <Filter className="h-4 w-4" />
            Filters
            {hasActiveFilters && (
              <span className="ml-1 px-1.5 py-0.5 bg-primary-foreground text-primary rounded text-[10px]">
                {Object.values(filters).filter(v => v !== '').length}
              </span>
            )}
            <ChevronDown className={cn("h-3 w-3 transition-transform", showFilters && "rotate-180")} />
          </Button>
        </div>

        {/* Filter Panel */}
        {showFilters && (
          <div className="mt-3 pt-3 border-t border-border">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              <div>
                <label className="text-xs font-medium text-muted-foreground mb-1 block">Property Type</label>
                <select
                  value={filters.propertyType}
                  onChange={(e) => setFilters({ ...filters, propertyType: e.target.value })}
                  className="w-full h-8 px-2 text-xs bg-background border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">All Types</option>
                  {Object.entries(PropertyTypeLabels).map(([value, label]) => (
                    <option key={value} value={value}>{label}</option>
                  ))}
                </select>
              </div>
              
              <div>
                <label className="text-xs font-medium text-muted-foreground mb-1 block">Installation Method</label>
                <select
                  value={filters.installationMethodId}
                  onChange={(e) => setFilters({ ...filters, installationMethodId: e.target.value })}
                  className="w-full h-8 px-2 text-xs bg-background border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">All Methods</option>
                  {installationMethods.map(m => (
                    <option key={m.id} value={m.id}>{m.name}</option>
                  ))}
                </select>
              </div>
              
              <div>
                <label className="text-xs font-medium text-muted-foreground mb-1 block">State</label>
                <select
                  value={filters.state}
                  onChange={(e) => setFilters({ ...filters, state: e.target.value })}
                  className="w-full h-8 px-2 text-xs bg-background border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">All States</option>
                  {uniqueStates.map(state => (
                    <option key={state} value={state}>{state}</option>
                  ))}
                </select>
              </div>
              
              <div>
                <label className="text-xs font-medium text-muted-foreground mb-1 block">Status</label>
                <select
                  value={filters.isActive}
                  onChange={(e) => setFilters({ ...filters, isActive: e.target.value })}
                  className="w-full h-8 px-2 text-xs bg-background border border-input rounded focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">All</option>
                  <option value="true">Active</option>
                  <option value="false">Inactive</option>
                </select>
              </div>
            </div>
            
            <div className="flex items-center justify-end gap-2 mt-3">
              <Button variant="ghost" size="sm" onClick={handleClearFilters}>
                <X className="h-3 w-3 mr-1" />
                Clear
              </Button>
              <Button size="sm" onClick={handleApplyFilters}>
                Apply Filters
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Results Summary */}
      <div className="flex items-center justify-between text-xs text-muted-foreground mb-2">
        <span>
          Showing <span className="font-medium text-foreground">{filteredBuildings.length}</span> of{' '}
          <span className="font-medium text-foreground">{buildings.length}</span> buildings
        </span>
      </div>

      {/* Bulk Actions Bar */}
      {selectedRows.length > 0 && (
        <BulkActionsBar
          selectedCount={selectedRows.length}
          onClearSelection={() => setSelectedRows([])}
          actions={[
            {
              label: 'Export',
              icon: Download,
              onClick: handleExportBuildings
            },
            ...(canManage ? [{
              label: 'Delete',
              icon: Trash2,
              variant: 'destructive' as const,
              onClick: handleDeleteBuildings
            }] : [])
          ]}
        />
      )}

      {/* Buildings Table */}
      <Card>
        {filteredBuildings.length === 0 ? (
          <EmptyState
            title="No buildings found"
            message={hasActiveFilters || searchTerm 
              ? "Try adjusting your filters or search term."
              : "Add your first building to get started."
            }
            action={
              !hasActiveFilters && !searchTerm && (
                <Button onClick={() => navigate('/buildings/new')} className="mt-2">
                  <Plus className="h-4 w-4 mr-2" />
                  Add Building
                </Button>
              )
            }
          />
        ) : (
          <StandardListTable
            data={filteredBuildings}
            selectedRows={selectedRows}
            onSelectionChange={setSelectedRows}
            onRowClick={(row: Building) => navigate(`/buildings/${row.id}`)}
            actions={{
              viewPath: '/buildings/{id}',
              editPath: '/buildings/{id}/edit',
              onDownload: handleDownloadTemplate,
              ...(canManage && {
                onDeactivate: handleDeactivateBuilding,
                onDelete: handleDeleteBuilding
              })
            }}
            pageSize={20}
            loading={loading}
            emptyMessage={hasActiveFilters || searchTerm 
              ? "Try adjusting your filters or search term."
              : "Add your first building to get started."}
          />
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default BuildingsListPage;

