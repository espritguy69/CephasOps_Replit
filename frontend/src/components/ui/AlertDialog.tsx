import React, { ReactNode } from 'react';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';
import Button from './Button';

interface AlertDialogProps {
  open: boolean;
  onOpenChange?: (open: boolean) => void;
  title?: string;
  description?: string;
  children?: ReactNode;
  className?: string;
}

const AlertDialog: React.FC<AlertDialogProps> & {
  Action: React.FC<AlertDialogActionProps>;
  Cancel: React.FC<AlertDialogCancelProps>;
} = ({ open, onOpenChange, title, description, children, className = '' }) => {
  if (!open) return null;

  return (
    <div 
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 animate-in fade-in"
      onClick={() => onOpenChange?.(false)}
    >
      <div
        className={cn(
          "relative bg-background rounded-lg shadow-lg flex flex-col max-w-md w-full animate-in slide-in-from-bottom-4",
          className
        )}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="p-4 border-b">
          <div className="flex items-center justify-between">
            {title && <h3 className="text-sm font-semibold">{title}</h3>}
            {onOpenChange && (
              <button
                onClick={() => onOpenChange(false)}
                className="rounded-sm opacity-70 hover:opacity-100 transition-opacity"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
          {description && (
            <p className="text-sm text-muted-foreground mt-2">{description}</p>
          )}
        </div>
        <div className="p-4">
          {children}
        </div>
      </div>
    </div>
  );
};

interface AlertDialogActionProps {
  children: ReactNode;
  onClick?: () => void;
  className?: string;
  variant?: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link';
}

const AlertDialogAction: React.FC<AlertDialogActionProps> = ({ children, onClick, className = '', variant = 'default' }) => {
  return (
    <Button onClick={onClick} variant={variant} className={className}>
      {children}
    </Button>
  );
};

interface AlertDialogCancelProps {
  children: ReactNode;
  onClick?: () => void;
  className?: string;
}

const AlertDialogCancel: React.FC<AlertDialogCancelProps> = ({ children, onClick, className = '' }) => {
  return (
    <Button onClick={onClick} variant="outline" className={className}>
      {children}
    </Button>
  );
};

AlertDialog.Action = AlertDialogAction;
AlertDialog.Cancel = AlertDialogCancel;

export default AlertDialog;

