using System.Globalization;

namespace Voxa.Services;

/// <summary>Dates as the club sees them: in the Netherlands' time zone, written in the mail's language.</summary>
public static class ClubTime
{
    private static readonly TimeZoneInfo s_zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), s_zone);

    public static DateTime ToUtc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), s_zone);

    /// <summary>"dinsdag 2 februari 2027" or "Tuesday 2 February 2027".</summary>
    public static string FormatDate(DateTime utc, CultureInfo culture) => ToLocal(utc).ToString("dddd d MMMM yyyy", culture);

    /// <summary>"dinsdag 2 februari 2027, 18:45" or "Tuesday 2 February 2027, 18:45".</summary>
    public static string FormatDateTime(DateTime utc, CultureInfo culture) => ToLocal(utc).ToString("dddd d MMMM yyyy, HH:mm", culture);
}
