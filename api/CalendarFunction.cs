using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Api;

public class CalendarFunction
{
    [Function("calendar")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendar/{season}/{competition}")] HttpRequestData req,
        string season,
        string competition)
    {
        if (!TryGetCalendarPath(season, competition, out var calendarPath))
        {
            return await CreateErrorResponseAsync(
                req,
                HttpStatusCode.BadRequest,
                "Season must contain four digits and competition must be an alphanumeric code.");
        }

        if (!File.Exists(calendarPath))
        {
            return await CreateErrorResponseAsync(req, HttpStatusCode.NotFound, "Calendar not found.");
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(await LoadGamesAsync(calendarPath));
        return response;
    }

    [Function("calendarByTeam")]
    public async Task<HttpResponseData> RunByTeam(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendar/{season}/{competition}/{teamName}")] HttpRequestData req,
        string season,
        string competition,
        string teamName)
    {
        if (!TryGetCalendarPath(season, competition, out var calendarPath))
        {
            return await CreateErrorResponseAsync(
                req,
                HttpStatusCode.BadRequest,
                "Season must contain four digits and competition must be an alphanumeric code.");
        }

        if (!File.Exists(calendarPath))
        {
            return await CreateErrorResponseAsync(req, HttpStatusCode.NotFound, "Calendar not found.");
        }

        var games = (await LoadGamesAsync(calendarPath))
            .Where(g => g.TeamHome.Contains(teamName, StringComparison.OrdinalIgnoreCase)
                     || g.TeamAway.Contains(teamName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(games);
        return response;
    }

    private static bool TryGetCalendarPath(string season, string competition, out string calendarPath)
    {
        var isValidSeason = season.Length == 4 && season.All(char.IsAsciiDigit);
        var isValidCompetition = competition.Length > 0 && competition.All(char.IsAsciiLetterOrDigit);

        calendarPath = isValidSeason && isValidCompetition
            ? Path.Combine(AppContext.BaseDirectory, "data", "calendar", season, competition, "items.json")
            : string.Empty;

        return isValidSeason && isValidCompetition;
    }

    private static async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        string message)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }

    private static async Task<List<CalendarGameDto>> LoadGamesAsync(string calendarPath)
    {
        await using var stream = File.OpenRead(calendarPath);
        var weeks = await JsonSerializer.DeserializeAsync<List<CalendarWeek>>(stream) ?? [];
        return CalendarMapper.ToDtos(weeks);
    }
}
