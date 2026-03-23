using Microsoft.Extensions.Logging;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Barcode;
using Syncfusion.Drawing;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Color = Syncfusion.Drawing.Color;

namespace CephasOps.Application.Documents;

/// <summary>
/// BOQ (Bill of Quantities) Generator using Syncfusion XlsIO
/// Generates professional Excel BOQ with:
/// - Native Excel formulas (SUM, totals)
/// - Multiple sheets (Materials, Labor, Summary)
/// - Professional styling (colors, borders, fonts)
/// - Editable by users (can modify quantities/prices)
/// - Convert to PDF when final
/// </summary>
public interface ISyncfusionBoqGenerator
{
    /// <summary>
    /// Generate BOQ Excel file for a project
    /// </summary>
    Task<byte[]> GenerateBoqExcelAsync(Guid projectId, Guid companyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Convert BOQ Excel to PDF
    /// </summary>
    byte[] ConvertBoqToPdf(byte[] excelBytes);
}

public class SyncfusionBoqGenerator : ISyncfusionBoqGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SyncfusionBoqGenerator> _logger;

    public SyncfusionBoqGenerator(
        ApplicationDbContext context,
        ILogger<SyncfusionBoqGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> GenerateBoqExcelAsync(Guid projectId, Guid companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating BOQ Excel for project {ProjectId}", projectId);

            // Create Excel engine
            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;

            var workbook = application.Workbooks.Create(3); // 3 sheets: Materials, Labor, Summary

            // Sheet 1: Materials BOQ
            await CreateMaterialsSheet(workbook.Worksheets[0], projectId, companyId);

            // Sheet 2: Labor BOQ
            await CreateLaborSheet(workbook.Worksheets[1], projectId, companyId);

            // Sheet 3: Summary
            await CreateSummarySheet(workbook.Worksheets[2], projectId, companyId);

            // Save to byte array
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            workbook.Close();

            var bytes = stream.ToArray();
            _logger.LogInformation("✅ BOQ Excel generated successfully: {Size} KB", bytes.Length / 1024);

            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating BOQ Excel for project {ProjectId}", projectId);
            throw;
        }
    }

    private async Task CreateMaterialsSheet(IWorksheet worksheet, Guid projectId, Guid companyId)
    {
        worksheet.Name = "Materials";

        // Header Section
        worksheet.Range["A1"].Text = "BILL OF QUANTITIES - MATERIALS";
        worksheet.Range["A1"].CellStyle.Font.Bold = true;
        worksheet.Range["A1"].CellStyle.Font.Size = 16;
        worksheet.Range["A1"].CellStyle.Font.Color = ExcelKnownColors.White;
        worksheet.Range["A1:F1"].Merge();
        worksheet.Range["A1:F1"].CellStyle.Color = Color.FromArgb(68, 114, 196);
        worksheet.Range["A1:F1"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
        worksheet.Range["A1:F1"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;

        // Fetch project from database
        var project = await _context.Set<CephasOps.Domain.Projects.Entities.Project>()
            .Include(p => p.BoqItems)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

        if (project == null)
        {
            throw new KeyNotFoundException($"Project with ID {projectId} not found");
        }

        // Project Info
        worksheet.Range["A3"].Text = "Project:";
        worksheet.Range["B3"].Text = project.Name; // ✅ Get from database
        worksheet.Range["A4"].Text = "Date:";
        worksheet.Range["B4"].DateTime = DateTime.Now;
        worksheet.Range["B4"].NumberFormat = "dd MMM yyyy";
        worksheet.Range["A5"].Text = "Prepared By:";
        worksheet.Range["B5"].Text = "CephasOps";

        // Table Header (Row 7)
        var headers = new[] { "No.", "Description", "Unit", "Qty", "Unit Price (RM)", "Total (RM)" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Range[7, i + 1];
            cell.Text = headers[i];
            cell.CellStyle.Font.Bold = true;
            cell.CellStyle.Color = Color.FromArgb(217, 225, 242);
            cell.CellStyle.Borders.LineStyle = ExcelLineStyle.Thin;
        }

        // ✅ Fetch BOQ items from database (Material type)
        var materials = await _context.Set<CephasOps.Domain.Projects.Entities.BoqItem>()
            .Where(b => b.ProjectId == projectId 
                && b.CompanyId == companyId 
                && b.ItemType == "Material")
            .OrderBy(b => b.LineNumber)
            .ThenBy(b => b.Section)
            .ToListAsync();

        // Add data rows with formulas
        int row = 8;
        int itemNo = 1;
        foreach (var item in materials)
        {
            worksheet.Range[row, 1].Number = itemNo;
            worksheet.Range[row, 2].Text = item.Description;
            worksheet.Range[row, 3].Text = item.Unit;
            worksheet.Range[row, 4].Number = (double)item.Quantity;
            worksheet.Range[row, 5].Number = (double)item.UnitRate;
            worksheet.Range[row, 6].Formula = $"=D{row}*E{row}"; // Qty × Price
            
            // Formatting
            worksheet.Range[row, 5].NumberFormat = "RM #,##0.00";
            worksheet.Range[row, 6].NumberFormat = "RM #,##0.00";
            
            row++;
            itemNo++;
        }

        // If no materials, add a placeholder row
        if (materials.Count == 0)
        {
            worksheet.Range[row, 2].Text = "No materials defined";
            worksheet.Range[row, 2].CellStyle.Font.Italic = true;
            row++;
        }

        // Total Row
        worksheet.Range[row, 5].Text = "TOTAL:";
        worksheet.Range[row, 5].CellStyle.Font.Bold = true;
        worksheet.Range[row, 5].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
        worksheet.Range[row, 6].Formula = materials.Count > 0 ? $"=SUM(F8:F{row - 1})" : "0";
        worksheet.Range[row, 6].CellStyle.Font.Bold = true;
        worksheet.Range[row, 6].NumberFormat = "RM #,##0.00";
        worksheet.Range[row, 6].CellStyle.Color = Color.FromArgb(217, 225, 242);

        // Auto-fit columns
        worksheet.UsedRange.AutofitColumns();

        // Set column widths for better appearance
        worksheet.SetColumnWidth(1, 8);   // No.
        worksheet.SetColumnWidth(2, 35);  // Description
        worksheet.SetColumnWidth(3, 12);  // Unit
        worksheet.SetColumnWidth(4, 10);  // Qty
        worksheet.SetColumnWidth(5, 18);  // Unit Price
        worksheet.SetColumnWidth(6, 18);  // Total

        // Add borders to data range
        var dataRange = worksheet.Range[$"A7:F{row}"];
        dataRange.CellStyle.Borders.LineStyle = ExcelLineStyle.Thin;
        dataRange.CellStyle.Borders.Color = ExcelKnownColors.Grey_25_percent;
    }

    private async Task CreateLaborSheet(IWorksheet worksheet, Guid projectId, Guid companyId)
    {
        worksheet.Name = "Labor";

        // Header
        worksheet.Range["A1"].Text = "BILL OF QUANTITIES - LABOR";
        worksheet.Range["A1"].CellStyle.Font.Bold = true;
        worksheet.Range["A1"].CellStyle.Font.Size = 16;
        worksheet.Range["A1"].CellStyle.Font.Color = ExcelKnownColors.White;
        worksheet.Range["A1:F1"].Merge();
        worksheet.Range["A1:F1"].CellStyle.Color = Color.FromArgb(68, 114, 196);
        worksheet.Range["A1:F1"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

        // Table Header
        var headers = new[] { "No.", "Description", "Unit", "Qty", "Rate (RM)", "Total (RM)" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Range[7, i + 1];
            cell.Text = headers[i];
            cell.CellStyle.Font.Bold = true;
            cell.CellStyle.Color = Color.FromArgb(217, 225, 242);
        }

        // ✅ Fetch BOQ items from database (Labor type)
        var labor = await _context.Set<CephasOps.Domain.Projects.Entities.BoqItem>()
            .Where(b => b.ProjectId == projectId 
                && b.CompanyId == companyId 
                && b.ItemType == "Labor")
            .OrderBy(b => b.LineNumber)
            .ThenBy(b => b.Section)
            .ToListAsync();

        int row = 8;
        int itemNo = 1;
        foreach (var item in labor)
        {
            worksheet.Range[row, 1].Number = itemNo;
            worksheet.Range[row, 2].Text = item.Description;
            worksheet.Range[row, 3].Text = item.Unit;
            worksheet.Range[row, 4].Number = (double)item.Quantity;
            worksheet.Range[row, 5].Number = (double)item.UnitRate;
            worksheet.Range[row, 6].Formula = $"=D{row}*E{row}";
            
            worksheet.Range[row, 5].NumberFormat = "RM #,##0.00";
            worksheet.Range[row, 6].NumberFormat = "RM #,##0.00";
            
            row++;
            itemNo++;
        }

        // If no labor items, add a placeholder row
        if (labor.Count == 0)
        {
            worksheet.Range[row, 2].Text = "No labor items defined";
            worksheet.Range[row, 2].CellStyle.Font.Italic = true;
            row++;
        }

        // Total
        worksheet.Range[row, 5].Text = "TOTAL:";
        worksheet.Range[row, 5].CellStyle.Font.Bold = true;
        worksheet.Range[row, 5].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
        worksheet.Range[row, 6].Formula = labor.Count > 0 ? $"=SUM(F8:F{row - 1})" : "0";
        worksheet.Range[row, 6].CellStyle.Font.Bold = true;
        worksheet.Range[row, 6].NumberFormat = "RM #,##0.00";
        worksheet.Range[row, 6].CellStyle.Color = Color.FromArgb(217, 225, 242);

        worksheet.UsedRange.AutofitColumns();
    }

    private async Task CreateSummarySheet(IWorksheet worksheet, Guid projectId, Guid companyId)
    {
        worksheet.Name = "Summary";

        // Header
        worksheet.Range["A1"].Text = "BOQ SUMMARY";
        worksheet.Range["A1"].CellStyle.Font.Bold = true;
        worksheet.Range["A1"].CellStyle.Font.Size = 18;
        worksheet.Range["A1"].CellStyle.Font.Color = ExcelKnownColors.White;
        worksheet.Range["A1:D1"].Merge();
        worksheet.Range["A1:D1"].CellStyle.Color = Color.FromArgb(68, 114, 196);
        worksheet.Range["A1:D1"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;

        // Summary Table
        worksheet.Range["A3"].Text = "Category";
        worksheet.Range["B3"].Text = "Total (RM)";
        worksheet.Range["C3"].Text = "Percentage";
        worksheet.Range["A3:C3"].CellStyle.Font.Bold = true;
        worksheet.Range["A3:C3"].CellStyle.Color = Color.FromArgb(217, 225, 242);

        // Reference totals from other sheets with formulas
        // Use SUM on column F starting from row 8 (data rows) to handle dynamic row counts
        worksheet.Range["A4"].Text = "Materials";
        worksheet.Range["B4"].Formula = "=SUM(Materials!F8:F10000)"; // Sum all material totals (dynamic)
        worksheet.Range["B4"].NumberFormat = "RM #,##0.00";

        worksheet.Range["A5"].Text = "Labor";
        worksheet.Range["B5"].Formula = "=SUM(Labor!F8:F10000)"; // Sum all labor totals (dynamic)
        worksheet.Range["B5"].NumberFormat = "RM #,##0.00";

        worksheet.Range["A6"].Text = "GRAND TOTAL";
        worksheet.Range["A6"].CellStyle.Font.Bold = true;
        worksheet.Range["B6"].Formula = "=SUM(B4:B5)";
        worksheet.Range["B6"].CellStyle.Font.Bold = true;
        worksheet.Range["B6"].NumberFormat = "RM #,##0.00";
        worksheet.Range["B6"].CellStyle.Color = Color.FromArgb(217, 225, 242);

        // Percentage calculations
        worksheet.Range["C4"].Formula = "=B4/B6";
        worksheet.Range["C5"].Formula = "=B5/B6";
        worksheet.Range["C4:C5"].NumberFormat = "0.0%";

        // Add notes section
        worksheet.Range["A9"].Text = "Notes:";
        worksheet.Range["A9"].CellStyle.Font.Bold = true;
        worksheet.Range["A10"].Text = "• This BOQ is generated automatically by CephasOps";
        worksheet.Range["A11"].Text = "• Quantities and prices can be edited as needed";
        worksheet.Range["A12"].Text = "• Totals will auto-calculate via Excel formulas";
        worksheet.Range["A13"].Text = "• Save as PDF when finalized for submission";

        worksheet.UsedRange.AutofitColumns();
        worksheet.SetColumnWidth(1, 25);
        worksheet.SetColumnWidth(2, 20);
        worksheet.SetColumnWidth(3, 15);
    }

    public byte[] ConvertBoqToPdf(byte[] excelBytes)
    {
        try
        {
            _logger.LogInformation("Converting BOQ Excel to PDF");

            using var stream = new MemoryStream(excelBytes);
            
            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;

            var workbook = application.Workbooks.Open(stream);

            // Convert to PDF using XlsIORenderer
            var renderer = new XlsIORenderer();
            var pdfDocument = renderer.ConvertToPDF(workbook);

            using var pdfStream = new MemoryStream();
            pdfDocument.Save(pdfStream);
            pdfDocument.Close(true);
            workbook.Close();

            var pdfBytes = pdfStream.ToArray();
            _logger.LogInformation("✅ BOQ converted to PDF: {Size} KB", pdfBytes.Length / 1024);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error converting BOQ to PDF");
            throw;
        }
    }
}

/// <summary>
/// Enhanced Invoice Generator using Syncfusion PDF
/// Professional invoices with QR codes, watermarks, and digital signatures
/// </summary>
public interface ISyncfusionInvoiceGenerator
{
    Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId, Guid companyId, CancellationToken cancellationToken = default);
}

public class SyncfusionInvoiceGenerator : ISyncfusionInvoiceGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SyncfusionInvoiceGenerator> _logger;

    public SyncfusionInvoiceGenerator(
        ApplicationDbContext context,
        ILogger<SyncfusionInvoiceGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId, Guid companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Invoice PDF with Syncfusion for invoice {InvoiceId}", invoiceId);

        try
        {
            // Load invoice with related data
            var invoice = await _context.Set<CephasOps.Domain.Billing.Entities.Invoice>()
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == companyId, cancellationToken);

            if (invoice == null)
            {
                throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");
            }

            // Load company information
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

            // Load partner information
            var partner = await _context.Set<CephasOps.Domain.Companies.Entities.Partner>()
                .FirstOrDefaultAsync(p => p.Id == invoice.PartnerId, cancellationToken);

            // Create PDF document
            using var pdfDocument = new PdfDocument();
            var page = pdfDocument.Pages.Add();
            var graphics = page.Graphics;

            // Page settings
            var pageWidth = page.Size.Width;
            var pageHeight = page.Size.Height;
            var margin = 50f;
            var currentY = margin;

            // Define fonts
            var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 24, PdfFontStyle.Bold);
            var headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
            var normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
            var smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8);

            // Header Section - Company Info
            if (company != null)
            {
                graphics.DrawString(company.LegalName, titleFont, PdfBrushes.DarkBlue, new PointF(margin, currentY));
                currentY += 30;

                if (!string.IsNullOrEmpty(company.Address))
                {
                    graphics.DrawString(company.Address, normalFont, PdfBrushes.Black, new PointF(margin, currentY));
                    currentY += 15;
                }

                var contactInfo = new List<string>();
                if (!string.IsNullOrEmpty(company.Phone)) contactInfo.Add($"Phone: {company.Phone}");
                if (!string.IsNullOrEmpty(company.Email)) contactInfo.Add($"Email: {company.Email}");
                if (!string.IsNullOrEmpty(company.RegistrationNo)) contactInfo.Add($"Reg No: {company.RegistrationNo}");
                if (!string.IsNullOrEmpty(company.TaxId)) contactInfo.Add($"Tax ID: {company.TaxId}");

                if (contactInfo.Any())
                {
                    graphics.DrawString(string.Join(" | ", contactInfo), smallFont, PdfBrushes.Gray, new PointF(margin, currentY));
                    currentY += 20;
                }
            }

            // Invoice Title
            currentY += 10;
            graphics.DrawString("INVOICE", headerFont, PdfBrushes.DarkBlue, new PointF(pageWidth - margin - 100, margin));
            currentY = Math.Max(currentY, margin + 30);

            // Invoice Details Section
            currentY += 20;
            var invoiceDetailsY = currentY;

            // Bill To Section
            graphics.DrawString("Bill To:", headerFont, PdfBrushes.Black, new PointF(margin, currentY));
            currentY += 20;

            if (partner != null)
            {
                graphics.DrawString(partner.Name, normalFont, PdfBrushes.Black, new PointF(margin, currentY));
                currentY += 15;

                if (!string.IsNullOrEmpty(partner.BillingAddress))
                {
                    graphics.DrawString(partner.BillingAddress, normalFont, PdfBrushes.Black, new PointF(margin, currentY));
                    currentY += 15;
                }

                if (!string.IsNullOrEmpty(partner.ContactName))
                {
                    graphics.DrawString($"Contact: {partner.ContactName}", normalFont, PdfBrushes.Black, new PointF(margin, currentY));
                    currentY += 15;
                }
            }

            // Invoice Info Section (right side)
            var invoiceInfoX = pageWidth - margin - 200;
            var invoiceInfoY = invoiceDetailsY;

            graphics.DrawString("Invoice Number:", headerFont, PdfBrushes.Black, new PointF(invoiceInfoX, invoiceInfoY));
            graphics.DrawString(invoice.InvoiceNumber, normalFont, PdfBrushes.Black, new PointF(invoiceInfoX + 120, invoiceInfoY));
            invoiceInfoY += 20;

            graphics.DrawString("Invoice Date:", headerFont, PdfBrushes.Black, new PointF(invoiceInfoX, invoiceInfoY));
            graphics.DrawString(invoice.InvoiceDate.ToString("dd MMM yyyy"), normalFont, PdfBrushes.Black, new PointF(invoiceInfoX + 120, invoiceInfoY));
            invoiceInfoY += 20;

            if (invoice.DueDate.HasValue)
            {
                graphics.DrawString("Due Date:", headerFont, PdfBrushes.Black, new PointF(invoiceInfoX, invoiceInfoY));
                graphics.DrawString(invoice.DueDate.Value.ToString("dd MMM yyyy"), normalFont, PdfBrushes.Black, new PointF(invoiceInfoX + 120, invoiceInfoY));
                invoiceInfoY += 20;
            }

            graphics.DrawString("Status:", headerFont, PdfBrushes.Black, new PointF(invoiceInfoX, invoiceInfoY));
            var statusColor = invoice.Status == "Paid" ? PdfBrushes.Green : PdfBrushes.Red;
            graphics.DrawString(invoice.Status, normalFont, statusColor, new PointF(invoiceInfoX + 120, invoiceInfoY));

            // Line Items Table
            currentY = Math.Max(currentY, invoiceInfoY) + 30;

            // Table Header
            var tableStartY = currentY;
            var colWidths = new float[] { 50, 250, 80, 100, 100 };
            var colX = margin;

            graphics.DrawString("No.", headerFont, PdfBrushes.White, new PointF(colX, currentY));
            colX += colWidths[0];
            graphics.DrawString("Description", headerFont, PdfBrushes.White, new PointF(colX, currentY));
            colX += colWidths[1];
            graphics.DrawString("Qty", headerFont, PdfBrushes.White, new PointF(colX, currentY));
            colX += colWidths[2];
            graphics.DrawString("Unit Price", headerFont, PdfBrushes.White, new PointF(colX, currentY));
            colX += colWidths[3];
            graphics.DrawString("Total", headerFont, PdfBrushes.White, new PointF(colX, currentY));

            // Draw header background
            var headerRect = new RectangleF(margin, currentY - 5, pageWidth - 2 * margin, 25);
            graphics.DrawRectangle(new PdfSolidBrush(Color.FromArgb(68, 114, 196)), headerRect);

            currentY += 25;

            // Line Items
            int itemNo = 1;
            foreach (var lineItem in invoice.LineItems.OrderBy(li => itemNo++))
            {
                colX = margin;
                graphics.DrawString(itemNo.ToString(), normalFont, PdfBrushes.Black, new PointF(colX, currentY));
                colX += colWidths[0];
                graphics.DrawString(lineItem.Description, normalFont, PdfBrushes.Black, new PointF(colX, currentY));
                colX += colWidths[1];
                graphics.DrawString(lineItem.Quantity.ToString("N2"), normalFont, PdfBrushes.Black, new PointF(colX, currentY));
                colX += colWidths[2];
                graphics.DrawString($"RM {lineItem.UnitPrice:N2}", normalFont, PdfBrushes.Black, new PointF(colX, currentY));
                colX += colWidths[3];
                graphics.DrawString($"RM {lineItem.Total:N2}", normalFont, PdfBrushes.Black, new PointF(colX, currentY));

                currentY += 20;

                // Draw row separator
                graphics.DrawLine(new PdfPen(Color.LightGray, 0.5f), new PointF(margin, currentY - 5), new PointF(pageWidth - margin, currentY - 5));
            }

            // Totals Section
            currentY += 10;
            var totalsX = pageWidth - margin - 200;

            graphics.DrawString("Subtotal:", headerFont, PdfBrushes.Black, new PointF(totalsX, currentY));
            graphics.DrawString($"RM {invoice.SubTotal:N2}", normalFont, PdfBrushes.Black, new PointF(totalsX + 100, currentY));
            currentY += 20;

            if (invoice.TaxAmount > 0)
            {
                graphics.DrawString("Tax (SST):", headerFont, PdfBrushes.Black, new PointF(totalsX, currentY));
                graphics.DrawString($"RM {invoice.TaxAmount:N2}", normalFont, PdfBrushes.Black, new PointF(totalsX + 100, currentY));
                currentY += 20;
            }

            // Total line
            graphics.DrawLine(new PdfPen(Color.Black, 1f), new PointF(totalsX, currentY), new PointF(pageWidth - margin, currentY));
            currentY += 10;

            graphics.DrawString("TOTAL:", new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold), PdfBrushes.Black, new PointF(totalsX, currentY));
            graphics.DrawString($"RM {invoice.TotalAmount:N2}", new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold), PdfBrushes.DarkBlue, new PointF(totalsX + 100, currentY));

            // QR Code for Payment (if invoice is unpaid)
            if (invoice.Status != "Paid")
            {
                currentY += 40;
                var qrCodeY = currentY;

                // Generate QR code with payment information
                var paymentInfo = $"Invoice:{invoice.InvoiceNumber}|Amount:{invoice.TotalAmount:N2}|Company:{companyId}";
                var qrCode = new PdfQRBarcode();
                qrCode.Text = paymentInfo;
                qrCode.ErrorCorrectionLevel = PdfErrorCorrectionLevel.Medium;
                qrCode.XDimension = 3;

                var qrCodeSize = 100f;
                var qrCodeRect = new RectangleF(margin, qrCodeY, qrCodeSize, qrCodeSize);
                qrCode.Draw(graphics, qrCodeRect);

                graphics.DrawString("Scan to Pay", smallFont, PdfBrushes.Gray, new PointF(margin, qrCodeY + qrCodeSize + 5));
            }

            // Watermark
            if (invoice.Status == "Paid")
            {
                var watermarkFont = new PdfStandardFont(PdfFontFamily.Helvetica, 72, PdfFontStyle.Bold);
                var watermarkBrush = new PdfSolidBrush(Color.FromArgb(50, 0, 150, 0)); // Semi-transparent green
                graphics.DrawString("PAID", watermarkFont, watermarkBrush, new PointF(pageWidth / 2 - 100, pageHeight / 2 - 50));
            }
            else if (invoice.Status == "Overdue")
            {
                var watermarkFont = new PdfStandardFont(PdfFontFamily.Helvetica, 72, PdfFontStyle.Bold);
                var watermarkBrush = new PdfSolidBrush(Color.FromArgb(50, 150, 0, 0)); // Semi-transparent red
                graphics.DrawString("OVERDUE", watermarkFont, watermarkBrush, new PointF(pageWidth / 2 - 150, pageHeight / 2 - 50));
            }

            // Footer
            var footerY = pageHeight - margin - 30;
            graphics.DrawString("Thank you for your business!", normalFont, PdfBrushes.Gray, new PointF(margin, footerY));
            graphics.DrawString($"Generated on {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC", smallFont, PdfBrushes.Gray, new PointF(pageWidth - margin - 200, footerY));

            // Save PDF to memory stream
            using var stream = new MemoryStream();
            pdfDocument.Save(stream);
            pdfDocument.Close(true);

            var pdfBytes = stream.ToArray();
            _logger.LogInformation("✅ Invoice PDF generated successfully: {Size} KB", pdfBytes.Length / 1024);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating Invoice PDF for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }
}

