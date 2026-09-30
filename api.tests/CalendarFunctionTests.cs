using System.Net;
using System.Text.Json;
using Api;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Api.Tests;

public class CalendarFunctionTests
{
    [Theory]
    [InlineData("api/calendar/2627/vlm/H2", "SPORA LEUVEN H I")]
    [InlineData("api/calendar/2627/vlm/D2A", "SPORA LEUVEN DAMES")]
    public async Task Run_ReturnsCalendarForApiPath(string apiPath, string expectedTeam)
    {
        var routeSegments = apiPath.Split('/');
        var request = CreateRequest();

        var response = await new CalendarFunction().Run(request, routeSegments[2], routeSegments[3], routeSegments[4]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var games = await JsonSerializer.DeserializeAsync<List<CalendarGameDto>>(
            response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(games);
        Assert.NotEmpty(games);
        Assert.Contains(games, game => game.TeamHome == expectedTeam || game.TeamAway == expectedTeam);
    }

    [Theory]
    [InlineData("H2", "AARSCHOT BVS H")]
    [InlineData("D2A", "SPORA LEUVEN DAMES")]
    public async Task Run_ExcludesPlaceholderTeamsWithoutMatchingSubstrings(string competition, string expectedTeam)
    {
        var response = await new CalendarFunction().Run(CreateRequest(), "2627", "vlm", competition);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var games = await JsonSerializer.DeserializeAsync<List<CalendarGameDto>>(
            response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(games);
        Assert.DoesNotContain(games, game =>
            game.TeamHome is "A" or "B" || game.TeamAway is "A" or "B");
        Assert.Contains(games, game => game.TeamHome == expectedTeam || game.TeamAway == expectedTeam);
    }

    [Fact]
    public async Task Run_RejectsUnknownLeague()
    {
        var response = await new CalendarFunction().Run(CreateRequest(), "2627", "unknown", "H2");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Run_ReturnsNotFoundForMissingVriendschapCalendar()
    {
        var response = await new CalendarFunction().Run(CreateRequest(), "2627", "vriendschap", "H2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Run_LoadsCalendarFromVriendschapSubfolder()
    {
        var competition = $"Test{Guid.NewGuid():N}";
        var source = Path.Combine(AppContext.BaseDirectory, "data", "calendar", "2627", "vlm", "H2", "items.json");
        var directory = Path.Combine(AppContext.BaseDirectory, "data", "calendar", "2627", "vriendschap", competition);
        Directory.CreateDirectory(directory);
        try
        {
            File.Copy(source, Path.Combine(directory, "items.json"));

            var response = await new CalendarFunction().Run(CreateRequest(), "2627", "vriendschap", competition);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunByTeam_FiltersCalendarInLeague()
    {
        var response = await new CalendarFunction().RunByTeam(CreateRequest(), "2627", "vlm", "H2", "SPORA LEUVEN");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var games = await JsonSerializer.DeserializeAsync<List<CalendarGameDto>>(
            response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(games);
        Assert.NotEmpty(games);
        Assert.All(games, game => Assert.True(game.TeamHome.Contains("SPORA LEUVEN") || game.TeamAway.Contains("SPORA LEUVEN")));
    }

    [Theory]
    [InlineData("H2")]
    [InlineData("D2A")]
    public async Task RunTeams_ReturnsUniqueSortedNamesFromCalendar(string competition)
    {
        var function = new CalendarFunction();
        var calendarResponse = await function.Run(CreateRequest(), "2627", "vlm", competition);
        calendarResponse.Body.Position = 0;
        var games = await JsonSerializer.DeserializeAsync<List<CalendarGameDto>>(
            calendarResponse.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(games);

        var response = await new TeamsFunction().Run(CreateRequest(), "2627", "vlm", competition);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var teams = await JsonSerializer.DeserializeAsync<string[]>(response.Body);
        Assert.NotNull(teams);
        Assert.DoesNotContain("A", teams);
        Assert.DoesNotContain("B", teams);
        Assert.Equal(
            games.SelectMany(game => new[] { game.TeamHome, game.TeamAway })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(team => team, StringComparer.OrdinalIgnoreCase),
            teams);
    }

    [Theory]
    [InlineData("2627", "unknown", "H2", HttpStatusCode.BadRequest)]
    [InlineData("2627", "vriendschap", "H2", HttpStatusCode.NotFound)]
    public async Task RunTeams_UsesCalendarValidation(string season, string league, string competition, HttpStatusCode expectedStatus)
    {
        var response = await new TeamsFunction().Run(CreateRequest(), season, league, competition);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    private static HttpRequestData CreateRequest()
    {
        var services = new ServiceCollection();
        services.AddOptions<WorkerOptions>()
            .Configure(options => options.Serializer = new JsonObjectSerializer());

        var context = new Mock<FunctionContext>();
        context.SetupProperty(functionContext => functionContext.InstanceServices, services.BuildServiceProvider());

        var response = new Mock<HttpResponseData>(context.Object);
        response.SetupProperty(httpResponse => httpResponse.StatusCode);
        response.SetupProperty(httpResponse => httpResponse.Headers, new HttpHeadersCollection());
        response.SetupProperty(httpResponse => httpResponse.Body, new MemoryStream());
        response.SetupGet(httpResponse => httpResponse.Cookies).Returns(Mock.Of<HttpCookies>());

        var request = new Mock<HttpRequestData>(context.Object);
        request.Setup(httpRequest => httpRequest.CreateResponse()).Returns(response.Object);
        return request.Object;
    }
}