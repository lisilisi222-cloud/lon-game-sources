using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using L0N.Core;

namespace L0N.Launcher;

public sealed class DownloadJob : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private string _status = "Queued";
    private double _progress;
    private string _speed = "—";
    private CancellationTokenSource? _tokenSource;

    public string Title { get; init; } = "";
    public string Destination { get; internal set; } = "";
    public string Status { get => _status; internal set { _status = value; Changed(); } }
    public double Progress { get => _progress; internal set { _progress = value; Changed(); Changed(nameof(ProgressLabel)); } }
    public string ProgressLabel => $"{Progress:0.0}%";
    public string Speed { get => _speed; internal set { _speed = value; Changed(); } }
    internal CancellationToken Token => _tokenSource?.Token ?? CancellationToken.None;
    internal void StartCancellationToken() => _tokenSource = new CancellationTokenSource();
    public void Cancel() => _tokenSource?.Cancel();
    private void Changed([CallerMemberName] string? prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
}

public sealed class DownloadManager
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(30) };
    public ObservableCollection<DownloadJob> Jobs { get; } = new();

    // Never downloads torrents/repack pages or starts executables.
    public async Task DownloadAsync(GameEntry game, string destinationFolder)
    {
        if (!DownloadGuard.IsOfficialRelease(game.DownloadUrl))
            throw new InvalidOperationException("This entry has no verified official release URL.");
        if (string.IsNullOrWhiteSpace(destinationFolder))
            throw new InvalidOperationException("Choose a download folder first.");
        Directory.CreateDirectory(destinationFolder);
        var uri = new Uri(game.DownloadUrl!);
        var fileName = DownloadGuard.SafeFileName(uri);
        var output = Path.Combine(destinationFolder, fileName);
        if (File.Exists(output)) throw new IOException("A file with this name already exists; it will not be overwritten.");
        var partial = output + ".part";
        if (File.Exists(partial)) throw new IOException("A partial download already exists with this name.");
        var job = new DownloadJob { Title = game.Title, Destination = output, Status = "Connecting" };
        job.StartCancellationToken();
        Jobs.Insert(0, job);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, job.Token);
            response.EnsureSuccessStatusCode();
            var length = response.Content.Headers.ContentLength;
            await using var stream = await response.Content.ReadAsStreamAsync(job.Token);
            long total = 0;
            var stopwatch = Stopwatch.StartNew();
            var sinceUpdate = Stopwatch.StartNew();
            job.Status = "Downloading";
            // Close the .part stream before File.Move; Windows forbids moving files with an open exclusive handle.
            await using (var outputStream = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true))
            {
                var buffer = new byte[128 * 1024];
                while (true)
                {
                    var received = await stream.ReadAsync(buffer, job.Token);
                    if (received == 0) break;
                    await outputStream.WriteAsync(buffer.AsMemory(0, received), job.Token);
                    total += received;
                    if (sinceUpdate.ElapsedMilliseconds < 130) continue;
                    if (length is > 0) job.Progress = Math.Clamp(total * 100.0 / length.Value, 0, 100);
                    job.Speed = $"{total / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.1) / (1024.0 * 1024.0):0.0} MiB/s";
                    sinceUpdate.Restart();
                }
                await outputStream.FlushAsync(job.Token);
            }
            if (length.HasValue && total != length.Value)
                throw new IOException("Download size does not match HTTP Content-Length.");
            File.Move(partial, output);
            job.Progress = 100;
            job.Status = "Completed (not installed)";
            job.Speed = "—";
        }
        catch (OperationCanceledException)
        {
            job.Status = "Canceled";
            TryDelete(partial);
        }
        catch (Exception ex)
        {
            job.Status = "Failed: " + ex.Message;
            TryDelete(partial);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { }
    }
}
