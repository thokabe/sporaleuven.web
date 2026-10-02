using System.Text.Json;

namespace Api;

public sealed class FileCalendarRepository : ICalendarRepository
{
    private static readonly HashSet<string> ExcludedTeamNames = new(StringComparer.OrdinalIgnoreCase) { "A", "B" };

    public async Task<IReadOnlyList<CalendarGameDto>?> GetCalendarAsync(string season, string league, string competition)
    {
        var calendarPath = Path.Combine(
            AppContext.BaseDirectory, "data", "calendar", season, league, competition, "items.json");
        if (!File.Exists(calendarPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(calendarPath);
        var weeks = await JsonSerializer.DeserializeAsync<List<CalendarWeek>>(stream) ?? [];
        return CalendarMapper.ToDtos(weeks)
            .Where(game => !ExcludedTeamNames.Contains(game.TeamHome) && !ExcludedTeamNames.Contains(game.TeamAway))
            .ToList();
    }
}
