import React, { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Building2,
  Save,
  Power,
  MapPin,
  Calendar,
  Users,
  FileText,
  Boxes,
  Plus,
  Edit,
  Trash2,
  User,
  Phone,
  Mail,
  Star,
  Layers,
  Network,
  AlertTriangle,
  RefreshCw,
  Package
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import {
  getBuilding,
  updateBuilding,
  createBuilding,
  getBuildingContacts,
  getBuildingRules,
  createBuildingContact,
  updateBuildingContact,
  deleteBuildingContact,
  saveBuildingRules,
  PropertyTypeLabels,
  ContactRoles
} from '../../api/buildings';
import {
  getBuildingInfrastructure,
  createBuildingBlock,
  updateBuildingBlock,
  deleteBuildingBlock,
  createBuildingSplitter,
  updateBuildingSplitter,
  deleteBuildingSplitter,
  createStreet,
  updateStreet,
  deleteStreet,
  createHubBox,
  updateHubBox,
  deleteHubBox,
  createPole,
  updatePole,
  deletePole,
  SplitterStatusLabels,
  PoleStatusLabels,
  PoleTypes
} from '../../api/infrastructure';
import { getInstallationMethods } from '../../api/installationMethods';
import { getDepartments } from '../../api/departments';
import { getSplitterTypes } from '../../api/splitterTypes';
import { getOrderTypes } from '../../api/orderTypes';
import { getMaterials } from '../../api/inventory';
import { 
  getBuildingDefaultMaterials, 
  createBuildingDefaultMaterial, 
  updateBuildingDefaultMaterial, 
  deleteBuildingDefaultMaterial 
} from '../../api/buildingDefaultMaterials';
import { LoadingSpinner, useToast, Button, Card, TextInput, Modal } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { cn } from '@/lib/utils';
import type { Department } from '../../types/departments';
import type { InstallationMethod } from '../../types/installationMethods';
import type { Material } from '../../types/inventory';
import type { Building as BuildingDto } from '../../types/buildings';
import { HubBoxStatus } from '../../types/infrastructure';
import type { SplitterStatus, PoleStatus, PoleType } from '../../types/infrastructure';
import type { BuildingDefaultMaterial } from '../../types/buildingDefaultMaterials';

type TabId = 'general' | 'contacts' | 'rules' | 'infrastructure' | 'materials';

interface TabConfig {
  id: TabId;
  label: string;
  icon: LucideIcon;
}

interface BuildingFormData {
  name: string;
  code: string;
  propertyType: string;
  installationMethodId: string;
  departmentId: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  postcode: string;
  area: string;
  latitude: string;
  longitude: string;
  rfbAssignedDate: string;
  firstOrderDate: string;
  notes: string;
  isActive: boolean;
}

interface ContactFormData {
  role: string;
  name: string;
  phone: string;
  email: string;
  remarks: string;
  isPrimary: boolean;
  isActive: boolean;
}

type BuildingContactRecord = ContactFormData & { id: string };

interface RulesFormData {
  accessRules: string;
  installationRules: string;
  otherNotes: string;
}

interface BlockFormData {
  name: string;
  code: string;
  floors: number | string;
  unitsPerFloor: number | string;
  totalUnits: number | string;
  displayOrder: number;
  isActive: boolean;
  notes: string;
}

interface SplitterFormData {
  blockId: string;
  splitterTypeId: string;
  name: string;
  floor: string;
  locationDescription: string;
  portsTotal: number | string;
  portsUsed: number | string;
  status: SplitterStatus | string;
  serialNumber: string;
  remarks: string;
  needsAttention: boolean;
}

interface StreetFormData {
  name: string;
  code: string;
  section: string;
  displayOrder: number;
  isActive: boolean;
}

interface HubBoxFormData {
  streetId: string;
  name: string;
  code: string;
  locationDescription: string;
  latitude: string;
  longitude: string;
  portsTotal: number | string;
  portsUsed: number | string;
  status: HubBoxStatus | string;
  remarks: string;
  isActive: boolean;
}

interface PoleFormData {
  streetId: string;
  poleNumber: string;
  poleType: PoleType | '';
  locationDescription: string;
  latitude: string;
  longitude: string;
  heightMeters: string;
  hasExistingFibre: boolean;
  dropsCount: number;
  status: PoleStatus | string;
  remarks: string;
  isActive: boolean;
}

type BuildingRecord = Partial<BuildingDto> & {
  area?: string;
  latitude?: string | number;
  longitude?: string | number;
  rfbAssignedDate?: string;
  firstOrderDate?: string;
};

interface InfrastructureBlock {
  id: string;
  name: string;
  code?: string;
  floors?: number;
  splittersCount?: number;
  unitsPerFloor?: number;
  totalUnits?: number;
  displayOrder?: number;
  isActive?: boolean;
  notes?: string;
}

interface InfrastructureSplitter {
  id: string;
  name: string;
  blockId?: string;
  blockName?: string;
  floor?: string;
  portsTotal: number;
  portsUsed: number;
  status: SplitterStatus | string;
  serialNumber?: string;
  locationDescription?: string;
  remarks?: string;
  needsAttention?: boolean;
}

interface InfrastructureStreet {
  id: string;
  name: string;
  hubBoxesCount?: number;
  polesCount?: number;
  code?: string;
  section?: string;
  displayOrder?: number;
  isActive?: boolean;
}

interface InfrastructureHubBox {
  id: string;
  streetId?: string;
  streetName?: string;
  name: string;
  code?: string;
  locationDescription?: string;
  latitude?: string;
  longitude?: string;
  portsTotal: number;
  portsUsed: number;
  status: HubBoxStatus | string;
  remarks?: string;
  isActive?: boolean;
}

interface InfrastructurePole {
  id: string;
  streetId?: string;
  streetName?: string;
  poleNumber: string;
  poleType?: PoleType | string;
  locationDescription?: string;
  latitude?: string;
  longitude?: string;
  heightMeters?: string | number;
  hasExistingFibre?: boolean;
  dropsCount: number;
  status: PoleStatus | string;
  remarks?: string;
  isActive?: boolean;
}

interface InfrastructureData {
  totalBlocks?: number;
  totalStreets?: number;
  totalSplitters?: number;
  totalHubBoxes?: number;
  totalSplitterPorts?: number;
  usedSplitterPorts?: number;
  totalHubBoxPorts?: number;
  usedHubBoxPorts?: number;
  blocks?: InfrastructureBlock[];
  splitters?: InfrastructureSplitter[];
  streets?: InfrastructureStreet[];
  hubBoxes?: InfrastructureHubBox[];
  poles?: InfrastructurePole[];
}

interface OrderTypeOption {
  id: string;
  name: string;
  code?: string;
}

interface SplitterTypeOption {
  id: string;
  name: string;
}

type DefaultMaterialItem = BuildingDefaultMaterial & {
  materialCode?: string;
  materialDescription?: string;
  materialUnitOfMeasure?: string;
};

interface DefaultMaterialFormData {
  orderTypeId: string;
  materialId: string;
  defaultQuantity: string;
  notes: string;
}

type ExtendedMaterial = Material & { itemCode?: string };

interface StatusBadgeProps {
  isActive: boolean;
  onClick: () => void;
}

interface CapacityBarProps {
  used: number;
  total: number;
  showLabel?: boolean;
}

interface SplitterStatusBadgeProps {
  status: string;
}

// Tab definitions
const TABS: TabConfig[] = [
  { id: 'general', label: 'General', icon: Building2 },
  { id: 'contacts', label: 'Contacts & Maintenance', icon: Users },
  { id: 'rules', label: 'House Rules', icon: FileText },
  { id: 'infrastructure', label: 'Infrastructure', icon: Boxes },
  { id: 'materials', label: 'Default Materials', icon: Package },
];

const HUB_BOX_STATUS_OPTIONS = Object.values(HubBoxStatus);

// Status badge
const StatusBadge: React.FC<StatusBadgeProps> = ({ isActive, onClick }) => (
  <button
    onClick={onClick}
    className={cn(
      "flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium transition-colors cursor-pointer",
      isActive 
        ? "bg-emerald-100 text-emerald-700 hover:bg-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-400"
        : "bg-gray-100 text-gray-600 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-400"
    )}
  >
    <Power className="h-3 w-3" />
    {isActive ? 'Active' : 'Inactive'}
  </button>
);

// Capacity bar component
const CapacityBar: React.FC<CapacityBarProps> = ({ used, total, showLabel = true }) => {
  const percent = total > 0 ? (used / total) * 100 : 0;
  const getColor = () => {
    if (percent >= 100) return 'bg-red-500';
    if (percent >= 80) return 'bg-amber-500';
    return 'bg-emerald-500';
  };
  
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden">
        <div className={cn("h-full transition-all", getColor())} style={{ width: `${Math.min(percent, 100)}%` }} />
      </div>
      {showLabel && (
        <span className={cn("text-xs font-medium", percent >= 100 ? "text-red-500" : "text-muted-foreground")}>
          {used}/{total}
        </span>
      )}
    </div>
  );
};

// Splitter status badge
const SplitterStatusBadge: React.FC<SplitterStatusBadgeProps> = ({ status }) => {
  const colors = {
    Active: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
    Full: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
    Faulty: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
    MaintenanceRequired: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
    Decommissioned: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'
  };
  return (
    <span className={cn("px-2 py-0.5 rounded text-[10px] font-medium", colors[status] || colors.Active)}>
      {SplitterStatusLabels[status] || status}
    </span>
  );
};

const BuildingDetailPage: React.FC = () => {
  const params = useParams<{ id: string }>();
  const id = params.id ?? '';
  const navigate = useNavigate();
  const { showSuccess, showError } = useToast();
  const isNew = id === 'new';
  
  const [loading, setLoading] = useState<boolean>(!isNew);
  const [saving, setSaving] = useState<boolean>(false);
  const [activeTab, setActiveTab] = useState<TabId>('general');
  const [building, setBuilding] = useState<BuildingRecord | null>(null);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [splitterTypes, setSplitterTypes] = useState<SplitterTypeOption[]>([]);
  
  // Form state for General tab
  const [formData, setFormData] = useState<BuildingFormData>({
    name: '',
    code: '',
    propertyType: '',
    installationMethodId: '',
    departmentId: '',
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    postcode: '',
    area: '',
    latitude: '',
    longitude: '',
    rfbAssignedDate: '',
    firstOrderDate: '',
    notes: '',
    isActive: true
  });
  
  // Contacts state
  const [contacts, setContacts] = useState<BuildingContactRecord[]>([]);
  const [contactModalOpen, setContactModalOpen] = useState<boolean>(false);
  const [editingContact, setEditingContact] = useState<BuildingContactRecord | null>(null);
  const [contactForm, setContactForm] = useState<ContactFormData>({
    role: '',
    name: '',
    phone: '',
    email: '',
    remarks: '',
    isPrimary: false,
    isActive: true
  });
  
  // Rules state
  const [rules, setRules] = useState<RulesFormData>({
    accessRules: '',
    installationRules: '',
    otherNotes: ''
  });
  const [rulesChanged, setRulesChanged] = useState<boolean>(false);

  // Infrastructure state
  const [infrastructure, setInfrastructure] = useState<InfrastructureData | null>(null);
  const [infraLoading, setInfraLoading] = useState<boolean>(false);
  
  // Block modal
  const [blockModalOpen, setBlockModalOpen] = useState<boolean>(false);
  const [editingBlock, setEditingBlock] = useState<InfrastructureBlock | null>(null);
  const [blockForm, setBlockForm] = useState<BlockFormData>({
    name: '', code: '', floors: 1, unitsPerFloor: '', totalUnits: '', displayOrder: 0, isActive: true, notes: ''
  });
  
  // Splitter modal
  const [splitterModalOpen, setSplitterModalOpen] = useState<boolean>(false);
  const [editingSplitter, setEditingSplitter] = useState<InfrastructureSplitter | null>(null);
  const [splitterForm, setSplitterForm] = useState<SplitterFormData>({
    blockId: '', splitterTypeId: '', name: '', floor: '', locationDescription: '',
    portsTotal: 8, portsUsed: 0, status: 'Active', serialNumber: '', remarks: '', needsAttention: false
  });
  
  // Street modal
  const [streetModalOpen, setStreetModalOpen] = useState<boolean>(false);
  const [editingStreet, setEditingStreet] = useState<InfrastructureStreet | null>(null);
  const [streetForm, setStreetForm] = useState<StreetFormData>({
    name: '', code: '', section: '', displayOrder: 0, isActive: true
  });
  
  // Hub Box modal
  const [hubBoxModalOpen, setHubBoxModalOpen] = useState<boolean>(false);
  const [editingHubBox, setEditingHubBox] = useState<InfrastructureHubBox | null>(null);
  const [hubBoxForm, setHubBoxForm] = useState<HubBoxFormData>({
    streetId: '', name: '', code: '', locationDescription: '', latitude: '', longitude: '',
    portsTotal: 8, portsUsed: 0, status: 'Active', remarks: '', isActive: true
  });
  
  // Pole modal
  const [poleModalOpen, setPoleModalOpen] = useState<boolean>(false);
  const [editingPole, setEditingPole] = useState<InfrastructurePole | null>(null);
  const [poleForm, setPoleForm] = useState<PoleFormData>({
    streetId: '', poleNumber: '', poleType: '', locationDescription: '', latitude: '', longitude: '',
    heightMeters: '', hasExistingFibre: false, dropsCount: 0, status: 'Good', remarks: '', isActive: true
  });

  // Default Materials state
  const [defaultMaterials, setDefaultMaterials] = useState<DefaultMaterialItem[]>([]);
  const [orderTypes, setOrderTypes] = useState<OrderTypeOption[]>([]);
  const [availableMaterials, setAvailableMaterials] = useState<ExtendedMaterial[]>([]);
  const [materialsLoading, setMaterialsLoading] = useState<boolean>(false);
  const [materialModalOpen, setMaterialModalOpen] = useState<boolean>(false);
  const [editingMaterial, setEditingMaterial] = useState<DefaultMaterialItem | null>(null);
  const [materialForm, setMaterialForm] = useState<DefaultMaterialFormData>({
    orderTypeId: '', materialId: '', defaultQuantity: '1', notes: ''
  });

  useEffect(() => {
    loadData();
  }, [id]);

  const loadData = async (): Promise<void> => {
    try {
      setLoading(true);
      const [methodsData, deptsData, typesData] = await Promise.all([
        getInstallationMethods({ isActive: true }),
        getDepartments(),
        getSplitterTypes ? getSplitterTypes({ isActive: true }).catch(() => []) : Promise.resolve([])
      ]);
      setInstallationMethods(Array.isArray(methodsData) ? methodsData : []);
      setDepartments(Array.isArray(deptsData) ? deptsData : []);
      setSplitterTypes(Array.isArray(typesData) ? (typesData as SplitterTypeOption[]) : []);

      if (!isNew) {
        const buildingData = await getBuilding(id);
        setBuilding(buildingData as BuildingRecord);
        setFormData({
          name: buildingData.name || '',
          code: buildingData.code || '',
          propertyType: buildingData.propertyType || '',
          installationMethodId: buildingData.installationMethodId || '',
          departmentId: buildingData.departmentId || '',
          addressLine1: buildingData.addressLine1 || '',
          addressLine2: buildingData.addressLine2 || '',
          city: buildingData.city || '',
          state: buildingData.state || '',
          postcode: buildingData.postcode || '',
          area: (buildingData as BuildingRecord).area || '',
          latitude: buildingData.latitude?.toString() || '',
          longitude: buildingData.longitude?.toString() || '',
          rfbAssignedDate: buildingData.rfbAssignedDate ? buildingData.rfbAssignedDate.split('T')[0] : '',
          firstOrderDate: buildingData.firstOrderDate ? buildingData.firstOrderDate.split('T')[0] : '',
          notes: buildingData.notes || '',
          isActive: buildingData.isActive ?? true
        });
      // Load contacts if available in response, otherwise will load when tab is selected
      if (buildingData.contacts && Array.isArray(buildingData.contacts) && buildingData.contacts.length > 0) {
        setContacts(((buildingData.contacts ?? []) as BuildingContactRecord[]).map((contact) => {
          const contactRecord = contact as BuildingContactRecord & { notes?: string };
          return {
            id: contactRecord.id,
            role: contactRecord.role || '',
            name: contactRecord.name || '',
            phone: contactRecord.phone || '',
            email: contactRecord.email || '',
            remarks: contactRecord.remarks ?? contactRecord.notes ?? '',
            isPrimary: contactRecord.isPrimary ?? false,
            isActive: contactRecord.isActive ?? true
          };
        }));
      }
      // Load rules if available in response, otherwise will load when tab is selected
      if (buildingData.rules) {
        setRules({
          accessRules: (buildingData.rules as any).accessRules || '',
          installationRules: (buildingData.rules as any).installationRules || '',
          otherNotes: (buildingData.rules as any).otherNotes || ''
        });
      }
      }
    } catch (err) {
      showError(err.message || 'Failed to load building');
      navigate('/buildings');
    } finally {
      setLoading(false);
    }
  };

  const loadInfrastructure = async (): Promise<void> => {
    if (isNew || !id || id === 'new') return;
    try {
      setInfraLoading(true);
      const data = await getBuildingInfrastructure(id);
      setInfrastructure((data ?? null) as InfrastructureData | null);
    } catch (err: any) {
      // Only show error if it's not a 404 (building might not have infrastructure yet)
      if (err.status !== 404) {
        showError(err.message || 'Failed to load infrastructure');
      }
    } finally {
      setInfraLoading(false);
    }
  };

  useEffect(() => {
    if (activeTab === 'infrastructure' && !isNew && id && id !== 'new' && !infrastructure) {
      loadInfrastructure();
    }
  }, [activeTab, id, isNew]);

  // Load contacts when contacts tab is selected
  useEffect(() => {
    if (activeTab === 'contacts' && !isNew && id && contacts.length === 0) {
      loadContacts();
    }
  }, [activeTab, id]);

  // Load rules when rules tab is selected
  useEffect(() => {
    if (activeTab === 'rules' && !isNew && id && id !== 'new' && !rules.accessRules && !rules.installationRules && !rules.otherNotes) {
      loadRules();
    }
  }, [activeTab, id, isNew]);

  // Load default materials when materials tab is selected
  useEffect(() => {
    if (activeTab === 'materials' && !isNew) {
      loadDefaultMaterials();
    }
  }, [activeTab, id]);

  const loadContacts = async (): Promise<void> => {
    if (isNew || !id) return;
    try {
      const contactsData = await getBuildingContacts(id);
      setContacts(contactsData.map((contact) => ({
        id: contact.id,
        role: contact.role || '',
        name: contact.name || '',
        phone: contact.phone || '',
        email: contact.email || '',
        remarks: contact.remarks || contact.notes || '',
        isPrimary: contact.isPrimary ?? false,
        isActive: contact.isActive ?? true
      })));
    } catch (err: any) {
      console.error('Error loading contacts:', err);
      showError(err.message || 'Failed to load contacts');
    }
  };

  const loadRules = async (): Promise<void> => {
    if (isNew || !id || id === 'new') return;
    try {
      const rulesData = await getBuildingRules(id);
      if (rulesData) {
        setRules({
          accessRules: rulesData.accessRules || '',
          installationRules: rulesData.installationRules || '',
          otherNotes: rulesData.otherNotes || ''
        });
      }
    } catch (err: any) {
      console.error('Error loading rules:', err);
      // Rules might not exist yet, that's okay - only show error if it's not a 404
      if (err.status !== 404 && err.message && !err.message.includes('not found')) {
        showError(err.message || 'Failed to load rules');
      }
    }
  };

  const loadDefaultMaterials = async (): Promise<void> => {
    if (isNew) return;
    try {
      setMaterialsLoading(true);
      const [materialsData, orderTypesData, allMaterialsData] = await Promise.all([
        getBuildingDefaultMaterials(id),
        getOrderTypes({ isActive: true }),
        getMaterials({ isActive: true, isSerialised: false })
      ]);
      setDefaultMaterials(Array.isArray(materialsData) ? (materialsData as DefaultMaterialItem[]) : []);
      setOrderTypes(Array.isArray(orderTypesData) ? (orderTypesData as OrderTypeOption[]) : []);
      setAvailableMaterials(Array.isArray(allMaterialsData) ? (allMaterialsData as ExtendedMaterial[]) : []);
    } catch (err) {
      console.error('Error loading default materials:', err);
      showError(err.message || 'Failed to load default materials');
    } finally {
      setMaterialsLoading(false);
    }
  };

  // Default Material handlers
  const openMaterialModal = (material: DefaultMaterialItem | null = null): void => {
    if (material) {
      setEditingMaterial(material);
      setMaterialForm({
        orderTypeId: material.orderTypeId,
        materialId: material.materialId,
        defaultQuantity: material.defaultQuantity?.toString() ?? '1',
        notes: material.notes || ''
      });
    } else {
      setEditingMaterial(null);
      setMaterialForm({ orderTypeId: '', materialId: '', defaultQuantity: '1', notes: '' });
    }
    setMaterialModalOpen(true);
  };

  const handleSaveMaterial = async (): Promise<void> => {
    if (isNew) return;
    try {
      if (editingMaterial) {
        await updateBuildingDefaultMaterial(id, editingMaterial.id, {
          defaultQuantity: parseFloat(materialForm.defaultQuantity),
          notes: materialForm.notes || null
        });
        showSuccess('Material updated');
      } else {
        await createBuildingDefaultMaterial(id, {
          orderTypeId: materialForm.orderTypeId,
          materialId: materialForm.materialId,
          defaultQuantity: parseFloat(materialForm.defaultQuantity),
          notes: materialForm.notes || null
        });
        showSuccess('Material added');
      }
      setMaterialModalOpen(false);
      await loadDefaultMaterials();
    } catch (err) {
      showError(err.message || 'Failed to save material');
    }
  };

  const handleDeleteMaterial = async (materialId: string): Promise<void> => {
    if (isNew) return;
    if (!confirm('Delete this default material?')) return;
    try {
      await deleteBuildingDefaultMaterial(id, materialId);
      showSuccess('Material deleted');
      await loadDefaultMaterials();
    } catch (err) {
      showError(err.message || 'Failed to delete material');
    }
  };

  // Group materials by order type
  const materialsByJobType = useMemo(() => {
    const grouped: Record<string, { orderType: OrderTypeOption; materials: DefaultMaterialItem[] }> = {};
    orderTypes.forEach((ot) => {
      grouped[ot.id] = {
        orderType: ot,
        materials: defaultMaterials.filter((m) => m.orderTypeId === ot.id)
      };
    });
    return grouped;
  }, [orderTypes, defaultMaterials]);

  const handleSaveGeneral = async (): Promise<void> => {
    try {
      setSaving(true);
      const parseOptionalNumber = (value: string): number | null => {
        if (!value) return null;
        const parsed = Number(value);
        return Number.isNaN(parsed) ? null : parsed;
      };

      const data = {
        ...formData,
        latitude: parseOptionalNumber(formData.latitude),
        longitude: parseOptionalNumber(formData.longitude),
        rfbAssignedDate: formData.rfbAssignedDate || null,
        firstOrderDate: formData.firstOrderDate || null,
        installationMethodId: formData.installationMethodId || null,
        departmentId: formData.departmentId || null
      };
      
      if (isNew) {
        const newBuilding = await createBuilding(data);
        showSuccess('Building created successfully');
        navigate(`/buildings/${newBuilding.id}`);
      } else {
        await updateBuilding(id, data);
        showSuccess('Building updated successfully');
        await loadData();
      }
    } catch (err) {
      showError(err.message || 'Failed to save building');
    } finally {
      setSaving(false);
    }
  };

  const handleToggleStatus = async (): Promise<void> => {
    if (isNew || !building) return;
    try {
      await updateBuilding(id, { isActive: !building.isActive });
      showSuccess(`Building ${building.isActive ? 'deactivated' : 'activated'}`);
      await loadData();
    } catch (err) {
      showError(err.message || 'Failed to update status');
    }
  };

  // Contact handlers
  const openContactModal = (contact: BuildingContactRecord | null = null): void => {
    if (contact) {
      setEditingContact(contact);
      setContactForm({
        role: contact.role,
        name: contact.name,
        phone: contact.phone || '',
        email: contact.email || '',
        remarks: contact.remarks || '',
        isPrimary: contact.isPrimary,
        isActive: contact.isActive
      });
    } else {
      setEditingContact(null);
      setContactForm({
        role: '',
        name: '',
        phone: '',
        email: '',
        remarks: '',
        isPrimary: false,
        isActive: true
      });
    }
    setContactModalOpen(true);
  };

  const handleSaveContact = async (): Promise<void> => {
    if (isNew) return;
    try {
      // Map contactForm to API request format (ensure remarks is sent, not notes)
      const contactData = {
        role: contactForm.role,
        name: contactForm.name,
        phone: contactForm.phone || undefined,
        email: contactForm.email || undefined,
        remarks: contactForm.remarks || undefined,
        isPrimary: contactForm.isPrimary,
        isActive: contactForm.isActive
      };
      
      if (editingContact) {
        await updateBuildingContact(id, editingContact.id, contactData);
        showSuccess('Contact updated');
      } else {
        await createBuildingContact(id, contactData);
        showSuccess('Contact added');
      }
      setContactModalOpen(false);
      await loadContacts(); // Reload contacts instead of full building data
    } catch (err: any) {
      showError(err.message || 'Failed to save contact');
    }
  };

  const handleDeleteContact = async (contactId: string): Promise<void> => {
    if (isNew) return;
    if (!confirm('Delete this contact?')) return;
    try {
      await deleteBuildingContact(id, contactId);
      showSuccess('Contact deleted');
      await loadContacts(); // Reload contacts instead of full building data
    } catch (err) {
      showError(err.message || 'Failed to delete contact');
    }
  };

  // Rules handlers
  const handleSaveRules = async (): Promise<void> => {
    if (isNew) return;
    try {
      setSaving(true);
      await saveBuildingRules(id, rules);
      showSuccess('Rules saved');
      setRulesChanged(false);
    } catch (err) {
      showError(err.message || 'Failed to save rules');
    } finally {
      setSaving(false);
    }
  };

  // Block handlers
  const openBlockModal = (block: InfrastructureBlock | null = null): void => {
    if (block) {
      setEditingBlock(block);
      setBlockForm({
        name: block.name, code: block.code || '', floors: block.floors, 
        unitsPerFloor: block.unitsPerFloor || '', totalUnits: block.totalUnits || '',
        displayOrder: block.displayOrder, isActive: block.isActive, notes: block.notes || ''
      });
    } else {
      setEditingBlock(null);
      setBlockForm({ name: '', code: '', floors: 1, unitsPerFloor: '', totalUnits: '', displayOrder: 0, isActive: true, notes: '' });
    }
    setBlockModalOpen(true);
  };

  const handleSaveBlock = async (): Promise<void> => {
    if (isNew) return;
    try {
      const data = { ...blockForm, floors: parseInt(blockForm.floors) || 1 };
      if (editingBlock) {
        await updateBuildingBlock(id, editingBlock.id, data);
        showSuccess('Block updated');
      } else {
        await createBuildingBlock(id, data);
        showSuccess('Block added');
      }
      setBlockModalOpen(false);
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to save block');
    }
  };

  const handleDeleteBlock = async (blockId: string): Promise<void> => {
    if (isNew) return;
    if (!confirm('Delete this block? This will also affect splitters.')) return;
    try {
      await deleteBuildingBlock(id, blockId);
      showSuccess('Block deleted');
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to delete block');
    }
  };

  // Splitter handlers
  const openSplitterModal = (splitter: InfrastructureSplitter | null = null): void => {
    if (splitter) {
      setEditingSplitter(splitter);
      setSplitterForm({
        blockId: splitter.blockId || '',
        splitterTypeId: splitter.splitterTypeId || '',
        name: splitter.name,
        floor: splitter.floor?.toString() || '',
        locationDescription: splitter.locationDescription || '',
        portsTotal: splitter.portsTotal,
        portsUsed: splitter.portsUsed,
        status: splitter.status,
        serialNumber: splitter.serialNumber || '',
        remarks: splitter.remarks || '',
        needsAttention: splitter.needsAttention ?? false
      });
    } else {
      setEditingSplitter(null);
      setSplitterForm({
        blockId: '', splitterTypeId: '', name: '', floor: '', locationDescription: '',
        portsTotal: 8, portsUsed: 0, status: 'Active', serialNumber: '', remarks: '', needsAttention: false
      });
    }
    setSplitterModalOpen(true);
  };

  const handleSaveSplitter = async (): Promise<void> => {
    if (isNew) return;
    try {
      const data = { ...splitterForm, portsTotal: parseInt(splitterForm.portsTotal), portsUsed: parseInt(splitterForm.portsUsed) };
      if (editingSplitter) {
        await updateBuildingSplitter(id, editingSplitter.id, data);
        showSuccess('Splitter updated');
      } else {
        await createBuildingSplitter(id, data);
        showSuccess('Splitter added');
      }
      setSplitterModalOpen(false);
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to save splitter');
    }
  };

  const handleDeleteSplitter = async (splitterId: string): Promise<void> => {
    if (isNew) return;
    if (!confirm('Delete this splitter?')) return;
    try {
      await deleteBuildingSplitter(id, splitterId);
      showSuccess('Splitter deleted');
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to delete splitter');
    }
  };

  // Street handlers
  const openStreetModal = (street: InfrastructureStreet | null = null): void => {
    if (street) {
      setEditingStreet(street);
      setStreetForm({ name: street.name, code: street.code || '', section: street.section || '', displayOrder: street.displayOrder, isActive: street.isActive });
    } else {
      setEditingStreet(null);
      setStreetForm({ name: '', code: '', section: '', displayOrder: 0, isActive: true });
    }
    setStreetModalOpen(true);
  };

  const handleSaveStreet = async (): Promise<void> => {
    if (isNew) return;
    try {
      if (editingStreet) {
        await updateStreet(id, editingStreet.id, streetForm);
        showSuccess('Street updated');
      } else {
        await createStreet(id, streetForm);
        showSuccess('Street added');
      }
      setStreetModalOpen(false);
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to save street');
    }
  };

  const handleDeleteStreet = async (streetId: string): Promise<void> => {
    if (isNew) return;
    if (!confirm('Delete this street?')) return;
    try {
      await deleteStreet(id, streetId);
      showSuccess('Street deleted');
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to delete street');
    }
  };

  // Hub Box handlers
  const openHubBoxModal = (hubBox: InfrastructureHubBox | null = null): void => {
    if (hubBox) {
      setEditingHubBox(hubBox);
      setHubBoxForm({
        streetId: hubBox.streetId || '',
        name: hubBox.name,
        code: hubBox.code || '',
        locationDescription: hubBox.locationDescription || '',
        latitude: hubBox.latitude?.toString() || '',
        longitude: hubBox.longitude?.toString() || '',
        portsTotal: hubBox.portsTotal,
        portsUsed: hubBox.portsUsed,
        status: hubBox.status,
        remarks: hubBox.remarks || '',
        isActive: hubBox.isActive ?? true
      });
    } else {
      setEditingHubBox(null);
      setHubBoxForm({
        streetId: '', name: '', code: '', locationDescription: '', latitude: '', longitude: '',
        portsTotal: 8, portsUsed: 0, status: 'Active', remarks: '', isActive: true
      });
    }
    setHubBoxModalOpen(true);
  };

  const handleSaveHubBox = async (): Promise<void> => {
    if (isNew) return;
    try {
      const data = { ...hubBoxForm, portsTotal: parseInt(hubBoxForm.portsTotal), portsUsed: parseInt(hubBoxForm.portsUsed) };
      if (editingHubBox) {
        await updateHubBox(id, editingHubBox.id, data);
        showSuccess('Hub Box updated');
      } else {
        await createHubBox(id, data);
        showSuccess('Hub Box added');
      }
      setHubBoxModalOpen(false);
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to save hub box');
    }
  };

  const handleDeleteHubBox = async (hubBoxId: string): Promise<void> => {
    if (isNew) return;
    if (!confirm('Delete this hub box?')) return;
    try {
      await deleteHubBox(id, hubBoxId);
      showSuccess('Hub Box deleted');
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to delete hub box');
    }
  };

  // Pole handlers
  const openPoleModal = (pole: InfrastructurePole | null = null): void => {
    if (pole) {
      setEditingPole(pole);
      setPoleForm({
        streetId: pole.streetId || '',
        poleNumber: pole.poleNumber || '',
        poleType: pole.poleType || '',
        locationDescription: pole.locationDescription || '',
        latitude: pole.latitude?.toString() || '',
        longitude: pole.longitude?.toString() || '',
        heightMeters: pole.heightMeters?.toString() || '',
        hasExistingFibre: pole.hasExistingFibre ?? false,
        dropsCount: pole.dropsCount ?? 0,
        status: pole.status || 'Good',
        remarks: pole.remarks || '',
        isActive: pole.isActive ?? true
      });
    } else {
      setEditingPole(null);
      setPoleForm({
        streetId: '', poleNumber: '', poleType: '', locationDescription: '', latitude: '', longitude: '',
        heightMeters: '', hasExistingFibre: false, dropsCount: 0, status: 'Good', remarks: '', isActive: true
      });
    }
    setPoleModalOpen(true);
  };

  const handleSavePole = async (): Promise<void> => {
    if (isNew) return;
    try {
      if (editingPole) {
        await updatePole(id, editingPole.id, poleForm);
        showSuccess('Pole updated');
      } else {
        await createPole(id, poleForm);
        showSuccess('Pole added');
      }
      setPoleModalOpen(false);
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to save pole');
    }
  };

  const handleDeletePole = async (poleId: string): Promise<void> => {
    if (isNew) return;
    if (!confirm('Delete this pole?')) return;
    try {
      await deletePole(id, poleId);
      showSuccess('Pole deleted');
      await loadInfrastructure();
    } catch (err) {
      showError(err.message || 'Failed to delete pole');
    }
  };

  const isMDU = formData.propertyType === 'MDU' || formData.propertyType === '';

  if (loading) {
    return (
      <PageShell title="Building" breadcrumbs={[{ label: 'Buildings', path: '/buildings' }, { label: 'Details' }]}>
        <LoadingSpinner message="Loading building..." fullPage />
      </PageShell>
    );
  }

  const pageTitle = isNew ? 'New Building' : (building?.name || 'Building Details');
  const breadcrumbLabel = isNew ? 'New Building' : (building?.name || building?.code || 'Details');

  return (
    <PageShell
      title={pageTitle}
      breadcrumbs={[{ label: 'Buildings', path: '/buildings' }, { label: breadcrumbLabel }]}
      actions={
        <>
          <Button variant="outline" size="sm" onClick={() => navigate('/buildings')} className="gap-1">
            <ArrowLeft className="h-4 w-4" />
            Back
          </Button>
          {!isNew && building && (
            <StatusBadge isActive={building.isActive} onClick={handleToggleStatus} />
          )}
        </>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      {/* Tabs */}
      <div className="border-b border-border">
        <div className="flex gap-1">
          {TABS.map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              disabled={isNew && tab.id !== 'general'}
              className={cn(
                "flex items-center gap-2 px-4 py-2.5 text-sm font-medium transition-colors relative",
                activeTab === tab.id
                  ? "text-primary"
                  : "text-muted-foreground hover:text-foreground",
                isNew && tab.id !== 'general' && "opacity-50 cursor-not-allowed"
              )}
            >
              <tab.icon className="h-4 w-4" />
              {tab.label}
              {activeTab === tab.id && (
                <div className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary" />
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Tab Content */}
      <div className="min-h-[400px]">
        {/* General Tab */}
        {activeTab === 'general' && (
          <Card className="p-4 space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <TextInput
                label="Building Name *"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="e.g., The Residences @ City Center"
                required
              />
              <TextInput
                label="Code"
                value={formData.code}
                onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                placeholder="e.g., RES-CC-001"
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="text-xs font-medium text-muted-foreground mb-1 block">Property Type</label>
                <select
                  value={formData.propertyType}
                  onChange={(e) => setFormData({ ...formData, propertyType: e.target.value })}
                  className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">Select Property Type</option>
                  {Object.entries(PropertyTypeLabels).map(([value, label]) => (
                    <option key={value} value={value}>{label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground mb-1 block">Installation Method</label>
                <select
                  value={formData.installationMethodId}
                  onChange={(e) => setFormData({ ...formData, installationMethodId: e.target.value })}
                  className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">Select Method</option>
                  {installationMethods.map(m => (
                    <option key={m.id} value={m.id}>{m.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="text-xs font-medium text-muted-foreground mb-1 block">Department</label>
                <select
                  value={formData.departmentId}
                  onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
                  className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  <option value="">No Department</option>
                  {departments.map(d => (
                    <option key={d.id} value={d.id}>{d.name}</option>
                  ))}
                </select>
              </div>
            </div>

            <hr className="border-border" />

            <h3 className="text-sm font-semibold flex items-center gap-2">
              <MapPin className="h-4 w-4" />
              Location
            </h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <TextInput
                label="Address Line 1 *"
                value={formData.addressLine1}
                onChange={(e) => setFormData({ ...formData, addressLine1: e.target.value })}
                placeholder="Street address"
                required
              />
              <TextInput
                label="Address Line 2"
                value={formData.addressLine2}
                onChange={(e) => setFormData({ ...formData, addressLine2: e.target.value })}
                placeholder="Unit, floor, etc."
              />
            </div>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <TextInput
                label="City *"
                value={formData.city}
                onChange={(e) => setFormData({ ...formData, city: e.target.value })}
                required
              />
              <TextInput
                label="State *"
                value={formData.state}
                onChange={(e) => setFormData({ ...formData, state: e.target.value })}
                required
              />
              <TextInput
                label="Postcode"
                value={formData.postcode}
                onChange={(e) => setFormData({ ...formData, postcode: e.target.value })}
              />
              <TextInput
                label="Area"
                value={formData.area}
                onChange={(e) => setFormData({ ...formData, area: e.target.value })}
                placeholder="e.g., TTDI"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Latitude"
                type="number"
                step="any"
                value={formData.latitude}
                onChange={(e) => setFormData({ ...formData, latitude: e.target.value })}
                placeholder="e.g., 3.1234"
              />
              <TextInput
                label="Longitude"
                type="number"
                step="any"
                value={formData.longitude}
                onChange={(e) => setFormData({ ...formData, longitude: e.target.value })}
                placeholder="e.g., 101.5678"
              />
            </div>

            <hr className="border-border" />

            <h3 className="text-sm font-semibold flex items-center gap-2">
              <Calendar className="h-4 w-4" />
              Timeline
            </h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <TextInput
                label="RFB Assigned Date"
                type="date"
                value={formData.rfbAssignedDate}
                onChange={(e) => setFormData({ ...formData, rfbAssignedDate: e.target.value })}
              />
              <TextInput
                label="First Order Date"
                type="date"
                value={formData.firstOrderDate}
                onChange={(e) => setFormData({ ...formData, firstOrderDate: e.target.value })}
              />
            </div>

            <div>
              <label className="text-xs font-medium text-muted-foreground mb-1 block">Notes</label>
              <textarea
                value={formData.notes}
                onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                rows={3}
                className="w-full px-3 py-2 text-sm bg-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring resize-none"
                placeholder="Additional notes about this building..."
              />
            </div>

            <div className="flex justify-end pt-4">
              <Button onClick={handleSaveGeneral} disabled={saving} className="gap-2">
                <Save className="h-4 w-4" />
                {saving ? 'Saving...' : 'Save Changes'}
              </Button>
            </div>
          </Card>
        )}

        {/* Contacts Tab */}
        {activeTab === 'contacts' && !isNew && (
          <Card className="p-4">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-semibold">Building Contacts</h3>
              <Button size="sm" onClick={() => openContactModal()} className="gap-2">
                <Plus className="h-3 w-3" />
                Add Contact
              </Button>
            </div>

            {contacts.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                <Users className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p className="text-sm">No contacts added yet</p>
                <p className="text-xs mt-1">Add building manager, maintenance contacts, etc.</p>
              </div>
            ) : (
              <div className="space-y-2">
                {contacts.map(contact => (
                  <div 
                    key={contact.id}
                    className={cn(
                      "flex items-center justify-between p-3 rounded-lg border",
                      contact.isActive ? "border-border" : "border-dashed border-muted-foreground/30 opacity-60"
                    )}
                  >
                    <div className="flex items-center gap-3">
                      <div className="h-9 w-9 rounded-full bg-muted flex items-center justify-center">
                        <User className="h-4 w-4 text-muted-foreground" />
                      </div>
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium">{contact.name}</span>
                          {contact.isPrimary && (
                            <Star className="h-3 w-3 text-amber-500 fill-amber-500" />
                          )}
                          <span className="px-1.5 py-0.5 rounded text-[10px] bg-muted text-muted-foreground">
                            {contact.role}
                          </span>
                        </div>
                        <div className="flex items-center gap-3 text-xs text-muted-foreground mt-0.5">
                          {contact.phone && (
                            <span className="flex items-center gap-1">
                              <Phone className="h-3 w-3" />
                              {contact.phone}
                            </span>
                          )}
                          {contact.email && (
                            <span className="flex items-center gap-1">
                              <Mail className="h-3 w-3" />
                              {contact.email}
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                    <div className="flex items-center gap-1">
                      <button onClick={() => openContactModal(contact)} className="p-1.5 rounded hover:bg-accent transition-colors">
                        <Edit className="h-4 w-4 text-muted-foreground" />
                      </button>
                      <button onClick={() => handleDeleteContact(contact.id)} className="p-1.5 rounded hover:bg-destructive/10 transition-colors">
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {/* Contact Modal */}
            <Modal isOpen={contactModalOpen} onClose={() => setContactModalOpen(false)}>
              <div className="bg-card rounded-lg p-4 w-full max-w-md">
                <h3 className="text-sm font-semibold mb-4">{editingContact ? 'Edit Contact' : 'Add Contact'}</h3>
                <div className="space-y-3">
                  <div>
                    <label className="text-xs font-medium text-muted-foreground mb-1 block">Role *</label>
                    <select
                      value={contactForm.role}
                      onChange={(e) => setContactForm({ ...contactForm, role: e.target.value })}
                      className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                    >
                      <option value="">Select Role</option>
                      {ContactRoles.map(role => (
                        <option key={role} value={role}>{role}</option>
                      ))}
                    </select>
                  </div>
                  <TextInput label="Name *" value={contactForm.name} onChange={(e) => setContactForm({ ...contactForm, name: e.target.value })} required />
                  <TextInput label="Phone" value={contactForm.phone} onChange={(e) => setContactForm({ ...contactForm, phone: e.target.value })} />
                  <TextInput label="Email" type="email" value={contactForm.email} onChange={(e) => setContactForm({ ...contactForm, email: e.target.value })} />
                  <TextInput label="Remarks" value={contactForm.remarks} onChange={(e) => setContactForm({ ...contactForm, remarks: e.target.value })} />
                  <div className="flex items-center gap-4">
                    <label className="flex items-center gap-2 text-sm">
                      <input type="checkbox" checked={contactForm.isPrimary} onChange={(e) => setContactForm({ ...contactForm, isPrimary: e.target.checked })} className="rounded border-input" />
                      Primary contact
                    </label>
                    <label className="flex items-center gap-2 text-sm">
                      <input type="checkbox" checked={contactForm.isActive} onChange={(e) => setContactForm({ ...contactForm, isActive: e.target.checked })} className="rounded border-input" />
                      Active
                    </label>
                  </div>
                </div>
                <div className="flex justify-end gap-2 mt-4">
                  <Button variant="outline" onClick={() => setContactModalOpen(false)}>Cancel</Button>
                  <Button onClick={handleSaveContact}>{editingContact ? 'Update' : 'Add'} Contact</Button>
                </div>
              </div>
            </Modal>
          </Card>
        )}

        {/* House Rules Tab */}
        {activeTab === 'rules' && !isNew && (
          <Card className="p-4 space-y-4">
            <h3 className="text-sm font-semibold">House Rules & Guidelines</h3>
            <div>
              <label className="text-xs font-medium text-muted-foreground mb-1 block">Access Rules</label>
              <textarea value={rules.accessRules} onChange={(e) => { setRules({ ...rules, accessRules: e.target.value }); setRulesChanged(true); }} rows={4} className="w-full px-3 py-2 text-sm bg-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring resize-none" placeholder="Access procedures, key collection, security requirements..." />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground mb-1 block">Installation Rules</label>
              <textarea value={rules.installationRules} onChange={(e) => { setRules({ ...rules, installationRules: e.target.value }); setRulesChanged(true); }} rows={4} className="w-full px-3 py-2 text-sm bg-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring resize-none" placeholder="Installation guidelines, restrictions, required approvals..." />
            </div>
            <div>
              <label className="text-xs font-medium text-muted-foreground mb-1 block">Other Notes</label>
              <textarea value={rules.otherNotes} onChange={(e) => { setRules({ ...rules, otherNotes: e.target.value }); setRulesChanged(true); }} rows={4} className="w-full px-3 py-2 text-sm bg-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring resize-none" placeholder="Any other relevant information..." />
            </div>
            <div className="flex justify-end pt-4">
              <Button onClick={handleSaveRules} disabled={saving || !rulesChanged} className="gap-2">
                <Save className="h-4 w-4" />
                {saving ? 'Saving...' : 'Save Rules'}
              </Button>
            </div>
          </Card>
        )}

        {/* Infrastructure Tab */}
        {activeTab === 'infrastructure' && !isNew && (
          <div className="space-y-4">
            {/* Summary Stats */}
            {infrastructure && (
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <Card className="p-3">
                  <p className="text-xs text-muted-foreground">Total {isMDU ? 'Blocks' : 'Streets'}</p>
                  <p className="text-xl font-bold">{isMDU ? infrastructure.totalBlocks : infrastructure.totalStreets}</p>
                </Card>
                <Card className="p-3">
                  <p className="text-xs text-muted-foreground">{isMDU ? 'Splitters' : 'Hub Boxes'}</p>
                  <p className="text-xl font-bold">{isMDU ? infrastructure.totalSplitters : infrastructure.totalHubBoxes}</p>
                </Card>
                <Card className="p-3">
                  <p className="text-xs text-muted-foreground">Total Ports</p>
                  <p className="text-xl font-bold">{isMDU ? infrastructure.totalSplitterPorts : infrastructure.totalHubBoxPorts}</p>
                </Card>
                <Card className="p-3">
                  <p className="text-xs text-muted-foreground">Used Ports</p>
                  <p className="text-xl font-bold">{isMDU ? infrastructure.usedSplitterPorts : infrastructure.usedHubBoxPorts}</p>
                  <CapacityBar 
                    used={isMDU ? infrastructure.usedSplitterPorts : infrastructure.usedHubBoxPorts} 
                    total={isMDU ? infrastructure.totalSplitterPorts : infrastructure.totalHubBoxPorts}
                    showLabel={false}
                  />
                </Card>
              </div>
            )}

            {infraLoading ? (
              <LoadingSpinner message="Loading infrastructure..." />
            ) : isMDU ? (
              /* MDU Infrastructure */
              <>
                {/* Blocks Section */}
                <Card className="p-4">
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-sm font-semibold flex items-center gap-2">
                      <Layers className="h-4 w-4" />
                      Building Blocks
                    </h3>
                    <div className="flex items-center gap-2">
                      <Button variant="ghost" size="sm" onClick={loadInfrastructure}><RefreshCw className="h-3 w-3" /></Button>
                      <Button size="sm" onClick={() => openBlockModal()} className="gap-1"><Plus className="h-3 w-3" />Add Block</Button>
                    </div>
                  </div>
                  {infrastructure?.blocks?.length === 0 ? (
                    <p className="text-center text-muted-foreground py-6 text-sm">No blocks defined. Add your first block.</p>
                  ) : (
                    <div className="space-y-2">
                      {infrastructure?.blocks?.map(block => (
                        <div key={block.id} className="flex items-center justify-between p-3 border border-border rounded-lg">
                          <div>
                            <p className="text-sm font-medium">{block.name} {block.code && <span className="text-muted-foreground">({block.code})</span>}</p>
                            <p className="text-xs text-muted-foreground">{block.floors} floors • {block.splittersCount} splitters</p>
                          </div>
                          <div className="flex items-center gap-1">
                            <button onClick={() => openBlockModal(block)} className="p-1.5 rounded hover:bg-accent"><Edit className="h-4 w-4 text-muted-foreground" /></button>
                            <button onClick={() => handleDeleteBlock(block.id)} className="p-1.5 rounded hover:bg-destructive/10"><Trash2 className="h-4 w-4 text-destructive" /></button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </Card>

                {/* Splitters Section */}
                <Card className="p-4">
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-sm font-semibold flex items-center gap-2">
                      <Network className="h-4 w-4" />
                      Splitters
                    </h3>
                    <Button size="sm" onClick={() => openSplitterModal()} className="gap-1"><Plus className="h-3 w-3" />Add Splitter</Button>
                  </div>
                  {infrastructure?.splitters?.length === 0 ? (
                    <p className="text-center text-muted-foreground py-6 text-sm">No splitters defined.</p>
                  ) : (
                    <div className="overflow-x-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="border-b border-border">
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Name</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Block</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Floor</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Capacity</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Status</th>
                            <th className="text-right py-2 px-2 text-xs font-medium text-muted-foreground">Actions</th>
                          </tr>
                        </thead>
                        <tbody>
                          {infrastructure?.splitters?.map(splitter => (
                            <tr key={splitter.id} className="border-b border-border last:border-0 hover:bg-muted/50">
                              <td className="py-2 px-2">
                                <div className="flex items-center gap-2">
                                  {splitter.needsAttention && <AlertTriangle className="h-3 w-3 text-amber-500" />}
                                  <span>{splitter.name}</span>
                                </div>
                              </td>
                              <td className="py-2 px-2 text-muted-foreground">{splitter.blockName || '-'}</td>
                              <td className="py-2 px-2 text-muted-foreground">{splitter.floor || '-'}</td>
                              <td className="py-2 px-2 w-32">
                                <CapacityBar used={splitter.portsUsed} total={splitter.portsTotal} />
                              </td>
                              <td className="py-2 px-2"><SplitterStatusBadge status={splitter.status} /></td>
                              <td className="py-2 px-2 text-right">
                                <button onClick={() => openSplitterModal(splitter)} className="p-1 rounded hover:bg-accent"><Edit className="h-3 w-3 text-muted-foreground" /></button>
                                <button onClick={() => handleDeleteSplitter(splitter.id)} className="p-1 rounded hover:bg-destructive/10"><Trash2 className="h-3 w-3 text-destructive" /></button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </Card>
              </>
            ) : (
              /* Landed/SDU Infrastructure */
              <>
                {/* Streets Section */}
                <Card className="p-4">
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-sm font-semibold">Streets</h3>
                    <Button size="sm" onClick={() => openStreetModal()} className="gap-1"><Plus className="h-3 w-3" />Add Street</Button>
                  </div>
                  {infrastructure?.streets?.length === 0 ? (
                    <p className="text-center text-muted-foreground py-6 text-sm">No streets defined.</p>
                  ) : (
                    <div className="space-y-2">
                      {infrastructure?.streets?.map(street => (
                        <div key={street.id} className="flex items-center justify-between p-3 border border-border rounded-lg">
                          <div>
                            <p className="text-sm font-medium">{street.name}</p>
                            <p className="text-xs text-muted-foreground">{street.hubBoxesCount} hub boxes • {street.polesCount} poles</p>
                          </div>
                          <div className="flex items-center gap-1">
                            <button onClick={() => openStreetModal(street)} className="p-1.5 rounded hover:bg-accent"><Edit className="h-4 w-4 text-muted-foreground" /></button>
                            <button onClick={() => handleDeleteStreet(street.id)} className="p-1.5 rounded hover:bg-destructive/10"><Trash2 className="h-4 w-4 text-destructive" /></button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </Card>

                {/* Hub Boxes Section */}
                <Card className="p-4">
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-sm font-semibold">Hub Boxes</h3>
                    <Button size="sm" onClick={() => openHubBoxModal()} className="gap-1"><Plus className="h-3 w-3" />Add Hub Box</Button>
                  </div>
                  {infrastructure?.hubBoxes?.length === 0 ? (
                    <p className="text-center text-muted-foreground py-6 text-sm">No hub boxes defined.</p>
                  ) : (
                    <div className="overflow-x-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="border-b border-border">
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Name</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Street</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Capacity</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Status</th>
                            <th className="text-right py-2 px-2 text-xs font-medium text-muted-foreground">Actions</th>
                          </tr>
                        </thead>
                        <tbody>
                          {infrastructure?.hubBoxes?.map(hubBox => (
                            <tr key={hubBox.id} className="border-b border-border last:border-0 hover:bg-muted/50">
                              <td className="py-2 px-2">{hubBox.name}</td>
                              <td className="py-2 px-2 text-muted-foreground">{hubBox.streetName || '-'}</td>
                              <td className="py-2 px-2 w-32"><CapacityBar used={hubBox.portsUsed} total={hubBox.portsTotal} /></td>
                              <td className="py-2 px-2"><SplitterStatusBadge status={hubBox.status} /></td>
                              <td className="py-2 px-2 text-right">
                                <button onClick={() => openHubBoxModal(hubBox)} className="p-1 rounded hover:bg-accent"><Edit className="h-3 w-3 text-muted-foreground" /></button>
                                <button onClick={() => handleDeleteHubBox(hubBox.id)} className="p-1 rounded hover:bg-destructive/10"><Trash2 className="h-3 w-3 text-destructive" /></button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </Card>

                {/* Poles Section */}
                <Card className="p-4">
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-sm font-semibold">Poles</h3>
                    <Button size="sm" onClick={() => openPoleModal()} className="gap-1"><Plus className="h-3 w-3" />Add Pole</Button>
                  </div>
                  {infrastructure?.poles?.length === 0 ? (
                    <p className="text-center text-muted-foreground py-6 text-sm">No poles defined.</p>
                  ) : (
                    <div className="overflow-x-auto">
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="border-b border-border">
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Pole #</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Street</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Type</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Drops</th>
                            <th className="text-left py-2 px-2 text-xs font-medium text-muted-foreground">Status</th>
                            <th className="text-right py-2 px-2 text-xs font-medium text-muted-foreground">Actions</th>
                          </tr>
                        </thead>
                        <tbody>
                          {infrastructure?.poles?.map(pole => (
                            <tr key={pole.id} className="border-b border-border last:border-0 hover:bg-muted/50">
                              <td className="py-2 px-2">{pole.poleNumber}</td>
                              <td className="py-2 px-2 text-muted-foreground">{pole.streetName || '-'}</td>
                              <td className="py-2 px-2 text-muted-foreground">{pole.poleType || '-'}</td>
                              <td className="py-2 px-2">{pole.dropsCount}</td>
                              <td className="py-2 px-2">
                                <span className={cn("px-2 py-0.5 rounded text-[10px] font-medium",
                                  pole.status === 'Good' ? 'bg-emerald-100 text-emerald-700' :
                                  pole.status === 'Damaged' ? 'bg-red-100 text-red-700' : 'bg-amber-100 text-amber-700'
                                )}>{PoleStatusLabels[pole.status] || pole.status}</span>
                              </td>
                              <td className="py-2 px-2 text-right">
                                <button onClick={() => openPoleModal(pole)} className="p-1 rounded hover:bg-accent"><Edit className="h-3 w-3 text-muted-foreground" /></button>
                                <button onClick={() => handleDeletePole(pole.id)} className="p-1 rounded hover:bg-destructive/10"><Trash2 className="h-3 w-3 text-destructive" /></button>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </Card>
              </>
            )}

            {/* Block Modal */}
            <Modal isOpen={blockModalOpen} onClose={() => setBlockModalOpen(false)}>
              <div className="bg-card rounded-lg p-4 w-full max-w-md">
                <h3 className="text-sm font-semibold mb-4">{editingBlock ? 'Edit Block' : 'Add Block'}</h3>
                <div className="space-y-3">
                  <TextInput label="Name *" value={blockForm.name} onChange={(e) => setBlockForm({ ...blockForm, name: e.target.value })} placeholder="e.g., Block A" />
                  <TextInput label="Code" value={blockForm.code} onChange={(e) => setBlockForm({ ...blockForm, code: e.target.value })} />
                  <TextInput label="Floors" type="number" value={blockForm.floors} onChange={(e) => setBlockForm({ ...blockForm, floors: e.target.value })} />
                  <TextInput label="Units Per Floor" type="number" value={blockForm.unitsPerFloor} onChange={(e) => setBlockForm({ ...blockForm, unitsPerFloor: e.target.value })} />
                </div>
                <div className="flex justify-end gap-2 mt-4">
                  <Button variant="outline" onClick={() => setBlockModalOpen(false)}>Cancel</Button>
                  <Button onClick={handleSaveBlock}>{editingBlock ? 'Update' : 'Add'} Block</Button>
                </div>
              </div>
            </Modal>

            {/* Splitter Modal */}
            <Modal isOpen={splitterModalOpen} onClose={() => setSplitterModalOpen(false)}>
              <div className="bg-card rounded-lg p-4 w-full max-w-lg">
                <h3 className="text-sm font-semibold mb-4">{editingSplitter ? 'Edit Splitter' : 'Add Splitter'}</h3>
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput label="Name *" value={splitterForm.name} onChange={(e) => setSplitterForm({ ...splitterForm, name: e.target.value })} placeholder="e.g., SPL-A-01" />
                    <div>
                      <label className="text-xs font-medium text-muted-foreground mb-1 block">Block</label>
                      <select value={splitterForm.blockId} onChange={(e) => setSplitterForm({ ...splitterForm, blockId: e.target.value })} className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg">
                        <option value="">No Block</option>
                        {infrastructure?.blocks?.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
                      </select>
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput label="Floor" type="number" value={splitterForm.floor} onChange={(e) => setSplitterForm({ ...splitterForm, floor: e.target.value })} />
                    <div>
                      <label className="text-xs font-medium text-muted-foreground mb-1 block">Type</label>
                      <select value={splitterForm.splitterTypeId} onChange={(e) => setSplitterForm({ ...splitterForm, splitterTypeId: e.target.value })} className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg">
                        <option value="">Select Type</option>
                        {splitterTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                      </select>
                    </div>
                  </div>
                  <TextInput label="Location" value={splitterForm.locationDescription} onChange={(e) => setSplitterForm({ ...splitterForm, locationDescription: e.target.value })} placeholder="e.g., Riser Room Level 5" />
                  <div className="grid grid-cols-3 gap-3">
                    <TextInput label="Total Ports" type="number" value={splitterForm.portsTotal} onChange={(e) => setSplitterForm({ ...splitterForm, portsTotal: e.target.value })} />
                    <TextInput label="Ports Used" type="number" value={splitterForm.portsUsed} onChange={(e) => setSplitterForm({ ...splitterForm, portsUsed: e.target.value })} />
                    <div>
                      <label className="text-xs font-medium text-muted-foreground mb-1 block">Status</label>
                      <select value={splitterForm.status} onChange={(e) => setSplitterForm({ ...splitterForm, status: e.target.value })} className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg">
                        {Object.entries(SplitterStatusLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
                      </select>
                    </div>
                  </div>
                </div>
                <div className="flex justify-end gap-2 mt-4">
                  <Button variant="outline" onClick={() => setSplitterModalOpen(false)}>Cancel</Button>
                  <Button onClick={handleSaveSplitter}>{editingSplitter ? 'Update' : 'Add'} Splitter</Button>
                </div>
              </div>
            </Modal>

            {/* Street Modal */}
            <Modal isOpen={streetModalOpen} onClose={() => setStreetModalOpen(false)}>
              <div className="bg-card rounded-lg p-4 w-full max-w-md">
                <h3 className="text-sm font-semibold mb-4">{editingStreet ? 'Edit Street' : 'Add Street'}</h3>
                <div className="space-y-3">
                  <TextInput label="Name *" value={streetForm.name} onChange={(e) => setStreetForm({ ...streetForm, name: e.target.value })} />
                  <TextInput label="Code" value={streetForm.code} onChange={(e) => setStreetForm({ ...streetForm, code: e.target.value })} />
                  <TextInput label="Section" value={streetForm.section} onChange={(e) => setStreetForm({ ...streetForm, section: e.target.value })} placeholder="e.g., Section 14" />
                </div>
                <div className="flex justify-end gap-2 mt-4">
                  <Button variant="outline" onClick={() => setStreetModalOpen(false)}>Cancel</Button>
                  <Button onClick={handleSaveStreet}>{editingStreet ? 'Update' : 'Add'} Street</Button>
                </div>
              </div>
            </Modal>

            {/* Hub Box Modal */}
            <Modal isOpen={hubBoxModalOpen} onClose={() => setHubBoxModalOpen(false)}>
              <div className="bg-card rounded-lg p-4 w-full max-w-lg">
                <h3 className="text-sm font-semibold mb-4">{editingHubBox ? 'Edit Hub Box' : 'Add Hub Box'}</h3>
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput label="Name *" value={hubBoxForm.name} onChange={(e) => setHubBoxForm({ ...hubBoxForm, name: e.target.value })} />
                    <div>
                      <label className="text-xs font-medium text-muted-foreground mb-1 block">Street</label>
                      <select value={hubBoxForm.streetId} onChange={(e) => setHubBoxForm({ ...hubBoxForm, streetId: e.target.value })} className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg">
                        <option value="">No Street</option>
                        {infrastructure?.streets?.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                      </select>
                    </div>
                  </div>
                  <TextInput label="Location" value={hubBoxForm.locationDescription} onChange={(e) => setHubBoxForm({ ...hubBoxForm, locationDescription: e.target.value })} />
                  <div className="grid grid-cols-3 gap-3">
                    <TextInput label="Total Ports" type="number" value={hubBoxForm.portsTotal} onChange={(e) => setHubBoxForm({ ...hubBoxForm, portsTotal: e.target.value })} />
                    <TextInput label="Ports Used" type="number" value={hubBoxForm.portsUsed} onChange={(e) => setHubBoxForm({ ...hubBoxForm, portsUsed: e.target.value })} />
                    <div>
                      <label className="text-xs font-medium text-muted-foreground mb-1 block">Status</label>
                      <select value={hubBoxForm.status} onChange={(e) => setHubBoxForm({ ...hubBoxForm, status: e.target.value })} className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg">
                        {HUB_BOX_STATUS_OPTIONS.map(status => (
                          <option key={status} value={status}>
                            {status}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>
                </div>
                <div className="flex justify-end gap-2 mt-4">
                  <Button variant="outline" onClick={() => setHubBoxModalOpen(false)}>Cancel</Button>
                  <Button onClick={handleSaveHubBox}>{editingHubBox ? 'Update' : 'Add'} Hub Box</Button>
                </div>
              </div>
            </Modal>

            {/* Pole Modal */}
            <Modal isOpen={poleModalOpen} onClose={() => setPoleModalOpen(false)}>
              <div className="bg-card rounded-lg p-4 w-full max-w-lg">
                <h3 className="text-sm font-semibold mb-4">{editingPole ? 'Edit Pole' : 'Add Pole'}</h3>
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput label="Pole Number *" value={poleForm.poleNumber} onChange={(e) => setPoleForm({ ...poleForm, poleNumber: e.target.value })} placeholder="e.g., P-001" />
                    <div>
                      <label className="text-xs font-medium text-muted-foreground mb-1 block">Street</label>
                      <select value={poleForm.streetId} onChange={(e) => setPoleForm({ ...poleForm, streetId: e.target.value })} className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg">
                        <option value="">No Street</option>
                        {infrastructure?.streets?.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                      </select>
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="text-xs font-medium text-muted-foreground mb-1 block">Type</label>
                      <select value={poleForm.poleType} onChange={(e) => setPoleForm({ ...poleForm, poleType: e.target.value })} className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg">
                        <option value="">Select Type</option>
                        {PoleTypes.map(t => <option key={t} value={t}>{t}</option>)}
                      </select>
                    </div>
                    <div>
                      <label className="text-xs font-medium text-muted-foreground mb-1 block">Status</label>
                      <select value={poleForm.status} onChange={(e) => setPoleForm({ ...poleForm, status: e.target.value })} className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg">
                        {Object.entries(PoleStatusLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
                      </select>
                    </div>
                  </div>
                  <TextInput label="Drops Count" type="number" value={poleForm.dropsCount} onChange={(e) => setPoleForm({ ...poleForm, dropsCount: parseInt(e.target.value) || 0 })} />
                  <label className="flex items-center gap-2 text-sm">
                    <input type="checkbox" checked={poleForm.hasExistingFibre} onChange={(e) => setPoleForm({ ...poleForm, hasExistingFibre: e.target.checked })} className="rounded border-input" />
                    Has existing fibre
                  </label>
                </div>
                <div className="flex justify-end gap-2 mt-4">
                  <Button variant="outline" onClick={() => setPoleModalOpen(false)}>Cancel</Button>
                  <Button onClick={handleSavePole}>{editingPole ? 'Update' : 'Add'} Pole</Button>
                </div>
              </div>
            </Modal>
          </div>
        )}

        {/* Default Materials Tab */}
        {activeTab === 'materials' && !isNew && (
          <div className="space-y-6">
            {/* Summary Cards */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Card className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-slate-400 text-sm">Total Materials</p>
                    <p className="text-3xl font-bold text-white">{defaultMaterials.length}</p>
                    <p className="text-xs text-slate-500">configured</p>
                  </div>
                  <div className="p-3 bg-brand-500/10 rounded-lg">
                    <Package className="h-6 w-6 text-brand-500" />
                  </div>
                </div>
              </Card>
              <Card className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-slate-400 text-sm">Order Types</p>
                    <p className="text-3xl font-bold text-white">
                      {Object.values(materialsByJobType).filter(g => g.materials.length > 0).length}
                    </p>
                    <p className="text-xs text-slate-500">with materials</p>
                  </div>
                  <div className="p-3 bg-emerald-500/10 rounded-lg">
                    <Boxes className="h-6 w-6 text-emerald-500" />
                  </div>
                </div>
              </Card>
              <Card className="p-4 bg-gradient-to-br from-slate-800 to-slate-900">
                <div className="flex items-center gap-3 mb-2">
                  <div className="p-2 bg-blue-500/20 rounded">
                    <AlertTriangle className="h-5 w-5 text-blue-400" />
                  </div>
                  <h3 className="text-sm font-semibold text-white">How It Works</h3>
                </div>
                <ul className="text-xs text-slate-400 space-y-1">
                  <li>• Order created → Materials auto-applied</li>
                  <li>• Order completed → Stock deducted</li>
                </ul>
              </Card>
            </div>

            {materialsLoading ? (
              <LoadingSpinner message="Loading materials..." />
            ) : (
              <>
                {/* Materials by Order Type */}
                {orderTypes.filter(ot => ot.code === 'ACT' || ot.code === 'ACTIVATION' || ot.code === 'MOD_OUT' || ot.code === 'MODIFICATION_OUTDOOR' || materialsByJobType[ot.id]?.materials.length > 0).map(orderType => {
                  const group = materialsByJobType[orderType.id] || { orderType, materials: [] };
                  const isActivation = orderType.code === 'ACT' || orderType.code === 'ACTIVATION' || orderType.name?.toLowerCase().includes('activation');
                  const bgColor = isActivation ? 'from-emerald-900/20 to-slate-900' : 'from-blue-900/20 to-slate-900';
                  const iconColor = isActivation ? 'text-emerald-500' : 'text-blue-500';
                  
                  return (
                    <Card key={orderType.id} className={cn("p-4 bg-gradient-to-br", bgColor)}>
                      <div className="flex items-center justify-between mb-4">
                        <div className="flex items-center gap-3">
                          <div className={cn("p-2 rounded", isActivation ? "bg-emerald-500/20" : "bg-blue-500/20")}>
                            <Package className={cn("h-5 w-5", iconColor)} />
                          </div>
                          <div>
                            <h3 className="text-lg font-semibold text-white">{orderType.name}</h3>
                            <p className="text-xs text-slate-400">{orderType.code} • {group.materials.length} material(s)</p>
                          </div>
                        </div>
                        <Button
                          size="sm"
                          onClick={() => {
                            setMaterialForm({ orderTypeId: orderType.id, materialId: '', defaultQuantity: '1', notes: '' });
                            setEditingMaterial(null);
                            setMaterialModalOpen(true);
                          }}
                          className="gap-1"
                        >
                          <Plus className="h-3 w-3" />
                          Add Item
                        </Button>
                      </div>

                      {group.materials.length === 0 ? (
                        <p className="text-center text-slate-500 py-6 text-sm">No materials configured for this order type</p>
                      ) : (
                        <div className="overflow-x-auto">
                          <table className="w-full text-sm">
                            <thead>
                              <tr className="border-b border-slate-700">
                                <th className="text-left py-2 px-2 text-xs font-medium text-slate-400">Material Code</th>
                                <th className="text-left py-2 px-2 text-xs font-medium text-slate-400">Description</th>
                                <th className="text-center py-2 px-2 text-xs font-medium text-slate-400">Qty</th>
                                <th className="text-left py-2 px-2 text-xs font-medium text-slate-400">Notes</th>
                                <th className="text-right py-2 px-2 text-xs font-medium text-slate-400">Actions</th>
                              </tr>
                            </thead>
                            <tbody>
                              {group.materials.map(material => (
                                <tr key={material.id} className="border-b border-slate-700/50 last:border-0 hover:bg-white/5">
                                  <td className="py-2 px-2">
                                    <span className="font-mono text-brand-400">{material.materialCode}</span>
                                  </td>
                                  <td className="py-2 px-2 text-slate-300">{material.materialDescription}</td>
                                  <td className="py-2 px-2 text-center">
                                    <span className="px-2 py-0.5 bg-slate-700 rounded text-white font-medium">
                                      {material.defaultQuantity} {material.materialUnitOfMeasure}
                                    </span>
                                  </td>
                                  <td className="py-2 px-2 text-slate-400 text-xs">{material.notes || '-'}</td>
                                  <td className="py-2 px-2 text-right">
                                    <div className="flex items-center justify-end gap-2">
                                      <button 
                                        onClick={() => openMaterialModal(material)} 
                                        className="p-1.5 rounded hover:bg-white/10 transition-colors"
                                        title="Edit Material"
                                      >
                                        <Edit className="h-4 w-4 text-blue-400 hover:text-blue-300" />
                                      </button>
                                      <button 
                                        onClick={() => handleDeleteMaterial(material.id)} 
                                        className="p-1.5 rounded hover:bg-red-500/20 transition-colors"
                                        title="Delete Material"
                                      >
                                        <Trash2 className="h-4 w-4 text-red-400 hover:text-red-300" />
                                      </button>
                                    </div>
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </Card>
                  );
                })}

                {/* Add Order Type Button */}
                {orderTypes.filter(ot => !materialsByJobType[ot.id]?.materials.length && ot.code !== 'ACT' && ot.code !== 'ACTIVATION' && ot.code !== 'MOD_OUT' && ot.code !== 'MODIFICATION_OUTDOOR').length > 0 && (
                  <Card className="p-4 border-dashed border-2 border-slate-600 bg-transparent">
                    <div className="flex items-center justify-center gap-2 text-slate-500">
                      <Plus className="h-4 w-4" />
                      <span className="text-sm">Add materials for other order types (Assurance, VAS, etc.)</span>
                    </div>
                    <div className="flex flex-wrap gap-2 justify-center mt-3">
                      {orderTypes.filter(ot => !materialsByJobType[ot.id]?.materials.length && ot.code !== 'ACT' && ot.code !== 'ACTIVATION' && ot.code !== 'MOD_OUT' && ot.code !== 'MODIFICATION_OUTDOOR').map(ot => (
                        <Button
                          key={ot.id}
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            setMaterialForm({ orderTypeId: ot.id, materialId: '', defaultQuantity: '1', notes: '' });
                            setEditingMaterial(null);
                            setMaterialModalOpen(true);
                          }}
                        >
                          {ot.name}
                        </Button>
                      ))}
                    </div>
                  </Card>
                )}
              </>
            )}

            {/* Material Modal */}
            <Modal isOpen={materialModalOpen} onClose={() => setMaterialModalOpen(false)}>
              <div className="bg-card rounded-lg p-4 w-full max-w-md">
                <h3 className="text-sm font-semibold mb-4">
                  {editingMaterial ? 'Edit Default Material' : 'Add Default Material'}
                </h3>
                <div className="space-y-3">
                  {!editingMaterial && (
                    <>
                      <div>
                        <label className="text-xs font-medium text-muted-foreground mb-1 block">Order Type *</label>
                        <select
                          value={materialForm.orderTypeId}
                          onChange={(e) => setMaterialForm({ ...materialForm, orderTypeId: e.target.value })}
                          className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg"
                          disabled={editingMaterial}
                        >
                          <option value="">Select Order Type</option>
                          {orderTypes.map(ot => (
                            <option key={ot.id} value={ot.id}>{ot.name} ({ot.code})</option>
                          ))}
                        </select>
                      </div>
                      <div>
                        <label className="text-xs font-medium text-muted-foreground mb-1 block">Material *</label>
                        <select
                          value={materialForm.materialId}
                          onChange={(e) => setMaterialForm({ ...materialForm, materialId: e.target.value })}
                          className="w-full h-9 px-3 text-sm bg-background border border-input rounded-lg"
                          disabled={editingMaterial}
                        >
                          <option value="">Select Material</option>
                          {availableMaterials.map(m => (
                            <option key={m.id} value={m.id}>{m.itemCode} - {m.description}</option>
                          ))}
                        </select>
                        <p className="text-xs text-muted-foreground mt-1">Only non-serialized materials are shown</p>
                      </div>
                    </>
                  )}
                  {editingMaterial && (
                    <div className="p-3 bg-muted rounded-lg">
                      <p className="text-sm font-medium">{editingMaterial.materialCode}</p>
                      <p className="text-xs text-muted-foreground">{editingMaterial.materialDescription}</p>
                    </div>
                  )}
                  <TextInput
                    label="Default Quantity *"
                    type="number"
                    step="0.01"
                    min="0.01"
                    value={materialForm.defaultQuantity}
                    onChange={(e) => setMaterialForm({ ...materialForm, defaultQuantity: e.target.value })}
                  />
                  <TextInput
                    label="Notes"
                    value={materialForm.notes}
                    onChange={(e) => setMaterialForm({ ...materialForm, notes: e.target.value })}
                    placeholder="Optional notes..."
                  />
                </div>
                <div className="flex justify-end gap-2 mt-4">
                  <Button variant="outline" onClick={() => setMaterialModalOpen(false)}>Cancel</Button>
                  <Button 
                    onClick={handleSaveMaterial}
                    disabled={!editingMaterial && (!materialForm.orderTypeId || !materialForm.materialId)}
                  >
                    {editingMaterial ? 'Update' : 'Add'} Material
                  </Button>
                </div>
              </div>
            </Modal>
          </div>
        )}
      </div>
      </div>
    </PageShell>
  );
};

export default BuildingDetailPage;
