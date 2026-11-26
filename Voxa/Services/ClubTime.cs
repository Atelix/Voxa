using System.Globalization;

namespace Voxa.Services;

/// <summary>Dates as the club sees them: in the Netherlands' time zone, written in Dutch.</summary>
public static class ClubTime
{
    private static readonly CultureInfo s_dutch = CultureInfo.GetCultureInfo("nl-NL");
    private static readonly TimeZoneInfo s_zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), s_zone);

    public static DateTime ToUtc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), s_zone);

    public static string FormatDate(DateTime utc) => ToLocal(utc).ToString("dddd d MMMM yyyy", s_dutch);

    public static string FormatDateTime(DateTime utc) => ToLocal(utc).ToString("dddd d MMMM yyyy, HH:mm", s_dutch);
}
