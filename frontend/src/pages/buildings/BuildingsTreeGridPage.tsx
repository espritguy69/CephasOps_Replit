import React, { useState, useEffect } from 'react';
import { 
  TreeGridComponent, 
  ColumnsDirective, 
  ColumnDirective, 
  Inject, 
  Page, 
  Sort, 
  Filter, 
  Toolbar,
  ExcelExport,
  Aggregate,
  AggregateColumnsDirective,
  AggregateColumnDirective,
  AggregateDirective,
  AggregatesDirective
} from '@syncfusion/ej2-react-treegrid';
import { LoadingSpinner, useToast, Button } from '../../components/ui';
import { PageShell } from '../../components/layout';
import { Building2, RefreshCw } from 'lucide-react';

/**
 * Buildings TreeGrid - Hierarchical View
 * 🔥 PROFESSIONAL FEATURE 🔥
 * 
 * Hierarchy: Building → Block → Floor → Unit
 * 
 * Features:
 * - Expand/collapse hierarchy
 * - Capacity tracking per floor
 * - Aggregates (total units, connected count)
 * - Excel export with hierarchy
 * - Search in tree
 * - Color-coded utilization
 */

interface BuildingHierarchy {
  id: string;
  name: string;
  type: string;
  totalUnits: number;
  connectedUnits: number;
  utilization: number;
  children?: BuildingHierarchy[];
}

const BuildingsTreeGridPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [loading, setLoading] = useState(false);
  const gridRef = React.useRef<TreeGridComponent>(null);

  // Sample hierarchical data (in production, fetch from API)
  const buildingsData: BuildingHierarchy[] = [
    {
      id: '1',
      name: 'Menara Time',
      type: 'MDU',
      totalUnits: 300,
      connectedUnits: 270,
      utilization: 90,
      children: [
        {
          id: '1-A',
          name: 'Block A',
          type: 'Block',
          totalUnits: 150,
          connectedUnits: 145,
          utilization: 97,
          children: [
            { id: '1-A-1', name: 'Floor 1', type: 'Floor', totalUnits: 12, connectedUnits: 12, utilization: 100 },
            { id: '1-A-2', name: 'Floor 2', type: 'Floor', totalUnits: 12, connectedUnits: 11, utilization: 92 },
            { id: '1-A-3', name: 'Floor 3', type: 'Floor', totalUnits: 12, connectedUnits: 10, utilization: 83 }
          ]
        },
        {
          id: '1-B',
          name: 'Block B',
          type: 'Block',
          totalUnits: 150,
          connectedUnits: 125,
          utilization: 83,
          children: [
            { id: '1-B-1', name: 'Floor 1', type: 'Floor', totalUnits: 12, connectedUnits: 10, utilization: 83 },
            { id: '1-B-2', name: 'Floor 2', type: 'Floor', totalUnits: 12, connectedUnits: 9, utilization: 75 }
          ]
        }
      ]
    },
    {
      id: '2',
      name: 'Kelana Mall Tower',
      type: 'MDU',
      totalUnits: 450,
      connectedUnits: 180,
      utilization: 40,
      children: [
        {
          id: '2-A',
          name: 'Block A',
          type: 'Block',
          totalUnits: 150,
          connectedUnits: 60,
          utilization: 40,
          children: [
            { id: '2-A-1', name: 'Floor 1', type: 'Floor', totalUnits: 12, connectedUnits: 5, utilization: 42 },
            { id: '2-A-2', name: 'Floor 2', type: 'Floor', totalUnits: 12, connectedUnits: 4, utilization: 33 }
          ]
        }
      ]
    }
  ];

  // Utilization bar template
  const utilizationTemplate = (props: any) => {
    const util = props.utilization || 0;
    let color = 'bg-emerald-500';
    if (util < 50) color = 'bg-red-500';
    else if (util < 80) color = 'bg-amber-500';

    return (
      <div className="flex items-center gap-2">
        <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden min-w-[80px]">
          <div className={`h-full ${color}`} style={{ width: `${util}%` }}></div>
        </div>
        <span className="text-xs font-medium whitespace-nowrap">{util}%</span>
      </div>
    );
  };

  // Connection status template
  const connectionTemplate = (props: any) => {
    return (
      <span className="text-sm">
        <strong>{props.connectedUnits}</strong> / {props.totalUnits}
      </span>
    );
  };

  const toolbarClick = (args: any) => {
    if (gridRef.current && args.item.id.includes('excelexport')) {
      gridRef.current.excelExport({
        fileName: 'Buildings_Hierarchy.xlsx',
        hierarchyExportMode: 'All'
      });
    }
  };

  return (
    <PageShell
      title="Buildings - Hierarchical View"
      subtitle="Building → Block → Floor → Unit hierarchy with capacity tracking"
      actions={
        <Button size="sm" variant="outline" className="gap-2">
          <RefreshCw className="h-4 w-4" />
          Refresh
        </Button>
      }
    >
      <div className="space-y-4">
        <div className="bg-card rounded-xl border border-border shadow-sm p-4">
          <TreeGridComponent
            ref={gridRef}
            dataSource={buildingsData}
            treeColumnIndex={1}
            childMapping="children"
            allowPaging={true}
            allowSorting={true}
            allowFiltering={true}
            allowExcelExport={true}
            toolbar={['ExcelExport', 'Search', 'ExpandAll', 'CollapseAll']}
            pageSettings={{ pageSize: 20 }}
            toolbarClick={toolbarClick}
          >
            <ColumnsDirective>
              <ColumnDirective field="id" headerText="ID" width="100" isPrimaryKey={true} visible={false} />
              <ColumnDirective field="name" headerText="Name" width="250" />
              <ColumnDirective field="type" headerText="Type" width="100" />
              <ColumnDirective 
                field="utilization" 
                headerText="Utilization" 
                width="180" 
                template={utilizationTemplate}
                textAlign="Left"
              />
              <ColumnDirective 
                field="connectedUnits" 
                headerText="Connected" 
                width="120" 
                template={connectionTemplate}
                textAlign="Center"
              />
              <ColumnDirective field="totalUnits" headerText="Total Units" width="120" textAlign="Center" />
            </ColumnsDirective>

            <AggregatesDirective>
              <AggregateDirective>
                <AggregateColumnsDirective>
                  <AggregateColumnDirective
                    field="totalUnits"
                    type="Sum"
                    footerTemplate={(props: any) => <span><strong>Total Units: {props.Sum}</strong></span>}
                  />
                  <AggregateColumnDirective
                    field="connectedUnits"
                    type="Sum"
                    footerTemplate={(props: any) => <span><strong>Connected: {props.Sum}</strong></span>}
                  />
                </AggregateColumnsDirective>
              </AggregateDirective>
            </AggregatesDirective>

            <Inject services={[Page, Sort, Filter, Toolbar, ExcelExport, Aggregate]} />
          </TreeGridComponent>
        </div>

        {/* Feature Guide */}
        <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 rounded-lg p-4">
          <h3 className="font-semibold text-sm mb-2">✨ TreeGrid Features</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-2 text-sm">
            <div>• <strong>Expand/Collapse</strong>: Click arrows to drill down</div>
            <div>• <strong>Export Hierarchy</strong>: Excel export keeps tree structure</div>
            <div>• <strong>Aggregates</strong>: See totals at bottom</div>
            <div>• <strong>Search</strong>: Find units across all buildings</div>
            <div>• <strong>Color-Coded</strong>: Utilization bars show capacity</div>
            <div>• <strong>Toolbar</strong>: Expand/Collapse All buttons</div>
          </div>
        </div>
      </div>
    </PageShell>
  );
};

export default BuildingsTreeGridPage;

