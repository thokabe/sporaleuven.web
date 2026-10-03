using System.Net;
using System.Text.Json;
using Api;
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Api.Tests;

public class RankingFunctionTests
{
    [Fact]
    public async Task Run_ReturnsRankingFromRepository()
    {
        IReadOnlyList<RankingTeamDto> expectedRanking =
        [
            new RankingTeamDto(1812, "AARSCHOT BVS H", 1, 1, 0, 0, 3, 1, 3, 0)
        ];
        var repository = new Mock<IRankingRepository>();
        repository.Setup(repo => repo.GetRankingAsync("2627", "vlm", "H2"))
            .ReturnsAsync(expectedRanking);

        var response = await new RankingFunction(repository.Object)
            .Run(CreateRequest(), "2627", "vlm", "H2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var ranking = await JsonSerializer.DeserializeAsync<List<RankingTeamDto>>(
            response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(expectedRanking, ranking);
        repository.Verify(repo => repo.GetRankingAsync("2627", "vlm", "H2"), Times.Once);
    }

    [Theory]
    [InlineData("H2", "AARSCHOT BVS H")]
    [InlineData("D2A", "BETEKOM VBC")]
    public async Task Run_ReturnsRankingFromDataFile(string competition, string expectedTopTeam)
    {
        var response = await new RankingFunction(new FileRankingRepository())
            .Run(CreateRequest(), "2627", "vlm", competition);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var ranking = await JsonSerializer.DeserializeAsync<List<RankingTeamDto>>(
            response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(ranking);
        Assert.NotEmpty(ranking);
        Assert.Equal(expectedTopTeam, ranking[0].ClubName);
    }

    [Fact]
    public async Task Run_ReturnsNotFoundForMissingRanking()
    {
        var response = await new RankingFunction(new FileRankingRepository())
            .Run(CreateRequest(), "2627", "vriendschap", "H2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Run_RejectsUnknownLeague()
    {
        var repository = new Mock<IRankingRepository>();

        var response = await new RankingFunction(repository.Object)
            .Run(CreateRequest(), "2627", "unknown", "H2");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        repository.Verify(repo => repo.GetRankingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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
