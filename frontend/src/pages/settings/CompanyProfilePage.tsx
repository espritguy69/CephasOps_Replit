import React, { useEffect, useState } from 'react';
import { Globe, Clock, Calendar, DollarSign } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Button, Card, LoadingSpinner, TextInput, useToast } from '../../components/ui';
import { createCompany, getCompanies, updateCompany } from '../../api/companies';
import { getVerticals } from '../../api/verticals';
import type { Company, CreateCompanyRequest, UpdateCompanyRequest } from '../../types/companies';
import { 
  TIMEZONE_OPTIONS, 
  DATE_FORMAT_OPTIONS, 
  TIME_FORMAT_OPTIONS, 
  CURRENCY_OPTIONS, 
  LOCALE_OPTIONS 
} from '../../types/companies';
import type { Vertical } from '../../types/verticals';

interface CompanyFormData {
  legalName: string;
  shortName: string;
  vertical: string;
  registrationNo: string;
  taxId: string;
  address: string;
  phone: string;
  email: string;
  isActive: boolean;
  // Locale Settings
  defaultTimezone: string;
  defaultDateFormat: string;
  defaultTimeFormat: string;
  defaultCurrency: string;
  defaultLocale: string;
}

const initialForm: CompanyFormData = {
  legalName: '',
  shortName: '',
  vertical: '',
  registrationNo: '',
  taxId: '',
  address: '',
  phone: '',
  email: '',
  isActive: true,
  // Locale Settings - Malaysian defaults
  defaultTimezone: 'Asia/Kuala_Lumpur',
  defaultDateFormat: 'dd/MM/yyyy',
  defaultTimeFormat: 'hh:mm a',
  defaultCurrency: 'MYR',
  defaultLocale: 'en-MY'
};

const CompanyProfilePage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [form, setForm] = useState<CompanyFormData>(initialForm);
  const [loading, setLoading] = useState<boolean>(true);
  const [saving, setSaving] = useState<boolean>(false);
  const [existingCompany, setExistingCompany] = useState<Company | null>(null);
  const [submitError, setSubmitError] = useState<string>('');
  const [verticals, setVerticals] = useState<Vertical[]>([]);
  const [verticalsError, setVerticalsError] = useState<string>('');

  useEffect(() => {
    const init = async (): Promise<void> => {
      await Promise.all([loadCompanies(), loadVerticals()]);
    };
    init();
  }, []);

  const loadCompanies = async (): Promise<void> => {
    setLoading(true);
    setSubmitError('');
    try {
      const companies = await getCompanies();
      if (companies.length > 0) {
        const [company] = companies;
        setExistingCompany(company);
        setForm({
          legalName: company.legalName || '',
          shortName: company.shortName || '',
          vertical: company.vertical || '',
          registrationNo: company.registrationNo || '',
          taxId: company.taxId || '',
          address: company.address || '',
          phone: company.phone || '',
          email: company.email || '',
          isActive: company.isActive ?? true,
          // Locale Settings
          defaultTimezone: company.defaultTimezone || 'Asia/Kuala_Lumpur',
          defaultDateFormat: company.defaultDateFormat || 'dd/MM/yyyy',
          defaultTimeFormat: company.defaultTimeFormat || 'hh:mm a',
          defaultCurrency: company.defaultCurrency || 'MYR',
          defaultLocale: company.defaultLocale || 'en-MY'
        });
      } else {
        setExistingCompany(null);
        setForm(initialForm);
      }
    } catch (error: any) {
      console.error('Failed to load company profile', error);
      const message =
        error?.message && error.message.includes('Network error')
          ? error.message
          : 'Unable to load company profile. Please try again.';
      setSubmitError(message);
      showError(message);
    } finally {
      setLoading(false);
    }
  };

  const loadVerticals = async (): Promise<void> => {
    setVerticalsError('');
    try {
      const data = await getVerticals({ isActive: true });
      setVerticals(Array.isArray(data) ? data : []);
    } catch (error: any) {
      console.error('Failed to load verticals', error);
      setVerticalsError('Unable to load verticals. You can still type a custom value.');
    }
  };

  const handleChange = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>): void => {
    const { name, value, type } = event.target;
    const checked = (event.target as HTMLInputElement).checked;
    const payload = type === 'checkbox' ? checked : value;
    setForm((prev) => ({
      ...prev,
      [name]: payload
    }));
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault();
    setSaving(true);
    setSubmitError('');
    try {
      const payload: CreateCompanyRequest | UpdateCompanyRequest = {
        legalName: form.legalName.trim(),
        shortName: form.shortName.trim(),
        vertical: form.vertical.trim(),
        registrationNo: form.registrationNo?.trim() || undefined,
        taxId: form.taxId?.trim() || undefined,
        address: form.address?.trim() || undefined,
        phone: form.phone?.trim() || undefined,
        email: form.email?.trim() || undefined,
        isActive: form.isActive,
        // Locale Settings
        defaultTimezone: form.defaultTimezone,
        defaultDateFormat: form.defaultDateFormat,
        defaultTimeFormat: form.defaultTimeFormat,
        defaultCurrency: form.defaultCurrency,
        defaultLocale: form.defaultLocale
      };

      if (!payload.legalName || !payload.shortName || !payload.vertical) {
        setSubmitError('Legal name, short name and vertical are required.');
        showError('Please fill in the required fields.');
        setSaving(false);
        return;
      }

      if (existingCompany) {
        await updateCompany(existingCompany.id, payload as UpdateCompanyRequest);
        showSuccess('Company profile updated.');
      } else {
        await createCompany(payload as CreateCompanyRequest);
        showSuccess('Company created.');
      }

      await loadCompanies();
    } catch (error: any) {
      console.error('Company save failed', error);
      const message = error?.message || 'Unable to save company profile.';
      setSubmitError(message);
      showError(message);
    } finally {
      setSaving(false);
    }
  };

  // Get current time in selected timezone for preview
  const getCurrentTimePreview = (): string => {
    try {
      const now = new Date();
      return now.toLocaleString(form.defaultLocale, {
        timeZone: form.defaultTimezone,
        dateStyle: 'medium',
        timeStyle: 'short'
      });
    } catch {
      return 'Invalid timezone';
    }
  };

  if (loading) {
    return (
      <PageShell title="Company Profile" breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Company Profile' }]}>
        <div data-testid="settings-company-root">
          <LoadingSpinner message="Loading company profile..." />
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell
      title={existingCompany ? 'Edit company profile' : 'Create company profile'}
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Company Profile' }]}
    >
      <div data-testid="settings-company-root" className="space-y-6">
        <Card className="max-w-3xl space-y-6 p-6">
          <div className="space-y-1">
            <p className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
              Single company setup
            </p>
            <p className="text-xs text-slate-400">
              This form creates or updates the single legal entity that powers the entire platform.
            </p>
            {existingCompany && (
              <p className="text-xs text-slate-400">
                Currently configured: <span className="font-medium">{existingCompany.legalName}</span> (
                {existingCompany.shortName})
              </p>
            )}
          </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Basic Information */}
          <div className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <TextInput
                label="Legal name"
                name="legalName"
                value={form.legalName}
                onChange={handleChange}
                required
              />
              <TextInput
                label="Short name"
                name="shortName"
                value={form.shortName}
                onChange={handleChange}
                required
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-medium text-slate-300">
                Vertical
              </label>
              <select
                name="vertical"
                value={form.vertical}
                onChange={handleChange}
                className="mt-1 w-full rounded border border-input bg-background px-2 py-1 text-xs text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                required
              >
                <option value="">Select vertical</option>
                {verticals.map((v) => (
                  <option key={v.id} value={v.name}>
                    {v.name}
                  </option>
                ))}
                {/* If existing company has a vertical not in the list, keep it selectable */}
                {form.vertical &&
                  !verticals.some((v) => v.name === form.vertical) && (
                    <option value={form.vertical}>{form.vertical}</option>
                  )}
              </select>
              {verticalsError && (
                <p className="text-[10px] text-yellow-400">{verticalsError}</p>
              )}
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <TextInput
                label="Registration number"
                name="registrationNo"
                value={form.registrationNo}
                onChange={handleChange}
              />
              <TextInput
                label="Tax ID"
                name="taxId"
                value={form.taxId}
                onChange={handleChange}
              />
            </div>
            <div>
              <label className="text-xs font-medium text-slate-300">Address</label>
              <textarea
                name="address"
                value={form.address}
                onChange={handleChange}
                className="mt-1 h-24 w-full rounded border border-input bg-background px-2 py-1 text-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              />
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <TextInput label="Phone" name="phone" value={form.phone} onChange={handleChange} />
              <TextInput
                label="Email"
                name="email"
                type="email"
                value={form.email}
                onChange={handleChange}
              />
            </div>

            <div className="flex items-center gap-2">
              <input
                id="company-active"
                type="checkbox"
                name="isActive"
                className="h-4 w-4 rounded border border-input bg-background"
                checked={form.isActive}
                onChange={handleChange}
              />
              <label htmlFor="company-active" className="text-xs font-medium text-slate-300">
                Company is active
              </label>
            </div>
          </div>

          {/* Locale Settings Section */}
          <div className="border-t border-slate-700 pt-6">
            <div className="flex items-center gap-2 mb-4">
              <Globe className="h-5 w-5 text-brand-500" />
              <h2 className="text-base font-semibold text-slate-100">Locale Settings</h2>
            </div>
            <p className="text-xs text-slate-400 mb-4">
              Configure timezone and formatting preferences for the entire system. All dates and times will be displayed using these settings.
            </p>

            {/* Preview Box */}
            <div className="bg-slate-800/50 border border-slate-700 rounded-lg p-4 mb-4">
              <div className="flex items-center gap-2 mb-2">
                <Clock className="h-4 w-4 text-cyan-400" />
                <span className="text-xs font-medium text-slate-300">Current Time Preview</span>
              </div>
              <p className="text-lg font-mono text-cyan-400">{getCurrentTimePreview()}</p>
              <p className="text-[10px] text-slate-500 mt-1">
                Timezone: {form.defaultTimezone} | Locale: {form.defaultLocale}
              </p>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              {/* Timezone */}
              <div className="space-y-1">
                <label className="flex items-center gap-1.5 text-xs font-medium text-slate-300">
                  <Globe className="h-3.5 w-3.5" />
                  Timezone
                </label>
                <select
                  name="defaultTimezone"
                  value={form.defaultTimezone}
                  onChange={handleChange}
                  className="mt-1 w-full rounded border border-input bg-background px-2 py-1.5 text-xs text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                >
                  {TIMEZONE_OPTIONS.map((tz) => (
                    <option key={tz.value} value={tz.value}>
                      {tz.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Locale */}
              <div className="space-y-1">
                <label className="flex items-center gap-1.5 text-xs font-medium text-slate-300">
                  <Globe className="h-3.5 w-3.5" />
                  Display Locale
                </label>
                <select
                  name="defaultLocale"
                  value={form.defaultLocale}
                  onChange={handleChange}
                  className="mt-1 w-full rounded border border-input bg-background px-2 py-1.5 text-xs text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                >
                  {LOCALE_OPTIONS.map((loc) => (
                    <option key={loc.value} value={loc.value}>
                      {loc.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Date Format */}
              <div className="space-y-1">
                <label className="flex items-center gap-1.5 text-xs font-medium text-slate-300">
                  <Calendar className="h-3.5 w-3.5" />
                  Date Format
                </label>
                <select
                  name="defaultDateFormat"
                  value={form.defaultDateFormat}
                  onChange={handleChange}
                  className="mt-1 w-full rounded border border-input bg-background px-2 py-1.5 text-xs text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                >
                  {DATE_FORMAT_OPTIONS.map((df) => (
                    <option key={df.value} value={df.value}>
                      {df.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Time Format */}
              <div className="space-y-1">
                <label className="flex items-center gap-1.5 text-xs font-medium text-slate-300">
                  <Clock className="h-3.5 w-3.5" />
                  Time Format
                </label>
                <select
                  name="defaultTimeFormat"
                  value={form.defaultTimeFormat}
                  onChange={handleChange}
                  className="mt-1 w-full rounded border border-input bg-background px-2 py-1.5 text-xs text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                >
                  {TIME_FORMAT_OPTIONS.map((tf) => (
                    <option key={tf.value} value={tf.value}>
                      {tf.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Currency */}
              <div className="space-y-1">
                <label className="flex items-center gap-1.5 text-xs font-medium text-slate-300">
                  <DollarSign className="h-3.5 w-3.5" />
                  Default Currency
                </label>
                <select
                  name="defaultCurrency"
                  value={form.defaultCurrency}
                  onChange={handleChange}
                  className="mt-1 w-full rounded border border-input bg-background px-2 py-1.5 text-xs text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                >
                  {CURRENCY_OPTIONS.map((cur) => (
                    <option key={cur.value} value={cur.value}>
                      {cur.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {/* Info Box */}
            <div className="mt-4 bg-blue-500/10 border border-blue-500/30 rounded-lg p-3">
              <p className="text-xs text-blue-300">
                <strong>Note:</strong> These settings affect how dates, times, and currencies are displayed throughout the system. 
                All times are stored in UTC and converted to the selected timezone for display.
              </p>
            </div>
          </div>

          {submitError && (
            <p className="text-xs font-medium text-destructive">
              {submitError}
            </p>
          )}

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-700">
            <Button type="submit" variant="default" disabled={saving}>
              {existingCompany ? (saving ? 'Updating…' : 'Update company') : (saving ? 'Creating…' : 'Create company')}
            </Button>
          </div>
        </form>
      </Card>
      </div>
    </PageShell>
  );
};

export default CompanyProfilePage;
