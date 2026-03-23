import React, { useState, useEffect } from 'react';
import { 
  Plus, Edit, Trash2, Save, Power, DollarSign, RefreshCcw, 
  Lightbulb, ChevronDown, ChevronUp, Filter, TrendingUp, TrendingDown,
  Users, Building2, Layers, Calculator, Search, X, LayoutGrid, List
} from 'lucide-react';
import { 
  getGponPartnerJobRates, createGponPartnerJobRate, updateGponPartnerJobRate, deleteGponPartnerJobRate,
  getGponSiJobRates, createGponSiJobRate, updateGponSiJobRate, deleteGponSiJobRate,
  getGponSiCustomRates, createGponSiCustomRate, updateGponSiCustomRate, deleteGponSiCustomRate,
  resolveRates
} from '../../api/rates';
import { getPartnerGroups } from '../../api/partnerGroups';
import { getPartners } from '../../api/partners';
import { getOrderTypes } from '../../api/orderTypes';
import { getOrderCategories } from '../../api/orderCategories';
import { getInstallationMethods } from '../../api/installationMethods';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { PageShell } from '../../components/layout';
import { 
  LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, 
  Modal, DataTable, Tabs, TabPanel, Select, StatusBadge, ConfirmDialog 
} from '../../components/ui';
import { GroupedRateTable } from '../../components/rates/GroupedRateTable';
import { cn } from '@/lib/utils';
import type { GponPartnerJobRate, GponSiJobRate, GponSiCustomRate, RateResolutionResult } from '../../types/rates';
import type { PartnerGroup } from '../../types/partnerGroups';
import type { Partner } from '../../types/partners';
import type { OrderType } from '../../types/orderTypes';
import type { InstallationMethod } from '../../types/installationMethods';

interface OrderCategory {
  id: string;
  name: string;
  code?: string;
  isActive?: boolean;
}

interface ServiceInstaller {
  id: string;
  name: string;
  siLevel?: string;
  isActive?: boolean;
}

const SI_LEVELS = ['Junior', 'Senior', 'Subcon'];

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const RateEngineManagementPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  
  // Reference data
  const [partnerGroups, setPartnerGroups] = useState<PartnerGroup[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [orderTypes, setOrderTypes] = useState<OrderType[]>([]);
  const [orderCategories, setOrderCategories] = useState<OrderCategory[]>([]);
  const [installationMethods, setInstallationMethods] = useState<InstallationMethod[]>([]);
  const [serviceInstallers, setServiceInstallers] = useState<ServiceInstaller[]>([]);
  
  // Rate data
  const [partnerRates, setPartnerRates] = useState<GponPartnerJobRate[]>([]);
  const [siRates, setSiRates] = useState<GponSiJobRate[]>([]);
  const [customRates, setCustomRates] = useState<GponSiCustomRate[]>([]);
  
  // UI state
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState(0);
  const [showGuide, setShowGuide] = useState(true);
  const [showFilters, setShowFilters] = useState(false);
  
  // View mode: 'flat' or 'grouped'
  const [viewMode, setViewMode] = useState<'flat' | 'grouped'>(() => {
    const saved = localStorage.getItem('rateEngineViewMode');
    return (saved === 'flat' || saved === 'grouped') ? saved : 'grouped';
  });
  
  // Search query
  const [searchQuery, setSearchQuery] = useState('');
  
  // Selected rates for bulk operations
  const [selectedRates, setSelectedRates] = useState<Set<string>>(new Set());
  
  // Modal state
  const [showPartnerRateModal, setShowPartnerRateModal] = useState(false);
  const [showSiRateModal, setShowSiRateModal] = useState(false);
  const [showCustomRateModal, setShowCustomRateModal] = useState(false);
  const [showRateCalculator, setShowRateCalculator] = useState(false);
  
  // Edit state
  const [editingPartnerRate, setEditingPartnerRate] = useState<GponPartnerJobRate | null>(null);
  const [editingSiRate, setEditingSiRate] = useState<GponSiJobRate | null>(null);
  const [editingCustomRate, setEditingCustomRate] = useState<GponSiCustomRate | null>(null);
  
  // Delete confirmation
  const [deletingPartnerRate, setDeletingPartnerRate] = useState<GponPartnerJobRate | null>(null);
  const [deletingSiRate, setDeletingSiRate] = useState<GponSiJobRate | null>(null);
  const [deletingCustomRate, setDeletingCustomRate] = useState<GponSiCustomRate | null>(null);
  
  // Filters
  const [filters, setFilters] = useState({
    partnerGroupId: '',
    orderTypeId: '',
    orderCategoryId: '',
    installationMethodId: '',
    siLevel: '',
    isActive: 'true'
  });
  
  // Form data
  const [partnerRateForm, setPartnerRateForm] = useState({
    partnerGroupId: '',
    partnerId: '',
    orderTypeId: '',
    orderCategoryIds: [] as string[],
    installationMethodIds: [] as string[],
    revenueAmount: '',
    validFrom: '',
    validTo: '',
    notes: '',
    isActive: true
  });
  
  const [siRateForm, setSiRateForm] = useState({
    orderTypeIds: [] as string[],
    orderCategoryIds: [] as string[],
    installationMethodIds: [] as string[],
    siLevels: [] as string[],
    partnerGroupId: '',
    payoutAmount: '',
    validFrom: '',
    validTo: '',
    notes: '',
    isActive: true
  });
  
  const [customRateForm, setCustomRateForm] = useState({
    serviceInstallerId: '',
    orderTypeIds: [] as string[],
    orderCategoryIds: [] as string[],
    installationMethodIds: [] as string[],
    customPayoutAmount: '',
    reason: '',
    validFrom: '',
    validTo: '',
    notes: '',
    isActive: true
  });
  
  // Rate calculator state
  const [calcRequest, setCalcRequest] = useState({
    partnerGroupId: '',
    orderTypeId: '',
    orderCategoryId: '',
    installationMethodId: '',
    siLevel: 'Junior'
  });
  const [calcResult, setCalcResult] = useState<RateResolutionResult | null>(null);
  
  // Save view mode to localStorage when it changes
  useEffect(() => {
    localStorage.setItem('rateEngineViewMode', viewMode);
  }, [viewMode]);
  
  useEffect(() => {
    loadAllData();
  }, [filters]);
  
  const loadAllData = async () => {
    try {
      setLoading(true);
      
      const apiFilters = {
        isActive: filters.isActive === '' ? undefined : filters.isActive === 'true',
        partnerGroupId: filters.partnerGroupId || undefined,
        orderTypeId: filters.orderTypeId || undefined
      };
      
      const [
        partnerGroupsRes,
        partnersRes,
        orderTypesRes,
        installationTypesRes,
        installationMethodsRes,
        serviceInstallersRes,
        partnerRatesRes,
        siRatesRes,
        customRatesRes
      ] = await Promise.all([
        getPartnerGroups().catch(() => []),
        getPartners({ isActive: true }).catch(() => []),
        getOrderTypes({ isActive: true }).catch(() => []),
        getOrderCategories({ isActive: true }).catch(() => []),
        getInstallationMethods({ isActive: true }).catch(() => []),
        getServiceInstallers({ isActive: true }).catch(() => []),
        getGponPartnerJobRates(apiFilters).catch(() => []),
        getGponSiJobRates({ ...apiFilters, siLevel: filters.siLevel || undefined }).catch(() => []),
        getGponSiCustomRates(apiFilters).catch(() => [])
      ]);
      
      setPartnerGroups(Array.isArray(partnerGroupsRes) ? partnerGroupsRes : []);
      setPartners(Array.isArray(partnersRes) ? partnersRes : []);
      setOrderTypes(Array.isArray(orderTypesRes) ? orderTypesRes : []);
      setOrderCategories(Array.isArray(installationTypesRes) ? installationTypesRes : []);
      setInstallationMethods(Array.isArray(installationMethodsRes) ? installationMethodsRes : []);
      setServiceInstallers(Array.isArray(serviceInstallersRes) ? serviceInstallersRes : []);
      setPartnerRates(Array.isArray(partnerRatesRes) ? partnerRatesRes : []);
      setSiRates(Array.isArray(siRatesRes) ? siRatesRes : []);
      setCustomRates(Array.isArray(customRatesRes) ? customRatesRes : []);
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to load rate data');
    } finally {
      setLoading(false);
    }
  };
  
  // Partner Rate CRUD
  const handleCreatePartnerRate = async () => {
    // Validation
    if (!partnerRateForm.partnerGroupId || !partnerRateForm.orderTypeId) {
      showError('Partner Group and Order Type are required');
      return;
    }
    
    if (partnerRateForm.orderCategoryIds.length === 0) {
      showError('At least one Order Category must be selected');
      return;
    }
    
    if (partnerRateForm.installationMethodIds.length === 0) {
      showError('At least one Installation Method must be selected');
      return;
    }
    
    // Generate all combinations
    const combinations: Array<{
      orderCategoryId: string;
      installationMethodId: string | undefined;
    }> = [];
    
    // Filter out empty string from installationMethodIds (if "All Methods" is selected)
    const methodIds = partnerRateForm.installationMethodIds.filter(id => id !== '');
    const hasAllMethods = partnerRateForm.installationMethodIds.includes('');
    
    for (const orderCategoryId of partnerRateForm.orderCategoryIds) {
      if (hasAllMethods && methodIds.length === 0) {
        // Only "All Methods" selected - create one record with null method
        combinations.push({ 
          orderCategoryId, 
          installationMethodId: undefined 
        });
      } else {
        // Specific methods selected (or mix of "All Methods" + specific methods)
        // If "All Methods" is also selected, we still create records for specific methods
        // (The rate resolution logic will match null methods as fallback)
        for (const installationMethodId of methodIds) {
          combinations.push({ 
            orderCategoryId, 
            installationMethodId: installationMethodId || undefined 
          });
        }
      }
    }
    
    // Create all rate records
    try {
      const promises = combinations.map(combo =>
        createGponPartnerJobRate({
          partnerGroupId: partnerRateForm.partnerGroupId,
          partnerId: partnerRateForm.partnerId || undefined,
          orderTypeId: partnerRateForm.orderTypeId,
          orderCategoryId: combo.orderCategoryId,
          installationMethodId: combo.installationMethodId,
          revenueAmount: parseFloat(partnerRateForm.revenueAmount) || 0,
          validFrom: partnerRateForm.validFrom || undefined,
          validTo: partnerRateForm.validTo || undefined,
          notes: partnerRateForm.notes || undefined,
          isActive: partnerRateForm.isActive
        })
      );
      
      await Promise.all(promises);
      showSuccess(`Created ${combinations.length} partner revenue rate(s) successfully`);
      setShowPartnerRateModal(false);
      resetPartnerRateForm();
      loadAllData();
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to create partner rates');
    }
  };
  
  const handleUpdatePartnerRate = async () => {
    if (!editingPartnerRate) return;
    
    // Validation
    if (!partnerRateForm.partnerGroupId || !partnerRateForm.orderTypeId) {
      showError('Partner Group and Order Type are required');
      return;
    }
    
    if (partnerRateForm.orderCategoryIds.length === 0) {
      showError('At least one Order Category must be selected');
      return;
    }
    
    if (partnerRateForm.installationMethodIds.length === 0) {
      showError('At least one Installation Method must be selected');
      return;
    }
    
    // Generate all combinations (same logic as create)
    const combinations: Array<{
      orderCategoryId: string;
      installationMethodId: string | undefined;
    }> = [];
    
    // Filter out empty string from installationMethodIds (if "All Methods" is selected)
    const methodIds = partnerRateForm.installationMethodIds.filter(id => id !== '');
    const hasAllMethods = partnerRateForm.installationMethodIds.includes('');
    
    for (const orderCategoryId of partnerRateForm.orderCategoryIds) {
      if (hasAllMethods && methodIds.length === 0) {
        // Only "All Methods" selected - create one record with null method
        combinations.push({ 
          orderCategoryId, 
          installationMethodId: undefined 
        });
      } else {
        // Specific methods selected (or mix of "All Methods" + specific methods)
        for (const installationMethodId of methodIds) {
          combinations.push({ 
            orderCategoryId, 
            installationMethodId: installationMethodId || undefined 
          });
        }
      }
    }
    
    try {
      // Delete the original record
      await deleteGponPartnerJobRate(editingPartnerRate.id);
      
      // Create new records for all combinations
      const promises = combinations.map(combo =>
        createGponPartnerJobRate({
          partnerGroupId: partnerRateForm.partnerGroupId,
          partnerId: partnerRateForm.partnerId || undefined,
          orderTypeId: partnerRateForm.orderTypeId,
          orderCategoryId: combo.orderCategoryId,
          installationMethodId: combo.installationMethodId,
          revenueAmount: parseFloat(partnerRateForm.revenueAmount) || 0,
          validFrom: partnerRateForm.validFrom || undefined,
          validTo: partnerRateForm.validTo || undefined,
          notes: partnerRateForm.notes || undefined,
          isActive: partnerRateForm.isActive
        })
      );
      
      await Promise.all(promises);
      showSuccess(`Updated: Deleted 1 record and created ${combinations.length} new rate record(s)`);
      setShowPartnerRateModal(false);
      setEditingPartnerRate(null);
      resetPartnerRateForm();
      loadAllData();
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to update partner rates');
    }
  };
  
  const handleDeletePartnerRate = async () => {
    if (!deletingPartnerRate) return;
    try {
      await deleteGponPartnerJobRate(deletingPartnerRate.id);
      showSuccess('Partner revenue rate deleted');
      setDeletingPartnerRate(null);
      loadAllData();
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to delete partner rate');
    }
  };
  
  // SI Rate CRUD
  const handleCreateSiRate = async () => {
    // Validation
    if (siRateForm.siLevels.length === 0) {
      showError('At least one SI Level must be selected');
      return;
    }
    
    if (siRateForm.orderTypeIds.length === 0) {
      showError('At least one Order Type must be selected');
      return;
    }
    
    if (siRateForm.orderCategoryIds.length === 0) {
      showError('At least one Order Category must be selected');
      return;
    }
    
    if (siRateForm.installationMethodIds.length === 0) {
      showError('At least one Installation Method must be selected');
      return;
    }
    
    // Generate all combinations: OrderTypes × OrderCategories × InstallationMethods × SiLevels
    const combinations: Array<{
      orderTypeId: string;
      orderCategoryId: string;
      installationMethodId: string | undefined;
      siLevel: string;
    }> = [];
    
    // Filter out empty string from installationMethodIds (if "All Methods" is selected)
    const methodIds = siRateForm.installationMethodIds.filter(id => id !== '');
    const hasAllMethods = siRateForm.installationMethodIds.includes('');
    
    for (const orderTypeId of siRateForm.orderTypeIds) {
      for (const orderCategoryId of siRateForm.orderCategoryIds) {
        for (const siLevel of siRateForm.siLevels) {
          if (hasAllMethods && methodIds.length === 0) {
            // Only "All Methods" selected - create one record with null method
            combinations.push({ 
              orderTypeId, 
              orderCategoryId, 
              installationMethodId: undefined,
              siLevel
            });
          } else {
            // Specific methods selected (or mix of "All Methods" + specific methods)
            for (const installationMethodId of methodIds) {
              combinations.push({ 
                orderTypeId, 
                orderCategoryId, 
                installationMethodId: installationMethodId || undefined,
                siLevel
              });
            }
          }
        }
      }
    }
    
    // Create all rate records
    try {
      const promises = combinations.map(combo =>
        createGponSiJobRate({
          orderTypeId: combo.orderTypeId,
          orderCategoryId: combo.orderCategoryId,
          installationMethodId: combo.installationMethodId,
          siLevel: combo.siLevel,
          partnerGroupId: siRateForm.partnerGroupId || undefined,
          payoutAmount: parseFloat(siRateForm.payoutAmount) || 0,
          validFrom: siRateForm.validFrom || undefined,
          validTo: siRateForm.validTo || undefined,
          notes: siRateForm.notes || undefined,
          isActive: siRateForm.isActive
        })
      );
      
      await Promise.all(promises);
      showSuccess(`Created ${combinations.length} SI payout rate(s) successfully`);
      setShowSiRateModal(false);
      resetSiRateForm();
      loadAllData();
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to create SI rates');
    }
  };
  
  const handleUpdateSiRate = async () => {
    if (!editingSiRate) return;
    
    // Validation
    if (siRateForm.siLevels.length === 0) {
      showError('At least one SI Level must be selected');
      return;
    }
    
    if (siRateForm.orderTypeIds.length === 0) {
      showError('At least one Order Type must be selected');
      return;
    }
    
    if (siRateForm.orderCategoryIds.length === 0) {
      showError('At least one Order Category must be selected');
      return;
    }
    
    if (siRateForm.installationMethodIds.length === 0) {
      showError('At least one Installation Method must be selected');
      return;
    }
    
    // Generate all combinations (same logic as create)
    const combinations: Array<{
      orderTypeId: string;
      orderCategoryId: string;
      installationMethodId: string | undefined;
      siLevel: string;
    }> = [];
    
    // Filter out empty string from installationMethodIds (if "All Methods" is selected)
    const methodIds = siRateForm.installationMethodIds.filter(id => id !== '');
    const hasAllMethods = siRateForm.installationMethodIds.includes('');
    
    for (const orderTypeId of siRateForm.orderTypeIds) {
      for (const orderCategoryId of siRateForm.orderCategoryIds) {
        for (const siLevel of siRateForm.siLevels) {
          if (hasAllMethods && methodIds.length === 0) {
            // Only "All Methods" selected - create one record with null method
            combinations.push({ 
              orderTypeId, 
              orderCategoryId, 
              installationMethodId: undefined,
              siLevel
            });
          } else {
            // Specific methods selected (or mix of "All Methods" + specific methods)
            for (const installationMethodId of methodIds) {
              combinations.push({ 
                orderTypeId, 
                orderCategoryId, 
                installationMethodId: installationMethodId || undefined,
                siLevel
              });
            }
          }
        }
      }
    }
    
    try {
      // Delete the original record
      await deleteGponSiJobRate(editingSiRate.id);
      
      // Create new records for all combinations
      const promises = combinations.map(combo =>
        createGponSiJobRate({
          orderTypeId: combo.orderTypeId,
          orderCategoryId: combo.orderCategoryId,
          installationMethodId: combo.installationMethodId,
          siLevel: combo.siLevel,
          partnerGroupId: siRateForm.partnerGroupId || undefined,
          payoutAmount: parseFloat(siRateForm.payoutAmount) || 0,
          validFrom: siRateForm.validFrom || undefined,
          validTo: siRateForm.validTo || undefined,
          notes: siRateForm.notes || undefined,
          isActive: siRateForm.isActive
        })
      );
      
      await Promise.all(promises);
      showSuccess(`Updated: Deleted 1 record and created ${combinations.length} new SI payout rate(s)`);
      setShowSiRateModal(false);
      setEditingSiRate(null);
      resetSiRateForm();
      loadAllData();
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to update SI rates');
    }
  };
  
  const handleDeleteSiRate = async () => {
    if (!deletingSiRate) return;
    try {
      await deleteGponSiJobRate(deletingSiRate.id);
      showSuccess('SI payout rate deleted');
      setDeletingSiRate(null);
      loadAllData();
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to delete SI rate');
    }
  };
  
  // Custom Rate CRUD
  const handleCreateCustomRate = async () => {
    // Validation
    if (!customRateForm.serviceInstallerId) {
      showError('Service Installer is required');
      return;
    }
    
    if (customRateForm.orderTypeIds.length === 0) {
      showError('At least one Order Type must be selected');
      return;
    }
    
    if (customRateForm.orderCategoryIds.length === 0) {
      showError('At least one Order Category must be selected');
      return;
    }
    
    if (customRateForm.installationMethodIds.length === 0) {
      showError('At least one Installation Method must be selected');
      return;
    }
    
    // Generate all combinations: OrderTypes × OrderCategories × InstallationMethods
    const combinations: Array<{
      orderTypeId: string;
      orderCategoryId: string;
      installationMethodId: string | undefined;
    }> = [];
    
    // Filter out empty string from installationMethodIds (if "All Methods" is selected)
    const methodIds = customRateForm.installationMethodIds.filter(id => id !== '');
    const hasAllMethods = customRateForm.installationMethodIds.includes('');
    
    for (const orderTypeId of customRateForm.orderTypeIds) {
      for (const orderCategoryId of customRateForm.orderCategoryIds) {
        if (hasAllMethods && methodIds.length === 0) {
          // Only "All Methods" selected - create one record with null method
          combinations.push({ 
            orderTypeId, 
            orderCategoryId, 
            installationMethodId: undefined 
          });
        } else {
          // Specific methods selected (or mix of "All Methods" + specific methods)
          for (const installationMethodId of methodIds) {
            combinations.push({ 
              orderTypeId, 
              orderCategoryId, 
              installationMethodId: installationMethodId || undefined 
            });
          }
        }
      }
    }
    
    // Create all rate records
    try {
      const promises = combinations.map(combo =>
        createGponSiCustomRate({
          serviceInstallerId: customRateForm.serviceInstallerId,
          orderTypeId: combo.orderTypeId,
          orderCategoryId: combo.orderCategoryId,
          installationMethodId: combo.installationMethodId,
          customPayoutAmount: parseFloat(customRateForm.customPayoutAmount) || 0,
          reason: customRateForm.reason || undefined,
          validFrom: customRateForm.validFrom || undefined,
          validTo: customRateForm.validTo || undefined,
          notes: customRateForm.notes || undefined,
          isActive: customRateForm.isActive
        })
      );
      
      await Promise.all(promises);
      showSuccess(`Created ${combinations.length} custom SI rate(s) successfully`);
      setShowCustomRateModal(false);
      resetCustomRateForm();
      loadAllData();
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to create custom rates');
    }
  };
  
  const handleUpdateCustomRate = async () => {
    if (!editingCustomRate) return;
    
    // Validation
    if (!customRateForm.serviceInstallerId) {
      showError('Service Installer is required');
      return;
    }
    
    if (customRateForm.orderTypeIds.length === 0) {
      showError('At least one Order Type must be selected');
      return;
    }
    
    if (customRateForm.orderCategoryIds.length === 0) {
      showError('At least one Order Category must be selected');
      return;
    }
    
    if (customRateForm.installationMethodIds.length === 0) {
      showError('At least one Installation Method must be selected');
      return;
    }
    
    // Generate all combinations (same logic as create)
    const combinations: Array<{
      orderTypeId: string;
      orderCategoryId: string;
      installationMethodId: string | undefined;
    }> = [];
    
    // Filter out empty string from installationMethodIds (if "All Methods" is selected)
    const methodIds = customRateForm.installationMethodIds.filter(id => id !== '');
    const hasAllMethods = customRateForm.installationMethodIds.includes('');
    
    for (const orderTypeId of customRateForm.orderTypeIds) {
      for (const orderCategoryId of customRateForm.orderCategoryIds) {
        if (hasAllMethods && methodIds.length === 0) {
          // Only "All Methods" selected - create one record with null method
          combinations.push({ 
            orderTypeId, 
            orderCategoryId, 
            installationMethodId: undefined 
          });
        } else {
          // Specific methods selected (or mix of "All Methods" + specific methods)
          for (const installationMethodId of methodIds) {
            combinations.push({ 
              orderTypeId, 
              orderCategoryId, 
              installationMethodId: installationMethodId || undefined 
            });
          }
        }
      }
    }
    
    try {
      // Delete the original record
      await deleteGponSiCustomRate(editingCustomRate.id);
      
      // Create new records for all combinations
      const promises = combinations.map(combo =>
        createGponSiCustomRate({
          serviceInstallerId: customRateForm.serviceInstallerId,
          orderTypeId: combo.orderTypeId,
          orderCategoryId: combo.orderCategoryId,
          installationMethodId: combo.installationMethodId,
          customPayoutAmount: parseFloat(customRateForm.customPayoutAmount) || 0,
          reason: customRateForm.reason || undefined,
          validFrom: customRateForm.validFrom || undefined,
          validTo: customRateForm.validTo || undefined,
          notes: customRateForm.notes || undefined,
          isActive: customRateForm.isActive
        })
      );
      
      await Promise.all(promises);
      showSuccess(`Updated: Deleted 1 record and created ${combinations.length} new custom SI rate(s)`);
      setShowCustomRateModal(false);
      setEditingCustomRate(null);
      resetCustomRateForm();
      loadAllData();
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to update custom rates');
    }
  };
  
  const handleDeleteCustomRate = async () => {
    if (!deletingCustomRate) return;
    try {
      await deleteGponSiCustomRate(deletingCustomRate.id);
      showSuccess('Custom SI rate deleted');
      setDeletingCustomRate(null);
      loadAllData();
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to delete custom rate');
    }
  };
  
  // Rate Calculator
  const handleCalculateRate = async () => {
    if (!calcRequest.orderTypeId || !calcRequest.orderCategoryId) {
      showError('Please select order type and order category');
      return;
    }
    
    try {
      const result = await resolveRates({
        partnerGroupId: calcRequest.partnerGroupId || undefined,
        orderTypeId: calcRequest.orderTypeId,
        orderCategoryId: calcRequest.orderCategoryId,
        installationMethodId: calcRequest.installationMethodId || undefined,
        siLevel: calcRequest.siLevel
      });
      setCalcResult(result);
    } catch (err: unknown) {
      const error = err as Error;
      showError(error.message || 'Failed to calculate rates');
    }
  };
  
  // Reset forms
  const resetPartnerRateForm = () => {
    setPartnerRateForm({
      partnerGroupId: '',
      partnerId: '',
      orderTypeId: '',
      orderCategoryIds: [],
      installationMethodIds: [],
      revenueAmount: '',
      validFrom: '',
      validTo: '',
      notes: '',
      isActive: true
    });
  };
  
  const resetSiRateForm = () => {
    setSiRateForm({
      orderTypeIds: [],
      orderCategoryIds: [],
      installationMethodIds: [],
      siLevels: [],
      partnerGroupId: '',
      payoutAmount: '',
      validFrom: '',
      validTo: '',
      notes: '',
      isActive: true
    });
  };
  
  const resetCustomRateForm = () => {
    setCustomRateForm({
      serviceInstallerId: '',
      orderTypeIds: [],
      orderCategoryIds: [],
      installationMethodIds: [],
      customPayoutAmount: '',
      reason: '',
      validFrom: '',
      validTo: '',
      notes: '',
      isActive: true
    });
  };
  
  // Open edit modals
  const openEditPartnerRate = (rate: GponPartnerJobRate) => {
    setEditingPartnerRate(rate);
    setPartnerRateForm({
      partnerGroupId: rate.partnerGroupId,
      partnerId: rate.partnerId || '',
      orderTypeId: rate.orderTypeId,
      orderCategoryIds: rate.orderCategoryId ? [rate.orderCategoryId] : [],
      installationMethodIds: rate.installationMethodId ? [rate.installationMethodId] : (rate.installationMethodId === null ? [''] : []),
      revenueAmount: rate.revenueAmount.toString(),
      validFrom: rate.validFrom?.split('T')[0] || '',
      validTo: rate.validTo?.split('T')[0] || '',
      notes: rate.notes || '',
      isActive: rate.isActive
    });
    setShowPartnerRateModal(true);
  };
  
  const openEditSiRate = (rate: GponSiJobRate) => {
    setEditingSiRate(rate);
    setSiRateForm({
      orderTypeIds: rate.orderTypeId ? [rate.orderTypeId] : [],
      orderCategoryIds: rate.orderCategoryId ? [rate.orderCategoryId] : [],
      installationMethodIds: rate.installationMethodId ? [rate.installationMethodId] : (rate.installationMethodId === null ? [''] : []),
      siLevels: rate.siLevel ? [rate.siLevel] : [],
      partnerGroupId: rate.partnerGroupId || '',
      payoutAmount: rate.payoutAmount.toString(),
      validFrom: rate.validFrom?.split('T')[0] || '',
      validTo: rate.validTo?.split('T')[0] || '',
      notes: rate.notes || '',
      isActive: rate.isActive
    });
    setShowSiRateModal(true);
  };
  
  const openEditCustomRate = (rate: GponSiCustomRate) => {
    setEditingCustomRate(rate);
    setCustomRateForm({
      serviceInstallerId: rate.serviceInstallerId,
      orderTypeIds: rate.orderTypeId ? [rate.orderTypeId] : [],
      orderCategoryIds: rate.orderCategoryId ? [rate.orderCategoryId] : [],
      installationMethodIds: rate.installationMethodId ? [rate.installationMethodId] : (rate.installationMethodId === null ? [''] : []),
      customPayoutAmount: rate.customPayoutAmount.toString(),
      reason: rate.reason || '',
      validFrom: rate.validFrom?.split('T')[0] || '',
      validTo: rate.validTo?.split('T')[0] || '',
      notes: rate.notes || '',
      isActive: rate.isActive
    });
    setShowCustomRateModal(true);
  };
  
  // Table columns
  const partnerRateColumns: TableColumn<GponPartnerJobRate>[] = [
    {
      key: 'partnerGroupName',
      label: 'Partner Group',
      render: (value, row) => (
        <div>
          <div className="font-medium">{value as string || 'N/A'}</div>
          {row.partnerName && <div className="text-xs text-muted-foreground">{row.partnerName}</div>}
        </div>
      )
    },
    { key: 'orderTypeName', label: 'Order Type', render: (v) => v || '-' },
    { key: 'orderCategoryName', label: 'Order Category', render: (v) => v || '-' },
    { key: 'installationMethodName', label: 'Method', render: (v) => v || <span className="text-muted-foreground text-xs">All</span> },
    { key: 'revenueAmount', label: 'Revenue (RM)', render: (v) => `RM ${(v as number || 0).toFixed(2)}` },
    {
      key: 'isActive',
      label: 'Status',
      render: (v) => <StatusBadge variant={v ? 'success' : 'default'}>{v ? 'Active' : 'Inactive'}</StatusBadge>
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_value, row) => (
        <div className="flex items-center gap-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              openEditPartnerRate(row);
            }}
            title="Edit"
            className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
          >
            <Edit className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setDeletingPartnerRate(row);
            }}
            title="Delete"
            className="p-1 rounded text-red-600 hover:text-red-700 hover:bg-muted transition-colors"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      ),
      width: '100px'
    }
  ];
  
  const siRateColumns: TableColumn<GponSiJobRate>[] = [
    { key: 'siLevel', label: 'SI Level' },
    { key: 'orderTypeName', label: 'Order Type', render: (v) => v || '-' },
    { key: 'orderCategoryName', label: 'Order Category', render: (v) => v || '-' },
    { key: 'installationMethodName', label: 'Method', render: (v) => v || <span className="text-muted-foreground text-xs">All</span> },
    { key: 'partnerGroupName', label: 'Partner Group', render: (v) => v || <span className="text-muted-foreground text-xs">All</span> },
    { key: 'payoutAmount', label: 'Payout (RM)', render: (v) => `RM ${(v as number || 0).toFixed(2)}` },
    {
      key: 'isActive',
      label: 'Status',
      render: (v) => <StatusBadge variant={v ? 'success' : 'default'}>{v ? 'Active' : 'Inactive'}</StatusBadge>
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_value, row) => (
        <div className="flex items-center gap-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              openEditSiRate(row);
            }}
            title="Edit"
            className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
          >
            <Edit className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setDeletingSiRate(row);
            }}
            title="Delete"
            className="p-1 rounded text-red-600 hover:text-red-700 hover:bg-muted transition-colors"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      ),
      width: '100px'
    }
  ];
  
  const customRateColumns: TableColumn<GponSiCustomRate>[] = [
    { key: 'serviceInstallerName', label: 'Service Installer' },
    { key: 'orderTypeName', label: 'Order Type', render: (v) => v || '-' },
    { key: 'orderCategoryName', label: 'Order Category', render: (v) => v || '-' },
    { key: 'installationMethodName', label: 'Method', render: (v) => v || <span className="text-muted-foreground text-xs">All</span> },
    { key: 'customPayoutAmount', label: 'Custom Payout (RM)', render: (v) => `RM ${(v as number || 0).toFixed(2)}` },
    { key: 'reason', label: 'Reason', render: (v) => v || '-' },
    {
      key: 'isActive',
      label: 'Status',
      render: (v) => <StatusBadge variant={v ? 'success' : 'default'}>{v ? 'Active' : 'Inactive'}</StatusBadge>
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_value, row) => (
        <div className="flex items-center gap-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              openEditCustomRate(row);
            }}
            title="Edit"
            className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
          >
            <Edit className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              setDeletingCustomRate(row);
            }}
            title="Delete"
            className="p-1 rounded text-red-600 hover:text-red-700 hover:bg-muted transition-colors"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      ),
      width: '100px'
    }
  ];
  
  // Helper function to check if a string matches search query
  const matchesSearch = (text: string | null | undefined, query: string): boolean => {
    if (!query) return true;
    if (!text) return false;
    return text.toLowerCase().includes(query.toLowerCase());
  };
  
  // Filter and search rates
  const getFilteredRates = <T extends GponPartnerJobRate | GponSiJobRate | GponSiCustomRate>(
    rates: T[],
    tabType: 'partner' | 'si' | 'custom'
  ): T[] => {
    let filtered = [...rates];
    
    // Apply filters
    if (filters.partnerGroupId) {
      filtered = filtered.filter(r => {
        if ('partnerGroupId' in r) {
          return r.partnerGroupId === filters.partnerGroupId;
        }
        return false;
      });
    }
    if (filters.orderTypeId) {
      filtered = filtered.filter(r => r.orderTypeId === filters.orderTypeId);
    }
    if (filters.orderCategoryId) {
      filtered = filtered.filter(r => r.orderCategoryId === filters.orderCategoryId);
    }
    if (filters.installationMethodId) {
      filtered = filtered.filter(r => r.installationMethodId === filters.installationMethodId);
    }
    if (filters.siLevel && tabType === 'si') {
      filtered = filtered.filter(r => (r as GponSiJobRate).siLevel === filters.siLevel);
    }
    if (filters.isActive !== '') {
      const isActive = filters.isActive === 'true';
      filtered = filtered.filter(r => r.isActive === isActive);
    }
    
    // Apply search query
    if (searchQuery) {
      filtered = filtered.filter(rate => {
        // Search in partner group name (only for partner and SI rates)
        if ('partnerGroupName' in rate && matchesSearch(rate.partnerGroupName, searchQuery)) return true;
        // Search in partner name (only for partner rates)
        if ('partnerName' in rate && matchesSearch(rate.partnerName, searchQuery)) return true;
        // Search in order type name
        if (matchesSearch(rate.orderTypeName, searchQuery)) return true;
        // Search in order category name
        if (matchesSearch(rate.orderCategoryName, searchQuery)) return true;
        // Search in installation method name
        if (matchesSearch(rate.installationMethodName, searchQuery)) return true;
        // Search in service installer name (for custom rates)
        if ('serviceInstallerName' in rate && matchesSearch(rate.serviceInstallerName, searchQuery)) return true;
        // Search in SI level (for SI rates)
        if ('siLevel' in rate && matchesSearch((rate as GponSiJobRate).siLevel, searchQuery)) return true;
        return false;
      });
    }
    
    return filtered;
  };
  
  const filteredPartnerRates = getFilteredRates(partnerRates, 'partner');
  const filteredSiRates = getFilteredRates(siRates, 'si');
  const filteredCustomRates = getFilteredRates(customRates, 'custom');
  
  // Get active filter count
  const activeFilterCount = Object.entries(filters).filter(([key, value]) => {
    if (key === 'isActive') return value !== '' && value !== 'true';
    return value !== '';
  }).length;
  
  // Clear all filters
  const clearAllFilters = () => {
    setFilters({
      partnerGroupId: '',
      orderTypeId: '',
      orderCategoryId: '',
      installationMethodId: '',
      siLevel: '',
      isActive: 'true'
    });
    setSearchQuery('');
  };
  
  // Clear a specific filter
  const clearFilter = (key: keyof typeof filters) => {
    setFilters(prev => ({ ...prev, [key]: key === 'isActive' ? 'true' : '' }));
  };
  
  // Handle rate selection
  const handleSelectRate = (rate: GponPartnerJobRate | GponSiJobRate | GponSiCustomRate, selected: boolean) => {
    setSelectedRates(prev => {
      const next = new Set(prev);
      if (selected) {
        next.add(rate.id);
      } else {
        next.delete(rate.id);
      }
      return next;
    });
  };
  
  // Clear all selections
  const clearSelections = () => {
    setSelectedRates(new Set());
  };
  
  if (loading) {
    return (
      <PageShell title="GPON Rate Engine" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Rate Engine' }]}>
        <LoadingSpinner message="Loading rate engine data..." />
      </PageShell>
    );
  }
  
  return (
    <PageShell
      title="GPON Rate Engine"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Rate Engine' }]}
      actions={
        <div className="flex items-center gap-2 flex-wrap">
          <Button variant="outline" size="sm" onClick={() => setShowRateCalculator(true)}>
            <Calculator className="h-3 w-3 mr-1" />
            Calculate
          </Button>
          <Button variant="outline" size="sm" onClick={loadAllData}>
            <RefreshCcw className="h-3 w-3" />
          </Button>
        </div>
      }
    >
      <div className="flex-1 p-3 md:p-4 max-w-7xl mx-auto h-full flex flex-col">
      {/* How-To Guide */}
      <Card className="mb-3 bg-gradient-to-r from-emerald-900/20 to-blue-900/20 border-emerald-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-3 py-2"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-emerald-400" />
            <span className="font-medium text-white text-sm">How GPON Rate Engine Works</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-3 pb-3">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2">
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <TrendingUp className="h-3 w-3 text-green-400" />
                  Partner Revenue
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• What Cephas earns</li>
                  <li>• By partner group</li>
                  <li>• Order + Install type</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <TrendingDown className="h-3 w-3 text-red-400" />
                  SI Payouts
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• What SI earns</li>
                  <li>• By level (Jr/Sr/Sub)</li>
                  <li>• Default rates</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <Users className="h-3 w-3 text-purple-400" />
                  Custom Overrides
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>• Per-SI exceptions</li>
                  <li>• Special agreements</li>
                  <li>• Takes priority</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-2">
                <h4 className="text-xs font-medium text-white mb-1 flex items-center gap-1">
                  <Layers className="h-3 w-3 text-orange-400" />
                  Resolution Order
                </h4>
                <ul className="text-[11px] text-slate-300 space-y-0.5">
                  <li>1. Custom Rate</li>
                  <li>2. SI Level Rate</li>
                  <li>3. Partner Revenue</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>
      
      {/* Search and View Toggle */}
      <Card className="mb-3">
        <div className="p-3">
          <div className="flex flex-col sm:flex-row gap-3 items-start sm:items-center justify-between">
            {/* Search Input */}
            <div className="flex-1 w-full sm:max-w-md">
              <div className="relative">
                <Search className="absolute left-2 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <TextInput
                  type="text"
                  placeholder="Search by partner, installer, order type, category, method..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-8 pr-3"
                />
                {searchQuery && (
                  <button
                    onClick={() => setSearchQuery('')}
                    className="absolute right-2 top-1/2 transform -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  >
                    <X className="h-4 w-4" />
                  </button>
                )}
              </div>
            </div>
            
            {/* View Toggle */}
            <div className="flex items-center gap-2">
              <span className="text-xs text-muted-foreground">View:</span>
              <div className="flex border rounded-md overflow-hidden">
                <button
                  onClick={() => setViewMode('flat')}
                  className={cn(
                    "px-3 py-1.5 text-xs font-medium transition-colors flex items-center gap-1",
                    viewMode === 'flat' 
                      ? "bg-primary text-primary-foreground" 
                      : "bg-background text-muted-foreground hover:bg-muted"
                  )}
                >
                  <List className="h-3 w-3" />
                  Flat
                </button>
                <button
                  onClick={() => setViewMode('grouped')}
                  className={cn(
                    "px-3 py-1.5 text-xs font-medium transition-colors flex items-center gap-1",
                    viewMode === 'grouped' 
                      ? "bg-primary text-primary-foreground" 
                      : "bg-background text-muted-foreground hover:bg-muted"
                  )}
                >
                  <LayoutGrid className="h-3 w-3" />
                  Grouped
                </button>
              </div>
            </div>
          </div>
        </div>
      </Card>
      
      {/* Filters */}
      <Card className="mb-3">
        <div className="p-3">
          <div className="flex items-center justify-between mb-2">
            <div className="flex items-center gap-2">
              <span className="text-xs font-medium">Filters</span>
              {activeFilterCount > 0 && (
                <span className="text-xs bg-primary text-primary-foreground px-2 py-0.5 rounded-full">
                  {activeFilterCount}
                </span>
              )}
            </div>
            <div className="flex items-center gap-2">
              {activeFilterCount > 0 && (
                <button
                  onClick={clearAllFilters}
                  className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1"
                >
                  <X className="h-3 w-3" />
                  Clear All
                </button>
              )}
              <button 
                onClick={() => setShowFilters(!showFilters)}
                className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1"
              >
                <Filter className="h-3 w-3" />
                {showFilters ? 'Hide' : 'Show'}
              </button>
            </div>
          </div>
          
          {/* Filter Chips */}
          {activeFilterCount > 0 && (
            <div className="flex flex-wrap gap-2 mb-2 pt-2 border-t border-border">
              {filters.partnerGroupId && (
                <div className="flex items-center gap-1 bg-muted px-2 py-1 rounded text-xs">
                  <span>Partner Group: {partnerGroups.find(pg => pg.id === filters.partnerGroupId)?.name || 'Unknown'}</span>
                  <button onClick={() => clearFilter('partnerGroupId')} className="hover:text-destructive">
                    <X className="h-3 w-3" />
                  </button>
                </div>
              )}
              {filters.orderTypeId && (
                <div className="flex items-center gap-1 bg-muted px-2 py-1 rounded text-xs">
                  <span>Order Type: {orderTypes.find(ot => ot.id === filters.orderTypeId)?.name || 'Unknown'}</span>
                  <button onClick={() => clearFilter('orderTypeId')} className="hover:text-destructive">
                    <X className="h-3 w-3" />
                  </button>
                </div>
              )}
              {filters.orderCategoryId && (
                <div className="flex items-center gap-1 bg-muted px-2 py-1 rounded text-xs">
                  <span>Category: {orderCategories.find(oc => oc.id === filters.orderCategoryId)?.name || 'Unknown'}</span>
                  <button onClick={() => clearFilter('orderCategoryId')} className="hover:text-destructive">
                    <X className="h-3 w-3" />
                  </button>
                </div>
              )}
              {filters.installationMethodId && (
                <div className="flex items-center gap-1 bg-muted px-2 py-1 rounded text-xs">
                  <span>Method: {installationMethods.find(im => im.id === filters.installationMethodId)?.name || 'Unknown'}</span>
                  <button onClick={() => clearFilter('installationMethodId')} className="hover:text-destructive">
                    <X className="h-3 w-3" />
                  </button>
                </div>
              )}
              {filters.siLevel && (
                <div className="flex items-center gap-1 bg-muted px-2 py-1 rounded text-xs">
                  <span>SI Level: {filters.siLevel}</span>
                  <button onClick={() => clearFilter('siLevel')} className="hover:text-destructive">
                    <X className="h-3 w-3" />
                  </button>
                </div>
              )}
              {filters.isActive !== '' && filters.isActive !== 'true' && (
                <div className="flex items-center gap-1 bg-muted px-2 py-1 rounded text-xs">
                  <span>Status: {filters.isActive === 'true' ? 'Active' : 'Inactive'}</span>
                  <button onClick={() => clearFilter('isActive')} className="hover:text-destructive">
                    <X className="h-3 w-3" />
                  </button>
                </div>
              )}
            </div>
          )}
          
          {showFilters && (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-3 pt-2 border-t border-border">
              <Select
                label="Partner Group"
                value={filters.partnerGroupId}
                onChange={(e) => setFilters({ ...filters, partnerGroupId: e.target.value })}
                options={[
                  { value: '', label: 'All Partner Groups' },
                  ...partnerGroups.map(pg => ({ value: pg.id, label: pg.name }))
                ]}
              />
              <Select
                label="Order Type"
                value={filters.orderTypeId}
                onChange={(e) => setFilters({ ...filters, orderTypeId: e.target.value })}
                options={[
                  { value: '', label: 'All Order Types' },
                  ...orderTypes.map(ot => ({ value: ot.id, label: ot.name }))
                ]}
              />
              <Select
                label="Order Category"
                value={filters.orderCategoryId}
                onChange={(e) => setFilters({ ...filters, orderCategoryId: e.target.value })}
                options={[
                  { value: '', label: 'All Categories' },
                  ...orderCategories.map(oc => ({ value: oc.id, label: oc.name }))
                ]}
              />
              <Select
                label="Installation Method"
                value={filters.installationMethodId}
                onChange={(e) => setFilters({ ...filters, installationMethodId: e.target.value })}
                options={[
                  { value: '', label: 'All Methods' },
                  ...installationMethods.map(im => ({ value: im.id, label: im.name }))
                ]}
              />
              <Select
                label="SI Level"
                value={filters.siLevel}
                onChange={(e) => setFilters({ ...filters, siLevel: e.target.value })}
                options={[
                  { value: '', label: 'All Levels' },
                  ...SI_LEVELS.map(l => ({ value: l, label: l }))
                ]}
              />
              <Select
                label="Status"
                value={filters.isActive}
                onChange={(e) => setFilters({ ...filters, isActive: e.target.value })}
                options={[
                  { value: '', label: 'All' },
                  { value: 'true', label: 'Active Only' },
                  { value: 'false', label: 'Inactive Only' }
                ]}
              />
            </div>
          )}
        </div>
      </Card>
      
      {/* Bulk Actions Bar */}
      {selectedRates.size > 0 && (
        <Card className="mb-3 bg-primary/10 border-primary/20">
          <div className="p-3 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium text-foreground">
                {selectedRates.size} rate{selectedRates.size !== 1 ? 's' : ''} selected
              </span>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={clearSelections}
                className="text-xs"
              >
                Clear Selection
              </Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={async () => {
                  if (confirm(`Are you sure you want to delete ${selectedRates.size} rate(s)?`)) {
                    try {
                      const allRates = [...filteredPartnerRates, ...filteredSiRates, ...filteredCustomRates];
                      const ratesToDelete = allRates.filter(r => selectedRates.has(r.id));
                      await Promise.all(
                        ratesToDelete.map(rate => {
                          if ('revenueAmount' in rate) {
                            return deleteGponPartnerJobRate(rate.id);
                          } else if ('siLevel' in rate) {
                            return deleteGponSiJobRate(rate.id);
                          } else {
                            return deleteGponSiCustomRate(rate.id);
                          }
                        })
                      );
                      showSuccess(`Deleted ${selectedRates.size} rate(s) successfully`);
                      clearSelections();
                      loadAllData();
                    } catch (err: unknown) {
                      const error = err as Error;
                      showError(error.message || 'Failed to delete rates');
                    }
                  }
                }}
                className="text-xs"
              >
                <Trash2 className="h-3 w-3 mr-1" />
                Delete Selected
              </Button>
            </div>
          </div>
        </Card>
      )}
      
      {/* Tabs */}
      <Card className="flex-1 flex flex-col min-h-0">
        <Tabs defaultActiveTab={activeTab} onChange={setActiveTab}>
          <TabPanel label={`Partner Revenue (${filteredPartnerRates.length}${filteredPartnerRates.length !== partnerRates.length ? ` / ${partnerRates.length}` : ''})`}>
            <div className="p-3">
              <div className="flex justify-between items-center mb-3">
                <p className="text-xs text-muted-foreground">Revenue rates from partners (what Cephas earns)</p>
                <Button size="sm" onClick={() => { resetPartnerRateForm(); setShowPartnerRateModal(true); }}>
                  <Plus className="h-3 w-3 mr-1" />
                  Add Rate
                </Button>
              </div>
              {filteredPartnerRates.length > 0 ? (
                viewMode === 'grouped' ? (
                  <GroupedRateTable
                    rates={filteredPartnerRates}
                    onEdit={openEditPartnerRate}
                    onDelete={(rate) => setDeletingPartnerRate(rate as GponPartnerJobRate)}
                    searchQuery={searchQuery}
                    groupType="partner"
                    emptyMessage="No matching partner rates"
                    selectedRates={selectedRates}
                    onSelectRate={handleSelectRate}
                  />
                ) : (
                  <DataTable
                    data={filteredPartnerRates}
                    columns={partnerRateColumns}
                  />
                )
              ) : partnerRates.length > 0 ? (
                <EmptyState title="No matching partner rates" message="Try adjusting your filters or search query." />
              ) : (
                <EmptyState title="No partner rates" message="Add partner revenue rates to get started." />
              )}
            </div>
          </TabPanel>
          
          <TabPanel label={`SI Payouts (${filteredSiRates.length}${filteredSiRates.length !== siRates.length ? ` / ${siRates.length}` : ''})`}>
            <div className="p-3">
              <div className="flex justify-between items-center mb-3">
                <p className="text-xs text-muted-foreground">Default payout rates to SIs by level</p>
                <Button size="sm" onClick={() => { resetSiRateForm(); setShowSiRateModal(true); }}>
                  <Plus className="h-3 w-3 mr-1" />
                  Add Rate
                </Button>
              </div>
              {filteredSiRates.length > 0 ? (
                viewMode === 'grouped' ? (
                  <GroupedRateTable
                    rates={filteredSiRates}
                    onEdit={openEditSiRate}
                    onDelete={(rate) => setDeletingSiRate(rate as GponSiJobRate)}
                    searchQuery={searchQuery}
                    groupType="si"
                    emptyMessage="No matching SI rates"
                    selectedRates={selectedRates}
                    onSelectRate={handleSelectRate}
                  />
                ) : (
                  <DataTable
                    data={filteredSiRates}
                    columns={siRateColumns}
                  />
                )
              ) : siRates.length > 0 ? (
                <EmptyState title="No matching SI rates" message="Try adjusting your filters or search query." />
              ) : (
                <EmptyState title="No SI rates" message="Add SI payout rates to get started." />
              )}
            </div>
          </TabPanel>
          
          <TabPanel label={`Custom Overrides (${filteredCustomRates.length}${filteredCustomRates.length !== customRates.length ? ` / ${customRates.length}` : ''})`}>
            <div className="p-3">
              <div className="flex justify-between items-center mb-3">
                <p className="text-xs text-muted-foreground">Per-SI custom rate overrides (highest priority)</p>
                <Button size="sm" onClick={() => { resetCustomRateForm(); setShowCustomRateModal(true); }}>
                  <Plus className="h-3 w-3 mr-1" />
                  Add Override
                </Button>
              </div>
              {filteredCustomRates.length > 0 ? (
                viewMode === 'grouped' ? (
                  <GroupedRateTable
                    rates={filteredCustomRates}
                    onEdit={openEditCustomRate}
                    onDelete={(rate) => setDeletingCustomRate(rate as GponSiCustomRate)}
                    searchQuery={searchQuery}
                    groupType="custom"
                    emptyMessage="No matching custom overrides"
                    selectedRates={selectedRates}
                    onSelectRate={handleSelectRate}
                  />
                ) : (
                  <DataTable
                    data={filteredCustomRates}
                    columns={customRateColumns}
                  />
                )
              ) : customRates.length > 0 ? (
                <EmptyState title="No matching custom overrides" message="Try adjusting your filters or search query." />
              ) : (
                <EmptyState title="No custom overrides" message="Add per-SI custom rate overrides as needed." />
              )}
            </div>
          </TabPanel>
        </Tabs>
      </Card>
      
      {/* Partner Rate Modal */}
      <Modal
        isOpen={showPartnerRateModal}
        onClose={() => { setShowPartnerRateModal(false); setEditingPartnerRate(null); resetPartnerRateForm(); }}
        title={editingPartnerRate ? 'Edit Partner Revenue Rate' : 'Add Partner Revenue Rate'}
        size="lg"
      >
        <div className="space-y-4 p-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="Partner Group *"
              value={partnerRateForm.partnerGroupId}
              onChange={(e) => setPartnerRateForm({ ...partnerRateForm, partnerGroupId: e.target.value })}
              options={[
                { value: '', label: 'Select Partner Group' },
                ...partnerGroups.map(pg => ({ value: pg.id, label: pg.name }))
              ]}
              required
            />
            <Select
              label="Partner (Optional Override)"
              value={partnerRateForm.partnerId}
              onChange={(e) => setPartnerRateForm({ ...partnerRateForm, partnerId: e.target.value })}
              options={[
                { value: '', label: 'All Partners in Group' },
                ...partners.filter(p => !partnerRateForm.partnerGroupId || p.partnerGroupId === partnerRateForm.partnerGroupId)
                  .map(p => ({ value: p.id, label: p.name }))
              ]}
            />
          </div>
          <div className="grid grid-cols-1 gap-4">
            <Select
              label="Order Type *"
              value={partnerRateForm.orderTypeId}
              onChange={(e) => setPartnerRateForm({ ...partnerRateForm, orderTypeId: e.target.value })}
              options={[
                { value: '', label: 'Select Order Type' },
                ...orderTypes.map(ot => ({ value: ot.id, label: ot.name }))
              ]}
              required
            />
          </div>
          
          {/* Order Categories - Checkbox Group */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Order Categories * 
              {partnerRateForm.orderCategoryIds.length > 0 && (
                <span className="text-xs text-muted-foreground ml-2">
                  ({partnerRateForm.orderCategoryIds.length} selected)
                </span>
              )}
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto border rounded-lg p-3 bg-muted/30">
              {orderCategories.length === 0 ? (
                <p className="text-xs text-muted-foreground">No order categories available</p>
              ) : (
                orderCategories.map(cat => (
                  <label 
                    key={cat.id} 
                    className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={partnerRateForm.orderCategoryIds.includes(cat.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setPartnerRateForm({
                            ...partnerRateForm,
                            orderCategoryIds: [...partnerRateForm.orderCategoryIds, cat.id]
                          });
                        } else {
                          setPartnerRateForm({
                            ...partnerRateForm,
                            orderCategoryIds: partnerRateForm.orderCategoryIds.filter(id => id !== cat.id)
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                    />
                    <span className="text-sm">{cat.name}</span>
                  </label>
                ))
              )}
            </div>
            {partnerRateForm.orderCategoryIds.length === 0 && (
              <p className="text-xs text-destructive">At least one category must be selected</p>
            )}
          </div>
          
          {/* Installation Methods - Checkbox Group */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Installation Methods * 
              {partnerRateForm.installationMethodIds.length > 0 && (
                <span className="text-xs text-muted-foreground ml-2">
                  ({partnerRateForm.installationMethodIds.length} selected)
                </span>
              )}
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto border rounded-lg p-3 bg-muted/30">
              {installationMethods.length === 0 ? (
                <p className="text-xs text-muted-foreground">No installation methods available</p>
              ) : (
                <>
                  {/* Option for "All Methods" (null) */}
                  <label 
                    className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={partnerRateForm.installationMethodIds.includes('')}
                      onChange={(e) => {
                        if (e.target.checked) {
                          // If "All Methods" is checked, clear other selections
                          setPartnerRateForm({
                            ...partnerRateForm,
                            installationMethodIds: ['']
                          });
                        } else {
                          setPartnerRateForm({
                            ...partnerRateForm,
                            installationMethodIds: partnerRateForm.installationMethodIds.filter(id => id !== '')
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                    />
                    <span className="text-sm font-medium">All Methods (No specific method)</span>
                  </label>
                  
                  {/* Specific installation methods */}
                  {installationMethods.map(im => (
                    <label 
                      key={im.id} 
                      className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                    >
                      <input
                        type="checkbox"
                        checked={partnerRateForm.installationMethodIds.includes(im.id)}
                        onChange={(e) => {
                          if (e.target.checked) {
                            // Remove "All Methods" if a specific method is selected
                            const filtered = partnerRateForm.installationMethodIds.filter(id => id !== '');
                            setPartnerRateForm({
                              ...partnerRateForm,
                              installationMethodIds: [...filtered, im.id]
                            });
                          } else {
                            setPartnerRateForm({
                              ...partnerRateForm,
                              installationMethodIds: partnerRateForm.installationMethodIds.filter(id => id !== im.id)
                            });
                          }
                        }}
                        className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                      />
                      <span className="text-sm">{im.name}</span>
                    </label>
                  ))}
                </>
              )}
            </div>
            {partnerRateForm.installationMethodIds.length === 0 && (
              <p className="text-xs text-destructive">At least one method must be selected</p>
            )}
            {partnerRateForm.orderCategoryIds.length > 0 && partnerRateForm.installationMethodIds.length > 0 && (
              <p className="text-xs text-muted-foreground">
                {(() => {
                  const methodIds = partnerRateForm.installationMethodIds.filter(id => id !== '');
                  const hasAllMethods = partnerRateForm.installationMethodIds.includes('');
                  const methodCount = hasAllMethods && methodIds.length === 0 ? 1 : methodIds.length;
                  const totalCombinations = partnerRateForm.orderCategoryIds.length * methodCount;
                  return `Will create ${totalCombinations} rate record(s) for all combinations.`;
                })()}
              </p>
            )}
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <TextInput
              label="Revenue Amount (RM) *"
              type="number"
              step="0.01"
              value={partnerRateForm.revenueAmount}
              onChange={(e) => setPartnerRateForm({ ...partnerRateForm, revenueAmount: e.target.value })}
              placeholder="e.g., 150.00"
              required
            />
            <TextInput
              label="Valid From"
              type="date"
              value={partnerRateForm.validFrom}
              onChange={(e) => setPartnerRateForm({ ...partnerRateForm, validFrom: e.target.value })}
            />
            <TextInput
              label="Valid To"
              type="date"
              value={partnerRateForm.validTo}
              onChange={(e) => setPartnerRateForm({ ...partnerRateForm, validTo: e.target.value })}
            />
          </div>
          <TextInput
            label="Notes"
            value={partnerRateForm.notes}
            onChange={(e) => setPartnerRateForm({ ...partnerRateForm, notes: e.target.value })}
            placeholder="Optional notes"
          />
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="partnerRateActive"
              checked={partnerRateForm.isActive}
              onChange={(e) => setPartnerRateForm({ ...partnerRateForm, isActive: e.target.checked })}
              className="h-4 w-4 rounded border-input"
            />
            <label htmlFor="partnerRateActive" className="text-sm">Active</label>
          </div>
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => { setShowPartnerRateModal(false); setEditingPartnerRate(null); resetPartnerRateForm(); }}>
              Cancel
            </Button>
            <Button onClick={editingPartnerRate ? handleUpdatePartnerRate : handleCreatePartnerRate}>
              <Save className="h-3 w-3 mr-1" />
              {editingPartnerRate ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
      
      {/* SI Rate Modal */}
      <Modal
        isOpen={showSiRateModal}
        onClose={() => { setShowSiRateModal(false); setEditingSiRate(null); resetSiRateForm(); }}
        title={editingSiRate ? 'Edit SI Payout Rate' : 'Add SI Payout Rate'}
        size="lg"
      >
        <div className="space-y-4 p-4">
          <Select
            label="Partner Group (Optional)"
            value={siRateForm.partnerGroupId}
            onChange={(e) => setSiRateForm({ ...siRateForm, partnerGroupId: e.target.value })}
            options={[
              { value: '', label: 'All Partner Groups' },
              ...partnerGroups.map(pg => ({ value: pg.id, label: pg.name }))
            ]}
          />
          
          {/* SI Levels - Checkbox Group */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              SI Levels * 
              {siRateForm.siLevels.length > 0 && (
                <span className="text-xs text-muted-foreground ml-2">
                  ({siRateForm.siLevels.length} selected)
                </span>
              )}
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto border rounded-lg p-3 bg-muted/30">
              {SI_LEVELS.length === 0 ? (
                <p className="text-xs text-muted-foreground">No SI levels available</p>
              ) : (
                SI_LEVELS.map(level => (
                  <label 
                    key={level} 
                    className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={siRateForm.siLevels.includes(level)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSiRateForm({
                            ...siRateForm,
                            siLevels: [...siRateForm.siLevels, level]
                          });
                        } else {
                          setSiRateForm({
                            ...siRateForm,
                            siLevels: siRateForm.siLevels.filter(l => l !== level)
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                    />
                    <span className="text-sm">{level}</span>
                  </label>
                ))
              )}
            </div>
            {siRateForm.siLevels.length === 0 && (
              <p className="text-xs text-destructive">At least one SI level must be selected</p>
            )}
          </div>
          {/* Order Types - Checkbox Group */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Order Types * 
              {siRateForm.orderTypeIds.length > 0 && (
                <span className="text-xs text-muted-foreground ml-2">
                  ({siRateForm.orderTypeIds.length} selected)
                </span>
              )}
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto border rounded-lg p-3 bg-muted/30">
              {orderTypes.length === 0 ? (
                <p className="text-xs text-muted-foreground">No order types available</p>
              ) : (
                orderTypes.map(ot => (
                  <label 
                    key={ot.id} 
                    className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={siRateForm.orderTypeIds.includes(ot.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSiRateForm({
                            ...siRateForm,
                            orderTypeIds: [...siRateForm.orderTypeIds, ot.id]
                          });
                        } else {
                          setSiRateForm({
                            ...siRateForm,
                            orderTypeIds: siRateForm.orderTypeIds.filter(id => id !== ot.id)
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                    />
                    <span className="text-sm">{ot.name}</span>
                  </label>
                ))
              )}
            </div>
            {siRateForm.orderTypeIds.length === 0 && (
              <p className="text-xs text-destructive">At least one order type must be selected</p>
            )}
          </div>
          
          {/* Order Categories - Checkbox Group */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Order Categories * 
              {siRateForm.orderCategoryIds.length > 0 && (
                <span className="text-xs text-muted-foreground ml-2">
                  ({siRateForm.orderCategoryIds.length} selected)
                </span>
              )}
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto border rounded-lg p-3 bg-muted/30">
              {orderCategories.length === 0 ? (
                <p className="text-xs text-muted-foreground">No order categories available</p>
              ) : (
                orderCategories.map(cat => (
                  <label 
                    key={cat.id} 
                    className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={siRateForm.orderCategoryIds.includes(cat.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setSiRateForm({
                            ...siRateForm,
                            orderCategoryIds: [...siRateForm.orderCategoryIds, cat.id]
                          });
                        } else {
                          setSiRateForm({
                            ...siRateForm,
                            orderCategoryIds: siRateForm.orderCategoryIds.filter(id => id !== cat.id)
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                    />
                    <span className="text-sm">{cat.name}</span>
                  </label>
                ))
              )}
            </div>
            {siRateForm.orderCategoryIds.length === 0 && (
              <p className="text-xs text-destructive">At least one category must be selected</p>
            )}
          </div>
          
          {/* Installation Methods - Checkbox Group */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Installation Methods * 
              {siRateForm.installationMethodIds.length > 0 && (
                <span className="text-xs text-muted-foreground ml-2">
                  ({siRateForm.installationMethodIds.length} selected)
                </span>
              )}
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto border rounded-lg p-3 bg-muted/30">
              {installationMethods.length === 0 ? (
                <p className="text-xs text-muted-foreground">No installation methods available</p>
              ) : (
                <>
                  {/* Option for "All Methods" (null) */}
                  <label 
                    className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={siRateForm.installationMethodIds.includes('')}
                      onChange={(e) => {
                        if (e.target.checked) {
                          // If "All Methods" is checked, clear other selections
                          setSiRateForm({
                            ...siRateForm,
                            installationMethodIds: ['']
                          });
                        } else {
                          setSiRateForm({
                            ...siRateForm,
                            installationMethodIds: siRateForm.installationMethodIds.filter(id => id !== '')
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                    />
                    <span className="text-sm font-medium">All Methods (No specific method)</span>
                  </label>
                  
                  {/* Specific installation methods */}
                  {installationMethods.map(im => (
                    <label 
                      key={im.id} 
                      className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                    >
                      <input
                        type="checkbox"
                        checked={siRateForm.installationMethodIds.includes(im.id)}
                        onChange={(e) => {
                          if (e.target.checked) {
                            // Remove "All Methods" if a specific method is selected
                            const filtered = siRateForm.installationMethodIds.filter(id => id !== '');
                            setSiRateForm({
                              ...siRateForm,
                              installationMethodIds: [...filtered, im.id]
                            });
                          } else {
                            setSiRateForm({
                              ...siRateForm,
                              installationMethodIds: siRateForm.installationMethodIds.filter(id => id !== im.id)
                            });
                          }
                        }}
                        className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                      />
                      <span className="text-sm">{im.name}</span>
                    </label>
                  ))}
                </>
              )}
            </div>
            {siRateForm.installationMethodIds.length === 0 && (
              <p className="text-xs text-destructive">At least one method must be selected</p>
            )}
            {siRateForm.orderTypeIds.length > 0 && siRateForm.orderCategoryIds.length > 0 && siRateForm.installationMethodIds.length > 0 && siRateForm.siLevels.length > 0 && (
              <p className="text-xs text-muted-foreground">
                {(() => {
                  const methodIds = siRateForm.installationMethodIds.filter(id => id !== '');
                  const hasAllMethods = siRateForm.installationMethodIds.includes('');
                  const methodCount = hasAllMethods && methodIds.length === 0 ? 1 : methodIds.length;
                  const totalCombinations = siRateForm.orderTypeIds.length * siRateForm.orderCategoryIds.length * methodCount * siRateForm.siLevels.length;
                  return `Will create ${totalCombinations} rate record(s) for all combinations.`;
                })()}
              </p>
            )}
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <TextInput
              label="Payout Amount (RM) *"
              type="number"
              step="0.01"
              value={siRateForm.payoutAmount}
              onChange={(e) => setSiRateForm({ ...siRateForm, payoutAmount: e.target.value })}
              placeholder="e.g., 45.00"
              required
            />
            <TextInput
              label="Valid From"
              type="date"
              value={siRateForm.validFrom}
              onChange={(e) => setSiRateForm({ ...siRateForm, validFrom: e.target.value })}
            />
            <TextInput
              label="Valid To"
              type="date"
              value={siRateForm.validTo}
              onChange={(e) => setSiRateForm({ ...siRateForm, validTo: e.target.value })}
            />
          </div>
          <TextInput
            label="Notes"
            value={siRateForm.notes}
            onChange={(e) => setSiRateForm({ ...siRateForm, notes: e.target.value })}
            placeholder="Optional notes"
          />
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="siRateActive"
              checked={siRateForm.isActive}
              onChange={(e) => setSiRateForm({ ...siRateForm, isActive: e.target.checked })}
              className="h-4 w-4 rounded border-input"
            />
            <label htmlFor="siRateActive" className="text-sm">Active</label>
          </div>
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => { setShowSiRateModal(false); setEditingSiRate(null); resetSiRateForm(); }}>
              Cancel
            </Button>
            <Button onClick={editingSiRate ? handleUpdateSiRate : handleCreateSiRate}>
              <Save className="h-3 w-3 mr-1" />
              {editingSiRate ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
      
      {/* Custom Rate Modal */}
      <Modal
        isOpen={showCustomRateModal}
        onClose={() => { setShowCustomRateModal(false); setEditingCustomRate(null); resetCustomRateForm(); }}
        title={editingCustomRate ? 'Edit Custom SI Rate' : 'Add Custom SI Rate'}
        size="lg"
      >
        <div className="space-y-4 p-4">
          <Select
            label="Service Installer *"
            value={customRateForm.serviceInstallerId}
            onChange={(e) => setCustomRateForm({ ...customRateForm, serviceInstallerId: e.target.value })}
            options={[
              { value: '', label: 'Select Service Installer' },
              ...serviceInstallers.map(si => ({ value: si.id, label: si.name }))
            ]}
            required
          />
          {/* Order Types - Checkbox Group */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Order Types * 
              {customRateForm.orderTypeIds.length > 0 && (
                <span className="text-xs text-muted-foreground ml-2">
                  ({customRateForm.orderTypeIds.length} selected)
                </span>
              )}
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto border rounded-lg p-3 bg-muted/30">
              {orderTypes.length === 0 ? (
                <p className="text-xs text-muted-foreground">No order types available</p>
              ) : (
                orderTypes.map(ot => (
                  <label 
                    key={ot.id} 
                    className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={customRateForm.orderTypeIds.includes(ot.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setCustomRateForm({
                            ...customRateForm,
                            orderTypeIds: [...customRateForm.orderTypeIds, ot.id]
                          });
                        } else {
                          setCustomRateForm({
                            ...customRateForm,
                            orderTypeIds: customRateForm.orderTypeIds.filter(id => id !== ot.id)
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                    />
                    <span className="text-sm">{ot.name}</span>
                  </label>
                ))
              )}
            </div>
            {customRateForm.orderTypeIds.length === 0 && (
              <p className="text-xs text-destructive">At least one order type must be selected</p>
            )}
          </div>
          
          {/* Order Categories - Checkbox Group */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Order Categories * 
              {customRateForm.orderCategoryIds.length > 0 && (
                <span className="text-xs text-muted-foreground ml-2">
                  ({customRateForm.orderCategoryIds.length} selected)
                </span>
              )}
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto border rounded-lg p-3 bg-muted/30">
              {orderCategories.length === 0 ? (
                <p className="text-xs text-muted-foreground">No order categories available</p>
              ) : (
                orderCategories.map(cat => (
                  <label 
                    key={cat.id} 
                    className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={customRateForm.orderCategoryIds.includes(cat.id)}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setCustomRateForm({
                            ...customRateForm,
                            orderCategoryIds: [...customRateForm.orderCategoryIds, cat.id]
                          });
                        } else {
                          setCustomRateForm({
                            ...customRateForm,
                            orderCategoryIds: customRateForm.orderCategoryIds.filter(id => id !== cat.id)
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                    />
                    <span className="text-sm">{cat.name}</span>
                  </label>
                ))
              )}
            </div>
            {customRateForm.orderCategoryIds.length === 0 && (
              <p className="text-xs text-destructive">At least one category must be selected</p>
            )}
          </div>
          
          {/* Installation Methods - Checkbox Group */}
          <div className="space-y-2">
            <label className="text-sm font-medium">
              Installation Methods * 
              {customRateForm.installationMethodIds.length > 0 && (
                <span className="text-xs text-muted-foreground ml-2">
                  ({customRateForm.installationMethodIds.length} selected)
                </span>
              )}
            </label>
            <div className="space-y-2 max-h-48 overflow-y-auto border rounded-lg p-3 bg-muted/30">
              {installationMethods.length === 0 ? (
                <p className="text-xs text-muted-foreground">No installation methods available</p>
              ) : (
                <>
                  {/* Option for "All Methods" (null) */}
                  <label 
                    className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                  >
                    <input
                      type="checkbox"
                      checked={customRateForm.installationMethodIds.includes('')}
                      onChange={(e) => {
                        if (e.target.checked) {
                          // If "All Methods" is checked, clear other selections
                          setCustomRateForm({
                            ...customRateForm,
                            installationMethodIds: ['']
                          });
                        } else {
                          setCustomRateForm({
                            ...customRateForm,
                            installationMethodIds: customRateForm.installationMethodIds.filter(id => id !== '')
                          });
                        }
                      }}
                      className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                    />
                    <span className="text-sm font-medium">All Methods (No specific method)</span>
                  </label>
                  
                  {/* Specific installation methods */}
                  {installationMethods.map(im => (
                    <label 
                      key={im.id} 
                      className="flex items-center gap-2 cursor-pointer hover:bg-muted/50 p-2 rounded transition-colors"
                    >
                      <input
                        type="checkbox"
                        checked={customRateForm.installationMethodIds.includes(im.id)}
                        onChange={(e) => {
                          if (e.target.checked) {
                            // Remove "All Methods" if a specific method is selected
                            const filtered = customRateForm.installationMethodIds.filter(id => id !== '');
                            setCustomRateForm({
                              ...customRateForm,
                              installationMethodIds: [...filtered, im.id]
                            });
                          } else {
                            setCustomRateForm({
                              ...customRateForm,
                              installationMethodIds: customRateForm.installationMethodIds.filter(id => id !== im.id)
                            });
                          }
                        }}
                        className="h-4 w-4 rounded border-input text-primary focus:ring-primary cursor-pointer"
                      />
                      <span className="text-sm">{im.name}</span>
                    </label>
                  ))}
                </>
              )}
            </div>
            {customRateForm.installationMethodIds.length === 0 && (
              <p className="text-xs text-destructive">At least one method must be selected</p>
            )}
            {customRateForm.orderTypeIds.length > 0 && customRateForm.orderCategoryIds.length > 0 && customRateForm.installationMethodIds.length > 0 && (
              <p className="text-xs text-muted-foreground">
                {(() => {
                  const methodIds = customRateForm.installationMethodIds.filter(id => id !== '');
                  const hasAllMethods = customRateForm.installationMethodIds.includes('');
                  const methodCount = hasAllMethods && methodIds.length === 0 ? 1 : methodIds.length;
                  const totalCombinations = customRateForm.orderTypeIds.length * customRateForm.orderCategoryIds.length * methodCount;
                  return `Will create ${totalCombinations} rate record(s) for all combinations.`;
                })()}
              </p>
            )}
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <TextInput
              label="Custom Payout (RM) *"
              type="number"
              step="0.01"
              value={customRateForm.customPayoutAmount}
              onChange={(e) => setCustomRateForm({ ...customRateForm, customPayoutAmount: e.target.value })}
              placeholder="e.g., 55.00"
              required
            />
            <TextInput
              label="Valid From"
              type="date"
              value={customRateForm.validFrom}
              onChange={(e) => setCustomRateForm({ ...customRateForm, validFrom: e.target.value })}
            />
            <TextInput
              label="Valid To"
              type="date"
              value={customRateForm.validTo}
              onChange={(e) => setCustomRateForm({ ...customRateForm, validTo: e.target.value })}
            />
          </div>
          <TextInput
            label="Reason *"
            value={customRateForm.reason}
            onChange={(e) => setCustomRateForm({ ...customRateForm, reason: e.target.value })}
            placeholder="Why this SI gets a custom rate"
            required
          />
          <TextInput
            label="Notes"
            value={customRateForm.notes}
            onChange={(e) => setCustomRateForm({ ...customRateForm, notes: e.target.value })}
            placeholder="Optional notes"
          />
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="customRateActive"
              checked={customRateForm.isActive}
              onChange={(e) => setCustomRateForm({ ...customRateForm, isActive: e.target.checked })}
              className="h-4 w-4 rounded border-input"
            />
            <label htmlFor="customRateActive" className="text-sm">Active</label>
          </div>
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => { setShowCustomRateModal(false); setEditingCustomRate(null); resetCustomRateForm(); }}>
              Cancel
            </Button>
            <Button onClick={editingCustomRate ? handleUpdateCustomRate : handleCreateCustomRate}>
              <Save className="h-3 w-3 mr-1" />
              {editingCustomRate ? 'Update' : 'Create'}
            </Button>
          </div>
        </div>
      </Modal>
      
      {/* Rate Calculator Modal */}
      <Modal
        isOpen={showRateCalculator}
        onClose={() => { setShowRateCalculator(false); setCalcResult(null); }}
        title="Rate Calculator"
        size="lg"
      >
        <div className="space-y-4 p-4">
          <p className="text-sm text-muted-foreground">
            Test rate resolution for a given order configuration. This shows how the system will calculate revenue and payout.
          </p>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="Partner Group"
              value={calcRequest.partnerGroupId}
              onChange={(e) => setCalcRequest({ ...calcRequest, partnerGroupId: e.target.value })}
              options={[
                { value: '', label: 'Select Partner Group' },
                ...partnerGroups.map(pg => ({ value: pg.id, label: pg.name }))
              ]}
            />
            <Select
              label="SI Level"
              value={calcRequest.siLevel}
              onChange={(e) => setCalcRequest({ ...calcRequest, siLevel: e.target.value })}
              options={SI_LEVELS.map(l => ({ value: l, label: l }))}
            />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Select
              label="Order Type *"
              value={calcRequest.orderTypeId}
              onChange={(e) => setCalcRequest({ ...calcRequest, orderTypeId: e.target.value })}
              options={[
                { value: '', label: 'Select Order Type' },
                ...orderTypes.map(ot => ({ value: ot.id, label: ot.name }))
              ]}
              required
            />
            <Select
              label="Order Category *"
              value={calcRequest.orderCategoryId}
              onChange={(e) => setCalcRequest({ ...calcRequest, orderCategoryId: e.target.value })}
              options={[
                { value: '', label: 'Select Order Category' },
                ...orderCategories.map(oc => ({ value: oc.id, label: oc.name }))
              ]}
              required
            />
            <Select
              label="Installation Method"
              value={calcRequest.installationMethodId}
              onChange={(e) => setCalcRequest({ ...calcRequest, installationMethodId: e.target.value })}
              options={[
                { value: '', label: 'Any Method' },
                ...installationMethods.map(im => ({ value: im.id, label: im.name }))
              ]}
            />
          </div>
          
          <Button onClick={handleCalculateRate} className="w-full">
            <Calculator className="h-4 w-4 mr-2" />
            Calculate Rates
          </Button>
          
          {calcResult && (
            <div className="mt-4 p-4 bg-muted rounded-lg">
              <h4 className="font-medium mb-3">Resolution Result</h4>
              <div className="grid grid-cols-2 gap-4">
                <div className="bg-green-900/20 rounded p-3">
                  <p className="text-xs text-muted-foreground">Revenue (What Cephas Earns)</p>
                  <p className="text-lg font-bold text-green-400">RM {calcResult.revenueAmount.toFixed(2)}</p>
                  <p className="text-xs text-muted-foreground mt-1">Source: {calcResult.revenueRateSource || 'N/A'}</p>
                </div>
                <div className="bg-red-900/20 rounded p-3">
                  <p className="text-xs text-muted-foreground">Payout (What SI Earns)</p>
                  <p className="text-lg font-bold text-red-400">RM {calcResult.payoutAmount.toFixed(2)}</p>
                  <p className="text-xs text-muted-foreground mt-1">Source: {calcResult.payoutRateSource || 'N/A'}</p>
                </div>
              </div>
              <div className="mt-3 p-3 bg-blue-900/20 rounded">
                <p className="text-xs text-muted-foreground">Gross Margin</p>
                <p className="text-lg font-bold text-blue-400">
                  RM {calcResult.margin.toFixed(2)} ({calcResult.marginPercentage.toFixed(1)}%)
                </p>
              </div>
            </div>
          )}
          
          <div className="flex justify-end pt-4 border-t">
            <Button variant="outline" onClick={() => { setShowRateCalculator(false); setCalcResult(null); }}>
              Close
            </Button>
          </div>
        </div>
      </Modal>
      
      {/* Delete Confirmations */}
      <ConfirmDialog
        isOpen={deletingPartnerRate !== null}
        onClose={() => setDeletingPartnerRate(null)}
        onConfirm={handleDeletePartnerRate}
        title="Delete Partner Rate"
        message="Are you sure you want to delete this partner revenue rate? This action cannot be undone."
        confirmLabel="Delete"
        confirmVariant="danger"
      />
      
      <ConfirmDialog
        isOpen={deletingSiRate !== null}
        onClose={() => setDeletingSiRate(null)}
        onConfirm={handleDeleteSiRate}
        title="Delete SI Rate"
        message="Are you sure you want to delete this SI payout rate? This action cannot be undone."
        confirmLabel="Delete"
        confirmVariant="danger"
      />
      
      <ConfirmDialog
        isOpen={deletingCustomRate !== null}
        onClose={() => setDeletingCustomRate(null)}
        onConfirm={handleDeleteCustomRate}
        title="Delete Custom Rate"
        message="Are you sure you want to delete this custom SI rate override? This action cannot be undone."
        confirmLabel="Delete"
        confirmVariant="danger"
      />
      </div>
    </PageShell>
  );
};

export default RateEngineManagementPage;

