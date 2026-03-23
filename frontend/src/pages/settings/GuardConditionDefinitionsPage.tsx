import React, { useRef } from 'react';
import { Plus, RefreshCw, Shield } from 'lucide-react';
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
import { getGuardConditionDefinitions, updateGuardConditionDefinition, type GuardConditionDefinition } from '../../api/guardConditionDefinitions';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

/**
 * Guard Condition Definitions Page - Enhanced with Syncfusion Grid
 * 
 * Features:
 * - Inline editing
 * - Validator configuration
 * - Entity type filtering
 * - Excel export
 */

const GuardConditionDefinitionsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const gridRef = useRef<GridComponent>(null);
  const { activeDepartment, departments } = useDepartment();
  const queryClient = useQueryClient();
  
  // Multi-tenant: fallback to first department's companyId when activeDepartment has none
  const companyId = activeDepartment?.companyId 
    || departments[0]?.companyId 
    || '';
  
  const { data: definitions = [], isLoading, refetch } = useQuery({
    queryKey: ['guardConditionDefinitions', companyId],
    queryFn: () => getGuardConditionDefinitions(),
    enabled: !!companyId,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<GuardConditionDefinition> }) => 
      updateGuardConditionDefinition(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['guardConditionDefinitions'] });
      showSuccess('Guard condition definition updated successfully');
    },
    onError: (error: any) => {
      showError(error.message || 'Failed to update guard condition definition');
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
      gridRef.current.excelExport({ fileName: 'GuardConditionDefinitions.xlsx' });
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
            validatorType: args.data.validatorType,
            validatorConfigJson: args.data.validatorConfigJson,
            isActive: args.data.isActive,
            displayOrder: args.data.displayOrder
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

  if (!companyId) {
    return (
      <PageShell
        title="Guard Condition Definitions"
        subtitle="Configure validation rules for workflow transitions"
      >
        <div className="bg-card rounded-xl border border-border shadow-sm p-8 text-center">
          <p className="text-muted-foreground">Please select a department to view guard condition definitions.</p>
        </div>
      </PageShell>
    );
  }

  if (isLoading) {
    return <LoadingSpinner message="Loading guard condition definitions..." fullPage />;
  }

  return (
    <PageShell
      title="Guard Condition Definitions"
      subtitle="Define validation rules that must be met before workflow transitions"
      actions={
        <div className="flex gap-2">
          <Button size="sm" variant="outline" className="gap-2" onClick={handleRefresh}>
            <RefreshCw className="h-4 w-4" />
            Refresh
          </Button>
          <Button size="sm" className="gap-2">
            <Plus className="h-4 w-4" />
            Add Definition
          </Button>
        </div>
      }
    >
      <div className="bg-card rounded-xl border border-border shadow-sm p-4">
        <GridComponent
          ref={gridRef}
          dataSource={definitions}
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
            <ColumnDirective field="key" headerText="Key" width="150" validationRules={{ required: true }} />
            <ColumnDirective field="name" headerText="Name" width="200" validationRules={{ required: true }} />
            <ColumnDirective field="description" headerText="Description" width="250" />
            <ColumnDirective field="entityType" headerText="Entity Type" width="120" allowGrouping={true} />
            <ColumnDirective field="validatorType" headerText="Validator Type" width="180" />
            <ColumnDirective field="validatorConfigJson" headerText="Config" width="200" />
            <ColumnDirective field="displayOrder" headerText="Order" width="100" editType="numericedit" textAlign="Center" />
            <ColumnDirective field="isActive" headerText="Status" width="120" template={statusTemplate} allowEditing={false} />
          </ColumnsDirective>
          
          <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport, Edit]} />
        </GridComponent>
      </div>
    </PageShell>
  );
};

export default GuardConditionDefinitionsPage;

