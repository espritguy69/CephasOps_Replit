import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { 
  Building2, TrendingUp, TrendingDown, MapPin, 
  Package, Plus, Search, ChevronRight, Boxes, Eye
} from 'lucide-react';
import { getBuildingsSummary, getBuildings, PropertyTypeLabels } from '../../api/buildings';
import { getDefaultMaterialsSummary } from '../../api/buildingDefaultMaterials';
import { LoadingSpinner, EmptyState, useToast, Card, Button } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '@/lib/utils';
import type { Building, BuildingsSummary } from '../../types/buildings';

interface MaterialsSummary {
  totalMaterialItems?: number;
  totalBuildings?: number;
}

const BuildingsDashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError } = useToast();
  const [summary, setSummary] = useState<BuildingsSummary | null>(null);
  const [materialsSummary, setMaterialsSummary] = useState<MaterialsSummary | null>(null);
  const [buildings, setBuildings] = useState<Building[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [searchQuery, setSearchQuery] = useState<string>('');

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async (): Promise<void> => {
    try {
      setLoading(true);
      const [summaryData, materialsData, buildingsData] = await Promise.all([
        getBuildingsSummary(),
        getDefaultMaterialsSummary().catch(() => null),
        getBuildings({ isActive: true })
      ]);
      setSummary(summaryData as BuildingsSummary);
      setMaterialsSummary(materialsData as MaterialsSummary | null);
      setBuildings(Array.isArray(buildingsData) ? buildingsData : []);
    } catch (err: any) {
      console.error('Error loading dashboard:', err);
      showError('Failed to load buildings dashboard');
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('en-MY', { day: 'numeric', month: 'short' });
  };

  // Filter buildings by search query
  const filteredBuildings = buildings.filter(b => 
    b.name?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    b.code?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    b.city?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    b.state?.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Show recent 10 or filtered results (up to 20)
  const displayBuildings = searchQuery 
    ? filteredBuildings.slice(0, 20) 
    : filteredBuildings.slice(0, 10);

  if (loading) {
    return (
      <PageShell title="Buildings" breadcrumbs={[{ label: 'Buildings' }]}>
        <LoadingSpinner fullPage />
      </PageShell>
    );
  }
  if (!summary) {
    return (
      <PageShell title="Buildings" breadcrumbs={[{ label: 'Buildings' }]}>
        <EmptyState title="No data available" message="Buildings summary could not be loaded." />
      </PageShell>
    );
  }

  const extendedSummary = summary as BuildingsSummary & {
    totalOrders?: number;
    ordersGrowthPercent?: number;
    ordersThisMonth?: number;
    byState?: string[];
  };

  return (
    <PageShell
      title="Buildings"
      breadcrumbs={[{ label: 'Buildings' }]}
      actions={
        <Button onClick={() => navigate('/buildings/new')} size="sm" className="gap-1">
          <Plus className="h-3.5 w-3.5" />
          Add Building
        </Button>
      }
    >
      <div className="space-y-4">
      {/* Compact KPI Row */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-brand-500/10 rounded">
            <Building2 className="h-4 w-4 text-brand-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{summary.totalBuildings}</p>
            <p className="text-xs text-slate-400">Buildings</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-green-500/10 rounded">
            <Building2 className="h-4 w-4 text-green-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{summary.activeBuildings}</p>
            <p className="text-xs text-slate-400">Active</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-blue-500/10 rounded">
            <Boxes className="h-4 w-4 text-blue-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{(extendedSummary.totalOrders || 0).toLocaleString()}</p>
            <p className="text-xs text-slate-400">Orders</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className={cn("p-2 rounded", (extendedSummary.ordersGrowthPercent || 0) >= 0 ? "bg-emerald-500/10" : "bg-red-500/10")}>
            {(extendedSummary.ordersGrowthPercent || 0) >= 0 ? (
              <TrendingUp className="h-4 w-4 text-emerald-500" />
            ) : (
              <TrendingDown className="h-4 w-4 text-red-500" />
            )}
          </div>
          <div>
            <p className="text-xl font-bold text-white">{extendedSummary.ordersThisMonth || 0}</p>
            <p className="text-xs text-slate-400">This Month</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-purple-500/10 rounded">
            <Package className="h-4 w-4 text-purple-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{materialsSummary?.totalMaterialItems || 0}</p>
            <p className="text-xs text-slate-400">Materials</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-amber-500/10 rounded">
            <MapPin className="h-4 w-4 text-amber-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{(extendedSummary.byState?.length || Object.keys(summary.byState || {}).length) || 0}</p>
            <p className="text-xs text-slate-400">States</p>
          </div>
        </Card>
      </div>

      {/* Buildings List with Search */}
      <Card className="p-4">
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 mb-4">
          <h3 className="text-lg font-semibold text-white flex items-center gap-2">
            <Building2 className="h-5 w-5 text-brand-500" />
            {searchQuery ? `Search Results (${filteredBuildings.length})` : 'Recent Buildings'}
          </h3>
          <div className="flex items-center gap-2 w-full sm:w-auto">
            <div className="relative flex-1 sm:w-64">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                placeholder="Search building name, city, state..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-9 pr-3 py-2 text-sm bg-slate-800 border border-slate-600 rounded-lg text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent"
              />
            </div>
            <Link to="/buildings/list">
              <Button variant="outline" size="sm" className="gap-1 whitespace-nowrap">
                View All <ChevronRight className="h-3.5 w-3.5" />
              </Button>
            </Link>
          </div>
        </div>

        {displayBuildings.length === 0 ? (
          <div className="text-center py-8 text-slate-400">
            {searchQuery ? `No buildings found matching "${searchQuery}"` : 'No buildings found'}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-700">
                  <th className="text-left py-2 px-2 text-xs font-medium text-slate-400">Building Name</th>
                  <th className="text-left py-2 px-2 text-xs font-medium text-slate-400 hidden md:table-cell">Property Type</th>
                  <th className="text-left py-2 px-2 text-xs font-medium text-slate-400">Location</th>
                  <th className="text-left py-2 px-2 text-xs font-medium text-slate-400 hidden lg:table-cell">Installation Method</th>
                  <th className="text-center py-2 px-2 text-xs font-medium text-slate-400">Orders</th>
                  <th className="text-right py-2 px-2 text-xs font-medium text-slate-400">Actions</th>
                </tr>
              </thead>
              <tbody>
                {displayBuildings.map((building, idx) => (
                  <tr 
                    key={building.id} 
                    className={cn(
                      "border-b border-slate-700/50 last:border-0 hover:bg-slate-800/50 cursor-pointer transition-colors",
                      idx % 2 === 0 ? "bg-slate-900/30" : ""
                    )}
                    onClick={() => navigate(`/buildings/${building.id}`)}
                  >
                    <td className="py-3 px-2">
                      <div>
                        <p className="text-white font-medium">{building.name}</p>
                        {building.code && (
                          <p className="text-xs text-slate-500 font-mono">{building.code}</p>
                        )}
                      </div>
                    </td>
                    <td className="py-3 px-2 hidden md:table-cell">
                      <span className={cn(
                        "px-2 py-0.5 rounded text-xs font-medium",
                        building.propertyType === 'MDU' ? "bg-blue-500/20 text-blue-400" :
                        building.propertyType === 'SDU' ? "bg-green-500/20 text-green-400" :
                        "bg-slate-500/20 text-slate-400"
                      )}>
                        {PropertyTypeLabels[building.propertyType] || building.propertyType || '-'}
                      </span>
                    </td>
                    <td className="py-3 px-2">
                      <div>
                        <p className="text-slate-300">{building.city}</p>
                        <p className="text-xs text-slate-500">{building.state}</p>
                      </div>
                    </td>
                    <td className="py-3 px-2 hidden lg:table-cell">
                      <span className="text-slate-400 text-xs">
                        {building.installationMethodName || '-'}
                      </span>
                    </td>
                    <td className="py-3 px-2 text-center">
                      <span className="px-2 py-0.5 bg-slate-700 rounded text-white font-medium text-xs">
                        {(building as any).ordersCount || 0}
                      </span>
                    </td>
                    <td className="py-3 px-2 text-right">
                      <button 
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(`/buildings/${building.id}`);
                        }}
                        className="p-1.5 rounded hover:bg-brand-500/20 text-brand-400 transition-colors"
                        title="View Details"
                      >
                        <Eye className="h-4 w-4" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Show count info */}
        {!searchQuery && buildings.length > 10 && (
          <div className="mt-3 pt-3 border-t border-slate-700 flex justify-between items-center">
            <p className="text-xs text-slate-400">
              Showing 10 of {buildings.length} buildings
            </p>
            <Link to="/buildings/list" className="text-brand-400 text-xs hover:text-brand-300">
              View all buildings →
            </Link>
          </div>
        )}
      </Card>
      </div>
    </PageShell>
  );
};

export default BuildingsDashboardPage;

