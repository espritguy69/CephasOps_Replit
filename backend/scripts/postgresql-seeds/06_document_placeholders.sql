-- ============================================
-- Document Placeholder Definitions Seed Script
-- ============================================
-- Seeds: DocumentPlaceholderDefinitions
-- Dependencies: None
-- ============================================

-- ============================================
-- Invoice Placeholders
-- ============================================
INSERT INTO "DocumentPlaceholderDefinitions" (
    "Id", "DocumentType", "Key", "Description", "ExampleValue", "IsRequired", "CreatedAt"
) VALUES
    (gen_random_uuid(), 'Invoice', '{{invoice.id}}', 'Invoice unique identifier', '550e8400-e29b-41d4-a716-446655440000', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{invoice.number}}', 'Invoice number', 'INV-2024-0001', true, NOW()),
    (gen_random_uuid(), 'Invoice', '{{invoice.date}}', 'Invoice date', '2024-01-15', true, NOW()),
    (gen_random_uuid(), 'Invoice', '{{invoice.dueDate}}', 'Payment due date', '2024-02-15', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{invoice.status}}', 'Invoice status', 'Pending', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{invoice.subTotal}}', 'Subtotal before tax', '1000.00', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{invoice.taxAmount}}', 'Tax amount', '60.00', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{invoice.totalAmount}}', 'Total amount', '1060.00', true, NOW()),
    (gen_random_uuid(), 'Invoice', '{{invoice.notes}}', 'Invoice notes', 'Payment within 30 days', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{partner.id}}', 'Partner unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{partner.name}}', 'Partner name', 'TIME dotCom', true, NOW()),
    (gen_random_uuid(), 'Invoice', '{{partner.code}}', 'Partner code', 'TIME', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{#each lineItems}}', 'Start of line items loop', NULL, true, NOW()),
    (gen_random_uuid(), 'Invoice', '{{lineItems.description}}', 'Line item description', 'GPON Installation', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{lineItems.quantity}}', 'Line item quantity', '1', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{lineItems.unitPrice}}', 'Line item unit price', '250.00', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{lineItems.total}}', 'Line item total', '250.00', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{/each}}', 'End of line items loop', NULL, false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{currency invoice.totalAmount}}', 'Format as currency (RM X.XX)', 'RM 1,060.00', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{date invoice.date "dd MMM yyyy"}}', 'Format date', '15 Jan 2024', false, NOW()),
    (gen_random_uuid(), 'Invoice', '{{generatedAt}}', 'Document generation timestamp', '15 Jan 2024 10:30', false, NOW())
ON CONFLICT ("DocumentType", "Key") DO NOTHING;

-- ============================================
-- JobDocket Placeholders
-- ============================================
INSERT INTO "DocumentPlaceholderDefinitions" (
    "Id", "DocumentType", "Key", "Description", "ExampleValue", "IsRequired", "CreatedAt"
) VALUES
    (gen_random_uuid(), 'JobDocket', '{{order.id}}', 'Order unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{order.serviceId}}', 'Service ID / TBBN', 'TBBN12345678', true, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{order.ticketId}}', 'Ticket ID', 'TKT-2024-001', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{order.status}}', 'Order status', 'Assigned', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{order.priority}}', 'Order priority', 'Normal', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{order.appointmentDate}}', 'Appointment date', '2024-01-15', true, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{order.appointmentWindowFrom}}', 'Appointment window start', '09:00', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{order.appointmentWindowTo}}', 'Appointment window end', '12:00', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{order.partnerNotes}}', 'Notes from partner', 'Customer prefers morning', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{order.orderNotesInternal}}', 'Internal notes', NULL, false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{customer.name}}', 'Customer name', 'Ahmad bin Ali', true, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{customer.phone}}', 'Customer phone', '0123456789', true, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{customer.email}}', 'Customer email', 'ahmad@email.com', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{location.buildingName}}', 'Building name', 'Menara ABC', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{location.unitNo}}', 'Unit number', 'A-12-03', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{location.fullAddress}}', 'Full address', 'A-12-03, Menara ABC, Jalan 1/2, 50000 KL', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{installer.name}}', 'Installer name', 'John Doe', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{installer.phone}}', 'Installer phone', '0198765432', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{partner.name}}', 'Partner name', 'TIME dotCom', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{orderType.name}}', 'Order type name', 'New Activation', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{time order.appointmentWindowFrom}}', 'Format time (HH:mm)', '09:00', false, NOW()),
    (gen_random_uuid(), 'JobDocket', '{{generatedAt}}', 'Document generation timestamp', NULL, false, NOW())
ON CONFLICT ("DocumentType", "Key") DO NOTHING;

-- ============================================
-- RmaForm Placeholders
-- ============================================
INSERT INTO "DocumentPlaceholderDefinitions" (
    "Id", "DocumentType", "Key", "Description", "ExampleValue", "IsRequired", "CreatedAt"
) VALUES
    (gen_random_uuid(), 'RmaForm', '{{rma.id}}', 'RMA request unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{rma.number}}', 'RMA number', 'RMA-2024-0001', true, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{rma.requestDate}}', 'RMA request date', '2024-01-15', true, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{rma.reason}}', 'RMA reason', 'Defective unit', false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{rma.status}}', 'RMA status', 'Pending', false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{partner.id}}', 'Partner unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{partner.name}}', 'Partner name', 'TIME dotCom', true, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{#each items}}', 'Start of items loop', NULL, true, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{items.index}}', 'Item index (0-based)', '0', false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{items.serialisedItemId}}', 'Serialised item ID', NULL, false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{items.notes}}', 'Item notes', 'Power LED not working', false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{items.result}}', 'RMA result', 'Pending', false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{/each}}', 'End of items loop', NULL, false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{rowIndex @index}}', 'Row number (1-based)', '1', false, NOW()),
    (gen_random_uuid(), 'RmaForm', '{{generatedAt}}', 'Document generation timestamp', NULL, false, NOW())
ON CONFLICT ("DocumentType", "Key") DO NOTHING;

-- ============================================
-- PurchaseOrder Placeholders
-- ============================================
INSERT INTO "DocumentPlaceholderDefinitions" (
    "Id", "DocumentType", "Key", "Description", "ExampleValue", "IsRequired", "CreatedAt"
) VALUES
    (gen_random_uuid(), 'PurchaseOrder', '{{po.id}}', 'Purchase order unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.number}}', 'PO number', 'PO-2024-0001', true, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.date}}', 'PO date', '2024-01-15', true, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.expectedDeliveryDate}}', 'Expected delivery date', '2024-01-30', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.status}}', 'PO status', 'Approved', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.deliveryAddress}}', 'Delivery address', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.subTotal}}', 'Subtotal before tax', '5000.00', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.taxAmount}}', 'Tax amount', '300.00', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.discountAmount}}', 'Discount amount', '0.00', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.totalAmount}}', 'Total amount', '5300.00', true, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.currency}}', 'Currency', 'MYR', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.paymentTerms}}', 'Payment terms', 'Net 30', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.termsAndConditions}}', 'Terms and conditions', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{po.notes}}', 'Notes', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.id}}', 'Supplier unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.name}}', 'Supplier name', 'ABC Supplies Sdn Bhd', true, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.code}}', 'Supplier code', 'ABC', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.contactPerson}}', 'Contact person', 'Mr. Lee', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.phone}}', 'Supplier phone', '03-12345678', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.email}}', 'Supplier email', 'sales@abc.com', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.address}}', 'Supplier address', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.city}}', 'Supplier city', 'Kuala Lumpur', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.state}}', 'Supplier state', 'Wilayah Persekutuan', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{supplier.postcode}}', 'Supplier postcode', '50000', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{#each items}}', 'Start of items loop', NULL, true, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.index}}', 'Item index (0-based)', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.lineNumber}}', 'Line number', '1', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.description}}', 'Item description', 'ONT Router ZTE F660', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.sku}}', 'SKU / Part number', 'ZTE-F660', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.unit}}', 'Unit of measure', 'pcs', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.quantity}}', 'Quantity', '100', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.unitPrice}}', 'Unit price', '50.00', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.discountPercent}}', 'Discount %', '0', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.taxPercent}}', 'Tax %', '6', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.total}}', 'Line total', '5000.00', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{items.notes}}', 'Item notes', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{/each}}', 'End of items loop', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{currency po.totalAmount}}', 'Format as currency', 'RM 5,300.00', false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{rowIndex @index}}', 'Row number (1-based)', NULL, false, NOW()),
    (gen_random_uuid(), 'PurchaseOrder', '{{generatedAt}}', 'Document generation timestamp', NULL, false, NOW())
ON CONFLICT ("DocumentType", "Key") DO NOTHING;

-- ============================================
-- Quotation Placeholders
-- ============================================
INSERT INTO "DocumentPlaceholderDefinitions" (
    "Id", "DocumentType", "Key", "Description", "ExampleValue", "IsRequired", "CreatedAt"
) VALUES
    (gen_random_uuid(), 'Quotation', '{{quotation.id}}', 'Quotation unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.number}}', 'Quotation number', 'QT-2024-0001', true, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.date}}', 'Quotation date', '2024-01-15', true, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.validUntil}}', 'Valid until date', '2024-02-15', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.status}}', 'Quotation status', 'Sent', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.subject}}', 'Subject / title', 'Solar Panel Installation', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.introduction}}', 'Introduction text', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.subTotal}}', 'Subtotal before tax', '10000.00', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.taxAmount}}', 'Tax amount', '600.00', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.discountAmount}}', 'Discount amount', '500.00', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.totalAmount}}', 'Total amount', '10100.00', true, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.currency}}', 'Currency', 'MYR', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.paymentTerms}}', 'Payment terms', '50% upfront, 50% on completion', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.deliveryTerms}}', 'Delivery terms', '2 weeks from order confirmation', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.termsAndConditions}}', 'Terms and conditions', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{quotation.notes}}', 'Notes', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{customer.name}}', 'Customer name', 'Ahmad bin Ali', true, NOW()),
    (gen_random_uuid(), 'Quotation', '{{customer.phone}}', 'Customer phone', '0123456789', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{customer.email}}', 'Customer email', 'ahmad@email.com', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{customer.address}}', 'Customer address', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{partner.id}}', 'Partner unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{partner.name}}', 'Partner name', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{#each items}}', 'Start of items loop', NULL, true, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.index}}', 'Item index (0-based)', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.lineNumber}}', 'Line number', '1', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.itemType}}', 'Item type', 'Material', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.description}}', 'Item description', 'Solar Panel 400W', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.sku}}', 'SKU / Part number', 'SP-400W', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.unit}}', 'Unit of measure', 'pcs', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.quantity}}', 'Quantity', '10', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.unitPrice}}', 'Unit price', '800.00', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.discountPercent}}', 'Discount %', '5', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.taxPercent}}', 'Tax %', '6', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.total}}', 'Line total', '8000.00', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{items.notes}}', 'Item notes', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{/each}}', 'End of items loop', NULL, false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{currency quotation.totalAmount}}', 'Format as currency', 'RM 10,100.00', false, NOW()),
    (gen_random_uuid(), 'Quotation', '{{generatedAt}}', 'Document generation timestamp', NULL, false, NOW())
ON CONFLICT ("DocumentType", "Key") DO NOTHING;

-- ============================================
-- BOQ Placeholders
-- ============================================
INSERT INTO "DocumentPlaceholderDefinitions" (
    "Id", "DocumentType", "Key", "Description", "ExampleValue", "IsRequired", "CreatedAt"
) VALUES
    (gen_random_uuid(), 'BOQ', '{{project.id}}', 'Project unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.code}}', 'Project code', 'PRJ-2024-001', true, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.name}}', 'Project name', 'Solar Installation - Menara ABC', true, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.description}}', 'Project description', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.projectType}}', 'Project type', 'Solar', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.status}}', 'Project status', 'Planning', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.customerName}}', 'Customer name', 'ABC Corporation', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.customerPhone}}', 'Customer phone', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.customerEmail}}', 'Customer email', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.siteAddress}}', 'Site address', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.city}}', 'City', 'Kuala Lumpur', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.state}}', 'State', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.postcode}}', 'Postcode', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.startDate}}', 'Start date', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.endDate}}', 'End date', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.budgetAmount}}', 'Budget amount', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.contractValue}}', 'Contract value', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{project.currency}}', 'Currency', 'MYR', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{partner.id}}', 'Partner unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{partner.name}}', 'Partner name', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{#each materials}}', 'Start of materials loop', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.index}}', 'Item index (0-based)', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.lineNumber}}', 'Line number', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.section}}', 'Section', 'Solar Panels', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.description}}', 'Description', '400W Monocrystalline Panel', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.sku}}', 'SKU', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.unit}}', 'Unit', 'pcs', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.quantity}}', 'Quantity', '20', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.unitRate}}', 'Unit rate', '800.00', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.total}}', 'Total', '16000.00', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.markupPercent}}', 'Markup %', '15', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.sellingPrice}}', 'Selling price', '18400.00', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{materials.isOptional}}', 'Is optional', 'false', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{/each}}', 'End of materials loop', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{#each labor}}', 'Start of labor loop', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{labor.description}}', 'Labor description', 'Installation labor', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{labor.unit}}', 'Unit', 'day', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{labor.quantity}}', 'Quantity', '5', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{labor.unitRate}}', 'Unit rate', '500.00', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{labor.total}}', 'Total', '2500.00', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{/each}}', 'End of labor loop', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{#each otherItems}}', 'Start of other items loop', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{otherItems.itemType}}', 'Item type', 'Equipment', false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{otherItems.description}}', 'Description', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{otherItems.total}}', 'Total', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{/each}}', 'End of other items loop', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{summary.materialTotal}}', 'Total materials cost', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{summary.laborTotal}}', 'Total labor cost', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{summary.otherTotal}}', 'Total other costs', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{summary.grandTotal}}', 'Grand total (cost)', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{summary.sellingTotal}}', 'Total selling price', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{currency summary.grandTotal}}', 'Format as currency', NULL, false, NOW()),
    (gen_random_uuid(), 'BOQ', '{{generatedAt}}', 'Document generation timestamp', NULL, false, NOW())
ON CONFLICT ("DocumentType", "Key") DO NOTHING;

-- ============================================
-- DeliveryOrder Placeholders
-- ============================================
INSERT INTO "DocumentPlaceholderDefinitions" (
    "Id", "DocumentType", "Key", "Description", "ExampleValue", "IsRequired", "CreatedAt"
) VALUES
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.id}}', 'Delivery order unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.number}}', 'DO number', 'DO-2024-0001', true, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.date}}', 'DO date', '2024-01-15', true, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.type}}', 'DO type', 'Outbound', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.status}}', 'DO status', 'InTransit', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.expectedDeliveryDate}}', 'Expected delivery date', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.actualDeliveryDate}}', 'Actual delivery date', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.deliveryPerson}}', 'Delivery person', 'John Driver', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.vehicleNumber}}', 'Vehicle number', 'WXY 1234', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{deliveryOrder.notes}}', 'Notes', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{recipient.name}}', 'Recipient name', 'Ahmad bin Ali', true, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{recipient.phone}}', 'Recipient phone', '0123456789', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{recipient.email}}', 'Recipient email', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{recipient.address}}', 'Delivery address', '123 Jalan ABC', true, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{recipient.city}}', 'City', 'Kuala Lumpur', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{recipient.state}}', 'State', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{recipient.postcode}}', 'Postcode', '50000', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{#each items}}', 'Start of items loop', NULL, true, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{items.index}}', 'Item index (0-based)', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{items.lineNumber}}', 'Line number', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{items.description}}', 'Item description', 'ONT Router ZTE F660', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{items.sku}}', 'SKU', 'ZTE-F660', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{items.unit}}', 'Unit', 'pcs', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{items.quantity}}', 'Quantity to deliver', '10', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{items.quantityDelivered}}', 'Quantity delivered', '10', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{items.serialNumbers}}', 'Serial numbers', 'SN001, SN002, SN003', false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{items.notes}}', 'Item notes', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{/each}}', 'End of items loop', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{rowIndex @index}}', 'Row number (1-based)', NULL, false, NOW()),
    (gen_random_uuid(), 'DeliveryOrder', '{{generatedAt}}', 'Document generation timestamp', NULL, false, NOW())
ON CONFLICT ("DocumentType", "Key") DO NOTHING;

-- ============================================
-- PaymentReceipt Placeholders
-- ============================================
INSERT INTO "DocumentPlaceholderDefinitions" (
    "Id", "DocumentType", "Key", "Description", "ExampleValue", "IsRequired", "CreatedAt"
) VALUES
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.id}}', 'Payment unique identifier', NULL, false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.number}}', 'Payment/receipt number', 'RCP-2024-0001', true, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.date}}', 'Payment date', '2024-01-15', true, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.type}}', 'Payment type', 'Income', false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.method}}', 'Payment method', 'BankTransfer', false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.amount}}', 'Payment amount', '1060.00', true, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.currency}}', 'Currency', 'MYR', false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.bankAccount}}', 'Bank account used', NULL, false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.bankReference}}', 'Bank reference', 'TRX123456', false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.chequeNumber}}', 'Cheque number', NULL, false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.description}}', 'Description', 'Payment for Invoice INV-2024-0001', false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payment.notes}}', 'Notes', NULL, false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{payer.name}}', 'Payer/Payee name', 'TIME dotCom Berhad', true, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{currency payment.amount}}', 'Format as currency', 'RM 1,060.00', false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{uppercase payment.method}}', 'Uppercase text', 'BANKTRANSFER', false, NOW()),
    (gen_random_uuid(), 'PaymentReceipt', '{{generatedAt}}', 'Document generation timestamp', NULL, false, NOW())
ON CONFLICT ("DocumentType", "Key") DO NOTHING;

-- ============================================
-- Verification
-- ============================================
DO $$
DECLARE
    v_total_count INT;
    v_by_type RECORD;
BEGIN
    SELECT COUNT(*) INTO v_total_count FROM "DocumentPlaceholderDefinitions";
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Document Placeholder Seeding Complete';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Total Placeholders: %', v_total_count;
    RAISE NOTICE '----------------------------------------';
    RAISE NOTICE 'By Document Type:';
    
    FOR v_by_type IN 
        SELECT "DocumentType", COUNT(*) as count 
        FROM "DocumentPlaceholderDefinitions" 
        GROUP BY "DocumentType" 
        ORDER BY "DocumentType"
    LOOP
        RAISE NOTICE '  %: %', v_by_type."DocumentType", v_by_type.count;
    END LOOP;
    
    RAISE NOTICE '========================================';
END $$;

