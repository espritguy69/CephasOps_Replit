import React, { useRef } from 'react';
import { Plus, RefreshCw, AlertTriangle, Play, Pause } from 'lucide-react';
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
import { getEscalationRules, updateEscalationRule, toggleEscalationRuleActive, type EscalationRule } from '../../api/escalationRules';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

/**
 * Escalation Rules Page - Enhanced with Syncfusion Grid
 * 
 * Features:
 * - Inline editing
 * - Trigger & Escalation configuration
 * - Escalation chain support
 * - Priority-based execution
 * - Excel export
 */

const EscalationRulesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const gridRef = useRef<GridComponent>(null);
  const { activeDepartment, departments } = useDepartment();
  const queryClient = useQueryClient();
  
  // Multi-tenant: fallback to first department's companyId when activeDepartment has none
  const companyId = activeDepartment?.companyId 
    || departments[0]?.companyId 
    || '';
  
  const { data: escalationRules = [], isLoading, refetch } = useQuery({
    queryKey: ['escalationRules', companyId],
    queryFn: () => getEscalationRules(),
    enabled: !!companyId,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<EscalationRule> }) => 
      updateEscalationRule(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['escalationRules'] });
      showSuccess('Escalation rule updated successfully');
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to update escalation rule');
    },
  });

  const toggleMutation = useMutation({
    mutationFn: (id: string) => toggleEscalationRuleActive(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['escalationRules'] });
      showSuccess('Escalation rule status toggled');
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to toggle escalation rule');
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
      gridRef.current.excelExport({ fileName: 'EscalationRules.xlsx' });
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
            entityType: args.data.entityType,
            triggerType: args.data.triggerType,
            triggerStatus: args.data.triggerStatus,
            triggerDelayMinutes: args.data.triggerDelayMinutes,
            escalationType: args.data.escalationType,
            targetRole: args.data.targetRole,
            priority: args.data.priority,
            isActive: args.data.isActive,
            stopOnMatch: args.data.stopOnMatch
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

  const handleToggleActive = async (id: string) => {
    await toggleMutation.mutateAsync(id);
  };

  // Status template with toggle button
  const statusTemplate = (props: any) => {
    const isActive = props.isActive;
    const ruleId = props.id;
    
    return (
      <div className="flex items-center gap-2">
        <span className={`px-2 py-1 rounded text-xs font-medium ${
          isActive 
            ? 'bg-emerald-100 text-emerald-700' 
            : 'bg-gray-100 text-gray-600'
        }`}>
          {isActive ? 'Active' : 'Inactive'}
        </span>
        <button
          onClick={() => handleToggleActive(ruleId)}
          className="p-1 hover:bg-gray-100 rounded"
          title={isActive ? 'Deactivate' : 'Activate'}
        >
          {isActive ? (
            <Pause className="h-3 w-3 text-gray-600" />
          ) : (
            <Play className="h-3 w-3 text-gray-600" />
          )}
        </button>
      </div>
    );
  };

  // Escalation type badge template
  const escalationTypeTemplate = (props: any) => {
    const escalationType = props.escalationType;
    const colors: Record<string, string> = {
      NotifyUser: 'bg-blue-100 text-blue-700',
      NotifyRole: 'bg-purple-100 text-purple-700',
      AssignToUser: 'bg-green-100 text-green-700',
      AssignToRole: 'bg-orange-100 text-orange-700',
      ChangeStatus: 'bg-red-100 text-red-700',
      CreateTask: 'bg-yellow-100 text-yellow-700',
    };
    
    return (
      <span className={`px-2 py-1 rounded text-xs font-medium ${
        colors[escalationType] || 'bg-gray-100 text-gray-700'
      }`}>
        {escalationType}
      </span>
    );
  };

  // Trigger/Escalation summary template
  const triggerEscalationTemplate = (props: any) => {
    return (
      <div className="text-xs">
        <div className="font-medium">{props.triggerType}</div>
        <div className="text-muted-foreground">→ {props.escalationType}</div>
      </div>
    );
  };

  if (!companyId) {
    return (
      <PageShell
        title="Escalation Rules"
        subtitle="Configure auto-escalation based on time, status, or conditions"
      >
        <div className="bg-card rounded-xl border border-border shadow-sm p-8 text-center">
          <p className="text-muted-foreground">Please select a department to view escalation rules.</p>
        </div>
      </PageShell>
    );
  }

  if (isLoading) {
    return <LoadingSpinner message="Loading escalation rules..." fullPage />;
  }

  return (
    <PageShell
      title="Escalation Rules"
      subtitle="Configure auto-escalation based on time, status, or conditions"
      actions={
        <div className="flex gap-2">
          <Button size="sm" variant="outline" className="gap-2" onClick={handleRefresh}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          <Button size="sm" className="gap-2">
            <Plus className="h-4 w-4" />
            Add Rule
          </Button>
        </div>
      }
    >
      <div className="bg-card rounded-xl border border-border shadow-sm p-4">
        <GridComponent
          ref={gridRef}
          dataSource={escalationRules}
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
            <ColumnDirective field="entityType" headerText="Entity" width="100" allowGrouping={true} />
            <ColumnDirective field="orderType" headerText="Order Type" width="120" />
            <ColumnDirective field="triggerType" headerText="Trigger" width="130" />
            <ColumnDirective field="triggerStatus" headerText="Trigger Status" width="130" />
            <ColumnDirective field="triggerDelayMinutes" headerText="Delay (min)" width="110" editType="numericedit" textAlign="Center" />
            <ColumnDirective field="escalationType" headerText="Escalation" width="150" template={escalationTypeTemplate} />
            <ColumnDirective field="targetRole" headerText="Target Role" width="130" />
            <ColumnDirective field="targetStatus" headerText="Target Status" width="130" />
            <ColumnDirective field="continueEscalation" headerText="Continue" width="110" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="priority" headerText="Priority" width="100" editType="numericedit" textAlign="Center" />
            <ColumnDirective field="stopOnMatch" headerText="Stop on Match" width="130" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="isActive" headerText="Status" width="140" template={statusTemplate} allowEditing={false} />
          </ColumnsDirective>
          
          <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport, Edit]} />
        </GridComponent>
      </div>
    </PageShell>
  );
};

export default EscalationRulesPage;

