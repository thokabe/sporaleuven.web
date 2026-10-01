using System.Globalization;
using System.Text.Json.Serialization;

namespace Api;

public sealed record CalendarGameDto(
    DateTime DateTime,
    string TeamHome,
    string TeamAway,
    int? ScoreHome,
    int? ScoreAway)
{
    public DayOfWeek DayOfWeek => DateTime.DayOfWeek;
    public string DateTimeWithDayOfWeek => DateTime.ToString("ddd d MMM yyyy HH:mm", CultureInfo.GetCultureInfo("nl-BE"));
}

public sealed record CalendarErrorDto([property: JsonPropertyName("error")] string Error);
