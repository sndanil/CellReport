using Avalonia.Controls;
using Avalonia.Interactivity;
using CellReport.Avalonia.Viewer.ViewModels;
using System;

namespace CellReport.Avalonia.Viewer
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			Opened += OnMainWindowOpened;
		}

		private async void OnMainWindowOpened(object sender, EventArgs e)
		{
			if (DataContext is not MainWindowViewModel viewModel)
				return;

			var loaded = await viewModel.InitializeAsync(Program.StartupArgs, this);
			if (!loaded)
				Close();
		}
	}
}
