import React, { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { LoadingSpinner, EmptyState, useToast, Button, Card, Modal, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { Warehouse, Search, Package, RefreshCw, Download } from 'lucide-react';
import apiClient from '../../api/client';

interface BinSummary {
  id: string;
  code: string;
  name: string;
  section: string;
  capacity: number;
  currentStock: number;
  utilizationPercent: number;
}

interface BinContents {
  bin: BinSummary;
  stockBalances: Array<{
    id: string;
    materialCode: string;
    materialDescription: string;
    quantity: number;
  }>;
}

const getCapacityColor = (percent: number): { fill: string; stroke: string } => {
  if (percent >= 70) return { fill: '#10b981', stroke: '#059669' };
  if (percent >= 30) return { fill: '#f59e0b', stroke: '#d97706' };
  return { fill: '#ef4444', stroke: '#dc2626' };
};

const getStockLevelColor = (quantity: number, capacity: number): string => {
  if (capacity === 0) return 'text-gray-600';
  const percent = (quantity / capacity) * 100;
  if (percent < 30) return 'text-red-600';
  if (percent < 70) return 'text-yellow-600';
  return 'text-green-600';
};

const WarehouseLayoutPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedBinCode, setSelectedBinCode] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [highlightedBins, setHighlightedBins] = useState<string[]>([]);

  const { data: bins = [], isLoading: isLoadingBins, refetch: refetchBins } = useQuery<BinSummary[]>({
    queryKey: ['warehouseBins'],
    queryFn: async () => {
      const response = await apiClient.get<BinSummary[] | { data?: BinSummary[]; items?: BinSummary[] }>('/bins');
      if (Array.isArray(response)) return response;
      const wrapped = response as { data?: BinSummary[]; items?: BinSummary[] };
      return wrapped.data || wrapped.items || [];
    },
  });

  const { data: binContents, isLoading: isLoadingBinContents } = useQuery<BinContents>({
    queryKey: ['binContents', selectedBinCode],
    queryFn: async () => {
      const response = await apiClient.get<{ data?: BinContents } | BinContents>(`/bins/by-code/${selectedBinCode}/contents`);
      return (response as { data?: BinContents })?.data || response as BinContents;
    },
    enabled: !!selectedBinCode && isModalOpen,
  });

  const sections = useMemo(() => {
    const map = new Map<string, BinSummary[]>();
    bins.forEach((bin) => {
      const section = bin.section || 'Default';
      if (!map.has(section)) map.set(section, []);
      map.get(section)!.push(bin);
    });
    return map;
  }, [bins]);

  const handleSearch = async () => {
    if (!searchQuery.trim()) {
      setHighlightedBins([]);
      return;
    }
    try {
      interface BinSearchResult { binCode?: string; code?: string; id?: string }
      const response = await apiClient.get<BinSearchResult[] | { data?: BinSearchResult[] }>(`/bins/search?materialQuery=${encodeURIComponent(searchQuery)}`);
      const results: BinSearchResult[] = Array.isArray(response) ? response : (response as { data?: BinSearchResult[] })?.data || [];
      const binCodes = results.map((r) => r.binCode || r.code || r.id || '');
      setHighlightedBins(binCodes);
      if (binCodes.length === 0) {
        showError(`No bins found containing "${searchQuery}"`);
      } else {
        showSuccess(`Found material in ${binCodes.length} bin(s)`);
      }
    } catch {
      setHighlightedBins([]);
      const matching = bins.filter(b =>
        b.name?.toLowerCase().includes(searchQuery.toLowerCase()) ||
        b.code?.toLowerCase().includes(searchQuery.toLowerCase())
      );
      setHighlightedBins(matching.map(b => b.code));
      if (matching.length === 0) {
        showError(`No bins found matching "${searchQuery}"`);
      }
    }
  };

  const handleExport = () => {
    if (bins.length === 0) {
      showError('No bin data to export');
      return;
    }
    const headers = ['Code', 'Name', 'Section', 'Capacity', 'Current Stock', 'Utilization %'];
    const rows = bins.map(b => [b.code, b.name, b.section, b.capacity, b.currentStock, b.utilizationPercent.toFixed(1)]);
    const csv = [headers.join(','), ...rows.map(r => r.join(','))].join('\n');
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `warehouse-layout-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(a);
    a.click();
    URL.revokeObjectURL(url);
    document.body.removeChild(a);
    showSuccess('Warehouse layout exported');
  };

  const openBinModal = (binCode: string) => {
    setSelectedBinCode(binCode);
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setSelectedBinCode(null);
  };

  const binContentsColumns = [
    {
      key: 'materialCode',
      label: 'Material Code',
      width: '150px',
      render: (value: string) => <span className="font-medium font-mono text-sm">{value}</span>
    },
    {
      key: 'materialDescription',
      label: 'Description',
      width: '250px',
      render: (value: string) => <span className="text-sm">{value}</span>
    },
    {
      key: 'quantity',
      label: 'Quantity',
      width: '120px',
      render: (value: number) => (
        <span className={`font-semibold ${getStockLevelColor(value, binContents?.bin.capacity || 0)}`}>
          {value.toLocaleString()}
        </span>
      )
    }
  ];

  return (
    <PageShell
      title="Warehouse Layout"
      subtitle="Visual bin locations & capacity tracking"
      actions={
        <div className="flex gap-2">
          <Button size="sm" variant="outline" onClick={() => refetchBins()} className="gap-1">
            <RefreshCw className="h-3.5 w-3.5" />
            Refresh
          </Button>
          <Button size="sm" variant="outline" onClick={handleExport} className="gap-1">
            <Download className="h-3.5 w-3.5" />
            Export Layout
          </Button>
        </div>
      }
    >
      <div className="space-y-4">
        <Card className="p-4">
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search material to find bin location..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                className="w-full pl-10 pr-4 py-2 border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <Button onClick={handleSearch}>Search</Button>
            {highlightedBins.length > 0 && (
              <Button variant="outline" onClick={() => { setHighlightedBins([]); setSearchQuery(''); }}>
                Clear
              </Button>
            )}
          </div>
        </Card>

        {isLoadingBins ? (
          <LoadingSpinner message="Loading warehouse bins..." fullPage />
        ) : bins.length === 0 ? (
          <EmptyState
            title="No warehouse bins configured"
            description="Warehouse bins will appear here once they have been set up in the system. Contact your administrator to configure warehouse locations."
            icon={<Warehouse className="h-12 w-12" />}
          />
        ) : (
          <div className="space-y-6">
            {Array.from(sections.entries()).map(([sectionName, sectionBins]) => (
              <Card key={sectionName} className="p-4">
                <h3 className="font-semibold text-sm mb-3 flex items-center gap-2">
                  <Package className="h-4 w-4 text-blue-500" />
                  {sectionName}
                  <span className="text-muted-foreground font-normal">({sectionBins.length} bins)</span>
                </h3>
                <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
                  {sectionBins.map((bin) => {
                    const colors = getCapacityColor(bin.utilizationPercent);
                    const isHighlighted = highlightedBins.includes(bin.code);
                    return (
                      <div
                        key={bin.id}
                        onClick={() => openBinModal(bin.code)}
                        className={`
                          p-3 rounded-lg border-2 cursor-pointer transition-all hover:scale-105
                          ${isHighlighted ? 'ring-2 ring-blue-500 ring-offset-2 shadow-lg' : ''}
                        `}
                        style={{
                          backgroundColor: `${colors.fill}20`,
                          borderColor: colors.stroke,
                        }}
                      >
                        <div className="text-center">
                          <p className="font-bold text-sm">{bin.code}</p>
                          <p className="text-xs text-muted-foreground truncate" title={bin.name}>{bin.name}</p>
                          <p className="text-lg font-semibold mt-1" style={{ color: colors.stroke }}>
                            {bin.utilizationPercent.toFixed(0)}%
                          </p>
                          <p className="text-xs text-muted-foreground">
                            {bin.currentStock}/{bin.capacity}
                          </p>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </Card>
            ))}
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Card className="p-4">
            <h3 className="font-semibold text-sm mb-3">Capacity Legend</h3>
            <div className="space-y-2 text-sm">
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 rounded bg-emerald-500"></div>
                <span><strong>Green</strong>: {'>'} 70% capacity (Good stock)</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 rounded bg-amber-500"></div>
                <span><strong>Yellow</strong>: 30-70% capacity (Monitor)</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 rounded bg-red-500"></div>
                <span><strong>Red</strong>: {'<'} 30% capacity (Reorder needed)</span>
              </div>
            </div>
          </Card>

          <Card className="p-4">
            <h3 className="font-semibold text-sm mb-3">Quick Guide</h3>
            <ul className="space-y-1 text-sm">
              <li>Click any bin to view its contents and stock details</li>
              <li>Use the search bar to find which bin contains a material</li>
              <li>Color-coded bins provide instant capacity overview</li>
              <li>Export layout data as CSV for reporting</li>
            </ul>
          </Card>
        </div>
      </div>

      <Modal
        isOpen={isModalOpen}
        onClose={closeModal}
        title={binContents ? `Bin: ${binContents.bin.code} - ${binContents.bin.name}` : 'Bin Contents'}
        size="lg"
      >
        {isLoadingBinContents ? (
          <div className="flex items-center justify-center py-8">
            <LoadingSpinner />
          </div>
        ) : binContents ? (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="font-medium text-muted-foreground">Section:</span>
                <span className="ml-2">{binContents.bin.section}</span>
              </div>
              <div>
                <span className="font-medium text-muted-foreground">Capacity:</span>
                <span className="ml-2">{binContents.bin.capacity.toLocaleString()}</span>
              </div>
              <div>
                <span className="font-medium text-muted-foreground">Current Stock:</span>
                <span className="ml-2">{binContents.bin.currentStock.toLocaleString()}</span>
              </div>
              <div>
                <span className="font-medium text-muted-foreground">Utilization:</span>
                <span className={`ml-2 font-semibold ${getStockLevelColor(binContents.bin.currentStock, binContents.bin.capacity)}`}>
                  {binContents.bin.utilizationPercent.toFixed(1)}%
                </span>
              </div>
            </div>

            {binContents.stockBalances && binContents.stockBalances.length > 0 ? (
              <div>
                <h3 className="font-semibold mb-2">Materials in Bin</h3>
                <DataTable
                  data={binContents.stockBalances}
                  columns={binContentsColumns}
                  keyField="id"
                />
              </div>
            ) : (
              <div className="text-center py-8 text-muted-foreground">
                No materials in this bin
              </div>
            )}
          </div>
        ) : (
          <div className="text-center py-8 text-muted-foreground">
            Could not load bin contents. The bin may not exist or the server may be unavailable.
          </div>
        )}
      </Modal>
    </PageShell>
  );
};

export default WarehouseLayoutPage;

