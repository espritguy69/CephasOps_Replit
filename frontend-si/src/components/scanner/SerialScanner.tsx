import React, { useState } from 'react';
import { QrCode, PlusCircle, MapPin, Loader2 } from 'lucide-react';
import { Button, Card, TextInput, useToast } from '../ui';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { recordDeviceScan } from '../../api/si-app';
import { recordMaterialUsage, getRequiredMaterials, type RequiredMaterial, type RecordMaterialUsageRequest } from '../../api/orders';
import { useAuth } from '../../contexts/AuthContext';
import type { Location } from '../../types/api';

interface SerialScannerProps {
  orderId: string;
  sessionId: string;
  existingScans?: any[];
}

const getCurrentLocation = (): Promise<Location | null> => {
  return new Promise((resolve) => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          resolve({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
          });
        },
        (error) => {
          console.warn('Error getting location for scan:', error);
          resolve(null);
        },
        { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
      );
    } else {
      resolve(null);
    }
  });
};

export function SerialScanner({ orderId, sessionId, existingScans = [] }: SerialScannerProps) {
  const { user, serviceInstaller } = useAuth();
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();

  const [serialNumber, setSerialNumber] = useState('');
  const [deviceType, setDeviceType] = useState('');
  const [selectedMaterialId, setSelectedMaterialId] = useState<string>('');
  const [scans, setScans] = useState(existingScans);

  // Fetch required materials to filter serialised ones
  const { data: requiredMaterials } = useQuery({
    queryKey: ['requiredMaterials', orderId],
    queryFn: () => getRequiredMaterials(orderId),
    enabled: !!orderId,
  });

  // Filter to only serialised materials
  const serialisedMaterials = requiredMaterials?.filter(m => m.isSerialised) || [];

  const recordScanMutation = useMutation({
    mutationFn: async (scanData: { serialNumber: string; materialId: string; location: Location | null }) => {
      if (!user?.id) {
        throw new Error('Authentication context missing.');
      }

      // Use the new material usage API instead of device scan
      return recordMaterialUsage(orderId, {
        materialId: scanData.materialId,
        serialNumber: scanData.serialNumber,
        quantity: 1, // Serialised materials always have quantity 1
      });
    },
    onSuccess: (data) => {
      showSuccess('Serial number recorded successfully!');
      setSerialNumber('');
      setSelectedMaterialId('');
      setScans((prev) => [...prev, { id: data.id, serialNumber: data.serialNumber, materialName: data.materialName }]);
      queryClient.invalidateQueries({ queryKey: ['jobDetails', orderId] });
      queryClient.invalidateQueries({ queryKey: ['materialUsage', orderId] });
      queryClient.invalidateQueries({ queryKey: ['orderMaterials', orderId] });
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to record serial number.');
    },
  });

  const handleAddScan = async () => {
    if (!serialNumber.trim()) {
      showError('Serial number cannot be empty.');
      return;
    }

    if (!selectedMaterialId) {
      showError('Please select a material.');
      return;
    }

    // Verify selected material is serialised (double-check)
    const selectedMaterial = serialisedMaterials.find(m => m.materialId === selectedMaterialId);
    if (!selectedMaterial || !selectedMaterial.isSerialised) {
      showError('Only serialised materials can be scanned. Please select a serialised material.');
      return;
    }

    const location = await getCurrentLocation();

    recordScanMutation.mutate({
      serialNumber: serialNumber.trim(),
      materialId: selectedMaterialId,
      location,
    });
  };

  // Don't show component if no serialised materials
  if (serialisedMaterials.length === 0) {
    return null;
  }

  return (
    <Card className="p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-semibold text-lg flex items-center gap-2">
          <QrCode className="h-5 w-5 text-primary" />
          Device Scanning
        </h3>
      </div>

      <div className="space-y-3 mb-4">
        <div>
          <label className="block text-sm font-medium mb-1">Material</label>
          <select
            value={selectedMaterialId}
            onChange={(e) => setSelectedMaterialId(e.target.value)}
            disabled={recordScanMutation.isPending}
            className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
          >
            <option value="">Select material...</option>
            {serialisedMaterials.map((material) => (
              <option key={material.materialId} value={material.materialId}>
                {material.materialName} ({material.materialCode})
              </option>
            ))}
          </select>
        </div>
        <TextInput
          label="Serial Number"
          placeholder="Enter or scan serial number"
          value={serialNumber}
          onChange={(e) => setSerialNumber(e.target.value)}
          disabled={recordScanMutation.isPending || !selectedMaterialId}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              handleAddScan();
            }
          }}
        />
        <Button
          onClick={handleAddScan}
          disabled={recordScanMutation.isPending || !serialNumber.trim() || !selectedMaterialId}
          className="w-full flex items-center gap-2"
        >
          {recordScanMutation.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <PlusCircle className="h-4 w-4" />
          )}
          {recordScanMutation.isPending ? 'Recording...' : 'Record Serial'}
        </Button>
      </div>

      {scans.length > 0 && (
        <div>
          <h4 className="font-medium mb-2">Recorded Serials:</h4>
          <ul className="space-y-2 text-sm">
            {scans.map((scan, index) => (
              <li key={scan.id || index} className="flex items-center justify-between p-2 bg-muted rounded-md">
                <span>
                  <span className="font-semibold">{scan.serialNumber}</span>
                  {scan.materialName && <span className="text-muted-foreground"> - {scan.materialName}</span>}
                </span>
                {scan.location && (
                  <MapPin className="h-4 w-4 text-muted-foreground" title={`Lat: ${scan.location.latitude}, Lon: ${scan.location.longitude}`} />
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </Card>
  );
}

