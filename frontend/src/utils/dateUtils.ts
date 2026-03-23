/**
 * Utility functions for date/time formatting
 * All dates from backend are in UTC - we convert to Malaysia time (GMT+8) for display
 */

/**
 * Convert UTC date string to Malaysia timezone (GMT+8) and format
 * @param utcDateString - ISO 8601 date string from backend (UTC)
 * @returns Formatted date string in Malaysia timezone
 */
export const formatLocalDateTime = (utcDateString: string | undefined | null): string => {
  if (!utcDateString) return '';
  
  // Ensure the date string is treated as UTC
  let utcString = utcDateString.trim();
  
  // If no timezone indicator, assume UTC
  if (!utcString.includes('Z') && !utcString.includes('+') && !utcString.includes('-', 10)) {
    utcString = utcString + 'Z';
  }
  
  // Parse as UTC date
  const utcDate = new Date(utcString);
  
  // Check if date is valid
  if (isNaN(utcDate.getTime())) {
    return utcDateString; // Return original if parsing fails
  }
  
  // Convert UTC to Malaysia timezone (GMT+8) and format
  return utcDate.toLocaleString('en-GB', { 
    timeZone: 'Asia/Kuala_Lumpur', // GMT+8
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: true
  });
};

/**
 * Format date only (no time) in Malaysia timezone
 */
export const formatLocalDate = (utcDateString: string | undefined | null): string => {
  if (!utcDateString) return '';
  
  let utcString = utcDateString.trim();
  if (!utcString.includes('Z') && !utcString.includes('+') && !utcString.includes('-', 10)) {
    utcString = utcString + 'Z';
  }
  
  const utcDate = new Date(utcString);
  if (isNaN(utcDate.getTime())) {
    return utcDateString;
  }
  
  return utcDate.toLocaleDateString('en-GB', { 
    timeZone: 'Asia/Kuala_Lumpur',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  });
};

/**
 * Format time only (no date) in Malaysia timezone
 */
export const formatLocalTime = (utcDateString: string | undefined | null): string => {
  if (!utcDateString) return '';
  
  let utcString = utcDateString.trim();
  if (!utcString.includes('Z') && !utcString.includes('+') && !utcString.includes('-', 10)) {
    utcString = utcString + 'Z';
  }
  
  const utcDate = new Date(utcString);
  if (isNaN(utcDate.getTime())) {
    return utcDateString;
  }
  
  return utcDate.toLocaleTimeString('en-GB', { 
    timeZone: 'Asia/Kuala_Lumpur',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: true
  });
};

