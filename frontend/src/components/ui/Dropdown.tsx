import React, { useState, useEffect, useRef, ReactNode } from 'react';
import { cn } from '@/lib/utils';

type DropdownPlacement = 'bottom-left' | 'bottom-right' | 'top-left' | 'top-right';

interface DropdownProps {
  trigger: ReactNode;
  children: ReactNode;
  placement?: DropdownPlacement;
  onOpen?: () => void;
  onClose?: () => void;
  className?: string;
  closeOnClick?: boolean;
}

const Dropdown: React.FC<DropdownProps> = ({
  trigger,
  children,
  placement = 'bottom-left',
  onOpen,
  onClose,
  className = '',
  closeOnClick = true,
  ...props
}) => {
  const [isOpen, setIsOpen] = useState<boolean>(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent): void => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        closeDropdown();
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      if (onOpen) onOpen();
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen, onOpen]);

  const toggleDropdown = (): void => {
    setIsOpen(!isOpen);
    if (!isOpen && onOpen) {
      onOpen();
    } else if (isOpen && onClose) {
      onClose();
    }
  };

  const closeDropdown = (): void => {
    setIsOpen(false);
    if (onClose) onClose();
  };

  const handleItemClick = (): void => {
    if (closeOnClick) {
      closeDropdown();
    }
  };

  const placementStyles: Record<DropdownPlacement, string> = {
    'bottom-left': 'top-full left-0 mt-1',
    'bottom-right': 'top-full right-0 mt-1',
    'top-left': 'bottom-full left-0 mb-1',
    'top-right': 'bottom-full right-0 mb-1'
  };

  return (
    <div
      ref={dropdownRef}
      className={cn("relative inline-block", className)}
      {...props}
    >
      <div className="cursor-pointer" onClick={toggleDropdown}>
        {trigger}
      </div>
      {isOpen && (
        <div className={cn(
          "absolute z-50 min-w-[8rem] rounded-md border bg-popover p-1 text-popover-foreground shadow-md animate-in fade-in-0 zoom-in-95",
          placementStyles[placement]
        )}>
          <div onClick={handleItemClick} className="space-y-0.5">
            {children}
          </div>
        </div>
      )}
    </div>
  );
};

interface DropdownItemProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  children: ReactNode;
  onClick?: () => void;
  disabled?: boolean;
  divider?: boolean;
  className?: string;
}

export const DropdownItem: React.FC<DropdownItemProps> = ({
  children,
  onClick,
  disabled = false,
  divider = false,
  className = '',
  ...props
}) => {
  if (divider) {
    return <div className="h-px bg-border my-1" {...props} />;
  }

  return (
    <button
      className={cn(
        "relative flex w-full cursor-pointer select-none items-center rounded-sm px-2 py-1 text-xs outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground",
        disabled && "pointer-events-none opacity-50",
        className
      )}
      onClick={onClick}
      disabled={disabled}
      {...props}
    >
      {children}
    </button>
  );
};

interface DropdownHeaderProps {
  children: ReactNode;
  className?: string;
}

export const DropdownHeader: React.FC<DropdownHeaderProps> = ({ children, className = '' }) => {
  return (
    <div className={cn("px-2 py-1 text-xs font-semibold text-foreground", className)}>
      {children}
    </div>
  );
};

export default Dropdown;

