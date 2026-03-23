import React, { useState, useEffect } from 'react';
import { Building2, AlertTriangle, X, Check, Info } from 'lucide-react';
import { Modal, Button, useToast } from '../ui';
import { createBuilding, getBuildings, type CreateBuildingRequest, type Building, PropertyType } from '../../api/buildings';
import { getBuildingTypes } from '../../api/buildingTypes';
import { getInstallationMethods } from '../../api/installationMethods';
import type { ReferenceDataItem } from '../../types/referenceData';
import type { InstallationMethod } from '../../types/installationMethods';
import { cn } from '../../lib/utils';

interface QuickBuildingModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialData?: {
    buildingName?: string;
    addressLine1?: string;
    addressLine2?: string;
    city?: string;
    state?: string;
    postcode?: string;
  };
  onBuildingCreated?: (buildingId: string) => void;
  onBuildingSelected?: (buildingId: string) => void;
  mode?: 'create-only' | 'create-and-approve'; // For parser flow
  draftId?: string; // For parser flow - to approve after building creation
}

/**
 * Quick Building Modal - For creating buildings on-the-fly during order creation/approval
 * 
 * Features:
 * - Pre-filled form from parsed address data
 * - Similar buildings search and selection
 * - One-click building creation
 * - Auto-detection of property type
 */
const QuickBuildingModal: React.FC<QuickBuildingModalProps> = ({
  isOpen,
  onClose,
  initialData = {},
  onBuildingCreated,
  onBuildingSelected,
  mode = 'create-only',
  draftId
}) => {
  const { showSuccess, showError } = useToast();
  const [loading, setLoading] = useState(false);
  const [searchingSimilar, setSearchingSimilar] = useState(false);
  const [similarBuildings, setSimilarBuildings] = useState<Building[]>([]);
  const [selectedSimilarBuilding, setSelectedSimilarBuilding] = useState<string | null>(null);

  // Form state (building classification from buildingTypeId; propertyType deprecated/optional)
  const [formData, setFormData] = useState<CreateBuildingRequest>({
    name: initialData.buildingName || '',
    addressLine1: initialData.addressLine1 || '',
    addressLine2: initialData.addressLine2 || '',
    city: initialData.city || '',
    state: initialData.state || '',
    postcode: initialData.postcode || '',
    buildingTypeId: undefined,
    isActive: true
  });

  // Dropdown options
  const [buildingTypes, setBuildingTypes] = useState<ReferenceDataItem[]>([]);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [loadingOptions, setLoadingOptions] = useState(true);

  // Load dropdown options
  useEffect(() => {
    if (isOpen) {
      loadOptions();
      searchSimilarBuildings();
    }
  }, [isOpen]);

  // Pre-fill form when initialData changes; suggest buildingTypeId from address/name
  useEffect(() => {
    if (!initialData) return;
    const detected = detectPropertyType(initialData);
    const suggestedName = suggestedBuildingTypeName(detected);
    setFormData(prev => ({
      ...prev,
      name: initialData.buildingName || prev.name,
      addressLine1: initialData.addressLine1 || prev.addressLine1,
      addressLine2: initialData.addressLine2 || prev.addressLine2,
      city: initialData.city || prev.city,
      state: initialData.state || prev.state,
      postcode: initialData.postcode || prev.postcode,
      ...(suggestedName && buildingTypes.length > 0
        ? { buildingTypeId: buildingTypes.find(t => t.name === suggestedName)?.id ?? prev.buildingTypeId }
        : {})
    }));
  }, [initialData]);

  // When building types load, apply suggested building type from initialData if we had a suggestion
  useEffect(() => {
    if (buildingTypes.length === 0 || !initialData?.buildingName && !initialData?.addressLine1) return;
    const detected = detectPropertyType(initialData);
    const suggestedName = suggestedBuildingTypeName(detected);
    if (!suggestedName || formData.buildingTypeId) return;
    const match = buildingTypes.find(t => t.name === suggestedName);
    if (match) setFormData(prev => ({ ...prev, buildingTypeId: match.id }));
  }, [buildingTypes.length]);

  const loadOptions = async () => {
    try {
      setLoadingOptions(true);
      const [types, methods] = await Promise.all([
        getBuildingTypes({ isActive: true }),
        getInstallationMethods({ isActive: true })
      ]);
      setBuildingTypes(types);
      setInstallationMethods(methods);
    } catch (err: any) {
      console.error('Failed to load options:', err);
      showError('Failed to load building options');
    } finally {
      setLoadingOptions(false);
    }
  };

  const searchSimilarBuildings = async () => {
    if (!formData.name && !formData.city && !formData.state) {
      setSimilarBuildings([]);
      return;
    }

    try {
      setSearchingSimilar(true);
      const filters: any = {};
      if (formData.city) filters.city = formData.city;
      if (formData.state) filters.state = formData.state;
      filters.isActive = true;

      const buildings = await getBuildings(filters);
      
      // Filter by similar name (case-insensitive, partial match)
      const nameLower = formData.name.toLowerCase().trim();
      const similar = buildings.filter(b => {
        const buildingNameLower = b.name.toLowerCase();
        return buildingNameLower.includes(nameLower) || nameLower.includes(buildingNameLower);
      });

      setSimilarBuildings(similar.slice(0, 5)); // Limit to 5
    } catch (err: any) {
      console.error('Failed to search similar buildings:', err);
      // Don't show error - this is a background search
    } finally {
      setSearchingSimilar(false);
    }
  };

  const detectPropertyType = (data: typeof initialData): PropertyType | null => {
    if (!data) return null;

    const name = (data.buildingName || '').toLowerCase();
    const address = (data.addressLine1 || '').toLowerCase();
    const combined = `${name} ${address}`;

    if (combined.includes('condo') || combined.includes('apartment') ||
        combined.includes('residence') || combined.includes('tower') ||
        combined.includes('plaza') || combined.includes('suite')) {
      return PropertyType.MDU;
    }
    if (combined.includes('house') || combined.includes('bungalow') ||
        combined.includes('villa') || combined.includes('terrace')) {
      return PropertyType.SDU;
    }
    if (combined.includes('shoplot') || combined.includes('shop') || combined.includes('lot')) {
      return PropertyType.Shoplot;
    }
    if (combined.includes('factory') || combined.includes('warehouse')) {
      return PropertyType.Factory;
    }
    if (combined.includes('office') || combined.includes('menara') ||
        (combined.includes('tower') && combined.includes('office'))) {
      return PropertyType.Office;
    }
    return null;
  };

  /** Map detected PropertyType to a suggested BuildingTypes name (seeded list). */
  const suggestedBuildingTypeName = (detected: PropertyType | null): string | null => {
    if (!detected) return null;
    switch (detected) {
      case PropertyType.MDU: return 'Condominium';
      case PropertyType.SDU: return 'Terrace House';
      case PropertyType.Shoplot: return 'Shop Office';
      case PropertyType.Factory: return 'Industrial';
      case PropertyType.Office: return 'Office Tower';
      default: return 'Other';
    }
  };

  const handleFieldChange = (field: keyof CreateBuildingRequest, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    
    // Re-search similar buildings when name/city/state changes
    if (field === 'name' || field === 'city' || field === 'state') {
      setTimeout(() => searchSimilarBuildings(), 500); // Debounce
    }
  };

  const handleSelectSimilar = (buildingId: string) => {
    setSelectedSimilarBuilding(buildingId);
    if (onBuildingSelected) {
      onBuildingSelected(buildingId);
    }
  };

  const handleCreateBuilding = async () => {
    if (!formData.name?.trim()) {
      showError('Building name is required');
      return;
    }
    if (!formData.addressLine1?.trim()) {
      showError('Address is required');
      return;
    }
    if (!formData.city?.trim()) {
      showError('City is required');
      return;
    }
    if (!formData.state?.trim()) {
      showError('State is required');
      return;
    }
    if (!formData.postcode?.trim()) {
      showError('Postcode is required');
      return;
    }

    const payload: CreateBuildingRequest = {
      ...formData,
      buildingTypeId: formData.buildingTypeId || undefined,
      propertyType: undefined
    };

    try {
      setLoading(true);
      const building = await createBuilding(payload);
      showSuccess(`Building '${building.name}' created successfully!`);
      
      if (onBuildingCreated) {
        onBuildingCreated(building.id);
      }
      
      // Close modal after creation
      onClose();
    } catch (err: any) {
      const errorMessage = err.message || 'Failed to create building';
      showError(errorMessage);
      
      // Check if it's a duplicate error
      if (errorMessage.toLowerCase().includes('already exists')) {
        // Re-search similar buildings to show the duplicate
        await searchSimilarBuildings();
      }
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setFormData({
      name: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      postcode: '',
      buildingTypeId: undefined,
      isActive: true
    });
    setSimilarBuildings([]);
    setSelectedSimilarBuilding(null);
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Quick Add Building"
      size="large"
    >
      <div className="space-y-4">
        {/* Info Banner */}
        <div className="p-3 rounded-lg bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800">
          <div className="flex items-start gap-2">
            <Info className="h-4 w-4 text-blue-600 dark:text-blue-400 flex-shrink-0 mt-0.5" />
            <div className="text-sm text-blue-800 dark:text-blue-300">
              <p className="font-medium mb-1">Building Address vs Unit Address</p>
              <p className="text-xs">
                Enter the <strong>building's street address</strong> (not the customer's unit number).
                Unit details will be stored separately in the order.
              </p>
            </div>
          </div>
        </div>

        {/* Similar Buildings Warning */}
        {similarBuildings.length > 0 && !selectedSimilarBuilding && (
          <div className="p-3 rounded-lg bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800">
            <div className="flex items-start gap-2 mb-2">
              <AlertTriangle className="h-4 w-4 text-yellow-600 dark:text-yellow-400 flex-shrink-0 mt-0.5" />
              <div className="flex-1">
                <p className="text-sm font-medium text-yellow-800 dark:text-yellow-300 mb-2">
                  Similar buildings found. Please check if any of these match:
                </p>
                <div className="space-y-2">
                  {similarBuildings.map(building => (
                    <button
                      key={building.id}
                      type="button"
                      onClick={() => handleSelectSimilar(building.id)}
                      className={cn(
                        "w-full text-left p-2 rounded border transition-colors",
                        "hover:bg-yellow-100 dark:hover:bg-yellow-900/30",
                        selectedSimilarBuilding === building.id
                          ? "border-yellow-500 bg-yellow-100 dark:bg-yellow-900/30"
                          : "border-yellow-300 dark:border-yellow-800"
                      )}
                    >
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="font-medium text-sm text-yellow-900 dark:text-yellow-200">
                            {building.name}
                          </p>
                          <p className="text-xs text-yellow-700 dark:text-yellow-400">
                            {building.addressLine1}, {building.city}, {building.state} {building.postcode}
                          </p>
                        </div>
                        {selectedSimilarBuilding === building.id && (
                          <Check className="h-4 w-4 text-yellow-600 dark:text-yellow-400" />
                        )}
                      </div>
                    </button>
                  ))}
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setSimilarBuildings([])}
                  className="mt-2 text-xs"
                >
                  None of these match - Create New Building
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Selected Building Confirmation */}
        {selectedSimilarBuilding && (
          <div className="p-3 rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800">
            <div className="flex items-center gap-2">
              <Check className="h-4 w-4 text-green-600 dark:text-green-400" />
              <p className="text-sm text-green-800 dark:text-green-300">
                Using existing building. Click "Confirm" to proceed.
              </p>
            </div>
          </div>
        )}

        {/* Form */}
        {!selectedSimilarBuilding && (
          <div className="space-y-4">
            <div>
              <label className="block text-xs font-medium mb-1">
                Building Name *
              </label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => handleFieldChange('name', e.target.value)}
                className="w-full px-3 py-2 text-sm border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                placeholder="e.g., ROYCE RESIDENCE"
                required
              />
            </div>

            <div>
              <label className="block text-xs font-medium mb-1">
                Street Address *
              </label>
              <input
                type="text"
                value={formData.addressLine1}
                onChange={(e) => handleFieldChange('addressLine1', e.target.value)}
                className="w-full px-3 py-2 text-sm border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                placeholder="e.g., Jalan Yap Kwan Seng"
                required
              />
            </div>

            <div>
              <label className="block text-xs font-medium mb-1">
                Address Line 2 (Optional)
              </label>
              <input
                type="text"
                value={formData.addressLine2 || ''}
                onChange={(e) => handleFieldChange('addressLine2', e.target.value)}
                className="w-full px-3 py-2 text-sm border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                placeholder="e.g., Taman Desa"
              />
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="block text-xs font-medium mb-1">
                  City *
                </label>
                <input
                  type="text"
                  value={formData.city}
                  onChange={(e) => handleFieldChange('city', e.target.value)}
                  className="w-full px-3 py-2 text-sm border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                  placeholder="e.g., Kuala Lumpur"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-medium mb-1">
                  State *
                </label>
                <input
                  type="text"
                  value={formData.state}
                  onChange={(e) => handleFieldChange('state', e.target.value)}
                  className="w-full px-3 py-2 text-sm border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                  placeholder="e.g., Wilayah Persekutuan"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-medium mb-1">
                  Postcode *
                </label>
                <input
                  type="text"
                  value={formData.postcode}
                  onChange={(e) => handleFieldChange('postcode', e.target.value)}
                  className="w-full px-3 py-2 text-sm border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                  placeholder="e.g., 50450"
                  required
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-medium mb-1">
                Building type
              </label>
              <select
                value={formData.buildingTypeId || ''}
                onChange={(e) => handleFieldChange('buildingTypeId', e.target.value || undefined)}
                className="w-full px-3 py-2 text-sm border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                disabled={loadingOptions}
              >
                <option value="">Select building type (optional)</option>
                {buildingTypes.map(type => (
                  <option key={type.id} value={type.id}>{type.name}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-medium mb-1">
                Installation Method (Optional)
              </label>
              <select
                value={formData.installationMethodId || ''}
                onChange={(e) => handleFieldChange('installationMethodId', e.target.value || undefined)}
                className="w-full px-3 py-2 text-sm border rounded-md focus:outline-none focus:ring-1 focus:ring-blue-500"
                disabled={loadingOptions}
              >
                <option value="">Select Installation Method</option>
                {installationMethods.map(method => (
                  <option key={method.id} value={method.id}>{method.name}</option>
                ))}
              </select>
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center justify-end gap-2 pt-4 border-t">
          <Button
            type="button"
            variant="outline"
            onClick={handleClose}
            disabled={loading}
          >
            Cancel
          </Button>
          {selectedSimilarBuilding ? (
            <Button
              type="button"
              onClick={() => {
                if (onBuildingSelected) {
                  onBuildingSelected(selectedSimilarBuilding);
                }
                handleClose();
              }}
            >
              Confirm
            </Button>
          ) : (
            <Button
              type="button"
              onClick={handleCreateBuilding}
              disabled={loading || loadingOptions}
            >
              {loading ? 'Creating...' : mode === 'create-and-approve' ? 'Create Building & Approve Order' : 'Create Building'}
            </Button>
          )}
        </div>
      </div>
    </Modal>
  );
};

export default QuickBuildingModal;

