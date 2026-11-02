# AGENTS.md

Cross-platform .NET reporting framework (WinForms / WPF / Avalonia). Templates are XML (`.tcr`) or ZIP packages (`.tcrp`); documents are filled via `ReportDocument.AddSection` / `SetParameter` / `JoinSection`.

## Layout

- `src/CellReport/` — the core (library). Targets `net10.0;net48`. Everything else references it. Entry point: `ReportDocument`.
- `src/CellReport.WinForms/` — **`net48` only**, packable.
- `src/CellReport.WPF/` — `net10.0-windows;net48`, **not packable**, references WinForms.
- `src/CellReport.Avalonia/` — `net10.0`, packable. Avalonia 12 (12.0.4), `CommunityToolkit.Mvvm`.
- `src/CellReport.Editor/`, `src/CellReport.Viewer/`, `src/CellReport.Avalonia.Viewer/`, `src/CellReport.Editor.Avalonia/` — apps (not packable). `CellReport.Editor.Avalonia` is the Avalonia editor (`net10.0`), references `CellReport.Avalonia`. Its shell is `MainWindow.axaml`; the UI pieces live in `Views/` (`RibbonView`, `DesignerView`, `DocumentSettingsView`, `PrintSettingsView`, `DisplaySettingsView`, `ExpressionsView`, `CodeView`, `StatusBarView`). The four settings views are hosted by the `SettingsWindow` dialog (`Ctrl+,`), not by top-level mode tabs. Layout and window preferences (inspector width/collapsed, ribbon collapsed, theme, window size/state, recent files) persist to `%LOCALAPPDATA%\CellReport\editor-settings.json` (`Services/EditorSettings.cs`).
- `Tests/` — MSTest, references the core (and `CellReport.WinForms` for `net48` GDI-renderer tests).

## Commands

```bash
dotnet build CellReport.sln          # full solution (Windows; net48 needs .NET Framework targeting pack)
dotnet test Tests/Tests.csproj       # MSTest suite (fast, no services needed; 175+ tests)
./pack.ps1                           # packs NuGet -> artifacts/packages (Release by default)
./pack.ps1 -Configuration Release -Version 1.0.0   # version override (sets Assembly/FileVersion too)
```

- CI (`.github/workflows/nuget-pack.yml`) tests on Windows and Linux, builds the full solution on Windows, then packs packages.
- Only 3 projects are packable: `CellReport`, `CellReport.WinForms`, `CellReport.Avalonia`. NuGet package ID is `KCellReport` (not `CellReport`).

## Gotchas

- **`src/CellReport` compiles with C# 7.3** — `CellReport.csproj` overrides the `Directory.Build.props` `LangVersion=latest`. Do not use modern C# (records, target-typed `new`, range operators, etc.) in the core.
- Only `CellReport.Avalonia` and the Avalonia apps enable `<Nullable>enable</Nullable>`.
- All rendering (Skia) lives in `CellReport.Presenters.Skia`. Exporters (Excel/PDF/HTML/Image) require a renderer instance; the core has no renderer factory wired up by default.
- Test exporters must pass a renderer — use `Tests/Helpers/TestExportHelper.Renderer` (`SkiaReportRenderer`). The global `BaseExporter.RendererFactory` is set once in `TestAssemblySetup` (`[AssemblyInitialize]`).
- Template XML tests build templates in-memory via `TestTemplateFactory` string constants (`Tests/Helpers/TestTemplateFactory.cs`).
- Content overflow (text/images outside the cell) is a **third pass** in `BaseDisplayPresenter.RenderCellsRange`: grid/background, images, then content. Images are a separate layer (`ImagesRenderedSeparately`) drawn **before** text/borders, so text is always over images; `BaseRenderer.SupportsOverflow` gates overflow layout (`SkiaReportRenderer`, `GdiPlusRenderer`); `BasePresenter` queues text overflow per frame and `BeginRender` clears it. Frozen panes clip overflow at their boundaries, print clips per page/band. Text overflow is one line (`RichString.MaxLines = 1`), vertical overflow is images-only.
- Core UI/localization strings route through `CellReport.Localization.LocalizationManager`.
- **Do not hand-write `InitializeComponent()` (`AvaloniaXamlLoader.Load(this)`) in Avalonia views that use `x:Name`.** Defining it suppresses the generated method, and the generated fields for `x:Name` stay `null` at runtime. Just call `InitializeComponent()` from the constructor (`MainWindow` and `Views/*` do this). Library views under `CellReport.Avalonia/Views` sidestep the issue by resolving names with `Find<T>(...)`.