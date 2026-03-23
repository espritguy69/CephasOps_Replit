import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Users, Mail } from 'lucide-react';
import VipGroupsPage from './VipGroupsPage';
import VipEmailsPage from './VipEmailsPage';
import { cn } from '@/lib/utils';
import type { LucideIcon } from 'lucide-react';

// ============================================================================
// Types
// ============================================================================

type VipSubTabId = 'groups' | 'emails';

interface VipSubTab {
  id: VipSubTabId;
  label: string;
  icon: LucideIcon;
  component: React.ReactNode;
}

// ============================================================================
// Component
// ============================================================================

const VipPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  
  const getActiveSubTab = (): VipSubTabId => {
    const hash = location.hash;
    // Support both old format (#vip-groups, #vip) and new format (#vip-groups, #vip-emails)
    if (hash === '#vip-emails' || hash === '#emails') return 'emails';
    if (hash === '#vip-groups' || hash === '#groups') return 'groups';
    // Default to groups
    return 'groups';
  };

  const [activeSubTab, setActiveSubTab] = useState<VipSubTabId>(getActiveSubTab());

  const subTabs: VipSubTab[] = [
    {
      id: 'groups',
      label: 'VIP Groups',
      icon: Users,
      component: <VipGroupsPage />
    },
    {
      id: 'emails',
      label: 'VIP Emails',
      icon: Mail,
      component: <VipEmailsPage />
    }
  ];

  const handleSubTabChange = (subTabId: VipSubTabId) => {
    setActiveSubTab(subTabId);
    // Update URL hash to maintain navigation state
    navigate(`/settings/email#vip-${subTabId}`, { replace: true });
  };

  // Update active sub-tab when hash changes
  useEffect(() => {
    const hash = location.hash;
    if (hash === '#vip-emails' || hash === '#emails') {
      setActiveSubTab('emails');
    } else if (hash === '#vip-groups' || hash === '#groups') {
      setActiveSubTab('groups');
    } else {
      // Default to groups if hash doesn't match
      setActiveSubTab('groups');
    }
  }, [location.hash]);

  const activeSubTabData = subTabs.find(tab => tab.id === activeSubTab);

  return (
    <div className="flex flex-col h-full">
      {/* Sub-Tab Navigation */}
      <div className="border-b border-slate-700 bg-layout-sidebar/50">
        <div className="flex space-x-1 px-4">
          {subTabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeSubTab === tab.id;
            
            return (
              <button
                key={tab.id}
                onClick={() => handleSubTabChange(tab.id)}
                className={cn(
                  "flex items-center gap-2 px-4 py-2.5 text-sm font-medium transition-colors border-b-2",
                  isActive
                    ? "border-brand-500 text-brand-400 bg-brand-500/10"
                    : "border-transparent text-slate-400 hover:text-slate-300 hover:border-slate-600"
                )}
              >
                <Icon className="h-4 w-4" />
                <span>{tab.label}</span>
              </button>
            );
          })}
        </div>
      </div>

      {/* Sub-Tab Content */}
      <div className="flex-1 overflow-auto">
        {activeSubTabData?.component}
      </div>
    </div>
  );
};

export default VipPage;

