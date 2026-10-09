namespace L0N.Core;

public static class DownloadGuard
{
    // Direct-download whitelist only. We never treat a webpage as an installer/archive.
    public static bool IsOfficialRelease(string? raw)
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var url)) return false;
        if (url.Scheme != Uri.UriSchemeHttps || url.Host != "github.com" ||
            !url.AbsolutePath.Contains("/releases/download/", StringComparison.OrdinalIgnoreCase)) return false;
        var extension = Path.GetExtension(url.AbsolutePath);
        return extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".msi", StringComparison.OrdinalIgnoreCase);
    }

    public static string SafeFileName(Uri url)
    {
        var raw = Path.GetFileName(Uri.UnescapeDataString(url.AbsolutePath));
        var safe = new string(raw.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "download.bin" : safe;
    }
}
