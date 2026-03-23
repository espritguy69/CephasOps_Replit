using System.Globalization;
using System.Text.RegularExpressions;

namespace CephasOps.Application.Settings.Utilities;

/// <summary>
/// Utility class for converting between 12-hour and 24-hour time formats
/// Supports formats like: "8am", "8:00AM", "8:00 PM", "8:00PM", "08:00", "20:00"
/// </summary>
public static class TimeFormatConverter
{
    // Regex patterns for 12-hour format
    private static readonly Regex TwelveHourPattern1 = new Regex(@"^(\d{1,2}):?(\d{2})?\s*(AM|PM|am|pm)$", RegexOptions.IgnoreCase);
    private static readonly Regex TwelveHourPattern2 = new Regex(@"^(\d{1,2})\s*(AM|PM|am|pm)$", RegexOptions.IgnoreCase);
    
    // Regex pattern for 24-hour format
    private static readonly Regex TwentyFourHourPattern = new Regex(@"^(\d{1,2}):(\d{2})$");

    /// <summary>
    /// Converts a time string to 24-hour format (HH:mm)
    /// Accepts both 12-hour (8am, 8:00PM) and 24-hour (08:00, 20:00) formats
    /// </summary>
    /// <param name="timeString">Time string in any format</param>
    /// <returns>Time in 24-hour format (HH:mm) or null if invalid</returns>
    public static string? To24HourFormat(string? timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
        {
            return null;
        }

        var trimmed = timeString.Trim();

        // Check if already in 24-hour format
        if (TwentyFourHourPattern.IsMatch(trimmed))
        {
            // Validate and normalize (ensure 2-digit hours)
            var parts = trimmed.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var hour) && int.TryParse(parts[1], out var minute))
            {
                if (hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59)
                {
                    return $"{hour:D2}:{minute:D2}";
                }
            }
            return null; // Invalid 24-hour format
        }

        // Try 12-hour format patterns
        var match = TwelveHourPattern1.Match(trimmed);
        if (!match.Success)
        {
            match = TwelveHourPattern2.Match(trimmed);
        }

        if (match.Success)
        {
            var hourStr = match.Groups[1].Value;
            var minuteStr = match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value) 
                ? match.Groups[2].Value 
                : "00";
            var period = match.Groups[match.Groups.Count - 1].Value.ToUpper();

            if (int.TryParse(hourStr, out var hour) && int.TryParse(minuteStr, out var minute))
            {
                // Validate hour (1-12) and minute (0-59)
                if (hour < 1 || hour > 12 || minute < 0 || minute > 59)
                {
                    return null;
                }

                // Convert to 24-hour format
                if (period == "AM")
                {
                    if (hour == 12)
                    {
                        hour = 0; // 12:xx AM = 00:xx
                    }
                }
                else // PM
                {
                    if (hour != 12)
                    {
                        hour += 12; // 1-11 PM = 13-23
                    }
                    // 12:xx PM = 12:xx (no change)
                }

                return $"{hour:D2}:{minute:D2}";
            }
        }

        return null; // Invalid format
    }

    /// <summary>
    /// Converts a 24-hour format time (HH:mm) to 12-hour format (h:mm AM/PM)
    /// </summary>
    /// <param name="time24Hour">Time in 24-hour format (HH:mm)</param>
    /// <param name="includeMinutes">Whether to include minutes (default: true)</param>
    /// <returns>Time in 12-hour format (e.g., "8:00 AM" or "8 AM")</returns>
    public static string? To12HourFormat(string? time24Hour, bool includeMinutes = true)
    {
        if (string.IsNullOrWhiteSpace(time24Hour))
        {
            return null;
        }

        var trimmed = time24Hour.Trim();
        var match = TwentyFourHourPattern.Match(trimmed);

        if (!match.Success)
        {
            return null; // Invalid 24-hour format
        }

        if (int.TryParse(match.Groups[1].Value, out var hour) && 
            int.TryParse(match.Groups[2].Value, out var minute))
        {
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
            {
                return null;
            }

            var period = hour >= 12 ? "PM" : "AM";
            
            if (hour == 0)
            {
                hour = 12; // 00:xx = 12:xx AM
            }
            else if (hour > 12)
            {
                hour -= 12; // 13-23 = 1-11 PM
            }
            // 12:xx stays as 12:xx

            if (includeMinutes)
            {
                return $"{hour}:{minute:D2} {period}";
            }
            else
            {
                return $"{hour} {period}";
            }
        }

        return null;
    }

    /// <summary>
    /// Validates if a time string is in a valid format (12-hour or 24-hour)
    /// </summary>
    public static bool IsValidTimeFormat(string? timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
        {
            return false;
        }

        var converted = To24HourFormat(timeString);
        return converted != null;
    }

    /// <summary>
    /// Normalizes a time string to 24-hour format if valid, otherwise returns null
    /// </summary>
    public static string? NormalizeTime(string? timeString)
    {
        return To24HourFormat(timeString);
    }
}

