import React, { ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Home, User, LogOut, BarChart3, DollarSign, Briefcase, Package, QrCode, List, Users, RotateCcw } from 'lucide-react';
import { cn } from '../../lib/utils';
import { useAuth } from '../../contexts/AuthContext';

interface MainLayoutProps {
  children: ReactNode;
}

export function MainLayout({ children }: MainLayoutProps) {
  const location = useLocation();
  const { user, logout, isSubcontractor, isAdmin } = useAuth();

  return (
    <div className="flex flex-col h-screen bg-background">
      {/* Top Navigation Bar */}
      <header className="sticky top-0 z-50 w-full border-b bg-card safe-area-top">
        <div className="flex h-14 items-center px-4">
          <div className="flex-1">
            <h1 className="text-lg font-semibold">CephasOps SI</h1>
            {user && (
              <p className="text-xs text-muted-foreground">{user.name || user.email}</p>
            )}
          </div>
          <nav className="flex items-center gap-2">
            {isAdmin && (
              <>
                <Link
                  to="/service-installers"
                  className={cn(
                    "touch-target flex items-center justify-center rounded-lg px-3 transition-colors",
                    location.pathname === "/service-installers"
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                  )}
                  title="Service Installers"
                >
                  <Users className="h-5 w-5" />
                </Link>
                <Link
                  to="/materials/scan"
                  className={cn(
                    "touch-target flex items-center justify-center rounded-lg px-3 transition-colors",
                    location.pathname === "/materials/scan"
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                  )}
                  title="Scan Materials"
                >
                  <QrCode className="h-5 w-5" />
                </Link>
                <Link
                  to="/materials/tracking"
                  className={cn(
                    "touch-target flex items-center justify-center rounded-lg px-3 transition-colors",
                    location.pathname.startsWith("/materials/tracking")
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                  )}
                  title="Material Tracking"
                >
                  <Package className="h-5 w-5" />
                </Link>
                <Link
                  to="/materials/returns"
                  className={cn(
                    "touch-target flex items-center justify-center rounded-lg px-3 transition-colors",
                    location.pathname === "/materials/returns"
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                  )}
                  title="Material Returns"
                >
                  <RotateCcw className="h-5 w-5" />
                </Link>
              </>
            )}
            <Link
              to="/dashboard"
              className={cn(
                "touch-target flex items-center justify-center rounded-lg px-3 transition-colors",
                location.pathname === "/dashboard"
                  ? "bg-primary text-primary-foreground"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )}
            >
              <BarChart3 className="h-5 w-5" />
            </Link>
            <button
              className="touch-target flex items-center justify-center rounded-lg px-3 text-muted-foreground hover:bg-accent hover:text-accent-foreground transition-colors"
              onClick={logout}
              title="Logout"
            >
              <LogOut className="h-5 w-5" />
            </button>
          </nav>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 overflow-y-auto safe-area-bottom">
        {children}
      </main>

      {/* Bottom Navigation (Mobile) */}
      <nav className="sticky bottom-0 z-50 w-full border-t bg-card safe-area-bottom md:hidden">
        <div className="flex h-16 items-center justify-around">
          {/* Dashboard Link (always visible) */}
          <Link
            to="/dashboard"
            className={cn(
              "touch-target flex flex-col items-center justify-center flex-1 gap-1 transition-colors",
              location.pathname === "/dashboard"
                ? "text-primary"
                : "text-muted-foreground"
            )}
          >
            <BarChart3 className="h-5 w-5" />
            <span className="text-xs">Dashboard</span>
          </Link>
          {/* Jobs/Orders Link */}
          {isAdmin ? (
            <Link
              to="/orders"
              className={cn(
                "touch-target flex flex-col items-center justify-center flex-1 gap-1 transition-colors",
                location.pathname.startsWith("/orders") || location.pathname.startsWith("/jobs")
                  ? "text-primary"
                  : "text-muted-foreground"
              )}
            >
              <List className="h-5 w-5" />
              <span className="text-xs">Orders</span>
            </Link>
          ) : (
            <Link
              to="/jobs"
              className={cn(
                "touch-target flex flex-col items-center justify-center flex-1 gap-1 transition-colors",
                location.pathname.startsWith("/jobs")
                  ? "text-primary"
                  : "text-muted-foreground"
              )}
            >
              <Briefcase className="h-5 w-5" />
              <span className="text-xs">Jobs</span>
            </Link>
          )}
          {/* Materials Link (admin only) */}
          {isAdmin && (
            <>
              <Link
                to="/materials/tracking"
                className={cn(
                  "touch-target flex flex-col items-center justify-center flex-1 gap-1 transition-colors",
                  location.pathname.startsWith("/materials") && !location.pathname.includes("/returns")
                    ? "text-primary"
                    : "text-muted-foreground"
                )}
              >
                <Package className="h-5 w-5" />
                <span className="text-xs">Materials</span>
              </Link>
              <Link
                to="/materials/returns"
                className={cn(
                  "touch-target flex flex-col items-center justify-center flex-1 gap-1 transition-colors",
                  location.pathname === "/materials/returns"
                    ? "text-primary"
                    : "text-muted-foreground"
                )}
              >
                <RotateCcw className="h-5 w-5" />
                <span className="text-xs">Returns</span>
              </Link>
            </>
          )}
          {/* Earnings Link (subcontractors only) */}
          {isSubcontractor && !isAdmin && (
            <Link
              to="/earnings"
              className={cn(
                "touch-target flex flex-col items-center justify-center flex-1 gap-1 transition-colors",
                location.pathname === "/earnings"
                  ? "text-primary"
                  : "text-muted-foreground"
              )}
            >
              <DollarSign className="h-5 w-5" />
              <span className="text-xs">Earnings</span>
            </Link>
          )}
          {/* Profile Link (always visible) */}
          <button
            className="touch-target flex flex-col items-center justify-center flex-1 gap-1 text-muted-foreground transition-colors"
            onClick={() => {
              // TODO: Navigate to profile page
              console.log('Profile');
            }}
          >
            <User className="h-5 w-5" />
            <span className="text-xs">Profile</span>
          </button>
        </div>
      </nav>
    </div>
  );
}

