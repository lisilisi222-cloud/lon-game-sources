using L0N.Core;

var tests = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception("FAILED: " + name); Console.WriteLine("PASS " + name); tests++; }

Check(!DownloadGuard.IsOfficialRelease("https://dodi-repacks.site/example/"), "website post is not a download");
Check(!DownloadGuard.IsOfficialRelease("http://github.com/a/b/releases/download/v1/game.zip"), "HTTP release rejected");
Check(!DownloadGuard.IsOfficialRelease("https://github.com.evil.tld/a/releases/download/v1/a.zip"), "fake host rejected");
Check(!DownloadGuard.IsOfficialRelease("https://github.com/a/b/README.md"), "GitHub page rejected");
Check(DownloadGuard.IsOfficialRelease("https://github.com/SuperTux/supertux/releases/download/v1/supertux.zip"), "official GitHub release accepted");
Check(DownloadGuard.SafeFileName(new Uri("https://github.com/a/b/releases/download/v1/file.zip")) == "file.zip", "filename sanitized");
var sites = """{"items":[{"title":"Demo post","site":"DODI","pageUrl":"https://dodi-repacks.site/a/","published":"2026-10-09"},{"title":"Fake","site":"SteamRIP","pageUrl":"https://not-steamrip.example/a/","published":"2026-10-09"}]}""";
var parsed = CatalogParser.ParseWebsiteMetadata(sites);
Check(parsed.Count == 1 && parsed[0].Source == "DODI", "only canonical website posts accepted");
Check(parsed[0].DownloadUrl is null && parsed[0].Year == 2026, "website metadata has no download URL");
var official = """{"downloads":[{"title":"SuperTux","uris":["https://github.com/SuperTux/supertux/releases/download/v1/supertux.zip"]}]}""";
Check(CatalogParser.ParseOfficialDownloads(official).Single().HasOfficialDownload, "official catalogue parsed");
Check(CatalogParser.SampleSteamGames().Count >= 8, "Steam sample present");
Console.WriteLine($"Completed {tests} smoke checks.");
