namespace L0N.Core;

public sealed record GameEntry(
    string Id,
    string Title,
    string Source,
    int? Year,
    string Genres,
    string? PageUrl = null,
    string? DownloadUrl = null,
    string? Published = null)
{
    public bool HasOfficialDownload => DownloadGuard.IsOfficialRelease(DownloadUrl);
    public string SubTitle => $"{Source}  ·  {(Year?.ToString() ?? "Year unknown")}  ·  {Genres}";
}
