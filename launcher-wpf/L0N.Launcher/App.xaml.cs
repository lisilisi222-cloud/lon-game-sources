using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace L0N.Launcher;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Windows CI calls --selftest before the installer is packaged.
        // Never opens a visible window or a browser in this mode.
        if (e.Args.Any(x => string.Equals(x, "--selftest", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var store = new SqliteStore();
                var result = L0N.Core.CatalogParser.SampleSteamGames();
                if (result.Count == 0 || string.IsNullOrWhiteSpace(store.GetSetting("selftest", "ok")))
                    throw new InvalidOperationException("L0N startup prerequisites failed.");
                Shutdown(0);
            }
            catch (Exception ex)
            {
                try
                {
                    Directory.CreateDirectory(SqliteStore.DataDirectory);
                    File.AppendAllText(Path.Combine(SqliteStore.DataDirectory, "startup-errors.log"),
                        DateTimeOffset.Now + " | SELFTEST | " + ex + Environment.NewLine);
                }
                catch { }
                Shutdown(1);
            }
            return;
        }
        DispatcherUnhandledException += OnUnhandledException;
        base.OnStartup(e);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(SqliteStore.DataDirectory);
            File.AppendAllText(Path.Combine(SqliteStore.DataDirectory, "startup-errors.log"),
                DateTimeOffset.Now + " | " + e.Exception + Environment.NewLine);
        }
        catch { /* Avoid a second error while reporting an existing exception. */ }
        MessageBox.Show("L0N encountered an error:\n" + e.Exception.Message +
                        "\n\nSee %LOCALAPPDATA%\\L0NLauncher\\startup-errors.log",
            "L0N error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
