using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CellReport.Avalonia.ViewModels;
using CellReport.Core;

namespace CellReport.Avalonia.Views
{
	public partial class PrintOptionsDialog : Window
	{
		private readonly PrintOptionsDialogViewModel _viewModel;

		public string? PrinterName => _viewModel.SelectedPrinter;

		public int FromPage => _viewModel.FromPage;

		public int ToPage => _viewModel.ToPage;

		public int Copies => _viewModel.Copies;

		public PrintOptionsDialog(int totalPages, PrintSettings printSettings, string? printerName = null)
		{
			_viewModel = new PrintOptionsDialogViewModel(totalPages, printSettings.Copies, printerName);
			InitializeComponent();
			DataContext = _viewModel;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void OnPrintClick(object? sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(_viewModel.SelectedPrinter))
				return;

			_viewModel.NormalizePageRange();
			Close(true);
		}

		private void OnCancelClick(object? sender, RoutedEventArgs e)
		{
			Close(false);
		}
	}
}
