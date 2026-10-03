using System.Text.Json;

namespace Api;

public sealed class FileRankingRepository : IRankingRepository
{
    public async Task<IReadOnlyList<RankingTeamDto>?> GetRankingAsync(string season, string league, string competition)
    {
        var rankingPath = Path.Combine(
            AppContext.BaseDirectory, "data", "ranking", season, league, competition, "items.json");
        if (!File.Exists(rankingPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(rankingPath);
        var data = await JsonSerializer.DeserializeAsync<RankingData>(stream);
        return data?.Ranking;
    }
}
