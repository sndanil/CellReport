using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CellReport.Editor.Avalonia.Services;
using CellReport.Editor.Avalonia.ViewModels;

namespace CellReport.Editor.Avalonia;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);

#if DEBUG
		this.AttachDeveloperTools();
#endif
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			RequestedThemeVariant = ThemeVariantOf(EditorSettings.Load().Theme);

			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainWindowViewModel()
			};
			desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnMainWindowClose;
		}

		base.OnFrameworkInitializationCompleted();
	}

	public static ThemeVariant ThemeVariantOf(string? theme)
	{
		return theme switch
		{
			"Light" => ThemeVariant.Light,
			"Dark" => ThemeVariant.Dark,
			_ => ThemeVariant.Default
		};
	}
}
