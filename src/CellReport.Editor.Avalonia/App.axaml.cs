using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainWindowViewModel()
			};
			desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnMainWindowClose;
		}

		base.OnFrameworkInitializationCompleted();
	}
}
