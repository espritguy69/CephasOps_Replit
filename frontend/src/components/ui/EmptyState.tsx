import React, { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import { Inbox } from 'lucide-react';
import Button from './Button';

interface EmptyStateAction {
  label: string;
  onClick: () => void;
}

interface EmptyStateProps {
  title?: string;
  description?: string;
  message?: string;
  icon?: ReactNode;
  action?: ReactNode | EmptyStateAction;
  className?: string;
}

const EmptyState: React.FC<EmptyStateProps> = ({ 
  title = 'No items found',
  description,
  message,
  icon,
  action,
  className = ''
}) => {
  const displayMessage = description || message;
  
  // Render action - handle both ReactNode and object format
  const renderAction = () => {
    if (!action) return null;
    
    // Check if action is an object with label and onClick (not a React element)
    if (
      typeof action === 'object' && 
      action !== null &&
      !React.isValidElement(action) &&
      'label' in action && 
      'onClick' in action && 
      typeof (action as EmptyStateAction).onClick === 'function'
    ) {
      const actionObj = action as EmptyStateAction;
      return (
        <Button onClick={actionObj.onClick} className="mt-4">
          {actionObj.label}
        </Button>
      );
    }
    
    // Otherwise, render as ReactNode
    return <div className="mt-4">{action as ReactNode}</div>;
  };
  
  return (
    <div className={cn("flex flex-col items-center justify-center py-12 px-4 text-center", className)}>
      {icon && (
        <div className="mb-4 text-muted-foreground">
          {icon}
        </div>
      )}
      {!icon && (
        <Inbox className="h-12 w-12 mb-4 text-muted-foreground" />
      )}
      <h3 className="text-lg font-semibold mb-2">{title}</h3>
      {displayMessage && (
        <p className="text-sm text-muted-foreground max-w-sm mb-4">{displayMessage}</p>
      )}
      {renderAction()}
    </div>
  );
};

export default EmptyState;

