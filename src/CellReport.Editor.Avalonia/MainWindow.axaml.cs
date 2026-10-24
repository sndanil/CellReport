using Avalonia.Controls;
using CellReport.Editor.Avalonia.ViewModels;
using System;

namespace CellReport.Editor.Avalonia;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		Opened += OnMainWindowOpened;
	}

	private async void OnMainWindowOpened(object? sender, EventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		viewModel.ExitRequested += (_, _) => Close();
		await viewModel.InitializeAsync(Program.StartupArgs, this);
	}
}
