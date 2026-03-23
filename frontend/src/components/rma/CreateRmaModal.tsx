import React, { useState, useEffect } from 'react';
import { X, Package, AlertTriangle, CheckCircle } from 'lucide-react';
import { Button, Card, useToast, LoadingSpinner, Label, Textarea, Select } from '../ui';
import { createRmaFromOrder } from '../../api/rma';
import { getOrderMaterials, getMaterialUsage } from '../../api/orders';
import { getPartners } from '../../api/partners';
import type { CreateRmaRequest } from '../../api/rma';

interface SerialisedItem {
  id: string;
  serialNumber: string;
  materialId: string;
  materialName?: string;
  status?: string;
  orderId?: string;
}

interface Partner {
  id: string;
  name: string;
  code?: string;
}

interface CreateRmaModalProps {
  orderId: string;
  partnerId?: string;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const CreateRmaModal: React.FC<CreateRmaModalProps> = ({
  orderId,
  partnerId: initialPartnerId,
  isOpen,
  onClose,
  onSuccess
}) => {
  const { showSuccess, showError } = useToast();
  const [loading, setLoading] = useState(false);
  const [loadingMaterials, setLoadingMaterials] = useState(true);
  const [selectedSerialisedItems, setSelectedSerialisedItems] = useState<Set<string>>(new Set());
  const [serialisedItems, setSerialisedItems] = useState<SerialisedItem[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [selectedPartnerId, setSelectedPartnerId] = useState<string>(initialPartnerId || '');
  const [reason, setReason] = useState('');
  const [notes, setNotes] = useState('');

  useEffect(() => {
    if (isOpen && orderId) {
      loadData();
    } else {
      // Reset form when modal closes
      setSelectedSerialisedItems(new Set());
      setReason('');
      setNotes('');
      setSelectedPartnerId(initialPartnerId || '');
    }
  }, [isOpen, orderId, initialPartnerId]);

  const loadData = async () => {
    try {
      setLoadingMaterials(true);
      
      // Load partners
      const partnersData = await getPartners();
      setPartners(Array.isArray(partnersData) ? partnersData : []);

      // Load order material usage to find serialised items
      const materialUsageData = await getMaterialUsage(orderId);
      
      // Extract serialised items from material usage
      const serialised: SerialisedItem[] = [];
      
      if (Array.isArray(materialUsageData)) {
        materialUsageData.forEach((usage: any) => {
          // Only include items with serial numbers (serialised items)
          if (usage.serialisedItemId && usage.serialNumber) {
            serialised.push({
              id: usage.serialisedItemId,
              serialNumber: usage.serialNumber,
              materialId: usage.materialId,
              materialName: usage.materialName || 'Unknown Material',
              status: 'InstalledAtCustomer', // Default status for items on order
              orderId: orderId
            });
          }
        });
      }
      
      // If no serialised items found in usage, try loading from materials as fallback
      if (serialised.length === 0) {
        const materialsData = await getOrderMaterials(orderId);
        if (Array.isArray(materialsData)) {
          materialsData.forEach((material: any) => {
            if (material.serialNumber && material.serialisedItemId) {
              serialised.push({
                id: material.serialisedItemId,
                serialNumber: material.serialNumber,
                materialId: material.materialId || material.id,
                materialName: material.materialName || material.description || 'Unknown Material',
                status: 'InstalledAtCustomer',
                orderId: orderId
              });
            }
          });
        }
      }

      setSerialisedItems(serialised);
    } catch (error) {
      console.error('Error loading data for RMA modal:', error);
      showError('Failed to load materials data');
    } finally {
      setLoadingMaterials(false);
    }
  };

  const handleToggleItem = (itemId: string) => {
    const newSelected = new Set(selectedSerialisedItems);
    if (newSelected.has(itemId)) {
      newSelected.delete(itemId);
    } else {
      newSelected.add(itemId);
    }
    setSelectedSerialisedItems(newSelected);
  };

  const handleSubmit = async () => {
    if (!selectedPartnerId) {
      showError('Please select a partner');
      return;
    }

    if (selectedSerialisedItems.size === 0) {
      showError('Please select at least one serialised item');
      return;
    }

    if (!reason.trim()) {
      showError('Please provide a reason for the RMA');
      return;
    }

    try {
      setLoading(true);

      // Get selected items
      const selectedItems = serialisedItems.filter(item => selectedSerialisedItems.has(item.id));
      
      // Create RMA request
      const rmaData: CreateRmaRequest = {
        partnerId: selectedPartnerId,
        orderId: orderId,
        items: selectedItems.map(item => ({
          serialisedItemId: item.id,
          originalOrderId: orderId,
          notes: notes.trim() || undefined
        })),
        reason: reason.trim()
      };

      await createRmaFromOrder(orderId, rmaData);
      
      showSuccess('RMA request created successfully');
      onSuccess();
      onClose();
    } catch (error: any) {
      console.error('Error creating RMA request:', error);
      showError(error?.message || 'Failed to create RMA request');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-background rounded-lg shadow-lg w-full max-w-3xl max-h-[90vh] overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b">
          <div className="flex items-center gap-3">
            <AlertTriangle className="h-6 w-6 text-orange-500" />
            <h2 className="text-xl font-semibold">Create RMA Request</h2>
          </div>
          <button
            onClick={onClose}
            className="text-muted-foreground hover:text-foreground transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6 space-y-6">
          {loadingMaterials ? (
            <div className="flex justify-center items-center py-8">
              <LoadingSpinner />
            </div>
          ) : (
            <>
              {/* Partner Selection */}
              <div>
                <Label htmlFor="partner">Partner *</Label>
                <Select
                  id="partner"
                  value={selectedPartnerId}
                  onChange={(e) => setSelectedPartnerId(e.target.value)}
                  className="mt-1"
                >
                  <option value="">Select a partner</option>
                  {partners.map((partner) => (
                    <option key={partner.id} value={partner.id}>
                      {partner.name} {partner.code ? `(${partner.code})` : ''}
                    </option>
                  ))}
                </Select>
              </div>

              {/* Serialised Items Selection */}
              <div>
                <Label>Select Serialised Items *</Label>
                {serialisedItems.length === 0 ? (
                  <Card className="mt-2 p-4">
                    <div className="flex items-center gap-2 text-muted-foreground">
                      <Package className="h-5 w-5" />
                      <p>No serialised items found for this order.</p>
                    </div>
                  </Card>
                ) : (
                  <div className="mt-2 space-y-2 max-h-64 overflow-y-auto">
                    {serialisedItems.map((item) => {
                      const isSelected = selectedSerialisedItems.has(item.id);
                      return (
                        <Card
                          key={item.id}
                          className={`p-4 cursor-pointer transition-colors ${
                            isSelected
                              ? 'border-primary bg-primary/5'
                              : 'hover:bg-muted/50'
                          }`}
                          onClick={() => handleToggleItem(item.id)}
                        >
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              <div
                                className={`w-5 h-5 rounded border-2 flex items-center justify-center ${
                                  isSelected
                                    ? 'border-primary bg-primary'
                                    : 'border-muted-foreground'
                                }`}
                              >
                                {isSelected && (
                                  <CheckCircle className="h-4 w-4 text-primary-foreground" />
                                )}
                              </div>
                              <div>
                                <p className="font-medium">
                                  {item.materialName || 'Unknown Material'}
                                </p>
                                <p className="text-sm text-muted-foreground">
                                  Serial: {item.serialNumber}
                                </p>
                                {item.status && (
                                  <span className="text-xs px-2 py-0.5 rounded bg-muted mt-1 inline-block">
                                    {item.status}
                                  </span>
                                )}
                              </div>
                            </div>
                          </div>
                        </Card>
                      );
                    })}
                  </div>
                )}
              </div>

              {/* Reason */}
              <div>
                <Label htmlFor="reason">Reason *</Label>
                <Textarea
                  id="reason"
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  placeholder="Enter reason for RMA request..."
                  className="mt-1"
                  rows={3}
                />
              </div>

              {/* Notes */}
              <div>
                <Label htmlFor="notes">Notes (Optional)</Label>
                <Textarea
                  id="notes"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="Additional notes..."
                  className="mt-1"
                  rows={2}
                />
              </div>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end gap-3 p-6 border-t">
          <Button variant="outline" onClick={onClose} disabled={loading}>
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={loading || loadingMaterials || selectedSerialisedItems.size === 0 || !selectedPartnerId || !reason.trim()}
          >
            {loading ? 'Creating...' : 'Create RMA Request'}
          </Button>
        </div>
      </div>
    </div>
  );
};

