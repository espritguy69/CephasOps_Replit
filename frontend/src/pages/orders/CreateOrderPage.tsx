import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { useFieldArray, useForm, useWatch } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Save, X, Plus, Trash2, AlertTriangle, Building2, User, Wrench, Package, FileText, Check, XCircle } from 'lucide-react';
import { Button, LoadingSpinner, useToast, Card } from '../../components/ui';
import { PageShell } from '../../components/layout';
import DatePicker from '../../components/ui/DatePicker';
import TimePicker from '../../components/ui/TimePicker';
import { cn } from '../../lib/utils';
import { createOrder } from '../../api/orders';
import { getBuildings } from '../../api/buildings';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { getOrderTypeParents, getOrderTypeSubtypes, type OrderTypeDto } from '../../api/orderTypes';
import { getOrderCategories } from '../../api/orderCategories';
import { getPartners } from '../../api/partners';
import { getInstallationMethods } from '../../api/installationMethods';
import { getBuildingDefaultMaterials } from '../../api/buildingDefaultMaterials';
import QuickBuildingModal from '../../components/buildings/QuickBuildingModal';
import { parseAddressForBuilding } from '../../utils/addressParser';
import type { BuildingDefaultMaterial } from '../../types/buildingDefaultMaterials';
import { getMaterials } from '../../api/inventory';
import apiClient from '../../api/client';
import { getApiBaseUrl } from '../../api/config';
import { getParseSession, getParsedOrderDraft, updateParsedOrderDraft, approveParsedOrderDraft, rejectParsedOrderDraft } from '../../api/parser';
import type { ParsedOrderDraft } from '../../api/parser';
import type { Order, OrderMaterialReplacement, OrderNonSerialisedReplacement } from '../../types/orders';
import type { Building } from '../../types/buildings';
import type { ServiceInstaller } from '../../types/serviceInstallers';
import type { Partner } from '../../types/partners';
import type { Material } from '../../types/inventory';
import type { InstallationMethod } from '../../types/installationMethods';

// ============================================================================
// Types
// ============================================================================

type OrderStatusOption = { value: string; label: string };
type ParsedOrder = Record<string, unknown>;
type BuildingOption = Building & { fullAddress?: string; buildingType?: string; installationMethodId?: string };

interface MaterialRow {
  id: string;
  materialId: string;
  materialType: string;
  serialNumber?: string;
  isDefault?: boolean;
  isSerialized?: boolean;
}

interface RmaRow {
  id: string;
  oldMaterialId: string;
  oldMaterialType: string;
  oldSerialNumber: string;
  newMaterialId: string;
  newMaterialType: string;
  newSerialNumber: string;
  approvedBy: string;
  approvalNotes: string;
}

interface NonSerialReplacementRow {
  id: string;
  materialId: string;
  materialType: string;
  quantityReplaced: number;
  remark: string;
}

interface OrderFormValues {
  // Status & Assignment
  status: string;
  serviceInstallerId: string;
  supportInstallerId: string;
  teamCrew: string;
  // Order Identification
  serviceIdType?: 'Tbbn' | 'PartnerServiceId';
  serviceId: string;
  orderTypeParentId: string;
  orderSubTypeId: string;
  orderCategoryId: string;
  partnerId: string;
  appointmentDate: string;
  appointmentTime: string;
  // Assurance fields
  ticketNumber: string;
  awoNumber: string;
  issue: string;
  solution: string;
  // Customer Info
  customerName: string;
  contactNo1: string;
  contactNo2: string;
  address: string;
  block: string;
  level: string;
  unit: string;
  // Building
  buildingId: string;
  buildingType: string;
  installationMethod: string;
  installationMethodId: string;
  // Modification fields
  oldAddress: string;
  newAddress: string;
  indoorRemark: string;
  // Network Info
  networkPackage: string;
  networkBandwidth: string;
  networkLoginId: string;
  networkPassword: string;
  networkWanIp: string;
  networkLanIp: string;
  networkGateway: string;
  networkSubnetMask: string;
  onuPassword: string;
  // VOIP
  voipServiceId: string;
  voipPassword: string;
  voipIpAddressOnu: string;
  voipGatewayOnu: string;
  voipSubnetMaskOnu: string;
  voipIpAddressSrp: string;
  voipRemarks: string;
  // Splitter
  splitterNumber: string;
  splitterLocation: string;
  splitterPort: string;
  // Materials
  materials: MaterialRow[];
  // RMA (Assurance only)
  rmaRows: RmaRow[];
  nonSerialReplacements: NonSerialReplacementRow[];
}

// ============================================================================
// Constants
// ============================================================================

const STATUS_OPTIONS: OrderStatusOption[] = [
  { value: 'Pending', label: 'Pending' },
  { value: 'Assigned', label: 'Assigned' },
  { value: 'OnTheWay', label: 'On The Way' },
  { value: 'MetCustomer', label: 'Met Customer' },
  { value: 'OrderCompleted', label: 'Order Completed' },
  { value: 'DocketsReceived', label: 'Dockets Received' },
  { value: 'DocketsVerified', label: 'Dockets Verified' },
  { value: 'DocketsRejected', label: 'Dockets Rejected' },
  { value: 'DocketsUploaded', label: 'Dockets Uploaded' },
  { value: 'ReadyForInvoice', label: 'Ready For Invoice' },
  { value: 'Invoiced', label: 'Invoiced' },
  { value: 'Completed', label: 'Completed' },
];

// ============================================================================
// Helpers
// ============================================================================

/**
 * Split contact numbers by "/" and normalize
 */
const splitContactNumbers = (input: string): { contact1: string; contact2: string } => {
  if (!input) return { contact1: '', contact2: '' };
  
  const normalized = input.replace(/\s+/g, '').replace(/\+60/g, '0');
  const parts = normalized.split('/').filter(Boolean).map(p => p.trim());
  
  if (parts.length === 0) return { contact1: '', contact2: '' };
  if (parts.length === 1) return { contact1: parts[0], contact2: '' };
  
  // If both numbers are identical, don't duplicate
  if (parts[0] === parts[1]) return { contact1: parts[0], contact2: '' };
  
  return { contact1: parts[0], contact2: parts[1] };
};

/**
 * Check if status allows editing splitter/serial fields
 */
const canEditSplitterFields = (status: string): boolean => {
  const editableStatuses = ['MetCustomer', 'OrderCompleted', 'DocketsReceived', 'DocketsVerified', 'DocketsRejected', 'DocketsUploaded', 'ReadyForInvoice', 'Invoiced', 'Completed'];
  return editableStatuses.includes(status);
};

/**
 * Detect Service ID type from value
 */
const detectServiceIdType = (serviceId: string): 'Tbbn' | 'PartnerServiceId' | undefined => {
  if (!serviceId) return undefined;
  
  const trimmed = serviceId.trim().toUpperCase();
  
  // TBBN pattern: TBBN followed by optional letter (A/B) and digits, ending with optional letter
  const tbbnPattern = /^TBBN[A-Z]?\d+[A-Z]?$/;
  if (tbbnPattern.test(trimmed)) {
    return 'Tbbn';
  }
  
  // Partner Service ID patterns
  const partnerPatterns = [
    /^DIGI\d+$/,
    /^DIGI00\d+$/,
    /^CELCOM\d+$/,
    /^CELCOM00\d+$/,
    /^CELCOMDIGI\d+$/,
    /^CELCOMDIGI00\d+$/,
    /^UMOBILE\d+$/,
    /^UMOBILE00\d+$/,
  ];
  
  if (partnerPatterns.some(pattern => pattern.test(trimmed))) {
    return 'PartnerServiceId';
  }
  
  // Default: if starts with TBBN, assume TBBN
  if (trimmed.startsWith('TBBN')) {
    return 'Tbbn';
  }
  
  // Otherwise, assume Partner Service ID
  return 'PartnerServiceId';
};

/**
 * Auto-detect partner from Service ID, Order Type (parent code), Building, or Installation Method
 */
const autoDetectPartner = (
  serviceId: string,
  parentCode: string | undefined,
  buildingType: string,
  installationMethod: string,
  partners: Partner[]
): string | undefined => {
  if (parentCode?.toUpperCase() === 'ASSURANCE') {
    const timeAssurance = partners.find(p =>
      p.name?.toLowerCase().includes('time') &&
      p.name?.toLowerCase().includes('assurance')
    );
    if (timeAssurance) return timeAssurance.id;
  }
  
  // FTTO installation → TIME FTTO
  if (installationMethod?.toLowerCase().includes('ftto') || 
      buildingType?.toLowerCase().includes('ftto')) {
    const timeFtto = partners.find(p => 
      p.name?.toLowerCase().includes('time') && 
      p.name?.toLowerCase().includes('ftto')
    );
    if (timeFtto) return timeFtto.id;
  }
  
  // Service ID detection
  if (serviceId) {
    const trimmed = serviceId.trim().toUpperCase();
    
    // Digi patterns
    if (trimmed.startsWith('DIGI')) {
      const digi = partners.find(p => p.name?.toLowerCase().includes('digi'));
      if (digi) return digi.id;
    }
    
    // Celcom patterns
    if (trimmed.startsWith('CELCOM')) {
      const celcom = partners.find(p => p.name?.toLowerCase().includes('celcom'));
      if (celcom) return celcom.id;
    }
    
    // U Mobile patterns
    if (trimmed.startsWith('UMOBILE')) {
      const umobile = partners.find(p => p.name?.toLowerCase().includes('umobile') || p.name?.toLowerCase().includes('u mobile'));
      if (umobile) return umobile.id;
    }
    
    // TBBN → TIME
    if (trimmed.startsWith('TBBN')) {
      const time = partners.find(p => p.name?.toLowerCase().includes('time') && !p.name?.toLowerCase().includes('assurance') && !p.name?.toLowerCase().includes('ftto'));
      if (time) return time.id;
    }
  }
  
  return undefined;
};

// ============================================================================
// Validation Schema
// ============================================================================

const ORDER_FORM_SCHEMA = z.object({
  status: z.string().min(1, 'Status is required'),
  serviceInstallerId: z.string().optional(),
  supportInstallerId: z.string().optional(),
  teamCrew: z.string().optional(),
  serviceId: z.string().min(1, 'Service/TBBN ID is required'),
  orderTypeParentId: z.string().min(1, 'Order Type is required'),
  orderSubTypeId: z.string().optional(),
  orderCategoryId: z.string().optional(),
  partnerId: z.string().min(1, 'Partner is required'),
  appointmentDate: z.string().min(1, 'Appointment date is required'),
  appointmentTime: z.string().min(1, 'Appointment time is required'),
  ticketNumber: z.string().optional(),
  awoNumber: z.string().optional(),
  issue: z.string().optional(),
  solution: z.string().optional(),
  customerName: z.string().min(1, 'Customer name is required'),
  contactNo1: z.string().min(1, 'At least one contact number is required'),
  contactNo2: z.string().optional(),
  address: z.string().min(1, 'Address is required'),
  block: z.string().optional(),
  level: z.string().optional(),
  unit: z.string().optional(),
  buildingId: z.string().min(1, 'Building is required'),
  buildingType: z.string().optional(),
  installationMethod: z.string().optional(),
  installationMethodId: z.string().optional(),
  oldAddress: z.string().optional(),
  newAddress: z.string().optional(),
  indoorRemark: z.string().optional(),
  splitterNumber: z.string().optional(),
  splitterLocation: z.string().optional(),
  splitterPort: z.string().optional(),
  // Network Info
  networkPackage: z.string().optional(),
  networkBandwidth: z.string().optional(),
  networkLoginId: z.string().optional(),
  networkPassword: z.string().optional(),
  networkWanIp: z.string().optional(),
  networkLanIp: z.string().optional(),
  networkGateway: z.string().optional(),
  networkSubnetMask: z.string().optional(),
  // VOIP
  voipServiceId: z.string().optional(),
  voipPassword: z.string().optional(),
  voipIpAddressOnu: z.string().optional(),
  voipGatewayOnu: z.string().optional(),
  voipSubnetMaskOnu: z.string().optional(),
  voipIpAddressSrp: z.string().optional(),
  voipRemarks: z.string().optional(),
  materials: z.array(z.object({
    id: z.string(),
    materialId: z.string(),
    materialType: z.string(),
    serialNumber: z.string().optional(),
    isDefault: z.boolean().optional(),
    isSerialized: z.boolean().optional(),
  })),
  rmaRows: z.array(z.object({
    id: z.string(),
    oldMaterialId: z.string(),
    oldMaterialType: z.string(),
    oldSerialNumber: z.string(),
    newMaterialId: z.string(),
    newMaterialType: z.string(),
    newSerialNumber: z.string(),
    approvedBy: z.string(),
    approvalNotes: z.string(),
  })),
  nonSerialReplacements: z.array(z.object({
    id: z.string(),
    materialId: z.string(),
    materialType: z.string(),
    quantityReplaced: z.number(),
    remark: z.string(),
  })),
});

// ============================================================================
// Component
// ============================================================================

const CreateOrderPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const parsedOrder: ParsedOrder | null = location.state?.parsedOrder || location.state?.order || null;
  
  // Check if this is from the parser review page
  const draftIdFromUrl = searchParams.get('draftId');
  const fromParser = location.state?.fromParser === true || Boolean(draftIdFromUrl);
  const draftId = location.state?.draftId as string | undefined || draftIdFromUrl || undefined;
  const parseSessionId = location.state?.parseSessionId as string | undefined;
  const initialSnapshotUrl = location.state?.snapshotUrl as string | undefined;
  
  // Review mode: true when draftId is present (editing/reviewing a draft)
  const isReviewMode = Boolean(draftId);
  
  const { showSuccess, showError } = useToast();
  
  // Reference data: parent order types and subtypes for selected parent
  const [parentOrderTypes, setParentOrderTypes] = useState<OrderTypeDto[]>([]);
  const [subtypeOrderTypes, setSubtypeOrderTypes] = useState<OrderTypeDto[]>([]);
  const [serviceInstallers, setServiceInstallers] = useState<ServiceInstaller[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [buildings, setBuildings] = useState<BuildingOption[]>([]);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [orderCategories, setOrderCategories] = useState<{ id: string; name: string; code?: string }[]>([]);
  const [materialCatalog, setMaterialCatalog] = useState<Material[]>([]);
  const [loadingDefaultMaterials, setLoadingDefaultMaterials] = useState(false);
  const [defaultMaterialsLoaded, setDefaultMaterialsLoaded] = useState(false);
  
  // Loading states
  const [loadingSettings, setLoadingSettings] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [approving, setApproving] = useState(false);
  const [rejecting, setRejecting] = useState(false);

  /** When draft is loaded from ?draftId= URL (no location.state), use this for banner and additional info. */
  const [loadedDraftFromUrl, setLoadedDraftFromUrl] = useState<ParsedOrderDraft | null>(null);
  /** Set when draft referenced a subtype that is missing or inactive; user must pick a valid subtype. */
  const [draftSubtypeUnavailable, setDraftSubtypeUnavailable] = useState(false);
  /** Set when draft referenced an inactive parent order type; user must select an active order type. */
  const [draftInactiveParentWarning, setDraftInactiveParentWarning] = useState(false);
  // Snapshot viewer state
  const [snapshotUrl, setSnapshotUrl] = useState<string | undefined>(initialSnapshotUrl);
  const [showSnapshot, setShowSnapshot] = useState<boolean>(Boolean(fromParser && initialSnapshotUrl));
  
  // Building modal state
  const [showBuildingModal, setShowBuildingModal] = useState(false);
  /** When set, modal was opened from approve failure (building required); after create we update draft and retry approve. */
  const [buildingModalFromApproval, setBuildingModalFromApproval] = useState<{
    draftId: string;
    buildingDetection?: {
      detectedBuildingName?: string;
      detectedAddress?: string;
      detectedCity?: string;
      detectedState?: string;
      detectedPostcode?: string;
    };
  } | null>(null);
  const [snapshotPos, setSnapshotPos] = useState<{ x: number; y: number }>({
    x: Math.max(typeof window !== 'undefined' ? window.innerWidth - 440 : 0, 16),
    y: 72,
  });
  const draggingRef = useRef(false);
  const dragOffsetRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });

  // Form setup
  const defaultValues: OrderFormValues = useMemo(() => ({
    status: (parsedOrder?.status as string) || 'Pending',
    serviceInstallerId: (parsedOrder?.serviceInstallerId as string) || '',
    supportInstallerId: '',
    teamCrew: '',
    serviceId: (parsedOrder?.serviceId as string) || '',
    orderTypeParentId: '',
    orderSubTypeId: '',
    orderCategoryId: '',
    partnerId: (parsedOrder?.partnerId as string) || '',
    appointmentDate: '',
    appointmentTime: '',
    ticketNumber: (parsedOrder?.ticketNumber as string) || '',
    awoNumber: (parsedOrder?.awoNumber as string) || '',
    issue: (parsedOrder?.issue as string) || '',
    solution: '',
    customerName: (parsedOrder?.customerName as string) || '',
    contactNo1: (parsedOrder?.customerPhone as string) || '',
    contactNo2: '',
    address: (parsedOrder?.address as string) || '',
    block: (parsedOrder?.block as string) || '',
    level: (parsedOrder?.level as string) || '',
    unit: (parsedOrder?.unit as string) || '',
    buildingId: (parsedOrder?.buildingId as string) || '',
    buildingType: '',
    installationMethod: '',
    installationMethodId: '',
    oldAddress: (parsedOrder?.oldAddress as string) || '',
    newAddress: '',
    indoorRemark: (parsedOrder?.indoorRemark as string) || '',
    // Network Info
    networkPackage: (parsedOrder?.networkPackage as string) || (parsedOrder?.packageName as string) || '',
    networkBandwidth: (parsedOrder?.networkBandwidth as string) || (parsedOrder?.bandwidth as string) || '',
    networkLoginId: (parsedOrder?.networkLoginId as string) || (parsedOrder?.username as string) || (parsedOrder?.loginId as string) || '',
    networkPassword: (parsedOrder?.networkPassword as string) || (parsedOrder?.password as string) || '',
    networkWanIp: (parsedOrder?.networkWanIp as string) || (parsedOrder?.internetWanIp as string) || '',
    networkLanIp: (parsedOrder?.networkLanIp as string) || (parsedOrder?.internetLanIp as string) || '',
    networkGateway: (parsedOrder?.networkGateway as string) || (parsedOrder?.internetGateway as string) || '',
    networkSubnetMask: (parsedOrder?.networkSubnetMask as string) || (parsedOrder?.internetSubnetMask as string) || '',
    onuPassword: (parsedOrder?.onuPassword as string) || '',
    // VOIP
    voipServiceId: (parsedOrder?.voipServiceId as string) || '',
    voipPassword: (parsedOrder?.voipPassword as string) || '',
    voipIpAddressOnu: (parsedOrder?.voipIpAddressOnu as string) || (parsedOrder?.voipOnuIp as string) || '',
    voipGatewayOnu: (parsedOrder?.voipGatewayOnu as string) || (parsedOrder?.voipGateway as string) || '',
    voipSubnetMaskOnu: (parsedOrder?.voipSubnetMaskOnu as string) || (parsedOrder?.voipSubnetMask as string) || '',
    voipIpAddressSrp: (parsedOrder?.voipIpAddressSrp as string) || (parsedOrder?.voipSrpIp as string) || '',
    voipRemarks: (parsedOrder?.voipRemarks as string) || '',
    splitterNumber: '',
    splitterLocation: '',
    splitterPort: '',
    materials: [],
    rmaRows: [],
    nonSerialReplacements: [],
  }), [parsedOrder]);

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    setError,
    formState: { errors },
  } = useForm<OrderFormValues>({
    resolver: zodResolver(ORDER_FORM_SCHEMA),
    defaultValues,
  });

  const { fields: materialFields, append: appendMaterial, remove: removeMaterial } = useFieldArray({
    control,
    name: 'materials',
  });

  const { fields: rmaFields, append: appendRma, remove: removeRma } = useFieldArray({
    control,
    name: 'rmaRows',
  });

  const { fields: nonSerialFields, append: appendNonSerial, remove: removeNonSerial } = useFieldArray({
    control,
    name: 'nonSerialReplacements',
  });

  // Watch values
  const statusValue = watch('status');
  const orderTypeParentIdValue = watch('orderTypeParentId');
  const orderSubTypeIdValue = watch('orderSubTypeId');
  const buildingIdValue = watch('buildingId');
  const contactNo1Value = watch('contactNo1');
  const serviceIdValue = watch('serviceId');
  const buildingTypeValue = watch('buildingType');
  const installationMethodValue = watch('installationMethod');

  const selectedParent = useMemo(() => parentOrderTypes.find(p => p.id === orderTypeParentIdValue), [parentOrderTypes, orderTypeParentIdValue]);
  const selectedSubtype = useMemo(() => subtypeOrderTypes.find(s => s.id === orderSubTypeIdValue), [subtypeOrderTypes, orderSubTypeIdValue]);
  const parentCode = selectedParent?.code?.toUpperCase();
  const subtypeCode = selectedSubtype?.code?.toUpperCase();
  const hasSubType = (selectedParent?.childCount ?? 0) > 0;
  const isAssurance = parentCode === 'ASSURANCE';
  const isModification = parentCode === 'MODIFICATION';
  const isValueAddedService = parentCode === 'VALUE_ADDED_SERVICE';
  const isIndoor = isModification && subtypeCode === 'INDOOR';
  const isOutdoor = isModification && subtypeCode === 'OUTDOOR';
  const splitterFieldsLocked = !canEditSplitterFields(statusValue);

  // Load subtypes when parent changes; clear subtype selection when parent changes so old value does not apply
  useEffect(() => {
    setDraftSubtypeUnavailable(false);
    setDraftInactiveParentWarning(false);
    if (!orderTypeParentIdValue) {
      setSubtypeOrderTypes([]);
      setValue('orderSubTypeId', '', { shouldValidate: true });
      return;
    }
    setValue('orderSubTypeId', '', { shouldValidate: true });
    getOrderTypeSubtypes(orderTypeParentIdValue, { isActive: true })
      .then((data) => setSubtypeOrderTypes(Array.isArray(data) ? data : []))
      .catch(() => setSubtypeOrderTypes([]));
  }, [orderTypeParentIdValue, setValue]);

  // Selected building
  const selectedBuilding = useMemo(() => 
    buildings.find(b => b.id === buildingIdValue),
    [buildings, buildingIdValue]
  );

  // Material options for dropdowns
  const materialOptions = useMemo(() => {
    return materialCatalog.map(m => ({
      value: m.id,
      label: m.code ? `${m.code} - ${m.name}` : m.name,
      isSerialized: m.isSerialised,
    }));
  }, [materialCatalog]);

  // ============================================================================
  // Data Loading
  // ============================================================================

  const loadSettings = useCallback(async () => {
    try {
      setLoadingSettings(true);
      setLoadError(null);

      const [
        orderTypesRes,
        installersRes,
        buildingsRes,
        partnersRes,
        methodsRes,
        categoriesRes,
        materialsRes,
      ] = await Promise.all([
        getOrderTypeParents({ isActive: true }).catch(() => []),
        getServiceInstallers({ isActive: true }).catch(() => []),
        getBuildings({ isActive: true }).catch(() => []),
        getPartners({ isActive: true }).catch(() => []),
        getInstallationMethods({ isActive: true }).catch(() => []),
        getOrderCategories({ isActive: true }).catch(() => []),
        getMaterials({ isActive: true }).catch(() => []),
      ]);

      setParentOrderTypes(Array.isArray(orderTypesRes) ? orderTypesRes : []);
      setServiceInstallers(Array.isArray(installersRes) ? installersRes : []);
      setBuildings(Array.isArray(buildingsRes) ? buildingsRes : []);
      setPartners(Array.isArray(partnersRes) ? partnersRes : []);
      setInstallationMethods(Array.isArray(methodsRes) ? methodsRes : []);
      setOrderCategories(Array.isArray(categoriesRes) ? categoriesRes : []);
      setMaterialCatalog(Array.isArray(materialsRes) ? materialsRes : []);
    } catch (error) {
      console.error('Failed to load settings:', error);
      setLoadError('Failed to load form data. Please refresh.');
      showError('Failed to load form data');
    } finally {
      setLoadingSettings(false);
    }
  }, [showError]);

  // Reload buildings list (used after creating new building)
  const reloadBuildings = useCallback(async () => {
    try {
      const buildingsRes = await getBuildings({ isActive: true });
      setBuildings(Array.isArray(buildingsRes) ? buildingsRes : []);
    } catch (error) {
      console.error('Failed to reload buildings:', error);
    }
  }, []);

  // Handle building creation from modal
  const handleBuildingCreated = useCallback(async (buildingId: string) => {
    await reloadBuildings();
    setValue('buildingId', buildingId);
    setShowBuildingModal(false);

    const fromApproval = buildingModalFromApproval;
    if (fromApproval) {
      setBuildingModalFromApproval(null);
      try {
        await updateParsedOrderDraft(fromApproval.draftId, { buildingId });
        await approveParsedOrderDraft(fromApproval.draftId);
        showSuccess('Building created and draft approved successfully');
        navigate('/orders/parser');
      } catch (err: any) {
        showError(err.message || 'Failed to approve draft after creating building');
      }
      return;
    }

    showSuccess('Building created and selected');
  }, [reloadBuildings, setValue, showSuccess, buildingModalFromApproval, navigate]);

  // Handle building selection from modal
  const handleBuildingSelected = useCallback(async (buildingId: string) => {
    setValue('buildingId', buildingId);
    setShowBuildingModal(false);
    showSuccess('Building selected');
  }, [setValue, showSuccess]);

  // Load draft data from URL if draftId is present
  useEffect(() => {
    const loadDraftFromUrl = async () => {
      if (draftIdFromUrl && !parsedOrder && !loadingSettings) {
        setLoadedDraftFromUrl(null);
        try {
          const draft = await getParsedOrderDraft(draftIdFromUrl);
          setLoadedDraftFromUrl(draft);

          // Resolve parent and subtype from draft codes (API-driven)
          const code = (draft.orderTypeCode || draft.orderTypeHint || '').toString().trim().toUpperCase();
          const deriveParentCode = (): string | null => {
            if (!code) return null;
            if (code.includes('ASSURANCE')) return 'ASSURANCE';
            if (code.includes('MODIFICATION') || code.includes('INDOOR') || code.includes('OUTDOOR') || code.includes('MOD_OUT')) return 'MODIFICATION';
            if (code.includes('ACTIVATION') || code.includes('FTTH') || code.includes('FTTO')) return 'ACTIVATION';
            if (code.includes('VALUE_ADDED') || code.includes('VAS') || code.includes('IAD') || code.includes('FIXED') || code.includes('UPGRADE')) return 'VALUE_ADDED_SERVICE';
            return null;
          };
          const deriveSubtypeCode = (): string | null => {
            const sub = (draft.orderSubType || '').toString().trim().toUpperCase();
            if (sub) return sub;
            if (code.includes('MODIFICATION_INDOOR') || (code.includes('MODIFICATION') && code.includes('INDOOR'))) return 'INDOOR';
            if (code.includes('MODIFICATION_OUTDOOR') || code.includes('MOD_OUT') || (code.includes('OUTDOOR'))) return 'OUTDOOR';
            if (code.includes('REPULL')) return 'REPULL';
            if (code.includes('ASSURANCE')) return 'STANDARD';
            if (code.includes('IAD')) return 'IAD';
            if (code.includes('FIXED')) return 'FIXED_IP';
            if (code.includes('VAS') || code.includes('UPGRADE')) return 'UPGRADE';
            return null;
          };
          const parentCode = deriveParentCode();
          setDraftInactiveParentWarning(false);
          setDraftSubtypeUnavailable(false);
          const parents = await getOrderTypeParents({ isActive: true });
          const parent = parents.find((p: OrderTypeDto) => (p.code || '').toUpperCase() === parentCode);
          if (parent) {
            setValue('orderTypeParentId', parent.id);
            const subtypes = await getOrderTypeSubtypes(parent.id, { isActive: true }).catch(() => []);
            const subtypeList = Array.isArray(subtypes) ? subtypes : [];
            const subtypeCode = deriveSubtypeCode();
            const subtype = subtypeCode ? subtypeList.find((s: OrderTypeDto) => (s.code || '').toUpperCase() === subtypeCode) : null;
            if (subtype) {
              setValue('orderSubTypeId', subtype.id);
              setDraftSubtypeUnavailable(false);
            } else if (subtypeCode && subtypeList.length > 0) {
              setValue('orderSubTypeId', '');
              setDraftSubtypeUnavailable(true);
            } else {
              setDraftSubtypeUnavailable(false);
            }
          } else if (parentCode) {
            setDraftInactiveParentWarning(true);
          }
          if (draft.serviceId) setValue('serviceId', draft.serviceId);
          if (draft.ticketId) setValue('ticketNumber', draft.ticketId);
          if (draft.awoNumber) setValue('awoNumber', draft.awoNumber);
          if (draft.issue) setValue('issue', draft.issue);
          if (draft.additionalContactNumber) setValue('contactNo2', draft.additionalContactNumber);
          if (draft.customerName) setValue('customerName', draft.customerName);
          if (draft.customerPhone) setValue('contactNo1', draft.customerPhone);
          if (draft.customerEmail) setValue('customerEmail', draft.customerEmail);
          if (draft.addressText) setValue('address', draft.addressText);
          if (draft.oldAddress) setValue('oldAddress', draft.oldAddress);
          if (draft.buildingId) setValue('buildingId', draft.buildingId);
          if (draft.partnerId) setValue('partnerId', draft.partnerId); // §2: never override draft.partnerId
          if (draft.packageName) setValue('networkPackage', draft.packageName);
          if (draft.bandwidth) setValue('networkBandwidth', draft.bandwidth);
          if (draft.username) setValue('networkLoginId', draft.username);
          if (draft.password) setValue('networkPassword', draft.password);
          if (draft.onuPassword) setValue('onuPassword', draft.onuPassword);
          if (draft.internetWanIp) setValue('networkWanIp', draft.internetWanIp);
          if (draft.internetLanIp) setValue('networkLanIp', draft.internetLanIp);
          if (draft.internetGateway) setValue('networkGateway', draft.internetGateway);
          if (draft.internetSubnetMask) setValue('networkSubnetMask', draft.internetSubnetMask);
          if (draft.voipServiceId) setValue('voipServiceId', draft.voipServiceId);
          if (draft.remarks) setValue('indoorRemark', draft.remarks); // For Indoor Modification
          
          // Issue 3: API stores appointment in UTC; convert to Malaysia (GMT+8) for display so 02:00Z shows as 10:00
          if (draft.appointmentDate) {
            const appointmentDateStr = draft.appointmentDate;
            if (appointmentDateStr.includes('T')) {
              const utcDate = new Date(appointmentDateStr);
              if (!Number.isNaN(utcDate.getTime())) {
                const malaysiaHours = 8;
                const malaysiaMs = malaysiaHours * 60 * 60 * 1000;
                const forDisplay = new Date(utcDate.getTime() + malaysiaMs);
                const date = forDisplay.toISOString().slice(0, 10);
                const time = forDisplay.toISOString().slice(11, 16);
                setValue('appointmentDate', date);
                setValue('appointmentTime', time);
              } else {
                const [datePart, timePart] = appointmentDateStr.split('T');
                setValue('appointmentDate', datePart || '');
                setValue('appointmentTime', timePart ? timePart.substring(0, 5) : '09:00');
              }
            } else {
              setValue('appointmentDate', appointmentDateStr);
              setValue('appointmentTime', draft.appointmentWindow || '09:00');
            }
          }
          
          // Load parse session for snapshot
          if (draft.parseSessionId) {
            try {
              const session = await getParseSession(draft.parseSessionId);
              if (session?.snapshotFileId) {
                const url = `${getApiBaseUrl()}/files/${session.snapshotFileId}/download`;
                setSnapshotUrl(url);
                setShowSnapshot(true);
              }
            } catch (err) {
              console.warn('Failed to load session snapshot', err);
            }
          }
        } catch (err) {
          showError('Failed to load draft data');
          console.error('Error loading draft:', err);
        }
      }
    };
    
    if (!loadingSettings) {
      loadDraftFromUrl();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [draftIdFromUrl, parsedOrder, loadingSettings]);

  // When parent has subtypes: default to first if current selection invalid. When current selection not in active list (e.g. inactive), clear and show message. Skip auto-select when draft referenced an unavailable subtype.
  useEffect(() => {
    if (!orderTypeParentIdValue) return;
    if (subtypeOrderTypes.length === 0) {
      setValue('orderSubTypeId', '', { shouldValidate: true });
      return;
    }
    const currentSubId = watch('orderSubTypeId');
    const valid = currentSubId && subtypeOrderTypes.some(s => s.id === currentSubId);
    if (valid) setDraftSubtypeUnavailable(false);
    if (currentSubId && !valid) {
      setValue('orderSubTypeId', '', { shouldValidate: true });
      setDraftSubtypeUnavailable(true);
      return;
    }
    if (draftSubtypeUnavailable) return;
    if (!valid && subtypeOrderTypes[0]) setValue('orderSubTypeId', subtypeOrderTypes[0].id, { shouldValidate: true });
  }, [orderTypeParentIdValue, subtypeOrderTypes, setValue, watch, draftSubtypeUnavailable]);

  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  // Fetch snapshot URL when coming from parser if not already provided
  useEffect(() => {
    const fetchSnapshot = async () => {
      if (!fromParser || snapshotUrl || !parseSessionId) return;
      try {
        const session = await getParseSession(parseSessionId);
        if (session?.snapshotFileId) {
          const url = `${getApiBaseUrl()}/files/${session.snapshotFileId}/download`;
          setSnapshotUrl(url);
          setShowSnapshot(true);
        }
      } catch (err) {
        console.warn('Failed to fetch snapshot URL', err);
      }
    };
    fetchSnapshot();
  }, [fromParser, parseSessionId, snapshotUrl]);

  // Draggable handlers for snapshot popup
  const onDragMouseDown = (e: React.MouseEvent) => {
    draggingRef.current = true;
    dragOffsetRef.current = {
      x: e.clientX - snapshotPos.x,
      y: e.clientY - snapshotPos.y,
    };
    document.addEventListener('mousemove', onDragMouseMove);
    document.addEventListener('mouseup', onDragMouseUp);
  };

  const onDragMouseMove = (e: MouseEvent) => {
    if (!draggingRef.current) return;
    const next = { x: e.clientX - dragOffsetRef.current.x, y: e.clientY - dragOffsetRef.current.y };
    const clamped = {
      x: Math.min(Math.max(next.x, 8), (typeof window !== 'undefined' ? window.innerWidth - 440 : next.x)),
      y: Math.min(Math.max(next.y, 8), (typeof window !== 'undefined' ? window.innerHeight - 200 : next.y)),
    };
    setSnapshotPos(clamped);
  };

  const onDragMouseUp = () => {
    draggingRef.current = false;
    document.removeEventListener('mousemove', onDragMouseMove);
    document.removeEventListener('mouseup', onDragMouseUp);
  };

  useEffect(() => {
    return () => {
      document.removeEventListener('mousemove', onDragMouseMove);
      document.removeEventListener('mouseup', onDragMouseUp);
    };
  }, []);

  // Auto-detect Service ID type; partner auto-detect only for manual orders (UNIFIED_ORDER_SYSTEM_SPEC §2: never override draft.partnerId)
  useEffect(() => {
    if (serviceIdValue) {
      const detectedType = detectServiceIdType(serviceIdValue);
      if (detectedType) setValue('serviceIdType', detectedType);
      if (draftIdFromUrl || fromParser) return; // Never auto-set partner when from parser/draft
      const autoPartnerId = autoDetectPartner(
        serviceIdValue,
        selectedParent?.code,
        buildingTypeValue,
        installationMethodValue,
        partners
      );
      const currentPartnerId = watch('partnerId');
      if (autoPartnerId && !currentPartnerId) setValue('partnerId', autoPartnerId);
    }
  }, [serviceIdValue, selectedParent?.code, buildingTypeValue, installationMethodValue, partners, setValue, watch, draftIdFromUrl, fromParser]);

  // Auto-split contact numbers
  useEffect(() => {
    if (contactNo1Value?.includes('/')) {
      const { contact1, contact2 } = splitContactNumbers(contactNo1Value);
      setValue('contactNo1', contact1);
      if (contact2) setValue('contactNo2', contact2);
    }
  }, [contactNo1Value, setValue]);

  // Auto-fill building details
  useEffect(() => {
    if (selectedBuilding) {
      setValue('buildingType', selectedBuilding.buildingType || selectedBuilding.propertyType || '');
      
      // Find installation method
      const methodId = selectedBuilding.installationMethodId;
      if (methodId) {
        setValue('installationMethodId', methodId);
        const method = installationMethods.find(m => m.id === methodId);
        if (method) setValue('installationMethod', method.name);
      }
      
      // Auto-fill address if empty
      const currentAddress = watch('address');
      if (!currentAddress && selectedBuilding.fullAddress) {
        setValue('address', selectedBuilding.fullAddress);
      }
    }
  }, [selectedBuilding, installationMethods, setValue, watch]);

  // Resolved leaf order type ID (subtype if selected, else parent)
  const resolvedOrderTypeId = orderSubTypeIdValue || orderTypeParentIdValue;

  // Load default materials when building or order type changes
  useEffect(() => {
    if (materialCatalog.length === 0) return;
    if (!buildingIdValue || !resolvedOrderTypeId) {
      setValue('materials', []);
      setDefaultMaterialsLoaded(false);
      return;
    }
    const loadDefaultMaterials = async () => {
      setLoadingDefaultMaterials(true);
      setDefaultMaterialsLoaded(false);
      try {
        if (parentCode !== 'ACTIVATION') {
          setValue('materials', []);
          setDefaultMaterialsLoaded(false);
          return;
        }
        const response = await getBuildingDefaultMaterials(buildingIdValue, {
          orderTypeId: resolvedOrderTypeId,
          isActive: true,
        });
        
        if (Array.isArray(response) && response.length > 0) {
          // Map default materials to form format, matching dropdown labels
          const defaultMaterials: MaterialRow[] = response.map((item: BuildingDefaultMaterial, idx: number) => {
            // Find matching material in catalog to get the correct label format
            const material = materialCatalog.find(m => m.id === item.materialId);
            const materialLabel = material 
              ? (material.code ? `${material.code} - ${material.name}` : material.name)
              : (item.materialDescription || item.materialCode || 'Unknown Material');
            
            return {
              id: `default-${Date.now()}-${idx}`,
              materialId: item.materialId,
              materialType: materialLabel,
              serialNumber: '',
              isDefault: true,
              isSerialized: material?.isSerialised || false,
            };
          });
          
          // Set materials array directly (useFieldArray will pick it up)
          setValue('materials', defaultMaterials, { shouldValidate: false, shouldDirty: false });
          setDefaultMaterialsLoaded(true);
          
          // Show success message
          showSuccess(`Loaded ${defaultMaterials.length} default material(s) from building`);
        } else {
          // No default materials found, clear the array
          setValue('materials', []);
          setDefaultMaterialsLoaded(false);
        }
      } catch (error) {
        console.warn('Failed to load default materials:', error);
        setDefaultMaterialsLoaded(false);
        // Don't clear materials on error, keep existing ones
      } finally {
        setLoadingDefaultMaterials(false);
      }
    };
    
    loadDefaultMaterials();
  }, [buildingIdValue, resolvedOrderTypeId, parentCode, materialCatalog, setValue, showSuccess]);

  // ============================================================================
  // Form Submission
  // ============================================================================

  // Handle approve draft
  const handleApproveDraft = async () => {
    if (!draftId) return;

    try {
      setApproving(true);
      await approveParsedOrderDraft(draftId);
      showSuccess('Draft approved successfully');
      navigate('/orders/parser');
    } catch (err: any) {
      if (err?.status === 400 && err?.data?.errorCode === 'BUILDING_NOT_FOUND') {
        setBuildingModalFromApproval({
          draftId,
          buildingDetection: err.data?.buildingDetection
            ? {
                detectedBuildingName: err.data.buildingDetection.detectedBuildingName,
                detectedAddress: err.data.buildingDetection.detectedAddress,
                detectedCity: err.data.buildingDetection.detectedCity,
                detectedState: err.data.buildingDetection.detectedState,
                detectedPostcode: err.data.buildingDetection.detectedPostcode
              }
            : undefined
        });
        setShowBuildingModal(true);
      } else {
        const msg = err?.message ?? 'Failed to approve draft';
        if (msg.includes('Partner not configured')) {
          const templateSuffix = msg.match(/ \(Parser template:[^)]+\)\.?/);
          showError(
            'Partner not configured. Set PartnerId on the parser template used for this draft.' +
              (templateSuffix ? ` ${templateSuffix[0].trim()}` : '')
          );
        } else {
          showError(msg);
        }
      }
    } finally {
      setApproving(false);
    }
  };

  // Handle reject draft
  const handleRejectDraft = async () => {
    if (!draftId) return;
    
    const notes = window.prompt('Enter rejection reason:');
    if (notes === null) return; // User cancelled
    
    try {
      setRejecting(true);
      await rejectParsedOrderDraft(draftId, { validationNotes: notes || 'Rejected by user' });
      showSuccess('Draft rejected successfully');
      navigate('/orders/parser');
    } catch (err: any) {
      showError(err.message || 'Failed to reject draft');
    } finally {
      setRejecting(false);
    }
  };

  const onSubmit = async (values: OrderFormValues) => {
    try {
      setSaving(true);

      const parent = parentOrderTypes.find(p => p.id === values.orderTypeParentId);
      const subtype = subtypeOrderTypes.find(s => s.id === values.orderSubTypeId);
      const parentCode = parent?.code?.toUpperCase();
      const subtypeCode = subtype?.code?.toUpperCase();

      // Validation: selected parent and subtype must be in active lists (API-derived; prevents inactive from being submitted)
      const parentInList = parentOrderTypes.some(p => p.id === values.orderTypeParentId);
      if (!parentInList || (parent && !parent.isActive)) {
        setError('orderTypeParentId', { message: 'Please select an active order type.' });
        setSaving(false);
        return;
      }
      if (hasSubType) {
        const subtypeInList = subtypeOrderTypes.some(s => s.id === values.orderSubTypeId);
        if (!values.orderSubTypeId || !subtypeInList || (subtype && !subtype.isActive)) {
          setError('orderSubTypeId', { message: 'Please select an active subtype for this order type.' });
          setSaving(false);
          return;
        }
      }

      // Validation: assurance/modification rules (API/code-driven)
      if (parentCode === 'ASSURANCE') {
        if (!values.ticketNumber?.trim()) { setError('ticketNumber', { message: 'Ticket Number is required for Assurance orders' }); setSaving(false); return; }
        if (!values.awoNumber?.trim()) { setError('awoNumber', { message: 'AWO Number is required for Assurance orders' }); setSaving(false); return; }
        if (hasSubType && (!subtypeCode || (subtypeCode !== 'STANDARD' && subtypeCode !== 'REPULL'))) {
          setError('orderSubTypeId', { message: 'Assurance requires SubType: Standard or Repull' }); setSaving(false); return;
        }
      }
      if (parentCode === 'MODIFICATION') {
        if (!subtypeCode || (subtypeCode !== 'INDOOR' && subtypeCode !== 'OUTDOOR')) {
          setError('orderSubTypeId', { message: 'Modification requires SubType: Indoor or Outdoor' }); setSaving(false); return;
        }
        if (subtypeCode === 'OUTDOOR') {
          if (!values.oldAddress?.trim()) { setError('oldAddress', { message: 'Old Address is required for Outdoor Modification' }); setSaving(false); return; }
          if (!values.newAddress?.trim()) { setError('newAddress', { message: 'New Address is required for Outdoor Modification' }); setSaving(false); return; }
        }
        if (subtypeCode === 'INDOOR' && !values.indoorRemark?.trim()) {
          setError('indoorRemark', { message: 'Indoor Relocation Remark is required' }); setSaving(false); return;
        }
      }

      const orderTypeId = values.orderSubTypeId || values.orderTypeParentId;
      const orderSubTypePayload = subtype?.code ?? (parentCode === 'VALUE_ADDED_SERVICE' ? 'UPGRADE' : undefined);

      const appointmentDate = values.appointmentDate;
      const [hours, minutes] = (time || '09:00').split(':').map(Number);
      const appointmentWindowFrom = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:00`;
      const endHour = (hours + 1) % 24;
      const appointmentWindowTo = `${endHour.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:00`;

      const orderPayload: Record<string, unknown> = {
        partnerId: values.partnerId,
        orderTypeId,
        orderSubType: orderSubTypePayload,
        orderCategoryId: values.orderCategoryId || null,
        serviceIdType: values.serviceIdType || undefined,
        serviceId: values.serviceId,
        ticketId: values.ticketNumber || null,
        awoNumber: values.awoNumber || null,
        priority: 'Normal',
        buildingId: values.buildingId,
        unitNo: values.unit || null,
        addressLine1: values.address,
        city: '',
        state: '',
        postcode: '',
        customerName: values.customerName,
        customerPhone: values.contactNo1,
        customerPhone2: values.contactNo2 || null,
        issue: isAssurance ? (values.issue || null) : null,
        solution: isAssurance ? (values.solution || null) : null,
        appointmentDate,
        appointmentWindowFrom,
        appointmentWindowTo,
        installationMethodId: values.installationMethodId || null,
        // Modification fields
        relocationType: subtypeCode === 'OUTDOOR' ? 'Outdoor' : subtypeCode === 'INDOOR' ? 'Indoor' : null,
        oldAddress: values.oldAddress || null,
        oldLocationNote: values.indoorRemark || null,
        // Splitter fields
        splitterNumber: values.splitterNumber || null,
        splitterLocation: values.splitterLocation || null,
        splitterPort: values.splitterPort || null,
        // Network Info fields
        networkPackage: values.networkPackage || null,
        networkBandwidth: values.networkBandwidth || null,
        networkLoginId: values.networkLoginId || null,
        networkPassword: values.networkPassword || null,
        networkWanIp: values.networkWanIp || null,
        networkLanIp: values.networkLanIp || null,
        networkGateway: values.networkGateway || null,
        networkSubnetMask: values.networkSubnetMask || null,
        onupassword: values.onuPassword || null,
        // VOIP fields
        voipServiceId: values.voipServiceId || null,
        voipPassword: values.voipPassword || null,
        voipIpAddressOnu: values.voipIpAddressOnu || null,
        voipGatewayOnu: values.voipGatewayOnu || null,
        voipSubnetMaskOnu: values.voipSubnetMaskOnu || null,
        voipIpAddressSrp: values.voipIpAddressSrp || null,
        voipRemarks: values.voipRemarks || null,
      };

      // If this was from the parser, update the draft first with form values
      if (fromParser && draftId) {
        try {
          // Map form values back to draft update format
          const draftUpdate = {
            serviceId: values.serviceId || undefined,
            ticketId: values.ticketNumber || undefined,
            customerName: values.customerName || undefined,
            customerPhone: values.contactNo1 || undefined,
            customerEmail: values.contactNo2 || undefined,
            addressText: values.address || undefined,
            oldAddress: values.oldAddress || undefined,
            buildingId: values.buildingId || undefined,
            orderTypeCode: parent?.code ?? undefined,
            orderSubType: orderSubTypePayload ?? subtype?.code ?? undefined,
            packageName: values.networkPackage || undefined,
            bandwidth: values.networkBandwidth || undefined,
            username: values.networkLoginId || undefined,
            password: values.networkPassword || undefined,
            onuPassword: values.onuPassword || undefined,
            internetWanIp: values.networkWanIp || undefined,
            internetLanIp: values.networkLanIp || undefined,
            internetGateway: values.networkGateway || undefined,
            internetSubnetMask: values.networkSubnetMask || undefined,
            voipServiceId: values.voipServiceId || undefined,
            remarks: values.indoorRemark || undefined,
          };
          
          await updateParsedOrderDraft(draftId, draftUpdate);
        } catch (updateError) {
          console.warn('Failed to update draft:', updateError);
          // Continue with order creation even if draft update fails
        }
      }
      
      const response = await createOrder(orderPayload as Partial<Order>);
      const createdOrder = response as Order;
      
      // If this was from the parser, mark the draft as approved
      if (fromParser && draftId && createdOrder?.id) {
        try {
          await apiClient.post(`/parser/drafts/${draftId}/mark-approved`, {
            createdOrderId: createdOrder.id
          });
          showSuccess(`Order created from parsed draft! ID: ${createdOrder.id}`);
        } catch (approvalError) {
          console.warn('Failed to mark draft as approved:', approvalError);
          showSuccess(`Order created! ID: ${createdOrder.id} (Note: Draft status update failed)`);
        }
      } else {
        showSuccess(`Order created! ID: ${createdOrder?.id || 'N/A'}`);
      }
      
      // Navigate back to parser review if came from there, otherwise to orders list
      setTimeout(() => {
        if (fromParser) {
          navigate('/orders/parser');
        } else {
          navigate('/orders');
        }
      }, 1000);
    } catch (error) {
      console.error('Error creating order:', error);
      showError('Failed to create order');
    } finally {
      setSaving(false);
    }
  };

  // ============================================================================
  // Render Helpers
  // ============================================================================

  const inputClass = cn(
    'h-8 w-full rounded border border-input bg-background px-2 text-xs',
    'focus:outline-none focus:ring-1 focus:ring-ring',
    'disabled:cursor-not-allowed disabled:bg-muted disabled:text-muted-foreground'
  );

  const selectClass = cn(inputClass, 'appearance-none');

  const labelClass = 'text-[10px] font-medium uppercase tracking-wide text-muted-foreground';

  const sectionClass = 'rounded-lg border border-border bg-card p-2 space-y-2';

  const sectionTitleClass = 'text-xs font-semibold text-foreground flex items-center gap-1.5';

  // ============================================================================
  // Loading State
  // ============================================================================

  if (loadingSettings) {
    return (
      <PageShell title="Create Order" breadcrumbs={[{ label: 'Orders', path: '/orders' }, { label: 'Create' }]}>
        <LoadingSpinner message="Loading form..." fullPage />
      </PageShell>
    );
  }

  // ============================================================================
  // Main Render
  // ============================================================================

  const createOrderBreadcrumbs = fromParser
    ? [{ label: 'Parser', path: '/orders/parser' }, { label: 'Review' }, { label: 'Create' }]
    : [{ label: 'Orders', path: '/orders' }, { label: 'Create' }];

  return (
    <PageShell
      title="Create Order"
      breadcrumbs={createOrderBreadcrumbs}
      actions={
        <div className="flex items-center gap-2">
          {fromParser && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={!snapshotUrl}
              onClick={() => setShowSnapshot((prev) => !prev)}
              className="h-7 px-2 text-xs"
            >
              Snapshot
            </Button>
          )}
          {isReviewMode && (
            <>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleApproveDraft}
                disabled={approving || rejecting}
                className="h-7 px-2 text-xs text-green-600 hover:text-green-700 border-green-600 hover:bg-green-50 dark:hover:bg-green-900/20"
              >
                <Check className="h-3 w-3 mr-1" />
                {approving ? 'Approving...' : 'Approve'}
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleRejectDraft}
                disabled={approving || rejecting}
                className="h-7 px-2 text-xs text-red-600 hover:text-red-700 border-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
              >
                <XCircle className="h-3 w-3 mr-1" />
                {rejecting ? 'Rejecting...' : 'Reject'}
              </Button>
            </>
          )}
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => fromParser ? navigate('/orders/parser') : navigate('/orders')}
            className="h-7 px-2 text-xs"
          >
            <X className="h-3 w-3 mr-1" />
            Cancel
          </Button>
          <Button
            type="submit"
            form="order-form"
            size="sm"
            disabled={saving || approving || rejecting}
            className={cn("h-7 px-2 text-xs", fromParser && "bg-blue-600 hover:bg-blue-700")}
          >
            <Save className="h-3 w-3 mr-1" />
            {saving ? 'Creating...' : fromParser ? 'Create Order' : 'Save'}
          </Button>
        </div>
      }
    >
      <div className="flex-1 overflow-y-auto p-3 space-y-2">
        <form id="order-form" onSubmit={handleSubmit(onSubmit)} className="space-y-2">
          {/* Snapshot Popup */}
          {fromParser && snapshotUrl && showSnapshot && (
            <div
              className="fixed z-50 w-full max-w-[calc(100vw-2rem)] md:w-[420px] h-[400px] md:h-[520px] bg-white border shadow-xl rounded-lg"
              style={{ left: snapshotPos.x, top: snapshotPos.y }}
            >
              <div
                className="flex items-center justify-between px-3 py-2 border-b cursor-move select-none bg-slate-50"
                onMouseDown={onDragMouseDown}
              >
                <div className="text-xs font-semibold text-slate-700">Snapshot</div>
                <Button variant="ghost" size="icon" onClick={() => setShowSnapshot(false)}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
              <div className="h-[calc(520px-42px)]">
                <iframe
                  src={snapshotUrl}
                  title="Snapshot"
                  className="w-full h-full rounded-b-lg"
                />
              </div>
            </div>
          )}
          
          {/* Parser Origin Banner - show when from parser with draft (state or loaded from URL) */}
          {fromParser && (parsedOrder || loadedDraftFromUrl) && (() => {
            const displayDraft = (parsedOrder || loadedDraftFromUrl) as ParsedOrder;
            return (
              <Card className="bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800 p-3">
                <div className="flex items-start gap-3">
                  <FileText className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5 flex-shrink-0" />
                  <div className="flex-1">
                    <h3 className="text-sm font-semibold text-blue-900 dark:text-blue-100">
                      Creating Order from Parsed Data
                    </h3>
                    <p className="text-xs text-blue-700 dark:text-blue-300 mt-1">
                      This form has been pre-filled with data extracted from an email or uploaded file.
                      Please review all fields carefully before saving.
                    </p>
                    {/* UNIFIED_ORDER_SYSTEM_SPEC §7: Parser banner shows OrderType + SubType */}
                    {(displayDraft.orderTypeCode || displayDraft.orderSubType) && (
                      <p className="text-xs text-blue-600 dark:text-blue-400 mt-1">
                        Order: {[displayDraft.orderTypeCode, displayDraft.orderSubType].filter(Boolean).join(' / ')}
                      </p>
                    )}
                    {displayDraft.confidenceScore !== undefined && (
                      <div className="flex items-center gap-2 mt-2">
                        <span className="text-xs text-blue-600 dark:text-blue-400">Confidence Score:</span>
                        <span className={cn(
                          'text-xs font-semibold',
                          (displayDraft.confidenceScore as number) >= 0.8 ? 'text-green-600' :
                          (displayDraft.confidenceScore as number) >= 0.5 ? 'text-yellow-600' : 'text-red-600'
                        )}>
                          {Math.round((displayDraft.confidenceScore as number) * 100)}%
                        </span>
                      </div>
                    )}
                    {displayDraft.validationNotes && (
                      <div className="mt-2 p-2 bg-yellow-100 dark:bg-yellow-900/30 rounded text-xs text-yellow-800 dark:text-yellow-200">
                        <strong>Note:</strong> {displayDraft.validationNotes as string}
                      </div>
                    )}
                  </div>
                </div>
              </Card>
            );
          })()}

          {/* Unmatched parsed materials warning (parser review: backend truth from loaded draft) */}
          {fromParser && loadedDraftFromUrl && (loadedDraftFromUrl.unmatchedMaterialCount ?? 0) > 0 && (
            <div className="rounded-md border border-amber-500/60 bg-amber-500/10 p-3 text-sm text-amber-800 dark:text-amber-200">
              <span className="font-medium">⚠ {loadedDraftFromUrl.unmatchedMaterialCount} material(s) could not be matched</span>
              {Array.isArray(loadedDraftFromUrl.unmatchedMaterialNames) && loadedDraftFromUrl.unmatchedMaterialNames.length > 0 && (
                <ul className="mt-1 list-inside list-disc">
                  {loadedDraftFromUrl.unmatchedMaterialNames.map((name, i) => (
                    <li key={i}>{name}</li>
                  ))}
                </ul>
              )}
            </div>
          )}

          {/* Additional Information (unmapped parser sections) - read-only, shown when present from state or loaded draft */}
          {(parsedOrder?.additionalInformation || loadedDraftFromUrl?.additionalInformation) && (
            <Card className="p-3">
              <div className="flex items-center justify-between gap-2 mb-1.5">
                <span className="text-xs font-medium text-muted-foreground">Additional Information</span>
                <Button
                  type="button"
                  size="sm"
                  variant="ghost"
                  className="h-6 text-xs"
                  onClick={() => {
                    const text = (parsedOrder?.additionalInformation ?? loadedDraftFromUrl?.additionalInformation) as string;
                    navigator.clipboard.writeText(text);
                    showSuccess('Copied to clipboard');
                  }}
                >
                  Copy
                </Button>
              </div>
              <pre className="text-xs whitespace-pre-wrap break-words bg-muted/50 rounded p-2 max-h-40 overflow-auto font-sans">
                {(parsedOrder?.additionalInformation ?? loadedDraftFromUrl?.additionalInformation) as string}
              </pre>
            </Card>
          )}

          {/* Error Banner */}
          {loadError && (
            <div className="flex items-center gap-2 rounded border border-amber-300 bg-amber-50 px-2 py-1.5 text-xs text-amber-800">
              <AlertTriangle className="h-3 w-3" />
              <span>{loadError}</span>
              <Button size="sm" variant="outline" onClick={loadSettings} className="ml-auto h-6 text-xs">
                Retry
              </Button>
            </div>
          )}

          {/* 2) STATUS | SERVICE INSTALLER | SUPPORT | TEAM/CREW | PARTNER */}
          <div className={sectionClass}>
            <div className="flex flex-wrap gap-2">
              <div className="flex-1 min-w-[120px]">
                <label className={labelClass}>Status</label>
                <select {...register('status')} className={cn(selectClass, errors.status && 'border-destructive')}>
                  {STATUS_OPTIONS.map(opt => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
              </div>
              <div className="flex-1 min-w-[140px]">
                <label className={labelClass}>Service Installer</label>
                <select {...register('serviceInstallerId')} className={selectClass}>
                  <option value="">Select SI</option>
                  {serviceInstallers.map(si => (
                    <option key={si.id} value={si.id}>{si.name}</option>
                  ))}
                </select>
              </div>
              <div className="flex-1 min-w-[140px]">
                <label className={labelClass}>Support</label>
                <select {...register('supportInstallerId')} className={selectClass}>
                  <option value="">Optional</option>
                  {serviceInstallers.map(si => (
                    <option key={si.id} value={si.id}>{si.name}</option>
                  ))}
                </select>
              </div>
              <div className="flex-1 min-w-[100px]">
                <label className={labelClass}>Team/Crew</label>
                <input type="text" {...register('teamCrew')} placeholder="POD/Crew" className={inputClass} />
              </div>
              <div className="flex-1 min-w-[140px]">
                <label className={labelClass}>Partner *</label>
                <select
                  {...register('partnerId')}
                  className={cn(selectClass, errors.partnerId && 'border-destructive')}
                  disabled={fromParser}
                  title={fromParser ? 'Partner is set by the system from parsed data (UNIFIED_ORDER_SYSTEM_SPEC §2)' : undefined}
                >
                  <option value="">Select</option>
                  {partners.map(p => (
                    <option key={p.id} value={p.id}>{p.name || p.displayName}</option>
                  ))}
                </select>
                {fromParser && <p className="text-[9px] text-muted-foreground mt-0.5">Set from parser (read-only)</p>}
                {errors.partnerId && <span className="text-[10px] text-destructive">{errors.partnerId.message}</span>}
              </div>
            </div>
          </div>

          {/* 3) ORDER IDENTIFICATION */}
          <div className={sectionClass}>
            <div className={sectionTitleClass}>
              <Package className="h-3.5 w-3.5" />
              Order Identification
            </div>
            
            {/* Row 1: Service ID / TBBN | Order Type | Order Sub-Type | Order Categories (Partner is in the Status bar above) */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <div>
                <label className={labelClass}>
                  Service ID / TBBN *
                  {watch('serviceIdType') && (
                    <span className="text-[9px] font-normal text-muted-foreground ml-1">
                      ({watch('serviceIdType') === 'Tbbn' ? 'TBBN' : 'Partner Service ID'})
                    </span>
                  )}
                </label>
                <input
                  type="text"
                  {...register('serviceId')}
                  placeholder={watch('serviceIdType') === 'Tbbn' ? 'TBBN1234567 or TBBNA12345' : watch('serviceIdType') === 'PartnerServiceId' ? 'CELCOM0016996 or DIGI0012345' : 'TBBN000000 or Partner ID'}
                  className={cn(inputClass, errors.serviceId && 'border-destructive')}
                />
                <div className="text-[9px] text-muted-foreground mt-0.5">
                  {watch('serviceIdType') === 'Tbbn'
                    ? 'Format: TBBN[A-Z]? + digits (e.g., TBBN12345, TBBNA1234)'
                    : watch('serviceIdType') === 'PartnerServiceId'
                    ? 'Partner Service ID (e.g., CELCOM0016996, DIGI0012345)'
                    : 'Enter TBBN (TIME direct) or Partner Service ID (wholesale)'}
                </div>
                {errors.serviceId && <span className="text-[10px] text-destructive">{errors.serviceId.message}</span>}
              </div>
              <div>
                <label className={labelClass}>Order Type *</label>
                <select {...register('orderTypeParentId')} className={cn(selectClass, errors.orderTypeParentId && 'border-destructive')}>
                  <option value="">Select</option>
                  {parentOrderTypes.map((t) => (
                    <option key={t.id} value={t.id}>{t.name} ({t.code})</option>
                  ))}
                </select>
                {errors.orderTypeParentId && <span className="text-[10px] text-destructive">{errors.orderTypeParentId.message}</span>}
                {draftInactiveParentWarning && (
                  <span className="text-[10px] text-amber-600 dark:text-amber-400">Draft referenced an inactive order type. Please select an active order type.</span>
                )}
              </div>
              <div>
                <label className={labelClass}>Order Sub-Type {hasSubType ? '*' : ''}</label>
                <select {...register('orderSubTypeId')} className={cn(selectClass, hasSubType && errors.orderSubTypeId && 'border-destructive')} disabled={!hasSubType}>
                  <option value="">{hasSubType ? 'Select' : '—'}</option>
                  {subtypeOrderTypes.map((s) => (
                    <option key={s.id} value={s.id}>{s.name} ({s.code})</option>
                  ))}
                </select>
                {hasSubType && errors.orderSubTypeId && (
                  <span className="text-[10px] text-destructive">{errors.orderSubTypeId.message}</span>
                )}
                {draftSubtypeUnavailable && hasSubType && (
                  <span className="text-[10px] text-amber-600 dark:text-amber-400">Draft referenced a subtype that is no longer available. Please select a subtype.</span>
                )}
              </div>
              <div>
                <div className="flex items-center justify-between mb-1">
                  <label className={labelClass}>Order Categories</label>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => navigate('/settings/gpon/order-categories')}
                    className="h-6 px-2 text-xs"
                  >
                    Manage
                  </Button>
                </div>
                <select {...register('orderCategoryId')} className={selectClass}>
                  <option value="">Select (optional)</option>
                  {orderCategories.map((oc) => (
                    <option key={oc.id} value={oc.id}>{oc.name}{oc.code ? ` (${oc.code})` : ''}</option>
                  ))}
                </select>
              </div>
            </div>

            {/* Row 2: Appointment Date and Time */}
            <div className="max-w-md">
              <div className="space-y-1 mb-1">
                <label className="text-xs font-medium leading-none">
                  APPOINTMENT
                  <span className="text-destructive ml-1">*</span>
                </label>
              </div>
              <div className="grid grid-cols-2 gap-2">
                <DatePicker
                  label=""
                  name="appointmentDate"
                  value={watch('appointmentDate')}
                  onChange={(e) => setValue('appointmentDate', e.target.value)}
                  error={errors.appointmentDate?.message}
                  required
                  placeholder="dd/mm/yyyy"
                />
                <TimePicker
                  label=""
                  name="appointmentTime"
                  value={watch('appointmentTime')}
                  onChange={(e) => setValue('appointmentTime', e.target.value)}
                  error={errors.appointmentTime?.message}
                  required
                  timeIncrement={30}
                  placeholder="Time"
                />
              </div>
              {(errors.appointmentDate || errors.appointmentTime) && (
                <div className="text-xs text-destructive mt-1">
                  {errors.appointmentDate?.message || errors.appointmentTime?.message}
                </div>
              )}
            </div>

            {/* Row 3: Assurance Fields (Conditional) */}
            {isAssurance && (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1 border-t border-border/50">
                <div>
                  <label className={labelClass}>Ticket Number (TKT #) *</label>
                  <input
                    type="text"
                    {...register('ticketNumber')}
                    placeholder="TTKT..."
                    className={cn(inputClass, errors.ticketNumber && 'border-destructive')}
                  />
                  {errors.ticketNumber && <span className="text-[10px] text-destructive">{errors.ticketNumber.message}</span>}
                </div>
                <div>
                  <label className={labelClass}>AWO Number *</label>
                  <input
                    type="text"
                    {...register('awoNumber')}
                    placeholder="AWO..."
                    className={cn(inputClass, errors.awoNumber && 'border-destructive')}
                  />
                  {errors.awoNumber && <span className="text-[10px] text-destructive">{errors.awoNumber.message}</span>}
                </div>
              </div>
            )}

            {/* Row 4: Issue and Solution Fields (Assurance only) */}
            {isAssurance && (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1 border-t border-border/50">
                <div>
                  <label className={labelClass}>Issue</label>
                  <input
                    type="text"
                    {...register('issue')}
                    placeholder="e.g., Link Down, LOSi, LOBi"
                    className={cn(inputClass, errors.issue && 'border-destructive')}
                  />
                  {errors.issue && <span className="text-[10px] text-destructive">{errors.issue.message}</span>}
                </div>
                <div>
                  <label className={labelClass}>Solution</label>
                  <textarea
                    {...register('solution')}
                    placeholder="Enter solution/resolution after meeting customer"
                    rows={2}
                    className={cn(inputClass, errors.solution && 'border-destructive')}
                  />
                  {errors.solution && <span className="text-[10px] text-destructive">{errors.solution.message}</span>}
                </div>
              </div>
            )}
          </div>

          {/* 4) CUSTOMER & BUILDING */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-2">
            {/* Customer Info */}
            <div className={sectionClass}>
              <div className={sectionTitleClass}>
                <User className="h-3.5 w-3.5" />
                Customer Information
              </div>
              <div className="space-y-1.5">
                <div>
                  <label className={labelClass}>Name *</label>
                  <input
                    type="text"
                    {...register('customerName')}
                    className={cn(inputClass, errors.customerName && 'border-destructive')}
                  />
                </div>
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <label className={labelClass}>Contact No 1 *</label>
                    <input
                      type="tel"
                      {...register('contactNo1')}
                      placeholder="0123456789"
                      className={cn(inputClass, errors.contactNo1 && 'border-destructive')}
                    />
                  </div>
                  <div>
                    <label className={labelClass}>Contact No 2</label>
                    <input
                      type="tel"
                      {...register('contactNo2')}
                      placeholder="Optional"
                      className={inputClass}
                    />
                  </div>
                </div>
                <div>
                  <label className={labelClass}>Address *</label>
                  <textarea
                    {...register('address')}
                    rows={2}
                    className={cn(inputClass, 'h-auto py-1.5 resize-none', errors.address && 'border-destructive')}
                  />
                </div>
                <div className="grid grid-cols-3 gap-2">
                  <div>
                    <label className={labelClass}>Block</label>
                    <input type="text" {...register('block')} className={inputClass} />
                  </div>
                  <div>
                    <label className={labelClass}>Level</label>
                    <input type="text" {...register('level')} className={inputClass} />
                  </div>
                  <div>
                    <label className={labelClass}>Unit</label>
                    <input type="text" {...register('unit')} className={inputClass} />
                  </div>
                </div>
              </div>
            </div>

            {/* Building Details */}
            <div className={sectionClass}>
              <div className={sectionTitleClass}>
                <Building2 className="h-3.5 w-3.5" />
                Building Details
              </div>
              <div className="space-y-1.5">
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <label className={labelClass}>Building *</label>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => setShowBuildingModal(true)}
                      className="h-6 px-2 text-xs"
                    >
                      <Plus className="h-3 w-3 mr-1" />
                      Add New
                    </Button>
                  </div>
                  <select
                    {...register('buildingId')}
                    className={cn(selectClass, errors.buildingId && 'border-destructive')}
                  >
                    <option value="">Select Building</option>
                    {buildings.map(b => (
                      <option key={b.id} value={b.id}>{b.name}</option>
                    ))}
                  </select>
                </div>
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <label className={labelClass}>Building Type</label>
                    <input
                      type="text"
                      {...register('buildingType')}
                      className={cn(inputClass, 'bg-muted')}
                      readOnly
                    />
                  </div>
                  <div>
                    <label className={labelClass}>Installation Method</label>
                    <input
                      type="text"
                      {...register('installationMethod')}
                      className={cn(inputClass, 'bg-muted')}
                      readOnly
                    />
                  </div>
                </div>
                {/* Building Summary */}
                {selectedBuilding && (
                  <div className="rounded border border-border/50 bg-muted/30 p-2 text-xs space-y-0.5">
                    <div className="font-medium text-foreground">Building Summary</div>
                    <div className="text-muted-foreground">
                      <div><span className="font-medium">Name:</span> {selectedBuilding.name}</div>
                      <div><span className="font-medium">Type:</span> {selectedBuilding.buildingType || selectedBuilding.propertyType || '–'}</div>
                      <div><span className="font-medium">Address:</span> {selectedBuilding.fullAddress || selectedBuilding.addressLine1 || '–'}</div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* 5) MODIFICATION DETAILS (Conditional) */}
          {isModification && (
            <div className={sectionClass}>
              <div className={sectionTitleClass}>
                <Wrench className="h-3.5 w-3.5" />
                Modification Details
                <span className="text-[10px] font-normal text-muted-foreground ml-1">
                  ({isOutdoor ? 'Outdoor' : 'Indoor'})
                </span>
              </div>
              {isOutdoor && (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  <div>
                    <label className={labelClass}>Old Address *</label>
                    <textarea
                      {...register('oldAddress')}
                      rows={2}
                      className={cn(inputClass, 'h-auto py-1.5 resize-none', errors.oldAddress && 'border-destructive')}
                    />
                    {errors.oldAddress && <span className="text-[10px] text-destructive">{errors.oldAddress.message}</span>}
                  </div>
                  <div>
                    <label className={labelClass}>New Address *</label>
                    <textarea
                      {...register('newAddress')}
                      rows={2}
                      className={cn(inputClass, 'h-auto py-1.5 resize-none', errors.newAddress && 'border-destructive')}
                    />
                    {errors.newAddress && <span className="text-[10px] text-destructive">{errors.newAddress.message}</span>}
                  </div>
                </div>
              )}
              {isIndoor && (
                <div>
                  <label className={labelClass}>Indoor Relocation Remark *</label>
                  <textarea
                    {...register('indoorRemark')}
                    rows={2}
                    placeholder="e.g., Move ONU from bedroom to living room"
                    className={cn(inputClass, 'h-auto py-1.5 resize-none', errors.indoorRemark && 'border-destructive')}
                  />
                  {errors.indoorRemark && <span className="text-[10px] text-destructive">{errors.indoorRemark.message}</span>}
                </div>
              )}
            </div>
          )}

          {/* 6) SPLITTER & MATERIALS */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-2">
            {/* Splitter Details */}
            <div className={sectionClass}>
              <div className={sectionTitleClass}>
                Splitter Details
                {splitterFieldsLocked && (
                  <span className="text-[10px] font-normal text-amber-600 ml-1">(Unlocks at Met Customer)</span>
                )}
              </div>
              <div className="grid grid-cols-3 gap-2">
                <div>
                  <label className={labelClass}>Splitter #</label>
                  <input
                    type="text"
                    {...register('splitterNumber')}
                    disabled={splitterFieldsLocked}
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>Location</label>
                  <input
                    type="text"
                    {...register('splitterLocation')}
                    disabled={splitterFieldsLocked}
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>Port #</label>
                  <input
                    type="text"
                    {...register('splitterPort')}
                    disabled={splitterFieldsLocked}
                    className={inputClass}
                  />
                </div>
              </div>
            </div>

            {/* Customer Premise Materials */}
            <div className={sectionClass}>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className={sectionTitleClass}>
                    <Package className="h-3.5 w-3.5" />
                    Customer Premise Materials
                  </div>
                  {defaultMaterialsLoaded && (
                    <span className="text-[10px] px-1.5 py-0.5 bg-blue-500/20 text-blue-600 rounded border border-blue-500/30">
                      Defaults Applied
                    </span>
                  )}
                  {loadingDefaultMaterials && (
                    <span className="text-[10px] text-muted-foreground">Loading...</span>
                  )}
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => appendMaterial({
                    id: `mat-${Date.now()}`,
                    materialId: '',
                    materialType: '',
                    serialNumber: '',
                    isDefault: false,
                    isSerialized: true,
                  })}
                  className="h-6 px-2 text-xs"
                >
                  <Plus className="h-3 w-3 mr-1" />
                  Add
                </Button>
              </div>
              
              <div className="space-y-1">
                {/* Header */}
                <div className="grid grid-cols-12 gap-1 text-[10px] font-medium text-muted-foreground uppercase">
                  <div className="col-span-5">Material</div>
                  <div className="col-span-5">Serial #</div>
                  <div className="col-span-2"></div>
                </div>
                
                {materialFields.length === 0 && (
                  <div className="text-xs text-muted-foreground text-center py-2 border border-dashed rounded">
                    No materials. Click "+ Add" to add materials.
                  </div>
                )}
                
                {materialFields.map((field, index) => (
                  <div key={field.id} className="grid grid-cols-12 gap-1 items-center">
                    <div className={cn("col-span-5", field.isDefault && "relative")}>
                      <select
                        {...register(`materials.${index}.materialType`)}
                        className={cn(selectClass, 'text-xs', field.isDefault && 'bg-blue-50 dark:bg-blue-950/20 border-blue-300 dark:border-blue-700 pr-8')}
                        disabled={field.isDefault}
                        title={field.isDefault ? 'Default material from building' : ''}
                      >
                        <option value="">Select</option>
                        {materialOptions.map(opt => (
                          <option key={opt.value} value={opt.label}>{opt.label}</option>
                        ))}
                      </select>
                      {field.isDefault && (
                        <span className="absolute right-1 top-1/2 -translate-y-1/2 text-[8px] px-1 py-0.5 bg-blue-500 text-white rounded pointer-events-none">
                          Default
                        </span>
                      )}
                    </div>
                    <div className="col-span-5">
                      <input
                        type="text"
                        {...register(`materials.${index}.serialNumber`)}
                        placeholder={field.isSerialized ? 'Serial #' : 'N/A'}
                        disabled={splitterFieldsLocked || !field.isSerialized}
                        className={cn(inputClass, 'text-xs')}
                      />
                    </div>
                    <div className="col-span-2 flex justify-end">
                      {!field.isDefault && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => removeMaterial(index)}
                          className="h-6 w-6 p-0 text-destructive hover:text-destructive"
                        >
                          <Trash2 className="h-3 w-3" />
                        </Button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* 7) NETWORK INFO */}
          <div className={sectionClass}>
            <div className={sectionTitleClass}>
              Network Info
            </div>
            <div className="space-y-1.5">
              <div>
                <label className={labelClass}>Package</label>
                <textarea
                  {...register('networkPackage')}
                  rows={2}
                  placeholder="e.g., TIME Fibre 600Mbps Home Broadband"
                  className={cn(inputClass, 'h-auto py-1.5 resize-none')}
                />
              </div>
              <div className="grid grid-cols-3 gap-2">
                <div>
                  <label className={labelClass}>Bandwidth</label>
                  <input
                    type="text"
                    {...register('networkBandwidth')}
                    placeholder="e.g., 600 Mbps"
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>Login ID</label>
                  <input
                    type="text"
                    {...register('networkLoginId')}
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>Password</label>
                  <input
                    type="password"
                    {...register('networkPassword')}
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>ONU Password</label>
                  <input
                    type="password"
                    {...register('onuPassword')}
                    className={inputClass}
                    placeholder="ONU device password"
                  />
                </div>
              </div>
              <div className="grid grid-cols-4 gap-2">
                <div>
                  <label className={labelClass}>WAN IP</label>
                  <input
                    type="text"
                    {...register('networkWanIp')}
                    placeholder="e.g., 1.2.3.4"
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>LAN IP</label>
                  <input
                    type="text"
                    {...register('networkLanIp')}
                    placeholder="e.g., 192.168.1.1"
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>Gateway</label>
                  <input
                    type="text"
                    {...register('networkGateway')}
                    placeholder="e.g., 192.168.1.1"
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>Subnet Mask</label>
                  <input
                    type="text"
                    {...register('networkSubnetMask')}
                    placeholder="e.g., 255.255.255.0"
                    className={inputClass}
                  />
                </div>
              </div>
            </div>
          </div>

          {/* 8) VOIP */}
          <div className={sectionClass}>
            <div className={sectionTitleClass}>
              VOIP
            </div>
            <div className="space-y-1.5">
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className={labelClass}>Service ID / Password</label>
                  <input
                    type="text"
                    {...register('voipServiceId')}
                    placeholder="e.g., 0327098036/abcd1234"
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>Password</label>
                  <input
                    type="password"
                    {...register('voipPassword')}
                    className={inputClass}
                  />
                </div>
              </div>
              <div className="grid grid-cols-4 gap-2">
                <div>
                  <label className={labelClass}>IP Address ONU</label>
                  <input
                    type="text"
                    {...register('voipIpAddressOnu')}
                    placeholder="e.g., 192.168.1.100"
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>Gateway ONU</label>
                  <input
                    type="text"
                    {...register('voipGatewayOnu')}
                    placeholder="e.g., 192.168.1.1"
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>Subnet Mask ONU</label>
                  <input
                    type="text"
                    {...register('voipSubnetMaskOnu')}
                    placeholder="e.g., 255.255.255.0"
                    className={inputClass}
                  />
                </div>
                <div>
                  <label className={labelClass}>IP Address SRP</label>
                  <input
                    type="text"
                    {...register('voipIpAddressSrp')}
                    placeholder="e.g., 10.0.0.1"
                    className={inputClass}
                  />
                </div>
              </div>
              <div>
                <label className={labelClass}>Remarks</label>
                <textarea
                  {...register('voipRemarks')}
                  rows={2}
                  className={cn(inputClass, 'h-auto py-1.5 resize-none')}
                />
              </div>
            </div>
          </div>

          {/* 9) ASSURANCE MATERIALS (Conditional) */}
          {isAssurance && (
            <div className="space-y-2">
              {/* 7A) RMA - Serialised Replacements */}
              <div className={sectionClass}>
                <div className="flex items-center justify-between">
                  <div className={sectionTitleClass}>
                    RMA - Serialised Replacements
                    <span className="text-[10px] font-normal text-muted-foreground ml-1">(TIME Approval Required)</span>
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => appendRma({
                      id: `rma-${Date.now()}`,
                      oldMaterialId: '',
                      oldMaterialType: '',
                      oldSerialNumber: '',
                      newMaterialId: '',
                      newMaterialType: '',
                      newSerialNumber: '',
                      approvedBy: '',
                      approvalNotes: '',
                    })}
                    disabled={splitterFieldsLocked}
                    className="h-6 px-2 text-xs"
                  >
                    <Plus className="h-3 w-3 mr-1" />
                    Add RMA
                  </Button>
                </div>
                
                {rmaFields.length === 0 ? (
                  <div className="text-xs text-muted-foreground text-center py-2 border border-dashed rounded">
                    No RMA entries. Add if serialised materials were replaced.
                  </div>
                ) : (
                  <div className="space-y-2">
                    {rmaFields.map((field, index) => (
                      <div key={field.id} className="grid grid-cols-12 gap-1 p-2 bg-muted/30 rounded border border-border/50">
                        <div className="col-span-12 sm:col-span-6 grid grid-cols-2 gap-1">
                          <div>
                            <label className={labelClass}>Old Material</label>
                            <select
                              {...register(`rmaRows.${index}.oldMaterialType`)}
                              disabled={splitterFieldsLocked}
                              className={cn(selectClass, 'text-xs')}
                            >
                              <option value="">Select</option>
                              {materialOptions.filter(o => o.isSerialized).map(opt => (
                                <option key={opt.value} value={opt.label}>{opt.label}</option>
                              ))}
                            </select>
                          </div>
                          <div>
                            <label className={labelClass}>Old Serial #</label>
                            <input
                              type="text"
                              {...register(`rmaRows.${index}.oldSerialNumber`)}
                              disabled={splitterFieldsLocked}
                              className={cn(inputClass, 'text-xs')}
                            />
                          </div>
                        </div>
                        <div className="col-span-12 sm:col-span-6 grid grid-cols-2 gap-1">
                          <div>
                            <label className={labelClass}>New Material</label>
                            <select
                              {...register(`rmaRows.${index}.newMaterialType`)}
                              disabled={splitterFieldsLocked}
                              className={cn(selectClass, 'text-xs')}
                            >
                              <option value="">Select</option>
                              {materialOptions.filter(o => o.isSerialized).map(opt => (
                                <option key={opt.value} value={opt.label}>{opt.label}</option>
                              ))}
                            </select>
                          </div>
                          <div>
                            <label className={labelClass}>New Serial #</label>
                            <input
                              type="text"
                              {...register(`rmaRows.${index}.newSerialNumber`)}
                              disabled={splitterFieldsLocked}
                              className={cn(inputClass, 'text-xs')}
                            />
                          </div>
                        </div>
                        <div className="col-span-10 sm:col-span-5 grid grid-cols-2 gap-1">
                          <div>
                            <label className={labelClass}>Approved By (TIME)</label>
                            <input
                              type="text"
                              {...register(`rmaRows.${index}.approvedBy`)}
                              disabled={splitterFieldsLocked}
                              className={cn(inputClass, 'text-xs')}
                            />
                          </div>
                          <div>
                            <label className={labelClass}>Approval Notes</label>
                            <input
                              type="text"
                              {...register(`rmaRows.${index}.approvalNotes`)}
                              disabled={splitterFieldsLocked}
                              className={cn(inputClass, 'text-xs')}
                            />
                          </div>
                        </div>
                        <div className="col-span-2 sm:col-span-1 flex items-end justify-end">
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => removeRma(index)}
                            className="h-6 w-6 p-0 text-destructive hover:text-destructive"
                          >
                            <Trash2 className="h-3 w-3" />
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* 7B) Non-Serialised Replacements */}
              <div className={sectionClass}>
                <div className="flex items-center justify-between">
                  <div className={sectionTitleClass}>
                    Non-Serialised Replacements
                    <span className="text-[10px] font-normal text-muted-foreground ml-1">(No Approval Required)</span>
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => appendNonSerial({
                      id: `nonserial-${Date.now()}`,
                      materialId: '',
                      materialType: '',
                      quantityReplaced: 1,
                      remark: '',
                    })}
                    className="h-6 px-2 text-xs"
                  >
                    <Plus className="h-3 w-3 mr-1" />
                    Add
                  </Button>
                </div>
                
                {nonSerialFields.length === 0 ? (
                  <div className="text-xs text-muted-foreground text-center py-2 border border-dashed rounded">
                    No non-serialised replacements. Add if patch cords, connectors, etc. were replaced.
                  </div>
                ) : (
                  <div className="space-y-1">
                    <div className="grid grid-cols-12 gap-1 text-[10px] font-medium text-muted-foreground uppercase">
                      <div className="col-span-5">Material</div>
                      <div className="col-span-2">Qty</div>
                      <div className="col-span-4">Remark</div>
                      <div className="col-span-1"></div>
                    </div>
                    {nonSerialFields.map((field, index) => (
                      <div key={field.id} className="grid grid-cols-12 gap-1 items-center">
                        <div className="col-span-5">
                          <select
                            {...register(`nonSerialReplacements.${index}.materialType`)}
                            className={cn(selectClass, 'text-xs')}
                          >
                            <option value="">Select</option>
                            {materialOptions.filter(o => !o.isSerialized).map(opt => (
                              <option key={opt.value} value={opt.label}>{opt.label}</option>
                            ))}
                          </select>
                        </div>
                        <div className="col-span-2">
                          <input
                            type="number"
                            min="1"
                            {...register(`nonSerialReplacements.${index}.quantityReplaced`, { valueAsNumber: true })}
                            className={cn(inputClass, 'text-xs')}
                          />
                        </div>
                        <div className="col-span-4">
                          <input
                            type="text"
                            {...register(`nonSerialReplacements.${index}.remark`)}
                            placeholder="Optional"
                            className={cn(inputClass, 'text-xs')}
                          />
                        </div>
                        <div className="col-span-1 flex justify-end">
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => removeNonSerial(index)}
                            className="h-6 w-6 p-0 text-destructive hover:text-destructive"
                          >
                            <Trash2 className="h-3 w-3" />
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}

        </form>
      </div>

      {/* Quick Building Modal */}
      <QuickBuildingModal
        isOpen={showBuildingModal}
        onClose={() => {
          setShowBuildingModal(false);
          setBuildingModalFromApproval(null);
        }}
        initialData={
          buildingModalFromApproval?.buildingDetection
            ? {
                buildingName: buildingModalFromApproval.buildingDetection.detectedBuildingName,
                addressLine1: buildingModalFromApproval.buildingDetection.detectedAddress,
                city: buildingModalFromApproval.buildingDetection.detectedCity,
                state: buildingModalFromApproval.buildingDetection.detectedState,
                postcode: buildingModalFromApproval.buildingDetection.detectedPostcode
              }
            : parseAddressForBuilding(watch('address'))
        }
        onBuildingCreated={handleBuildingCreated}
        onBuildingSelected={handleBuildingSelected}
        mode={buildingModalFromApproval ? 'create-and-approve' : 'create-only'}
        draftId={buildingModalFromApproval ? buildingModalFromApproval.draftId : undefined}
      />
    </PageShell>
  );
};

export default CreateOrderPage;
