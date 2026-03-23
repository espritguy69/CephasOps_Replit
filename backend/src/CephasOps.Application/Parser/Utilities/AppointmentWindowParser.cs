using System.Text.RegularExpressions;

namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Utility for parsing appointment window strings into TimeSpan pairs
/// </summary>
public static class AppointmentWindowParser
{
    /// <summary>
    /// Parse appointment window string to TimeSpan pair
    /// </summary>
    /// <remarks>
    /// Supported formats:
    /// - "09:00-11:00"
    /// - "11:00-13:00"
    /// - "9am-11am"
    /// - "2pm-4pm"
    /// - "09:00 - 11:00" (with spaces)
    /// </remarks>
    public static (TimeSpan From, TimeSpan To) ParseAppointmentWindow(string? windowString)
    {
        if (string.IsNullOrWhiteSpace(windowString))
        {
            throw new ArgumentException("Appointment window is required", nameof(windowString));
        }

        var normalized = windowString.Trim().ToLowerInvariant();

        // Try 24-hour format first: "09:00-11:00" or "09:00 - 11:00"
        var match24Hour = Regex.Match(normalized, @"(\d{1,2}):(\d{2})\s*-\s*(\d{1,2}):(\d{2})");
        if (match24Hour.Success)
        {
            var fromHour = int.Parse(match24Hour.Groups[1].Value);
            var fromMinute = int.Parse(match24Hour.Groups[2].Value);
            var toHour = int.Parse(match24Hour.Groups[3].Value);
            var toMinute = int.Parse(match24Hour.Groups[4].Value);

            return (new TimeSpan(fromHour, fromMinute, 0), new TimeSpan(toHour, toMinute, 0));
        }

        // Try 12-hour format: "9am-11am" or "2pm-4pm"
        var match12Hour = Regex.Match(normalized, @"(\d{1,2})\s*(am|pm)\s*-\s*(\d{1,2})\s*(am|pm)");
        if (match12Hour.Success)
        {
            var fromHour = ConvertTo24Hour(
                int.Parse(match12Hour.Groups[1].Value),
                match12Hour.Groups[2].Value);
            var toHour = ConvertTo24Hour(
                int.Parse(match12Hour.Groups[3].Value),
                match12Hour.Groups[4].Value);

            return (new TimeSpan(fromHour, 0, 0), new TimeSpan(toHour, 0, 0));
        }

        // Try simple hour format: "9-11" (assumes same AM/PM)
        var matchSimple = Regex.Match(normalized, @"^(\d{1,2})\s*-\s*(\d{1,2})$");
        if (matchSimple.Success)
        {
            var fromHour = int.Parse(matchSimple.Groups[1].Value);
            var toHour = int.Parse(matchSimple.Groups[2].Value);

            // Assume morning if from < 12 and to <= 12
            // Otherwise adjust for PM
            if (fromHour < 12 && toHour < fromHour)
            {
                toHour += 12; // e.g., "10-2" means 10am-2pm
            }

            return (new TimeSpan(fromHour, 0, 0), new TimeSpan(toHour, 0, 0));
        }

        throw new FormatException($"Invalid appointment window format: '{windowString}'. Expected formats: '09:00-11:00', '9am-11am', '2pm-4pm'");
    }

    /// <summary>
    /// Try to parse appointment window, returns false if invalid
    /// </summary>
    public static bool TryParseAppointmentWindow(string? windowString, out TimeSpan from, out TimeSpan to)
    {
        from = TimeSpan.Zero;
        to = TimeSpan.Zero;

        try
        {
            var result = ParseAppointmentWindow(windowString);
            from = result.From;
            to = result.To;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int ConvertTo24Hour(int hour, string amPm)
    {
        if (amPm == "am")
        {
            return hour == 12 ? 0 : hour;
        }
        else // pm
        {
            return hour == 12 ? 12 : hour + 12;
        }
    }
}

