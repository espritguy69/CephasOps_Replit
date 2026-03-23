import React from 'react';
import { Package, AlertCircle, CheckCircle, AlertTriangle } from 'lucide-react';
import { Card, EmptyState, LoadingSpinner, Button } from '../ui';
import { useQuery } from '@tanstack/react-query';
import { getOrder, getRequiredMaterials, getMaterialUsage, type RequiredMaterial, type MaterialUsageRecorded } from '../../api/orders';
import { useAuth } from '../../contexts/AuthContext';
import type { ParsedMaterial } from '../../types/api';

interface MaterialsDisplayProps {
  orderId: string;
  onMarkFaulty?: (serialNumber: string, materialName: string) => void;
}

export function MaterialsDisplay({ orderId, onMarkFaulty }: MaterialsDisplayProps) {
  const { user } = useAuth();

  const { data: order, isLoading, error } = useQuery({
    queryKey: ['orderMaterials', orderId],
    queryFn: () => getOrder(orderId),
    enabled: !!user?.id && !!orderId,
  });

  const { data: requiredMaterials, isLoading: isLoadingRequired } = useQuery({
    queryKey: ['requiredMaterials', orderId],
    queryFn: () => getRequiredMaterials(orderId),
    enabled: !!user?.id && !!orderId,
  });

  const { data: materialUsage, isLoading: isLoadingUsage } = useQuery({
    queryKey: ['materialUsage', orderId],
    queryFn: () => getMaterialUsage(orderId),
    enabled: !!user?.id && !!orderId,
  });

  if (isLoading || isLoadingRequired || isLoadingUsage) {
    return <LoadingSpinner />;
  }

  if (error) {
    return (
      <EmptyState
        title="Error loading materials"
        description={(error as Error).message || 'Failed to fetch materials for this job.'}
      />
    );
  }

  const materials: ParsedMaterial[] = order?.parsedMaterials || [];
  const recordedUsage: MaterialUsageRecorded[] = materialUsage || [];
  const hasRequiredMaterials = requiredMaterials && requiredMaterials.length > 0;
  const hasUsedMaterials = materials && materials.length > 0;
  const hasRecordedUsage = recordedUsage && recordedUsage.length > 0;

  return (
    <div className="space-y-4">
      {/* Required Materials Section */}
      {hasRequiredMaterials && (
        <Card className="p-4">
          <h3 className="font-semibold text-lg flex items-center gap-2 mb-3">
            <Package className="h-5 w-5 text-primary" />
            Required Materials
          </h3>
          <div className="space-y-2">
            {requiredMaterials.map((material: RequiredMaterial) => (
              <div key={material.materialId} className="flex items-center justify-between p-2 bg-muted rounded">
                <div className="flex items-center gap-2">
                  {material.isSerialised && (
                    <AlertCircle className="h-4 w-4 text-yellow-500" title="Serialised item" />
                  )}
                  <span className="font-medium">{material.materialName}</span>
                  <span className="text-sm text-muted-foreground">({material.materialCode})</span>
                </div>
                <span className="text-sm font-medium">
                  {material.quantity} {material.unitOfMeasure}
                </span>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Recorded Material Usage Section */}
      {hasRecordedUsage && (
        <Card className="p-4">
          <h3 className="font-semibold text-lg flex items-center gap-2 mb-3">
            <CheckCircle className="h-5 w-5 text-green-500" />
            Recorded Materials
          </h3>
          <div className="space-y-2">
            {recordedUsage.map((usage) => (
              <div key={usage.id} className="border-b border-border pb-2 last:border-b-0 last:pb-0">
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <p className="font-medium">{usage.materialName || 'Unknown Material'}</p>
                      {/* TODO: Add faulty status indicator when backend supports it */}
                    </div>
                    {usage.serialNumber && (
                      <p className="text-sm text-muted-foreground mt-1">Serial: {usage.serialNumber}</p>
                    )}
                    <p className="text-xs text-muted-foreground mt-1">
                      Recorded: {new Date(usage.recordedAt).toLocaleString()}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-sm text-muted-foreground">
                      {usage.quantity} {requiredMaterials?.find(m => m.materialId === usage.materialId)?.unitOfMeasure || 'units'}
                    </span>
                    {usage.serialNumber && onMarkFaulty && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => onMarkFaulty(usage.serialNumber!, usage.materialName)}
                        className="h-8 px-2"
                        title="Mark as faulty"
                      >
                        <AlertTriangle className="h-3 w-3 text-yellow-500" />
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Legacy Materials Used Section (from parsedMaterials) */}
      {hasUsedMaterials && !hasRecordedUsage && (
        <Card className="p-4">
          <h3 className="font-semibold text-lg flex items-center gap-2 mb-3">
            <CheckCircle className="h-5 w-5 text-green-500" />
            Materials Used (Legacy)
          </h3>
          <div className="space-y-2">
            {materials.map((material, index) => (
              <div key={material.id || index} className="border-b border-border pb-2 last:border-b-0 last:pb-0">
                <div className="flex justify-between items-center">
                  <p className="font-medium">{material.materialName || 'Unknown Material'}</p>
                  <span className="text-sm text-muted-foreground">
                    {material.quantity || 0} {material.unit || 'units'}
                  </span>
                </div>
                {material.serialNumber && (
                  <p className="text-sm text-muted-foreground">Serial: {material.serialNumber}</p>
                )}
                {material.category && (
                  <p className="text-xs text-muted-foreground italic">Category: {material.category}</p>
                )}
              </div>
            ))}
          </div>
        </Card>
      )}

      {/* Empty State */}
      {!hasRecordedUsage && !hasUsedMaterials && hasRequiredMaterials && (
        <Card className="p-4">
          <EmptyState 
            title="No Materials Recorded Yet" 
            description="Add materials using the forms above to track usage." 
          />
        </Card>
      )}

      {!hasRequiredMaterials && !hasRecordedUsage && !hasUsedMaterials && (
        <Card className="p-4">
          <EmptyState 
            title="No Materials Required" 
            description="No material template configured for this order type." 
          />
        </Card>
      )}
    </div>
  );
}

