using System.Text.Json;

namespace L0N.Core;

public static class CatalogParser
{
    private static readonly IReadOnlyDictionary<string, string> Sites =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SteamRIP"] = "steamrip.com",
            ["FitGirl"] = "fitgirl-repacks.site",
            ["DODI"] = "dodi-repacks.site"
        };

    public static IReadOnlyList<GameEntry> ParseWebsiteMetadata(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new FormatException("Missing website catalogue items array.");
        var results = new List<GameEntry>();
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var title = GetString(item, "title");
            var site = GetString(item, "site");
            var pageUrl = GetString(item, "pageUrl");
            if (string.IsNullOrWhiteSpace(title) || !Sites.TryGetValue(site, out var allowedHost) ||
                !Uri.TryCreate(pageUrl, UriKind.Absolute, out var url) ||
                url.Scheme != Uri.UriSchemeHttps || url.Host != allowedHost) continue;
            var published = GetString(item, "published");
            var year = DateOnly.TryParse(published, out var parsed) ? parsed.Year : (int?)null;
            results.Add(new GameEntry("web:" + site.ToLowerInvariant() + ":" + url.AbsolutePath,
                title.Trim(), site, year, "Website post · metadata only", url.AbsoluteUri,
                DownloadUrl: null, Published: published));
        }
        return results;
    }

    public static IReadOnlyList<GameEntry> ParseOfficialDownloads(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("downloads", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new FormatException("Missing official downloads array.");
        var result = new List<GameEntry>();
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var title = GetString(item, "title");
            if (string.IsNullOrWhiteSpace(title) || !item.TryGetProperty("uris", out var uris) || uris.ValueKind != JsonValueKind.Array) continue;
            var link = uris.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString()).FirstOrDefault(DownloadGuard.IsOfficialRelease);
            if (link is null) continue;
            result.Add(new GameEntry("official:" + title.ToLowerInvariant().Replace(' ', '-'),
                title.Trim(), "Official", null, "Open-source release", "https://github.com", link));
        }
        return result;
    }

    public static IReadOnlyList<GameEntry> SampleSteamGames() => new List<GameEntry>
    {
        new("steam:271590", "Grand Theft Auto V", "Steam sample", 2015, "Action, Adventure", "https://store.steampowered.com/app/271590/"),
        new("steam:1174180", "Red Dead Redemption 2", "Steam sample", 2019, "Action, Adventure", "https://store.steampowered.com/app/1174180/"),
        new("steam:1245620", "ELDEN RING", "Steam sample", 2022, "Action, RPG", "https://store.steampowered.com/app/1245620/"),
        new("steam:1888930", "The Last of Us Part I", "Steam sample", 2023, "Action, Adventure", "https://store.steampowered.com/app/1888930/"),
        new("steam:268910", "Cuphead", "Steam sample", 2017, "Action, Indie", "https://store.steampowered.com/app/268910/"),
        new("steam:1091500", "Cyberpunk 2077", "Steam sample", 2020, "Action, RPG", "https://store.steampowered.com/app/1091500/"),
        new("steam:322170", "Geometry Dash", "Steam sample", 2014, "Indie, Action", "https://store.steampowered.com/app/322170/"),
        new("steam:413150", "Stardew Valley", "Steam sample", 2016, "Indie, Simulation", "https://store.steampowered.com/app/413150/"),
        new("steam:367520", "Hollow Knight", "Steam sample", 2017, "Indie, Adventure", "https://store.steampowered.com/app/367520/"),
        new("steam:620", "Portal 2", "Steam sample", 2011, "Puzzle, Action", "https://store.steampowered.com/app/620/")
    };

    private static string GetString(JsonElement obj, string key) =>
        obj.TryGetProperty(key, out var x) && x.ValueKind == JsonValueKind.String ? x.GetString() ?? "" : "";
}
