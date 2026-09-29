using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        await using var stream = File.OpenRead(CalendarPath);
        var weeks = await JsonSerializer.DeserializeAsync<List<CalendarWeek>>(stream) ?? [];

        var games = weeks
            .SelectMany(w => w.Wedstrijden.Select(g => ParseGame(w.Week, g)))
            .ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(games);
        return response;
    }

    // Games are stored as PowerShell hashtable strings: "@{key=value; key=value}".
    private static Dictionary<string, object> ParseGame(int week, string raw)
    {
        var game = new Dictionary<string, object> { ["week"] = week };
        foreach (var pair in raw.TrimStart('@', '{').TrimEnd('}').Split("; "))
        {
            var separator = pair.IndexOf('=');
            if (separator > 0)
            {
                game[pair[..separator]] = pair[(separator + 1)..];
            }
        }
        return game;
    }

    private sealed record CalendarWeek(
        [property: JsonPropertyName("week")] int Week,
        [property: JsonPropertyName("wedstrijden")] List<string> Wedstrijden);
}
