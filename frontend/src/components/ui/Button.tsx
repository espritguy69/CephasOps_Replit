import React, { ReactNode, ButtonHTMLAttributes } from 'react';
import { cn } from '@/lib/utils';

interface ButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'size'> {
  children: ReactNode;
  variant?: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link';
  size?: 'default' | 'sm' | 'lg' | 'icon';
  className?: string;
  disabled?: boolean;
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
  type?: 'button' | 'submit' | 'reset';
}

const Button: React.FC<ButtonProps> = ({ 
  children, 
  variant = 'default', 
  size = 'default',
  className = '',
  disabled = false,
  onClick,
  type = 'button',
  ...props 
}) => {
  const variants: Record<string, string> = {
    default: 'bg-primary text-primary-foreground hover:bg-primary/90 active:bg-primary/95 shadow-sm',
    destructive: 'bg-destructive text-destructive-foreground hover:bg-destructive/90 active:bg-destructive/95 shadow-sm',
    outline: 'border border-input bg-background hover:bg-accent hover:text-accent-foreground active:bg-accent/80',
    secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80 active:bg-secondary/70',
    ghost: 'hover:bg-accent hover:text-accent-foreground active:bg-accent/80',
    link: 'text-primary underline-offset-4 hover:underline',
  };

  const sizes: Record<string, string> = {
    default: 'h-9 px-3 py-1 text-xs',
    sm: 'h-8 px-2 py-1 text-xs',
    lg: 'h-10 px-4 py-1 text-sm',
    icon: 'h-8 w-8',
  };

  return (
    <button
      type={type}
      className={cn(
        'inline-flex items-center justify-center rounded-lg font-medium',
        'transition-fast focus-ring disabled:pointer-events-none disabled:opacity-50',
        variants[variant],
        sizes[size],
        className
      )}
      disabled={disabled}
      onClick={onClick}
      {...props}
    >
      {children}
    </button>
  );
};

export default Button;

