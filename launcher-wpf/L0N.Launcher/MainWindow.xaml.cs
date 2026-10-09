using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using L0N.Core;

namespace L0N.Launcher;

public partial class MainWindow : Window
{
    private const string RawCatalogUrl = "https://raw.githubusercontent.com/lisilisi222-cloud/lon-game-sources/main/";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };
    private readonly SqliteStore _store;
    private readonly DownloadManager _manager = new();
    private readonly ObservableCollection<GameEntry> _filtered = new();
    private readonly ObservableCollection<LibraryEntry> _saved = new();
    private readonly List<GameEntry> _allGames = new();
    private bool _loaded;
    private bool _syncing;

    public MainWindow()
    {
        InitializeComponent();
        _store = new SqliteStore();
        _allGames.AddRange(CatalogParser.SampleSteamGames());
        CatalogueList.ItemsSource = _filtered;
        LibraryList.ItemsSource = _saved;
        DownloadsList.ItemsSource = _manager.Jobs;
        DownloadFolderBox.Text = _store.GetSetting("downloadsFolder", Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "L0N"));
        SteamRipEnabled.IsChecked = _store.GetSetting("steamrip", "true") == "true";
        FitGirlEnabled.IsChecked = _store.GetSetting("fitgirl", "true") == "true";
        DodiEnabled.IsChecked = _store.GetSetting("dodi", "true") == "true";
        CompletionNotifyEnabled.IsChecked = _store.GetSetting("notifyCompleted", "true") == "true";
        StartPageChoice.SelectedIndex = _store.GetSetting("startPage", "Home") switch
        {
            "Catalogue" => 1, "Library" => 2, "Downloads" => 3, _ => 0
        };
        ReloadLibrary();
        _loaded = true;
        ApplyFilters();
        Navigate(_store.GetSetting("startPage", "Home"));
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await SyncCataloguesAsync();

    private void Navigate(string name)
    {
        HomePage.Visibility = name == "Home" ? Visibility.Visible : Visibility.Collapsed;
        CataloguePage.Visibility = name == "Catalogue" ? Visibility.Visible : Visibility.Collapsed;
        LibraryPage.Visibility = name == "Library" ? Visibility.Visible : Visibility.Collapsed;
        DownloadsPage.Visibility = name == "Downloads" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = name == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        TopPageName.Text = name;
        foreach (var (button, value) in new[]
        {
            (NavHome,"Home"), (NavCatalogue,"Catalogue"), (NavLibrary,"Library"),
            (NavDownloads,"Downloads"), (NavSettings,"Settings")
        })
        {
            button.Background = name == value
                ? new SolidColorBrush(Color.FromRgb(37, 43, 51))
                : Brushes.Transparent;
            button.BorderBrush = name == value
                ? new SolidColorBrush(Color.FromRgb(31, 133, 193))
                : Brushes.Transparent;
        }
        if (name == "Catalogue") ApplyFilters();
    }

    private void NavHome_Click(object sender, RoutedEventArgs e) => Navigate("Home");
    private void NavCatalogue_Click(object sender, RoutedEventArgs e) => Navigate("Catalogue");
    private void NavLibrary_Click(object sender, RoutedEventArgs e) => Navigate("Library");
    private void NavDownloads_Click(object sender, RoutedEventArgs e) => Navigate("Downloads");
    private void NavSettings_Click(object sender, RoutedEventArgs e) => Navigate("Settings");

    private string SelectedSource => (SourceFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
    private void ApplyFilters()
    {
        if (!_loaded) return;
        var query = SearchInput.Text.Trim();
        int min = int.TryParse(YearMin.Text, out var x) ? x : 2005;
        int max = int.TryParse(YearMax.Text, out var y) ? y : 2026;
        var visible = _allGames.Where(g =>
            (SelectedSource == "All" || g.Source == SelectedSource) &&
            (g.Year is null || g.Year.Value >= min && g.Year.Value <= max) &&
            (query.Length == 0 || g.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (g.Source != "SteamRIP" || SteamRipEnabled.IsChecked == true) &&
            (g.Source != "FitGirl" || FitGirlEnabled.IsChecked == true) &&
            (g.Source != "DODI" || DodiEnabled.IsChecked == true))
            .OrderBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        _filtered.Clear();
        foreach (var game in visible) _filtered.Add(game);
        CatalogCount.Text = $"{visible.Length} entries  ·  only Steam SAMPLE metadata, not the entire Steam catalogue";
        if (!visible.Contains(CatalogueList.SelectedItem as GameEntry))
            CatalogueList.SelectedIndex = -1;
        UpdateSelection();
    }

    private void SearchInput_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
    private void SourceFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(YearMin.Text, out var from) || !int.TryParse(YearMax.Text, out var to) ||
            from < 1970 || to > 2100 || from > to)
        {
            MessageBox.Show(this, "Use a valid year range (e.g., 2005 to 2026).", "L0N filters", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ApplyFilters();
    }
    private void CatalogueList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSelection();

    private void UpdateSelection()
    {
        if (!_loaded) return;
        var g = CatalogueList.SelectedItem as GameEntry;
        SelectedTitle.Text = g?.Title ?? "Select a game";
        SelectedInfo.Text = g is null ? "Select a title on the left to see its details." :
            $"Source: {g.Source}\nYear: {g.Year?.ToString() ?? "unknown"}\nCategory: {g.Genres}\n\n" +
            (g.HasOfficialDownload ? "This entry has a verifiable public GitHub release download URL." :
             "No direct download is available for this listing. This is catalogue information only.");
        SaveLibraryButton.IsEnabled = g is not null;
        DownloadButton.IsEnabled = g?.HasOfficialDownload == true;
        OpenPageButton.IsEnabled = g?.PageUrl is not null;
    }

    private async Task SyncCataloguesAsync()
    {
        if (_syncing) return;
        _syncing = true;
        FooterStatus.Text = "Syncing L0N public catalogue metadata…";
        SourceStatus.Text = "Loading sources…";
        // Keep the locally available Steam sample even if there is no connection.
        _allGames.RemoveAll(x => x.Source != "Steam sample");
        var counts = new List<string>();
        try
        {
            var sourceJson = await Http.GetStringAsync(RawCatalogUrl + "website_catalog.json");
            var items = CatalogParser.ParseWebsiteMetadata(sourceJson);
            _allGames.AddRange(items);
            counts.Add($"SteamRIP {items.Count(x => x.Source == "SteamRIP")}");
            counts.Add($"FitGirl {items.Count(x => x.Source == "FitGirl")}");
            counts.Add($"DODI {items.Count(x => x.Source == "DODI")}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or FormatException)
        {
            counts.Add("Website metadata unavailable (offline / invalid feed)");
        }
        try
        {
            var officialJson = await Http.GetStringAsync(RawCatalogUrl + "games.json");
            var official = CatalogParser.ParseOfficialDownloads(officialJson);
            _allGames.AddRange(official);
            counts.Add($"Official {official.Count}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or FormatException)
        {
            counts.Add("Official feed unavailable");
        }
        _syncing = false;
        ApplyFilters();
        SourceStatus.Text = string.Join("  ·  ", counts);
        FooterStatus.Text = "Catalogue: " + SourceStatus.Text;
    }

    private async void Sync_Click(object sender, RoutedEventArgs e) => await SyncCataloguesAsync();

    private void ReloadLibrary()
    {
        _saved.Clear();
        foreach (var item in _store.GetLibrary()) _saved.Add(item);
        HomeLibraryCount.Text = _saved.Count + " saved games";
    }

    private void SaveLibrary_Click(object sender, RoutedEventArgs e)
    {
        if (CatalogueList.SelectedItem is not GameEntry item) return;
        _store.SaveGame(item);
        ReloadLibrary();
        FooterStatus.Text = "Saved to local Library: " + item.Title;
    }

    private void RemoveLibrary_Click(object sender, RoutedEventArgs e)
    {
        if (LibraryList.SelectedItem is not LibraryEntry item) return;
        _store.RemoveGame(item.Id);
        ReloadLibrary();
        FooterStatus.Text = "Removed library bookmark: " + item.Title;
    }

    private static void OpenExternalHttps(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Only HTTPS URLs can be opened.");
        Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }

    private void OpenPage_Click(object sender, RoutedEventArgs e)
    {
        if (CatalogueList.SelectedItem is not GameEntry game || string.IsNullOrWhiteSpace(game.PageUrl)) return;
        try { OpenExternalHttps(game.PageUrl); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Cannot open website", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private async void DownloadSelected_Click(object sender, RoutedEventArgs e)
    {
        if (CatalogueList.SelectedItem is not GameEntry game || !game.HasOfficialDownload) return;
        var folder = DownloadFolderBox.Text.Trim();
        try
        {
            Navigate("Downloads");
            FooterStatus.Text = "Starting official download: " + game.Title;
            await _manager.DownloadAsync(game, folder);
            FooterStatus.Text = "Download finished or stopped: " + game.Title;
            if (CompletionNotifyEnabled.IsChecked == true &&
                _manager.Jobs.FirstOrDefault()?.Status == "Completed (not installed)")
                MessageBox.Show(this, "Official file downloaded to your selected folder. It was not executed or installed.",
                    "L0N download complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            FooterStatus.Text = "Download not started: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Download not started", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CancelDownload_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DownloadJob job }) job.Cancel();
    }

    private void SaveFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = DownloadFolderBox.Text.Trim();
        try
        {
            if (string.IsNullOrWhiteSpace(folder) || !Path.IsPathFullyQualified(folder))
                throw new ArgumentException("Enter a full local folder path.");
            Directory.CreateDirectory(folder);
            _store.SetSetting("downloadsFolder", folder);
            FooterStatus.Text = "Download folder saved.";
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            MessageBox.Show(this, ex.Message, "Invalid download folder", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveStartPage_Click(object sender, RoutedEventArgs e)
    {
        if (StartPageChoice.SelectedItem is ComboBoxItem item)
        {
            _store.SetSetting("startPage", item.Content.ToString() ?? "Home");
            FooterStatus.Text = "Start page saved.";
        }
    }

    private void SourceSetting_Click(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        _store.SetSetting("steamrip", SteamRipEnabled.IsChecked == true ? "true" : "false");
        _store.SetSetting("fitgirl", FitGirlEnabled.IsChecked == true ? "true" : "false");
        _store.SetSetting("dodi", DodiEnabled.IsChecked == true ? "true" : "false");
        ApplyFilters();
    }

    private void NotifySetting_Click(object sender, RoutedEventArgs e)
    {
        if (_loaded) _store.SetSetting("notifyCompleted", CompletionNotifyEnabled.IsChecked == true ? "true" : "false");
    }

    private void OpenDotNet_Click(object sender, RoutedEventArgs e)
    {
        try { OpenExternalHttps("https://dotnet.microsoft.com/en-us/download/dotnet/8.0"); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Cannot open website", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
