import { useState, useEffect } from 'react';

/**
 * Returns a value that updates after the source value has been stable for `delayMs`.
 * Useful for debouncing search/filter inputs so API calls run after typing pauses.
 */
export function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedValue(value);
    }, delayMs);
    return () => window.clearTimeout(timer);
  }, [value, delayMs]);

  return debouncedValue;
}
