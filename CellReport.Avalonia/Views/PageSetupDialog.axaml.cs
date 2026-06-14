using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CellReport.Avalonia.ViewModels;
using CellReport.Core;

namespace CellReport.Avalonia.Views
{
	public partial class PageSetupDialog : Window
	{
		private readonly PageSetupDialogViewModel _viewModel;
		private readonly PrintSettings _settings;

		public PageSetupDialog(PrintSettings settings)
		{
			_settings = settings;
			_viewModel = new PageSetupDialogViewModel(settings);
			InitializeComponent();
			DataContext = _viewModel;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void OnOkClick(object? sender, RoutedEventArgs e)
		{
			_viewModel.ApplyTo(_settings);
			Close(true);
		}

		private void OnCancelClick(object? sender, RoutedEventArgs e)
		{
			Close(false);
		}
	}
}
