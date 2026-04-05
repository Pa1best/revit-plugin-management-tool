# RevitPluginsManager

Desktop utility for **Windows** to list Autodesk Revit add-in manifests (`.addin`) from your roaming profile and **enable or disable** them by moving files between the per-version Addins folder and a **`.disabled\<year>\`** folder (filenames stay `*.addin`). The UI is built with **WPF** and **[WPF-UI](https://github.com/lepoco/wpfui)** (Fluent Design).

**Not affiliated with Autodesk.** Revit is a trademark of Autodesk, Inc.

## Requirements

- Windows 10 or later (Windows 11 recommended for full window backdrop effects)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## How it works

- Reads manifests under `%AppData%\Autodesk\Revit\Addins\<year>\` (enabled) and `%AppData%\Autodesk\Revit\Addins\.disabled\<year>\` (disabled). The app resolves `%AppData%` via the Windows API (`Environment.SpecialFolder.ApplicationData`), not a hard-coded user path.
- **Enabled:** `<year>\*.addin` (Revit’s normal search path)
- **Disabled:** `.disabled\<year>\*.addin` (outside Revit’s default search, so those manifests are not loaded)
- Restart Revit after changes if a session already loaded an add-in you disabled.

Older releases used `*.addin.disabled` next to enabled manifests. Those are no longer listed; rename each to `*.addin` and move it into `.disabled\<year>\` if you still need them as disabled.

## Build

```bash
dotnet build -c Release
```

## Run

```bash
dotnet run --project RevitPluginsManager/RevitPluginsManager.csproj -c Release
```

Or run `RevitPluginsManager.exe` from `RevitPluginsManager/bin/Release/net8.0-windows/`.

## Tests

```bash
dotnet test -c Release
```

## Third-party libraries

| Package | License |
|--------|---------|
| [WPF-UI](https://github.com/lepoco/wpfui) | MIT |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MIT |

## License

This project is licensed under the [MIT License](LICENSE).

Copyright (c) 2026 Alex Slutski
