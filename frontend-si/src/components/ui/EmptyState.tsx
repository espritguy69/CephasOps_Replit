import React from 'react';
import { Inbox } from 'lucide-react';
import { Button } from './Button';

interface EmptyStateAction {
  label: string;
  onClick: () => void;
}

interface EmptyStateProps {
  title: string;
  description?: string;
  message?: string;
  icon?: React.ReactNode;
  action?: React.ReactNode | EmptyStateAction;
  className?: string;
}

export function EmptyState({
  title,
  description,
  message,
  icon,
  action,
  className,
}: EmptyStateProps) {
  const displayMessage = description ?? message;
  const renderAction = () => {
    if (!action) return null;
    if (
      typeof action === 'object' &&
      action !== null &&
      !React.isValidElement(action) &&
      'label' in action &&
      'onClick' in action
    ) {
      const a = action as EmptyStateAction;
      return (
        <Button onClick={a.onClick} className="mt-4">
          {a.label}
        </Button>
      );
    }
    return <div className="mt-4">{action as React.ReactNode}</div>;
  };
  return (
    <div className={`flex flex-col items-center justify-center py-12 px-4 text-center ${className ?? ''}`}>
      {icon ? (
        <div className="mb-4 text-muted-foreground">{icon}</div>
      ) : (
        <Inbox className="h-12 w-12 mb-4 text-muted-foreground" />
      )}
      <h3 className="text-lg font-semibold mb-2">{title}</h3>
      {displayMessage && (
        <p className="text-sm text-muted-foreground max-w-sm mb-4">{displayMessage}</p>
      )}
      {renderAction()}
    </div>
  );
}

