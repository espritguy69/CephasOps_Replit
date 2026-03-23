import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './contexts/AuthContext';
import ProtectedRoute from './components/auth/ProtectedRoute';
import SettingsProtectedRoute from './components/auth/SettingsProtectedRoute';
import PermissionProtectedRoute from './components/auth/PermissionProtectedRoute';
import MainLayout from './components/layout/MainLayout';
import LoginPage from './pages/auth/LoginPage';
import ChangePasswordPage from './pages/auth/ChangePasswordPage';
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage';
import ResetPasswordPage from './pages/auth/ResetPasswordPage';

// Feature pages
import DashboardPage from './pages/DashboardPage';
import MyTasksPage from './features/tasks/MyTasksPage';
import DepartmentTasksPage from './features/tasks/DepartmentTasksPage';
import TasksListPage from './pages/tasks/TasksListPage';
import NotificationsPage from './features/notifications/NotificationsPage';
import NotificationsCenterPage from './pages/notifications/NotificationsCenterPage';
import EmailSetupPage from './pages/settings/EmailSetupPage';
import OrdersListPage from './pages/orders/OrdersListPage';
import OrderDetailPage from './pages/orders/OrderDetailPage';
import CreateOrderPage from './pages/orders/CreateOrderPage';
import CalendarPage from './pages/scheduler/CalendarPage';
import SIAvailabilityPage from './pages/scheduler/SIAvailabilityPage';
import InstallerSchedulerPage from './pages/scheduler/InstallerSchedulerPage';
import ParserListingPage from './pages/parser/ParserListingPage';
import ParserDashboardPage from './pages/parser/ParserDashboardPage';
import ParseSessionDetailsPage from './pages/parser/ParseSessionDetailsPage';
import InventoryDashboardPage from './pages/inventory/InventoryDashboardPage';
import InventoryListPage from './pages/inventory/InventoryListPage';
import InventoryStockSummaryPage from './pages/inventory/InventoryStockSummaryPage';
import InventoryLedgerPage from './pages/inventory/InventoryLedgerPage';
import InventoryReceivePage from './pages/inventory/InventoryReceivePage';
import InventoryTransferPage from './pages/inventory/InventoryTransferPage';
import InventoryAllocatePage from './pages/inventory/InventoryAllocatePage';
import InventoryIssuePage from './pages/inventory/InventoryIssuePage';
import InventoryReturnPage from './pages/inventory/InventoryReturnPage';
import InventoryReportsIndexPage from './pages/inventory/InventoryReportsIndexPage';
import InventoryUsageByPeriodPage from './pages/inventory/InventoryUsageByPeriodPage';
import ReportsHubPage from './pages/reports/ReportsHubPage';
import ReportRunnerPage from './pages/reports/ReportRunnerPage';
import PayoutHealthDashboardPage from './pages/reports/PayoutHealthDashboardPage';
import PayoutAnomaliesPage from './pages/reports/PayoutAnomaliesPage';
import InventorySerialLifecyclePage from './pages/inventory/InventorySerialLifecyclePage';
import InventoryStockTrendPage from './pages/inventory/InventoryStockTrendPage';
import RMAListPage from './pages/rma/RMAListPage';
import DocketsPage from './pages/operations/DocketsPage';
import InstallerPayoutBreakdownPage from './pages/operations/InstallerPayoutBreakdownPage';
import InvoicesListPage from './pages/billing/InvoicesListPage';
import InvoiceDetailPage from './pages/billing/InvoiceDetailPage';
import InvoiceEditPage from './pages/billing/InvoiceEditPage';
// CompaniesPage removed - company feature disabled
import MaterialSetupPage from './pages/settings/MaterialSetupPage';
import DocumentTemplatesPage from './pages/settings/DocumentTemplatesPage';
import SettingsDocumentTemplateEditorPage from './pages/settings/DocumentTemplateEditorPage';
import CompanyProfilePage from './pages/settings/CompanyProfilePage';
import CompanyDeploymentPage from './pages/settings/CompanyDeploymentPage';
import DepartmentDeploymentPage from './pages/settings/DepartmentDeploymentPage';
import KpiProfilesPage from './pages/settings/KpiProfilesPage';
import KpiDashboardPage from './pages/kpi/KpiDashboardPage';
import KpiProfilesPageDedicated from './pages/kpi/KpiProfilesPage';
import PartnersPage from './pages/settings/PartnersPage';
import PartnerGroupsPage from './pages/settings/PartnerGroupsPage';
import BuildingsPage from './pages/settings/BuildingsPage'; // Settings page for building config
import BuildingMergePage from './pages/settings/BuildingMergePage';
// Buildings Module (main module)
import BuildingsDashboardPage from './pages/buildings/BuildingsDashboardPage';
import BuildingsListPage from './pages/buildings/BuildingsListPage';
import BuildingDetailPage from './pages/buildings/BuildingDetailPage';
import ServiceInstallersPage from './pages/settings/ServiceInstallersPage';
import SiRatePlansPage from './pages/settings/SiRatePlansPage';
import PartnerRatesPage from './pages/settings/PartnerRatesPage';
import RateEngineManagementPage from './pages/settings/RateEngineManagementPage';
import RateGroupsPage from './pages/settings/RateGroupsPage';
import RateDesignerPage from './pages/settings/RateDesignerPage';
import ServiceProfilesPage from './pages/settings/ServiceProfilesPage';
import ServiceProfileMappingsPage from './pages/settings/ServiceProfileMappingsPage';
import DepartmentsPage from './pages/settings/DepartmentsPage';
import OrderTypesPage from './pages/settings/OrderTypesPage';
import OrderCategoriesPage from './pages/settings/OrderCategoriesPage';
import BuildingTypesPage from './pages/settings/BuildingTypesPage';
import InstallationMethodsPage from './pages/settings/InstallationMethodsPage';
import SplitterTypesPage from './pages/settings/SplitterTypesPage';
import SplittersPage from './pages/settings/SplittersPage';
import VerticalsPage from './pages/settings/VerticalsPage';
import SettingsLayout from './pages/settings/SettingsLayout';
import WorkflowDefinitionsPage from './pages/workflow/WorkflowDefinitionsPage';
import GuardConditionsPage from './pages/workflow/GuardConditionsPage';
import SideEffectsPage from './pages/workflow/SideEffectsPage';
import PayrollPeriodsPage from './pages/payroll/PayrollPeriodsPage';
import PayrollRunsPage from './pages/payroll/PayrollRunsPage';
import PayrollEarningsPage from './pages/payroll/PayrollEarningsPage';
import PnlSummaryPage from './pages/pnl/PnlSummaryPage';
// PnlOrdersPage is legacy - route redirects to PnlDrilldownPage
import PnlOverheadsPage from './pages/pnl/PnlOverheadsPage';
import PnlDrilldownPage from './pages/pnl/PnlDrilldownPage';
import DocumentsPage from './pages/documents/DocumentsPage';
import FilesPage from './pages/files/FilesPage';
import DocumentTemplateEditorPage from './pages/doc-templates/DocumentTemplateEditorPage';

// Tests
import TestDashboardPage from './pages/tests/TestDashboardPage';

// Accounting Module
import { AccountingDashboardPage, SupplierInvoicesPage, PaymentsPage } from './pages/accounting';

// Assets Module
import { AssetsDashboardPage, AssetsListPage, AssetDetailPage, MaintenanceSchedulePage, DepreciationReportPage } from './pages/assets';

// Settings - Finance
import PnlTypesPage from './pages/settings/PnlTypesPage';
import AssetTypesPage from './pages/settings/AssetTypesPage';

// Settings - Workflow
import OrderStatusesPage from './pages/settings/OrderStatusesPage';
import TimeSlotSettingsPage from './pages/settings/TimeSlotSettingsPage';
import MaterialTemplatesPage from './pages/settings/MaterialTemplatesPage';

// Parser - Snapshot Viewer
import ParserSnapshotViewerPage from './pages/parser/ParserSnapshotViewerPage';

// Email Management
import EmailManagementPage from './pages/email/EmailManagementPage';

// Admin
import BackgroundJobsPage from './pages/admin/BackgroundJobsPage';
import EventBusMonitorPage from './pages/admin/EventBusMonitorPage';
import WorkersPage from './pages/admin/WorkersPage';
import SchedulerPage from './pages/admin/SchedulerPage';
import OperationalReplayPage from './pages/admin/OperationalReplayPage';
import StateRebuilderPage from './pages/admin/StateRebuilderPage';
import EventLedgerPage from './pages/admin/EventLedgerPage';
import TraceExplorerPage from './pages/admin/TraceExplorerPage';
import SlaMonitorPage from './pages/admin/SlaMonitorPage';
import UserManagementPage from './pages/admin/UserManagementPage';
import SecurityActivityPage from './pages/admin/SecurityActivityPage';
import RolePermissionsPage from './pages/admin/RolePermissionsPage';
import SiInsightsPage from './pages/admin/SiInsightsPage';
import PlatformObservabilityPage from './pages/admin/PlatformObservabilityPage';

// Operational dashboards (insights)
import PlatformDashboard from './pages/insights/PlatformDashboard';
import TenantDashboard from './pages/insights/TenantDashboard';
import OperationsDashboard from './pages/insights/OperationsDashboard';
import FinancialDashboard from './pages/insights/FinancialDashboard';
import RiskDashboard from './pages/insights/RiskDashboard';
import OperationalIntelligenceDashboard from './pages/insights/OperationalIntelligenceDashboard';
import SlaBreachDashboard from './pages/insights/SlaBreachDashboard';

// ============================================
// SYNCFUSION ENHANCED PAGES
// ============================================

import TasksKanbanPage from './pages/tasks/TasksKanbanPage';

// Visual Feature Pages (🔥 Unique Competitive Advantages)
import WarehouseLayoutPage from './pages/inventory/WarehouseLayoutPage';
import BuildingsTreeGridPage from './pages/buildings/BuildingsTreeGridPage';
import SplitterTopologyPage from './pages/settings/SplitterTopologyPage';

import MaterialCategoriesPage from './pages/settings/MaterialCategoriesPage';
import SkillsManagementPage from './pages/settings/SkillsManagementPage';
import SlaConfigurationPage from './pages/settings/SlaConfigurationPage';
import AutomationRulesPage from './pages/settings/AutomationRulesPage';
import ApprovalWorkflowsPage from './pages/settings/ApprovalWorkflowsPage';
import BusinessHoursPage from './pages/settings/BusinessHoursPage';
import EscalationRulesPage from './pages/settings/EscalationRulesPage';
import GuardConditionDefinitionsPage from './pages/settings/GuardConditionDefinitionsPage';
import SideEffectDefinitionsPage from './pages/settings/SideEffectDefinitionsPage';
import IntegrationsPage from './pages/settings/IntegrationsPage';

// Department Master Data Wrapper
import DepartmentMasterDataWrapper from './components/settings/DepartmentMasterDataWrapper';

const App: React.FC = () => {
  const { isAuthenticated, loading, pendingPasswordChangeEmail } = useAuth();

  return (
    <Routes>
      {/* Public routes */}
      <Route
        path="/login"
        element={
          !loading && pendingPasswordChangeEmail
            ? <Navigate to={{ pathname: '/change-password', state: { email: pendingPasswordChangeEmail } }} replace />
            : isAuthenticated
              ? <Navigate to="/dashboard" replace />
              : <LoginPage />
        }
      />
      <Route path="/change-password" element={<ChangePasswordPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />

      {/* Protected routes */}
      <Route
        path="/*"
        element={
          <ProtectedRoute>
            <MainLayout>
              <Routes>
                {/* Dashboard */}
                <Route path="/dashboard" element={<DashboardPage />} />

                {/* Orders */}
                <Route path="/orders" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Orders"><OrdersListPage /></PermissionProtectedRoute>} />
                <Route path="/orders/create" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Orders"><CreateOrderPage /></PermissionProtectedRoute>} />
                <Route path="/orders/:orderId" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Orders"><OrderDetailPage /></PermissionProtectedRoute>} />

                {/* Scheduler */}
                <Route path="/scheduler" element={<PermissionProtectedRoute permission="scheduler.view" fallbackMessage="Scheduler"><CalendarPage /></PermissionProtectedRoute>} />
                <Route path="/scheduler/timeline" element={<PermissionProtectedRoute permission="scheduler.view" fallbackMessage="Scheduler"><InstallerSchedulerPage /></PermissionProtectedRoute>} />
                <Route path="/scheduler/availability" element={<PermissionProtectedRoute permission="scheduler.view" fallbackMessage="Scheduler"><SIAvailabilityPage /></PermissionProtectedRoute>} />

                {/* Orders - Parser */}
                <Route path="/orders/parser" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Parser"><ParserListingPage /></PermissionProtectedRoute>} />
                <Route path="/orders/parser/dashboard" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Parser"><ParserDashboardPage /></PermissionProtectedRoute>} />
                <Route path="/orders/parser/sessions/:id" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Parser"><ParseSessionDetailsPage /></PermissionProtectedRoute>} />
                <Route path="/orders/parser/list" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Parser"><ParserListingPage /></PermissionProtectedRoute>} />
                <Route path="/orders/parser/snapshots" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Parser"><ParserSnapshotViewerPage /></PermissionProtectedRoute>} />

                {/* Email Management */}
                <Route path="/email" element={<PermissionProtectedRoute permission="email.view" fallbackMessage="Email Management"><EmailManagementPage /></PermissionProtectedRoute>} />

                {/* Inventory */}
                <Route path="/inventory" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory"><InventoryDashboardPage /></PermissionProtectedRoute>} />
                <Route path="/inventory/list" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory"><InventoryListPage /></PermissionProtectedRoute>} />
                <Route path="/inventory/stock-summary" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory"><InventoryStockSummaryPage /></PermissionProtectedRoute>} />
                <Route path="/inventory/ledger" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory"><InventoryLedgerPage /></PermissionProtectedRoute>} />
                <Route path="/inventory/receive" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory"><InventoryReceivePage /></PermissionProtectedRoute>} />
                <Route path="/inventory/transfer" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory"><InventoryTransferPage /></PermissionProtectedRoute>} />
                <Route path="/inventory/allocate" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory"><InventoryAllocatePage /></PermissionProtectedRoute>} />
                <Route path="/inventory/issue" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory"><InventoryIssuePage /></PermissionProtectedRoute>} />
                <Route path="/inventory/return" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory"><InventoryReturnPage /></PermissionProtectedRoute>} />
                <Route path="/reports" element={<PermissionProtectedRoute permission="reports.view" fallbackMessage="Reports"><ReportsHubPage /></PermissionProtectedRoute>} />
                <Route path="/reports/payout-health" element={<PermissionProtectedRoute permission="payout.health.view" fallbackMessage="Payout Health"><PayoutHealthDashboardPage /></PermissionProtectedRoute>} />
                <Route path="/reports/payout-health/anomalies" element={<PermissionProtectedRoute permission="payout.health.view" fallbackMessage="Payout Health"><PayoutAnomaliesPage /></PermissionProtectedRoute>} />
                <Route path="/reports/:reportKey" element={<PermissionProtectedRoute permission="reports.view" fallbackMessage="Reports"><ReportRunnerPage /></PermissionProtectedRoute>} />
                <Route path="/inventory/reports" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory Reports"><InventoryReportsIndexPage /></PermissionProtectedRoute>} />
                <Route path="/inventory/reports/usage" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory Reports"><InventoryUsageByPeriodPage /></PermissionProtectedRoute>} />
                <Route path="/inventory/reports/serial-lifecycle" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory Reports"><InventorySerialLifecyclePage /></PermissionProtectedRoute>} />
                <Route path="/inventory/reports/stock-trend" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Inventory Reports"><InventoryStockTrendPage /></PermissionProtectedRoute>} />

                {/* RMA */}
                <Route path="/rma" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="RMA"><RMAListPage /></PermissionProtectedRoute>} />

                {/* Operations - Dockets */}
                <Route path="/operations/dockets" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Dockets"><DocketsPage /></PermissionProtectedRoute>} />
                <Route path="/operations/installer-payout-breakdown" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Installer Payout"><InstallerPayoutBreakdownPage /></PermissionProtectedRoute>} />

                {/* Billing */}
                <Route path="/billing" element={<Navigate to="/billing/invoices" replace />} />
                <Route path="/billing/invoices" element={<PermissionProtectedRoute permission="billing.view" fallbackMessage="Billing"><InvoicesListPage /></PermissionProtectedRoute>} />
                <Route path="/billing/invoices/:id" element={<PermissionProtectedRoute permission="billing.view" fallbackMessage="Billing"><InvoiceDetailPage /></PermissionProtectedRoute>} />
                <Route path="/billing/invoices/:id/edit" element={<PermissionProtectedRoute permission="billing.view" fallbackMessage="Billing"><InvoiceEditPage /></PermissionProtectedRoute>} />

                {/* Payroll */}
                <Route path="/payroll" element={<Navigate to="/payroll/periods" replace />} />
                <Route path="/payroll/periods" element={<PermissionProtectedRoute permission="payroll.view" fallbackMessage="Payroll"><PayrollPeriodsPage /></PermissionProtectedRoute>} />
                <Route path="/payroll/runs" element={<PermissionProtectedRoute permission="payroll.view" fallbackMessage="Payroll"><PayrollRunsPage /></PermissionProtectedRoute>} />
                <Route path="/payroll/earnings" element={<PermissionProtectedRoute permission="payroll.view" fallbackMessage="Payroll"><PayrollEarningsPage /></PermissionProtectedRoute>} />

                {/* P&L */}
                <Route path="/pnl" element={<Navigate to="/pnl/summary" replace />} />
                <Route path="/pnl/summary" element={<PermissionProtectedRoute permission="pnl.view" fallbackMessage="P&L"><PnlSummaryPage /></PermissionProtectedRoute>} />
                <Route path="/pnl/orders" element={<Navigate to="/pnl/drilldown" replace />} /> {/* Legacy route redirects to new drilldown */}
                <Route path="/pnl/drilldown" element={<PermissionProtectedRoute permission="pnl.view" fallbackMessage="P&L"><PnlDrilldownPage /></PermissionProtectedRoute>} />
                <Route path="/pnl/overheads" element={<PermissionProtectedRoute permission="pnl.view" fallbackMessage="P&L"><PnlOverheadsPage /></PermissionProtectedRoute>} />

                {/* KPI Module */}
                <Route path="/kpi" element={<Navigate to="/kpi/dashboard" replace />} />
                <Route path="/kpi/dashboard" element={<PermissionProtectedRoute permission="kpi.view" fallbackMessage="KPI dashboards"><KpiDashboardPage /></PermissionProtectedRoute>} />
                <Route path="/kpi/profiles" element={<PermissionProtectedRoute permission="kpi.view" fallbackMessage="KPI dashboards"><KpiProfilesPageDedicated /></PermissionProtectedRoute>} />

                {/* Notifications */}
                <Route path="/notifications" element={<NotificationsCenterPage />} />

                {/* Accounting */}
                <Route path="/accounting" element={<PermissionProtectedRoute permission="accounting.view" fallbackMessage="Accounting"><AccountingDashboardPage /></PermissionProtectedRoute>} />
                <Route path="/accounting/supplier-invoices" element={<PermissionProtectedRoute permission="accounting.view" fallbackMessage="Accounting"><SupplierInvoicesPage /></PermissionProtectedRoute>} />
                <Route path="/accounting/payments" element={<PermissionProtectedRoute permission="accounting.view" fallbackMessage="Accounting"><PaymentsPage /></PermissionProtectedRoute>} />

                {/* Assets */}
                <Route path="/assets" element={<PermissionProtectedRoute permission="assets.view" fallbackMessage="Assets"><AssetsDashboardPage /></PermissionProtectedRoute>} />
                <Route path="/assets/list" element={<PermissionProtectedRoute permission="assets.view" fallbackMessage="Assets"><AssetsListPage /></PermissionProtectedRoute>} />
                <Route path="/assets/:id" element={<PermissionProtectedRoute permission="assets.view" fallbackMessage="Assets"><AssetDetailPage /></PermissionProtectedRoute>} />
                <Route path="/assets/maintenance" element={<PermissionProtectedRoute permission="assets.view" fallbackMessage="Assets"><MaintenanceSchedulePage /></PermissionProtectedRoute>} />
                <Route path="/assets/depreciation" element={<PermissionProtectedRoute permission="assets.view" fallbackMessage="Assets"><DepreciationReportPage /></PermissionProtectedRoute>} />

                {/* ============================================ */}
                {/* SYNCFUSION ENHANCED PAGES */}
                {/* ============================================ */}

                <Route path="/tasks/kanban" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Tasks"><TasksKanbanPage /></PermissionProtectedRoute>} />

                {/* Visual Features */}
                <Route path="/inventory/warehouse-layout" element={<PermissionProtectedRoute permission="inventory.view" fallbackMessage="Warehouse Layout"><WarehouseLayoutPage /></PermissionProtectedRoute>} />
                <Route path="/buildings/treegrid" element={<PermissionProtectedRoute permission="buildings.view" fallbackMessage="Buildings"><BuildingsTreeGridPage /></PermissionProtectedRoute>} />

                {/* Admin - Background Jobs (settings-level access) */}
                <Route path="/admin/background-jobs" element={<SettingsProtectedRoute><BackgroundJobsPage /></SettingsProtectedRoute>} />
                {/* Admin - Event Bus Monitor */}
                <Route path="/admin/event-bus" element={<SettingsProtectedRoute><EventBusMonitorPage /></SettingsProtectedRoute>} />
                {/* Admin - Workers (distributed worker coordination diagnostics) */}
                <Route path="/admin/workers" element={<SettingsProtectedRoute><WorkersPage /></SettingsProtectedRoute>} />
                {/* Admin - Scheduler (job polling coordinator diagnostics) */}
                <Route path="/admin/scheduler" element={<SettingsProtectedRoute><SchedulerPage /></SettingsProtectedRoute>} />
                {/* Admin - Operational Replay (batch replay; Jobs Admin) */}
                <Route path="/admin/operational-replay" element={<SettingsProtectedRoute><OperationalReplayPage /></SettingsProtectedRoute>} />
                <Route path="/admin/operational-replay/:id" element={<SettingsProtectedRoute><OperationalReplayPage /></SettingsProtectedRoute>} />
                {/* Admin - State Rebuilder (operational state rebuild from Event Store / Ledger) */}
                <Route path="/admin/state-rebuilder" element={<SettingsProtectedRoute><StateRebuilderPage /></SettingsProtectedRoute>} />
                {/* Admin - Event Ledger (operational ledger foundation) */}
                <Route path="/admin/event-ledger" element={<SettingsProtectedRoute><EventLedgerPage /></SettingsProtectedRoute>} />
                {/* Admin - Trace Explorer */}
                <Route path="/admin/trace-explorer" element={<SettingsProtectedRoute><TraceExplorerPage /></SettingsProtectedRoute>} />
                {/* Admin - SLA Monitor */}
                <Route path="/admin/sla-monitor" element={<SettingsProtectedRoute><SlaMonitorPage /></SettingsProtectedRoute>} />
                {/* Admin - SI Operational Insights (orders visibility) */}
                <Route path="/admin/si-insights" element={<SettingsProtectedRoute><SiInsightsPage /></SettingsProtectedRoute>} />
                {/* Admin - Platform Observability (SuperAdmin only; cross-tenant operational dashboard) */}
                <Route path="/admin/platform-observability" element={<SettingsProtectedRoute><PlatformObservabilityPage /></SettingsProtectedRoute>} />
                {/* Operational dashboards (insights) */}
                <Route path="/insights" element={<Navigate to="/insights/tenant" replace />} />
                <Route path="/insights/platform" element={<SettingsProtectedRoute><PlatformDashboard /></SettingsProtectedRoute>} />
                <Route path="/insights/tenant" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Tenant Performance dashboards"><TenantDashboard /></PermissionProtectedRoute>} />
                <Route path="/insights/operations" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Operations Control dashboards"><OperationsDashboard /></PermissionProtectedRoute>} />
                <Route path="/insights/financial" element={<PermissionProtectedRoute permissions={['billing.view', 'pnl.view']} fallbackMessage="Financial dashboards"><FinancialDashboard /></PermissionProtectedRoute>} />
                <Route path="/insights/risk" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Risk & Quality dashboards"><RiskDashboard /></PermissionProtectedRoute>} />
                <Route path="/insights/intelligence" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Operational Intelligence dashboards"><OperationalIntelligenceDashboard /></PermissionProtectedRoute>} />
                <Route path="/insights/sla" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="SLA Breach dashboards"><SlaBreachDashboard /></PermissionProtectedRoute>} />
                {/* Admin - User Management (SuperAdmin/Admin only; API enforces) */}
                <Route path="/admin/users" element={<SettingsProtectedRoute><UserManagementPage /></SettingsProtectedRoute>} />
                {/* Admin - Security Activity (auth events); SuperAdmin/Admin only */}
                <Route path="/admin/security/activity" element={<SettingsProtectedRoute><SecurityActivityPage /></SettingsProtectedRoute>} />
                {/* Admin - Role permissions matrix (RBAC v2) */}
                <Route path="/admin/security/roles" element={<SettingsProtectedRoute><RolePermissionsPage /></SettingsProtectedRoute>} />
                
                {/* ============================================ */}
                {/* SETTINGS ROUTES - ALL PROTECTED */}
                {/* ============================================ */}
                <Route path="/settings/*" element={
                  <SettingsProtectedRoute>
                    <Routes>
                      <Route path="/splitter-topology" element={<SettingsLayout><SplitterTopologyPage /></SettingsLayout>} />

                      <Route path="/material-categories" element={<SettingsLayout><MaterialCategoriesPage /></SettingsLayout>} />
                      <Route path="/settings/skills-management" element={<SettingsLayout><SkillsManagementPage /></SettingsLayout>} />
                      <Route path="/sla-configuration" element={<SettingsLayout><SlaConfigurationPage /></SettingsLayout>} />
                      <Route path="/settings/automation-rules" element={<SettingsLayout><AutomationRulesPage /></SettingsLayout>} />
                      <Route path="/settings/approval-workflows" element={<SettingsLayout><ApprovalWorkflowsPage /></SettingsLayout>} />
                      <Route path="/company/business-hours" element={<SettingsLayout><BusinessHoursPage /></SettingsLayout>} />
                      <Route path="/settings/escalation-rules" element={<SettingsLayout><EscalationRulesPage /></SettingsLayout>} />
                      <Route path="/settings/guard-condition-definitions" element={<SettingsLayout><GuardConditionDefinitionsPage /></SettingsLayout>} />
                      <Route path="/settings/side-effect-definitions" element={<SettingsLayout><SideEffectDefinitionsPage /></SettingsLayout>} />
                      <Route path="/integrations" element={<SettingsLayout><IntegrationsPage /></SettingsLayout>} />

                      {/* Settings - Standard Pages */}
                      <Route path="" element={<Navigate to="/settings/company" replace />} />
                      <Route path="/company" element={<SettingsLayout><CompanyProfilePage /></SettingsLayout>} />
                      <Route path="/company/deployment" element={<SettingsLayout><CompanyDeploymentPage /></SettingsLayout>} />
                      <Route path="/department/deployment" element={<SettingsLayout><DepartmentDeploymentPage /></SettingsLayout>} />
                      <Route path="/company/departments" element={<SettingsLayout><DepartmentsPage /></SettingsLayout>} />
                      <Route path="/company/verticals" element={<SettingsLayout><VerticalsPage /></SettingsLayout>} />
                      <Route path="/company/partners" element={<SettingsLayout><PartnersPage /></SettingsLayout>} />
                      <Route path="/company/partner-groups" element={<SettingsLayout><PartnerGroupsPage /></SettingsLayout>} />
                      <Route path="/service-installers" element={<SettingsLayout><ServiceInstallersPage /></SettingsLayout>} />
                      <Route path="/si-rates" element={<SettingsLayout><SiRatePlansPage /></SettingsLayout>} />
                      <Route path="/partner-rates" element={<SettingsLayout><PartnerRatesPage /></SettingsLayout>} />
                      <Route path="/rate-engine" element={<SettingsLayout><RateEngineManagementPage /></SettingsLayout>} />
                      <Route path="/order-types" element={<SettingsLayout><OrderTypesPage /></SettingsLayout>} />
                      <Route path="/order-categories" element={<SettingsLayout><OrderCategoriesPage /></SettingsLayout>} />
                      <Route path="/installation-types" element={<SettingsLayout><OrderCategoriesPage /></SettingsLayout>} />
                      <Route path="/building-types" element={<SettingsLayout><BuildingTypesPage /></SettingsLayout>} />
                      <Route path="/installation-methods" element={<SettingsLayout><InstallationMethodsPage /></SettingsLayout>} />
                      <Route path="/splitter-types" element={<SettingsLayout><SplitterTypesPage /></SettingsLayout>} />

                      {/* GPON Master Data Routes */}
                      <Route path="/gpon/service-installers" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><ServiceInstallersPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/gpon/skills-management" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><SkillsManagementPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/gpon/rate-engine" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><RateEngineManagementPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/gpon/rate-groups" element={<SettingsLayout><RateGroupsPage /></SettingsLayout>} />
                      <Route path="/gpon/rate-designer" element={<SettingsLayout><RateDesignerPage /></SettingsLayout>} />
                      <Route path="/gpon/service-profiles" element={<SettingsLayout><ServiceProfilesPage /></SettingsLayout>} />
                      <Route path="/gpon/service-profile-mappings" element={<SettingsLayout><ServiceProfileMappingsPage /></SettingsLayout>} />
                      <Route path="/gpon/order-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><OrderTypesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/gpon/order-categories" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><OrderCategoriesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/gpon/installation-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><OrderCategoriesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/gpon/installation-methods" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><InstallationMethodsPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/gpon/building-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><BuildingTypesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/gpon/splitter-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><SplitterTypesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />

                      {/* CWO Master Data Routes */}
                      <Route path="/cwo/service-installers" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><ServiceInstallersPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/cwo/skills-management" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><SkillsManagementPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/cwo/rate-engine" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><RateEngineManagementPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/cwo/order-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><OrderTypesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/cwo/order-categories" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><OrderCategoriesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/cwo/installation-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><OrderCategoriesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/cwo/installation-methods" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><InstallationMethodsPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/cwo/splitter-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><SplitterTypesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />

                      {/* NWO Master Data Routes */}
                      <Route path="/nwo/service-installers" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><ServiceInstallersPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/nwo/skills-management" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><SkillsManagementPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/nwo/rate-engine" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><RateEngineManagementPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/nwo/order-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><OrderTypesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/nwo/order-categories" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><OrderCategoriesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/nwo/installation-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><OrderCategoriesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/nwo/installation-methods" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><InstallationMethodsPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/nwo/splitter-types" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><SplitterTypesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/nwo/materials" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><MaterialSetupPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/nwo/material-templates" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="NWO"><MaterialTemplatesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />

                      {/* GPON Materials */}
                      <Route path="/gpon/materials" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><MaterialSetupPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/gpon/material-templates" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="GPON"><MaterialTemplatesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />

                      {/* CWO Materials */}
                      <Route path="/cwo/materials" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><MaterialSetupPage /></DepartmentMasterDataWrapper></SettingsLayout>} />
                      <Route path="/cwo/material-templates" element={<SettingsLayout><DepartmentMasterDataWrapper departmentCode="CWO"><MaterialTemplatesPage /></DepartmentMasterDataWrapper></SettingsLayout>} />

                      <Route path="/splitters" element={<SettingsLayout><SplittersPage /></SettingsLayout>} />
                      <Route path="/materials" element={<SettingsLayout><MaterialSetupPage /></SettingsLayout>} />
                      <Route path="/material-templates" element={<SettingsLayout><MaterialTemplatesPage /></SettingsLayout>} />
                      <Route path="/document-templates" element={<SettingsLayout><DocumentTemplatesPage /></SettingsLayout>} />
                      <Route path="/document-templates/new" element={<SettingsLayout><SettingsDocumentTemplateEditorPage /></SettingsLayout>} />
                      <Route path="/document-templates/:id" element={<SettingsLayout><SettingsDocumentTemplateEditorPage /></SettingsLayout>} />
                      <Route path="/kpi-profiles" element={<SettingsLayout><KpiProfilesPage /></SettingsLayout>} />
                      <Route path="/email" element={<SettingsLayout><EmailSetupPage /></SettingsLayout>} />
                      <Route path="/pnl-types" element={<SettingsLayout><PnlTypesPage /></SettingsLayout>} />
                      <Route path="/asset-types" element={<SettingsLayout><AssetTypesPage /></SettingsLayout>} />
                      <Route path="/order-statuses" element={<SettingsLayout><OrderStatusesPage /></SettingsLayout>} />
                      <Route path="/time-slots" element={<SettingsLayout><TimeSlotSettingsPage /></SettingsLayout>} />
                      <Route path="/buildings" element={<SettingsLayout><BuildingsPage /></SettingsLayout>} />
                      <Route path="/buildings-merge" element={<SettingsLayout><BuildingMergePage /></SettingsLayout>} />
                    </Routes>
                  </SettingsProtectedRoute>
                } />

                {/* Workflow */}
                <Route path="/workflow/definitions" element={<PermissionProtectedRoute permission="workflow.view" fallbackMessage="Workflow"><WorkflowDefinitionsPage /></PermissionProtectedRoute>} />
                <Route path="/workflow/guard-conditions" element={<PermissionProtectedRoute permission="workflow.view" fallbackMessage="Workflow"><GuardConditionsPage /></PermissionProtectedRoute>} />
                <Route path="/workflow/side-effects" element={<PermissionProtectedRoute permission="workflow.view" fallbackMessage="Workflow"><SideEffectsPage /></PermissionProtectedRoute>} />

                {/* Tasks */}
                <Route path="/tasks" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Tasks"><TasksListPage /></PermissionProtectedRoute>} />
                <Route path="/tasks/my" element={<MyTasksPage />} />
                <Route path="/tasks/department/:departmentId" element={<PermissionProtectedRoute permission="orders.view" fallbackMessage="Tasks"><DepartmentTasksPage /></PermissionProtectedRoute>} />

                {/* Notifications */}
                <Route path="/notifications" element={<NotificationsPage />} />

                {/* Documents */}
                <Route path="/documents" element={<PermissionProtectedRoute permission="documents.view" fallbackMessage="Documents"><DocumentsPage /></PermissionProtectedRoute>} />
                <Route path="/doc-templates/new" element={<PermissionProtectedRoute permission="documents.view" fallbackMessage="Document Templates"><DocumentTemplateEditorPage /></PermissionProtectedRoute>} />
                <Route path="/doc-templates/:id" element={<PermissionProtectedRoute permission="documents.view" fallbackMessage="Document Templates"><DocumentTemplateEditorPage /></PermissionProtectedRoute>} />

                {/* Files */}
                <Route path="/files" element={<PermissionProtectedRoute permission="files.view" fallbackMessage="Files"><FilesPage /></PermissionProtectedRoute>} />

                {/* Tests */}
                <Route path="/tests" element={<SettingsProtectedRoute><TestDashboardPage /></SettingsProtectedRoute>} />

                {/* Buildings Module */}
                <Route path="/buildings" element={<PermissionProtectedRoute permission="buildings.view" fallbackMessage="Buildings"><BuildingsDashboardPage /></PermissionProtectedRoute>} />
                <Route path="/buildings/list" element={<PermissionProtectedRoute permission="buildings.view" fallbackMessage="Buildings"><BuildingsListPage /></PermissionProtectedRoute>} />
                <Route path="/buildings/new" element={<PermissionProtectedRoute permission="buildings.view" fallbackMessage="Buildings"><BuildingDetailPage /></PermissionProtectedRoute>} />
                <Route path="/buildings/:id" element={<PermissionProtectedRoute permission="buildings.view" fallbackMessage="Buildings"><BuildingDetailPage /></PermissionProtectedRoute>} />
                <Route path="/buildings/:id/edit" element={<PermissionProtectedRoute permission="buildings.view" fallbackMessage="Buildings"><BuildingDetailPage /></PermissionProtectedRoute>} />

                {/* Default redirect */}
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route path="*" element={<Navigate to="/dashboard" replace />} />
              </Routes>
            </MainLayout>
          </ProtectedRoute>
        }
      />
    </Routes>
  );
};

export default App;
