/**
 * Excel Export Utilities
 * Centralized Excel export functionality using exceljs library
 * Used across the entire CephasOps frontend for consistent data export
 */

import ExcelJS from 'exceljs';

// ============================================================================
// Types
// ============================================================================

export interface ExportColumn<T> {
  key: keyof T | string;
  header: string;
  width?: number;
  formatter?: (value: any, row: T) => string | number;
}

export interface ExportOptions {
  filename?: string;
  sheetName?: string;
  includeTimestamp?: boolean;
  dateFormat?: string;
}

// ============================================================================
// Core Export Functions
// ============================================================================

/**
 * Export data to Excel file
 * @param data - Array of objects to export
 * @param columns - Column definitions
 * @param options - Export options
 */
export async function exportToExcel<T extends Record<string, any>>(
  data: T[],
  columns: ExportColumn<T>[],
  options: ExportOptions = {}
): Promise<void> {
  const {
    filename = 'export',
    sheetName = 'Data',
    includeTimestamp = true,
  } = options;

  if (data.length === 0) {
    console.warn('No data to export');
    return;
  }

  // Create workbook and worksheet
  const workbook = new ExcelJS.Workbook();
  const worksheet = workbook.addWorksheet(sheetName);

  // Define columns
  worksheet.columns = columns.map(col => ({
    header: col.header,
    key: col.header, // Use header as key for simplicity
    width: col.width || 15,
  }));

  // Style header row
  worksheet.getRow(1).font = { bold: true };
  worksheet.getRow(1).fill = {
    type: 'pattern',
    pattern: 'solid',
    fgColor: { argb: 'FFE0E0E0' }
  };

  // Transform and add data
  data.forEach(row => {
    const exportRow: Record<string, any> = {};
    
    columns.forEach(col => {
      const key = col.key as string;
      const value = getNestedValue(row, key);
      
      if (col.formatter) {
        exportRow[col.header] = col.formatter(value, row);
      } else {
        exportRow[col.header] = formatValue(value);
      }
    });
    
    worksheet.addRow(exportRow);
  });

  // Generate filename with optional timestamp
  const timestamp = includeTimestamp ? `_${new Date().toISOString().split('T')[0]}` : '';
  const fullFilename = `${filename}${timestamp}.xlsx`;

  // Download file
  const buffer = await workbook.xlsx.writeBuffer();
  downloadBuffer(buffer, fullFilename);
}

/**
 * Export simple array of objects to Excel
 * Automatically generates columns from object keys
 * @param data - Array of objects to export
 * @param filename - Output filename
 * @param sheetName - Sheet name
 */
export async function exportSimpleToExcel<T extends Record<string, any>>(
  data: T[],
  filename: string = 'export',
  sheetName: string = 'Data'
): Promise<void> {
  if (data.length === 0) {
    console.warn('No data to export');
    return;
  }

  // Create workbook and worksheet
  const workbook = new ExcelJS.Workbook();
  const worksheet = workbook.addWorksheet(sheetName);

  // Auto-generate columns from first object
  const keys = Object.keys(data[0]);
  worksheet.columns = keys.map(key => ({
    header: key,
    key: key,
    width: Math.min(Math.max(key.length + 2, 15), 50),
  }));

  // Style header row
  worksheet.getRow(1).font = { bold: true };
  worksheet.getRow(1).fill = {
    type: 'pattern',
    pattern: 'solid',
    fgColor: { argb: 'FFE0E0E0' }
  };

  // Add data
  data.forEach(row => {
    worksheet.addRow(row);
  });

  // Generate filename with timestamp
  const timestamp = new Date().toISOString().split('T')[0];
  const fullFilename = `${filename}_${timestamp}.xlsx`;

  // Download file
  const buffer = await workbook.xlsx.writeBuffer();
  downloadBuffer(buffer, fullFilename);
}

// ============================================================================
// Specialized Export Functions
// ============================================================================

/**
 * Export orders to Excel
 * @param orders - Array of orders
 * @param getInstallerName - Function to get installer name by ID
 */
export async function exportOrdersToExcel(
  orders: any[],
  getInstallerName?: (siId: string) => string
): Promise<void> {
  const columns: ExportColumn<any>[] = [
    { key: 'serviceId', header: 'Service ID', width: 15 },
    { key: 'ticketId', header: 'Ticket ID', width: 15 },
    { key: 'customerName', header: 'Customer', width: 25 },
    { key: 'customerPhone', header: 'Phone', width: 15 },
    { key: 'buildingName', header: 'Building', width: 20 },
    { key: 'address', header: 'Address', width: 30 },
    { key: 'orderType', header: 'Order Type', width: 15 },
    { key: 'priority', header: 'Priority', width: 10 },
    { key: 'status', header: 'Status', width: 15 },
    { 
      key: 'appointmentDate', 
      header: 'Appointment Date', 
      width: 15,
      formatter: (value) => formatDateValue(value)
    },
    { key: 'appointmentTime', header: 'Appointment Time', width: 12 },
    { 
      key: 'assignedSiId', 
      header: 'Installer', 
      width: 20,
      formatter: (value) => getInstallerName ? getInstallerName(value) : value || 'Unassigned'
    },
    { key: 'partnerName', header: 'Partner', width: 15 },
    { key: 'orderNotesInternal', header: 'Notes', width: 30 },
  ];

  await exportToExcel(orders, columns, {
    filename: 'Orders_Export',
    sheetName: 'Orders',
  });
}

/**
 * Export service installers to Excel
 * @param installers - Array of service installers
 */
export async function exportServiceInstallersToExcel(installers: any[]): Promise<void> {
  const columns: ExportColumn<any>[] = [
    { key: 'name', header: 'Name', width: 25 },
    { key: 'email', header: 'Email', width: 30 },
    { key: 'phone', header: 'Phone', width: 15 },
    { key: 'siLevel', header: 'SI Level', width: 12 },
    { 
      key: 'isSubcontractor', 
      header: 'Type', 
      width: 15,
      formatter: (value) => value ? 'Subcontractor' : 'In-house'
    },
    { 
      key: 'isActive', 
      header: 'Status', 
      width: 10,
      formatter: (value) => value ? 'Active' : 'Inactive'
    },
    { key: 'departmentName', header: 'Department', width: 20 },
  ];

  await exportToExcel(installers, columns, {
    filename: 'Service_Installers_Export',
    sheetName: 'Service Installers',
  });
}

/**
 * Export partners to Excel
 * @param partners - Array of partners
 */
export async function exportPartnersToExcel(partners: any[]): Promise<void> {
  const columns: ExportColumn<any>[] = [
    { key: 'name', header: 'Name', width: 25 },
    { key: 'code', header: 'Code', width: 10 },
    { key: 'groupName', header: 'Group', width: 15 },
    { key: 'contactPerson', header: 'Contact Person', width: 20 },
    { key: 'email', header: 'Email', width: 30 },
    { key: 'phone', header: 'Phone', width: 15 },
    { 
      key: 'isActive', 
      header: 'Status', 
      width: 10,
      formatter: (value) => value ? 'Active' : 'Inactive'
    },
  ];

  await exportToExcel(partners, columns, {
    filename: 'Partners_Export',
    sheetName: 'Partners',
  });
}

/**
 * Export buildings to Excel
 * @param buildings - Array of buildings
 */
export async function exportBuildingsToExcel(buildings: any[]): Promise<void> {
  const columns: ExportColumn<any>[] = [
    { key: 'name', header: 'Name', width: 30 },
    { key: 'code', header: 'Code', width: 15 },
    { key: 'buildingTypeName', header: 'Type', width: 15 },
    { key: 'address', header: 'Address', width: 40 },
    { key: 'city', header: 'City', width: 15 },
    { key: 'state', header: 'State', width: 15 },
    { key: 'postcode', header: 'Postcode', width: 10 },
    { 
      key: 'isActive', 
      header: 'Status', 
      width: 10,
      formatter: (value) => value ? 'Active' : 'Inactive'
    },
  ];

  await exportToExcel(buildings, columns, {
    filename: 'Buildings_Export',
    sheetName: 'Buildings',
  });
}

/**
 * Export invoices to Excel
 * @param invoices - Array of invoices
 */
export async function exportInvoicesToExcel(invoices: any[]): Promise<void> {
  const columns: ExportColumn<any>[] = [
    { key: 'invoiceNumber', header: 'Invoice #', width: 15 },
    { key: 'customerName', header: 'Customer', width: 25 },
    { key: 'partnerName', header: 'Partner', width: 20 },
    { 
      key: 'invoiceDate', 
      header: 'Invoice Date', 
      width: 15,
      formatter: (value) => formatDateValue(value)
    },
    { 
      key: 'dueDate', 
      header: 'Due Date', 
      width: 15,
      formatter: (value) => formatDateValue(value)
    },
    { 
      key: 'totalAmount', 
      header: 'Total Amount', 
      width: 15,
      formatter: (value) => formatCurrency(value)
    },
    { key: 'status', header: 'Status', width: 12 },
  ];

  await exportToExcel(invoices, columns, {
    filename: 'Invoices_Export',
    sheetName: 'Invoices',
  });
}

/**
 * Export payroll data to Excel
 * @param payrollData - Array of payroll records
 */
export async function exportPayrollToExcel(payrollData: any[]): Promise<void> {
  const columns: ExportColumn<any>[] = [
    { key: 'period', header: 'Period', width: 15 },
    { key: 'serviceInstallerName', header: 'Service Installer', width: 25 },
    { key: 'totalJobs', header: 'Total Jobs', width: 12 },
    { 
      key: 'totalEarnings', 
      header: 'Total Earnings', 
      width: 15,
      formatter: (value) => formatCurrency(value)
    },
    { 
      key: 'deductions', 
      header: 'Deductions', 
      width: 15,
      formatter: (value) => formatCurrency(value)
    },
    { 
      key: 'netPay', 
      header: 'Net Pay', 
      width: 15,
      formatter: (value) => formatCurrency(value)
    },
    { key: 'status', header: 'Status', width: 12 },
  ];

  await exportToExcel(payrollData, columns, {
    filename: 'Payroll_Export',
    sheetName: 'Payroll',
  });
}

/**
 * Export inventory to Excel
 * @param inventory - Array of inventory items
 */
export async function exportInventoryToExcel(inventory: any[]): Promise<void> {
  const columns: ExportColumn<any>[] = [
    { key: 'materialName', header: 'Material', width: 25 },
    { key: 'sku', header: 'SKU', width: 15 },
    { key: 'categoryName', header: 'Category', width: 15 },
    { key: 'quantity', header: 'Quantity', width: 12 },
    { key: 'unitOfMeasure', header: 'UOM', width: 10 },
    { key: 'reorderLevel', header: 'Reorder Level', width: 12 },
    { key: 'location', header: 'Location', width: 15 },
    { 
      key: 'lastUpdated', 
      header: 'Last Updated', 
      width: 15,
      formatter: (value) => formatDateValue(value)
    },
  ];

  await exportToExcel(inventory, columns, {
    filename: 'Inventory_Export',
    sheetName: 'Inventory',
  });
}

/**
 * Export assets to Excel
 * @param assets - Array of assets
 */
export async function exportAssetsToExcel(assets: any[]): Promise<void> {
  const columns: ExportColumn<any>[] = [
    { key: 'assetTag', header: 'Asset Tag', width: 15 },
    { key: 'name', header: 'Name', width: 25 },
    { key: 'assetTypeName', header: 'Type', width: 15 },
    { key: 'serialNumber', header: 'Serial Number', width: 20 },
    { key: 'assignedTo', header: 'Assigned To', width: 20 },
    { key: 'location', header: 'Location', width: 15 },
    { 
      key: 'purchaseDate', 
      header: 'Purchase Date', 
      width: 15,
      formatter: (value) => formatDateValue(value)
    },
    { 
      key: 'purchasePrice', 
      header: 'Purchase Price', 
      width: 15,
      formatter: (value) => formatCurrency(value)
    },
    { key: 'status', header: 'Status', width: 12 },
  ];

  await exportToExcel(assets, columns, {
    filename: 'Assets_Export',
    sheetName: 'Assets',
  });
}

// ============================================================================
// Helper Functions
// ============================================================================

/**
 * Get nested value from object using dot notation
 * @param obj - Object to get value from
 * @param path - Path to value (e.g., "customer.name")
 * @returns Value at path or undefined
 */
function getNestedValue(obj: Record<string, any>, path: string): any {
  return path.split('.').reduce((acc, key) => acc?.[key], obj);
}

/**
 * Format value for Excel
 * @param value - Value to format
 * @returns Formatted value
 */
function formatValue(value: any): string | number {
  if (value === null || value === undefined) return '-';
  if (typeof value === 'boolean') return value ? 'Yes' : 'No';
  if (value instanceof Date) return formatDateValue(value);
  return value;
}

/**
 * Format date value for Excel
 * @param value - Date value
 * @returns Formatted date string
 */
function formatDateValue(value: any): string {
  if (!value) return '-';
  
  try {
    const date = new Date(value);
    if (isNaN(date.getTime())) return '-';
    return date.toLocaleDateString('en-MY', { 
      day: 'numeric', 
      month: 'short', 
      year: 'numeric' 
    });
  } catch {
    return '-';
  }
}

/**
 * Format currency value for Excel
 * @param value - Currency value
 * @param currency - Currency code (default: MYR)
 * @returns Formatted currency string
 */
function formatCurrency(value: any, currency: string = 'MYR'): string {
  if (value === null || value === undefined) return '-';
  
  const num = typeof value === 'string' ? parseFloat(value) : value;
  if (isNaN(num)) return '-';
  
  return `${currency} ${num.toFixed(2)}`;
}

/**
 * Download buffer as file
 * @param buffer - File buffer
 * @param filename - File name
 */
function downloadBuffer(buffer: ExcelJS.Buffer, filename: string): void {
  const blob = new Blob([buffer], { 
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' 
  });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);
}

// ============================================================================
// Excel Import Functions (for reading uploaded files)
// ============================================================================

/**
 * Read Excel file and convert to JSON
 * @param file - File to read
 * @param sheetIndex - Sheet index to read (default: 0)
 * @returns Array of objects representing rows
 */
export async function importFromExcel(
  file: File,
  sheetIndex: number = 0
): Promise<any[]> {
  const buffer = await file.arrayBuffer();
  const workbook = new ExcelJS.Workbook();
  await workbook.xlsx.load(buffer);

  const worksheet = workbook.worksheets[sheetIndex];
  if (!worksheet) {
    throw new Error('Worksheet not found');
  }

  const data: any[] = [];
  const headers: string[] = [];

  // Get headers from first row
  worksheet.getRow(1).eachCell((cell, colNumber) => {
    headers[colNumber - 1] = cell.value?.toString() || `Column${colNumber}`;
  });

  // Read data rows
  worksheet.eachRow((row, rowNumber) => {
    if (rowNumber === 1) return; // Skip header row

    const rowData: any = {};
    row.eachCell((cell, colNumber) => {
      const header = headers[colNumber - 1];
      rowData[header] = cell.value;
    });

    data.push(rowData);
  });

  return data;
}

// ============================================================================
// Export Utility for Custom Reports
// ============================================================================

/**
 * Create a custom report exporter
 * @param reportName - Name of the report
 * @param columns - Column definitions
 * @returns Export function
 */
export function createReportExporter<T extends Record<string, any>>(
  reportName: string,
  columns: ExportColumn<T>[]
): (data: T[]) => Promise<void> {
  return async (data: T[]) => {
    await exportToExcel(data, columns, {
      filename: `${reportName}_Report`,
      sheetName: reportName,
    });
  };
}
