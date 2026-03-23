import React, { ReactNode } from 'react';
import { AlertTriangle, Info } from 'lucide-react';
import Modal from './Modal';
import Button from './Button';
import LoadingSpinner from './LoadingSpinner';

interface ConfirmDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm?: () => void;
  title?: string;
  message?: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'danger' | 'warning' | 'info';
  isLoading?: boolean;
}

const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
  isOpen,
  onClose,
  onConfirm,
  title = 'Confirm Action',
  message = 'Are you sure you want to proceed?',
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  variant = 'danger',
  isLoading = false
}) => {
  const handleConfirm = (): void => {
    if (onConfirm) {
      onConfirm();
    }
  };

  const iconVariants: Record<string, ReactNode> = {
    danger: <AlertTriangle className="h-6 w-6 text-destructive" />,
    warning: <AlertTriangle className="h-6 w-6 text-yellow-500" />,
    info: <Info className="h-6 w-6 text-primary" />
  };

  const confirmButtonVariants: Record<string, 'destructive' | 'default' | 'outline' | 'ghost' | 'link'> = {
    danger: 'destructive',
    warning: 'default',
    info: 'default'
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      size="small"
      closeOnOverlayClick={!isLoading}
      closeOnEscape={!isLoading}
      title={title}
    >
      <div className="space-y-6">
        <div className="flex flex-col items-center text-center space-y-4">
          <div className="flex items-center justify-center">
            {iconVariants[variant]}
          </div>
          <p className="text-sm text-muted-foreground">{message}</p>
        </div>
        <div className="flex gap-4 justify-end pt-4 border-t">
          <Button
            variant="outline"
            onClick={onClose}
            disabled={isLoading}
          >
            {cancelText}
          </Button>
          <Button
            variant={confirmButtonVariants[variant]}
            onClick={handleConfirm}
            disabled={isLoading}
          >
            {isLoading ? (
              <>
                <LoadingSpinner size="sm" message="" />
                <span className="ml-2">Processing...</span>
              </>
            ) : (
              confirmText
            )}
          </Button>
        </div>
      </div>
    </Modal>
  );
};

export default ConfirmDialog;

