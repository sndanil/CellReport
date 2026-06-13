using Avalonia;
using System;

namespace CellReport.Avalonia.Viewer
{
	internal class Program
	{
		public static string[] StartupArgs { get; private set; } = Array.Empty<string>();

		[STAThread]
		public static void Main(string[] args)
		{
			StartupArgs = args ?? Array.Empty<string>();
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace();
	}
}
