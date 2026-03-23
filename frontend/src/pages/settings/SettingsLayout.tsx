import React, { ReactNode } from 'react';

interface SettingsLayoutProps {
  children: ReactNode;
}

const SettingsLayout: React.FC<SettingsLayoutProps> = ({ children }) => {
  // SettingsLayout now just wraps the content without the sidebar
  // The sidebar is handled in the main Sidebar component
  return (
    <div className="flex-1 overflow-y-auto">
      {children}
    </div>
  );
};

export default SettingsLayout;

