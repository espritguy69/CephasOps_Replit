/**
 * Companies Types - Shared type definitions for Companies module
 */

export interface Company {
  id: string;
  legalName: string;
  shortName: string;
  vertical: string;
  registrationNo?: string;
  taxId?: string;
  address?: string;
  phone?: string;
  email?: string;
  isActive: boolean;
  createdAt?: string;
  // Locale Settings
  defaultTimezone: string;
  defaultDateFormat: string;
  defaultTimeFormat: string;
  defaultCurrency: string;
  defaultLocale: string;
}

export interface CreateCompanyRequest {
  legalName: string;
  shortName: string;
  vertical: string;
  registrationNo?: string;
  taxId?: string;
  address?: string;
  phone?: string;
  email?: string;
  isActive?: boolean;
  // Locale Settings
  defaultTimezone?: string;
  defaultDateFormat?: string;
  defaultTimeFormat?: string;
  defaultCurrency?: string;
  defaultLocale?: string;
}

export interface UpdateCompanyRequest {
  legalName?: string;
  shortName?: string;
  vertical?: string;
  registrationNo?: string;
  taxId?: string;
  address?: string;
  phone?: string;
  email?: string;
  isActive?: boolean;
  // Locale Settings
  defaultTimezone?: string;
  defaultDateFormat?: string;
  defaultTimeFormat?: string;
  defaultCurrency?: string;
  defaultLocale?: string;
}

// Common timezone options for dropdown
export const TIMEZONE_OPTIONS = [
  { value: 'Asia/Kuala_Lumpur', label: 'Malaysia (GMT+8)' },
  { value: 'Asia/Singapore', label: 'Singapore (GMT+8)' },
  { value: 'Asia/Hong_Kong', label: 'Hong Kong (GMT+8)' },
  { value: 'Asia/Jakarta', label: 'Indonesia - Jakarta (GMT+7)' },
  { value: 'Asia/Bangkok', label: 'Thailand (GMT+7)' },
  { value: 'Asia/Manila', label: 'Philippines (GMT+8)' },
  { value: 'Asia/Tokyo', label: 'Japan (GMT+9)' },
  { value: 'Asia/Seoul', label: 'South Korea (GMT+9)' },
  { value: 'Australia/Perth', label: 'Australia - Perth (GMT+8)' },
  { value: 'Australia/Sydney', label: 'Australia - Sydney (GMT+10/11)' },
  { value: 'Europe/London', label: 'UK (GMT+0/1)' },
  { value: 'America/New_York', label: 'US - Eastern (GMT-5/-4)' },
  { value: 'America/Los_Angeles', label: 'US - Pacific (GMT-8/-7)' },
  { value: 'UTC', label: 'UTC (GMT+0)' },
] as const;

// Date format options
export const DATE_FORMAT_OPTIONS = [
  { value: 'dd/MM/yyyy', label: 'DD/MM/YYYY (e.g., 25/12/2024)' },
  { value: 'MM/dd/yyyy', label: 'MM/DD/YYYY (e.g., 12/25/2024)' },
  { value: 'yyyy-MM-dd', label: 'YYYY-MM-DD (e.g., 2024-12-25)' },
  { value: 'd MMM yyyy', label: 'D MMM YYYY (e.g., 25 Dec 2024)' },
  { value: 'MMM d, yyyy', label: 'MMM D, YYYY (e.g., Dec 25, 2024)' },
] as const;

// Time format options
export const TIME_FORMAT_OPTIONS = [
  { value: 'hh:mm a', label: '12-hour (e.g., 02:30 PM)' },
  { value: 'HH:mm', label: '24-hour (e.g., 14:30)' },
] as const;

// Currency options
export const CURRENCY_OPTIONS = [
  { value: 'MYR', label: 'Malaysian Ringgit (MYR)' },
  { value: 'SGD', label: 'Singapore Dollar (SGD)' },
  { value: 'USD', label: 'US Dollar (USD)' },
  { value: 'EUR', label: 'Euro (EUR)' },
  { value: 'GBP', label: 'British Pound (GBP)' },
  { value: 'AUD', label: 'Australian Dollar (AUD)' },
  { value: 'IDR', label: 'Indonesian Rupiah (IDR)' },
  { value: 'THB', label: 'Thai Baht (THB)' },
  { value: 'PHP', label: 'Philippine Peso (PHP)' },
] as const;

// Locale options
export const LOCALE_OPTIONS = [
  { value: 'en-MY', label: 'English (Malaysia)' },
  { value: 'en-SG', label: 'English (Singapore)' },
  { value: 'en-US', label: 'English (US)' },
  { value: 'en-GB', label: 'English (UK)' },
  { value: 'en-AU', label: 'English (Australia)' },
  { value: 'ms-MY', label: 'Bahasa Malaysia' },
  { value: 'id-ID', label: 'Bahasa Indonesia' },
  { value: 'th-TH', label: 'Thai' },
  { value: 'zh-CN', label: 'Chinese (Simplified)' },
] as const;

