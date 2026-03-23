import React, { useState } from 'react';
import { Save, RefreshCw, Globe, Key, Lock, Link as LinkIcon, ToggleLeft, ToggleRight } from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Card, Button, TextInput, Label, useToast, LoadingSpinner } from '../../components/ui';
import { useGlobalSettings, useUpdateGlobalSetting } from '../../hooks/useGlobalSettings';

/**
 * MyInvois Settings Page
 * Configure MyInvois e-invoice integration settings
 */
const MyInvoisSettingsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { data: settings = [], isLoading, refetch } = useGlobalSettings();
  const updateMutation = useUpdateGlobalSetting();

  // Get MyInvois settings
  const enabled = settings.find(s => s.key === 'EInvoice_Enabled')?.value === 'true';
  const provider = settings.find(s => s.key === 'EInvoice_Provider')?.value || 'Null';
  const baseUrl = settings.find(s => s.key === 'MyInvois_BaseUrl')?.value || '';
  const clientId = settings.find(s => s.key === 'MyInvois_ClientId')?.value || '';
  const clientSecret = settings.find(s => s.key === 'MyInvois_ClientSecret')?.value || '';
  const myInvoisEnabled = settings.find(s => s.key === 'MyInvois_Enabled')?.value === 'true';

  const [formData, setFormData] = useState({
    enabled: enabled,
    provider: provider,
    baseUrl: baseUrl,
    clientId: clientId,
    clientSecret: clientSecret,
    myInvoisEnabled: myInvoisEnabled
  });

  const [saving, setSaving] = useState(false);

  const handleSave = async () => {
    try {
      setSaving(true);

      // Update all settings
      await Promise.all([
        updateMutation.mutateAsync({
          key: 'EInvoice_Enabled',
          data: { value: formData.enabled.toString() }
        }),
        updateMutation.mutateAsync({
          key: 'EInvoice_Provider',
          data: { value: formData.provider }
        }),
        updateMutation.mutateAsync({
          key: 'MyInvois_BaseUrl',
          data: { value: formData.baseUrl }
        }),
        updateMutation.mutateAsync({
          key: 'MyInvois_ClientId',
          data: { value: formData.clientId }
        }),
        updateMutation.mutateAsync({
          key: 'MyInvois_ClientSecret',
          data: { value: formData.clientSecret }
        }),
        updateMutation.mutateAsync({
          key: 'MyInvois_Enabled',
          data: { value: formData.myInvoisEnabled.toString() }
        })
      ]);

      showSuccess('MyInvois settings saved successfully');
      await refetch();
    } catch (error: any) {
      showError(error.message || 'Failed to save settings');
    } finally {
      setSaving(false);
    }
  };

  if (isLoading) {
    return (
      <PageShell title="MyInvois Settings" subtitle="Configure e-invoice integration">
        <LoadingSpinner message="Loading settings..." fullPage />
      </PageShell>
    );
  }

  return (
    <PageShell
      title="MyInvois Settings"
      subtitle="Configure MyInvois e-invoice integration for automatic invoice submission"
    >
      <div className="space-y-6">
        {/* General Settings */}
        <Card className="p-6">
          <div className="flex items-center gap-3 mb-6">
            <div className="p-2 bg-purple-100 rounded-lg">
              <Globe className="h-6 w-6 text-purple-600" />
            </div>
            <div>
              <h2 className="text-xl font-semibold">General Settings</h2>
              <p className="text-sm text-muted-foreground">Enable and configure e-invoice submission</p>
            </div>
          </div>

          <div className="space-y-4">
            <div className="flex items-center justify-between p-4 bg-muted rounded-lg">
              <div>
                <Label className="text-base font-medium">Enable E-Invoice</Label>
                <p className="text-sm text-muted-foreground">Enable e-invoice submission to MyInvois</p>
              </div>
              <button
                onClick={() => setFormData({ ...formData, enabled: !formData.enabled })}
                className="flex items-center gap-2"
              >
                {formData.enabled ? (
                  <ToggleRight className="h-6 w-6 text-primary" />
                ) : (
                  <ToggleLeft className="h-6 w-6 text-muted-foreground" />
                )}
              </button>
            </div>

            <div className="flex items-center justify-between p-4 bg-muted rounded-lg">
              <div>
                <Label className="text-base font-medium">Enable MyInvois</Label>
                <p className="text-sm text-muted-foreground">Enable MyInvois provider specifically</p>
              </div>
              <button
                onClick={() => setFormData({ ...formData, myInvoisEnabled: !formData.myInvoisEnabled })}
                className="flex items-center gap-2"
                disabled={!formData.enabled}
              >
                {formData.myInvoisEnabled ? (
                  <ToggleRight className="h-6 w-6 text-primary" />
                ) : (
                  <ToggleLeft className="h-6 w-6 text-muted-foreground" />
                )}
              </button>
            </div>

            <div>
              <Label htmlFor="provider">E-Invoice Provider</Label>
              <select
                id="provider"
                value={formData.provider}
                onChange={(e) => setFormData({ ...formData, provider: e.target.value })}
                className="w-full mt-1 px-3 py-2 border rounded-md"
                disabled={!formData.enabled}
              >
                <option value="Null">None (Disabled)</option>
                <option value="MyInvois">MyInvois</option>
              </select>
            </div>
          </div>
        </Card>

        {/* MyInvois API Configuration */}
        <Card className="p-6">
          <div className="flex items-center gap-3 mb-6">
            <div className="p-2 bg-blue-100 rounded-lg">
              <Key className="h-6 w-6 text-blue-600" />
            </div>
            <div>
              <h2 className="text-xl font-semibold">API Configuration</h2>
              <p className="text-sm text-muted-foreground">MyInvois API credentials and endpoints</p>
            </div>
          </div>

          <div className="space-y-4">
            <div>
              <Label htmlFor="baseUrl">Base URL</Label>
              <div className="flex items-center gap-2 mt-1">
                <LinkIcon className="h-4 w-4 text-muted-foreground" />
                <TextInput
                  id="baseUrl"
                  value={formData.baseUrl}
                  onChange={(e) => setFormData({ ...formData, baseUrl: e.target.value })}
                  placeholder="https://api-sandbox.myinvois.hasil.gov.my"
                  disabled={!formData.enabled || !formData.myInvoisEnabled}
                  className="flex-1"
                />
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                MyInvois API base URL (sandbox or production)
              </p>
            </div>

            <div>
              <Label htmlFor="clientId">Client ID</Label>
              <div className="flex items-center gap-2 mt-1">
                <Key className="h-4 w-4 text-muted-foreground" />
                <TextInput
                  id="clientId"
                  type="password"
                  value={formData.clientId}
                  onChange={(e) => setFormData({ ...formData, clientId: e.target.value })}
                  placeholder="Enter MyInvois Client ID"
                  disabled={!formData.enabled || !formData.myInvoisEnabled}
                  className="flex-1"
                />
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                MyInvois API Client ID (encrypted in database)
              </p>
            </div>

            <div>
              <Label htmlFor="clientSecret">Client Secret</Label>
              <div className="flex items-center gap-2 mt-1">
                <Lock className="h-4 w-4 text-muted-foreground" />
                <TextInput
                  id="clientSecret"
                  type="password"
                  value={formData.clientSecret}
                  onChange={(e) => setFormData({ ...formData, clientSecret: e.target.value })}
                  placeholder="Enter MyInvois Client Secret"
                  disabled={!formData.enabled || !formData.myInvoisEnabled}
                  className="flex-1"
                />
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                MyInvois API Client Secret (encrypted in database)
              </p>
            </div>
          </div>
        </Card>

        {/* Action Buttons */}
        <div className="flex items-center justify-end gap-3">
          <Button
            variant="outline"
            onClick={() => refetch()}
            disabled={saving}
          >
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
          <Button
            onClick={handleSave}
            disabled={saving || !formData.enabled}
          >
            {saving ? (
              <LoadingSpinner size="sm" />
            ) : (
              <Save className="h-4 w-4 mr-2" />
            )}
            Save Settings
          </Button>
        </div>

        {/* Info Card */}
        <Card className="p-6 bg-blue-50 border-blue-200">
          <h3 className="font-semibold mb-2">About MyInvois Integration</h3>
          <ul className="text-sm text-muted-foreground space-y-1 list-disc list-inside">
            <li>Invoices are automatically submitted to MyInvois when you click "Submit to MyInvois" in the Invoice Detail page</li>
            <li>Status polling runs automatically every 5 minutes after submission</li>
            <li>Credit notes can also be submitted via the same integration</li>
            <li>All credentials are encrypted and stored securely in the database</li>
          </ul>
        </Card>
      </div>
    </PageShell>
  );
};

export default MyInvoisSettingsPage;

