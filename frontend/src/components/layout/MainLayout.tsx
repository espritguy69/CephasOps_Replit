import React, { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import TopNav from './TopNav';
import Sidebar from './Sidebar';

/**
 * MainLayout - macOS-inspired app shell with frosted glass effects
 * 
 * Provides the main application structure with:
 * - Fixed top navigation bar (frosted)
 * - Collapsible sidebar (frosted)
 * - Scrollable content area
 */
interface MainLayoutProps {
  children: ReactNode;
}

const MainLayout: React.FC<MainLayoutProps> = ({ children }) => {
  const [sidebarOpen, setSidebarOpen] = React.useState<boolean>(true);
  const [mobileMenuOpen, setMobileMenuOpen] = React.useState<boolean>(false);

  const toggleSidebar = (): void => {
    // On mobile, toggle mobile menu drawer
    if (window.innerWidth < 768) {
      setMobileMenuOpen(!mobileMenuOpen);
    } else {
      // On tablet/desktop, toggle sidebar collapse
      setSidebarOpen(!sidebarOpen);
    }
  };

  // Close mobile menu when clicking outside
  const handleOverlayClick = (): void => {
    if (mobileMenuOpen) {
      setMobileMenuOpen(false);
    }
  };

  return (
    <div data-testid="app-shell" className="flex h-screen overflow-hidden bg-background">
      {/* Fixed Top Navigation */}
      <TopNav onMenuClick={toggleSidebar} />
      
      {/* Mobile Overlay Backdrop */}
      {mobileMenuOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-30 lg:hidden"
          onClick={handleOverlayClick}
          aria-hidden="true"
        />
      )}
      
      {/* Main Content Area */}
      <div className="flex flex-1 overflow-hidden pt-14">
        {/* Sidebar */}
        <Sidebar 
          isOpen={sidebarOpen} 
          mobileOpen={mobileMenuOpen}
          onMobileClose={() => setMobileMenuOpen(false)}
        />
        
        {/* Content Area */}
        <main data-testid="app-shell-main" className={cn(
          "flex-1 overflow-y-auto transition-smooth bg-muted/30 w-full",
          // Mobile: no margin (sidebar is overlay)
          // Tablet: no margin needed (sidebar is relative, takes space naturally)
          // Desktop: margin for fixed sidebar
          "ml-0 lg:ml-64",
          // Override desktop margin when sidebar is collapsed
          !sidebarOpen && "lg:ml-16"
        )}>
          <div className="min-h-full">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
};

export default MainLayout;

