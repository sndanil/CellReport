# AGENTS.md

Cross-platform .NET reporting framework (WinForms / WPF / Avalonia). Templates are XML (`.tcr`) or ZIP packages (`.tcrp`); documents are filled via `ReportDocument.AddSection` / `SetParameter` / `JoinSection`.

## Layout

- `src/CellReport/` — the core (library). Targets `net8.0;net48`. Everything else references it. Entry point: `ReportDocument`.
- `src/CellReport.WinForms/` — **`net48` only**, packable.
- `src/CellReport.WPF/` — `net8.0-windows;net48`, **not packable**, references WinForms.
- `src/CellReport.Avalonia/` — `net8.0`, packable. Avalonia 12 (12.0.4), `CommunityToolkit.Mvvm`.
- `src/CellReport.Editor/`, `src/CellReport.Viewer/`, `src/CellReport.Avalonia.Viewer/`, `src/CellReport.Editor.Avalonia/` — apps (not packable). `CellReport.Editor.Avalonia` is the Avalonia editor (`net10.0`), references `CellReport.Avalonia`. Its shell is `MainWindow.axaml`; the UI pieces live in `Views/` (`RibbonView`, `DesignerView`, `DocumentSettingsView`, `PrintSettingsView`, `DisplaySettingsView`, `ExpressionsView`, `CodeView`, `StatusBarView`). The four settings views are hosted by the `SettingsWindow` dialog (`Ctrl+,`), not by top-level mode tabs. Layout preferences (inspector width/collapsed) persist to `%LOCALAPPDATA%\CellReport\editor-settings.json` (`Services/EditorSettings.cs`).
- `Tests/` — MSTest, references only the core.

## Commands

```bash
dotnet build CellReport.sln          # full solution (Windows; net48 needs .NET Framework targeting pack)
dotnet test Tests/Tests.csproj       # MSTest suite (fast, no services needed; ~58 tests)
./pack.ps1                           # packs NuGet -> artifacts/packages (Release by default)
./pack.ps1 -Configuration Release -Version 1.0.0   # version override (sets Assembly/FileVersion too)
```

- The `.sln` and README write the tests path as lowercase `tests\Tests.csproj`; the folder is `Tests/`. On case-sensitive systems pass the real path explicitly.
- CI (`.github/workflows/nuget-pack.yml`) only packs packages; it does **not** run tests. Run `dotnet test` yourself.
- Only 3 projects are packable: `CellReport`, `CellReport.WinForms`, `CellReport.Avalonia`. NuGet package ID is `KCellReport` (not `CellReport`).

## Gotchas

- **`src/CellReport` compiles with C# 7.3** — `CellReport.csproj` overrides the `Directory.Build.props` `LangVersion=latest`. Do not use modern C# (records, target-typed `new`, range operators, etc.) in the core.
- Only `CellReport.Avalonia` and the Avalonia apps enable `<Nullable>enable</Nullable>`.
- All rendering (Skia) lives in `CellReport.Presenters.Skia`. Exporters (Excel/PDF/HTML/Image) require a renderer instance; the core has no renderer factory wired up by default.
- Test exporters must pass a renderer — use `Tests/Helpers/TestExportHelper.Renderer` (`SkiaReportRenderer`). The global `BaseExporter.RendererFactory` is set once in `TestAssemblySetup` (`[AssemblyInitialize]`).
- Template XML tests build templates in-memory via `TestTemplateFactory` string constants (`Tests/Helpers/TestTemplateFactory.cs`).
- Core UI/localization strings route through `CellReport.Localization.LocalizationManager`.
- **Do not hand-write `InitializeComponent()` (`AvaloniaXamlLoader.Load(this)`) in Avalonia views that use `x:Name`.** Defining it suppresses the generated method, and the generated fields for `x:Name` stay `null` at runtime. Just call `InitializeComponent()` from the constructor (`MainWindow` and `Views/*` do this). Library views under `CellReport.Avalonia/Views` sidestep the issue by resolving names with `Find<T>(...)`.