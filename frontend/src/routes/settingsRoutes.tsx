import React from 'react';
import { RouteObject } from 'react-router-dom';

// Non-Enhanced Settings Pages
const SmsWhatsAppSettingsPage = React.lazy(() => import('../pages/settings/SmsWhatsAppSettingsPage'));
const SlaConfigurationPage = React.lazy(() => import('../pages/settings/SlaConfigurationPage'));
const AutomationRulesPage = React.lazy(() => import('../pages/settings/AutomationRulesPage'));
const ApprovalWorkflowsPage = React.lazy(() => import('../pages/settings/ApprovalWorkflowsPage'));
const BusinessHoursPage = React.lazy(() => import('../pages/settings/BusinessHoursPage'));
const EscalationRulesPage = React.lazy(() => import('../pages/settings/EscalationRulesPage'));
const CompanyDeploymentPage = React.lazy(() => import('../pages/settings/CompanyDeploymentPage'));
const DepartmentDeploymentPage = React.lazy(() => import('../pages/settings/DepartmentDeploymentPage'));
const TimeSlotSettingsPage = React.lazy(() => import('../pages/settings/TimeSlotSettingsPage'));

/**
 * Settings Routes Configuration
 * 
 * Settings pages (non-Enhanced versions)
 */
export const settingsRoutes: RouteObject[] = [
  {
    path: 'company-deployment',
    element: <CompanyDeploymentPage />,
  },
  {
    path: 'department-deployment',
    element: <DepartmentDeploymentPage />,
  },
  {
    path: 'sms-whatsapp-settings',
    element: <SmsWhatsAppSettingsPage />,
  },
  {
    path: 'sla-configuration',
    element: <SlaConfigurationPage />,
  },
  {
    path: 'automation-rules',
    element: <AutomationRulesPage />,
  },
  {
    path: 'approval-workflows',
    element: <ApprovalWorkflowsPage />,
  },
  {
    path: 'business-hours',
    element: <BusinessHoursPage />,
  },
  {
    path: 'time-slots',
    element: <TimeSlotSettingsPage />,
  },
  {
    path: 'escalation-rules',
    element: <EscalationRulesPage />,
  },
];

/**
 * Settings navigation structure for menus
 */
export const settingsNavigation = {
  coreSettings: [],
  operationsHr: [],
  inventoryFinance: [],
  templates: [],
  systemReports: [
    { path: '/settings/time-slots', label: 'Time Slots', icon: 'Clock' },
  ],
};
