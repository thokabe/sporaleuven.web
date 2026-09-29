using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api;

public sealed record CalendarWeek(
    [property: JsonPropertyName("week")] int Week,
    [property: JsonPropertyName("wedstrijden")] List<JsonElement> Wedstrijden);

public static class CalendarMapper
{
    public static List<CalendarGameDto> ToDtos(IEnumerable<CalendarWeek> weeks) =>
        weeks.SelectMany(w => w.Wedstrijden).Select(ToDto).ToList();

    public static CalendarGameDto ToDto(JsonElement raw)
    {
        var fields = Parse(raw);

        var dateTime = DateTime.ParseExact(
            $"{fields["datum"]} {fields["uur"]}", "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        int? scoreHome = null, scoreAway = null;
        if (fields.TryGetValue("uitslag", out var result))
        {
            var sets = result.Split('-');
            scoreHome = int.Parse(sets[0], CultureInfo.InvariantCulture);
            scoreAway = int.Parse(sets[1], CultureInfo.InvariantCulture);
        }

        return new CalendarGameDto(
            dateTime,
            fields["thuisPloegClub"],
            fields["bezoekersPloegClub"],
            scoreHome,
            scoreAway);
    }

    private static Dictionary<string, string> Parse(JsonElement raw) => raw.ValueKind switch
    {
        JsonValueKind.String => ParsePowerShellHashtable(raw.GetString() ?? string.Empty),
        JsonValueKind.Object => raw.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.ToString()),
        _ => throw new JsonException($"Unsupported calendar game value: {raw.ValueKind}.")
    };

    private static Dictionary<string, string> ParsePowerShellHashtable(string raw)
    {
        var fields = new Dictionary<string, string>();
        foreach (var pair in raw.TrimStart('@', '{').TrimEnd('}').Split("; "))
        {
            var separator = pair.IndexOf('=');
            if (separator > 0)
            {
                fields[pair[..separator]] = pair[(separator + 1)..];
            }
        }
        return fields;
    }
}
