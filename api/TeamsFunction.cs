using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace Api;

public class TeamsFunction
{
    [Function("teams")]
    [OpenApiOperation("getTeams", "Teams", Summary = "Get distinct team names in a competition")]
    [OpenApiParameter("season", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Four-digit season, e.g. 2627.")]
    [OpenApiParameter("league", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "League: vlm or vriendschap.")]
    [OpenApiParameter("competition", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Alphanumeric competition code.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(string[]), Description = "Unique team names, sorted case-insensitively.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(CalendarErrorDto), Description = "Invalid route parameters.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(CalendarErrorDto), Description = "Calendar not found.")]
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