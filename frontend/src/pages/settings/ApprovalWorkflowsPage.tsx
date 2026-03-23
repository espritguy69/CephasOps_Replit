import React, { useRef } from 'react';
import { Plus, RefreshCw, CheckCircle2, Users } from 'lucide-react';
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
import { getApprovalWorkflows, updateApprovalWorkflow, type ApprovalWorkflow } from '../../api/approvalWorkflows';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

/**
 * Approval Workflows Page - Enhanced with Syncfusion Grid
 * 
 * Features:
 * - Inline editing
 * - Multi-step approval configuration
 * - Workflow type management
 * - Excel export
 */

const ApprovalWorkflowsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const gridRef = useRef<GridComponent>(null);
  const { activeDepartment, departments } = useDepartment();
  const queryClient = useQueryClient();
  
  // Single-company mode: fallback to first department's companyId if activeDepartment doesn't have one
  const companyId = activeDepartment?.companyId 
    || departments[0]?.companyId 
    || '';
  
  const { data: workflows = [], isLoading, refetch } = useQuery({
    queryKey: ['approvalWorkflows', companyId],
    queryFn: () => getApprovalWorkflows(),
    enabled: !!companyId,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<ApprovalWorkflow> }) => 
      updateApprovalWorkflow(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['approvalWorkflows'] });
      showSuccess('Approval workflow updated successfully');
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to update approval workflow');
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
      gridRef.current.excelExport({ fileName: 'ApprovalWorkflows.xlsx' });
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
            workflowType: args.data.workflowType,
            requireAllSteps: args.data.requireAllSteps,
            allowParallelApproval: args.data.allowParallelApproval,
            timeoutMinutes: args.data.timeoutMinutes,
            isActive: args.data.isActive,
            isDefault: args.data.isDefault
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

  // Workflow type badge template
  const workflowTypeTemplate = (props: any) => {
    const workflowType = props.workflowType;
    const colors: Record<string, string> = {
      RescheduleApproval: 'bg-blue-100 text-blue-700',
      RmaApproval: 'bg-orange-100 text-orange-700',
      InvoiceApproval: 'bg-green-100 text-green-700',
      SplitterOverrideApproval: 'bg-purple-100 text-purple-700',
      Custom: 'bg-gray-100 text-gray-700',
    };
    
    return (
      <span className={`px-2 py-1 rounded text-xs font-medium ${
        colors[workflowType] || 'bg-gray-100 text-gray-700'
      }`}>
        {workflowType}
      </span>
    );
  };

  // Steps count template
  const stepsTemplate = (props: any) => {
    const steps = props.steps || [];
    return (
      <div className="flex items-center gap-2">
        <Users className="h-4 w-4 text-muted-foreground" />
        <span className="text-sm font-medium">{steps.length} step{steps.length !== 1 ? 's' : ''}</span>
      </div>
    );
  };

  if (!companyId) {
    return (
      <PageShell
        title="Approval Workflows"
        subtitle="Configure multi-step approval processes for critical actions"
      >
        <div className="bg-card rounded-xl border border-border shadow-sm p-8 text-center">
          <p className="text-muted-foreground">Please select a department to view approval workflows.</p>
        </div>
      </PageShell>
    );
  }

  if (isLoading) {
    return <LoadingSpinner message="Loading approval workflows..." fullPage />;
  }

  return (
    <PageShell
      title="Approval Workflows"
      subtitle="Configure multi-step approval processes for critical actions"
      actions={
        <div className="flex gap-2">
          <Button size="sm" variant="outline" className="gap-2" onClick={handleRefresh}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          <Button size="sm" className="gap-2">
            <Plus className="h-4 w-4" />
            Add Workflow
          </Button>
        </div>
      }
    >
      <div className="bg-card rounded-xl border border-border shadow-sm p-4">
        <GridComponent
          ref={gridRef}
          dataSource={workflows}
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
            <ColumnDirective field="workflowType" headerText="Type" width="180" template={workflowTypeTemplate} allowGrouping={true} />
            <ColumnDirective field="entityType" headerText="Entity" width="100" allowGrouping={true} />
            <ColumnDirective field="orderType" headerText="Order Type" width="120" />
            <ColumnDirective field="steps" headerText="Steps" width="100" template={stepsTemplate} allowEditing={false} />
            <ColumnDirective field="requireAllSteps" headerText="Require All" width="110" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="allowParallelApproval" headerText="Parallel" width="100" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="timeoutMinutes" headerText="Timeout (min)" width="120" editType="numericedit" textAlign="Center" />
            <ColumnDirective field="autoApproveOnTimeout" headerText="Auto Approve" width="130" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="escalationRole" headerText="Escalate To" width="130" />
            <ColumnDirective field="isDefault" headerText="Default" width="100" displayAsCheckBox={true} textAlign="Center" />
            <ColumnDirective field="isActive" headerText="Status" width="120" template={statusTemplate} allowEditing={false} />
          </ColumnsDirective>
          
          <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport, Edit]} />
        </GridComponent>
      </div>
    </PageShell>
  );
};

export default ApprovalWorkflowsPage;

