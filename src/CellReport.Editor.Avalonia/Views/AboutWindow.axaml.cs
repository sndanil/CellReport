using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CellReport.Editor.Avalonia.Views;

public partial class AboutWindow : Window
{
	public AboutWindow()
	{
		InitializeComponent();

		var version = Assembly.GetExecutingAssembly().GetName().Version;
		VersionText.Text = "Версия " + (version == null ? "1.0" : version.ToString(3));
		RuntimeText.Text = RuntimeInformation.FrameworkDescription;

		var avaloniaVersion = typeof(global::Avalonia.Application).Assembly.GetName().Version;
		AvaloniaText.Text = "Avalonia " + (avaloniaVersion == null ? string.Empty : avaloniaVersion.ToString(3));
	}

	private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
