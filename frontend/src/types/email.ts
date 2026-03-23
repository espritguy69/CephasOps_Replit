/**
 * Email Types - Shared type definitions for Email features
 */

// ============================================================================
// Email Mailbox Types
// ============================================================================

export interface EmailMailbox {
  id: string;
  companyId?: string;
  name: string;
  emailAddress: string;
  username?: string;
  provider: 'POP3' | 'IMAP' | 'O365' | 'Gmail';
  host?: string;
  port?: number;
  useSsl?: boolean;
  pollIntervalMinutes?: number;
  isActive: boolean;
  defaultDepartmentId?: string;
  defaultDepartmentName?: string;
  defaultParserTemplateId?: string | null;
  defaultParserTemplateName?: string | null;
  // SMTP
  smtpHost?: string;
  smtpPort?: number;
  smtpUsername?: string;
  smtpUseSsl?: boolean;
  smtpUseTls?: boolean;
  smtpFromAddress?: string;
  smtpFromName?: string;
  // Audit
  createdAt?: string;
  updatedAt?: string;
}

export interface EmailMailboxFormData {
  name: string;
  emailAddress: string;
  provider: string;
  host: string;
  port: number;
  useSsl: boolean;
  username: string;
  password: string;
  pollIntervalMinutes: number;
  isActive: boolean;
  defaultDepartmentId: string;
  defaultParserTemplateId: string;
  smtpHost: string;
  smtpPort: number;
  smtpUsername: string;
  smtpPassword: string;
  smtpUseSsl: boolean;
  smtpUseTls: boolean;
  smtpFromAddress: string;
  smtpFromName: string;
}

export interface ConnectionTestResult {
  status: 'success' | 'error';
  message?: string;
  incomingSuccess?: boolean;
  incomingProtocol?: string;
  incomingResponseTimeMs?: number;
  incomingError?: string;
  smtpSuccess?: boolean;
  smtpResponseTimeMs?: number;
  smtpError?: string;
}

export interface PollResult {
  emailAccountId?: string;
  emailAccountName?: string;
  success: boolean;
  errorMessage?: string;
  emailsFetched?: number;
  parseSessionsCreated?: number;
  draftsCreated?: number;
  errors?: number;
  processedEmails?: string[];
  processedAt?: string;
}

// ============================================================================
// Email Rule Types
// ============================================================================

export interface EmailRule {
  id: string;
  companyId?: string;
  name: string;
  description?: string;
  senderPattern?: string;
  domainPattern?: string;
  subjectPattern?: string;
  bodyPattern?: string;
  action: 'Process' | 'Ignore' | 'MarkVipOnly' | 'RouteToDepartment' | 'RouteToUser' | 'MarkVipAndRouteToDepartment' | 'MarkVipAndRouteToUser';
  targetDepartmentId?: string | null;
  targetUserId?: string | null;
  isVip?: boolean;
  autoApprove: boolean;
  priority: number;
  isActive: boolean;
  emailAccountId?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

export interface EmailRuleFormData {
  name: string;
  description: string;
  senderPattern: string;
  domainPattern?: string;
  subjectPattern: string;
  bodyPattern: string;
  action: string;
  targetDepartmentId: string | null;
  targetUserId: string | null;
  isVip?: boolean;
  autoApprove: boolean;
  priority: number;
  isActive: boolean;
  emailAccountId?: string | null;
}

// ============================================================================
// VIP Email Types
// ============================================================================

export interface VipEmail {
  id: string;
  companyId?: string;
  emailAddress: string;
  displayName?: string;
  description?: string;
  vipGroupId?: string;
  vipGroupName?: string;
  notifyUserId?: string;
  notifyUserName?: string;
  notifyRole?: string;
  departmentId?: string;
  departmentName?: string;
  isActive: boolean;
  createdByUserId?: string;
  updatedByUserId?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface VipEmailFormData {
  emailAddress: string;
  displayName: string;
  vipGroupId: string;
  notifyUserId: string;
  notifyRole: string;
  departmentId: string;
  description: string;
  isActive: boolean;
}

// ============================================================================
// VIP Group Types
// ============================================================================

export interface VipGroupEmail {
  id: string;
  emailAddress: string;
  displayName?: string;
  isActive: boolean;
}

export interface VipGroup {
  id: string;
  companyId?: string;
  name: string;
  code: string;
  description?: string;
  notifyDepartmentId?: string;
  departmentName?: string;
  notifyUserId?: string;
  notifyUserName?: string;
  notifyHodUserId?: string;
  hodUserName?: string;
  notifyRole?: string;
  priority: number;
  isActive: boolean;
  emailAddresses?: VipGroupEmail[];
  createdAt?: string;
  updatedAt?: string;
}

export interface VipGroupFormData {
  name: string;
  code: string;
  description: string;
  notifyDepartmentId: string;
  notifyUserId: string;
  notifyHodUserId: string;
  notifyRole: string;
  priority: number;
  isActive: boolean;
  emailAddresses: string[];
}

// ============================================================================
// Parser Template Types
// ============================================================================

export interface ParserTemplate {
  id: string;
  companyId?: string;
  name: string;
  code: string;
  emailAccountId?: string;
  emailAccountName?: string;
  partnerPattern?: string;
  subjectPattern?: string;
  orderTypeCode?: string;
  defaultDepartmentId?: string;
  autoApprove: boolean;
  priority: number;
  isActive: boolean;
  description?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface ParserTemplateFormData {
  name: string;
  code: string;
  emailAccountId: string;
  partnerPattern: string;
  subjectPattern: string;
  orderTypeCode: string;
  defaultDepartmentId: string;
  autoApprove: boolean;
  priority: number;
  isActive: boolean;
  description: string;
}

// ============================================================================
// Department & User Types (for dropdowns)
// ============================================================================

export interface Department {
  id: string;
  name: string;
  code?: string;
  isActive: boolean;
}

export interface User {
  id: string;
  email: string;
  name?: string;
  fullName?: string;
}

// ============================================================================
// Table Column Type
// ============================================================================

export interface TableColumn<T> {
  key: keyof T | string;
  label: string;
  sortable?: boolean;
  render?: (value: unknown, item: T) => React.ReactNode;
}

// ============================================================================
// Select Option Type
// ============================================================================

export interface SelectOption {
  value: string;
  label: string;
}

// ============================================================================
// Email Message Types (for Mail Viewer)
// ============================================================================

export interface EmailMessage {
  id: string;
  emailAccountId: string;
  messageId: string;
  fromAddress: string;
  toAddresses: string;
  ccAddresses?: string;
  subject: string;
  bodyPreview?: string;
  bodyText?: string;
  bodyHtml?: string;
  receivedAt: string;
  sentAt?: string;
  direction: string;
  hasAttachments: boolean;
  parserStatus: string;
  parserError?: string;
  isVip: boolean;
  departmentId?: string;
  companyId?: string;
  createdAt: string;
  updatedAt: string;
  expiresAt: string;
  isExpired: boolean;
  attachments?: EmailAttachment[];
}

export interface EmailAttachment {
  id: string;
  emailMessageId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  isInline: boolean;
  contentId?: string;
  expiresAt: string;
  isExpired: boolean;
}

