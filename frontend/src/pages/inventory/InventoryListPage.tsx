import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Package, Warehouse } from 'lucide-react';
import { getMaterials, getStockByLocation } from '../../api/inventory';
import { LoadingSpinner, EmptyState, useToast, Button, Card, DataTable, Tabs, TabPanel, StatusBadge } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useAuth } from '../../contexts/AuthContext';
import type { Material, StockBalance } from '../../types/inventory';

interface InventoryFilters {
  [key: string]: unknown;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render: (value: unknown, item: T) => React.ReactNode;
}

const InventoryListPage: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { showError } = useToast();
  const roles = user?.roles ?? [];
  const permissions = user?.permissions ?? [];
  const canEditInventory = Boolean(
    roles.includes('SuperAdmin') ||
    permissions.includes('inventory.edit') ||
    (permissions.length === 0 && roles.includes('Admin'))
  );
  const [materials, setMaterials] = useState<Material[]>([]);
  const [stock, setStock] = useState<StockBalance[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<number>(0); // 0 = materials, 1 = stock
  const [filters, setFilters] = useState<InventoryFilters>({});

  useEffect(() => {
    loadData();
  }, [activeTab, filters]);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      setError(null);

      if (activeTab === 0) {
        const data = await getMaterials(filters);
        setMaterials(Array.isArray(data) ? data : []);
      } else {
        const data = await getStockByLocation(filters);
        setStock(Array.isArray(data) ? data : []);
      }
    } catch (err) {
      const errorMessage = (err as Error).message || 'Failed to load inventory data';
      setError(errorMessage);
      showError(errorMessage);
      console.error('Error loading inventory:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading && materials.length === 0 && stock.length === 0) {
    return (
      <PageShell title="Inventory Management" breadcrumbs={[{ label: 'Inventory' }]}>
        <LoadingSpinner message="Loading inventory..." fullPage />
      </PageShell>
    );
  }

  const materialsColumns: TableColumn<Material>[] = [
    {
      key: 'code',
      label: 'Code',
      render: (value, material) => material.code || 'N/A'
    },
    {
      key: 'description',
      label: 'Description',
      render: (value, material) => material.description || 'N/A'
    },
    {
      key: 'category',
      label: 'Category',
      render: (value, material) => material.categoryName || 'N/A'
    },
    {
      key: 'unit',
      label: 'Unit',
      render: (value, material) => material.unit || 'N/A'
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (value, material) => (
        <Button variant="ghost" size="sm" onClick={() => navigate(`/inventory/stock-summary?materialId=${material.id}`)}>View</Button>
      )
    }
  ];

  const stockColumns: TableColumn<StockBalance>[] = [
    {
      key: 'material',
      label: 'Material',
      render: (value, item) => item.materialName || item.materialId || 'N/A'
    },
    {
      key: 'location',
      label: 'Location',
      render: (value, item) => item.locationName || item.locationId || 'N/A'
    },
    {
      key: 'quantity',
      label: 'Quantity',
      render: (value, item) => item.quantity || 0
    },
    {
      key: 'status',
      label: 'Status',
      render: (value, item) => (
        <StatusBadge
          status={item.quantity > 0 ? 'In Stock' : 'Out of Stock'}
          variant={item.quantity > 0 ? 'success' : 'error'}
        />
      )
    }
  ];

  return (
    <PageShell
      title="Inventory Management"
      breadcrumbs={[{ label: 'Inventory' }]}
      actions={
        canEditInventory ? (
          <Button>
            <Plus className="h-4 w-4 mr-2" />
            Add Material
          </Button>
        ) : undefined
      }
    >
      <div className="max-w-7xl mx-auto space-y-2">
      {/* Error Banner */}
      {error && (
        <div className="mb-2 rounded border border-red-200 bg-red-50 p-2 text-xs text-red-800" role="alert">
          {error}
        </div>
      )}

      {/* Tabs */}
      <Tabs
        defaultActiveTab={activeTab}
        onTabChange={(index) => {
          setActiveTab(index);
          setLoading(true);
          loadData();
        }}
        className="mb-2"
      >
        <TabPanel label="Materials" icon={<Package className="h-4 w-4" />}>
          <Card>
            {materials.length > 0 ? (
              <DataTable
                data={materials}
                columns={materialsColumns}
                pagination={true}
                pageSize={10}
                sortable={true}
              />
            ) : (
              <EmptyState
                title="No materials found"
                description="Materials will appear here once created."
              />
            )}
          </Card>
        </TabPanel>

        <TabPanel label="Stock Levels" icon={<Warehouse className="h-4 w-4" />}>
          <Card className="p-6">
            {stock.length > 0 ? (
              <DataTable
                data={stock}
                columns={stockColumns}
                pagination={true}
                pageSize={10}
                sortable={true}
              />
            ) : (
              <EmptyState
                title="No stock data found"
                description="Stock levels will appear here once data is available."
              />
            )}
          </Card>
        </TabPanel>
      </Tabs>
      </div>
    </PageShell>
  );
};

export default InventoryListPage;

