import React, { useState, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import type { LucideIcon } from 'lucide-react';
import { 
  LayoutDashboard, FileText, Calendar, CheckSquare, Package, Receipt, 
  Bell, Mail, Workflow, Settings, DollarSign, TrendingUp, FolderOpen,
  Building2, Users, Wrench, FileCode, BarChart3, Shield, ShieldCheck, Network,
  ChevronDown, ChevronRight, ListOrdered, Zap, Home, Share2,
  Calculator, Boxes, CreditCard, FolderTree, Landmark, GitBranch, Eye, Image,
  ClipboardList, Truck, HardHat, PieChart, Clock, AlertTriangle, CheckCircle,
  Warehouse, TestTube, Lightbulb, Activity, Layers, Palette, Bus, Cpu,
  ArrowDownToLine, ArrowRightLeft, Link2, PackageCheck, Undo2, FileBarChart, RotateCcw, BookOpen, Database
} from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { cn } from '@/lib/utils';

interface NavSubItem {
  path: string;
  label: string;
  icon: LucideIcon;
}

interface NavItem {
  title: string;
  path: string;
  icon: LucideIcon;
  description?: string;
  permission?: string;
  badge?: string | number;
  subItems?: NavSubItem[];
}

interface NavSection {
  title: string;
  items: NavItem[];
}

interface SettingsSubItem {
  label?: string;
  path?: string;
  icon?: LucideIcon;
  children?: Array<{ path: string; label: string; icon: LucideIcon }>;
}

// Navigation configuration organized by category
const NAV_SECTIONS: NavSection[] = [
  {
    title: 'Main',
    items: [
      {
        title: 'Dashboard',
        path: '/dashboard',
        icon: LayoutDashboard,
        description: 'Operations overview'
      },
      {
        title: 'Reports Hub',
        path: '/reports',
        icon: FileBarChart,
        permission: 'reports.view',
        description: 'Search and run reports'
      },
      {
        title: 'Payout Health',
        path: '/reports/payout-health',
        icon: Activity,
        permission: 'payout.health.view',
        description: 'Snapshot coverage and payout anomalies'
      },
      {
        title: 'Payout Anomalies',
        path: '/reports/payout-health/anomalies',
        icon: Activity,
        permission: 'payout.health.view',
        description: 'Anomaly detection for payout patterns'
      },
      {
        title: 'Command Center',
        path: '/insights',
        icon: BarChart3,
        description: 'Operational dashboards',
        subItems: [
          { path: '/insights/platform', label: 'Platform Health', icon: Activity },
          { path: '/insights/tenant', label: 'Tenant Performance', icon: TrendingUp },
          { path: '/insights/operations', label: 'Operations Control', icon: CheckSquare },
          { path: '/insights/financial', label: 'Financial Overview', icon: DollarSign },
          { path: '/insights/risk', label: 'Risk & Quality', icon: AlertTriangle },
          { path: '/insights/intelligence', label: 'Operational Intelligence', icon: Lightbulb },
          { path: '/insights/sla', label: 'SLA Breach', icon: Clock }
        ]
      }
    ]
  },
  {
    title: 'Operations',
    items: [
      {
        title: 'Orders',
        path: '/orders',
        icon: ClipboardList,
        permission: 'orders.view'
      },
      {
        title: 'Parser List',
        path: '/orders/parser',
        icon: FileText,
        permission: 'orders.view'
      },
      {
        title: 'Scheduler',
        path: '/scheduler',
        icon: Calendar,
        permission: 'scheduler.view',
        subItems: [
          { path: '/scheduler', label: 'Calendar', icon: Calendar },
          { path: '/scheduler/timeline', label: 'Timeline', icon: Clock }
        ]
      },
      {
        title: 'Tasks',
        path: '/tasks',
        icon: CheckSquare,
        subItems: [
          { path: '/tasks', label: 'All Tasks', icon: CheckSquare },
          { path: '/tasks/my', label: 'My Tasks', icon: CheckSquare }
        ]
      },
      {
        title: 'Dockets',
        path: '/operations/dockets',
        icon: FileText,
        permission: 'orders.view',
        description: 'Docket receive, verify, upload'
      },
      {
        title: 'Installer Payout Breakdown',
        path: '/operations/installer-payout-breakdown',
        icon: DollarSign,
        permission: 'orders.view',
        description: 'View how installer payouts are calculated per order'
      }
    ]
  },
  {
    title: 'Resources',
    items: [
      {
        title: 'Assets',
        path: '/assets',
        icon: Boxes,
        permission: 'assets.view',
        subItems: [
          { path: '/assets', label: 'Dashboard', icon: Boxes },
          { path: '/assets/list', label: 'Asset Register', icon: FileText },
          { path: '/assets/maintenance', label: 'Maintenance', icon: Wrench },
          { path: '/assets/depreciation', label: 'Depreciation', icon: TrendingUp }
        ]
      },
      {
        title: 'Inventory',
        path: '/inventory',
        icon: Package,
        permission: 'inventory.view',
        subItems: [
          { path: '/inventory', label: 'Dashboard', icon: Package },
          { path: '/inventory/stock-summary', label: 'Stock Summary', icon: Package },
          { path: '/inventory/ledger', label: 'Ledger', icon: FileText },
          { path: '/inventory/receive', label: 'Receive', icon: ArrowDownToLine },
          { path: '/inventory/transfer', label: 'Transfer', icon: ArrowRightLeft },
          { path: '/inventory/allocate', label: 'Allocate', icon: Link2 },
          { path: '/inventory/issue', label: 'Issue', icon: PackageCheck },
          { path: '/inventory/return', label: 'Return', icon: Undo2 },
          { path: '/inventory/reports', label: 'Reports', icon: FileBarChart },
          { path: '/inventory/warehouses', label: 'Warehouses', icon: Warehouse },
          { path: '/rma', label: 'RMA', icon: Truck }
        ]
      },
      {
        title: 'Buildings',
        path: '/buildings',
        icon: Building2,
        permission: 'buildings.view'
      },
      {
        title: 'Splitter Master',
        path: '/settings/splitters',
        icon: Network,
        permission: 'settings.view'
      }
    ]
  },
  {
    title: 'Finance',
    items: [
      {
        title: 'Billing',
        path: '/billing/invoices',
        icon: Receipt,
        permission: 'billing.view'
      },
      {
        title: 'Payroll',
        path: '/payroll/periods',
        icon: DollarSign,
        permission: 'payroll.view'
      },
      {
        title: 'P&L',
        path: '/pnl/summary',
        icon: PieChart,
        permission: 'pnl.view',
        subItems: [
          { path: '/pnl/summary', label: 'Summary', icon: PieChart },
          { path: '/pnl/drilldown', label: 'Order Drilldown', icon: BarChart3 },
          { path: '/pnl/overheads', label: 'Overheads', icon: DollarSign }
        ]
      },
      {
        title: 'Accounting',
        path: '/accounting',
        icon: Calculator,
        permission: 'accounting.view',
        subItems: [
          { path: '/accounting', label: 'Dashboard', icon: Calculator },
          { path: '/accounting/supplier-invoices', label: 'Supplier Invoices', icon: FileText },
          { path: '/accounting/payments', label: 'Payments', icon: CreditCard }
        ]
      }
    ]
  },
  {
    title: 'Tools',
    items: [
      {
        title: 'Email',
        path: '/email',
        icon: Mail,
        permission: 'email.view',
        description: 'Inbox & email management'
      },
      {
        title: 'Documents',
        path: '/documents',
        icon: FileText,
        permission: 'documents.view'
      },
      {
        title: 'Files',
        path: '/files',
        icon: FolderOpen,
        permission: 'files.view'
      },
      {
        title: 'Workflow',
        path: '/workflow/definitions',
        icon: Workflow,
        permission: 'workflow.view'
      }
    ]
  },
  {
    title: 'System',
    items: [
      {
        title: 'KPI',
        path: '/kpi/dashboard',
        icon: BarChart3,
        permission: 'kpi.view',
        subItems: [
          { path: '/kpi/dashboard', label: 'Dashboard', icon: BarChart3 },
          { path: '/kpi/profiles', label: 'Profiles', icon: Settings }
        ]
      },
      {
        title: 'Notifications',
        path: '/notifications',
        icon: Bell
      },
      {
        title: 'Background Jobs',
        path: '/admin/background-jobs',
        icon: Activity,
        permission: 'jobs.view'
      },
      {
        title: 'Event Bus Monitor',
        path: '/admin/event-bus',
        icon: Bus,
        permission: 'jobs.view'
      },
      {
        title: 'Workers',
        path: '/admin/workers',
        icon: Cpu,
        permission: 'jobs.admin'
      },
      {
        title: 'Scheduler',
        path: '/admin/scheduler',
        icon: Clock,
        permission: 'jobs.admin'
      },
      {
        title: 'Operational Replay',
        path: '/admin/operational-replay',
        icon: RotateCcw,
        permission: 'jobs.admin'
      },
      {
        title: 'State Rebuilder',
        path: '/admin/state-rebuilder',
        icon: Database,
        permission: 'jobs.admin'
      },
      {
        title: 'Event Ledger',
        path: '/admin/event-ledger',
        icon: BookOpen,
        permission: 'jobs.admin'
      },
      {
        title: 'Trace Explorer',
        path: '/admin/trace-explorer',
        icon: GitBranch,
        permission: 'jobs.view'
      },
      {
        title: 'SLA Monitor',
        path: '/admin/sla-monitor',
        icon: AlertTriangle,
        permission: 'jobs.view'
      },
      {
        title: 'SI Insights',
        path: '/admin/si-insights',
        icon: BarChart3,
        permission: 'orders.view'
      },
      {
        title: 'Platform Observability',
        path: '/admin/platform-observability',
        icon: Activity,
        permission: 'admin.tenants.view',
        description: 'Tenant operations dashboard (platform admin only)'
      },
      {
        title: 'User Management',
        path: '/admin/users',
        icon: Users,
        permission: 'admin.view'
      },
      {
        title: 'Security Activity',
        path: '/admin/security/activity',
        icon: ShieldCheck,
        permission: 'admin.security.view'
      },
      {
        title: 'Role Permissions',
        path: '/admin/security/roles',
        icon: Shield,
        permission: 'admin.roles.view'
      },
      {
        title: 'Tests',
        path: '/tests',
        icon: TestTube,
        permission: 'admin.view'
      },
      {
        title: 'Settings',
        path: '/settings',
        icon: Settings,
        permission: 'settings.view'
      }
    ]
  }
];

// Settings sub-menu configuration
const SETTINGS_SUB_ITEMS: SettingsSubItem[] = [
  {
    label: 'Company',
    icon: Shield,
    children: [
      { path: '/settings/company', label: 'Company Profile', icon: Shield },
      { path: '/settings/company/departments', label: 'Departments', icon: Users },
      { path: '/settings/company/partners', label: 'Partners', icon: Users },
      { path: '/settings/company/partner-groups', label: 'Partner Groups', icon: Users },
      { path: '/settings/company/verticals', label: 'Verticals', icon: Network },
      { path: '/settings/company/business-hours', label: 'Business Hours & Holidays', icon: Clock }
    ]
  },
  {
    label: 'Finance',
    icon: Calculator,
    children: [
      { path: '/settings/pnl-types', label: 'P&L Types', icon: FolderTree },
      { path: '/settings/asset-types', label: 'Asset Types', icon: Boxes }
    ]
  },
  {
    label: 'Workflow',
    icon: GitBranch,
    children: [
      { path: '/settings/order-statuses', label: 'Order Statuses', icon: ListOrdered },
      { path: '/workflow/definitions', label: 'Workflow Definitions', icon: Workflow },
      { path: '/settings/guard-condition-definitions', label: 'Guard Condition Definitions', icon: Shield },
      { path: '/settings/side-effect-definitions', label: 'Side Effect Definitions', icon: Zap },
      { path: '/settings/sla-configuration', label: 'SLA Configuration', icon: Clock },
      { path: '/settings/automation-rules', label: 'Automation Rules', icon: Zap },
      { path: '/settings/approval-workflows', label: 'Approval Workflows', icon: CheckCircle },
      { path: '/settings/time-slots', label: 'Time Slots', icon: Clock },
      { path: '/settings/escalation-rules', label: 'Escalation Rules', icon: AlertTriangle }
    ]
  },
  {
    label: 'GPON Master Data',
    icon: Network,
    children: [
      { path: '/settings/gpon/service-installers', label: 'Service Installers', icon: HardHat },
      { path: '/settings/gpon/skills-management', label: 'Skills Management', icon: Lightbulb },
      { path: '/settings/gpon/rate-engine', label: 'Rate Engine', icon: Calculator },
      { path: '/settings/gpon/rate-designer', label: 'Rate Designer', icon: Palette },
      { path: '/settings/gpon/rate-groups', label: 'Rate Groups', icon: Layers },
      { path: '/settings/gpon/service-profiles', label: 'Service Profiles', icon: FolderTree },
      { path: '/settings/gpon/service-profile-mappings', label: 'Service Profile Mappings', icon: Link2 },
      { path: '/settings/gpon/order-types', label: 'Order Types', icon: ListOrdered },
      { path: '/settings/gpon/order-categories', label: 'Order Categories', icon: Zap },
      { path: '/settings/gpon/installation-methods', label: 'Installation Methods', icon: Home },
      { path: '/settings/gpon/building-types', label: 'Building Types', icon: Building2 },
      { path: '/settings/gpon/splitter-types', label: 'Splitter Types', icon: Share2 },
      { path: '/settings/gpon/materials', label: 'Materials', icon: Package },
      { path: '/settings/gpon/material-templates', label: 'Material Templates', icon: Package }
    ]
  },
  {
    label: 'CWO Master Data',
    icon: Wrench,
    children: [
      { path: '/settings/cwo/service-installers', label: 'Service Installers', icon: HardHat },
      { path: '/settings/cwo/skills-management', label: 'Skills Management', icon: Lightbulb },
      { path: '/settings/cwo/rate-engine', label: 'Rate Engine', icon: Calculator },
      { path: '/settings/cwo/order-types', label: 'Order Types', icon: ListOrdered },
      { path: '/settings/cwo/order-categories', label: 'Order Categories', icon: Zap },
      { path: '/settings/cwo/installation-methods', label: 'Installation Methods', icon: Home },
      { path: '/settings/cwo/splitter-types', label: 'Splitter Types', icon: Share2 },
      { path: '/settings/cwo/materials', label: 'Materials', icon: Package },
      { path: '/settings/cwo/material-templates', label: 'Material Templates', icon: Package }
    ]
  },
  {
    label: 'NWO Master Data',
    icon: Building2,
    children: [
      { path: '/settings/nwo/service-installers', label: 'Service Installers', icon: HardHat },
      { path: '/settings/nwo/skills-management', label: 'Skills Management', icon: Lightbulb },
      { path: '/settings/nwo/rate-engine', label: 'Rate Engine', icon: Calculator },
      { path: '/settings/nwo/order-types', label: 'Order Types', icon: ListOrdered },
      { path: '/settings/nwo/order-categories', label: 'Order Categories', icon: Zap },
      { path: '/settings/nwo/installation-methods', label: 'Installation Methods', icon: Home },
      { path: '/settings/nwo/splitter-types', label: 'Splitter Types', icon: Share2 },
      { path: '/settings/nwo/materials', label: 'Materials', icon: Package },
      { path: '/settings/nwo/material-templates', label: 'Material Templates', icon: Package }
    ]
  },
  { path: '/settings/integrations', label: 'Integrations', icon: Zap },
  { path: '/settings/document-templates', label: 'Document Templates', icon: FileCode },
  { path: '/settings/kpi-profiles', label: 'KPI Profiles', icon: BarChart3 }
];

interface SidebarProps {
  isOpen: boolean;
  mobileOpen?: boolean;
  onMobileClose?: () => void;
}

const Sidebar: React.FC<SidebarProps> = ({ isOpen, mobileOpen = false, onMobileClose }) => {
  const location = useLocation();
  const { user } = useAuth();
  const [expandedMenus, setExpandedMenus] = useState<Record<string, boolean>>({});
  const [settingsExpanded, setSettingsExpanded] = useState<boolean>(false);
  const [expandedSettingsGroups, setExpandedSettingsGroups] = useState<Record<string, boolean>>({});

  // Check if user has permission for a route (RBAC v2: use permissions when present, else role fallback)
  const hasPermission = (permission: string | undefined): boolean => {
    if (!user) return false;
    if (!permission) return true;

    const userRoles = user.roles || [];
    const userPermissions = user.permissions;

    // SuperAdmin bypasses all permission checks
    if (userRoles.includes('SuperAdmin')) return true;

    // When backend returns permissions (RBAC v2), use them for visibility
    if (Array.isArray(userPermissions) && userPermissions.length > 0) {
      return userPermissions.includes(permission);
    }

    // Fallback: role-based (pre-RBAC v2 or when permissions not loaded)
    if (permission === 'admin.view') return userRoles.includes('Admin');
    if (permission === 'admin.tenants.view') return userRoles.includes('Admin');
    if (permission === 'admin.security.view' || permission === 'admin.roles.view') return userRoles.includes('Admin');
    if (permission?.startsWith('payout.') || permission?.startsWith('rates.') || permission?.startsWith('payroll.')) return userRoles.includes('Admin');
    if (userRoles.includes('Admin')) return true;
    if (userRoles.length > 0) return true;

    return false;
  };

  // Toggle expanded menu
  const toggleMenu = (key: string): void => {
    setExpandedMenus(prev => ({ ...prev, [key]: !prev[key] }));
  };

  // Toggle settings group
  const toggleSettingsGroup = (label: string): void => {
    setExpandedSettingsGroups(prev => ({ ...prev, [label]: !prev[label] }));
  };

  // Check if a path is active
  const isActive = (path: string): boolean => {
    if (path === '/dashboard') return location.pathname === '/dashboard';
    return location.pathname === path || location.pathname.startsWith(path + '/');
  };

  // Check if any settings sub-item is active
  const getAllSettingsPaths = (): string[] => {
    const paths: string[] = [];
    SETTINGS_SUB_ITEMS.forEach(item => {
      if (item.children) {
        item.children.forEach(child => paths.push(child.path));
      } else if (item.path) {
        paths.push(item.path);
      }
    });
    return paths;
  };

  const hasActiveSettingsChild = getAllSettingsPaths().some(path => isActive(path));

  // Auto-expand menus when their children are active
  useEffect(() => {
    if (hasActiveSettingsChild) {
      setSettingsExpanded(true);
    }
    
    // Auto-expand parent menus
    NAV_SECTIONS.forEach(section => {
      section.items.forEach(item => {
        if (item.subItems) {
          const hasActiveChild = item.subItems.some(sub => isActive(sub.path));
          if (hasActiveChild) {
            setExpandedMenus(prev => ({ ...prev, [item.title]: true }));
          }
        }
      });
    });
  }, [location.pathname, hasActiveSettingsChild]);

  const userRoles = user?.roles || [];
  const isSuperAdmin = userRoles.includes('SuperAdmin');

  // Close mobile menu when clicking a link
  const handleLinkClick = (): void => {
    if (window.innerWidth < 768 && onMobileClose) {
      onMobileClose();
    }
  };

  return (
    <aside data-testid="sidebar" className={cn(
      // Base styles
      "top-14 h-[calc(100vh-3.5rem)] frosted-glass border-r border-border z-40 flex flex-col flex-shrink-0",
      // Mobile: slide-in drawer (hidden by default, shown when mobileOpen)
      "fixed left-0 transform transition-transform duration-300 ease-in-out",
      "w-64 -translate-x-full",
      mobileOpen && "translate-x-0",
      // Tablet: always visible, collapsed state (relative positioning, no gap)
      "md:translate-x-0 md:relative md:w-16 md:flex-shrink-0",
      // Desktop: fixed positioning, expanded/collapsed based on isOpen
      "lg:fixed lg:w-64",
      !isOpen && "lg:w-16"
    )}>
      {/* Scrollable Navigation */}
      <nav className="flex-1 py-3 overflow-y-auto scrollbar-thin">
        {NAV_SECTIONS.map((section, sectionIndex) => (
          <div key={section.title} className={cn(sectionIndex > 0 && "mt-4")}>
            {/* Section Title */}
            {isOpen && (
              <div className="px-4 mb-1">
                <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground/70">
                  {section.title}
                </span>
              </div>
            )}
            
            {/* Section Items */}
            <div className="space-y-0.5 px-2">
              {section.items.map((item) => {
                // Skip items without permission
                if (item.permission && !hasPermission(item.permission) && !isSuperAdmin) {
                  return null;
                }

                // Settings menu special handling
                if (item.title === 'Settings') {
                  return (
                    <SettingsMenu
                      key={item.path}
                      item={item}
                      isOpen={isOpen}
                      settingsExpanded={settingsExpanded}
                      setSettingsExpanded={setSettingsExpanded}
                      expandedGroups={expandedSettingsGroups}
                      toggleGroup={toggleSettingsGroup}
                      isActive={isActive}
                      hasActiveChild={hasActiveSettingsChild}
                      onLinkClick={handleLinkClick}
                    />
                  );
                }

                // Items with sub-items
                if (item.subItems) {
                  return (
                    <ExpandableMenuItem
                      key={item.path}
                      item={item}
                      isOpen={isOpen}
                      isExpanded={expandedMenus[item.title] || false}
                      onToggle={() => toggleMenu(item.title)}
                      isActive={isActive}
                      onLinkClick={handleLinkClick}
                    />
                  );
                }

                // Regular menu item
                return (
                  <MenuItem
                    key={item.path}
                    item={item}
                    isOpen={isOpen}
                    isActive={isActive(item.path)}
                    onLinkClick={handleLinkClick}
                  />
                );
              })}
            </div>
          </div>
        ))}
      </nav>

      {/* Footer */}
      {isOpen && (
        <div className="border-t border-border p-3">
          <div className="text-[10px] text-muted-foreground text-center">
            CephasOps v1.0.0
          </div>
        </div>
      )}
    </aside>
  );
};

// Regular Menu Item
interface MenuItemProps {
  item: NavItem;
  isOpen: boolean;
  isActive: boolean;
  onLinkClick?: () => void;
}

const MenuItem: React.FC<MenuItemProps> = ({ item, isOpen, isActive, onLinkClick }) => {
  const Icon = item.icon;
  
  return (
    <Link
      to={item.path}
      onClick={onLinkClick}
      className={cn(
        "flex items-center gap-3 px-3 py-2 rounded-lg transition-fast group",
        isActive
          ? "bg-primary text-primary-foreground shadow-sm"
          : "text-muted-foreground hover-subtle hover:text-foreground"
      )}
      title={!isOpen ? item.title : undefined}
    >
      <Icon className={cn("h-4 w-4 flex-shrink-0", isActive && "text-primary-foreground")} />
      {isOpen && (
        <span className="text-sm font-medium truncate">{item.title}</span>
      )}
      {item.badge && isOpen && (
        <span className="ml-auto px-1.5 py-0.5 text-[10px] font-semibold rounded-full bg-primary/10 text-primary">
          {item.badge}
        </span>
      )}
    </Link>
  );
};

// Expandable Menu Item (with sub-items)
interface ExpandableMenuItemProps {
  item: NavItem;
  isOpen: boolean;
  isExpanded: boolean;
  onToggle: () => void;
  isActive: (path: string) => boolean;
  onLinkClick?: () => void;
}

const ExpandableMenuItem: React.FC<ExpandableMenuItemProps> = ({ item, isOpen, isExpanded, onToggle, isActive, onLinkClick }) => {
  const Icon = item.icon;
  const hasActiveChild = item.subItems?.some(sub => isActive(sub.path)) || false;
  
  return (
    <div>
      <button
        onClick={onToggle}
        className={cn(
          "w-full flex items-center gap-3 px-3 py-2 rounded-lg transition-all duration-150",
          hasActiveChild
            ? "bg-primary/10 text-primary"
            : "text-muted-foreground hover:bg-accent hover:text-foreground"
        )}
      >
        <Icon className="h-4 w-4 flex-shrink-0" />
        {isOpen && (
          <>
            <span className="text-sm font-medium truncate flex-1 text-left">{item.title}</span>
            {isExpanded ? (
              <ChevronDown className="h-3.5 w-3.5" />
            ) : (
              <ChevronRight className="h-3.5 w-3.5" />
            )}
          </>
        )}
      </button>
      
      {isOpen && isExpanded && item.subItems && (
        <div className="mt-1 ml-4 pl-3 border-l border-border space-y-0.5">
          {item.subItems.map((subItem) => {
            const SubIcon = subItem.icon;
            const subActive = isActive(subItem.path);
            
            return (
              <Link
                key={subItem.path}
                to={subItem.path}
                onClick={onLinkClick}
                className={cn(
                  "flex items-center gap-2 px-2 py-1.5 rounded-md text-sm transition-colors",
                  subActive
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-accent hover:text-foreground"
                )}
              >
                <SubIcon className="h-3.5 w-3.5 flex-shrink-0" />
                <span className="truncate">{subItem.label}</span>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
};

// Settings Menu (special handling)
interface SettingsMenuProps {
  item: NavItem;
  isOpen: boolean;
  settingsExpanded: boolean;
  setSettingsExpanded: (expanded: boolean) => void;
  expandedGroups: Record<string, boolean>;
  toggleGroup: (label: string) => void;
  isActive: (path: string) => boolean;
  hasActiveChild: boolean;
  onLinkClick?: () => void;
}

const SettingsMenu: React.FC<SettingsMenuProps> = ({ 
  item, 
  isOpen, 
  settingsExpanded, 
  setSettingsExpanded, 
  expandedGroups, 
  toggleGroup, 
  isActive, 
  hasActiveChild,
  onLinkClick
}) => {
  const Icon = item.icon;
  
  return (
    <div>
      <button
        onClick={() => setSettingsExpanded(!settingsExpanded)}
        className={cn(
          "w-full flex items-center gap-3 px-3 py-2 rounded-lg transition-all duration-150",
          hasActiveChild
            ? "bg-primary/10 text-primary"
            : "text-muted-foreground hover:bg-accent hover:text-foreground"
        )}
      >
        <Icon className="h-4 w-4 flex-shrink-0" />
        {isOpen && (
          <>
            <span className="text-sm font-medium truncate flex-1 text-left">{item.title}</span>
            {settingsExpanded ? (
              <ChevronDown className="h-3.5 w-3.5" />
            ) : (
              <ChevronRight className="h-3.5 w-3.5" />
            )}
          </>
        )}
      </button>
      
      {isOpen && settingsExpanded && (
        <div className="mt-1 ml-4 pl-3 border-l border-border space-y-1">
          {SETTINGS_SUB_ITEMS.map((subItem) => {
            // Group with children
            if (subItem.children && subItem.icon) {
              const GroupIcon = subItem.icon;
              const isGroupExpanded = expandedGroups[subItem.label || ''] || false;
              const hasActiveGroupChild = subItem.children.some(child => isActive(child.path));
              
              return (
                <div key={subItem.label}>
                  <button
                    onClick={() => toggleGroup(subItem.label || '')}
                    className={cn(
                      "w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-sm transition-colors",
                      hasActiveGroupChild
                        ? "text-primary font-medium"
                        : "text-muted-foreground hover:text-foreground"
                    )}
                  >
                    <GroupIcon className="h-3.5 w-3.5 flex-shrink-0" />
                    <span className="truncate flex-1 text-left">{subItem.label}</span>
                    {isGroupExpanded ? (
                      <ChevronDown className="h-3 w-3" />
                    ) : (
                      <ChevronRight className="h-3 w-3" />
                    )}
                  </button>
                  
                  {isGroupExpanded && (
                    <div className="mt-0.5 ml-3 pl-2 border-l border-border/50 space-y-0.5">
                      {subItem.children.map((child) => {
                        const ChildIcon = child.icon;
                        const childActive = isActive(child.path);
                        
                        return (
                          <Link
                            key={child.path}
                            to={child.path}
                            onClick={onLinkClick}
                            className={cn(
                              "flex items-center gap-2 px-2 py-1 rounded text-xs transition-colors",
                              childActive
                                ? "bg-primary text-primary-foreground"
                                : "text-muted-foreground hover:bg-accent hover:text-foreground"
                            )}
                          >
                            <ChildIcon className="h-3 w-3 flex-shrink-0" />
                            <span className="truncate">{child.label}</span>
                          </Link>
                        );
                      })}
                    </div>
                  )}
                </div>
              );
            }
            
            // Regular settings item
            if (subItem.path && subItem.icon) {
              const SubIcon = subItem.icon;
              const subActive = isActive(subItem.path);
              
              return (
                <Link
                  key={subItem.path}
                  to={subItem.path}
                  onClick={onLinkClick}
                  className={cn(
                    "flex items-center gap-2 px-2 py-1.5 rounded-md text-sm transition-colors",
                    subActive
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-accent hover:text-foreground"
                  )}
                >
                  <SubIcon className="h-3.5 w-3.5 flex-shrink-0" />
                  <span className="truncate">{subItem.label || subItem.path}</span>
                </Link>
              );
            }
            
            return null;
          })}
        </div>
      )}
    </div>
  );
};

export default Sidebar;

