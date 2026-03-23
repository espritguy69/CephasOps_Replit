import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Mail, FileText, Shield, Cog } from 'lucide-react';
import { PageShell } from '../../components/layout';
import EmailMailboxesPage from '../../features/email/EmailMailboxesPage';
import EmailRulesPage from '../../features/email/EmailRulesPage';
import VipPage from '../../features/email/VipPage';
import ParserTemplatesPage from '../../features/email/ParserTemplatesPage';
import { cn } from '@/lib/utils';
import type { LucideIcon } from 'lucide-react';

// ============================================================================
// Types
// ============================================================================

type TabId = 'mailboxes' | 'rules' | 'vip' | 'parsers';

interface Tab {
  id: TabId;
  label: string;
  icon: LucideIcon;
  component: React.ReactNode;
}

// ============================================================================
// Component
// ============================================================================

const EmailSetupPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  
  // Determine active tab from URL hash or default to mailboxes
  const getActiveTab = (): TabId => {
    const hash = location.hash;
    if (hash.startsWith('#vip')) return 'vip'; // Handles #vip-groups, #vip-emails, #vip
    if (hash === '#rules') return 'rules';
    if (hash === '#parsers') return 'parsers';
    return 'mailboxes'; // default
  };

  const [activeTab, setActiveTab] = useState<TabId>(getActiveTab());

  const tabs: Tab[] = [
    {
      id: 'mailboxes',
      label: 'Email Mailboxes',
      icon: Mail,
      component: <EmailMailboxesPage />
    },
    {
      id: 'rules',
      label: 'Email Rules',
      icon: FileText,
      component: <EmailRulesPage />
    },
    {
      id: 'vip',
      label: 'VIP',
      icon: Shield,
      component: <VipPage />
    },
    {
      id: 'parsers',
      label: 'Parser Templates',
      icon: Cog,
      component: <ParserTemplatesPage />
    }
  ];

  const handleTabChange = (tabId: TabId) => {
    setActiveTab(tabId);
    if (tabId === 'vip') {
      // Default to groups sub-tab when switching to VIP
      navigate(`/settings/email#vip-groups`, { replace: true });
    } else {
      navigate(`/settings/email#${tabId}`, { replace: true });
    }
  };

  // Update active tab when hash changes
  useEffect(() => {
    const hash = location.hash;
    if (hash.startsWith('#vip')) setActiveTab('vip');
    else if (hash === '#rules') setActiveTab('rules');
    else if (hash === '#parsers') setActiveTab('parsers');
    else setActiveTab('mailboxes');
  }, [location.hash]);

  // Set default hash if none exists
  useEffect(() => {
    if (!location.hash) {
      navigate('/settings/email#mailboxes', { replace: true });
    }
  }, [location.hash, navigate]);

  const activeTabData = tabs.find(tab => tab.id === activeTab);

  return (
    <PageShell title="Email Setup" breadcrumbs={[{ label: 'Settings' }, { label: 'Email' }]}>
    <div className="flex flex-col h-full">
      {/* Tab Navigation */}
      <div className="border-b border-slate-700 bg-layout-sidebar">
        <div className="flex space-x-1 px-4">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;
            
            return (
              <button
                key={tab.id}
                onClick={() => handleTabChange(tab.id)}
                className={cn(
                  "flex items-center gap-2 px-4 py-3 text-sm font-medium transition-colors border-b-2",
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

      {/* Tab Content */}
      <div className="flex-1 overflow-auto">
        {activeTabData?.component}
      </div>
      </div>
    </PageShell>
  );
};

export default EmailSetupPage;

