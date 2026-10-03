using System.Text.Json.Serialization;

namespace Api;

public sealed record RankingTeamDto(
    [property: JsonPropertyName("clubID")] int ClubId,
    [property: JsonPropertyName("clubNaam")] string ClubName,
    [property: JsonPropertyName("aw")] int MatchesPlayed,
    [property: JsonPropertyName("w3")] int Wins3,
    [property: JsonPropertyName("w2")] int Wins2,
    [property: JsonPropertyName("w1")] int Wins1,
    [property: JsonPropertyName("gs")] int SetsWon,
    [property: JsonPropertyName("vs")] int SetsLost,
    [property: JsonPropertyName("pu")] int Points,
    [property: JsonPropertyName("ff")] int Forfeits);

public sealed record RankingData(
    [property: JsonPropertyName("ranking")] List<RankingTeamDto> Ranking);
