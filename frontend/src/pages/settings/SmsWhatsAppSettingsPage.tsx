import React, { useState, useRef } from 'react';
import { Settings, MessageSquare, MessageCircle, Save, TestTube, RefreshCw, CheckCircle, XCircle } from 'lucide-react';
import { 
  GridComponent, 
  ColumnsDirective, 
  ColumnDirective, 
  Page, 
  Sort, 
  Filter, 
  Group, 
  Toolbar, 
  ExcelExport,
  Edit,
  Inject
} from '@syncfusion/ej2-react-grids';
import { 
  Tabs, 
  TabPanel, 
  Button, 
  Card, 
  TextInput, 
  Select, 
  Switch, 
  useToast, 
  LoadingSpinner 
} from '../../components/ui';
import { PageShell } from '../../components/layout';
import { useGlobalSettings, useUpdateGlobalSetting } from '../../hooks/useGlobalSettings';
import { useSmsTemplates, useUpdateSmsTemplate } from '../../hooks/useSmsTemplates';
import { useWhatsAppTemplates, useUpdateWhatsAppTemplate } from '../../hooks/useWhatsAppTemplates';
import { useActiveSmsGateway, useRegisterSmsGateway } from '../../hooks/useSmsGateway';
import { useDepartment } from '../../contexts/DepartmentContext';

/**
 * SMS/WhatsApp Settings Page - Tabbed Interface
 * 
 * Features:
 * - Tab 1: Provider Configuration (Twilio, SMS Gateway, enable/disable)
 * - Tab 2: SMS Templates (reuse existing grid)
 * - Tab 3: WhatsApp Templates (reuse existing grid)
 */
const SmsWhatsAppSettingsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const { activeDepartment } = useDepartment();
  const companyId = activeDepartment?.companyId || '';

  // Global Settings
  const { data: allSettings = [], isLoading: isLoadingSettings, refetch: refetchSettings } = useGlobalSettings({ module: 'Notifications' });
  const updateSettingMutation = useUpdateGlobalSetting();

  // SMS Gateway
  const { data: activeGateway, isLoading: isLoadingGateway } = useActiveSmsGateway();
  const registerGatewayMutation = useRegisterSmsGateway();

  // Templates
  const { data: smsTemplates = [], isLoading: isLoadingSmsTemplates, refetch: refetchSmsTemplates } = useSmsTemplates({ companyId });
  const { data: whatsappTemplates = [], isLoading: isLoadingWhatsAppTemplates, refetch: refetchWhatsAppTemplates } = useWhatsAppTemplates({ companyId });
  const updateSmsTemplateMutation = useUpdateSmsTemplate();
  const updateWhatsAppTemplateMutation = useUpdateWhatsAppTemplate();

  const smsGridRef = useRef<GridComponent>(null);
  const whatsappGridRef = useRef<GridComponent>(null);

  // Form state for provider configuration
  const [smsEnabled, setSmsEnabled] = useState(false);
  const [smsProvider, setSmsProvider] = useState('None');
  const [smsTwilioAccountSid, setSmsTwilioAccountSid] = useState('');
  const [smsTwilioAuthToken, setSmsTwilioAuthToken] = useState('');
  const [smsTwilioFromNumber, setSmsTwilioFromNumber] = useState('');
  const [smsAutoSend, setSmsAutoSend] = useState(false);

  const [whatsAppEnabled, setWhatsAppEnabled] = useState(false);
  const [whatsAppProvider, setWhatsAppProvider] = useState('None');
  const [whatsAppTwilioAccountSid, setWhatsAppTwilioAccountSid] = useState('');
  const [whatsAppTwilioAuthToken, setWhatsAppTwilioAuthToken] = useState('');
  const [whatsAppTwilioFromNumber, setWhatsAppTwilioFromNumber] = useState('');
  const [whatsAppAutoSend, setWhatsAppAutoSend] = useState(false);

  // SMS Gateway form state
  const [gatewayDeviceName, setGatewayDeviceName] = useState('');
  const [gatewayBaseUrl, setGatewayBaseUrl] = useState('');
  const [gatewayApiKey, setGatewayApiKey] = useState('');
  const [gatewayAdditionalInfo, setGatewayAdditionalInfo] = useState('');

  // Load settings into form state
  React.useEffect(() => {
    if (allSettings.length > 0) {
      const getSetting = (key: string, defaultValue: any = '') => {
        const setting = allSettings.find(s => s.key === key);
        return setting?.value || defaultValue;
      };

      setSmsEnabled(getSetting('SMS_Enabled', 'false') === 'true');
      setSmsProvider(getSetting('SMS_Provider', 'None'));
      setSmsTwilioAccountSid(getSetting('SMS_Twilio_AccountSid', ''));
      setSmsTwilioAuthToken(getSetting('SMS_Twilio_AuthToken', ''));
      setSmsTwilioFromNumber(getSetting('SMS_Twilio_FromNumber', ''));
      setSmsAutoSend(getSetting('SMS_AutoSendOnStatusChange', 'false') === 'true');

      setWhatsAppEnabled(getSetting('WhatsApp_Enabled', 'false') === 'true');
      setWhatsAppProvider(getSetting('WhatsApp_Provider', 'None'));
      setWhatsAppTwilioAccountSid(getSetting('WhatsApp_Twilio_AccountSid', ''));
      setWhatsAppTwilioAuthToken(getSetting('WhatsApp_Twilio_AuthToken', ''));
      setWhatsAppTwilioFromNumber(getSetting('WhatsApp_Twilio_FromNumber', ''));
      setWhatsAppAutoSend(getSetting('WhatsApp_AutoSendOnStatusChange', 'false') === 'true');
    }
  }, [allSettings]);

  // Load active gateway into form
  React.useEffect(() => {
    if (activeGateway) {
      setGatewayDeviceName(activeGateway.deviceName);
      setGatewayBaseUrl(activeGateway.baseUrl);
      setGatewayApiKey(activeGateway.apiKey);
      setGatewayAdditionalInfo(activeGateway.additionalInfo || '');
    }
  }, [activeGateway]);

  const handleSaveSmsSettings = async () => {
    try {
      const settingsToUpdate = [
        { key: 'SMS_Enabled', value: smsEnabled.toString() },
        { key: 'SMS_Provider', value: smsProvider },
        { key: 'SMS_Twilio_AccountSid', value: smsTwilioAccountSid },
        { key: 'SMS_Twilio_AuthToken', value: smsTwilioAuthToken },
        { key: 'SMS_Twilio_FromNumber', value: smsTwilioFromNumber },
        { key: 'SMS_AutoSendOnStatusChange', value: smsAutoSend.toString() },
      ];

      for (const setting of settingsToUpdate) {
        await updateSettingMutation.mutateAsync({
          key: setting.key,
          data: { value: setting.value }
        });
      }

      showSuccess('SMS settings saved successfully');
      refetchSettings();
    } catch (error: any) {
      showError(error?.message || 'Failed to save SMS settings');
    }
  };

  const handleSaveWhatsAppSettings = async () => {
    try {
      const settingsToUpdate = [
        { key: 'WhatsApp_Enabled', value: whatsAppEnabled.toString() },
        { key: 'WhatsApp_Provider', value: whatsAppProvider },
        { key: 'WhatsApp_Twilio_AccountSid', value: whatsAppTwilioAccountSid },
        { key: 'WhatsApp_Twilio_AuthToken', value: whatsAppTwilioAuthToken },
        { key: 'WhatsApp_Twilio_FromNumber', value: whatsAppTwilioFromNumber },
        { key: 'WhatsApp_AutoSendOnStatusChange', value: whatsAppAutoSend.toString() },
      ];

      for (const setting of settingsToUpdate) {
        await updateSettingMutation.mutateAsync({
          key: setting.key,
          data: { value: setting.value }
        });
      }

      showSuccess('WhatsApp settings saved successfully');
      refetchSettings();
    } catch (error: any) {
      showError(error?.message || 'Failed to save WhatsApp settings');
    }
  };

  const handleRegisterGateway = async () => {
    try {
      await registerGatewayMutation.mutateAsync({
        deviceName: gatewayDeviceName,
        baseUrl: gatewayBaseUrl,
        apiKey: gatewayApiKey,
        additionalInfo: gatewayAdditionalInfo || undefined,
      });

      // Update SMS provider to use gateway
      await updateSettingMutation.mutateAsync({
        key: 'SMS_Provider',
        data: { value: 'SMS_Gateway' }
      });

      showSuccess('SMS Gateway registered successfully');
      refetchSettings();
    } catch (error: any) {
      showError(error?.message || 'Failed to register SMS Gateway');
    }
  };

  const handleSmsTemplateSave = async (args: any) => {
    if (args.requestType === 'save' && args.data) {
      try {
        await updateSmsTemplateMutation.mutateAsync({
          id: args.data.id,
          data: {
            name: args.data.name,
            description: args.data.description,
            category: args.data.category,
            messageText: args.data.messageText,
            isActive: args.data.isActive,
            notes: args.data.notes
          }
        });
      } catch (error) {
        if (smsGridRef.current) {
          smsGridRef.current.refresh();
        }
      }
    }
  };

  const handleWhatsAppTemplateSave = async (args: any) => {
    if (args.requestType === 'save' && args.data) {
      try {
        await updateWhatsAppTemplateMutation.mutateAsync({
          id: args.data.id,
          data: {
            name: args.data.name,
            description: args.data.description,
            category: args.data.category,
            templateId: args.data.templateId,
            approvalStatus: args.data.approvalStatus,
            messageBody: args.data.messageBody,
            language: args.data.language,
            isActive: args.data.isActive,
            notes: args.data.notes
          }
        });
      } catch (error) {
        if (whatsappGridRef.current) {
          whatsappGridRef.current.refresh();
        }
      }
    }
  };

  if (isLoadingSettings) {
    return <LoadingSpinner message="Loading settings..." fullPage />;
  }

  return (
    <PageShell
      title="SMS & WhatsApp Settings"
      subtitle="Configure SMS and WhatsApp notification providers and templates"
    >
      <Tabs defaultActiveTab={0}>
        {/* Tab 1: Provider Configuration */}
        <TabPanel label="Provider Configuration" icon={<Settings className="h-4 w-4" />}>
          <div className="space-y-6">
            {/* SMS Configuration */}
            <Card className="p-6">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <MessageSquare className="h-5 w-5" />
                SMS Configuration
              </h3>
              
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Enable SMS</label>
                    <p className="text-xs text-muted-foreground">Enable SMS notifications</p>
                  </div>
                  <Switch checked={smsEnabled} onCheckedChange={setSmsEnabled} />
                </div>

                {smsEnabled && (
                  <>
                    <div>
                      <label className="text-sm font-medium mb-2 block">SMS Provider</label>
                      <Select
                        value={smsProvider}
                        onChange={(e) => setSmsProvider(e.target.value)}
                        options={[
                          { value: 'None', label: 'None (Disabled)' },
                          { value: 'Twilio', label: 'Twilio' },
                          { value: 'SMS_Gateway', label: 'SMS Gateway (Android Device)' },
                        ]}
                      />
                    </div>

                    {smsProvider === 'Twilio' && (
                      <>
                        <TextInput
                          label="Twilio Account SID"
                          value={smsTwilioAccountSid}
                          onChange={(e) => setSmsTwilioAccountSid(e.target.value)}
                          type="password"
                          placeholder="Enter Twilio Account SID"
                        />
                        <TextInput
                          label="Twilio Auth Token"
                          value={smsTwilioAuthToken}
                          onChange={(e) => setSmsTwilioAuthToken(e.target.value)}
                          type="password"
                          placeholder="Enter Twilio Auth Token"
                        />
                        <TextInput
                          label="From Phone Number"
                          value={smsTwilioFromNumber}
                          onChange={(e) => setSmsTwilioFromNumber(e.target.value)}
                          placeholder="+1234567890"
                        />
                      </>
                    )}

                    {smsProvider === 'SMS_Gateway' && (
                      <div className="space-y-4 p-4 bg-muted rounded-lg">
                        <h4 className="font-medium">SMS Gateway Registration</h4>
                        {activeGateway && (
                          <div className="flex items-center gap-2 text-sm text-muted-foreground mb-4">
                            <CheckCircle className="h-4 w-4 text-emerald-600" />
                            <span>Active Gateway: {activeGateway.deviceName}</span>
                            <span className="text-xs">(Last seen: {formatLocalDateTime(activeGateway.lastSeenAtUtc)})</span>
                          </div>
                        )}
                        <TextInput
                          label="Device Name"
                          value={gatewayDeviceName}
                          onChange={(e) => setGatewayDeviceName(e.target.value)}
                          placeholder="Cephas Maxis SMS Gateway"
                        />
                        <TextInput
                          label="Base URL"
                          value={gatewayBaseUrl}
                          onChange={(e) => setGatewayBaseUrl(e.target.value)}
                          placeholder="http://192.168.0.50:8080"
                        />
                        <TextInput
                          label="API Key"
                          value={gatewayApiKey}
                          onChange={(e) => setGatewayApiKey(e.target.value)}
                          type="password"
                          placeholder="Enter API key"
                        />
                        <TextInput
                          label="Additional Info (Optional)"
                          value={gatewayAdditionalInfo}
                          onChange={(e) => setGatewayAdditionalInfo(e.target.value)}
                          placeholder="Android device info"
                        />
                        <Button onClick={handleRegisterGateway} disabled={registerGatewayMutation.isPending}>
                          {registerGatewayMutation.isPending ? 'Registering...' : 'Register Gateway'}
                        </Button>
                      </div>
                    )}

                    <div className="flex items-center justify-between">
                      <div>
                        <label className="text-sm font-medium">Auto-send on Status Change</label>
                        <p className="text-xs text-muted-foreground">Automatically send SMS when order status changes</p>
                      </div>
                      <Switch checked={smsAutoSend} onCheckedChange={setSmsAutoSend} />
                    </div>

                    <Button onClick={handleSaveSmsSettings} disabled={updateSettingMutation.isPending} className="w-full">
                      <Save className="h-4 w-4 mr-2" />
                      Save SMS Settings
                    </Button>
                  </>
                )}
              </div>
            </Card>

            {/* WhatsApp Configuration */}
            <Card className="p-6">
              <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                <MessageCircle className="h-5 w-5" />
                WhatsApp Configuration
              </h3>
              
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <label className="text-sm font-medium">Enable WhatsApp</label>
                    <p className="text-xs text-muted-foreground">Enable WhatsApp notifications</p>
                  </div>
                  <Switch checked={whatsAppEnabled} onCheckedChange={setWhatsAppEnabled} />
                </div>

                {whatsAppEnabled && (
                  <>
                    <div>
                      <label className="text-sm font-medium mb-2 block">WhatsApp Provider</label>
                      <Select
                        value={whatsAppProvider}
                        onChange={(e) => setWhatsAppProvider(e.target.value)}
                        options={[
                          { value: 'None', label: 'None (Disabled)' },
                          { value: 'Twilio', label: 'Twilio' },
                        ]}
                      />
                    </div>

                    {whatsAppProvider === 'Twilio' && (
                      <>
                        <TextInput
                          label="Twilio Account SID"
                          value={whatsAppTwilioAccountSid}
                          onChange={(e) => setWhatsAppTwilioAccountSid(e.target.value)}
                          type="password"
                          placeholder="Enter Twilio Account SID"
                        />
                        <TextInput
                          label="Twilio Auth Token"
                          value={whatsAppTwilioAuthToken}
                          onChange={(e) => setWhatsAppTwilioAuthToken(e.target.value)}
                          type="password"
                          placeholder="Enter Twilio Auth Token"
                        />
                        <TextInput
                          label="WhatsApp From Number"
                          value={whatsAppTwilioFromNumber}
                          onChange={(e) => setWhatsAppTwilioFromNumber(e.target.value)}
                          placeholder="whatsapp:+1234567890"
                        />
                      </>
                    )}

                    <div className="flex items-center justify-between">
                      <div>
                        <label className="text-sm font-medium">Auto-send on Status Change</label>
                        <p className="text-xs text-muted-foreground">Automatically send WhatsApp when order status changes</p>
                      </div>
                      <Switch checked={whatsAppAutoSend} onCheckedChange={setWhatsAppAutoSend} />
                    </div>

                    <Button onClick={handleSaveWhatsAppSettings} disabled={updateSettingMutation.isPending} className="w-full">
                      <Save className="h-4 w-4 mr-2" />
                      Save WhatsApp Settings
                    </Button>
                  </>
                )}
              </div>
            </Card>
          </div>
        </TabPanel>

        {/* Tab 2: SMS Templates */}
        <TabPanel label="SMS Templates" icon={<MessageSquare className="h-4 w-4" />}>
          <div className="bg-card rounded-xl border border-border shadow-sm p-4">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-lg font-semibold">SMS Templates</h3>
                <p className="text-sm text-muted-foreground">Manage SMS notification message templates</p>
              </div>
              <Button size="sm" variant="outline" onClick={() => refetchSmsTemplates()}>
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
            </div>
            
            {isLoadingSmsTemplates ? (
              <LoadingSpinner message="Loading SMS templates..." />
            ) : (
              <GridComponent
                ref={smsGridRef}
                dataSource={smsTemplates}
                allowPaging={true}
                allowSorting={true}
                allowFiltering={true}
                allowGrouping={true}
                allowExcelExport={true}
                editSettings={{
                  allowEditing: true,
                  allowAdding: true,
                  allowDeleting: false,
                  mode: 'Normal' as any
                }}
                toolbar={['Add', 'Edit', 'Update', 'Cancel', 'ExcelExport', 'Search']}
                pageSettings={{ pageSize: 20, pageSizes: [10, 20, 50, 100] }}
                filterSettings={{ type: 'Menu' }}
                toolbarClick={(args: any) => {
                  if (smsGridRef.current && args.item.id.includes('excelexport')) {
                    smsGridRef.current.excelExport({ fileName: 'SmsTemplates.xlsx' });
                  }
                }}
                actionComplete={handleSmsTemplateSave}
                enableHover={true}
              >
                <ColumnsDirective>
                  <ColumnDirective field="code" headerText="Code" width="150" isPrimaryKey={true} validationRules={{ required: true }} />
                  <ColumnDirective field="name" headerText="Name" width="220" validationRules={{ required: true }} />
                  <ColumnDirective field="description" headerText="Description" width="200" />
                  <ColumnDirective field="category" headerText="Category" width="140" allowGrouping={true} />
                  <ColumnDirective field="messageText" headerText="Message" width="400" />
                  <ColumnDirective field="charCount" headerText="Characters" width="120" textAlign="Center" allowEditing={false} />
                  <ColumnDirective 
                    field="isActive" 
                    headerText="Status" 
                    width="120" 
                    template={(props: any) => (
                      <span className={`px-2 py-1 rounded text-xs font-medium ${
                        props.isActive 
                          ? 'bg-emerald-100 text-emerald-700' 
                          : 'bg-gray-100 text-gray-600'
                      }`}>
                        {props.isActive ? 'Active' : 'Inactive'}
                      </span>
                    )} 
                    allowEditing={false} 
                  />
                </ColumnsDirective>
                <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport, Edit]} />
              </GridComponent>
            )}
          </div>
        </TabPanel>

        {/* Tab 3: WhatsApp Templates */}
        <TabPanel label="WhatsApp Templates" icon={<MessageCircle className="h-4 w-4" />}>
          <div className="bg-card rounded-xl border border-border shadow-sm p-4">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-lg font-semibold">WhatsApp Templates</h3>
                <p className="text-sm text-muted-foreground">Manage WhatsApp Business API templates</p>
              </div>
              <Button size="sm" variant="outline" onClick={() => refetchWhatsAppTemplates()}>
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
            </div>
            
            {isLoadingWhatsAppTemplates ? (
              <LoadingSpinner message="Loading WhatsApp templates..." />
            ) : (
              <GridComponent
                ref={whatsappGridRef}
                dataSource={whatsappTemplates}
                allowPaging={true}
                allowSorting={true}
                allowFiltering={true}
                allowGrouping={true}
                allowExcelExport={true}
                editSettings={{
                  allowEditing: true,
                  allowAdding: true,
                  allowDeleting: false,
                  mode: 'Normal' as any
                }}
                toolbar={['Add', 'Edit', 'Update', 'Cancel', 'ExcelExport', 'Search']}
                pageSettings={{ pageSize: 20, pageSizes: [10, 20, 50, 100] }}
                filterSettings={{ type: 'Menu' }}
                toolbarClick={(args: any) => {
                  if (whatsappGridRef.current && args.item.id.includes('excelexport')) {
                    whatsappGridRef.current.excelExport({ fileName: 'WhatsAppTemplates.xlsx' });
                  }
                }}
                actionComplete={handleWhatsAppTemplateSave}
                enableHover={true}
              >
                <ColumnsDirective>
                  <ColumnDirective field="code" headerText="Code" width="130" isPrimaryKey={true} validationRules={{ required: true }} />
                  <ColumnDirective field="name" headerText="Name" width="220" validationRules={{ required: true }} />
                  <ColumnDirective field="description" headerText="Description" width="200" />
                  <ColumnDirective field="category" headerText="Category" width="140" allowGrouping={true} />
                  <ColumnDirective field="templateId" headerText="Template ID" width="200" />
                  <ColumnDirective 
                    field="approvalStatus" 
                    headerText="Approval" 
                    width="140" 
                    template={(props: any) => {
                      const status = props.approvalStatus;
                      const colors: Record<string, string> = {
                        'Approved': 'bg-emerald-100 text-emerald-700',
                        'Pending': 'bg-amber-100 text-amber-700',
                        'Rejected': 'bg-red-100 text-red-700'
                      };
                      return (
                        <span className={`px-2 py-1 rounded text-xs font-medium ${colors[status] || 'bg-gray-100 text-gray-600'}`}>
                          {status}
                        </span>
                      );
                    }} 
                  />
                  <ColumnDirective 
                    field="isActive" 
                    headerText="Status" 
                    width="120" 
                    template={(props: any) => (
                      <span className={`px-2 py-1 rounded text-xs font-medium ${
                        props.isActive 
                          ? 'bg-emerald-100 text-emerald-700' 
                          : 'bg-gray-100 text-gray-600'
                      }`}>
                        {props.isActive ? 'Active' : 'Inactive'}
                      </span>
                    )} 
                    allowEditing={false} 
                  />
                </ColumnsDirective>
                <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport, Edit]} />
              </GridComponent>
            )}
          </div>
        </TabPanel>
      </Tabs>
    </PageShell>
  );
};

export default SmsWhatsAppSettingsPage;

