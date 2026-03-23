import React from 'react';
import { Calendar } from 'lucide-react';
import { cn } from '@/lib/utils';

interface DatePickerProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type' | 'value' | 'onChange'> {
  label?: string;
  name?: string;
  value?: string;
  onChange?: (e: React.ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  min?: string;
  max?: string;
  className?: string;
}

const DatePicker: React.FC<DatePickerProps> = ({
  label,
  name,
  value,
  onChange,
  placeholder = 'DATE',
  error,
  required = false,
  disabled = false,
  min,
  max,
  className = '',
  ...props
}) => {
  const inputId = name || `date-${label?.toLowerCase().replace(/\s+/g, '-')}`;
  const inputRef = React.useRef<HTMLInputElement>(null);

  // Format date value for display (convert YYYY-MM-DD to DD/MM/YYYY)
  const formatDateForDisplay = (dateValue: string): string => {
    if (!dateValue) return '';
    const [year, month, day] = dateValue.split('-');
    if (year && month && day) {
      return `${day}/${month}/${year}`;
    }
    return dateValue;
  };

  // Convert DD/MM/YYYY to YYYY-MM-DD for input value
  const parseDateForInput = (dateStr: string): string => {
    if (!dateStr) return '';
    // If already in YYYY-MM-DD format, return as is
    if (/^\d{4}-\d{2}-\d{2}$/.test(dateStr)) {
      return dateStr;
    }
    // Try to parse DD/MM/YYYY
    const parts = dateStr.split('/');
    if (parts.length === 3) {
      const [day, month, year] = parts;
      return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
    }
    return dateStr;
  };

  // Handle click on calendar icon to open date picker
  const handleCalendarClick = () => {
    if (!disabled && inputRef.current) {
      inputRef.current.showPicker?.();
      inputRef.current.focus();
    }
  };

  return (
    <div className={cn("space-y-0.5", className)}>
      {label && (
        <label htmlFor={inputId} className="text-xs font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
          {label}
          {required && <span className="text-destructive ml-1">*</span>}
        </label>
      )}
      <div className="relative">
        <input
          ref={inputRef}
          id={inputId}
          name={name}
          type="date"
          value={value || ''}
          onChange={onChange}
          placeholder={placeholder}
          required={required}
          disabled={disabled}
          min={min}
          max={max}
          className={cn(
            "flex h-9 w-full rounded border border-input bg-background pl-2 pr-8 py-1 text-xs ring-offset-background",
            "file:border-0 file:bg-transparent file:text-xs file:font-medium",
            "placeholder:text-muted-foreground",
            "focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring",
            "disabled:cursor-not-allowed disabled:opacity-50",
            "text-foreground",
            "[&::-webkit-calendar-picker-indicator]:hidden", // Hide native calendar icon in Chrome/Safari
            "[&::-webkit-inner-spin-button]:hidden", // Hide native spinner
            "[&::-webkit-outer-spin-button]:hidden", // Hide native spinner
            error && "border-destructive focus-visible:ring-destructive"
          )}
          aria-invalid={!!error}
          aria-describedby={error ? `${inputId}-error` : undefined}
          {...props}
        />
        <Calendar
          className={cn(
            "absolute right-2 top-1/2 -translate-y-1/2 h-5 w-5 text-foreground pointer-events-none",
            disabled && "opacity-50"
          )}
          aria-hidden="true"
        />
        {/* Clickable overlay for calendar icon */}
        <button
          type="button"
          onClick={handleCalendarClick}
          disabled={disabled}
          className={cn(
            "absolute right-0 top-0 bottom-0 w-8 flex items-center justify-center",
            "cursor-pointer",
            disabled && "cursor-not-allowed"
          )}
          aria-label="Open date picker"
          tabIndex={-1}
        >
          <span className="sr-only">Open date picker</span>
        </button>
      </div>
      {error && (
        <div id={`${inputId}-error`} className="text-xs font-medium text-destructive" role="alert">
          {error}
        </div>
      )}
    </div>
  );
};

export default DatePicker;

