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
        await using var stream = File.OpenRead(CalendarPath);
        var weeks = await JsonSerializer.DeserializeAsync<List<CalendarWeek>>(stream) ?? [];

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(CalendarMapper.ToDtos(weeks));
        return response;
    }
}
