import React, { useEffect, ReactNode } from 'react';
import { X } from 'lucide-react';
import { cn } from '../../lib/utils';

interface ModalProps {
  isOpen: boolean;
  onClose?: () => void;
  title?: string;
  children: ReactNode;
  size?: 'small' | 'medium' | 'large' | 'xl';
  closeOnOverlayClick?: boolean;
  closeOnEscape?: boolean;
  className?: string;
}

const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  children,
  size = 'medium',
  closeOnOverlayClick = true,
  closeOnEscape = true,
  className = '',
}) => {
  useEffect(() => {
    if (!isOpen) return;

    const handleEscape = (e: KeyboardEvent): void => {
      if (e.key === 'Escape' && closeOnEscape && onClose) {
        onClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    document.body.style.overflow = 'hidden';

    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = '';
    };
  }, [isOpen, closeOnEscape, onClose]);

  if (!isOpen) return null;

  const handleOverlayClick = (e: React.MouseEvent<HTMLDivElement>): void => {
    if (e.target === e.currentTarget && closeOnOverlayClick && onClose) {
      onClose();
    }
  };

  const sizeStyles: Record<string, string> = {
    small: 'w-full max-w-[calc(100vw-2rem)] md:max-w-md lg:max-w-md',
    medium: 'w-full max-w-[calc(100vw-2rem)] md:max-w-lg lg:max-w-lg',
    large: 'w-full max-w-[calc(100vw-2rem)] md:max-w-2xl lg:max-w-4xl',
    xl: 'w-full max-w-[calc(100vw-2rem)] md:max-w-4xl lg:max-w-6xl',
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-3 md:p-4"
      onClick={handleOverlayClick}
    >
      <div
        className={cn(
          'relative bg-background rounded-lg shadow-lg flex flex-col max-h-[calc(100vh-2rem)] md:max-h-[90vh] w-full border border-border',
          sizeStyles[size],
          className
        )}
        role="dialog"
        aria-modal="true"
        aria-labelledby={title ? 'modal-title' : undefined}
        onClick={(e) => e.stopPropagation()}
      >
        {(title || onClose) && (
          <div className="flex items-center justify-between p-3 md:p-4 border-b border-border flex-shrink-0">
            {title && (
              <h2 id="modal-title" className="text-sm md:text-base font-semibold pr-2">
                {title}
              </h2>
            )}
            {onClose && (
              <button
                type="button"
                className="rounded-sm opacity-70 ring-offset-background transition-opacity hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:pointer-events-none min-h-[44px] min-w-[44px] flex items-center justify-center"
                onClick={onClose}
                aria-label="Close modal"
              >
                <X className="h-4 w-4" />
                <span className="sr-only">Close</span>
              </button>
            )}
          </div>
        )}
        <div className="p-3 md:p-4 lg:p-6 overflow-y-auto flex-1">
          <div className="space-y-2 md:space-y-3">
            {children}
          </div>
        </div>
      </div>
    </div>
  );
};

export default Modal;
