export const DEFAULT_ALLOWED_VARIABLES = [
  'CustomerName',
  'Address',
  'GeranNo',
  'TitleNumber',
  'LotCode',
  'AccountNo',
  'Mukim',
  'TypeOfTitle',
];

export const DEFAULT_RECOMMENDED_VARIABLES = [
  'CustomerName',
  'Address',
  'AccountNo',
];

export const DEFAULT_TEMPLATE_CATEGORIES = [
  'Invoice',
  'JobDocket',
  'RmaForm',
  'PurchaseOrder',
  'Quotation',
  'BOQ',
  'DeliveryOrder',
  'PaymentReceipt',
];

export const OUTPUT_TYPES = ['PDF', 'DOCX', 'HTML'] as const;
export type OutputType = typeof OUTPUT_TYPES[number];

export const CONTENT_FORMATS = ['Markdown', 'HTML'] as const;
export type ContentFormat = typeof CONTENT_FORMATS[number];

export const DEFAULT_TEST_RENDER_DATA = {
  CustomerName: 'Aisyah Binti Ali',
  Address: '12 Jalan Merdeka, Kuala Lumpur',
  GeranNo: 'GRN-23901',
  TitleNumber: 'TN-88421',
  LotCode: 'LOT-22A',
  AccountNo: 'ACCT-009821',
  Mukim: 'Mukim Setapak',
  TypeOfTitle: 'Leasehold',
};
