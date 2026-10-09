# L0N Game Launcher — Native Windows WPF beta

Source under this folder is unpacked by the project's Windows GitHub Actions build from the source bundle.
This is a C# / WPF / XAML application, with SQLite, no browser UI and no localhost server.

## Build (Windows 10/11 / Visual Studio 2022+)

- Install .NET 8 SDK and .NET desktop workload.
- Open L0N.GameLauncher.sln and build or run `dotnet build L0N.Launcher/L0N.Launcher.csproj -c Release`.
- `dotnet run --project tests/L0N.Core.SmokeTests/L0N.Core.SmokeTests.csproj` exercises basic parser and URL safety logic.
- `dotnet publish L0N.Launcher/L0N.Launcher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish`
- Build `installer/L0N.iss` with Inno Setup 6 to produce dist/L0N_Game_Launcher_Setup.exe.

## Scope

Steam is represented by 10 example titles, not the full Steam catalog. GitHub JSON website feeds from SteamRIP/FitGirl/DODI supply only article metadata. The download engine supports only verified direct release URLs on github.com.

**Unsigned:** This beta is not digitally signed; Windows Smart App Control or organizational controls may block it. Do not disable security controls. Sign it with a trusted certificate and get IT approval before using on a managed work PC.
