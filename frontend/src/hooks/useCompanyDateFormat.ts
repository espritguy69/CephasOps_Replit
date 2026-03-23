import { useEffect } from 'react';
import { useCompanySettings } from '../contexts/CompanySettingsContext';
import { 
  setCompanyTimezone, 
  setCompanyLocale, 
  setCompanyDateFormat, 
  setCompanyTimeFormat,
  formatDate,
  formatDateTimeInTimezone,
  formatTimeInTimezone,
  getCompanyTimezone,
  getCompanyLocale
} from '../utils/dateHelpers';

/**
 * Hook that syncs company settings with the dateHelpers utility
 * and provides formatted date/time functions
 * 
 * Usage:
 * const { formatDate, formatDateTime, formatTime, timezone, locale } = useCompanyDateFormat();
 */
export function useCompanyDateFormat() {
  const { settings, loading } = useCompanySettings();

  // Sync settings with dateHelpers when they change
  useEffect(() => {
    if (!loading) {
      setCompanyTimezone(settings.timezone);
      setCompanyLocale(settings.locale);
      setCompanyDateFormat(settings.dateFormat);
      setCompanyTimeFormat(settings.timeFormat);
    }
  }, [settings, loading]);

  return {
    // Formatting functions
    formatDate,
    formatDateTime: formatDateTimeInTimezone,
    formatTime: formatTimeInTimezone,
    
    // Current settings
    timezone: settings.timezone,
    locale: settings.locale,
    dateFormat: settings.dateFormat,
    timeFormat: settings.timeFormat,
    currency: settings.currency,
    
    // Utility getters
    getTimezone: getCompanyTimezone,
    getLocale: getCompanyLocale,
    
    // Loading state
    loading
  };
}

/**
 * Format currency with company settings
 */
export function useCompanyCurrency() {
  const { settings } = useCompanySettings();

  const formatCurrency = (amount: number | null | undefined): string => {
    if (amount === null || amount === undefined) return '-';
    
    try {
      return new Intl.NumberFormat(settings.locale, {
        style: 'currency',
        currency: settings.currency,
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
      }).format(amount);
    } catch {
      return `${settings.currency} ${amount.toFixed(2)}`;
    }
  };

  const formatNumber = (value: number | null | undefined, decimals = 2): string => {
    if (value === null || value === undefined) return '-';
    
    try {
      return new Intl.NumberFormat(settings.locale, {
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals
      }).format(value);
    } catch {
      return value.toFixed(decimals);
    }
  };

  return {
    formatCurrency,
    formatNumber,
    currency: settings.currency,
    locale: settings.locale
  };
}

export default useCompanyDateFormat;

