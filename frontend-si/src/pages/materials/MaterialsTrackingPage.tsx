import React, { useState } from 'react';
import { Package, Search, Filter, TrendingDown, TrendingUp, Minus } from 'lucide-react';
import { Card, TextInput, Button, Skeleton, EmptyState } from '../../components/ui';
import { useQuery } from '@tanstack/react-query';
import { getAllMaterials, getStockLevels, getStockMovements } from '../../api/inventory';
import { useAuth } from '../../contexts/AuthContext';
import { format } from 'date-fns';

export function MaterialsTrackingPage() {
  const { user } = useAuth();
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedTab, setSelectedTab] = useState<'materials' | 'stock' | 'movements'>('materials');

  // Fetch materials
  const { data: materials, isLoading: isLoadingMaterials } = useQuery({
    queryKey: ['materials', searchTerm],
    queryFn: () => getAllMaterials({ search: searchTerm || undefined, isActive: true }),
    enabled: !!user?.id,
  });

  // Fetch stock levels
  const { data: stockLevels, isLoading: isLoadingStock } = useQuery({
    queryKey: ['stockLevels'],
    queryFn: () => getStockLevels({ lowStockOnly: false }),
    enabled: !!user?.id && selectedTab === 'stock',
  });

  // Fetch stock movements
  const { data: movements, isLoading: isLoadingMovements } = useQuery({
    queryKey: ['stockMovements'],
    queryFn: () => getStockMovements({
      fromDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
    }),
    enabled: !!user?.id && selectedTab === 'movements',
  });

  const filteredMaterials = materials?.filter(m => 
    !searchTerm || 
    m.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    m.code?.toLowerCase().includes(searchTerm.toLowerCase())
  ) || [];

  return (
    <div className="p-4 space-y-4">
      <h2 className="text-2xl font-bold text-foreground flex items-center gap-2">
        <Package className="h-6 w-6" />
        Material Tracking
      </h2>

      {/* Search Bar */}
      <Card className="p-4">
        <div className="flex gap-2">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <TextInput
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Search materials..."
              className="pl-10"
            />
          </div>
        </div>
      </Card>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-border">
        <button
          onClick={() => setSelectedTab('materials')}
          className={`px-4 py-2 font-medium transition-colors ${
            selectedTab === 'materials'
              ? 'border-b-2 border-primary text-primary'
              : 'text-muted-foreground'
          }`}
        >
          Materials
        </button>
        <button
          onClick={() => setSelectedTab('stock')}
          className={`px-4 py-2 font-medium transition-colors ${
            selectedTab === 'stock'
              ? 'border-b-2 border-primary text-primary'
              : 'text-muted-foreground'
          }`}
        >
          Stock Levels
        </button>
        <button
          onClick={() => setSelectedTab('movements')}
          className={`px-4 py-2 font-medium transition-colors ${
            selectedTab === 'movements'
              ? 'border-b-2 border-primary text-primary'
              : 'text-muted-foreground'
          }`}
        >
          Movements
        </button>
      </div>

      {/* Materials Tab */}
      {selectedTab === 'materials' && (
        <>
          {isLoadingMaterials ? (
            <div className="space-y-2">
              {[1, 2, 3, 4].map((i) => (
                <Card key={i} className="p-4">
                  <div className="flex justify-between items-start">
                    <div className="flex-1 space-y-2">
                      <Skeleton className="h-5 w-3/4" />
                      <Skeleton className="h-4 w-1/2" />
                      <Skeleton className="h-4 w-1/3" />
                    </div>
                    <Skeleton className="h-5 w-5 rounded" />
                  </div>
                </Card>
              ))}
            </div>
          ) : filteredMaterials.length === 0 ? (
            <EmptyState
              title="No materials found"
              description={searchTerm ? "Try a different search term" : "No materials available"}
            />
          ) : (
            <div className="space-y-2">
              {filteredMaterials.map((material) => (
                <Card key={material.id} className="p-4">
                  <div className="flex justify-between items-start">
                    <div className="flex-1">
                      <h3 className="font-semibold">{material.name}</h3>
                      {material.code && (
                        <p className="text-sm text-muted-foreground">Code: {material.code}</p>
                      )}
                      {material.categoryName && (
                        <p className="text-sm text-muted-foreground">Category: {material.categoryName}</p>
                      )}
                      {material.unit && (
                        <p className="text-sm text-muted-foreground">Unit: {material.unit}</p>
                      )}
                    </div>
                    <Package className="h-5 w-5 text-muted-foreground" />
                  </div>
                </Card>
              ))}
            </div>
          )}
        </>
      )}

      {/* Stock Levels Tab */}
      {selectedTab === 'stock' && (
        <>
          {isLoadingStock ? (
            <div className="space-y-2">
              {[1, 2, 3].map((i) => (
                <Card key={i} className="p-4">
                  <div className="flex justify-between items-start">
                    <div className="flex-1 space-y-2">
                      <Skeleton className="h-5 w-2/3" />
                      <Skeleton className="h-4 w-1/2" />
                    </div>
                    <Skeleton className="h-6 w-16 rounded-md" />
                  </div>
                </Card>
              ))}
            </div>
          ) : !stockLevels || stockLevels.length === 0 ? (
            <EmptyState
              title="No stock data"
              description="Stock levels will appear here"
            />
          ) : (
            <div className="space-y-2">
              {stockLevels.map((stock) => (
                <Card key={`${stock.materialId}-${stock.warehouseId || 'default'}`} className="p-4">
                  <div className="flex justify-between items-start">
                    <div className="flex-1">
                      <h3 className="font-semibold">{stock.materialName}</h3>
                      {stock.warehouseName && (
                        <p className="text-sm text-muted-foreground">Warehouse: {stock.warehouseName}</p>
                      )}
                      <div className="flex items-center gap-4 mt-2">
                        <div>
                          <p className="text-sm text-muted-foreground">Quantity</p>
                          <p className="text-lg font-semibold">{stock.quantity} {stock.unit || ''}</p>
                        </div>
                        {stock.availableQuantity !== undefined && (
                          <div>
                            <p className="text-sm text-muted-foreground">Available</p>
                            <p className="text-lg font-semibold">{stock.availableQuantity} {stock.unit || ''}</p>
                          </div>
                        )}
                        {stock.reservedQuantity !== undefined && stock.reservedQuantity > 0 && (
                          <div>
                            <p className="text-sm text-muted-foreground">Reserved</p>
                            <p className="text-lg font-semibold text-yellow-600">{stock.reservedQuantity} {stock.unit || ''}</p>
                          </div>
                        )}
                      </div>
                    </div>
                    {stock.availableQuantity !== undefined && stock.availableQuantity < 10 && (
                      <TrendingDown className="h-5 w-5 text-red-500" />
                    )}
                  </div>
                </Card>
              ))}
            </div>
          )}
        </>
      )}

      {/* Movements Tab */}
      {selectedTab === 'movements' && (
        <>
          {isLoadingMovements ? (
            <div className="space-y-2">
              {[1, 2, 3].map((i) => (
                <Card key={i} className="p-4">
                  <div className="flex justify-between items-start">
                    <div className="flex-1 space-y-2">
                      <div className="flex items-center gap-2">
                        <Skeleton className="h-4 w-4 rounded" />
                        <Skeleton className="h-5 w-32" />
                      </div>
                      <Skeleton className="h-4 w-full" />
                      <Skeleton className="h-3 w-24" />
                    </div>
                  </div>
                </Card>
              ))}
            </div>
          ) : !movements || movements.length === 0 ? (
            <EmptyState
              title="No movements"
              description="Stock movements will appear here"
            />
          ) : (
            <div className="space-y-2">
              {movements.map((movement) => (
                <Card key={movement.id} className="p-4">
                  <div className="flex justify-between items-start">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        {movement.movementType === 'Out' || movement.movementType === 'Issue' ? (
                          <TrendingDown className="h-4 w-4 text-red-500" />
                        ) : movement.movementType === 'In' || movement.movementType === 'Receipt' ? (
                          <TrendingUp className="h-4 w-4 text-green-500" />
                        ) : (
                          <Minus className="h-4 w-4 text-muted-foreground" />
                        )}
                        <h3 className="font-semibold">{movement.materialName || 'Material'}</h3>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        {movement.movementType}: {movement.quantity} {movement.materialName ? '' : 'units'}
                      </p>
                      {movement.fromLocation && (
                        <p className="text-sm text-muted-foreground">From: {movement.fromLocation}</p>
                      )}
                      {movement.toLocation && (
                        <p className="text-sm text-muted-foreground">To: {movement.toLocation}</p>
                      )}
                      {movement.orderId && (
                        <p className="text-sm text-muted-foreground">Order: {movement.orderId}</p>
                      )}
                      {movement.serialNumber && (
                        <p className="text-sm text-muted-foreground">Serial: {movement.serialNumber}</p>
                      )}
                      {movement.createdAt && (
                        <p className="text-xs text-muted-foreground mt-1">
                          {format(new Date(movement.createdAt), 'MMM dd, yyyy HH:mm')}
                        </p>
                      )}
                    </div>
                  </div>
                </Card>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}

