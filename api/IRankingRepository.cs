namespace Api;

public interface IRankingRepository
{
    Task<IReadOnlyList<RankingTeamDto>?> GetRankingAsync(string season, string league, string competition);
}
