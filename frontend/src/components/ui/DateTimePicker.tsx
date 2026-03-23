import React, { useMemo } from 'react';
import { cn } from '@/lib/utils';

interface DateTimePickerProps {
  label?: string;
  name?: string;
  value?: string; // ISO format: "2025-12-06T09:30"
  onChange?: (value: string) => void;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  min?: string;
  max?: string;
  className?: string;
  timeIncrement?: number; // Minutes between time slots (default: 30)
}

/**
 * DateTimePicker - Combined date and time picker with configurable time increments
 * 
 * Outputs ISO datetime string (e.g., "2025-12-06T09:30")
 * Time slots are in 30-minute increments by default
 */
const DateTimePicker: React.FC<DateTimePickerProps> = ({
  label,
  name,
  value,
  onChange,
  error,
  required = false,
  disabled = false,
  min,
  max,
  className = '',
  timeIncrement = 30,
}) => {
  const inputId = name || `datetime-${label?.toLowerCase().replace(/\s+/g, '-')}`;

  // Parse value into date and time parts
  const { dateValue, timeValue } = useMemo(() => {
    if (!value) return { dateValue: '', timeValue: '' };
    
    // Handle both "2025-12-06T09:30" and "2025-12-06T09:30:00" formats
    const [datePart, timePart] = value.split('T');
    const time = timePart ? timePart.substring(0, 5) : ''; // Get HH:mm
    
    return { dateValue: datePart || '', timeValue: time };
  }, [value]);

  // Generate time slots based on increment
  const timeSlots = useMemo(() => {
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

  const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newDate = e.target.value;
    const newValue = newDate && timeValue ? `${newDate}T${timeValue}` : newDate ? `${newDate}T09:00` : '';
    onChange?.(newValue);
  };

  const handleTimeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newTime = e.target.value;
    const newValue = dateValue && newTime ? `${dateValue}T${newTime}` : '';
    onChange?.(newValue);
  };

  const inputBaseClass = cn(
    'h-8 rounded border border-input bg-background px-2 text-xs',
    'focus:outline-none focus:ring-1 focus:ring-ring',
    'disabled:cursor-not-allowed disabled:opacity-50',
    error && 'border-destructive focus:ring-destructive'
  );

  return (
    <div className={cn('flex flex-col gap-1', className)}>
      {label && (
        <label 
          htmlFor={inputId} 
          className="text-[10px] font-medium uppercase tracking-wide text-muted-foreground"
        >
          {label}
          {required && <span className="text-destructive ml-0.5">*</span>}
        </label>
      )}
      
      <div className="flex gap-2">
        {/* Date Input */}
        <input
          id={inputId}
          name={name ? `${name}-date` : undefined}
          type="date"
          value={dateValue}
          onChange={handleDateChange}
          required={required}
          disabled={disabled}
          min={min?.split('T')[0]}
          max={max?.split('T')[0]}
          className={cn(inputBaseClass, 'flex-1 min-w-[120px]')}
          aria-invalid={!!error}
        />
        
        {/* Time Select */}
        <select
          name={name ? `${name}-time` : undefined}
          value={timeValue}
          onChange={handleTimeChange}
          disabled={disabled || !dateValue}
          className={cn(inputBaseClass, 'w-24')}
          aria-label="Select time"
        >
          <option value="">Time</option>
          {timeSlots.map((slot) => (
            <option key={slot.value} value={slot.value}>
              {slot.label}
            </option>
          ))}
        </select>
      </div>
      
      {error && (
        <span className="text-[10px] text-destructive">{error}</span>
      )}
    </div>
  );
};

export default DateTimePicker;

