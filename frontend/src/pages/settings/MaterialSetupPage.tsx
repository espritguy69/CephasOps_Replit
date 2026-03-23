import React, { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Package, FolderOpen, Network, Tag } from 'lucide-react';
import { PageShell } from '../../components/layout';
import MaterialsPage from './MaterialsPage';
import MaterialCategoriesPage from './MaterialCategoriesPage';
import MaterialVerticalsPage from './MaterialVerticalsPage';
import MaterialTagsPage from './MaterialTagsPage';
import { cn } from '@/lib/utils';
import type { Icon } from 'lucide-react';

interface Tab {
  id: string;
  label: string;
  icon: Icon;
  component: React.ReactElement;
}

const MaterialSetupPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  
  // Determine active tab from URL hash or default to materials
  const getActiveTab = (): string => {
    const hash = location.hash;
    if (hash === '#categories') return 'categories';
    if (hash === '#verticals') return 'verticals';
    if (hash === '#tags') return 'tags';
    return 'materials'; // default
  };

  const [activeTab, setActiveTab] = useState<string>(getActiveTab());

  const tabs: Tab[] = [
    {
      id: 'materials',
      label: 'Materials',
      icon: Package,
      component: <MaterialsPage />
    },
    {
      id: 'categories',
      label: 'Categories',
      icon: FolderOpen,
      component: <MaterialCategoriesPage />
    },
    {
      id: 'verticals',
      label: 'Verticals',
      icon: Network,
      component: <MaterialVerticalsPage />
    },
    {
      id: 'tags',
      label: 'Tags',
      icon: Tag,
      component: <MaterialTagsPage />
    }
  ];

  const handleTabChange = (tabId: string): void => {
    setActiveTab(tabId);
    navigate(`/settings/materials#${tabId}`, { replace: true });
  };

  // Update active tab when hash changes
  useEffect(() => {
    const hash = location.hash;
    if (hash === '#categories') setActiveTab('categories');
    else if (hash === '#verticals') setActiveTab('verticals');
    else if (hash === '#tags') setActiveTab('tags');
    else setActiveTab('materials');
  }, [location.hash]);

  // Set default hash if none exists
  useEffect(() => {
    if (!location.hash) {
      navigate('/settings/materials#materials', { replace: true });
    }
  }, [location.hash, navigate]);

  const activeTabData = tabs.find(tab => tab.id === activeTab);

  return (
    <PageShell
      title="Material Setup"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'Materials' }]}
    >
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

export default MaterialSetupPage;

