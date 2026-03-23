import React, { useRef, useState } from 'react';
import { Plus, RefreshCw, Clock, Calendar, FileText } from 'lucide-react';
import { 
  GridComponent, 
  ColumnsDirective, 
  ColumnDirective, 
  Page, 
  Sort, 
  Filter, 
  Group, 
  Toolbar, 
  ExcelExport,
  Edit,
  Inject
} from '@syncfusion/ej2-react-grids';
import { LoadingSpinner, useToast, Button, Card, Modal, TextInput, Select, Label, Switch } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useDepartment } from '../../contexts/DepartmentContext';
import { 
  getBusinessHours, 
  createBusinessHours, 
  updateBusinessHours, 
  deleteBusinessHours,
  getPublicHolidays, 
  createPublicHoliday, 
  updatePublicHoliday, 
  deletePublicHoliday,
  createTemplateBusinessHours,
  type BusinessHours, 
  type PublicHoliday,
  type CreateBusinessHoursDto
} from '../../api/businessHours';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

/**
 * Business Hours & Holidays Page
 * 
 * Features:
 * - Business hours configuration per day
 * - Public holidays management
 * - Department-specific hours
 * - Template creation (8am-6pm Mon-Fri)
 * - 12-hour format support (8am, 8:00PM, etc.)
 * - Excel export
 */

interface BusinessHoursFormData {
  name: string;
  description?: string;
  departmentId?: string;
  timezone: string;
  mondayStart?: string;
  mondayEnd?: string;
  tuesdayStart?: string;
  tuesdayEnd?: string;
  wednesdayStart?: string;
  wednesdayEnd?: string;
  thursdayStart?: string;
  thursdayEnd?: string;
  fridayStart?: string;
  fridayEnd?: string;
  saturdayStart?: string;
  saturdayEnd?: string;
  sundayStart?: string;
  sundayEnd?: string;
  isDefault: boolean;
  isActive: boolean;
}

const BusinessHoursPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const businessHoursGridRef = useRef<GridComponent>(null);
  const holidaysGridRef = useRef<GridComponent>(null);
  const { activeDepartment, departments } = useDepartment();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState(0);
  const [showBusinessHoursModal, setShowBusinessHoursModal] = useState(false);
  const [editingBusinessHours, setEditingBusinessHours] = useState<BusinessHours | null>(null);
  const [formData, setFormData] = useState<BusinessHoursFormData>({
    name: '',
    description: '',
    departmentId: '',
    timezone: 'Asia/Kuala_Lumpur',
    isDefault: false,
    isActive: true
  });
  
  // Multi-tenant: fallback to first department's companyId when activeDepartment has none
  const companyId = activeDepartment?.companyId 
    || departments[0]?.companyId 
    || '';
  
  const { data: businessHours = [], isLoading: isLoadingHours, refetch: refetchHours } = useQuery({
    queryKey: ['businessHours', companyId],
    queryFn: () => getBusinessHours(),
    enabled: !!companyId,
  });

  const { data: holidays = [], isLoading: isLoadingHolidays, refetch: refetchHolidays } = useQuery({
    queryKey: ['publicHolidays', companyId],
    queryFn: () => getPublicHolidays({ year: new Date().getFullYear() }),
    enabled: !!companyId,
  });

  const createHoursMutation = useMutation({
    mutationFn: (data: CreateBusinessHoursDto) => createBusinessHours(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['businessHours'] });
      showSuccess('Business hours created successfully');
      setShowBusinessHoursModal(false);
      resetForm();
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to create business hours');
    },
  });

  const updateHoursMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<BusinessHours> }) => 
      updateBusinessHours(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['businessHours'] });
      showSuccess('Business hours updated successfully');
      setShowBusinessHoursModal(false);
      resetForm();
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to update business hours');
    },
  });

  const templateMutation = useMutation({
    mutationFn: (name?: string) => createTemplateBusinessHours(name, activeDepartment?.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['businessHours'] });
      showSuccess('Template business hours created successfully (8am-6pm Mon-Fri)');
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to create template business hours');
    },
  });

  const updateHolidayMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<PublicHoliday> }) => 
      updatePublicHoliday(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['publicHolidays'] });
      showSuccess('Public holiday updated successfully');
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to update public holiday');
    },
  });

  const resetForm = () => {
    setFormData({
      name: '',
      description: '',
      departmentId: activeDepartment?.id || '',
      timezone: 'Asia/Kuala_Lumpur',
      isDefault: false,
      isActive: true
    });
    setEditingBusinessHours(null);
  };

  const handleOpenCreateModal = () => {
    resetForm();
    setShowBusinessHoursModal(true);
  };

  const handleOpenEditModal = (businessHours: BusinessHours) => {
    setEditingBusinessHours(businessHours);
    setFormData({
      name: businessHours.name,
      description: businessHours.description || '',
      departmentId: businessHours.departmentId || '',
      timezone: businessHours.timezone,
      mondayStart: businessHours.mondayStart || '',
      mondayEnd: businessHours.mondayEnd || '',
      tuesdayStart: businessHours.tuesdayStart || '',
      tuesdayEnd: businessHours.tuesdayEnd || '',
      wednesdayStart: businessHours.wednesdayStart || '',
      wednesdayEnd: businessHours.wednesdayEnd || '',
      thursdayStart: businessHours.thursdayStart || '',
      thursdayEnd: businessHours.thursdayEnd || '',
      fridayStart: businessHours.fridayStart || '',
      fridayEnd: businessHours.fridayEnd || '',
      saturdayStart: businessHours.saturdayStart || '',
      saturdayEnd: businessHours.saturdayEnd || '',
      sundayStart: businessHours.sundayStart || '',
      sundayEnd: businessHours.sundayEnd || '',
      isDefault: businessHours.isDefault,
      isActive: businessHours.isActive
    });
    setShowBusinessHoursModal(true);
  };

  const handleCreateTemplate = async () => {
    if (!window.confirm('Create template business hours (8am-6pm Monday-Friday)?')) {
      return;
    }
    await templateMutation.mutateAsync('Standard Business Hours (8am-6pm)');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const submitData: CreateBusinessHoursDto = {
      name: formData.name,
      description: formData.description || undefined,
      departmentId: formData.departmentId || undefined,
      timezone: formData.timezone,
      mondayStart: formData.mondayStart || undefined,
      mondayEnd: formData.mondayEnd || undefined,
      tuesdayStart: formData.tuesdayStart || undefined,
      tuesdayEnd: formData.tuesdayEnd || undefined,
      wednesdayStart: formData.wednesdayStart || undefined,
      wednesdayEnd: formData.wednesdayEnd || undefined,
      thursdayStart: formData.thursdayStart || undefined,
      thursdayEnd: formData.thursdayEnd || undefined,
      fridayStart: formData.fridayStart || undefined,
      fridayEnd: formData.fridayEnd || undefined,
      saturdayStart: formData.saturdayStart || undefined,
      saturdayEnd: formData.saturdayEnd || undefined,
      sundayStart: formData.sundayStart || undefined,
      sundayEnd: formData.sundayEnd || undefined,
      isDefault: formData.isDefault,
      isActive: formData.isActive
    };

    if (editingBusinessHours) {
      await updateHoursMutation.mutateAsync({ id: editingBusinessHours.id, data: submitData });
    } else {
      await createHoursMutation.mutateAsync(submitData);
    }
  };

  const editSettings = {
    allowEditing: true,
    allowAdding: false, // Disable inline add, use modal instead
    allowDeleting: false,
    mode: 'Normal' as any
  };

  const toolbar = ['Edit', 'Update', 'Cancel', 'ExcelExport', 'Search'];

  const toolbarClick = (args: any, gridRef: React.RefObject<GridComponent>) => {
    if (gridRef.current && args.item.id.includes('excelexport')) {
      const fileName = activeTab === 0 ? 'BusinessHours.xlsx' : 'PublicHolidays.xlsx';
      gridRef.current.excelExport({ fileName });
    }
  };

  const actionCompleteHours = async (args: any) => {
    if (args.requestType === 'save' && args.data) {
      try {
        await updateHoursMutation.mutateAsync({
          id: args.data.id,
          data: {
            name: args.data.name,
            mondayStart: args.data.mondayStart,
            mondayEnd: args.data.mondayEnd,
            tuesdayStart: args.data.tuesdayStart,
            tuesdayEnd: args.data.tuesdayEnd,
            wednesdayStart: args.data.wednesdayStart,
            wednesdayEnd: args.data.wednesdayEnd,
            thursdayStart: args.data.thursdayStart,
            thursdayEnd: args.data.thursdayEnd,
            fridayStart: args.data.fridayStart,
            fridayEnd: args.data.fridayEnd,
            saturdayStart: args.data.saturdayStart,
            saturdayEnd: args.data.saturdayEnd,
            sundayStart: args.data.sundayStart,
            sundayEnd: args.data.sundayEnd,
            isActive: args.data.isActive,
            isDefault: args.data.isDefault
          }
        });
      } catch (error) {
        if (businessHoursGridRef.current) {
          businessHoursGridRef.current.refresh();
        }
      }
    }
  };

  const actionCompleteHolidays = async (args: any) => {
    if (args.requestType === 'save' && args.data) {
      try {
        await updateHolidayMutation.mutateAsync({
          id: args.data.id,
          data: {
            name: args.data.name,
            holidayDate: args.data.holidayDate,
            holidayType: args.data.holidayType,
            isRecurring: args.data.isRecurring,
            isActive: args.data.isActive
          }
        });
      } catch (error) {
        if (holidaysGridRef.current) {
          holidaysGridRef.current.refresh();
        }
      }
    }
  };

  const handleRefresh = () => {
    if (activeTab === 0) {
      refetchHours();
    } else {
      refetchHolidays();
    }
  };

  // Status template
  const statusTemplate = (props: any) => {
    const isActive = props.isActive;
    return (
      <span className={`px-2 py-1 rounded text-xs font-medium ${
        isActive 
          ? 'bg-emerald-100 text-emerald-700' 
          : 'bg-gray-100 text-gray-600'
      }`}>
        {isActive ? 'Active' : 'Inactive'}
      </span>
    );
  };

  // Day hours template
  const dayHoursTemplate = (props: any, day: string) => {
    const start = props[`${day}Start`];
    const end = props[`${day}End`];
    if (!start || !end) return <span className="text-muted-foreground text-sm">Closed</span>;
    return <span className="text-sm">{start} - {end}</span>;
  };

  if (!companyId) {
    return (
      <PageShell
        title="Business Hours & Holidays"
        subtitle="Configure operating hours and public holidays for SLA calculations"
      >
        <div className="bg-card rounded-xl border border-border shadow-sm p-8 text-center">
          <p className="text-muted-foreground">Please select a department to view business hours and holidays.</p>
        </div>
      </PageShell>
    );
  }

  if (isLoadingHours || isLoadingHolidays) {
    return <LoadingSpinner message="Loading business hours and holidays..." fullPage />;
  }

  const days = [
    { key: 'monday', label: 'Monday' },
    { key: 'tuesday', label: 'Tuesday' },
    { key: 'wednesday', label: 'Wednesday' },
    { key: 'thursday', label: 'Thursday' },
    { key: 'friday', label: 'Friday' },
    { key: 'saturday', label: 'Saturday' },
    { key: 'sunday', label: 'Sunday' }
  ];

  return (
    <PageShell
      title="Business Hours & Holidays"
      subtitle="Configure operating hours and public holidays for SLA calculations"
      actions={
        <div className="flex gap-2">
          <Button size="sm" variant="outline" className="gap-2" onClick={handleRefresh}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          {activeTab === 0 && (
            <>
              <Button 
                size="sm" 
                variant="outline" 
                className="gap-2"
                onClick={handleCreateTemplate}
                disabled={templateMutation.isPending}
              >
                <FileText className="h-4 w-4" />
                Create Template
              </Button>
              <Button size="sm" className="gap-2" onClick={handleOpenCreateModal}>
                <Plus className="h-4 w-4" />
                Add Business Hours
              </Button>
            </>
          )}
          {activeTab === 1 && (
            <Button size="sm" className="gap-2">
              <Plus className="h-4 w-4" />
              Add Holiday
            </Button>
          )}
        </div>
      }
    >
      <div className="space-y-6">
        {/* Tabs */}
        <div className="border-b">
          <div className="flex gap-4">
            <button
              onClick={() => setActiveTab(0)}
              className={`pb-2 px-1 border-b-2 font-medium text-sm transition-colors ${
                activeTab === 0
                  ? 'border-primary text-primary'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              }`}
            >
              <Clock className="h-4 w-4 inline mr-2" />
              Business Hours
            </button>
            <button
              onClick={() => setActiveTab(1)}
              className={`pb-2 px-1 border-b-2 font-medium text-sm transition-colors ${
                activeTab === 1
                  ? 'border-primary text-primary'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              }`}
            >
              <Calendar className="h-4 w-4 inline mr-2" />
              Public Holidays
            </button>
          </div>
        </div>

        {/* Business Hours Tab */}
        {activeTab === 0 && (
          <Card className="p-4">
            <GridComponent
              ref={businessHoursGridRef}
              dataSource={businessHours}
              allowPaging={true}
              allowSorting={true}
              allowFiltering={true}
              allowGrouping={true}
              allowExcelExport={true}
              editSettings={editSettings}
              toolbar={toolbar}
              pageSettings={{ pageSize: 20, pageSizes: [10, 20, 50, 100] }}
              filterSettings={{ type: 'Menu' }}
              toolbarClick={(args) => toolbarClick(args, businessHoursGridRef)}
              actionComplete={actionCompleteHours}
              enableHover={true}
            >
              <ColumnsDirective>
                <ColumnDirective field="id" headerText="ID" width="100" isPrimaryKey={true} visible={false} />
                <ColumnDirective field="name" headerText="Name" width="200" validationRules={{ required: true }} />
                <ColumnDirective field="timezone" headerText="Timezone" width="150" />
                <ColumnDirective field="mondayStart" headerText="Mon Start" width="100" />
                <ColumnDirective field="mondayEnd" headerText="Mon End" width="100" />
                <ColumnDirective field="tuesdayStart" headerText="Tue Start" width="100" />
                <ColumnDirective field="tuesdayEnd" headerText="Tue End" width="100" />
                <ColumnDirective field="wednesdayStart" headerText="Wed Start" width="100" />
                <ColumnDirective field="wednesdayEnd" headerText="Wed End" width="100" />
                <ColumnDirective field="thursdayStart" headerText="Thu Start" width="100" />
                <ColumnDirective field="thursdayEnd" headerText="Thu End" width="100" />
                <ColumnDirective field="fridayStart" headerText="Fri Start" width="100" />
                <ColumnDirective field="fridayEnd" headerText="Fri End" width="100" />
                <ColumnDirective field="saturdayStart" headerText="Sat Start" width="100" />
                <ColumnDirective field="saturdayEnd" headerText="Sat End" width="100" />
                <ColumnDirective field="sundayStart" headerText="Sun Start" width="100" />
                <ColumnDirective field="sundayEnd" headerText="Sun End" width="100" />
                <ColumnDirective field="isDefault" headerText="Default" width="100" displayAsCheckBox={true} textAlign="Center" />
                <ColumnDirective field="isActive" headerText="Status" width="120" template={statusTemplate} allowEditing={false} />
              </ColumnsDirective>
              
              <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport, Edit]} />
            </GridComponent>
          </Card>
        )}

        {/* Public Holidays Tab */}
        {activeTab === 1 && (
          <Card className="p-4">
            <GridComponent
              ref={holidaysGridRef}
              dataSource={holidays}
              allowPaging={true}
              allowSorting={true}
              allowFiltering={true}
              allowGrouping={true}
              allowExcelExport={true}
              editSettings={editSettings}
              toolbar={toolbar}
              pageSettings={{ pageSize: 20, pageSizes: [10, 20, 50, 100] }}
              filterSettings={{ type: 'Menu' }}
              toolbarClick={(args) => toolbarClick(args, holidaysGridRef)}
              actionComplete={actionCompleteHolidays}
              enableHover={true}
            >
              <ColumnsDirective>
                <ColumnDirective field="id" headerText="ID" width="100" isPrimaryKey={true} visible={false} />
                <ColumnDirective field="name" headerText="Name" width="200" validationRules={{ required: true }} />
                <ColumnDirective field="holidayDate" headerText="Date" width="120" type="date" format="dd/MM/yyyy" />
                <ColumnDirective field="holidayType" headerText="Type" width="120" allowGrouping={true} />
                <ColumnDirective field="state" headerText="State" width="120" />
                <ColumnDirective field="isRecurring" headerText="Recurring" width="110" displayAsCheckBox={true} textAlign="Center" />
                <ColumnDirective field="isActive" headerText="Status" width="120" template={statusTemplate} allowEditing={false} />
              </ColumnsDirective>
              
              <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport, Edit]} />
            </GridComponent>
          </Card>
        )}
      </div>

      {/* Business Hours Create/Edit Modal */}
      <Modal 
        isOpen={showBusinessHoursModal} 
        onClose={() => {
          setShowBusinessHoursModal(false);
          resetForm();
        }}
        title={editingBusinessHours ? 'Edit Business Hours' : 'Create Business Hours'}
      >
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-4 max-h-[70vh] overflow-y-auto pr-2">
            <TextInput
              label="Name"
              name="name"
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="e.g., Standard Business Hours"
              required
            />

            <TextInput
              label="Description"
              name="description"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              placeholder="Optional description"
            />

            <Select
              label="Timezone"
              name="timezone"
              value={formData.timezone}
              onChange={(e) => setFormData({ ...formData, timezone: e.target.value })}
              options={[
                { value: 'Asia/Kuala_Lumpur', label: 'Asia/Kuala_Lumpur (GMT+8)' },
                { value: 'UTC', label: 'UTC (GMT+0)' },
                { value: 'America/New_York', label: 'America/New_York (GMT-5)' },
                { value: 'Europe/London', label: 'Europe/London (GMT+0)' }
              ]}
            />

            {/* Day-specific hours */}
            <div className="space-y-4 border-t pt-4">
              <Label className="text-base font-semibold">Operating Hours</Label>
              <p className="text-sm text-muted-foreground mb-4">
                Enter times in 12-hour format (8am, 8:00PM) or 24-hour format (08:00, 20:00)
              </p>
              
              {days.map((day) => {
                const startKey = `${day.key}Start` as keyof BusinessHoursFormData;
                const endKey = `${day.key}End` as keyof BusinessHoursFormData;
                return (
                  <div key={day.key} className="grid grid-cols-3 gap-3 items-end">
                    <Label className="col-span-3 text-sm font-medium">{day.label}</Label>
                    <TextInput
                      label="Start Time"
                      name={startKey}
                      value={(formData[startKey] as string) || ''}
                      onChange={(e) => setFormData({ ...formData, [startKey]: e.target.value })}
                      placeholder="8:00 AM or 08:00"
                    />
                    <TextInput
                      label="End Time"
                      name={endKey}
                      value={(formData[endKey] as string) || ''}
                      onChange={(e) => setFormData({ ...formData, [endKey]: e.target.value })}
                      placeholder="6:00 PM or 18:00"
                    />
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        setFormData({
                          ...formData,
                          [startKey]: '',
                          [endKey]: ''
                        });
                      }}
                    >
                      Clear
                    </Button>
                  </div>
                );
              })}
            </div>

            <div className="flex items-center gap-6 pt-4 border-t">
              <div className="flex items-center gap-2">
                <Switch
                  checked={formData.isDefault}
                  onCheckedChange={(checked) => setFormData({ ...formData, isDefault: checked })}
                />
                <Label>Set as Default</Label>
              </div>
              <div className="flex items-center gap-2">
                <Switch
                  checked={formData.isActive}
                  onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked })}
                />
                <Label>Active</Label>
              </div>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setShowBusinessHoursModal(false);
                resetForm();
              }}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={createHoursMutation.isPending || updateHoursMutation.isPending}
            >
              {editingBusinessHours ? 'Update' : 'Create'}
            </Button>
          </div>
        </form>
      </Modal>
    </PageShell>
  );
};

export default BusinessHoursPage;
