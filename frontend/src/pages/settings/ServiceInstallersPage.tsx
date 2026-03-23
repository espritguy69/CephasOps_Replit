import React, { useState, useEffect, useMemo } from 'react';
import { Plus, Edit, Trash2, Save, X, Power, Lightbulb, ChevronDown, ChevronUp, Search, Download } from 'lucide-react';
import { 
  exportServiceInstallers,
  importServiceInstallers,
  downloadServiceInstallersTemplate
} from '../../api/serviceInstallers';
import { getDepartments } from '../../api/departments';
import { getSkillsByCategory, getInstallerSkills, assignSkills, removeSkill } from '../../api/skills';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable, ImportExportButtons } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { sortData, useSortState } from '../../components/ui/SortableHeader';
import { exportServiceInstallersToExcel } from '../../utils/excelExport';
import { getBooleanStatusColor } from '../../utils/statusColors';
import {
  useServiceInstallers,
  useCreateServiceInstaller,
  useUpdateServiceInstaller,
  useDeleteServiceInstaller,
  useServiceInstallerContacts,
  useCreateServiceInstallerContact,
  useUpdateServiceInstallerContact,
  useDeleteServiceInstallerContact,
} from '../../hooks/useServiceInstallers';
import { useDepartment } from '../../contexts/DepartmentContext';
import type { ServiceInstaller, ServiceInstallerContact, CreateServiceInstallerRequest, UpdateServiceInstallerRequest, CreateServiceInstallerContactRequest, UpdateServiceInstallerContactRequest } from '../../types/serviceInstallers';
import type { Department } from '../../types/departments';

// ServiceInstaller and ServiceInstallerContact types now match backend, no need for extended interfaces

interface ServiceInstallerFormData {
  departmentId: string;
  name: string;
  employeeId: string;
  phone: string;
  email: string;
  siLevel: 'Junior' | 'Senior';
  installerType: 'InHouse' | 'Subcontractor';
  isSubcontractor: boolean; // Kept for backward compatibility
  isActive: boolean;
  availabilityStatus: string;
  hireDate: string;
  employmentStatus: string;
  contractorId: string;
  contractorCompany: string;
  contractStartDate: string;
  contractEndDate: string;
  icNumber: string;
  bankName: string;
  bankAccountNumber: string;
  address: string;
  emergencyContact: string;
  skillIds: string[];
}

interface ContactFormData {
  name: string;
  phone: string;
  email: string;
  contactType: string;
  isPrimary: boolean;
}

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
  sortable?: boolean;
  sortValue?: (row: T) => string;
}

const ServiceInstallersPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { departmentId: contextDepartmentId, loading: departmentLoading } = useDepartment();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [editingSI, setEditingSI] = useState<ServiceInstaller | null>(null);
  const [showGuide, setShowGuide] = useState<boolean>(false);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [sortConfig, handleSort] = useSortState({ key: 'name', direction: 'asc' });
  const [formData, setFormData] = useState<ServiceInstallerFormData>({
    departmentId: '',
    name: '',
    employeeId: '',
    phone: '',
    email: '',
    siLevel: 'Junior',
    installerType: 'InHouse',
    isSubcontractor: false,
    isActive: true,
    availabilityStatus: 'Available',
    hireDate: '',
    employmentStatus: '',
    contractorId: '',
    contractorCompany: '',
    contractStartDate: '',
    contractEndDate: '',
    icNumber: '',
    bankName: '',
    bankAccountNumber: '',
    address: '',
    emergencyContact: '',
    skillIds: []
  });
  const [activeTab, setActiveTab] = useState<'details' | 'contacts'>('details');
  const [newContact, setNewContact] = useState<ContactFormData>({
    name: '',
    phone: '',
    email: '',
    contactType: 'Backup',
    isPrimary: false
  });
  const [contactError, setContactError] = useState<string>('');
  const [levelFilter, setLevelFilter] = useState<string>('all');
  const [selectedSkillIds, setSelectedSkillIds] = useState<string[]>([]);
  const queryClient = useQueryClient();

  // Load skills for the skills selector
  const { data: skillsByCategory = {} } = useQuery({
    queryKey: ['skillsByCategory'],
    queryFn: () => getSkillsByCategory(true) // Only active skills
  });

  // Load installer skills when editing
  const { data: installerSkills = [] } = useQuery({
    queryKey: ['installerSkills', editingSI?.id],
    queryFn: () => editingSI ? getInstallerSkills(editingSI.id) : [],
    enabled: !!editingSI
  });

  // Use React Query hooks for data fetching
  const filters = useMemo(() => ({
    departmentId: contextDepartmentId || undefined,
    isActive: statusFilter === 'all' ? undefined : statusFilter === 'active',
    installerType: typeFilter === 'all' ? undefined : (typeFilter === 'inhouse' ? 'InHouse' : 'Subcontractor'),
    siLevel: levelFilter === 'all' ? undefined : (levelFilter === 'junior' ? 'Junior' : 'Senior'),
    skillIds: selectedSkillIds.length > 0 ? selectedSkillIds : undefined
  }), [contextDepartmentId, statusFilter, typeFilter, levelFilter, selectedSkillIds]);

  const {
    data: serviceInstallersData = [],
    isLoading: serviceInstallersLoading,
  } = useServiceInstallers(filters);

  const serviceInstallers = serviceInstallersData as ServiceInstaller[];

  // Load contacts for editing SI
  const {
    data: contactsData = [],
    isLoading: contactsLoading,
  } = useServiceInstallerContacts(editingSI?.id || '');

  const contacts = contactsData as ServiceInstallerContact[];

  // Mutation hooks
  const createMutation = useCreateServiceInstaller();
  const updateMutation = useUpdateServiceInstaller();
  const deleteMutation = useDeleteServiceInstaller();
  const createContactMutation = useCreateServiceInstallerContact();
  const updateContactMutation = useUpdateServiceInstallerContact();
  const deleteContactMutation = useDeleteServiceInstallerContact();

  const loading = serviceInstallersLoading || departmentLoading || contactsLoading;

  // Load departments (this doesn't need React Query as it's rarely used)
  useEffect(() => {
    const loadDepartments = async () => {
      try {
        const data = await getDepartments();
        setDepartments(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error('Error loading departments:', err);
      }
    };
    loadDepartments();
  }, []);

  // Filtered and sorted service installers
  const filteredServiceInstallers = useMemo(() => {
    let result = serviceInstallers;

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      result = result.filter(si =>
        si.name?.toLowerCase().includes(query) ||
        si.email?.toLowerCase().includes(query) ||
        si.phone?.toLowerCase().includes(query) ||
        si.employeeId?.toLowerCase().includes(query) ||
        si.departmentName?.toLowerCase().includes(query)
      );
    }

    // Status filter is handled by React Query filters, but we can still filter locally if needed
    // Apply type filter
    if (typeFilter !== 'all') {
      result = result.filter(si => {
        const installerType = si.installerType || (si.isSubcontractor ? 'Subcontractor' : 'InHouse');
        if (typeFilter === 'subcontractor') {
          return installerType === 'Subcontractor';
        } else if (typeFilter === 'inhouse') {
          return installerType === 'InHouse';
        }
        return true;
      });
    }

    // Apply sorting
    return sortData(result, sortConfig);
  }, [serviceInstallers, searchQuery, typeFilter, sortConfig]);

  const handleCreate = async (): Promise<void> => {
    try {
      const nameTrimmed = formData.name.trim();
      
      if (!nameTrimmed) {
        showError('Name is required');
        return;
      }

      // Validation: Email domain check for In-House installers
      if (formData.installerType === 'InHouse' && formData.email) {
        const email = formData.email.trim().toLowerCase();
        if (!email.endsWith('@cephas.com') && !email.endsWith('@cephas.com.my')) {
          showError('In-House installers must have an email address ending with @cephas.com or @cephas.com.my');
          return;
        }
      }

      // Validation: Conditional field requirements
      if (formData.installerType === 'InHouse' && !formData.employeeId?.trim()) {
        showError('Employee ID is required for In-House installers');
        return;
      }

      if (formData.installerType === 'Subcontractor' && !formData.contractorId?.trim()) {
        showError('Contractor ID is required for Subcontractor installers');
        return;
      }

      const serviceInstallerData: CreateServiceInstallerRequest = {
        name: nameTrimmed,
        email: formData.email?.trim() || undefined,
        phone: formData.phone?.trim() || undefined,
        isActive: formData.isActive ?? true,
        employeeId: formData.employeeId?.trim() || undefined,
        siLevel: formData.siLevel || 'Junior',
        installerType: formData.installerType || 'InHouse',
        isSubcontractor: formData.installerType === 'Subcontractor', // Sync for backward compatibility
        departmentId: formData.departmentId || contextDepartmentId || undefined,
        availabilityStatus: formData.availabilityStatus || 'Available',
        hireDate: formData.hireDate ? new Date(formData.hireDate).toISOString() : undefined,
        employmentStatus: formData.employmentStatus || undefined,
        contractorId: formData.contractorId?.trim() || undefined,
        contractorCompany: formData.contractorCompany?.trim() || undefined,
        contractStartDate: formData.contractStartDate ? new Date(formData.contractStartDate).toISOString() : undefined,
        contractEndDate: formData.contractEndDate ? new Date(formData.contractEndDate).toISOString() : undefined,
        icNumber: formData.icNumber?.trim() || undefined,
        bankName: formData.bankName?.trim() || undefined,
        bankAccountNumber: formData.bankAccountNumber?.trim() || undefined,
        address: formData.address?.trim() || undefined,
        emergencyContact: formData.emergencyContact?.trim() || undefined,
        skillIds: formData.skillIds.length > 0 ? formData.skillIds : undefined
      };
      
      await createMutation.mutateAsync(serviceInstallerData);
      setShowCreateModal(false);
      resetForm();
    } catch (err) {
      // Error is handled by mutation hook
    }
  };

  const handleUpdate = async (): Promise<void> => {
    if (!editingSI) return;
    
    try {
      const nameTrimmed = formData.name.trim();
      
      if (!nameTrimmed) {
        showError('Name is required');
        return;
      }

      // Validation: Email domain check for In-House installers
      if (formData.installerType === 'InHouse' && formData.email) {
        const email = formData.email.trim().toLowerCase();
        if (!email.endsWith('@cephas.com') && !email.endsWith('@cephas.com.my')) {
          showError('In-House installers must have an email address ending with @cephas.com or @cephas.com.my');
          return;
        }
      }

      // Validation: Conditional field requirements
      if (formData.installerType === 'InHouse' && !formData.employeeId?.trim()) {
        showError('Employee ID is required for In-House installers');
        return;
      }

      if (formData.installerType === 'Subcontractor' && !formData.contractorId?.trim()) {
        showError('Contractor ID is required for Subcontractor installers');
        return;
      }

      const serviceInstallerData: UpdateServiceInstallerRequest = {
        name: nameTrimmed,
        email: formData.email?.trim() || undefined,
        phone: formData.phone?.trim() || undefined,
        isActive: formData.isActive,
        employeeId: formData.employeeId?.trim() || undefined,
        siLevel: formData.siLevel,
        installerType: formData.installerType,
        isSubcontractor: formData.installerType === 'Subcontractor', // Sync for backward compatibility
        departmentId: formData.departmentId || undefined,
        availabilityStatus: formData.availabilityStatus || undefined,
        hireDate: formData.hireDate ? new Date(formData.hireDate).toISOString() : undefined,
        employmentStatus: formData.employmentStatus || undefined,
        contractorId: formData.contractorId?.trim() || undefined,
        contractorCompany: formData.contractorCompany?.trim() || undefined,
        contractStartDate: formData.contractStartDate ? new Date(formData.contractStartDate).toISOString() : undefined,
        contractEndDate: formData.contractEndDate ? new Date(formData.contractEndDate).toISOString() : undefined,
        icNumber: formData.icNumber?.trim() || undefined,
        bankName: formData.bankName?.trim() || undefined,
        bankAccountNumber: formData.bankAccountNumber?.trim() || undefined,
        address: formData.address?.trim() || undefined,
        emergencyContact: formData.emergencyContact?.trim() || undefined,
        skillIds: formData.skillIds
      };
      
      await updateMutation.mutateAsync({ id: editingSI.id, data: serviceInstallerData });
      setShowCreateModal(false);
      setEditingSI(null);
      resetForm();
    } catch (err) {
      // Error is handled by mutation hook
    }
  };

  const handleToggleStatus = async (si: ServiceInstaller): Promise<void> => {
    try {
      const installerType = si.installerType || (si.isSubcontractor ? 'Subcontractor' : 'InHouse');
      const updatedData: UpdateServiceInstallerRequest = {
        name: si.name,
        email: si.email || undefined,
        phone: si.phone || undefined,
        isActive: !si.isActive,
        employeeId: si.employeeId || undefined,
        siLevel: si.siLevel,
        installerType: installerType,
        isSubcontractor: installerType === 'Subcontractor', // Sync for backward compatibility
        departmentId: si.departmentId || undefined,
      };
      
      await updateMutation.mutateAsync({ id: si.id, data: updatedData });
    } catch (err) {
      // Error is handled by mutation hook
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    if (!window.confirm('Are you sure you want to delete this service installer?')) return;
    
    try {
      await deleteMutation.mutateAsync(id);
    } catch (err) {
      // Error is handled by mutation hook
    }
  };

  const handleExport = (): void => {
    exportServiceInstallersToExcel(filteredServiceInstallers);
    showSuccess('Service Installers exported successfully!');
  };

  const resetForm = (): void => {
    setFormData({
      departmentId: '',
      name: '',
      employeeId: '',
      phone: '',
      email: '',
      siLevel: 'Junior',
      installerType: 'InHouse',
      isSubcontractor: false,
      isActive: true,
      availabilityStatus: 'Available',
      hireDate: '',
      employmentStatus: '',
      contractorId: '',
      contractorCompany: '',
      contractStartDate: '',
      contractEndDate: '',
      icNumber: '',
      bankName: '',
      bankAccountNumber: '',
      address: '',
      emergencyContact: '',
      skillIds: []
    });
    setActiveTab('details');
    setNewContact({
      name: '',
      phone: '',
      email: '',
      contactType: 'Backup',
      isPrimary: false
    });
    setContactError('');
  };

  const openEditModal = (si: ServiceInstaller): void => {
    setEditingSI(si);
    // Determine installerType from si.installerType or fallback to isSubcontractor
    const installerType = si.installerType || (si.isSubcontractor ? 'Subcontractor' : 'InHouse');
    // Normalize siLevel - only Junior or Senior
    const siLevel = (si.siLevel === 'Junior' || si.siLevel === 'Senior') ? si.siLevel : 'Junior';
    
    setFormData({
      departmentId: si.departmentId || '',
      name: si.name,
      employeeId: si.employeeId || '',
      phone: si.phone || '',
      email: si.email || '',
      siLevel: siLevel,
      installerType: installerType,
      isSubcontractor: si.isSubcontractor || false,
      isActive: si.isActive,
      availabilityStatus: si.availabilityStatus || 'Available',
      hireDate: si.hireDate ? new Date(si.hireDate).toISOString().split('T')[0] : '',
      employmentStatus: si.employmentStatus || '',
      contractorId: si.contractorId || '',
      contractorCompany: si.contractorCompany || '',
      contractStartDate: si.contractStartDate ? new Date(si.contractStartDate).toISOString().split('T')[0] : '',
      contractEndDate: si.contractEndDate ? new Date(si.contractEndDate).toISOString().split('T')[0] : '',
      icNumber: si.icNumber || '',
      bankName: si.bankName || '',
      bankAccountNumber: si.bankAccountNumber || '',
      address: si.address || '',
      emergencyContact: si.emergencyContact || '',
      skillIds: si.skills?.map(s => s.skillId) || []
    });
    setActiveTab('details');
    setContactError('');
    setShowCreateModal(true);
  };

  const columns: TableColumn<ServiceInstaller>[] = [
    { 
      key: 'name', 
      label: 'Name',
      width: 'auto' // Flexible width
    },
    { 
      key: 'phone', 
      label: 'Mobile',
      width: '140px' // Fixed width for phone numbers
    },
    { 
      key: 'emergencyContact', 
      label: 'Emergency Contact',
      render: (value) => value ? (
        <span className="text-sm text-gray-700">{value as string}</span>
      ) : <span className="text-gray-400 text-xs">—</span>,
      width: '180px' // Fixed width
    },
    { 
      key: 'siLevel', 
      label: 'Level', 
      render: (value) => {
        const level = value === 'Junior' || value === 'Senior' ? value : 'Junior';
        const levelColors: Record<string, string> = {
          'Junior': 'bg-blue-100 text-blue-800 border-blue-300',
          'Senior': 'bg-purple-100 text-purple-800 border-purple-300',
        };
        const color = levelColors[level] || 'bg-gray-100 text-gray-800 border-gray-300';
        return (
          <span className={`px-2 py-1 rounded text-[9px] font-medium border ${color}`}>
            {level}
          </span>
        );
      },
      sortable: true,
      width: '100px'
    },
    { 
      key: 'installerType', 
      label: 'Type', 
      render: (value, row) => {
        // Normalize installerType: handle both string and number enum values
        // Backend may send: "InHouse" (string) or 0 (number) for InHouse
        // Backend may send: "Subcontractor" (string) or 1 (number) for Subcontractor
        let installerType: string;
        
        if (value === 'InHouse' || value === 0 || value === '0') {
          installerType = 'InHouse';
        } else if (value === 'Subcontractor' || value === 1 || value === '1') {
          installerType = 'Subcontractor';
        } else if (typeof value === 'string' && (value === 'InHouse' || value === 'Subcontractor')) {
          installerType = value;
        } else {
          // Fallback to isSubcontractor for backward compatibility
          installerType = row.isSubcontractor ? 'Subcontractor' : 'InHouse';
        }
        
        const typeColors: Record<string, string> = {
          'InHouse': 'bg-teal-100 text-teal-800 border-teal-300',
          'Subcontractor': 'bg-amber-100 text-amber-800 border-amber-300',
        };
        const color = typeColors[installerType] || 'bg-gray-100 text-gray-800 border-gray-300';
        return (
          <span className={`px-2 py-1 rounded text-[9px] font-medium border ${color}`}>
            {installerType === 'InHouse' ? 'In-House' : installerType === 'Subcontractor' ? 'Subcontractor' : '-'}
          </span>
        );
      },
      sortable: true,
      sortValue: (row) => {
        // Normalize installerType for sorting
        let installerType: string;
        const value = row.installerType;
        
        if (value === 'InHouse' || value === 0 || value === '0') {
          installerType = 'InHouse';
        } else if (value === 'Subcontractor' || value === 1 || value === '1') {
          installerType = 'Subcontractor';
        } else if (typeof value === 'string' && (value === 'InHouse' || value === 'Subcontractor')) {
          installerType = value;
        } else {
          installerType = row.isSubcontractor ? 'Subcontractor' : 'InHouse';
        }
        
        return installerType === 'InHouse' ? 'In-House' : 'Subcontractor';
      },
      width: '120px' // Fixed width for badge
    },
    {
      key: 'skills',
      label: 'Skills',
      render: (_value, row) => {
        const skills = row.skills || [];
        if (skills.length === 0) {
          return <span className="text-xs text-gray-400">—</span>;
        }
        return (
          <div className="flex flex-wrap gap-1">
            {skills.slice(0, 3).map((skill) => (
              <span
                key={skill.id}
                className="px-2 py-0.5 rounded text-[9px] bg-gray-100 text-gray-700 border border-gray-300"
                title={skill.skill?.name || 'Skill'}
              >
                {skill.skill?.name || 'Unknown'}
              </span>
            ))}
            {skills.length > 3 && (
              <span className="px-2 py-0.5 rounded text-[9px] bg-gray-100 text-gray-500 border border-gray-300">
                +{skills.length - 3}
              </span>
            )}
          </div>
        );
      },
      width: '200px'
    },
    { 
      key: 'isActive', 
      label: 'Status', 
      render: (value) => (
        <span className={`px-2 py-1 rounded text-[9px] font-medium border ${getBooleanStatusColor(value as boolean)}`}>
          {value ? 'Active' : 'Inactive'}
        </span>
      ),
      sortable: true,
      sortValue: (row) => row.isActive ? 'Active' : 'Inactive',
      width: '100px' // Fixed width for badge
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_value, row) => (
        <div className="flex items-center gap-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              openEditModal(row);
            }}
            title="Edit"
            className="p-1 rounded text-blue-600 hover:text-blue-700 hover:bg-muted transition-colors"
          >
            <Edit className="h-4 w-4" />
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation();
              handleDelete(row.id);
            }}
            title="Delete"
            className="p-1 rounded text-red-600 hover:text-red-700 hover:bg-muted transition-colors"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      ),
      width: '100px' // Fixed width for 2 icons
    }
  ];

  if (loading) {
    return (
      <PageShell title="Service Installers" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Service Installers' }]}>
        <LoadingSpinner message="Loading service installers..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Service Installers"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Service Installers' }]}
      actions={
        <div className="flex items-center gap-2">
          <ImportExportButtons
            entityName="Service Installers"
            onExport={() => exportServiceInstallers(filters)}
            onImport={async (file) => {
              const result = await importServiceInstallers(file);
              return result;
            }}
            onDownloadTemplate={downloadServiceInstallersTemplate}
          />
          <Button variant="outline" size="sm" onClick={handleExport} className="gap-1">
            <Download className="h-4 w-4" />
            Export Excel
          </Button>
          <Button size="sm" onClick={() => setShowCreateModal(true)}>
            + ADD
          </Button>
        </div>
      }
    >
      <div className="max-w-7xl mx-auto space-y-4">
      {/* How-To Guide */}
      <Card className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-blue-700/30">
        <button 
          onClick={() => setShowGuide(!showGuide)}
          className="w-full flex items-center justify-between px-4 py-3"
        >
          <div className="flex items-center gap-2">
            <Lightbulb className="h-4 w-4 text-blue-400" />
            <span className="font-medium text-white text-sm">How Service Installers Work</span>
          </div>
          {showGuide ? <ChevronUp className="h-4 w-4 text-slate-400" /> : <ChevronDown className="h-4 w-4 text-slate-400" />}
        </button>
        
        {showGuide && (
          <div className="px-4 pb-4">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-blue-500 rounded-full flex items-center justify-center text-xs">1</span>
                  Purpose
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• Manage field technicians</li>
                  <li>• Track SI performance</li>
                  <li>• Assign to orders</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-green-500 rounded-full flex items-center justify-center text-xs">2</span>
                  SI Levels
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• <strong>Junior</strong> - Basic installs</li>
                  <li>• <strong>Senior</strong> - Complex jobs</li>
                  <li>• <strong>Subcon</strong> - Contractors</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-purple-500 rounded-full flex items-center justify-center text-xs">3</span>
                  Departments
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• NWO - New Orders</li>
                  <li>• CWO - Current Orders</li>
                  <li>• GPON - Fibre Infra</li>
                </ul>
              </div>
              
              <div className="bg-slate-800/50 rounded p-3">
                <h4 className="text-xs font-medium text-white mb-2 flex items-center gap-1">
                  <span className="w-5 h-5 bg-orange-500 rounded-full flex items-center justify-center text-xs">4</span>
                  Contacts
                </h4>
                <ul className="text-xs text-slate-300 space-y-1">
                  <li>• Add backup contacts</li>
                  <li>• Emergency numbers</li>
                  <li>• Edit to add contacts</li>
                </ul>
              </div>
            </div>
          </div>
        )}
      </Card>

      {/* Filters */}
      <Card className="p-4">
        <div className="flex flex-wrap items-center gap-4">
          {/* Search */}
          <div className="flex-1 min-w-[200px]">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Search by name, email, phone, employee ID..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          {/* Status Filter */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Status:</span>
            <div className="flex gap-1">
              <button
                onClick={() => setStatusFilter('all')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'all'
                    ? 'bg-gray-600 text-white border-gray-700'
                    : 'bg-gray-100 text-gray-700 border-gray-300 hover:bg-gray-200'
                }`}
              >
                All
              </button>
              <button
                onClick={() => setStatusFilter('active')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'active'
                    ? 'bg-green-600 text-white border-green-700'
                    : 'bg-green-100 text-green-700 border-green-300 hover:bg-green-200'
                }`}
              >
                Active
              </button>
              <button
                onClick={() => setStatusFilter('inactive')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  statusFilter === 'inactive'
                    ? 'bg-gray-600 text-white border-gray-700'
                    : 'bg-gray-100 text-gray-600 border-gray-300 hover:bg-gray-200'
                }`}
              >
                Inactive
              </button>
            </div>
          </div>

          {/* Type Filter */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Type:</span>
            <div className="flex gap-1">
              <button
                onClick={() => setTypeFilter('all')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  typeFilter === 'all'
                    ? 'bg-gray-600 text-white border-gray-700'
                    : 'bg-gray-100 text-gray-700 border-gray-300 hover:bg-gray-200'
                }`}
              >
                All
              </button>
              <button
                onClick={() => setTypeFilter('inhouse')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  typeFilter === 'inhouse'
                    ? 'bg-teal-600 text-white border-teal-700'
                    : 'bg-teal-100 text-teal-700 border-teal-300 hover:bg-teal-200'
                }`}
              >
                In-House
              </button>
              <button
                onClick={() => setTypeFilter('subcontractor')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  typeFilter === 'subcontractor'
                    ? 'bg-amber-600 text-white border-amber-700'
                    : 'bg-amber-100 text-amber-700 border-amber-300 hover:bg-amber-200'
                }`}
              >
                Subcontractor
              </button>
            </div>
          </div>

          {/* Level Filter */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Level:</span>
            <div className="flex gap-1">
              <button
                onClick={() => setLevelFilter('all')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  levelFilter === 'all'
                    ? 'bg-gray-600 text-white border-gray-700'
                    : 'bg-gray-100 text-gray-700 border-gray-300 hover:bg-gray-200'
                }`}
              >
                All
              </button>
              <button
                onClick={() => setLevelFilter('junior')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  levelFilter === 'junior'
                    ? 'bg-blue-600 text-white border-blue-700'
                    : 'bg-blue-100 text-blue-700 border-blue-300 hover:bg-blue-200'
                }`}
              >
                Junior
              </button>
              <button
                onClick={() => setLevelFilter('senior')}
                className={`px-3 py-1.5 text-xs font-medium rounded border transition-colors ${
                  levelFilter === 'senior'
                    ? 'bg-purple-600 text-white border-purple-700'
                    : 'bg-purple-100 text-purple-700 border-purple-300 hover:bg-purple-200'
                }`}
              >
                Senior
              </button>
            </div>
          </div>

          {/* Skills Filter */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Skills:</span>
            <select
              value={selectedSkillIds.length > 0 ? selectedSkillIds[0] : ''}
              onChange={(e) => {
                if (e.target.value) {
                  setSelectedSkillIds([e.target.value]);
                } else {
                  setSelectedSkillIds([]);
                }
              }}
              className="px-3 py-1.5 text-xs rounded border border-gray-300 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500 min-w-[150px]"
            >
              <option value="">All Skills</option>
              {Object.entries(skillsByCategory).map(([category, skills]) => (
                <optgroup key={category} label={category}>
                  {skills.map((skill) => (
                    <option key={skill.id} value={skill.id}>
                      {skill.name}
                    </option>
                  ))}
                </optgroup>
              ))}
            </select>
            {selectedSkillIds.length > 0 && (
              <button
                onClick={() => setSelectedSkillIds([])}
                className="px-2 py-1 text-xs text-red-600 hover:text-red-700 hover:bg-red-50 rounded border border-red-200"
                title="Clear skill filter"
              >
                Clear
              </button>
            )}
          </div>
        </div>
      </Card>

      {/* Data Table */}
      <Card className="overflow-x-auto">
        {filteredServiceInstallers.length > 0 ? (
          <div className="min-w-full">
            <DataTable
              data={filteredServiceInstallers}
              columns={columns}
              sortable={true}
            />
          </div>
        ) : (
          <EmptyState
            title="No service installers found"
            message={searchQuery || statusFilter !== 'all' || typeFilter !== 'all'
              ? "No service installers match your search criteria."
              : "Add your first service installer to get started."}
          />
        )}
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showCreateModal || editingSI !== null}
        onClose={() => {
          setShowCreateModal(false);
          setEditingSI(null);
          resetForm();
        }}
        title={editingSI ? 'Edit Service Installer' : 'Create Service Installer'}
        size="large"
      >
        <div className="space-y-4">
            <div className="flex border-b mb-2">
              <button
                type="button"
                className={`px-4 py-2 text-sm ${activeTab === 'details' ? 'border-b-2 border-blue-500 font-semibold' : 'text-gray-500'}`}
                onClick={() => setActiveTab('details')}
              >
                Details
              </button>
              {editingSI && (
                <button
                  type="button"
                  className={`px-4 py-2 text-sm ${activeTab === 'contacts' ? 'border-b-2 border-blue-500 font-semibold' : 'text-gray-500'}`}
                  onClick={() => setActiveTab('contacts')}
                >
                  Contacts
                </button>
              )}
            </div>

            {activeTab === 'details' && (
            <>
            <TextInput
              label="Name *"
              name="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              required
            />

            <div className="space-y-1">
              <label className="text-sm font-medium">Department</label>
              <select
                value={formData.departmentId}
                onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
                className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
              >
                <option value="">No Department</option>
                {departments.map(dept => (
                  <option key={dept.id} value={dept.id}>{dept.name}</option>
                ))}
              </select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Employee ID"
                name="employeeId"
                value={formData.employeeId}
                onChange={(e) => setFormData({ ...formData, employeeId: e.target.value })}
              />
              <div className="space-y-1">
                <label className="text-sm font-medium">SI Level *</label>
                <select
                  name="siLevel"
                  value={formData.siLevel}
                  onChange={(e) => setFormData({ ...formData, siLevel: e.target.value as 'Junior' | 'Senior' })}
                  className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
                  required
                >
                  <option value="Junior">Junior</option>
                  <option value="Senior">Senior</option>
                </select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <TextInput
                label="Phone"
                name="phone"
                value={formData.phone}
                onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
              />
              <TextInput
                label="Email"
                name="email"
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1">
                <label className="text-sm font-medium">Installer Type *</label>
                <select
                  name="installerType"
                  value={formData.installerType}
                  onChange={(e) => {
                    const newType = e.target.value as 'InHouse' | 'Subcontractor';
                    setFormData({ 
                      ...formData, 
                      installerType: newType,
                      isSubcontractor: newType === 'Subcontractor', // Sync for backward compatibility
                      // Clear conditional fields when type changes
                      employeeId: newType === 'Subcontractor' ? '' : formData.employeeId,
                      contractorId: newType === 'InHouse' ? '' : formData.contractorId
                    });
                  }}
                  className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
                  required
                >
                  <option value="InHouse">In-House</option>
                  <option value="Subcontractor">Subcontractor</option>
                </select>
              </div>
              <div className="space-y-1">
                <label className="text-sm font-medium">Availability Status</label>
                <select
                  name="availabilityStatus"
                  value={formData.availabilityStatus}
                  onChange={(e) => setFormData({ ...formData, availabilityStatus: e.target.value })}
                  className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
                >
                  <option value="Available">Available</option>
                  <option value="Busy">Busy</option>
                  <option value="On Leave">On Leave</option>
                  <option value="Unavailable">Unavailable</option>
                </select>
              </div>
            </div>

            {/* Conditional fields based on installer type */}
            {formData.installerType === 'InHouse' && (
              <div className="grid grid-cols-2 gap-4">
                <TextInput
                  label="Employee ID *"
                  name="employeeId"
                  value={formData.employeeId}
                  onChange={(e) => setFormData({ ...formData, employeeId: e.target.value })}
                  required
                  placeholder="Required for In-House installers"
                />
                <div className="space-y-1">
                  <label className="text-sm font-medium">Hire Date</label>
                  <input
                    type="date"
                    name="hireDate"
                    value={formData.hireDate}
                    onChange={(e) => setFormData({ ...formData, hireDate: e.target.value })}
                    className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
                  />
                </div>
              </div>
            )}

            {formData.installerType === 'Subcontractor' && (
              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <TextInput
                    label="Contractor ID *"
                    name="contractorId"
                    value={formData.contractorId}
                    onChange={(e) => setFormData({ ...formData, contractorId: e.target.value })}
                    required
                    placeholder="Required for Subcontractors"
                  />
                  <TextInput
                    label="Contractor Company"
                    name="contractorCompany"
                    value={formData.contractorCompany}
                    onChange={(e) => setFormData({ ...formData, contractorCompany: e.target.value })}
                    placeholder="Company name"
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <label className="text-sm font-medium">Contract Start Date</label>
                    <input
                      type="date"
                      name="contractStartDate"
                      value={formData.contractStartDate}
                      onChange={(e) => setFormData({ ...formData, contractStartDate: e.target.value })}
                      className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="text-sm font-medium">Contract End Date</label>
                    <input
                      type="date"
                      name="contractEndDate"
                      value={formData.contractEndDate}
                      onChange={(e) => setFormData({ ...formData, contractEndDate: e.target.value })}
                      className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
                    />
                  </div>
                </div>
              </div>
            )}

            {formData.installerType === 'InHouse' && (
              <div className="space-y-1">
                <label className="text-sm font-medium">Employment Status</label>
                <select
                  name="employmentStatus"
                  value={formData.employmentStatus}
                  onChange={(e) => setFormData({ ...formData, employmentStatus: e.target.value })}
                  className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
                >
                  <option value="">Select status</option>
                  <option value="Full-Time">Full-Time</option>
                  <option value="Part-Time">Part-Time</option>
                  <option value="Contract">Contract</option>
                  <option value="Probation">Probation</option>
                </select>
              </div>
            )}

            {/* Email validation hint for In-House */}
            {formData.installerType === 'InHouse' && formData.email && (
              <div className="text-xs text-amber-600 bg-amber-50 p-2 rounded">
                In-House installers must have an email ending with @cephas.com or @cephas.com.my
              </div>
            )}

            <div className="flex items-center gap-4">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="h-4 w-4"
                />
                <span className="text-sm font-medium">Active</span>
              </label>
            </div>

            <div className="border-t pt-4 mt-4">
              <h3 className="text-sm font-semibold mb-3">Additional Information</h3>
              
              <TextInput
                label="IC Number"
                name="icNumber"
                value={formData.icNumber}
                onChange={(e) => setFormData({ ...formData, icNumber: e.target.value })}
                placeholder="e.g., 123456-78-9012"
              />

              <div className="grid grid-cols-2 gap-4">
                <TextInput
                  label="Bank Name"
                  name="bankName"
                  value={formData.bankName}
                  onChange={(e) => setFormData({ ...formData, bankName: e.target.value })}
                  placeholder="e.g., Maybank, CIMB"
                />
                <TextInput
                  label="Bank Account Number"
                  name="bankAccountNumber"
                  value={formData.bankAccountNumber}
                  onChange={(e) => setFormData({ ...formData, bankAccountNumber: e.target.value })}
                  placeholder="Account number"
                />
              </div>

              <TextInput
                label="Address"
                name="address"
                value={formData.address}
                onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                placeholder="Full address"
              />

              <TextInput
                label="Emergency Contact"
                name="emergencyContact"
                value={formData.emergencyContact}
                onChange={(e) => setFormData({ ...formData, emergencyContact: e.target.value })}
                placeholder="Name and phone number"
              />
            </div>

            {/* Skills Management */}
            <div className="border-t pt-4 mt-4">
              <h3 className="text-sm font-semibold mb-3">Skills</h3>
              {Object.keys(skillsByCategory).length === 0 ? (
                <div className="text-sm text-gray-500 p-3 bg-gray-50 rounded">
                  No skills available. Skills must be configured in the system first.
                </div>
              ) : (
                <div className="space-y-4 max-h-64 overflow-y-auto">
                  {Object.entries(skillsByCategory).map(([category, skills]) => (
                    <div key={category} className="space-y-2">
                      <h4 className="text-xs font-medium text-gray-700 uppercase tracking-wide">{category}</h4>
                      <div className="grid grid-cols-2 gap-2">
                        {skills.map((skill) => {
                          const isSelected = formData.skillIds.includes(skill.id);
                          return (
                            <label
                              key={skill.id}
                              className={`flex items-center gap-2 p-2 rounded border cursor-pointer transition-colors ${
                                isSelected
                                  ? 'bg-blue-50 border-blue-300'
                                  : 'bg-white border-gray-200 hover:bg-gray-50'
                              }`}
                            >
                              <input
                                type="checkbox"
                                checked={isSelected}
                                onChange={(e) => {
                                  if (e.target.checked) {
                                    setFormData({
                                      ...formData,
                                      skillIds: [...formData.skillIds, skill.id]
                                    });
                                  } else {
                                    setFormData({
                                      ...formData,
                                      skillIds: formData.skillIds.filter(id => id !== skill.id)
                                    });
                                  }
                                }}
                                className="h-4 w-4 text-blue-600 rounded"
                              />
                              <span className="text-sm text-gray-700">{skill.name}</span>
                            </label>
                          );
                        })}
                      </div>
                    </div>
                  ))}
                </div>
              )}
              {formData.skillIds.length > 0 && (
                <div className="mt-3 text-xs text-gray-600">
                  {formData.skillIds.length} skill{formData.skillIds.length !== 1 ? 's' : ''} selected
                </div>
              )}
            </div>
            </>
            )}

            {activeTab === 'contacts' && editingSI && (
              <div className="space-y-4">
                {contactError && (
                  <div className="text-sm text-red-600 p-2 bg-red-50 rounded">{contactError}</div>
                )}

                <div className="space-y-3 p-3 bg-gray-50 rounded">
                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="Name"
                      name="contactName"
                      value={newContact.name}
                      onChange={(e) => setNewContact({ ...newContact, name: e.target.value })}
                    />
                    <div className="space-y-1">
                      <label className="text-sm font-medium">Type</label>
                      <select
                        value={newContact.contactType}
                        onChange={(e) => setNewContact({ ...newContact, contactType: e.target.value })}
                        className="flex h-10 w-full rounded border border-input bg-background px-3 py-2 text-sm"
                      >
                        <option value="Backup">Backup</option>
                        <option value="Emergency">Emergency</option>
                      </select>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <TextInput
                      label="Phone"
                      name="contactPhone"
                      value={newContact.phone}
                      onChange={(e) => setNewContact({ ...newContact, phone: e.target.value })}
                    />
                    <TextInput
                      label="Email"
                      name="contactEmail"
                      type="email"
                      value={newContact.email}
                      onChange={(e) => setNewContact({ ...newContact, email: e.target.value })}
                    />
                  </div>

                  <label className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={newContact.isPrimary}
                      onChange={(e) => setNewContact({ ...newContact, isPrimary: e.target.checked })}
                      className="h-4 w-4"
                    />
                    Primary contact
                  </label>

                  <div className="flex justify-end">
                    <Button
                      size="sm"
                      onClick={async () => {
                        setContactError('');

                        const phone = (newContact.phone || '').trim();
                        const email = (newContact.email || '').trim().toLowerCase();

                        const duplicate = contacts.find(c =>
                          c.contactType === newContact.contactType &&
                          (
                            (phone && c.phone === phone) ||
                            (email && c.email && c.email.toLowerCase() === email)
                          )
                        );

                        if (duplicate) {
                          setContactError('A contact with the same type and contact details already exists.');
                          return;
                        }

                        if (!editingSI) {
                          setContactError('No service installer selected');
                          return;
                        }

                        try {
                          const contactRequest: CreateServiceInstallerContactRequest = {
                            name: newContact.name,
                            phone: phone || undefined,
                            email: email || undefined,
                            isPrimary: newContact.isPrimary,
                            contactType: newContact.contactType
                          };
                          
                          await createContactMutation.mutateAsync({ siId: editingSI.id, data: contactRequest });

                          setNewContact({
                            name: '',
                            phone: '',
                            email: '',
                            contactType: 'Backup',
                            isPrimary: false
                          });
                        } catch (err) {
                          setContactError((err as Error).message || 'Failed to add contact');
                        }
                      }}
                    >
                      Add Contact
                    </Button>
                  </div>
                </div>

                <div className="border-t pt-3 space-y-2">
                  <h4 className="text-sm font-medium">Existing Contacts</h4>
                  {contacts.length === 0 ? (
                    <div className="text-sm text-gray-500">No contacts added yet.</div>
                  ) : (
                    contacts.map(c => (
                      <div key={c.id} className="flex justify-between items-center p-2 bg-gray-50 rounded">
                        <div>
                          <div className="font-medium text-sm">
                            {c.name || '(No name)'}{' '}
                            {c.isPrimary && (
                              <span className="text-xs text-blue-600">(Primary)</span>
                            )}
                          </div>
                          <div className="text-xs text-gray-600">
                            {c.contactType || c.role || 'Contact'} · {c.phone || '-'} · {c.email || '-'}
                          </div>
                        </div>
                        <button
                          type="button"
                          className="text-red-600 hover:text-red-700 p-1"
                          onClick={async () => {
                            try {
                              await deleteContactMutation.mutateAsync(c.id);
                            } catch (err) {
                              setContactError((err as Error).message || 'Failed to delete contact');
                            }
                          }}
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    ))
                  )}
                </div>
              </div>
            )}

            <div className="flex justify-end gap-3 pt-4 border-t">
              <Button
                variant="outline"
                onClick={() => {
                  setShowCreateModal(false);
                  setEditingSI(null);
                  resetForm();
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={editingSI ? handleUpdate : handleCreate}
                className="flex items-center gap-2"
              >
                <Save className="h-4 w-4" />
                {editingSI ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
      </Modal>
      </div>
    </PageShell>
  );
};

export default ServiceInstallersPage;
