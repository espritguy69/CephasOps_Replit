import apiClient from './client';
import type {
  EmailRule,
  EmailRuleFormData,
  EmailMailbox,
  EmailMailboxFormData,
  VipEmail,
  ParserTemplate,
  PollResult
} from '../types/email';

// Backend DTO types (matching backend structure)
interface BackendEmailRuleDto {
  id: string;
  companyId?: string | null;
  emailAccountId?: string | null;
  fromAddressPattern?: string | null;
  domainPattern?: string | null;
  subjectContains?: string | null;
  isVip: boolean;
  targetDepartmentId?: string | null;
  targetUserId?: string | null;
  actionType: string;
  priority: number;
  isActive: boolean;
  description?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

interface CreateEmailRuleDto {
  emailAccountId?: string | null;
  fromAddressPattern?: string | null;
  domainPattern?: string | null;
  subjectContains?: string | null;
  isVip?: boolean;
  targetDepartmentId?: string | null;
  targetUserId?: string | null;
  actionType: string;
  priority: number;
  isActive: boolean;
  description?: string | null;
}

interface BackendEmailAccountDto {
  id: string;
  companyId?: string | null;
  name: string;
  provider: string;
  host?: string | null;
  port?: number | null;
  useSsl?: boolean;
  username: string;
  pollIntervalSec?: number | null;
  isActive: boolean;
  lastPolledAt?: string | null;
  defaultDepartmentId?: string | null;
  defaultDepartmentName?: string | null;
  defaultParserTemplateId?: string | null;
  defaultParserTemplateName?: string | null;
  smtpHost?: string | null;
  smtpPort?: number | null;
  smtpUsername?: string | null;
  smtpPassword?: string | null;
  smtpUseSsl?: boolean;
  smtpUseTls?: boolean;
  smtpFromAddress?: string | null;
  smtpFromName?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

const secondsToMinutes = (seconds?: number | null): number | undefined => {
  if (typeof seconds !== 'number' || Number.isNaN(seconds)) {
    return undefined;
  }
  return Math.max(1, Math.round(seconds / 60));
};

const toNumberOrUndefined = (value: string | number | undefined): number | undefined => {
  if (value === undefined) return undefined;
  const numeric = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(numeric) ? numeric : undefined;
};

const transformEmailAccountFromBackend = (dto: BackendEmailAccountDto): EmailMailbox => ({
  id: dto.id,
  companyId: dto.companyId ?? undefined,
  name: dto.name,
  provider: (dto.provider as EmailMailbox['provider']) ?? 'POP3',
  host: dto.host ?? '',
  port: dto.port ?? undefined,
  useSsl: dto.useSsl ?? false,
  username: dto.username,
  emailAddress: dto.smtpFromAddress || dto.username,
  pollIntervalMinutes: secondsToMinutes(dto.pollIntervalSec),
  isActive: dto.isActive,
  defaultDepartmentId: dto.defaultDepartmentId ?? undefined,
  defaultDepartmentName: dto.defaultDepartmentName ?? undefined,
  defaultParserTemplateId: dto.defaultParserTemplateId ?? undefined,
  defaultParserTemplateName: dto.defaultParserTemplateName ?? undefined,
  smtpHost: dto.smtpHost ?? undefined,
  smtpPort: dto.smtpPort ?? undefined,
  smtpUsername: dto.smtpUsername ?? undefined,
  smtpUseSsl: dto.smtpUseSsl ?? false,
  smtpUseTls: dto.smtpUseTls ?? false,
  smtpFromAddress: dto.smtpFromAddress ?? undefined,
  smtpFromName: dto.smtpFromName ?? undefined,
  createdAt: dto.createdAt,
  updatedAt: dto.updatedAt
});

const toGuidOrNull = (value?: string): string | null => {
  if (!value) {
    return null;
  }
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
};

const minutesToSeconds = (minutes?: number | string): number => {
  const numericMinutes = typeof minutes === 'number' ? minutes : Number(minutes);
  if (!Number.isFinite(numericMinutes) || numericMinutes <= 0) {
    return 60;
  }
  return Math.max(60, Math.round(numericMinutes) * 60);
};

export const buildEmailAccountPayload = (
  formData: EmailMailboxFormData,
  options: { includePasswords?: boolean } = {}
): Record<string, any> => {
  const payload: Record<string, any> = {
    name: formData.name,
    provider: formData.provider,
    host: formData.host || undefined,
    port: toNumberOrUndefined(formData.port),
    useSsl: formData.useSsl,
    username: formData.username,
    pollIntervalSec: minutesToSeconds(formData.pollIntervalMinutes),
    isActive: formData.isActive,
    defaultDepartmentId: toGuidOrNull(formData.defaultDepartmentId),
    defaultParserTemplateId: toGuidOrNull(formData.defaultParserTemplateId),
    smtpHost: formData.smtpHost || undefined,
    smtpPort: toNumberOrUndefined(formData.smtpPort),
    smtpUsername: formData.smtpUsername || undefined,
    smtpUseSsl: formData.smtpUseSsl,
    smtpUseTls: formData.smtpUseTls,
    smtpFromAddress: formData.smtpFromAddress || undefined,
    smtpFromName: formData.smtpFromName || undefined
  };

  const includePasswords = options.includePasswords ?? false;

  if (includePasswords || (formData.password && formData.password.trim().length > 0)) {
    payload.password = formData.password;
  }

  if (includePasswords || (formData.smtpPassword && formData.smtpPassword.trim().length > 0)) {
    payload.smtpPassword = formData.smtpPassword;
  }

  return payload;
};

/**
 * Email Account API
 */
export const getEmailAccounts = async (): Promise<EmailMailbox[]> => {
  const response = await apiClient.get<BackendEmailAccountDto[]>(`/email-accounts`);
  if (!Array.isArray(response)) {
    return [];
  }
  return response.map(transformEmailAccountFromBackend);
};

export const getEmailAccount = async (accountId: string): Promise<EmailMailbox> => {
  const response = await apiClient.get<BackendEmailAccountDto>(`/email-accounts/${accountId}`);
  return transformEmailAccountFromBackend(response);
};

export const createEmailAccount = async (accountData: EmailMailboxFormData): Promise<EmailMailbox> => {
  const payload = buildEmailAccountPayload(accountData, { includePasswords: true });
  const response = await apiClient.post<BackendEmailAccountDto>(`/email-accounts`, payload);
  return transformEmailAccountFromBackend(response);
};

export const updateEmailAccount = async (
  accountId: string,
  accountData: EmailMailboxFormData
): Promise<EmailMailbox> => {
  const payload = buildEmailAccountPayload(accountData, { includePasswords: false });
  const response = await apiClient.put<BackendEmailAccountDto>(`/email-accounts/${accountId}`, payload);
  return transformEmailAccountFromBackend(response);
};

export const deleteEmailAccount = async (accountId: string): Promise<void> => {
  await apiClient.delete(`/email-accounts/${accountId}`);
};

export const testEmailAccountConnection = async (accountId: string): Promise<{ success: boolean; message?: string }> => {
  const response = await apiClient.post(`/email-accounts/${accountId}/test-connection`);
  return response as { success: boolean; message?: string };
};

export const pollEmailAccount = async (accountId: string): Promise<PollResult> => {
  const response = await apiClient.post(`/email-accounts/${accountId}/poll`);
  return response as PollResult;
};

export const pollAllEmailAccounts = async (): Promise<PollResult[]> => {
  const response = await apiClient.post(`/email-accounts/poll-all`);
  return Array.isArray(response) ? (response as PollResult[]) : [];
};

/**
 * Email Rules API
 */
export const getEmailRules = async (): Promise<EmailRule[]> => {
  const response = await apiClient.get(`/email-rules`);
  // Transform backend format to frontend format
  if (Array.isArray(response)) {
    return (response as BackendEmailRuleDto[]).map(transformBackendToFrontend);
  }
  return [];
};

export const getEmailRule = async (ruleId: string): Promise<EmailRule> => {
  const response = await apiClient.get(`/email-rules/${ruleId}`);
  return transformBackendToFrontend(response as BackendEmailRuleDto);
};

export const createEmailRule = async (ruleData: EmailRuleFormData): Promise<EmailRule> => {
  // Transform frontend format to backend format
  const backendData = transformFrontendToBackend(ruleData);
  const response = await apiClient.post(`/email-rules`, backendData);
  return transformBackendToFrontend(response as BackendEmailRuleDto);
};

export const updateEmailRule = async (ruleId: string, ruleData: Partial<EmailRuleFormData>): Promise<EmailRule> => {
  // Transform frontend format to backend format
  const backendData = transformFrontendToBackend(ruleData as EmailRuleFormData);
  const response = await apiClient.put(`/email-rules/${ruleId}`, backendData);
  return transformBackendToFrontend(response as BackendEmailRuleDto);
};

export const deleteEmailRule = async (ruleId: string): Promise<void> => {
  await apiClient.delete(`/email-rules/${ruleId}`);
};

// Helper functions to transform between frontend and backend formats
function transformBackendToFrontend(backendRule: BackendEmailRuleDto | null): EmailRule {
  if (!backendRule) {
    throw new Error('Invalid email rule data');
  }
  
  const actionType = backendRule.actionType || 'Process';
  const validActions: EmailRule['action'][] = [
    'Process',
    'Ignore',
    'MarkVipOnly',
    'RouteToDepartment',
    'RouteToUser',
    'MarkVipAndRouteToDepartment',
    'MarkVipAndRouteToUser'
  ];
  const action = validActions.includes(actionType as EmailRule['action'])
    ? (actionType as EmailRule['action'])
    : 'Process';

  return {
    id: backendRule.id,
    companyId: backendRule.companyId || undefined,
    name: backendRule.description || '', // Backend uses Description as name
    description: backendRule.description || '',
    senderPattern: backendRule.fromAddressPattern || '',
    domainPattern: backendRule.domainPattern || '',
    subjectPattern: backendRule.subjectContains || '',
    bodyPattern: '', // Backend doesn't support body pattern
    action,
    targetDepartmentId: backendRule.targetDepartmentId || null,
    targetUserId: backendRule.targetUserId || null,
    isVip: backendRule.isVip || false,
    autoApprove: false, // Not supported in EmailRule, only in ParserTemplate
    priority: backendRule.priority || 0,
    isActive: backendRule.isActive !== false,
    emailAccountId: backendRule.emailAccountId || null,
    createdAt: backendRule.createdAt,
    updatedAt: backendRule.updatedAt
  };
}

function transformFrontendToBackend(frontendRule: EmailRuleFormData): CreateEmailRuleDto {
  return {
    emailAccountId: frontendRule.emailAccountId || null,
    fromAddressPattern: frontendRule.senderPattern || null,
    domainPattern: frontendRule.domainPattern || null,
    subjectContains: frontendRule.subjectPattern || null,
    isVip: frontendRule.isVip || false,
    targetDepartmentId: frontendRule.targetDepartmentId || null,
    targetUserId: frontendRule.targetUserId || null,
    actionType: frontendRule.action || 'Process',
    priority: frontendRule.priority || 0,
    isActive: frontendRule.isActive !== false,
    description: frontendRule.name || frontendRule.description || '' // Use name as description
  };
}

/**
 * VIP Emails API
 */
export const getVipEmails = async (): Promise<VipEmail[]> => {
  const response = await apiClient.get(`/vip-emails`);
  return response as VipEmail[];
};

export const getVipEmail = async (vipEmailId: string): Promise<VipEmail> => {
  const response = await apiClient.get(`/vip-emails/${vipEmailId}`);
  return response as VipEmail;
};

export const createVipEmail = async (vipEmailData: Partial<VipEmail>): Promise<VipEmail> => {
  const response = await apiClient.post(`/vip-emails`, vipEmailData);
  return response as VipEmail;
};

export const updateVipEmail = async (vipEmailId: string, vipEmailData: Partial<VipEmail>): Promise<VipEmail> => {
  const response = await apiClient.put(`/vip-emails/${vipEmailId}`, vipEmailData);
  return response as VipEmail;
};

export const deleteVipEmail = async (vipEmailId: string): Promise<void> => {
  await apiClient.delete(`/vip-emails/${vipEmailId}`);
};

/**
 * VIP Groups API
 */
export const getVipGroups = async (): Promise<any[]> => {
  const response = await apiClient.get(`/vip-groups`);
  return response as any[];
};

export const getVipGroup = async (groupId: string): Promise<any> => {
  const response = await apiClient.get(`/vip-groups/${groupId}`);
  return response;
};

export const getVipGroupByCode = async (code: string): Promise<any> => {
  const response = await apiClient.get(`/vip-groups/by-code/${code}`);
  return response;
};

export const createVipGroup = async (groupData: any): Promise<any> => {
  const response = await apiClient.post(`/vip-groups`, groupData);
  return response;
};

export const updateVipGroup = async (groupId: string, groupData: any): Promise<any> => {
  const response = await apiClient.put(`/vip-groups/${groupId}`, groupData);
  return response;
};

export const deleteVipGroup = async (groupId: string): Promise<void> => {
  await apiClient.delete(`/vip-groups/${groupId}`);
};

/**
 * Parser Templates API
 */
export const getParserTemplates = async (): Promise<ParserTemplate[]> => {
  const response = await apiClient.get(`/parser-templates`);
  return response as ParserTemplate[];
};

export const getParserTemplate = async (templateId: string): Promise<ParserTemplate> => {
  const response = await apiClient.get(`/parser-templates/${templateId}`);
  return response as ParserTemplate;
};

export const getParserTemplateByCode = async (code: string): Promise<ParserTemplate> => {
  const response = await apiClient.get(`/parser-templates/by-code/${code}`);
  return response as ParserTemplate;
};

export const createParserTemplate = async (templateData: Partial<ParserTemplate>): Promise<ParserTemplate> => {
  const response = await apiClient.post(`/parser-templates`, templateData);
  return response as ParserTemplate;
};

export const updateParserTemplate = async (templateId: string, templateData: Partial<ParserTemplate>): Promise<ParserTemplate> => {
  const response = await apiClient.put(`/parser-templates/${templateId}`, templateData);
  return response as ParserTemplate;
};

export const deleteParserTemplate = async (templateId: string): Promise<void> => {
  await apiClient.delete(`/parser-templates/${templateId}`);
};

export const toggleParserTemplateAutoApprove = async (templateId: string, autoApprove: boolean): Promise<ParserTemplate> => {
  const response = await apiClient.post(`/parser-templates/${templateId}/toggle-auto-approve`, { autoApprove });
  return response as ParserTemplate;
};

export interface ParserTemplateTestData {
  fromAddress: string;
  subject: string;
  body?: string;
  hasAttachments: boolean;
  attachmentFileNames?: string[];
}

export interface TemplateMatchDetails {
  fromAddressMatched: boolean;
  subjectMatched: boolean;
  fromAddressPattern?: string;
  subjectPattern?: string;
  priority: number;
}

export interface ParserTemplateTestResult {
  matched: boolean;
  matchedTemplate?: ParserTemplate;
  matchDetails?: TemplateMatchDetails;
  errorMessage?: string;
  extractedData?: Record<string, any>;
}

export const testParserTemplate = async (templateId: string, testData: ParserTemplateTestData): Promise<ParserTemplateTestResult> => {
  const response = await apiClient.post<ParserTemplateTestResult>(`/parser-templates/${templateId}/test`, testData);
  return response;
};

/**
 * ParseSession API
 */
export const getParseSessions = async (filters: Record<string, any> = {}): Promise<any[]> => {
  const response = await apiClient.get(`/parser/sessions`, { params: filters });
  return response as any[];
};

export const getParseSession = async (sessionId: string): Promise<any> => {
  const response = await apiClient.get(`/parser/sessions/${sessionId}`);
  return response;
};

export const approveParseSession = async (sessionId: string, orderData: Record<string, any> = {}): Promise<any> => {
  const response = await apiClient.post(`/parser/sessions/${sessionId}/approve`, orderData);
  return response;
};

export const rejectParseSession = async (sessionId: string, reason: string): Promise<any> => {
  const response = await apiClient.post(`/parser/sessions/${sessionId}/reject`, { reason });
  return response;
};

/**
 * Email System Settings API
 */
export interface EmailSystemSettings {
  pollIntervalMinutes: number;
  retentionHours: number;
}

export const getEmailSystemSettings = async (): Promise<EmailSystemSettings> => {
  const response = await apiClient.get<EmailSystemSettings>('/email-accounts/settings');
  return response.data?.data || response.data || response;
};

export const updateEmailSystemSettings = async (settings: EmailSystemSettings): Promise<void> => {
  await apiClient.put('/email-accounts/settings', settings);
};

