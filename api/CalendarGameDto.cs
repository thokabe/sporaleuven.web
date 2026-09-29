namespace Api;

public sealed record CalendarGameDto(
    DateTime DateTime,
    string TeamHome,
    string TeamAway,
    int? ScoreHome,
    int? ScoreAway);
