import React, { useState, useEffect } from 'react';
import { Package, PlusCircle, Loader2, QrCode, X } from 'lucide-react';
import { Card, Button, TextInput, useToast } from '../ui';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { recordMaterialUsage, getRequiredMaterials, type RequiredMaterial, type RecordMaterialUsageRequest } from '../../api/orders';
import { getMaterialByBarcode } from '../../api/inventory';
import { useAuth } from '../../contexts/AuthContext';

interface NonSerialisedMaterialEntryProps {
  orderId: string;
  requiredMaterials: RequiredMaterial[];
}

export function NonSerialisedMaterialEntry({ orderId, requiredMaterials }: NonSerialisedMaterialEntryProps) {
  const { user } = useAuth();
  const { showSuccess, showError } = useToast();
  const queryClient = useQueryClient();

  // Filter to only non-serialised materials
  const nonSerialisedMaterials = requiredMaterials.filter(m => !m.isSerialised);

  const [selectedMaterialId, setSelectedMaterialId] = useState<string>('');
  const [quantity, setQuantity] = useState<string>('1');
  const [notes, setNotes] = useState<string>('');
  const [scanning, setScanning] = useState(false);
  const [scannedMaterial, setScannedMaterial] = useState<{ name: string; code?: string; barcode?: string } | null>(null);
  const [scannerId] = useState(`non-serialised-scanner-${Date.now()}`);

  const recordUsageMutation = useMutation({
    mutationFn: async (data: RecordMaterialUsageRequest) => {
      return recordMaterialUsage(orderId, data);
    },
    onSuccess: () => {
      showSuccess('Material recorded successfully!');
      setSelectedMaterialId('');
      setQuantity('1');
      setNotes('');
      queryClient.invalidateQueries({ queryKey: ['orderMaterials', orderId] });
      queryClient.invalidateQueries({ queryKey: ['materialUsage', orderId] });
      queryClient.invalidateQueries({ queryKey: ['jobDetails', orderId] });
    },
    onError: (err: any) => {
      showError(err.message || 'Failed to record material usage.');
    },
  });

  const handleAddMaterial = async () => {
    if (!selectedMaterialId) {
      showError('Please select a material');
      return;
    }

    const qty = parseFloat(quantity);
    if (isNaN(qty) || qty <= 0) {
      showError('Please enter a valid quantity');
      return;
    }

    const selectedMaterial = nonSerialisedMaterials.find(m => m.materialId === selectedMaterialId);
    if (!selectedMaterial) {
      showError('Selected material not found');
      return;
    }

    recordUsageMutation.mutate({
      materialId: selectedMaterialId,
      quantity: qty,
      notes: notes.trim() || undefined,
    });
  };

  const handleStartScan = async () => {
    try {
      setScanning(true);
      setScannedMaterial(null);

      // Dynamically import html5-qrcode
      const { Html5Qrcode } = await import('html5-qrcode');
      
      const html5QrCode = new Html5Qrcode(scannerId);

      await html5QrCode.start(
        { facingMode: 'environment' },
        {
          fps: 10,
          qrbox: { width: 250, height: 250 }
        },
        async (decodedText: string) => {
          // Successfully scanned barcode
          try {
            // Stop scanner
            await html5QrCode.stop();
            await html5QrCode.clear();
            setScanning(false);

            // Look up material by barcode
            const material = await getMaterialByBarcode(decodedText);
            
            if (!material) {
              showError(`Material with barcode '${decodedText}' not found`);
              setScannedMaterial(null);
              return;
            }

            // Check if material is in the required materials list
            const matchingRequiredMaterial = nonSerialisedMaterials.find(
              m => m.materialId === material.id
            );

            if (!matchingRequiredMaterial) {
              showError(`Material '${material.name}' is not required for this order`);
              setScannedMaterial({
                name: material.name,
                code: material.code,
                barcode: material.barcode || decodedText
              });
              return;
            }

            // Auto-select the material
            setSelectedMaterialId(material.id);
            setScannedMaterial({
              name: material.name,
              code: material.code,
              barcode: material.barcode || decodedText
            });
            showSuccess(`Material '${material.name}' selected successfully!`);
          } catch (err: any) {
            console.error('Error looking up material:', err);
            showError(err.message || 'Failed to look up material by barcode');
            setScanning(false);
          }
        },
        (errorMessage: string) => {
          // Ignore scanning errors (they're frequent during scanning)
        }
      );
    } catch (err: any) {
      console.error('Error starting scanner:', err);
      showError(err.message || 'Failed to start camera. Please check permissions.');
      setScanning(false);
    }
  };

  const handleStopScan = async () => {
    try {
      const { Html5Qrcode } = await import('html5-qrcode');
      const html5QrCode = new Html5Qrcode(scannerId);
      await html5QrCode.stop();
      await html5QrCode.clear();
    } catch (err) {
      // Ignore errors when stopping
    }
    setScanning(false);
  };

  // Cleanup scanner on unmount
  useEffect(() => {
    return () => {
      if (scanning) {
        handleStopScan().catch(() => {});
      }
    };
  }, [scanning]);

  if (nonSerialisedMaterials.length === 0) {
    return null; // Don't show component if no non-serialised materials
  }

  return (
    <Card className="p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-semibold text-lg flex items-center gap-2">
          <Package className="h-5 w-5 text-primary" />
          Add Non-Serialised Materials
        </h3>
      </div>

      <div className="space-y-3">
        <div>
          <div className="flex items-center justify-between mb-1">
            <label className="block text-sm font-medium">Material</label>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={scanning ? handleStopScan : handleStartScan}
              disabled={recordUsageMutation.isPending}
              className="h-8 px-2 gap-1"
            >
              {scanning ? (
                <>
                  <X className="h-3.5 w-3.5" />
                  Stop Scan
                </>
              ) : (
                <>
                  <QrCode className="h-3.5 w-3.5" />
                  Scan Barcode
                </>
              )}
            </Button>
          </div>
          {scanning && (
            <div className="mb-2">
              <div id={scannerId} className="w-full max-w-xs mx-auto rounded-md overflow-hidden" />
              <p className="text-xs text-muted-foreground text-center mt-1">
                Point camera at barcode to scan
              </p>
            </div>
          )}
          {scannedMaterial && !scanning && (
            <div className="mb-2 p-2 bg-primary/10 border border-primary/20 rounded-md">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-primary">{scannedMaterial.name}</p>
                  {scannedMaterial.code && (
                    <p className="text-xs text-muted-foreground">Code: {scannedMaterial.code}</p>
                  )}
                  {scannedMaterial.barcode && (
                    <p className="text-xs text-muted-foreground">Barcode: {scannedMaterial.barcode}</p>
                  )}
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setScannedMaterial(null)}
                  className="h-6 w-6 p-0"
                >
                  <X className="h-3 w-3" />
                </Button>
              </div>
            </div>
          )}
          <select
            value={selectedMaterialId}
            onChange={(e) => {
              setSelectedMaterialId(e.target.value);
              setScannedMaterial(null); // Clear scanned material when manually selecting
            }}
            disabled={recordUsageMutation.isPending || scanning}
            className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary min-h-[44px] text-base"
          >
            <option value="">Select material...</option>
            {nonSerialisedMaterials.map((material) => (
              <option key={material.materialId} value={material.materialId}>
                {material.materialName} ({material.materialCode}) - Required: {material.quantity} {material.unitOfMeasure}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Quantity</label>
          <TextInput
            type="number"
            min="0.01"
            step="0.01"
            placeholder="Enter quantity"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            disabled={recordUsageMutation.isPending}
          />
          {selectedMaterialId && (
            <p className="text-xs text-muted-foreground mt-1">
              Unit: {nonSerialisedMaterials.find(m => m.materialId === selectedMaterialId)?.unitOfMeasure || 'pcs'}
            </p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Notes (Optional)</label>
          <TextInput
            placeholder="Optional notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            disabled={recordUsageMutation.isPending}
          />
        </div>

        <Button
          onClick={handleAddMaterial}
          disabled={recordUsageMutation.isPending || !selectedMaterialId || !quantity}
          className="w-full flex items-center gap-2"
        >
          {recordUsageMutation.isPending ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <PlusCircle className="h-4 w-4" />
          )}
          {recordUsageMutation.isPending ? 'Recording...' : 'Add Material'}
        </Button>
      </div>
    </Card>
  );
}

