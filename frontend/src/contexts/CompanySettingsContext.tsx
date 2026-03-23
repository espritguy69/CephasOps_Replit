import React, { createContext, useContext, useState, useEffect, useCallback, ReactNode } from 'react';
import { getCompanies } from '../api/companies';
import { useAuth } from '../contexts/AuthContext';
import type { Company } from '../types/companies';

/**
 * Company Settings Context
 * Provides company-wide locale settings (timezone, date format, currency, etc.)
 * to all components in the application.
 */

interface CompanySettings {
  timezone: string;
  dateFormat: string;
  timeFormat: string;
  currency: string;
  locale: string;
  companyName: string;
}

interface CompanySettingsContextType {
  settings: CompanySettings;
  loading: boolean;
  error: string | null;
  refreshSettings: () => Promise<void>;
}

// Default settings (Malaysian)
const defaultSettings: CompanySettings = {
  timezone: 'Asia/Kuala_Lumpur',
  dateFormat: 'dd/MM/yyyy',
  timeFormat: 'hh:mm a',
  currency: 'MYR',
  locale: 'en-MY',
  companyName: ''
};

const CompanySettingsContext = createContext<CompanySettingsContextType>({
  settings: defaultSettings,
  loading: true,
  error: null,
  refreshSettings: async () => {}
});

export const useCompanySettings = (): CompanySettingsContextType => {
  const context = useContext(CompanySettingsContext);
  if (!context) {
    throw new Error('useCompanySettings must be used within a CompanySettingsProvider');
  }
  return context;
};

interface CompanySettingsProviderProps {
  children: ReactNode;
}

export const CompanySettingsProvider: React.FC<CompanySettingsProviderProps> = ({ children }) => {
  const [settings, setSettings] = useState<CompanySettings>(defaultSettings);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { isAuthenticated } = useAuth();

  const loadSettings = useCallback(async () => {
    // Only load companies if user is authenticated
    if (!isAuthenticated) {
      setLoading(false);
      setError(null);
      // Keep default settings when not authenticated
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const companies = await getCompanies();
      
      if (companies.length > 0) {
        const company: Company = companies[0];
        setSettings({
          timezone: company.defaultTimezone || defaultSettings.timezone,
          dateFormat: company.defaultDateFormat || defaultSettings.dateFormat,
          timeFormat: company.defaultTimeFormat || defaultSettings.timeFormat,
          currency: company.defaultCurrency || defaultSettings.currency,
          locale: company.defaultLocale || defaultSettings.locale,
          companyName: company.shortName || company.legalName || ''
        });
      }
    } catch (err: any) {
      // Handle 401 errors gracefully (expected when not authenticated)
      if (err?.status === 401) {
        console.log('Not authenticated, using default company settings');
        setError(null);
        // Keep default settings
      } else {
        console.error('Failed to load company settings:', err);
        setError('Failed to load company settings');
        // Keep default settings on error
      }
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated]);

  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  const refreshSettings = useCallback(async () => {
    await loadSettings();
  }, [loadSettings]);

  return (
    <CompanySettingsContext.Provider value={{ settings, loading, error, refreshSettings }}>
      {children}
    </CompanySettingsContext.Provider>
  );
};

export default CompanySettingsContext;

