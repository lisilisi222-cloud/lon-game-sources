L0N Game Launcher – Build instructions for Windows developer

Prerequisites:
- Windows 10/11 x64
- Visual Studio 2022 or newer with .NET desktop development / .NET 8 SDK
- Inno Setup 6

1) Open L0N.GameLauncher.sln in Visual Studio.
2) Build solution Release configuration.
3) Run tests/L0N.Core.SmokeTests using dotnet run from a Developer Command Prompt.
4) Publish L0N.Launcher as Release, win-x64, self-contained, single-file to ./publish
5) Compile installer/L0N.iss with Inno Setup 6.
6) New SETUP will be at ./dist/L0N_Game_Launcher_Setup.exe.

The unsigned installer is not guaranteed to pass Smart App Control.
Do not disable security on a work machine. Obtain IT approval + code signing.
