using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

namespace Api;

public class RankingFunction
{
    private readonly IRankingRepository _rankingRepository;

    public RankingFunction(IRankingRepository rankingRepository)
    {
        _rankingRepository = rankingRepository;
    }

    [Function("ranking")]
    [OpenApiOperation("getRanking", "Ranking", Summary = "Get the overall ranking for a competition")]
    [OpenApiParameter("season", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Four-digit season, e.g. 2627.")]
    [OpenApiParameter("league", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "League: vlm or vriendschap.")]
    [OpenApiParameter("competition", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Alphanumeric competition code.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(RankingTeamDto[]), Description = "Competition ranking.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof(CalendarErrorDto), Description = "Invalid route parameters.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof(CalendarErrorDto), Description = "Ranking not found.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ranking/{season}/{league}/{competition}")] HttpRequestData req,
        string season,
        string league,
        string competition)
    {
        if (!CalendarFunction.IsValidCalendarRequest(season, league, competition))
        {
            return await CalendarFunction.CreateErrorResponseAsync(
                req,
                HttpStatusCode.BadRequest,
                "Season must contain four digits, league must be vlm or vriendschap, and competition must be an alphanumeric code.");
        }

        var ranking = await _rankingRepository.GetRankingAsync(season, league, competition);
        if (ranking is null)
        {
            return await CalendarFunction.CreateErrorResponseAsync(req, HttpStatusCode.NotFound, "Ranking not found.");
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(ranking);
        return response;
    }
}
