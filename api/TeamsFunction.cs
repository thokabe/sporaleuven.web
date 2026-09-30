using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Api;

public class TeamsFunction
{
    [Function("teams")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "teams/{season}/{league}/{competition}")] HttpRequestData req,
        string season,
        string league,
        string competition)
    {
        if (!CalendarFunction.TryGetCalendarPath(season, league, competition, out var calendarPath))
        {
            return await CalendarFunction.CreateErrorResponseAsync(
                req,
                HttpStatusCode.BadRequest,
                "Season must contain four digits, league must be vlm or vriendschap, and competition must be an alphanumeric code.");
        }

        if (!File.Exists(calendarPath))
        {
            return await CalendarFunction.CreateErrorResponseAsync(req, HttpStatusCode.NotFound, "Calendar not found.");
        }

        var teams = (await CalendarFunction.LoadGamesAsync(calendarPath))
            .SelectMany(game => new[] { game.TeamHome, game.TeamAway })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(team => team, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(teams);
        return response;
    }
}