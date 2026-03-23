import React from 'react';
import { cn } from '@/lib/utils';

interface TimePickerProps {
  label?: string;
  name?: string;
  value?: string; // HH:mm format (e.g., "09:30")
  onChange?: (e: React.ChangeEvent<HTMLSelectElement>) => void;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  className?: string;
  timeIncrement?: number; // Minutes between time slots (default: 30)
  placeholder?: string;
}

/**
 * TimePicker - Time selection dropdown with configurable time increments
 * 
 * Outputs time string in HH:mm format (e.g., "09:30")
 * Time slots are in 30-minute increments by default
 */
const TimePicker: React.FC<TimePickerProps> = ({
  label,
  name,
  value,
  onChange,
  error,
  required = false,
  disabled = false,
  className = '',
  timeIncrement = 30,
  placeholder = 'Time',
}) => {
  const inputId = name || `time-${label?.toLowerCase().replace(/\s+/g, '-')}`;

  // Generate time slots based on increment
  const timeSlots = React.useMemo(() => {
    const slots: { value: string; label: string }[] = [];
    const totalMinutesInDay = 24 * 60;
    
    for (let minutes = 0; minutes < totalMinutesInDay; minutes += timeIncrement) {
      const hours = Math.floor(minutes / 60);
      const mins = minutes % 60;
      const timeStr = `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}`;
      
      // Format label as 12-hour time
      const period = hours >= 12 ? 'PM' : 'AM';
      const displayHour = hours === 0 ? 12 : hours > 12 ? hours - 12 : hours;
      const label = `${displayHour}:${mins.toString().padStart(2, '0')} ${period}`;
      
      slots.push({ value: timeStr, label });
    }
    
    return slots;
  }, [timeIncrement]);

  return (
    <div className={cn("space-y-0.5", className)}>
      {label && (
        <label 
          htmlFor={inputId} 
          className="text-xs font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
        >
          {label}
          {required && <span className="text-destructive ml-1">*</span>}
        </label>
      )}
      <select
        id={inputId}
        name={name}
        value={value || ''}
        onChange={onChange}
        required={required}
        disabled={disabled}
        className={cn(
          "flex h-9 w-full rounded border border-input bg-background px-2 py-1 text-xs ring-offset-background focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50",
          error && "border-destructive focus-visible:ring-destructive"
        )}
        aria-invalid={!!error}
        aria-describedby={error ? `${inputId}-error` : undefined}
      >
        <option value="">{placeholder}</option>
        {timeSlots.map((slot) => (
          <option key={slot.value} value={slot.value}>
            {slot.label}
          </option>
        ))}
      </select>
      {error && (
        <div id={`${inputId}-error`} className="text-xs font-medium text-destructive" role="alert">
          {error}
        </div>
      )}
    </div>
  );
};

export default TimePicker;

