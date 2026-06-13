using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CellReport.Avalonia.Viewer.ViewModels;

namespace CellReport.Avalonia.Viewer
{
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
				var mainWindow = new MainWindow
				{
					DataContext = new MainWindowViewModel()
				};
				desktop.MainWindow = mainWindow;
				desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnMainWindowClose;
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}
