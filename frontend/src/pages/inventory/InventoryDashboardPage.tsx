import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Package, Warehouse, TrendingUp, TrendingDown, AlertTriangle,
  Plus, Minus, Search, ArrowUpRight, ArrowDownLeft, RefreshCw, X
} from 'lucide-react';
import { 
  getMaterials, 
  getStockByLocation, 
  getStockMovements, 
  recordStockMovement,
  getStockLocations
} from '../../api/inventory';
import { getWarehouses } from '../../api/warehouses';
import { LoadingSpinner, useToast, Card, Button, Modal, TextInput, SelectInput } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '@/lib/utils';
import type { Material, StockBalance, StockMovement, StockLocation } from '../../types/inventory';

interface MovementForm {
  materialId: string;
  locationId: string;
  quantity: number | string;
  reference: string;
  notes: string;
}

const InventoryDashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();
  const [materials, setMaterials] = useState<Material[]>([]);
  const [stock, setStock] = useState<StockBalance[]>([]);
  const [movements, setMovements] = useState<StockMovement[]>([]);
  const [locations, setLocations] = useState<StockLocation[]>([]);
  const [warehouses, setWarehouses] = useState<any[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [searchQuery, setSearchQuery] = useState<string>('');
  
  // Stock movement modal
  const [movementModalOpen, setMovementModalOpen] = useState<boolean>(false);
  const [movementType, setMovementType] = useState<'IN' | 'OUT'>('IN');
  const [movementForm, setMovementForm] = useState<MovementForm>({
    materialId: '',
    locationId: '',
    quantity: 1,
    reference: '',
    notes: ''
  });
  const [submitting, setSubmitting] = useState<boolean>(false);

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async (): Promise<void> => {
    try {
      setLoading(true);
      const [materialsData, stockData, movementsData, locationsData, warehousesData] = await Promise.all([
        getMaterials({ isActive: true }),
        getStockByLocation(),
        getStockMovements({ limit: 10 }),
        getStockLocations().catch(() => []),
        getWarehouses().catch(() => [])
      ]);
      setMaterials(Array.isArray(materialsData) ? materialsData : []);
      setStock(Array.isArray(stockData) ? stockData : []);
      setMovements(Array.isArray(movementsData) ? movementsData : []);
      setLocations(Array.isArray(locationsData) ? locationsData : []);
      setWarehouses(Array.isArray(warehousesData) ? warehousesData : []);
    } catch (err) {
      console.error('Error loading dashboard:', err);
      showError('Failed to load inventory dashboard');
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateStr?: string): string => {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    const now = new Date();
    const diffHours = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60));
    
    if (diffHours < 1) return 'Just now';
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffHours < 48) return 'Yesterday';
    return date.toLocaleDateString('en-MY', { day: 'numeric', month: 'short' });
  };

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount || 0);
  };

  // Calculate KPIs
  const totalMaterials = materials.length;
  const serializedMaterials = materials.filter(m => m.isSerialised).length;
  const nonSerializedMaterials = totalMaterials - serializedMaterials;
  
  const totalStockValue = stock.reduce((sum, s) => {
    const material = materials.find(m => m.id === s.materialId);
    return sum + (s.quantity * (material?.unitPrice || 0));
  }, 0);
  
  const lowStockItems = stock.filter(s => {
    const material = materials.find(m => m.id === s.materialId);
    return s.quantity <= (material?.minStockLevel || 5);
  }).length;

  // Filter materials by search
  const filteredMaterials = materials.filter(m => 
    m.code?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    m.description?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    m.categoryName?.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Open stock movement modal
  const openMovementModal = (type: 'IN' | 'OUT'): void => {
    setMovementType(type);
    setMovementForm({
      materialId: '',
      locationId: locations[0]?.id || '',
      quantity: 1,
      reference: '',
      notes: ''
    });
    setMovementModalOpen(true);
  };

  // Submit stock movement
  const handleSubmitMovement = async (): Promise<void> => {
    if (!movementForm.materialId || !movementForm.quantity) {
      showError('Please select a material and enter quantity');
      return;
    }
    
    try {
      setSubmitting(true);
      await recordStockMovement({
        materialId: movementForm.materialId,
        locationId: movementForm.locationId || null,
        quantity: typeof movementForm.quantity === 'string' ? parseFloat(movementForm.quantity) : movementForm.quantity,
        movementType: movementType === 'IN' ? 'StockIn' : 'StockOut',
        reference: movementForm.reference,
        notes: movementForm.notes
      } as any);
      
      showSuccess(`Stock ${movementType === 'IN' ? 'added' : 'removed'} successfully`);
      setMovementModalOpen(false);
      await loadDashboard();
    } catch (err) {
      showError((err as Error).message || 'Failed to record movement');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <PageShell title="Inventory" breadcrumbs={[{ label: 'Inventory' }]}>
        <LoadingSpinner message="Loading inventory..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Inventory"
      breadcrumbs={[{ label: 'Inventory' }]}
      actions={
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={loadDashboard} className="gap-1">
            <RefreshCw className="h-3.5 w-3.5" />
            Refresh
          </Button>
          <Button size="sm" onClick={() => openMovementModal('IN')} className="gap-1 bg-green-600 hover:bg-green-700">
            <Plus className="h-3.5 w-3.5" />
            Stock In
          </Button>
          <Button size="sm" onClick={() => openMovementModal('OUT')} className="gap-1 bg-amber-600 hover:bg-amber-700">
            <Minus className="h-3.5 w-3.5" />
            Stock Out
          </Button>
        </div>
      }
    >
      <div className="space-y-4">
        <p className="text-sm text-muted-foreground">Manage stock levels and movements</p>

      {/* Compact KPI Row */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-brand-500/10 rounded">
            <Package className="h-4 w-4 text-brand-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{totalMaterials}</p>
            <p className="text-xs text-slate-400">Materials</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-blue-500/10 rounded">
            <Package className="h-4 w-4 text-blue-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{nonSerializedMaterials}</p>
            <p className="text-xs text-slate-400">Non-Serial</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-purple-500/10 rounded">
            <Package className="h-4 w-4 text-purple-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{serializedMaterials}</p>
            <p className="text-xs text-slate-400">Serialized</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-green-500/10 rounded">
            <TrendingUp className="h-4 w-4 text-green-500" />
          </div>
          <div>
            <p className="text-lg font-bold text-white">{formatCurrency(totalStockValue)}</p>
            <p className="text-xs text-slate-400">Stock Value</p>
          </div>
        </Card>

        <Card className={cn("p-3 flex items-center gap-3", lowStockItems > 0 && "border-amber-500/50")}>
          <div className={cn("p-2 rounded", lowStockItems > 0 ? "bg-amber-500/10" : "bg-slate-500/10")}>
            <AlertTriangle className={cn("h-4 w-4", lowStockItems > 0 ? "text-amber-500" : "text-slate-500")} />
          </div>
          <div>
            <p className={cn("text-xl font-bold", lowStockItems > 0 ? "text-amber-400" : "text-white")}>{lowStockItems}</p>
            <p className="text-xs text-slate-400">Low Stock</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-emerald-500/10 rounded">
            <Warehouse className="h-4 w-4 text-emerald-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{warehouses.length || 0}</p>
            <p className="text-xs text-slate-400">Warehouses</p>
          </div>
        </Card>

        <Card className="p-3 flex items-center gap-3">
          <div className="p-2 bg-cyan-500/10 rounded">
            <Package className="h-4 w-4 text-cyan-500" />
          </div>
          <div>
            <p className="text-xl font-bold text-white">{locations.length || 0}</p>
            <p className="text-xs text-slate-400">Locations</p>
          </div>
        </Card>
      </div>

      {/* Warehouse Overview Section */}
      {warehouses.length > 0 && (
        <Card className="p-4">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-white flex items-center gap-2">
              <Warehouse className="h-5 w-5 text-emerald-500" />
              Warehouses Overview
            </h3>
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/inventory/warehouse-layout')}
              className="gap-1"
            >
              View All
              <ArrowUpRight className="h-3.5 w-3.5" />
            </Button>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            {warehouses.slice(0, 6).map((warehouse: any) => (
              <div
                key={warehouse.id}
                className="p-3 bg-slate-800/50 rounded-lg border border-slate-700 hover:border-emerald-500/50 transition-colors cursor-pointer"
                onClick={() => navigate(`/inventory/warehouse-layout`)}
              >
                <div className="flex items-start justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <Warehouse className="h-4 w-4 text-emerald-500" />
                    <p className="font-medium text-white">{warehouse.name || warehouse.code}</p>
                  </div>
                  {warehouse.isActive === false && (
                    <span className="text-xs px-2 py-0.5 rounded bg-slate-700 text-slate-400">Inactive</span>
                  )}
                </div>
                {warehouse.description && (
                  <p className="text-xs text-slate-400 mb-2">{warehouse.description}</p>
                )}
                <div className="flex items-center gap-4 text-xs text-slate-400">
                  {warehouse.currentStock !== undefined && (
                    <span>Stock: <span className="text-white font-medium">{warehouse.currentStock || 0}</span></span>
                  )}
                  {warehouse.capacity !== undefined && (
                    <span>Capacity: <span className="text-white font-medium">{warehouse.capacity || 0}</span></span>
                  )}
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Main Content - Two Columns */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Materials List - 2 columns */}
        <Card className="lg:col-span-2 p-4">
          <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3 mb-4">
            <h3 className="text-lg font-semibold text-white flex items-center gap-2">
              <Package className="h-5 w-5 text-brand-500" />
              Stock Levels
            </h3>
            <div className="relative w-full sm:w-64">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                placeholder="Search materials..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-9 pr-3 py-2 text-sm bg-slate-800 border border-slate-600 rounded-lg text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
          </div>

          {filteredMaterials.length === 0 ? (
            <div className="text-center py-8 text-slate-400">
              {searchQuery ? `No materials found matching "${searchQuery}"` : 'No materials found'}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-700">
                    <th className="text-left py-2 px-2 text-xs font-medium text-slate-400">Code</th>
                    <th className="text-left py-2 px-2 text-xs font-medium text-slate-400">Description</th>
                    <th className="text-left py-2 px-2 text-xs font-medium text-slate-400 hidden md:table-cell">Category</th>
                    <th className="text-center py-2 px-2 text-xs font-medium text-slate-400">Stock</th>
                    <th className="text-center py-2 px-2 text-xs font-medium text-slate-400">Type</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredMaterials.slice(0, 15).map((material, idx) => {
                    const stockItem = stock.find(s => s.materialId === material.id);
                    const qty = stockItem?.quantity || 0;
                    const isLow = qty <= (material.minStockLevel || 5);
                    
                    return (
                      <tr 
                        key={material.id}
                        className={cn(
                          "border-b border-slate-700/50 last:border-0 hover:bg-slate-800/50 transition-colors",
                          idx % 2 === 0 ? "bg-slate-900/30" : ""
                        )}
                      >
                        <td className="py-2 px-2">
                          <span className="font-mono text-brand-400">{material.code || material.id}</span>
                        </td>
                        <td className="py-2 px-2 text-white">{material.description || material.name}</td>
                        <td className="py-2 px-2 text-slate-400 hidden md:table-cell">{material.categoryName || '-'}</td>
                        <td className="py-2 px-2 text-center">
                          <span className={cn(
                            "px-2 py-0.5 rounded font-medium text-xs",
                            isLow ? "bg-amber-500/20 text-amber-400" : "bg-slate-700 text-white"
                          )}>
                            {qty} {material.unit || 'pcs'}
                          </span>
                        </td>
                        <td className="py-2 px-2 text-center">
                          <span className={cn(
                            "px-2 py-0.5 rounded text-xs",
                            material.isSerialised ? "bg-purple-500/20 text-purple-400" : "bg-blue-500/20 text-blue-400"
                          )}>
                            {material.isSerialised ? 'Serial' : 'Non-Serial'}
                          </span>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}

          {filteredMaterials.length > 15 && (
            <div className="mt-3 pt-3 border-t border-slate-700 text-center">
              <p className="text-xs text-slate-400">
                Showing 15 of {filteredMaterials.length} materials
              </p>
            </div>
          )}
        </Card>

        {/* Recent Movements - 1 column */}
        <Card className="p-4">
          <h3 className="text-lg font-semibold text-white flex items-center gap-2 mb-4">
            <TrendingUp className="h-5 w-5 text-emerald-500" />
            Recent Movements
          </h3>

          {movements.length === 0 ? (
            <div className="text-center py-8 text-slate-400">No recent movements</div>
          ) : (
            <div className="space-y-3">
              {movements.slice(0, 8).map((movement) => {
                const isIn = movement.movementType?.includes('In') || movement.quantity > 0;
                
                return (
                  <div 
                    key={movement.id}
                    className="flex items-center gap-3 py-2 border-b border-slate-700/50 last:border-0"
                  >
                    <div className={cn(
                      "p-1.5 rounded",
                      isIn ? "bg-green-500/20" : "bg-amber-500/20"
                    )}>
                      {isIn ? (
                        <ArrowDownLeft className="h-3.5 w-3.5 text-green-400" />
                      ) : (
                        <ArrowUpRight className="h-3.5 w-3.5 text-amber-400" />
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm text-white truncate">
                        {movement.materialName || movement.materialId?.substring(0, 8)}
                      </p>
                      <p className="text-xs text-slate-400">{movement.movementType}</p>
                    </div>
                    <div className="text-right">
                      <p className={cn(
                        "text-sm font-medium",
                        isIn ? "text-green-400" : "text-amber-400"
                      )}>
                        {isIn ? '+' : '-'}{Math.abs(movement.quantity)}
                      </p>
                      <p className="text-xs text-slate-500">{formatDate(movement.createdAt)}</p>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </Card>
      </div>

      {/* Stock Movement Modal */}
      <Modal isOpen={movementModalOpen} onClose={() => setMovementModalOpen(false)}>
        <div className="bg-card rounded-lg p-5 w-full max-w-md">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-white flex items-center gap-2">
              {movementType === 'IN' ? (
                <>
                  <ArrowDownLeft className="h-5 w-5 text-green-500" />
                  Stock In
                </>
              ) : (
                <>
                  <ArrowUpRight className="h-5 w-5 text-amber-500" />
                  Stock Out
                </>
              )}
            </h3>
            <button onClick={() => setMovementModalOpen(false)} className="text-slate-400 hover:text-white">
              <X className="h-5 w-5" />
            </button>
          </div>

          <div className="space-y-4">
            <div>
              <label className="text-xs font-medium text-slate-400 mb-1 block">Material *</label>
              <select
                value={movementForm.materialId}
                onChange={(e) => setMovementForm({ ...movementForm, materialId: e.target.value })}
                className="w-full px-3 py-2 text-sm bg-slate-800 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-brand-500"
              >
                <option value="">Select Material</option>
                {materials.filter(m => !m.isSerialised).map(m => (
                  <option key={m.id} value={m.id}>{m.code || m.id} - {m.description || m.name}</option>
                ))}
              </select>
            </div>

            {locations.length > 0 && (
              <div>
                <label className="text-xs font-medium text-slate-400 mb-1 block">Location</label>
                <select
                  value={movementForm.locationId}
                  onChange={(e) => setMovementForm({ ...movementForm, locationId: e.target.value })}
                  className="w-full px-3 py-2 text-sm bg-slate-800 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-brand-500"
                >
                  <option value="">Default Location</option>
                  {locations.map(loc => (
                    <option key={loc.id} value={loc.id}>{loc.name}</option>
                  ))}
                </select>
              </div>
            )}

            <TextInput
              label="Quantity *"
              type="number"
              min="1"
              step="1"
              value={movementForm.quantity}
              onChange={(e) => setMovementForm({ ...movementForm, quantity: e.target.value })}
            />

            <TextInput
              label="Reference (PO/DO/SO)"
              value={movementForm.reference}
              onChange={(e) => setMovementForm({ ...movementForm, reference: e.target.value })}
              placeholder="e.g., PO-2024-001"
            />

            <TextInput
              label="Notes"
              value={movementForm.notes}
              onChange={(e) => setMovementForm({ ...movementForm, notes: e.target.value })}
              placeholder="Optional notes..."
            />
          </div>

          <div className="flex justify-end gap-2 mt-6">
            <Button variant="outline" onClick={() => setMovementModalOpen(false)}>
              Cancel
            </Button>
            <Button 
              onClick={handleSubmitMovement}
              disabled={submitting || !movementForm.materialId || !movementForm.quantity}
              className={movementType === 'IN' ? 'bg-green-600 hover:bg-green-700' : 'bg-amber-600 hover:bg-amber-700'}
            >
              {submitting ? 'Processing...' : `Record ${movementType === 'IN' ? 'Stock In' : 'Stock Out'}`}
            </Button>
          </div>
        </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default InventoryDashboardPage;

