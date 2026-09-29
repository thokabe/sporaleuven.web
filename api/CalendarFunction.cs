using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Api;

public class CalendarFunction
{
    private static readonly string CalendarPath =
        Path.Combine(AppContext.BaseDirectory, "data", "calendar_H2_2627.json");

    [Function("calendar")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendar")] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(await LoadGamesAsync());
        return response;
    }

    [Function("calendarByTeam")]
    public async Task<HttpResponseData> RunByTeam(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendar/{teamName}")] HttpRequestData req,
        string teamName)
    {
        var games = (await LoadGamesAsync())
            .Where(g => g.TeamHome.Contains(teamName, StringComparison.OrdinalIgnoreCase)
                     || g.TeamAway.Contains(teamName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(games);
        return response;
    }

    private static async Task<List<CalendarGameDto>> LoadGamesAsync()
    {
        await using var stream = File.OpenRead(CalendarPath);
        var weeks = await JsonSerializer.DeserializeAsync<List<CalendarWeek>>(stream) ?? [];
        return CalendarMapper.ToDtos(weeks);
    }
}
