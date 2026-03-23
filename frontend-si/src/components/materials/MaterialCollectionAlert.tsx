import React from 'react';
import { AlertTriangle, Package, CheckCircle } from 'lucide-react';
import { Card } from '../ui';
import { useQuery } from '@tanstack/react-query';
import { checkMaterialCollection, getRequiredMaterials } from '../../api/orders';
import { LoadingSpinner } from '../ui';
import type { MissingMaterial, RequiredMaterial } from '../../api/orders';

interface MaterialCollectionAlertProps {
  orderId: string;
}

export function MaterialCollectionAlert({ orderId }: MaterialCollectionAlertProps) {
  const { data: checkResult, isLoading, error } = useQuery({
    queryKey: ['materialCollectionCheck', orderId],
    queryFn: () => checkMaterialCollection(orderId),
    enabled: !!orderId,
    refetchInterval: 30000, // Refetch every 30 seconds
  });

  const { data: requiredMaterials } = useQuery({
    queryKey: ['requiredMaterials', orderId],
    queryFn: () => getRequiredMaterials(orderId),
    enabled: !!orderId,
  });

  if (isLoading) {
    return (
      <Card className="p-4">
        <div className="flex items-center gap-2">
          <LoadingSpinner />
          <span className="text-sm text-muted-foreground">Checking material requirements...</span>
        </div>
      </Card>
    );
  }

  if (error) {
    // Show error state instead of hiding
    return (
      <Card className="p-4 bg-red-50 border-red-200 border">
        <div className="flex items-center gap-2 text-red-800">
          <AlertTriangle className="h-5 w-5" />
          <span className="text-sm">Unable to check material requirements. Please try again.</span>
        </div>
      </Card>
    );
  }

  if (!checkResult) {
    // Show info message if no check result
    return (
      <Card className="p-4 bg-blue-50 border-blue-200 border">
        <div className="flex items-center gap-2 text-blue-800">
          <Package className="h-5 w-5" />
          <span className="text-sm">Material requirements check not available for this order.</span>
        </div>
      </Card>
    );
  }

  // Show alert if materials need to be collected
  if (checkResult.requiresCollection && checkResult.missingMaterials.length > 0) {
    return (
      <Card className="p-4 bg-yellow-50 border-yellow-200 border-2">
        <div className="flex items-start gap-3">
          <AlertTriangle className="h-6 w-6 text-yellow-600 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <h3 className="font-semibold text-yellow-900 mb-2">Materials Required</h3>
            <p className="text-sm text-yellow-800 mb-3">
              {checkResult.message}
            </p>
            <div className="space-y-2">
              {checkResult.missingMaterials.map((material) => (
                <div key={material.materialId} className="bg-white rounded p-2 text-sm">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Package className="h-4 w-4 text-yellow-600" />
                      <span className="font-medium">{material.materialName}</span>
                      <span className="text-muted-foreground">({material.materialCode})</span>
                    </div>
                    <div className="text-right">
                      <span className="text-yellow-700 font-medium">
                        Need: {material.missingQuantity} {material.unitOfMeasure}
                      </span>
                      <span className="text-muted-foreground text-xs block">
                        Have: {material.availableQuantity} / Required: {material.requiredQuantity}
                      </span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
            <p className="text-xs text-yellow-700 mt-3">
              Please collect these materials from the warehouse before starting the job.
            </p>
          </div>
        </div>
      </Card>
    );
  }

  // Show success message if all materials are available
  if (requiredMaterials && requiredMaterials.length > 0 && !checkResult.requiresCollection) {
    return (
      <Card className="p-4 bg-green-50 border-green-200 border">
        <div className="flex items-center gap-3">
          <CheckCircle className="h-5 w-5 text-green-600" />
          <div>
            <h3 className="font-semibold text-green-900">All Materials Available</h3>
            <p className="text-sm text-green-700">
              You have all required materials for this job.
            </p>
          </div>
        </div>
      </Card>
    );
  }

  // If we have required materials but no check result (SI not assigned yet)
  if (requiredMaterials && requiredMaterials.length > 0 && !checkResult) {
    return (
      <Card className="p-4 bg-blue-50 border-blue-200 border">
        <div className="flex items-center gap-3">
          <Package className="h-5 w-5 text-blue-600" />
          <div>
            <h3 className="font-semibold text-blue-900">Materials Required</h3>
            <p className="text-sm text-blue-700">
              This order requires {requiredMaterials.length} material(s). Please collect from warehouse when assigned.
            </p>
          </div>
        </div>
      </Card>
    );
  }

  // If no required materials template configured
  if (!requiredMaterials || requiredMaterials.length === 0) {
    return (
      <Card className="p-4 bg-gray-50 border-gray-200 border">
        <div className="flex items-center gap-3">
          <Package className="h-5 w-5 text-gray-600" />
          <div>
            <h3 className="font-semibold text-gray-900">Material Requirements</h3>
            <p className="text-sm text-gray-700">
              No material template configured for this order type.
            </p>
          </div>
        </div>
      </Card>
    );
  }

  return null;
}

