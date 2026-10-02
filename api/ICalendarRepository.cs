namespace Api;

public interface ICalendarRepository
{
    Task<IReadOnlyList<CalendarGameDto>?> GetCalendarAsync(string season, string league, string competition);
}
