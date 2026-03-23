import React, { useState, Children, cloneElement, ReactNode } from 'react';
import { cn } from '../../lib/utils';

export interface TabsProps {
  children: ReactNode;
  defaultActiveTab?: number;
  onTabChange?: (index: number) => void;
  className?: string;
}

export function Tabs({ children, defaultActiveTab, onTabChange, className = '' }: TabsProps) {
  const [activeTab, setActiveTab] = useState<number>(defaultActiveTab ?? 0);

  const handleTabChange = (index: number): void => {
    setActiveTab(index);
    onTabChange?.(index);
  };

  const childrenArray = Children.toArray(children);
  const tabs = childrenArray.map((child, index) => {
    const childProps = (child as React.ReactElement).props;
    return {
      label: childProps.label,
      icon: childProps.icon,
      disabled: childProps.disabled,
      index,
    };
  });

  const activeTabContent = childrenArray[activeTab] as React.ReactElement;

  return (
    <div className={cn('w-full', className)}>
      <div
        className="flex flex-wrap gap-1 md:inline-flex h-8 md:items-center md:justify-center rounded bg-muted p-0.5 text-muted-foreground"
        role="tablist"
      >
        {tabs.map((tab) => (
          <button
            key={tab.index}
            className={cn(
              'inline-flex items-center justify-center whitespace-nowrap rounded px-2 md:px-3 py-1.5 md:py-1 text-xs md:text-sm font-medium ring-offset-background transition-all focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 min-h-[44px] md:min-h-0',
              activeTab === tab.index
                ? 'bg-background text-foreground shadow-sm'
                : 'text-muted-foreground hover:text-foreground'
            )}
            role="tab"
            aria-selected={activeTab === tab.index}
            aria-controls={`tabpanel-${tab.index}`}
            id={`tab-${tab.index}`}
            onClick={() => !tab.disabled && handleTabChange(tab.index)}
            disabled={tab.disabled}
          >
            {tab.icon && <span className="mr-1 md:mr-2">{tab.icon}</span>}
            <span>{tab.label}</span>
          </button>
        ))}
      </div>
      <div
        className="mt-2"
        role="tabpanel"
        id={`tabpanel-${activeTab}`}
        aria-labelledby={`tab-${activeTab}`}
      >
        {activeTabContent && cloneElement(activeTabContent, { active: true })}
      </div>
    </div>
  );
}

export interface TabPanelProps {
  children: ReactNode;
  label: string;
  icon?: ReactNode;
  disabled?: boolean;
  active?: boolean;
  className?: string;
}

export function TabPanel({ children, label, icon, disabled, active, className = '' }: TabPanelProps) {
  if (!active) return null;
  return <div className={cn(className)}>{children}</div>;
}
