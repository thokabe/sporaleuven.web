using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace Api;

public class CalendarFunction
{
    private static readonly HashSet<string> ExcludedTeamNames = new(StringComparer.OrdinalIgnoreCase) { "A", "B" };

    [Function("calendar")]
    [OpenApiOperation("getCalendar", "Calendar", Summary = "Get games for a competition")]
    [OpenApiParameter("season", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Four-digit season, e.g. 2627.")]
    [OpenApiParameter("league", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "League: vlm or vriendschap.")]
    [OpenApiParameter("competition", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Alphanumeric competition code.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CalendarGameDto[]), Description = "Calendar games.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(CalendarErrorDto), Description = "Invalid route parameters.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(CalendarErrorDto), Description = "Calendar not found.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendar/{season}/{league}/{competition}")] HttpRequestData req,
        string season,
        string league,
        string competition)
    {
        if (!TryGetCalendarPath(season, league, competition, out var calendarPath))
        {
            return await CreateErrorResponseAsync(
                req,
                HttpStatusCode.BadRequest,
                "Season must contain four digits, league must be vlm or vriendschap, and competition must be an alphanumeric code.");
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
    [OpenApiOperation("getCalendarByTeam", "Calendar", Summary = "Get games involving a team")]
    [OpenApiParameter("season", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Four-digit season, e.g. 2627.")]
    [OpenApiParameter("league", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "League: vlm or vriendschap.")]
    [OpenApiParameter("competition", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Alphanumeric competition code.")]
    [OpenApiParameter("teamName", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Case-insensitive substring of a home or away team name.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(CalendarGameDto[]), Description = "Games involving the team, or an empty array.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(CalendarErrorDto), Description = "Invalid route parameters.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(CalendarErrorDto), Description = "Calendar not found.")]
    public async Task<HttpResponseData> RunByTeam(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendar/{season}/{league}/{competition}/{teamName}")] HttpRequestData req,
        string season,
        string league,
        string competition,
        string teamName)
    {
        if (!TryGetCalendarPath(season, league, competition, out var calendarPath))
        {
            return await CreateErrorResponseAsync(
                req,
                HttpStatusCode.BadRequest,
                "Season must contain four digits, league must be vlm or vriendschap, and competition must be an alphanumeric code.");
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

    internal static bool TryGetCalendarPath(string season, string league, string competition, out string calendarPath)
    {
        var isValidSeason = season.Length == 4 && season.All(char.IsAsciiDigit);
        var isValidLeague = league is "vlm" or "vriendschap";
        var isValidCompetition = competition.Length > 0 && competition.All(char.IsAsciiLetterOrDigit);

        calendarPath = isValidSeason && isValidLeague && isValidCompetition
            ? Path.Combine(AppContext.BaseDirectory, "data", "calendar", season, league, competition, "items.json")
            : string.Empty;

        return isValidSeason && isValidLeague && isValidCompetition;
    }

    internal static async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        string message)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new CalendarErrorDto(message));
        return response;
    }

    internal static async Task<List<CalendarGameDto>> LoadGamesAsync(string calendarPath)
    {
        await using var stream = File.OpenRead(calendarPath);
        var weeks = await JsonSerializer.DeserializeAsync<List<CalendarWeek>>(stream) ?? [];
        return CalendarMapper.ToDtos(weeks)
            .Where(game => !ExcludedTeamNames.Contains(game.TeamHome) && !ExcludedTeamNames.Contains(game.TeamAway))
            .ToList();
    }
}
