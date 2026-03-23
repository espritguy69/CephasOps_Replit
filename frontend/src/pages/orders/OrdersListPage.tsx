import React, { useState, useEffect, useRef, useMemo } from 'react';
import { Plus, Upload, ChevronDown, Mail, FileUp, X, RefreshCw, Trash2, Download, ArrowUp, ArrowDown, ArrowUpDown, FileText, Search, Calendar } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { getOrdersPaged, updateOrder, generateOrderPdf, type GetOrdersPagedParams } from '../../api/orders';
import { getPartners } from '../../api/partners';
import { getServiceInstallers } from '../../api/serviceInstallers';
import { getApiBaseUrl } from '../../api/config';
import { exportOrdersToExcel } from '../../utils/excelExport';
import OrderFilters from '../../components/orders/OrderFilters';
import { LoadingSpinner, EmptyState, useToast, Button, Card, Modal, Select, Label, StatusBadge } from '../../components/ui';
import { Input } from '../../components/ui/input';
import { PageShell } from '../../components/layout';
import { cn } from '@/lib/utils';
import { useDepartment } from '../../contexts/DepartmentContext';
import { useAuth } from '../../contexts/AuthContext';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import type { Order, OrderFilters as OrderFiltersType, UploadResult } from '../../types/orders';
import type { Partner } from '../../types/partners';
import type { ServiceInstaller } from '../../types/serviceInstallers';
import { ORDER_STATUSES } from '../../types/scheduler';

// ============================================================================
// Types
// ============================================================================

interface ImportOrdersDropdownProps {
  onFromEmail: () => void;
  onUploadFile: () => void;
}

interface FileUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (result: UploadResult) => void;
}

// ============================================================================
// Status Badge Color Helper (uses centralized utility)
// Per SCHEDULER_MODULE.md: Grey=Pending, Blue=Assigned, Yellow=OnTheWay/MetCustomer, 
// Green=OrderCompleted, Red=Blocked/Overdue
// ============================================================================

import { getStatusBadgeColor as getStatusBadgeColorUtil, getPriorityBadgeVariant } from '../../utils/statusColors';

const getStatusBadgeColor = (status: string): string => {
  return getStatusBadgeColorUtil(status);
};

// ============================================================================
// Date Helper Functions
// ============================================================================

const parseAppointmentDate = (dateStr: string | null | undefined): Date | null => {
  if (!dateStr) return null;
  
  // Try ISO format first
  let date = new Date(dateStr);
  if (!isNaN(date.getTime())) return date;
  
  // Try DD/MM/YYYY format
  const parts = dateStr.split('/');
  if (parts.length === 3) {
    const [day, month, year] = parts;
    date = new Date(parseInt(year), parseInt(month) - 1, parseInt(day));
    if (!isNaN(date.getTime())) return date;
  }
  
  return null;
};

const formatDate = (dateStr: string | null | undefined): string => {
  const date = parseAppointmentDate(dateStr);
  if (!date) return '-';
  return date.toLocaleDateString('en-MY', { day: 'numeric', month: 'short', year: 'numeric' });
};

const formatTime = (timeStr: string | null | undefined): string => {
  if (!timeStr) return '-';
  // Normalize time format (e.g., "02:30 PM" -> "2:30 PM")
  return timeStr.replace(/^0(\d)/, '$1');
};

// ============================================================================
// Import Orders Dropdown Component
// ============================================================================

const ImportOrdersDropdown: React.FC<ImportOrdersDropdownProps> = ({ onFromEmail, onUploadFile }) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div className="relative" ref={dropdownRef}>
      <Button
        variant="outline"
        className="gap-2"
        onClick={() => setIsOpen(!isOpen)}
      >
        <Upload className="h-4 w-4" />
        Import Orders
        <ChevronDown className={cn("h-4 w-4 transition-transform", isOpen && "rotate-180")} />
      </Button>
      
      {isOpen && (
        <div className="absolute right-0 mt-2 w-64 bg-card border border-border rounded-lg shadow-lg z-50 overflow-hidden">
          <div className="py-1">
            <button
              onClick={() => {
                onFromEmail();
                setIsOpen(false);
              }}
              className="w-full flex items-center gap-3 px-4 py-3 text-sm text-foreground hover:bg-accent transition-colors text-left"
            >
              <div className="h-8 w-8 rounded-lg bg-blue-100 flex items-center justify-center">
                <Mail className="h-4 w-4 text-blue-600" />
              </div>
              <div>
                <p className="font-medium">From Email</p>
                <p className="text-xs text-muted-foreground">Review parsed orders from emails</p>
              </div>
            </button>
            <div className="border-t border-border" />
            <button
              onClick={() => {
                onUploadFile();
                setIsOpen(false);
              }}
              className="w-full flex items-center gap-3 px-4 py-3 text-sm text-foreground hover:bg-accent transition-colors text-left"
            >
              <div className="h-8 w-8 rounded-lg bg-emerald-100 flex items-center justify-center">
                <FileUp className="h-4 w-4 text-emerald-600" />
              </div>
              <div>
                <p className="font-medium">Upload File</p>
                <p className="text-xs text-muted-foreground">PDF, Excel (.xls, .xlsx), Outlook (.msg)</p>
              </div>
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

// ============================================================================
// File Upload Modal Component
// ============================================================================

const FileUploadModal: React.FC<FileUploadModalProps> = ({ isOpen, onClose, onSuccess }) => {
  const [files, setFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const allowedExtensions = ['.pdf', '.xls', '.xlsx', '.msg'];
  const maxFileSize = 10 * 1024 * 1024; // 10MB

  const handleDrag = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const validateFile = (file: File): string | null => {
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!allowedExtensions.includes(ext)) {
      return `File type not supported. Allowed: PDF, Excel, Outlook (.msg)`;
    }
    if (file.size > maxFileSize) {
      return `File exceeds 10MB limit`;
    }
    return null;
  };

  const handleFiles = (newFiles: FileList) => {
    const validFiles: File[] = [];
    const errors: string[] = [];

    Array.from(newFiles).forEach(file => {
      const validationError = validateFile(file);
      if (validationError) {
        errors.push(`${file.name}: ${validationError}`);
      } else {
        validFiles.push(file);
      }
    });

    if (errors.length > 0) {
      setError(errors.join('\n'));
    } else {
      setError(null);
    }

    setFiles(prev => [...prev, ...validFiles]);
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleFiles(e.dataTransfer.files);
    }
  };

  const handleBrowseClick = (e: React.MouseEvent<HTMLButtonElement>) => {
    e.preventDefault();
    e.stopPropagation();
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
      fileInputRef.current.click();
    }
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      handleFiles(e.target.files);
      e.target.value = '';
    }
  };

  const removeFile = (index: number) => {
    setFiles(prev => prev.filter((_, i) => i !== index));
  };

  const handleUpload = async () => {
    if (files.length === 0) return;

    setUploading(true);
    setError(null);

    try {
      const formData = new FormData();
      files.forEach(file => {
        formData.append('files', file);
      });

      const token = localStorage.getItem('authToken') || '';
      const response = await fetch(
        `${getApiBaseUrl()}/parser/upload`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`
          },
          body: formData
        }
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || errorData.errors?.join(', ') || 'Upload failed');
      }

      const result = await response.json() as UploadResult;
      onSuccess(result);
      onClose();
      setFiles([]);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to upload files';
      setError(errorMessage);
    } finally {
      setUploading(false);
    }
  };

  const getFileIcon = (fileName: string): string => {
    const ext = fileName.split('.').pop()?.toLowerCase();
    if (ext === 'pdf') return '📄';
    if (ext === 'xls' || ext === 'xlsx') return '📊';
    if (ext === 'msg') return '📧';
    return '📎';
  };

  if (!isOpen) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Upload Files for Parsing" size="lg">
      <div className="space-y-4">
        <div
          className={cn(
            "border-2 border-dashed rounded-xl p-8 text-center transition-colors",
            dragActive 
              ? "border-primary bg-primary/5" 
              : "border-border hover:border-muted-foreground/50"
          )}
          onDragEnter={handleDrag}
          onDragLeave={handleDrag}
          onDragOver={handleDrag}
          onDrop={handleDrop}
        >
          <input
            ref={fileInputRef}
            type="file"
            multiple
            accept=".pdf,.xls,.xlsx,.msg"
            onChange={handleFileInput}
            className="hidden"
          />
          <div className="flex flex-col items-center gap-3">
            <div className="h-14 w-14 rounded-full bg-primary/10 flex items-center justify-center">
              <Upload className="h-6 w-6 text-primary" />
            </div>
            <div>
              <p className="text-sm font-medium text-foreground">
                {dragActive ? 'Drop files here' : 'Drag & drop files here'}
              </p>
              <p className="text-xs text-muted-foreground mt-1">
                or use the button below to browse
              </p>
            </div>
            <p className="text-xs text-muted-foreground">
              Supported: PDF, Excel (.xls, .xlsx), Outlook (.msg) • Max 10MB per file
            </p>
            <button
              type="button"
              onClick={handleBrowseClick}
              className="mt-2 px-4 py-2 text-sm font-medium bg-primary text-primary-foreground rounded-md hover:bg-primary/90 transition-colors"
            >
              Browse Files
            </button>
          </div>
        </div>

        {files.length > 0 && (
          <div className="space-y-2">
            <p className="text-sm font-medium text-foreground">Selected files ({files.length})</p>
            <div className="max-h-40 overflow-y-auto space-y-2">
              {files.map((file, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-3 bg-muted/50 rounded-lg"
                >
                  <div className="flex items-center gap-3">
                    <span className="text-xl">{getFileIcon(file.name)}</span>
                    <div>
                      <p className="text-sm font-medium text-foreground truncate max-w-[300px]">
                        {file.name}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {(file.size / 1024).toFixed(1)} KB
                      </p>
                    </div>
                  </div>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      removeFile(index);
                    }}
                    className="p-1 rounded hover:bg-accent text-muted-foreground hover:text-foreground"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}

        {error && (
          <div className="p-3 bg-red-100 rounded-lg">
            <p className="text-sm text-red-700 whitespace-pre-line">{error}</p>
          </div>
        )}

        <div className="flex justify-end gap-2 pt-4 border-t border-border">
          <Button variant="outline" onClick={onClose} disabled={uploading}>
            Cancel
          </Button>
          <Button
            onClick={handleUpload}
            disabled={files.length === 0 || uploading}
            className="gap-2"
          >
            {uploading ? (
              <>
                <RefreshCw className="h-4 w-4 animate-spin" />
                Uploading...
              </>
            ) : (
              <>
                <Upload className="h-4 w-4" />
                Upload & Parse ({files.length})
              </>
            )}
          </Button>
        </div>
      </div>
    </Modal>
  );
};

// ============================================================================
// Main OrdersListPage Component
// ============================================================================

const OrdersListPage: React.FC = () => {
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();
  const { user } = useAuth();
  const { departmentId, loading: departmentLoading, error: departmentError } = useDepartment();
  
  const PAGE_SIZE = 50;

  // Data state
  const [orders, setOrders] = useState<Order[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [serviceInstallers, setServiceInstallers] = useState<ServiceInstaller[]>([]);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Filter state
  const [filters, setFilters] = useState<OrderFiltersType>({});
  const [searchQuery, setSearchQuery] = useState<string>('');
  const debouncedSearchQuery = useDebouncedValue(searchQuery, 300);
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [partnerFilter, setPartnerFilter] = useState<string>('');
  const [dateFilterMode, setDateFilterMode] = useState<'single' | 'range'>('single');
  const [dateFilter, setDateFilter] = useState<string>(() => {
    const today = new Date();
    return today.toISOString().split('T')[0];
  });
  const [startDate, setStartDate] = useState<string>('');
  const [endDate, setEndDate] = useState<string>('');
  
  // Sorting state
  const [sortColumn, setSortColumn] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  
  // Modal state
  const [showUploadModal, setShowUploadModal] = useState(false);
  const [selectedRows, setSelectedRows] = useState<Order[]>([]);

  const pageSize = PAGE_SIZE;

  // Load partners once for filter dropdown
  useEffect(() => {
    getPartners({ isActive: true }).then(setPartners).catch(() => setPartners([]));
  }, []);

  // Reset to page 1 when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearchQuery, statusFilter, partnerFilter, dateFilterMode, dateFilter, startDate, endDate]);

  // Load data when department, page, or filters change
  useEffect(() => {
    if (departmentLoading) return;
    loadData();
  }, [departmentId, departmentLoading, page, pageSize, debouncedSearchQuery, statusFilter, partnerFilter, dateFilterMode, dateFilter, startDate, endDate]);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(departmentError || null);

      let fromDate: string | undefined;
      let toDate: string | undefined;
      if (dateFilterMode === 'single' && dateFilter) {
        fromDate = dateFilter;
        toDate = dateFilter;
      } else if (dateFilterMode === 'range') {
        if (startDate) fromDate = startDate;
        if (endDate) toDate = endDate;
      }

      const params: GetOrdersPagedParams = {
        keyword: debouncedSearchQuery.trim() || undefined,
        page,
        pageSize,
        status: statusFilter === 'all' ? undefined : statusFilter,
        partnerId: partnerFilter || undefined,
        fromDate,
        toDate,
        ...(departmentId ? { departmentId } : {})
      };

      const [pagedResponse, siResponse] = await Promise.all([
        getOrdersPaged(params),
        getServiceInstallers({ isActive: true })
      ]);

      setOrders(pagedResponse.items);
      setTotalCount(pagedResponse.totalCount);
      setServiceInstallers(Array.isArray(siResponse) ? siResponse : []);
    } catch (err) {
      const error = err as { message?: string; status?: number };
      console.error('Error loading orders:', err);

      if (error.message?.includes('Network error')) {
        setError('Unable to connect to the server. Please check if the backend is running.');
        showError('Backend connection failed.');
      } else {
        setError(error.message || 'Failed to load orders');
        showError(error.message || 'Failed to load orders');
      }
      setOrders([]);
      setTotalCount(0);
    } finally {
      setLoading(false);
    }
  };

  // Get installer name by ID
  const getInstallerName = (siId: string | undefined): string => {
    if (!siId) return 'Unassigned';
    const installer = serviceInstallers.find(i => i.id === siId);
    return installer?.name || 'Unknown';
  };

  // Sort current page only (filtering is done server-side via paged API)
  const filteredOrders = useMemo(() => {
    const result = [...orders];
    if (!sortColumn) return result;
    result.sort((a, b) => {
      let aValue: any;
      let bValue: any;
      switch (sortColumn) {
        case 'serviceId':
          aValue = (a.serviceId || '').toLowerCase();
          bValue = (b.serviceId || '').toLowerCase();
          break;
        case 'ticketId':
          aValue = (a.ticketId || '').toLowerCase();
          bValue = (b.ticketId || '').toLowerCase();
          break;
        case 'status':
          aValue = a.status.toLowerCase();
          bValue = b.status.toLowerCase();
          break;
        case 'installer':
          aValue = getInstallerName(a.assignedSiId).toLowerCase();
          bValue = getInstallerName(b.assignedSiId).toLowerCase();
          break;
        case 'appointmentDate':
          aValue = a.appointmentDate ? new Date(a.appointmentDate).getTime() : 0;
          bValue = b.appointmentDate ? new Date(b.appointmentDate).getTime() : 0;
          break;
        default:
          return 0;
      }
      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });
    return result;
  }, [orders, sortColumn, sortDirection, serviceInstallers]);

  // Handle sort
  const handleSort = (column: string) => {
    if (sortColumn === column) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortColumn(column);
      setSortDirection('asc');
    }
  };

  // Handle status change
  const handleStatusChange = async (orderId: string, newStatus: string) => {
    try {
      await updateOrder(orderId, { status: newStatus });
      showSuccess('Status updated successfully');
      await loadData();
    } catch (err) {
      showError('Failed to update status');
      console.error('Error updating status:', err);
    }
  };

  // Export to Excel
  const handleExportToExcel = async () => {
    if (filteredOrders.length === 0) {
      showError('No orders to export');
      return;
    }

    try {
      // Prepare orders with formatted data
      const ordersForExport = filteredOrders.map(order => ({
        ...order,
        address: order.address || `${order.addressLine1 || ''} ${order.addressLine2 || ''}`.trim() || '-',
        appointmentTime: formatTime(order.appointmentTime)
      }));

      await exportOrdersToExcel(ordersForExport, getInstallerName);
      showSuccess(`Exported ${filteredOrders.length} orders`);
    } catch (error) {
      console.error('Export failed:', error);
      showError('Failed to export orders');
    }
  };

  // Handle Import from Email
  const handleFromEmail = () => {
    navigate('/orders/parser');
  };

  // Handle Upload File
  const handleUploadFile = () => {
    setShowUploadModal(true);
  };

  // Handle successful upload
  const handleUploadSuccess = (_result: UploadResult) => {
    navigate('/orders/parser');
  };

  // Handle download PDF
  const handleDownloadOrderPdf = async (order: Order) => {
    try {
      const blob = await generateOrderPdf(order.id);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `order-${order.serviceId || order.ticketId || order.id}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
      showSuccess('Order PDF downloaded successfully');
    } catch (err) {
      showError('Failed to download Order PDF');
      console.error('Error downloading order PDF:', err);
    }
  };

  // Check permissions (RBAC v2: orders.edit or legacy role)
  const userRoles = user?.roles ?? [];
  const userPermissions = user?.permissions ?? [];
  const isSuperAdmin = userRoles.includes('SuperAdmin');
  const isHeadOfDepartment = userRoles.includes('HeadOfDepartment');
  const canEditOrders = Boolean(
    isSuperAdmin ||
    userPermissions.includes('orders.edit') ||
    (userPermissions.length === 0 && (userRoles.includes('Admin') || isHeadOfDepartment))
  );
  const canManage = isSuperAdmin || isHeadOfDepartment;

  // Sort icon component
  const SortIcon: React.FC<{ column: string }> = ({ column }) => {
    if (sortColumn !== column) {
      return <ArrowUpDown className="h-3 w-3 ml-1" />;
    }
    return sortDirection === 'asc' 
      ? <ArrowUp className="h-3 w-3 ml-1" /> 
      : <ArrowDown className="h-3 w-3 ml-1" />;
  };

  return (
    <PageShell
      title="Orders"
      actions={
        <>
          <Button variant="outline" onClick={handleExportToExcel} disabled={filteredOrders.length === 0}>
            <Download className="h-4 w-4 mr-2" />
            Export to Excel
          </Button>
          {canEditOrders && (
            <>
              <ImportOrdersDropdown 
                onFromEmail={handleFromEmail}
                onUploadFile={handleUploadFile}
              />
              <Button 
                className="flex items-center gap-2"
                onClick={() => navigate('/orders/create')}
              >
                <Plus className="h-4 w-4" />
                Create Order
              </Button>
            </>
          )}
        </>
      }
      compact
    >
      <div data-testid="orders-page-root">
      {/* Filters Section */}
      <Card className="p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Search */}
          <div>
            <Label className="text-sm font-medium mb-2 block">Search</Label>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                type="text"
                placeholder="Search by Service ID, Customer, Phone..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9 pr-10"
              />
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  className="absolute right-2 top-1/2 -translate-y-1/2 p-1 hover:bg-gray-100 rounded-full"
                >
                  <X className="h-4 w-4 text-gray-500" />
                </button>
              )}
            </div>
          </div>

          {/* Status Filter */}
          <div>
            <Label className="text-sm font-medium mb-2 block">Status</Label>
            <Select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              options={[
                { value: 'all', label: 'All Statuses' },
                ...ORDER_STATUSES.map(s => ({ value: s.value, label: s.label }))
              ]}
            />
          </div>

          {/* Partner Filter (by GUID; display uses partner name) */}
          <div>
            <Label className="text-sm font-medium mb-2 block">Partner</Label>
            <Select
              value={partnerFilter}
              onChange={(e) => setPartnerFilter(e.target.value)}
              options={[
                { value: '', label: 'All Partners' },
                ...partners.map(p => ({ value: p.id, label: p.name + (p.code ? ` (${p.code})` : '') }))
              ]}
            />
          </div>

          {/* Date Filter */}
          <div className="lg:col-span-2">
            <div className="flex items-center justify-between mb-2">
              <Label className="text-sm font-medium">
                Appointment Date
                {dateFilterMode === 'single' && dateFilter && new Date(dateFilter).toDateString() === new Date().toDateString() && (
                  <span className="ml-2 text-xs text-blue-600 font-normal">(Today's orders)</span>
                )}
              </Label>
              <div className="flex gap-1">
                <Button
                  variant={dateFilterMode === 'single' ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => setDateFilterMode('single')}
                  className="text-xs h-7"
                >
                  Single Date
                </Button>
                <Button
                  variant={dateFilterMode === 'range' ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => setDateFilterMode('range')}
                  className="text-xs h-7"
                >
                  Date Range
                </Button>
              </div>
            </div>

            {dateFilterMode === 'single' ? (
              <>
                {/* Quick date shortcuts */}
                <div className="flex gap-2 mb-2">
                  <Button
                    variant={(() => {
                      if (!dateFilter) return 'outline';
                      const yesterday = new Date();
                      yesterday.setDate(yesterday.getDate() - 1);
                      return new Date(dateFilter).toDateString() === yesterday.toDateString() ? 'default' : 'outline';
                    })()}
                    size="sm"
                    onClick={() => {
                      const yesterday = new Date();
                      yesterday.setDate(yesterday.getDate() - 1);
                      setDateFilter(yesterday.toISOString().split('T')[0]);
                    }}
                    className="text-xs"
                  >
                    Yesterday
                  </Button>
                  <Button
                    variant={dateFilter && new Date(dateFilter).toDateString() === new Date().toDateString() ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => {
                      const today = new Date();
                      setDateFilter(today.toISOString().split('T')[0]);
                    }}
                    className="text-xs"
                  >
                    Today
                  </Button>
                  <Button
                    variant={(() => {
                      if (!dateFilter) return 'outline';
                      const tomorrow = new Date();
                      tomorrow.setDate(tomorrow.getDate() + 1);
                      return new Date(dateFilter).toDateString() === tomorrow.toDateString() ? 'default' : 'outline';
                    })()}
                    size="sm"
                    onClick={() => {
                      const tomorrow = new Date();
                      tomorrow.setDate(tomorrow.getDate() + 1);
                      setDateFilter(tomorrow.toISOString().split('T')[0]);
                    }}
                    className="text-xs"
                  >
                    Tomorrow
                  </Button>
                </div>
                <div className="flex gap-2">
                  <Input
                    type="date"
                    value={dateFilter}
                    onChange={(e) => setDateFilter(e.target.value)}
                  />
                  {dateFilter && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setDateFilter('')}
                      title="Clear date filter"
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </>
            ) : (
              <>
                {/* Quick range presets */}
                <div className="flex gap-2 mb-2 flex-wrap">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      const today = new Date();
                      const weekStart = new Date(today);
                      weekStart.setDate(today.getDate() - today.getDay());
                      const weekEnd = new Date(weekStart);
                      weekEnd.setDate(weekStart.getDate() + 6);
                      setStartDate(weekStart.toISOString().split('T')[0]);
                      setEndDate(weekEnd.toISOString().split('T')[0]);
                    }}
                    className="text-xs"
                  >
                    This Week
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      const today = new Date();
                      const last7Days = new Date(today);
                      last7Days.setDate(today.getDate() - 6);
                      setStartDate(last7Days.toISOString().split('T')[0]);
                      setEndDate(today.toISOString().split('T')[0]);
                    }}
                    className="text-xs"
                  >
                    Last 7 Days
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      const today = new Date();
                      const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
                      const monthEnd = new Date(today.getFullYear(), today.getMonth() + 1, 0);
                      setStartDate(monthStart.toISOString().split('T')[0]);
                      setEndDate(monthEnd.toISOString().split('T')[0]);
                    }}
                    className="text-xs"
                  >
                    This Month
                  </Button>
                </div>
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <label className="text-xs text-muted-foreground mb-1 block">Start Date</label>
                    <Input
                      type="date"
                      value={startDate}
                      onChange={(e) => setStartDate(e.target.value)}
                    />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground mb-1 block">End Date</label>
                    <Input
                      type="date"
                      value={endDate}
                      onChange={(e) => setEndDate(e.target.value)}
                    />
                  </div>
                </div>
                {(startDate || endDate) && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => {
                      setStartDate('');
                      setEndDate('');
                    }}
                    className="text-xs mt-2 w-full"
                  >
                    <X className="h-3 w-3 mr-1" />
                    Clear Date Range
                  </Button>
                )}
              </>
            )}
          </div>
        </div>
      </Card>

      {/* Stats and pagination */}
      <div className="mb-2 flex flex-wrap items-center justify-between gap-4 py-1 text-sm text-muted-foreground">
        <div className="flex items-center gap-4">
          <span>
            Showing{' '}
            <strong className="text-foreground">
              {totalCount === 0 ? 0 : (page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalCount)}
            </strong>{' '}
            of <strong className="text-foreground">{totalCount}</strong> orders
          </span>
          {statusFilter !== 'all' && (
            <span>
              Status: <strong className="text-foreground">{ORDER_STATUSES.find(s => s.value === statusFilter)?.label || statusFilter}</strong>
            </span>
          )}
        </div>
        {totalCount > pageSize && (
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1 || loading}
              onClick={() => setPage(p => Math.max(1, p - 1))}
            >
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page * pageSize >= totalCount || loading}
              onClick={() => setPage(p => p + 1)}
            >
              Next
            </Button>
          </div>
        )}
      </div>

      {/* Error Banner */}
      {error && (
        <div className="mb-2 rounded border border-red-200 bg-red-50 p-2 text-xs text-red-800">
          {error}
        </div>
      )}

      {/* Orders Table */}
      <Card>
        {loading && orders.length === 0 ? (
          <div className="p-8">
            <LoadingSpinner message="Loading orders..." />
          </div>
        ) : filteredOrders.length === 0 ? (
          <div className="p-8">
            <EmptyState
              title="No orders found"
              description={searchQuery || statusFilter !== 'all' || dateFilter ? "Try adjusting your filters" : "Create your first order to get started"}
            />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-muted/50">
                <tr>
                  <th className="px-3 py-2 text-left">
                    <button onClick={() => handleSort('serviceId')} className="flex items-center hover:text-primary">
                      Service ID <SortIcon column="serviceId" />
                    </button>
                  </th>
                  <th className="px-3 py-2 text-left">
                    <button onClick={() => handleSort('ticketId')} className="flex items-center hover:text-primary">
                      Ticket ID <SortIcon column="ticketId" />
                    </button>
                  </th>
                  <th className="px-3 py-2 text-left">Customer</th>
                  <th className="px-3 py-2 text-left">Building</th>
                  <th className="px-3 py-2 text-left">Order Type</th>
                  <th className="px-3 py-2 text-left">Partner–Category</th>
                  <th className="px-3 py-2 text-left">Inst. Type</th>
                  <th className="px-3 py-2 text-left">Inst. Method</th>
                  <th className="px-3 py-2 text-center">Priority</th>
                  <th className="px-3 py-2 text-left">
                    <button onClick={() => handleSort('status')} className="flex items-center hover:text-primary">
                      Status <SortIcon column="status" />
                    </button>
                  </th>
                  <th className="px-3 py-2 text-left">
                    <button onClick={() => handleSort('installer')} className="flex items-center hover:text-primary">
                      Installer <SortIcon column="installer" />
                    </button>
                  </th>
                  <th className="px-3 py-2 text-left">
                    <button onClick={() => handleSort('appointmentDate')} className="flex items-center hover:text-primary">
                      Appointment <SortIcon column="appointmentDate" />
                    </button>
                  </th>
                  <th className="px-3 py-2 text-center">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {filteredOrders.map((order) => (
                  <tr 
                    key={order.id} 
                    className="hover:bg-muted/30 cursor-pointer"
                    onClick={() => navigate(`/orders/${order.id}`)}
                  >
                    <td className="px-3 py-2 font-medium">
                      {order.serviceId || '-'}
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {order.ticketId || '-'}
                    </td>
                    <td className="px-3 py-2">
                      <div className="font-medium">{order.customerName || '-'}</div>
                      {order.customerPhone && (
                        <div className="text-xs text-muted-foreground">{order.customerPhone}</div>
                      )}
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {order.buildingName || '-'}
                    </td>
                    <td className="px-3 py-2">
                      {order.orderType || '-'}
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {order.derivedPartnerCategoryLabel || '-'}
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {order.orderCategoryCode || '-'}
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {order.installationMethodName || '-'}
                    </td>
                    <td className="px-3 py-2 text-center">
                      {order.priority && (
                        <StatusBadge variant={getPriorityBadgeVariant(order.priority)} size="sm">
                          {order.priority}
                        </StatusBadge>
                      )}
                    </td>
                    <td className="px-3 py-2" onClick={(e) => e.stopPropagation()}>
                      <select
                        value={order.status}
                        onChange={(e) => handleStatusChange(order.id, e.target.value)}
                        className={cn(
                          'px-2 py-1 rounded text-xs font-medium border cursor-pointer',
                          getStatusBadgeColor(order.status)
                        )}
                      >
                        {ORDER_STATUSES.map((status) => (
                          <option key={status.value} value={status.value}>
                            {status.label}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {getInstallerName(order.assignedSiId)}
                    </td>
                    <td className="px-3 py-2">
                      <div className="text-xs font-medium">{formatDate(order.appointmentDate)}</div>
                      <div className="text-xs text-muted-foreground">{formatTime(order.appointmentTime)}</div>
                    </td>
                    <td className="px-3 py-2 text-center" onClick={(e) => e.stopPropagation()}>
                      <div className="flex items-center justify-center gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            const params = new URLSearchParams({ orderId: order.id });
                            if (order.appointmentDate) params.set('date', order.appointmentDate.split('T')[0] || order.appointmentDate);
                            navigate(`/scheduler/timeline?${params.toString()}`);
                          }}
                          title="Schedule"
                          className="h-7 w-7 p-0"
                        >
                          <Calendar className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => navigate(`/orders/${order.id}/edit`)}
                          title="Edit"
                          className="h-7 w-7 p-0"
                        >
                          <FileText className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDownloadOrderPdf(order)}
                          title="Download PDF"
                          className="h-7 w-7 p-0"
                        >
                          <Download className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
      </div>

      {/* File Upload Modal */}
      <FileUploadModal
        isOpen={showUploadModal}
        onClose={() => setShowUploadModal(false)}
        onSuccess={handleUploadSuccess}
      />
    </PageShell>
  );
};

export default OrdersListPage;
