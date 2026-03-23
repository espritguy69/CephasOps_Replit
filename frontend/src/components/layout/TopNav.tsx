import React, { useState } from 'react';
import { Menu, ChevronDown, LogOut, User, Search, Command, Building2, Check, Sun, Moon } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { useNavigate, useLocation } from 'react-router-dom';
import NotificationBell from '../notifications/NotificationBell';
import { Dropdown, DropdownItem, DropdownHeader, Button } from '../ui';
import { cn } from '@/lib/utils';
import { useDepartment } from '../../contexts/DepartmentContext';
import { useTheme } from '../../contexts/ThemeContext';

type Environment = 'development' | 'staging' | 'production';

// Environment configuration - could come from env vars
const ENV_CONFIG: Record<Environment, { label: string; color: string }> = {
  development: { label: 'Dev', color: 'bg-amber-500 text-amber-950' },
  staging: { label: 'Staging', color: 'bg-purple-500 text-purple-950' },
  production: { label: 'Prod', color: 'bg-emerald-500 text-emerald-950' }
};

const getEnvironment = (): Environment => {
  const hostname = window.location.hostname;
  if (hostname === 'localhost' || hostname === '127.0.0.1' || hostname.includes('.replit.dev')) return 'development';
  if (hostname.includes('staging') || hostname.includes('test')) return 'staging';
  return 'production';
};

// Page titles mapping
const PAGE_TITLES: Record<string, string> = {
  '/dashboard': 'Operations Dashboard',
  '/orders': 'Orders',
  '/scheduler': 'Scheduler',
  '/tasks/my': 'My Tasks',
  '/inventory': 'Inventory',
  '/billing': 'Billing',
  '/billing/invoices': 'Invoices',
  '/payroll': 'Payroll',
  '/pnl': 'P&L Analysis',
  '/accounting': 'Accounting',
  '/assets': 'Assets',
  '/notifications': 'Notifications',
  '/orders/parser': 'Parser List',
  '/orders/parser/snapshots': 'Parser Snapshots',
  '/workflow': 'Workflow',
  '/documents': 'Documents',
  '/files': 'Files',
  '/buildings': 'Buildings',
  '/settings': 'Settings'
};

const getPageTitle = (pathname: string): string => {
  // Exact match first
  if (PAGE_TITLES[pathname]) return PAGE_TITLES[pathname];
  
  // Partial match (for nested routes)
  const matchingPath = Object.keys(PAGE_TITLES).find(path => 
    pathname.startsWith(path) && path !== '/'
  );
  
  return matchingPath ? PAGE_TITLES[matchingPath] : 'CephasOps';
};

interface TopNavProps {
  onMenuClick: () => void;
}

const TopNav: React.FC<TopNavProps> = ({ onMenuClick }) => {
  const { user, logout } = useAuth();
  const {
    departments,
    activeDepartment,
    selectDepartment,
    loading: departmentsLoading,
    error: departmentError
  } = useDepartment();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchFocused, setSearchFocused] = useState<boolean>(false);
  
  const environment = getEnvironment();
  const envConfig = ENV_CONFIG[environment];
  const pageTitle = getPageTitle(location.pathname);
  const departmentLabel = departmentsLoading
    ? 'Loading departments...'
    : activeDepartment?.name || 'All Departments';

  const { theme, toggleTheme } = useTheme();

  const handleLogout = async (): Promise<void> => {
    await logout();
    navigate('/login');
  };

  const handleDepartmentSelect = (departmentId: string | null): void => {
    if (!departmentId) {
      selectDepartment('');
      return;
    }
    selectDepartment(departmentId);
  };

  const renderDepartmentItems = (): React.ReactNode => (
    <>
      <DropdownHeader>
        <div className="py-1">
          <p className="text-xs font-semibold text-foreground">Active Department</p>
          <p className="text-[10px] text-muted-foreground">Controls data scope</p>
          {departmentError && (
            <p className="text-[10px] text-destructive mt-1">{departmentError}</p>
          )}
        </div>
      </DropdownHeader>
      <DropdownItem onClick={() => handleDepartmentSelect(null)}>
        <div className="flex items-center justify-between w-full">
          <span className="text-sm">All Departments</span>
          {!activeDepartment && <Check className="h-3 w-3" />}
        </div>
      </DropdownItem>
      {departments?.map((dept) => (
        <DropdownItem key={dept.id} onClick={() => handleDepartmentSelect(dept.id)}>
          <div className="flex items-center justify-between w-full">
            <div className="flex flex-col">
              <span className="text-sm">{dept.name}</span>
              {dept.code && (
                <span className="text-[10px] text-muted-foreground uppercase tracking-wide">
                  {dept.code}
                </span>
              )}
            </div>
            {activeDepartment?.id === dept.id && <Check className="h-3 w-3" />}
          </div>
        </DropdownItem>
      ))}
    </>
  );

  return (
    <nav className="fixed top-0 left-0 right-0 h-14 frosted-glass-strong border-b border-border z-50 flex items-center px-4">
      {/* Left Section - Menu & Logo */}
      <div className="flex items-center gap-3">
        <Button
          variant="ghost"
          size="icon"
          onClick={onMenuClick}
          aria-label="Toggle sidebar"
          className="h-9 w-9 min-h-[44px] min-w-[44px] md:h-9 md:w-9"
        >
          <Menu className="h-5 w-5" />
        </Button>
        
        <div className="flex items-center gap-3">
          {/* Logo */}
          <div className="flex items-center gap-2">
            <div className="h-8 w-8 rounded-lg bg-gradient-to-br from-brand-500 to-brand-700 flex items-center justify-center">
              <span className="text-white font-bold text-sm">CO</span>
            </div>
            <span className="text-sm font-semibold text-foreground hidden sm:inline">CephasOps</span>
          </div>
          
          {/* Page Title with separator */}
          <div className="hidden md:flex items-center gap-3">
            <div className="h-5 w-px bg-border" />
            <h1 className="text-sm font-medium text-foreground">{pageTitle}</h1>
          </div>
          
          {/* Environment Badge */}
          <span className={cn(
            "px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider",
            envConfig.color
          )}>
            {envConfig.label}
          </span>
        </div>
      </div>

      {/* Center Section - Global Search */}
      <div className="flex-1 max-w-xl mx-6 hidden md:block">
        <div className={cn(
          "relative transition-all duration-200",
          searchFocused && "scale-[1.02]"
        )}>
          <div className="absolute left-3 top-1/2 -translate-y-1/2 flex items-center gap-2 pointer-events-none">
            <Search className="h-4 w-4 text-muted-foreground" />
          </div>
          <input
            type="text"
            placeholder="Search orders, buildings, installers..."
            onFocus={() => setSearchFocused(true)}
            onBlur={() => setSearchFocused(false)}
            className={cn(
              "w-full h-9 pl-10 pr-16 text-sm bg-muted/50 border border-transparent rounded-lg",
              "placeholder:text-muted-foreground/60",
              "focus:outline-none focus:border-brand-500/50 focus:bg-background focus:ring-2 focus:ring-brand-500/20",
              "transition-all duration-200"
            )}
          />
          <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1 pointer-events-none">
            <kbd className="hidden lg:inline-flex h-5 items-center gap-0.5 rounded border border-border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground">
              <Command className="h-2.5 w-2.5" />K
            </kbd>
          </div>
        </div>
      </div>

      {/* Department Selector */}
      <div className="hidden lg:flex items-center">
        <Dropdown
          trigger={
            <Button
              data-testid="department-selector-trigger"
              variant="outline"
              size="sm"
              className="h-9 px-3 gap-2 text-xs font-medium whitespace-nowrap min-h-[44px]"
              disabled={departmentsLoading && !departments?.length}
            >
              <Building2 className="h-4 w-4 flex-shrink-0" />
              <span className="hidden sm:inline truncate max-w-[200px]">{departmentLabel}</span>
              <ChevronDown className="h-3 w-3 hidden sm:inline flex-shrink-0" />
            </Button>
          }
        >
          {renderDepartmentItems()}
        </Dropdown>
      </div>

      {/* Right Section */}
      <div className="flex items-center gap-1">
        {/* Mobile Department Selector */}
        <Dropdown
          trigger={
            <Button
              variant="ghost"
              size="icon"
              className="h-9 w-9 min-h-[44px] min-w-[44px] lg:hidden"
              aria-label="Select department"
            >
              <Building2 className="h-4 w-4" />
            </Button>
          }
        >
          {renderDepartmentItems()}
        </Dropdown>

        {/* Mobile Search Button */}
        <Button
          variant="ghost"
          size="icon"
          className="h-9 w-9 min-h-[44px] min-w-[44px] md:hidden"
          aria-label="Search"
        >
          <Search className="h-4 w-4" />
        </Button>

        {/* Theme Toggle */}
        <Button
          variant="ghost"
          size="icon"
          onClick={toggleTheme}
          className="h-9 w-9 min-h-[44px] min-w-[44px]"
          aria-label={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
        >
          {theme === 'light' ? (
            <Moon className="h-4 w-4" />
          ) : (
            <Sun className="h-4 w-4" />
          )}
        </Button>

        {/* Notification Bell */}
        <NotificationBell />
        
        {/* User Menu */}
        <Dropdown
          trigger={
            <button data-testid="user-menu-trigger" className="flex items-center gap-2 px-2 py-1.5 rounded-lg hover:bg-accent transition-colors ml-1 min-h-[44px]">
              <div className="h-8 w-8 rounded-full bg-gradient-to-br from-brand-400 to-brand-600 text-white flex items-center justify-center text-xs font-semibold ring-2 ring-background flex-shrink-0">
                {user?.name?.charAt(0).toUpperCase() || 'U'}
              </div>
              <div className="hidden lg:block text-left">
                <p className="text-xs font-medium text-foreground leading-tight">
                  {user?.name || 'User'}
                </p>
                <p className="text-[10px] text-muted-foreground leading-tight">
                  {user?.roles?.[0] || 'Member'}
                </p>
              </div>
              <ChevronDown className="h-3 w-3 text-muted-foreground hidden lg:block flex-shrink-0" />
            </button>
          }
          placement="bottom-right"
        >
          <DropdownHeader>
            <div className="py-1">
              <p className="text-sm font-medium">{user?.name || 'User'}</p>
              <p className="text-xs text-muted-foreground">{user?.email || ''}</p>
            </div>
          </DropdownHeader>
          <DropdownItem onClick={() => navigate('/settings/profile')}>
            <User className="h-4 w-4 mr-2" />
            Profile Settings
          </DropdownItem>
          <DropdownItem data-testid="logout-action" onClick={handleLogout} className="text-destructive">
            <LogOut className="h-4 w-4 mr-2" />
            Sign Out
          </DropdownItem>
        </Dropdown>
      </div>
    </nav>
  );
};

export default TopNav;

