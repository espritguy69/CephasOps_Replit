import React from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Building2, Users, Briefcase, Building, FileText, CheckCircle, Wrench, Layers, Package,
  Truck, DollarSign, Shield, Box, Zap, Tag, Store, Warehouse, Archive, CreditCard, Percent,
  Mail, MessageSquare, MessageCircle, BarChart3, Settings, Target, Bell, Upload, GitBranch,
  Clock, Zap as ZapIcon, AlertTriangle, Lightbulb, Merge
} from 'lucide-react';
import { PageShell } from '../../components/layout';
import { Card } from '../../components/ui';

/**
 * Settings Index Page
 * 
 * Central hub for all 29 Settings pages organized by category
 */

interface SettingCard {
  path: string;
  label: string;
  description: string;
  icon: React.ElementType;
}

const SettingsIndexPage: React.FC = () => {
  const navigate = useNavigate();

  const coreSettings: SettingCard[] = [
    { path: '/settings/company/deployment', label: 'Company Deployment', description: 'Deploy new company with Excel import/export', icon: Upload },
    { path: '/settings/department/deployment', label: 'Department Deployment', description: 'Deploy GPON, CWO, NWO departments with Excel', icon: Building2 },
    { path: '/settings/partners-enhanced', label: 'Partners', description: 'Manage partner companies (TIME, Maxis, etc.)', icon: Building2 },
    { path: '/settings/service-installers-enhanced', label: 'Service Installers', description: 'Field technicians and teams', icon: Users },
    { path: '/settings/skills-management', label: 'Skills Management', description: 'Manage installer skills and categories', icon: Lightbulb },
    { path: '/settings/departments-enhanced', label: 'Departments', description: 'Department structure & cost centers', icon: Briefcase },
    { path: '/settings/buildings-enhanced', label: 'Buildings', description: 'MDUs, commercial buildings, capacity', icon: Building },
    { path: '/settings/buildings-merge', label: 'Merge buildings', description: 'Merge duplicate buildings, reassign orders, soft-delete source', icon: Merge },
    { path: '/settings/order-types-enhanced', label: 'Order Types', description: 'Activation, Modification, Relocation, etc.', icon: FileText },
    { path: '/settings/order-statuses-enhanced', label: 'Order Statuses', description: 'Workflow statuses & transitions', icon: CheckCircle },
    { path: '/settings/installation-methods-enhanced', label: 'Installation Methods', description: 'Indoor, Outdoor, Aerial, Underground', icon: Wrench },
    { path: '/settings/material-categories-enhanced', label: 'Material Categories', description: 'Material classification hierarchy', icon: Layers },
    { path: '/settings/materials-enhanced', label: 'Materials', description: 'Material master data', icon: Package },
  ];

  const operationsHr: SettingCard[] = [
    { path: '/settings/asset-types-enhanced', label: 'Asset Types', description: 'Vehicles, Tools, Equipment, depreciation', icon: Truck },
    { path: '/settings/cost-centers-enhanced', label: 'Cost Centers', description: 'Budget allocations & tracking', icon: DollarSign },
    { path: '/settings/teams-enhanced', label: 'Teams', description: 'Work teams & team leaders', icon: Users },
    { path: '/settings/roles-enhanced', label: 'Roles', description: 'User roles & permissions', icon: Shield },
    { path: '/settings/product-types-enhanced', label: 'Product Types', description: 'Fiber, IPTV, VoIP, Solar', icon: Box },
    { path: '/settings/service-plans-enhanced', label: 'Service Plans', description: 'Pricing plans & speeds', icon: Zap },
    { path: '/settings/brands-enhanced', label: 'Brands', description: 'Equipment brands (Huawei, TP-Link, etc.)', icon: Tag },
    { path: '/settings/vendors-enhanced', label: 'Vendors', description: 'Suppliers & vendor management', icon: Store },
  ];

  const inventoryFinance: SettingCard[] = [
    { path: '/settings/integrations', label: 'Integrations', description: 'Email, MyInvois, SMS & WhatsApp', icon: Zap },
    { path: '/settings/warehouses-enhanced', label: 'Warehouses', description: 'Warehouse locations & capacity', icon: Warehouse },
    { path: '/settings/bins-enhanced', label: 'Bins', description: 'Bin locations & organization', icon: Archive },
    { path: '/settings/payment-terms-enhanced', label: 'Payment Terms', description: 'Net 30, Net 60, discounts', icon: CreditCard },
    { path: '/settings/tax-codes-enhanced', label: 'Tax Codes', description: 'SST, GST, tax rates', icon: Percent },
  ];

  const templates: SettingCard[] = [
    { path: '/settings/document-templates-enhanced', label: 'Document Templates', description: 'Invoice, PO, BOQ templates', icon: FileText },
    { path: '/settings/email-templates-enhanced', label: 'Email Templates', description: 'Email notification templates', icon: Mail },
  ];

  const systemReports: SettingCard[] = [
    { path: '/settings/report-definitions-enhanced', label: 'Report Definitions', description: 'Scheduled reports & formats', icon: BarChart3 },
    { path: '/settings/system-settings-enhanced', label: 'System Settings', description: 'Application-wide configuration', icon: Settings },
    { path: '/workflow/definitions', label: 'Workflow Definitions', description: 'Configure status transitions & workflow rules', icon: GitBranch },
    { path: '/settings/guard-condition-definitions', label: 'Guard Condition Definitions', description: 'Validation rules for transitions', icon: Shield },
    { path: '/settings/side-effect-definitions', label: 'Side Effect Definitions', description: 'Automatic actions on transitions', icon: Zap },
    { path: '/settings/kpi-profiles-enhanced', label: 'KPI Profiles', description: 'Performance targets & KPIs', icon: Target },
    { path: '/settings/sla-configuration', label: 'SLA Configuration', description: 'Response & resolution time SLAs', icon: Clock },
    { path: '/settings/automation-rules', label: 'Automation Rules', description: 'Auto-assignment & escalation rules', icon: ZapIcon },
    { path: '/settings/approval-workflows', label: 'Approval Workflows', description: 'Multi-step approval processes', icon: CheckCircle },
    { path: '/settings/company/business-hours', label: 'Business Hours & Holidays', description: 'Operating hours & public holidays', icon: Clock },
    { path: '/settings/escalation-rules', label: 'Escalation Rules', description: 'Auto-escalation based on conditions', icon: AlertTriangle },
    { path: '/settings/rate-plans-enhanced', label: 'Rate Plans', description: 'Service rates & pricing', icon: DollarSign },
    { path: '/settings/notification-templates-enhanced', label: 'Notification Templates', description: 'Multi-channel notifications', icon: Bell },
  ];

  const renderSettingCards = (cards: SettingCard[]) => {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {cards.map((card) => {
          const Icon = card.icon;
          return (
            <Card
              key={card.path}
              className="p-4 hover:shadow-lg transition-shadow cursor-pointer border-2 hover:border-primary"
              onClick={() => navigate(card.path)}
            >
              <div className="flex items-start gap-3">
                <div className="p-2 bg-primary/10 rounded-lg">
                  <Icon className="h-6 w-6 text-primary" />
                </div>
                <div className="flex-1">
                  <h3 className="font-semibold text-sm mb-1">{card.label}</h3>
                  <p className="text-xs text-muted-foreground">{card.description}</p>
                </div>
              </div>
            </Card>
          );
        })}
      </div>
    );
  };

  return (
    <PageShell
      title="Settings"
      subtitle="Configure all aspects of CephasOps - 32 Settings modules with Excel-like features"
    >
      <div className="space-y-8">
        {/* Feature Banner */}
        <Card className="p-6 bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-900/20 dark:to-purple-900/20 border-2 border-blue-200">
          <div className="flex items-center gap-3 mb-3">
            <div className="p-2 bg-blue-500 text-white rounded-lg">
              <Settings className="h-6 w-6" />
            </div>
            <div>
              <h2 className="text-lg font-bold">✨ Syncfusion-Enhanced Settings</h2>
              <p className="text-sm text-muted-foreground">All 29 pages include: Grouping • Filtering • Inline Editing • Excel Export • Advanced Search</p>
            </div>
          </div>
        </Card>

        {/* Core Settings */}
        <section>
          <h2 className="text-xl font-bold mb-4">Core Settings (11 pages)</h2>
          {renderSettingCards(coreSettings)}
        </section>

        {/* Operations & HR */}
        <section>
          <h2 className="text-xl font-bold mb-4">Operations & HR (8 pages)</h2>
          {renderSettingCards(operationsHr)}
        </section>

        {/* Inventory & Finance */}
        <section>
          <h2 className="text-xl font-bold mb-4">Inventory & Finance (4 pages)</h2>
          {renderSettingCards(inventoryFinance)}
        </section>

        {/* Integrations */}
        <section>
          <h2 className="text-xl font-bold mb-4">Integrations (3 pages)</h2>
          {renderSettingCards([
            { path: '/settings/integrations', label: 'Integrations', description: 'MyInvois, SMS, and WhatsApp configuration', icon: Zap },
          ])}
        </section>

        {/* Templates */}
        <section>
          <h2 className="text-xl font-bold mb-4">Templates (4 pages)</h2>
          {renderSettingCards(templates)}
        </section>

        {/* System & Reports */}
        <section>
          <h2 className="text-xl font-bold mb-4">System & Reports (13 pages)</h2>
          {renderSettingCards(systemReports)}
        </section>
      </div>
    </PageShell>
  );
};

export default SettingsIndexPage;

