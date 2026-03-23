# Syncfusion Quick Reference Guide

## 🚀 Quick Start

### Setup on New Machine
```powershell
# 1. Clone and install
git pull
cd frontend && npm install
cd ../backend && dotnet restore

# 2. Configure license keys
# Frontend: Create frontend/.env
VITE_SYNCFUSION_LICENSE_KEY=Ngo9BigBOggjGyl/Vkd+XU9FcVRDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS3tSdEVnWX1beXFQQmZYU091Xg==

# Backend: User secrets
cd backend/src/CephasOps.Api
dotnet user-secrets set "SYNCFUSION_LICENSE_KEY" "Ngo9BigBOggjGyl/Vkd+XU9FcVRDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS3tSdEVnWX1beXFQQmZYU091Xg=="

# 3. Start services
dotnet watch run  # Backend
npm run dev       # Frontend
```

---

## 📦 Installed Components

### Frontend
| Package | Purpose | Used In |
|---------|---------|---------|
| ej2-react-pdfviewer | PDF viewing | Parser review snapshots |
| ej2-react-grids | Data tables | All list pages |
| ej2-react-charts | Charts & gauges | Dashboards, PnL, Analytics |
| ej2-react-calendars | Date pickers | Filters, forms |
| ej2-react-dropdowns | Advanced dropdowns | Filters, multi-select |
| ej2-react-schedule | Calendar/Scheduler | SI assignment, appointments |
| ej2-react-treegrid | Hierarchical grids | Buildings, Splitters |
| ej2-react-diagrams | Network diagrams | Warehouse layout, Splitter topology |
| ej2-react-richtexteditor | Rich text | Templates, notes |
| ej2-react-inputs | Form inputs | All forms |
| ej2-react-spreadsheet | Excel viewer | Parser snapshots (alternative) |

### Backend
| Package | Purpose | Used In |
|---------|---------|---------|
| Syncfusion.XlsIO.Net.Core | Excel read/write | Parser, BOQ generation |
| Syncfusion.Pdf.Net.Core | PDF generation | Invoices, reports |
| Syncfusion.DocIO.Net.Core | Word documents | PO, Quotations |
| Syncfusion.DocIORenderer.Net.Core | Word to PDF | Document conversion |

---

## 🎨 Component Usage Examples

### 1. Syncfusion Grid (Replace DataTable)

**Before (Custom DataTable)**:
```tsx
<DataTable
  columns={columns}
  data={orders}
  loading={loading}
  pagination={true}
/>
```

**After (Syncfusion Grid)**:
```tsx
import { ColumnDirective, ColumnsDirective, GridComponent, Inject, Page, Sort, Filter, Group, Toolbar, ExcelExport } from '@syncfusion/ej2-react-grids';

<GridComponent
  dataSource={orders}
  allowPaging={true}
  allowSorting={true}
  allowFiltering={true}
  allowGrouping={true}
  allowExcelExport={true}
  toolbar={['ExcelExport', 'Search']}
  pageSettings={{ pageSize: 20 }}
>
  <ColumnsDirective>
    <ColumnDirective field='orderNumber' headerText='Order #' width='150' />
    <ColumnDirective field='customerName' headerText='Customer' width='200' />
    <ColumnDirective field='status' headerText='Status' width='120' />
  </ColumnsDirective>
  <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport]} />
</GridComponent>
```

---

### 2. Charts (Dashboards)

**Line Chart (Trend)**:
```tsx
import { ChartComponent, SeriesCollectionDirective, SeriesDirective, Inject, LineSeries, Category, Tooltip, Legend } from '@syncfusion/ej2-react-charts';

<ChartComponent
  primaryXAxis={{ valueType: 'Category' }}
  title='Orders Trend - Last 30 Days'
  tooltip={{ enable: true }}
>
  <Inject services={[LineSeries, Category, Tooltip, Legend]} />
  <SeriesCollectionDirective>
    <SeriesDirective
      dataSource={trendData}
      xName='date'
      yName='count'
      name='Orders'
      type='Line'
      marker={{ visible: true }}
    />
  </SeriesCollectionDirective>
</ChartComponent>
```

**Pie Chart (Breakdown)**:
```tsx
import { AccumulationChartComponent, AccumulationSeriesCollectionDirective, AccumulationSeriesDirective, Inject, AccumulationLegend, PieSeries, AccumulationTooltip, AccumulationDataLabel } from '@syncfusion/ej2-react-charts';

<AccumulationChartComponent
  title='Orders by Partner'
  tooltip={{ enable: true }}
  legendSettings={{ visible: true }}
>
  <Inject services={[AccumulationLegend, PieSeries, AccumulationTooltip, AccumulationDataLabel]} />
  <AccumulationSeriesCollectionDirective>
    <AccumulationSeriesDirective
      dataSource={partnerData}
      xName='partner'
      yName='count'
      type='Pie'
      dataLabel={{ visible: true, name: 'text', position: 'Outside' }}
    />
  </AccumulationSeriesCollectionDirective>
</AccumulationChartComponent>
```

---

### 3. Scheduler (Calendar)

```tsx
import { ScheduleComponent, Day, Week, Month, Agenda, Inject, ViewsDirective, ViewDirective, ResourcesDirective, ResourceDirective } from '@syncfusion/ej2-react-schedule';

<ScheduleComponent
  height='650px'
  selectedDate={new Date()}
  eventSettings={{ dataSource: appointments }}
  group={{ resources: ['ServiceInstallers'] }}
>
  <ResourcesDirective>
    <ResourceDirective
      field='serviceInstallerId'
      title='Service Installer'
      name='ServiceInstallers'
      dataSource={installers}
      textField='name'
      idField='id'
      colorField='color'
    />
  </ResourcesDirective>
  <ViewsDirective>
    <ViewDirective option='Day' />
    <ViewDirective option='Week' />
    <ViewDirective option='Month' />
    <ViewDirective option='Agenda' />
  </ViewsDirective>
  <Inject services={[Day, Week, Month, Agenda]} />
</ScheduleComponent>
```

---

### 4. TreeGrid (Hierarchical Data)

```tsx
import { TreeGridComponent, ColumnsDirective, ColumnDirective, Inject, Page, Sort, Filter, Toolbar } from '@syncfusion/ej2-react-treegrid';

<TreeGridComponent
  dataSource={buildingsHierarchy}
  treeColumnIndex={1}
  childMapping='children'
  allowPaging={true}
  allowSorting={true}
  allowFiltering={true}
>
  <ColumnsDirective>
    <ColumnDirective field='id' headerText='ID' width='100' />
    <ColumnDirective field='name' headerText='Name' width='250' />
    <ColumnDirective field='units' headerText='Units' width='100' />
    <ColumnDirective field='connected' headerText='Connected' width='100' />
  </ColumnsDirective>
  <Inject services={[Page, Sort, Filter, Toolbar]} />
</TreeGridComponent>
```

---

### 5. Diagram (Network Topology)

```tsx
import { DiagramComponent, Inject, DataBinding, HierarchicalTree } from '@syncfusion/ej2-react-diagrams';

<DiagramComponent
  width='100%'
  height='600px'
  dataSourceSettings={{
    id: 'id',
    parentId: 'parentId',
    dataSource: networkNodes
  }}
  layout={{
    type: 'HierarchicalTree',
    orientation: 'TopToBottom'
  }}
>
  <Inject services={[DataBinding, HierarchicalTree]} />
</DiagramComponent>
```

---

## 🎨 Theming

### Custom Theme Colors
Syncfusion uses CSS variables. Customize in `index.css`:

```css
:root {
  /* Syncfusion primary color (matches CephasOps brand) */
  --e-primary: #3b82f6;
  --e-primary-darker: #2563eb;
  --e-primary-lighter: #60a5fa;
}
```

### Dark Mode
Syncfusion supports dark mode. Import dark theme:
```css
@import '@syncfusion/ej2-base/styles/material-dark.css';
```

---

## 💡 Best Practices

### 1. Always Use Inject
```tsx
<GridComponent>
  <Inject services={[Page, Sort, Filter]} />  {/* Required! */}
</GridComponent>
```

### 2. Memoize Data
```tsx
const gridData = useMemo(() => orders, [orders]);
```

### 3. Handle Events
```tsx
<GridComponent
  dataSource={data}
  actionComplete={(args) => {
    if (args.requestType === 'save') {
      // Handle inline edit save
    }
  }}
/>
```

### 4. Export Functionality
```tsx
const gridRef = useRef<GridComponent>(null);

const exportToExcel = () => {
  gridRef.current?.excelExport();
};

<GridComponent ref={gridRef} allowExcelExport={true}>
  ...
</GridComponent>
```

---

## 🐛 Common Issues & Solutions

### Issue: License Warning
**Symptom**: "Trial version" watermark or warning
**Solution**: Verify license key in `.env` and `user-secrets`

### Issue: Styles Not Applied
**Symptom**: Components look unstyled
**Solution**: Import CSS in `index.css`

### Issue: Component Not Rendering
**Symptom**: Blank space where component should be
**Solution**: Check `<Inject services={[...]} />` is present

### Issue: Large Bundle Size
**Symptom**: Slow page loads
**Solution**: Use tree-shaking, import only needed services

---

## 📚 Resources

- **Documentation**: https://ej2.syncfusion.com/react/documentation/
- **Demos**: https://ej2.syncfusion.com/react/demos/
- **API Reference**: https://ej2.syncfusion.com/react/documentation/api/
- **Support**: https://www.syncfusion.com/support

---

**Last Updated**: December 4, 2025

