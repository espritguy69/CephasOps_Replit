/**
 * Date Helper Utilities
 * Centralized date parsing, formatting, and filtering functions
 * Used across the entire CephasOps frontend for consistent date handling
 * 
 * TIMEZONE HANDLING:
 * - All dates are stored in UTC in the database
 * - Display functions use company timezone settings
 * - Use setCompanyTimezone() to configure the timezone at app initialization
 */

// ============================================================================
// Company Timezone Configuration
// ============================================================================

// Default timezone (Malaysia GMT+8)
let companyTimezone = 'Asia/Kuala_Lumpur';
let companyLocale = 'en-MY';
let companyDateFormat = 'dd/MM/yyyy';
let companyTimeFormat = 'hh:mm a';

/**
 * Set the company timezone for all date formatting functions
 * Call this at app initialization with the company settings
 */
export function setCompanyTimezone(timezone: string): void {
  companyTimezone = timezone;
}

/**
 * Set the company locale for formatting
 */
export function setCompanyLocale(locale: string): void {
  companyLocale = locale;
}

/**
 * Set the company date format
 */
export function setCompanyDateFormat(format: string): void {
  companyDateFormat = format;
}

/**
 * Set the company time format
 */
export function setCompanyTimeFormat(format: string): void {
  companyTimeFormat = format;
}

/**
 * Get current company timezone
 */
export function getCompanyTimezone(): string {
  return companyTimezone;
}

/**
 * Get current company locale
 */
export function getCompanyLocale(): string {
  return companyLocale;
}

// ============================================================================
// Date Parsing Functions
// ============================================================================

/**
 * Parse appointment date from various formats
 * Handles ISO format, DD/MM/YYYY, MM/DD/YYYY, and other common formats
 * @param dateStr - The date string to parse
 * @returns Date object or null if parsing fails
 */
export function parseAppointmentDate(dateStr: string | null | undefined): Date | null {
  if (!dateStr) return null;
  
  // Try ISO format first (YYYY-MM-DD or full ISO string)
  let date = new Date(dateStr);
  if (!isNaN(date.getTime())) return date;
  
  // Try DD/MM/YYYY format (common in Malaysia)
  const ddmmyyyy = dateStr.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
  if (ddmmyyyy) {
    const [, day, month, year] = ddmmyyyy;
    date = new Date(parseInt(year), parseInt(month) - 1, parseInt(day));
    if (!isNaN(date.getTime())) return date;
  }
  
  // Try DD-MM-YYYY format
  const ddmmyyyyDash = dateStr.match(/^(\d{1,2})-(\d{1,2})-(\d{4})$/);
  if (ddmmyyyyDash) {
    const [, day, month, year] = ddmmyyyyDash;
    date = new Date(parseInt(year), parseInt(month) - 1, parseInt(day));
    if (!isNaN(date.getTime())) return date;
  }
  
  return null;
}

/**
 * Parse time string to 24-hour format number for comparison
 * @param timeStr - Time string (e.g., "9:00 AM", "14:30", "2:30 PM")
 * @returns Number representing time (e.g., 900, 1430)
 */
export function parseTimeTo24Hour(timeStr: string): number {
  const match = timeStr.match(/(\d{1,2}):(\d{2})\s*(AM|PM)?/i);
  if (!match) return 0;
  
  let hours = parseInt(match[1]);
  const minutes = parseInt(match[2]);
  const period = match[3]?.toUpperCase();
  
  if (period === 'PM' && hours !== 12) hours += 12;
  if (period === 'AM' && hours === 12) hours = 0;
  
  return hours * 100 + minutes;
}

// ============================================================================
// Date Formatting Functions
// ============================================================================

/**
 * Format date for display (e.g., "2 Dec 2024")
 * Uses company timezone settings for consistent display
 * @param dateStr - Date string or Date object
 * @param options - Intl.DateTimeFormatOptions
 * @returns Formatted date string
 */
export function formatDate(
  dateStr: string | Date | null | undefined,
  options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short', year: 'numeric' }
): string {
  if (!dateStr) return '-';
  
  const date = typeof dateStr === 'string' ? parseAppointmentDate(dateStr) : dateStr;
  if (!date) return '-';
  
  try {
    // Use company timezone for display
    return date.toLocaleDateString(companyLocale, {
      ...options,
      timeZone: companyTimezone
    });
  } catch {
    // Fallback if timezone is invalid
    return date.toLocaleDateString(companyLocale, options);
  }
}

/**
 * Format date and time for display with company timezone
 * @param dateStr - Date string or Date object (in UTC)
 * @returns Formatted date and time string in company timezone
 */
export function formatDateTimeInTimezone(
  dateStr: string | Date | null | undefined
): string {
  if (!dateStr) return '-';
  
  const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr;
  if (isNaN(date.getTime())) return '-';
  
  try {
    return date.toLocaleString(companyLocale, {
      timeZone: companyTimezone,
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: companyTimeFormat.includes('a') || companyTimeFormat.includes('A')
    });
  } catch {
    return date.toLocaleString(companyLocale);
  }
}

/**
 * Format time only with company timezone
 * @param dateStr - Date string or Date object (in UTC)
 * @returns Formatted time string in company timezone
 */
export function formatTimeInTimezone(
  dateStr: string | Date | null | undefined
): string {
  if (!dateStr) return '-';
  
  const date = typeof dateStr === 'string' ? new Date(dateStr) : dateStr;
  if (isNaN(date.getTime())) return '-';
  
  try {
    return date.toLocaleTimeString(companyLocale, {
      timeZone: companyTimezone,
      hour: 'numeric',
      minute: '2-digit',
      hour12: companyTimeFormat.includes('a') || companyTimeFormat.includes('A')
    });
  } catch {
    return date.toLocaleTimeString(companyLocale);
  }
}

/**
 * Format date for short display (e.g., "2 Dec")
 * @param dateStr - Date string or Date object
 * @returns Short formatted date string
 */
export function formatDateShort(dateStr: string | Date | null | undefined): string {
  return formatDate(dateStr, { day: 'numeric', month: 'short' });
}

/**
 * Format date for long display (e.g., "Monday, 2 December 2024")
 * @param dateStr - Date string or Date object
 * @returns Long formatted date string
 */
export function formatDateLong(dateStr: string | Date | null | undefined): string {
  return formatDate(dateStr, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
}

/**
 * Format date for input field (YYYY-MM-DD)
 * @param date - Date object
 * @returns ISO date string for input fields
 */
export function formatDateForInput(date: Date | null | undefined): string {
  if (!date) return '';
  return date.toISOString().split('T')[0];
}

/**
 * Normalize time format (e.g., "02:30 PM" -> "2:30 PM")
 * @param timeStr - Time string
 * @returns Normalized time string
 */
export function normalizeTimeFormat(timeStr: string | null | undefined): string {
  if (!timeStr) return '-';
  return timeStr.replace(/^0(\d)/, '$1');
}

/**
 * Format time for display
 * @param timeStr - Time string
 * @returns Formatted time string
 */
export function formatTime(timeStr: string | null | undefined): string {
  if (!timeStr) return '-';
  return normalizeTimeFormat(timeStr);
}

/**
 * Format date and time together
 * @param dateStr - Date string
 * @param timeStr - Time string
 * @returns Combined formatted string
 */
export function formatDateTime(dateStr: string | null | undefined, timeStr: string | null | undefined): string {
  const date = formatDate(dateStr);
  const time = formatTime(timeStr);
  
  if (date === '-' && time === '-') return '-';
  if (date === '-') return time;
  if (time === '-') return date;
  
  return `${date} ${time}`;
}

// ============================================================================
// Date Comparison Functions
// ============================================================================

/**
 * Check if two dates are the same day
 * @param date1 - First date
 * @param date2 - Second date
 * @returns True if same day
 */
export function isSameDay(date1: Date | null, date2: Date | null): boolean {
  if (!date1 || !date2) return false;
  return date1.toDateString() === date2.toDateString();
}

/**
 * Check if a date is today
 * @param date - Date to check
 * @returns True if today
 */
export function isToday(date: Date | string | null | undefined): boolean {
  if (!date) return false;
  const d = typeof date === 'string' ? parseAppointmentDate(date) : date;
  return d ? isSameDay(d, new Date()) : false;
}

/**
 * Check if a date is in the past
 * @param date - Date to check
 * @returns True if in the past
 */
export function isPastDate(date: Date | string | null | undefined): boolean {
  if (!date) return false;
  const d = typeof date === 'string' ? parseAppointmentDate(date) : date;
  if (!d) return false;
  
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return d.getTime() < today.getTime();
}

/**
 * Check if a date is in the future
 * @param date - Date to check
 * @returns True if in the future
 */
export function isFutureDate(date: Date | string | null | undefined): boolean {
  if (!date) return false;
  const d = typeof date === 'string' ? parseAppointmentDate(date) : date;
  if (!d) return false;
  
  const today = new Date();
  today.setHours(23, 59, 59, 999);
  return d.getTime() > today.getTime();
}

// ============================================================================
// Date Range Functions
// ============================================================================

/**
 * Get today's date as ISO string (YYYY-MM-DD)
 * @returns Today's date string
 */
export function getTodayString(): string {
  return new Date().toISOString().split('T')[0];
}

/**
 * Get yesterday's date as ISO string
 * @returns Yesterday's date string
 */
export function getYesterdayString(): string {
  const date = new Date();
  date.setDate(date.getDate() - 1);
  return date.toISOString().split('T')[0];
}

/**
 * Get tomorrow's date as ISO string
 * @returns Tomorrow's date string
 */
export function getTomorrowString(): string {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  return date.toISOString().split('T')[0];
}

/**
 * Get start and end of current week
 * @returns Object with start and end date strings
 */
export function getThisWeekRange(): { start: string; end: string } {
  const today = new Date();
  const dayOfWeek = today.getDay();
  
  const start = new Date(today);
  start.setDate(today.getDate() - dayOfWeek);
  
  const end = new Date(start);
  end.setDate(start.getDate() + 6);
  
  return {
    start: start.toISOString().split('T')[0],
    end: end.toISOString().split('T')[0],
  };
}

/**
 * Get last 7 days range
 * @returns Object with start and end date strings
 */
export function getLast7DaysRange(): { start: string; end: string } {
  const today = new Date();
  const start = new Date(today);
  start.setDate(today.getDate() - 6);
  
  return {
    start: start.toISOString().split('T')[0],
    end: today.toISOString().split('T')[0],
  };
}

/**
 * Get start and end of current month
 * @returns Object with start and end date strings
 */
export function getThisMonthRange(): { start: string; end: string } {
  const today = new Date();
  const start = new Date(today.getFullYear(), today.getMonth(), 1);
  const end = new Date(today.getFullYear(), today.getMonth() + 1, 0);
  
  return {
    start: start.toISOString().split('T')[0],
    end: end.toISOString().split('T')[0],
  };
}

/**
 * Get start and end of last month
 * @returns Object with start and end date strings
 */
export function getLastMonthRange(): { start: string; end: string } {
  const today = new Date();
  const start = new Date(today.getFullYear(), today.getMonth() - 1, 1);
  const end = new Date(today.getFullYear(), today.getMonth(), 0);
  
  return {
    start: start.toISOString().split('T')[0],
    end: end.toISOString().split('T')[0],
  };
}

/**
 * Get last 30 days range
 * @returns Object with start and end date strings
 */
export function getLast30DaysRange(): { start: string; end: string } {
  const today = new Date();
  const start = new Date(today);
  start.setDate(today.getDate() - 29);
  
  return {
    start: start.toISOString().split('T')[0],
    end: today.toISOString().split('T')[0],
  };
}

// ============================================================================
// Date Filtering Functions
// ============================================================================

/**
 * Check if a date falls within a range
 * @param date - Date to check
 * @param startDate - Start of range (inclusive)
 * @param endDate - End of range (inclusive)
 * @returns True if date is within range
 */
export function isDateInRange(
  date: Date | string | null | undefined,
  startDate: string | null | undefined,
  endDate: string | null | undefined
): boolean {
  if (!date) return false;
  
  const d = typeof date === 'string' ? parseAppointmentDate(date) : date;
  if (!d) return false;
  
  const dateTime = d.getTime();
  
  if (startDate) {
    const start = new Date(startDate);
    start.setHours(0, 0, 0, 0);
    if (dateTime < start.getTime()) return false;
  }
  
  if (endDate) {
    const end = new Date(endDate);
    end.setHours(23, 59, 59, 999);
    if (dateTime > end.getTime()) return false;
  }
  
  return true;
}

/**
 * Filter array by date field
 * @param items - Array of items to filter
 * @param dateField - Name of the date field
 * @param filterDate - Single date to filter by
 * @returns Filtered array
 */
export function filterByDate<T extends Record<string, any>>(
  items: T[],
  dateField: keyof T,
  filterDate: string | null | undefined
): T[] {
  if (!filterDate) return items;
  
  const targetDate = new Date(filterDate);
  
  return items.filter(item => {
    const itemDate = parseAppointmentDate(item[dateField] as string);
    return itemDate && isSameDay(itemDate, targetDate);
  });
}

/**
 * Filter array by date range
 * @param items - Array of items to filter
 * @param dateField - Name of the date field
 * @param startDate - Start of range
 * @param endDate - End of range
 * @returns Filtered array
 */
export function filterByDateRange<T extends Record<string, any>>(
  items: T[],
  dateField: keyof T,
  startDate: string | null | undefined,
  endDate: string | null | undefined
): T[] {
  if (!startDate && !endDate) return items;
  
  return items.filter(item => {
    return isDateInRange(item[dateField] as string, startDate, endDate);
  });
}

// ============================================================================
// Time Slot Generation
// ============================================================================

/**
 * Generate time slots for a given range
 * @param startHour - Starting hour (0-23)
 * @param endHour - Ending hour (0-23)
 * @param intervalMinutes - Interval in minutes (default 30)
 * @returns Array of time strings
 */
export function generateTimeSlots(
  startHour: number = 8,
  endHour: number = 18,
  intervalMinutes: number = 30
): string[] {
  const slots: string[] = [];
  
  for (let hour = startHour; hour <= endHour; hour++) {
    for (let minute = 0; minute < 60; minute += intervalMinutes) {
      if (hour === endHour && minute > 0) break;
      
      const h = hour % 12 || 12;
      const period = hour < 12 ? 'AM' : 'PM';
      const m = minute.toString().padStart(2, '0');
      
      slots.push(`${h}:${m} ${period}`);
    }
  }
  
  return slots;
}

/**
 * Format time slot for display
 * @param time - Time string
 * @returns Formatted time slot
 */
export function formatTimeSlot(time: string): string {
  return normalizeTimeFormat(time);
}

// ============================================================================
// Relative Time Functions
// ============================================================================

/**
 * Get relative time string (e.g., "2 days ago", "in 3 hours")
 * @param date - Date to compare
 * @returns Relative time string
 */
export function getRelativeTime(date: Date | string | null | undefined): string {
  if (!date) return '-';
  
  const d = typeof date === 'string' ? parseAppointmentDate(date) : date;
  if (!d) return '-';
  
  const now = new Date();
  const diffMs = d.getTime() - now.getTime();
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffMinutes = Math.floor(diffMs / (1000 * 60));
  
  if (Math.abs(diffDays) >= 1) {
    if (diffDays === -1) return 'Yesterday';
    if (diffDays === 1) return 'Tomorrow';
    if (diffDays < 0) return `${Math.abs(diffDays)} days ago`;
    return `in ${diffDays} days`;
  }
  
  if (Math.abs(diffHours) >= 1) {
    if (diffHours < 0) return `${Math.abs(diffHours)} hours ago`;
    return `in ${diffHours} hours`;
  }
  
  if (Math.abs(diffMinutes) >= 1) {
    if (diffMinutes < 0) return `${Math.abs(diffMinutes)} minutes ago`;
    return `in ${diffMinutes} minutes`;
  }
  
  return 'Just now';
}

