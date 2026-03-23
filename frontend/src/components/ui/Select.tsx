import React from 'react';
import { cn } from '@/lib/utils';

interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

type SelectOptionValue = string | SelectOption;

interface SelectProps extends Omit<React.SelectHTMLAttributes<HTMLSelectElement>, 'value' | 'onChange'> {
  label?: string;
  name?: string;
  value?: string;
  onChange?: (e: React.ChangeEvent<HTMLSelectElement>) => void;
  options?: SelectOptionValue[];
  placeholder?: string;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  className?: string;
}

const Select: React.FC<SelectProps> = ({
  label,
  name,
  value,
  onChange,
  options = [],
  placeholder,
  error,
  required = false,
  disabled = false,
  className = '',
  ...props
}) => {
  const selectId = name || `select-${label?.toLowerCase().replace(/\s+/g, '-')}`;

  return (
    <div className={cn("space-y-0.5 mb-3 md:mb-4", className)}>
      {label && (
        <label htmlFor={selectId} className="text-xs md:text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
          {label}
          {required && <span className="text-destructive ml-1">*</span>}
        </label>
      )}
      <select
        id={selectId}
        name={name}
        value={value || ''}
        onChange={onChange}
        required={required}
        disabled={disabled}
        className={cn(
          "flex min-h-[44px] h-9 w-full rounded-lg border border-input bg-background px-2.5 md:px-3 py-1.5 md:py-2 text-xs md:text-sm",
          "transition-fast focus-ring disabled:cursor-not-allowed disabled:opacity-50",
          "appearance-none",
          error && "border-destructive focus-visible:ring-destructive"
        )}
        aria-invalid={!!error}
        aria-describedby={error ? `${selectId}-error` : undefined}
        {...props}
      >
        {placeholder && (
          <option value="" disabled>
            {placeholder}
          </option>
        )}
        {options.map((option) => {
          if (typeof option === 'string') {
            return (
              <option key={option} value={option}>
                {option}
              </option>
            );
          }
          return (
            <option key={option.value} value={option.value} disabled={option.disabled}>
              {option.label}
            </option>
          );
        })}
      </select>
      {error && (
        <div id={`${selectId}-error`} className="text-xs font-medium text-destructive" role="alert">
          {error}
        </div>
      )}
    </div>
  );
};

export default Select;

