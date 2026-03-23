import React, { useRef, useState, useEffect } from 'react';
import { Plus, RefreshCw, Clock } from 'lucide-react';
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
import { LoadingSpinner, useToast, Button } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useDepartment } from '../../contexts/DepartmentContext';
import { getSlaProfiles, updateSlaProfile, type SlaProfile } from '../../api/slaProfiles';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

/**
 * SLA Configuration Page - Enhanced with Syncfusion Grid
 * 
 * Features:
 * - Inline editing
 * - Response & Resolution SLA tracking
 * - Escalation rules
 * - Excel export
 */

const SlaConfigurationPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const gridRef = useRef<GridComponent>(null);
  const { activeDepartment, departments } = useDepartment();
  const queryClient = useQueryClient();
  
  // Single-company mode: fallback to first department's companyId if activeDepartment doesn't have one
  const companyId = activeDepartment?.companyId 
    || departments[0]?.companyId 
    || '';
  
  const { data: slaProfiles = [], isLoading, refetch } = useQuery({
    queryKey: ['slaProfiles', companyId],
    queryFn: () => getSlaProfiles(),
    enabled: !!companyId,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<SlaProfile> }) => 
      updateSlaProfile(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['slaProfiles'] });
      showSuccess('SLA profile updated successfully');
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to update SLA profile');
    },
  });

  const editSettings = {
    allowEditing: true,
    allowAdding: true,
    allowDeleting: false,
    mode: 'Normal' as any
  };

  const toolbar = ['Add', 'Edit', 'Update', 'Cancel', 'ExcelExport', 'Search'];

  const toolbarClick = (args: any) => {
    if (gridRef.current && args.item.id.includes('excelexport')) {
      gridRef.current.excelExport({ fileName: 'SlaProfiles.xlsx' });
    }
  };

  const actionComplete = async (args: any) => {
    if (args.requestType === 'save' && args.data) {
      try {
        await updateMutation.mutateAsync({
          id: args.data.id,
          data: {
            name: args.data.name,
            description: args.data.description,
            orderType: args.data.orderType,
            responseSlaMinutes: args.data.responseSlaMinutes,
            resolutionSlaMinutes: args.data.resolutionSlaMinutes,
            escalationThresholdPercent: args.data.escalationThresholdPercent,
            notifyOnEscalation: args.data.notifyOnEscalation,
            notifyOnBreach: args.data.notifyOnBreach,
            isDefault: args.data.isDefault,
            isActive: args.data.isActive
          }
        });
      } catch (error) {
        if (gridRef.current) {
          gridRef.current.refresh();
        }
      }
    }
  };

  const handleRefresh = () => {
    refetch();
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

  // Format minutes to hours
  const formatMinutes = (minutes?: number) => {
    if (!minutes) return '-';
    if (minutes < 60) return `${minutes}m`;
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
  };

  const minutesTemplate = (props: any) => {
    return <span>{formatMinutes(props.responseSlaMinutes || props.resolutionSlaMinutes)}</span>;
  };

  if (!companyId) {
    return (
      <PageShell
        title="SLA Configuration"
        subtitle="Define response and resolution time SLAs"
      >
        <div className="bg-card rounded-xl border border-border shadow-sm p-8 text-center">
          <p className="text-muted-foreground">Please select a department to view SLA profiles.</p>
        </div>
      </PageShell>
    );
  }

  if (isLoading) {
    return <LoadingSpinner message="Loading SLA profiles..." fullPage />;
  }

  return (
    <PageShell
      title="SLA Configuration"
      subtitle="Manage Service Level Agreement rules for response and resolution times"
      actions={
        <div className="flex gap-2">
          <Button size="sm" variant="outline" className="gap-2" onClick={handleRefresh}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          <Button size="sm" className="gap-2">
            <Plus className="h-4 w-4" />
            Add SLA Profile
          </Button>
        </div>
      }
    >
      <div className="bg-card rounded-xl border border-border shadow-sm p-4">
        <GridComponent
          ref={gridRef}
          dataSource={slaProfiles}
          allowPaging={true}
          allowSorting={true}
          allowFiltering={true}
          allowGrouping={true}
          allowExcelExport={true}
          editSettings={editSettings}
          toolbar={toolbar}
          pageSettings={{ pageSize: 20, pageSizes: [10, 20, 50, 100] }}
          filterSettings={{ type: 'Menu' }}
          toolbarClick={toolbarClick}
          actionComplete={actionComplete}
          enableHover={true}
        >
          <ColumnsDirective>
            <ColumnDirective field="id" headerText="ID" width="100" isPrimaryKey={true} visible={false} />
            <ColumnDirective field="name" headerText="Name" width="200" validationRules={{ required: true }} />
            <ColumnDirective field="orderType" headerText="Order Type" width="120" allowGrouping={true} />
            <ColumnDirective field="isVipOnly" headerText="VIP Only" width="100" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="responseSlaMinutes" headerText="Response SLA" width="130" editType="numericedit" template={minutesTemplate} textAlign="Center" />
            <ColumnDirective field="responseSlaFromStatus" headerText="Response From" width="130" />
            <ColumnDirective field="responseSlaToStatus" headerText="Response To" width="130" />
            <ColumnDirective field="resolutionSlaMinutes" headerText="Resolution SLA" width="130" editType="numericedit" template={minutesTemplate} textAlign="Center" />
            <ColumnDirective field="resolutionSlaFromStatus" headerText="Resolution From" width="130" />
            <ColumnDirective field="resolutionSlaToStatus" headerText="Resolution To" width="130" />
            <ColumnDirective field="escalationThresholdPercent" headerText="Escalation %" width="120" editType="numericedit" textAlign="Center" />
            <ColumnDirective field="escalationRole" headerText="Escalate To" width="130" />
            <ColumnDirective field="notifyOnEscalation" headerText="Notify Escalation" width="140" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="notifyOnBreach" headerText="Notify Breach" width="130" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="isDefault" headerText="Default" width="100" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="isActive" headerText="Status" width="120" template={statusTemplate} allowEditing={false} />
          </ColumnsDirective>
          
          <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport, Edit]} />
        </GridComponent>
      </div>
    </PageShell>
  );
};

export default SlaConfigurationPage;

