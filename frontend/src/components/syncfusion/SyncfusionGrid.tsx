import React, { useRef } from 'react';
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
  PdfExport,
  Edit,
  CommandColumn,
  Inject,
  FilterSettingsModel,
  PageSettingsModel,
  EditSettingsModel
} from '@syncfusion/ej2-react-grids';

interface GridColumn {
  field: string;
  headerText: string;
  width?: number;
  type?: 'string' | 'number' | 'boolean' | 'date' | 'datetime';
  format?: string;
  template?: (props: any) => JSX.Element;
  allowEditing?: boolean;
  allowFiltering?: boolean;
  allowSorting?: boolean;
  allowGrouping?: boolean;
}

interface SyncfusionGridProps {
  dataSource: any[];
  columns: GridColumn[];
  allowPaging?: boolean;
  allowSorting?: boolean;
  allowFiltering?: boolean;
  allowGrouping?: boolean;
  allowExcelExport?: boolean;
  allowPdfExport?: boolean;
  allowEditing?: boolean;
  pageSize?: number;
  height?: string;
  toolbar?: string[];
  filterSettings?: FilterSettingsModel;
  editSettings?: EditSettingsModel;
  onRowSelected?: (args: any) => void;
  onActionComplete?: (args: any) => void;
  onActionFailure?: (args: any) => void;
}

/**
 * Syncfusion Grid Wrapper Component
 * Provides Excel-like data grid with advanced features:
 * - Grouping (drag column headers)
 * - Filtering (per column)
 * - Sorting (multi-column)
 * - Inline editing
 * - Excel/PDF export
 * - Context menu
 * - Frozen columns
 * - Aggregates
 */
const SyncfusionGrid: React.FC<SyncfusionGridProps> = ({
  dataSource,
  columns,
  allowPaging = true,
  allowSorting = true,
  allowFiltering = true,
  allowGrouping = true,
  allowExcelExport = true,
  allowPdfExport = false,
  allowEditing = false,
  pageSize = 20,
  height = 'auto',
  toolbar,
  filterSettings,
  editSettings,
  onRowSelected,
  onActionComplete,
  onActionFailure
}) => {
  const gridRef = useRef<GridComponent>(null);

  // Default toolbar with export options
  const defaultToolbar = ['ExcelExport', 'Search'];
  const finalToolbar = toolbar || (allowExcelExport ? defaultToolbar : ['Search']);

  // Default filter settings - filter bar under each column
  const defaultFilterSettings: FilterSettingsModel = {
    type: 'FilterBar',
    mode: 'Immediate',
    showFilterBarStatus: true
  };

  // Default edit settings
  const defaultEditSettings: EditSettingsModel = {
    allowEditing: true,
    allowAdding: false,
    allowDeleting: false,
    mode: 'Normal'
  };

  // Default page settings
  const pageSettings: PageSettingsModel = {
    pageSize: pageSize,
    pageSizes: [10, 20, 50, 100, 'All']
  };

  // Handle toolbar click (export)
  const toolbarClick = (args: any) => {
    if (gridRef.current) {
      if (args.item.id.includes('excelexport')) {
        const excelExportProperties = {
          fileName: `export_${new Date().getTime()}.xlsx`
        };
        gridRef.current.excelExport(excelExportProperties);
      } else if (args.item.id.includes('pdfexport')) {
        const pdfExportProperties = {
          fileName: `export_${new Date().getTime()}.pdf`
        };
        gridRef.current.pdfExport(pdfExportProperties);
      }
    }
  };

  return (
    <div className="syncfusion-grid-wrapper">
      <GridComponent
        ref={gridRef}
        dataSource={dataSource}
        allowPaging={allowPaging}
        allowSorting={allowSorting}
        allowFiltering={allowFiltering}
        allowGrouping={allowGrouping}
        allowExcelExport={allowExcelExport}
        allowPdfExport={allowPdfExport}
        toolbar={finalToolbar}
        pageSettings={pageSettings}
        filterSettings={filterSettings || defaultFilterSettings}
        editSettings={allowEditing ? (editSettings || defaultEditSettings) : undefined}
        height={height}
        toolbarClick={toolbarClick}
        rowSelected={onRowSelected}
        actionComplete={onActionComplete}
        actionFailure={onActionFailure}
        enableHover={true}
        enableHeaderFocus={true}
        enableStickyHeader={true}
      >
        <ColumnsDirective>
          {columns.map((col, index) => (
            <ColumnDirective
              key={`${col.field}-${index}`}
              field={col.field}
              headerText={col.headerText}
              width={col.width}
              type={col.type}
              format={col.format}
              template={col.template}
              allowEditing={col.allowEditing !== undefined ? col.allowEditing : true}
              allowFiltering={col.allowFiltering !== undefined ? col.allowFiltering : true}
              allowSorting={col.allowSorting !== undefined ? col.allowSorting : true}
              allowGrouping={col.allowGrouping !== undefined ? col.allowGrouping : true}
            />
          ))}
        </ColumnsDirective>
        <Inject services={[
          Page, 
          Sort, 
          Filter, 
          Group, 
          Toolbar, 
          ExcelExport, 
          PdfExport, 
          Edit, 
          CommandColumn
        ]} />
      </GridComponent>
    </div>
  );
};

export default SyncfusionGrid;

