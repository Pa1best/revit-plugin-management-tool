# RevitPluginsManager

Desktop utility for **Windows** to list Autodesk Revit add-in manifests (`.addin`) from your user profile and **enable or disable** them by renaming files (`*.addin` ↔ `*.addin.disabled`). The UI is built with **WPF** and **[WPF-UI](https://github.com/lepoco/wpfui)** (Fluent Design).

**Not affiliated with Autodesk.** Revit is a trademark of Autodesk, Inc.

## Requirements

- Windows 10 or later (Windows 11 recommended for full window backdrop effects)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## How it works

- Reads manifests under `%AppData%\Autodesk\Revit\Addins\<year>\` (one folder per Revit version, e.g. `2025`).
- **Enabled:** `*.addin`
- **Disabled:** `*.addin.disabled` (Revit does not load these)
- Restart Revit after changes so manifests are picked up.

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
