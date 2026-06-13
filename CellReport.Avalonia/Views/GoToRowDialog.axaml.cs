using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CellReport.Avalonia.ViewModels;

namespace CellReport.Avalonia.Views
{
	public partial class GoToRowDialog : Window
	{
		public GoToRowDialogViewModel ViewModel { get; }

		public GoToRowDialog(GoToRowDialogViewModel viewModel)
		{
			ViewModel = viewModel;
			InitializeComponent();
			DataContext = viewModel;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		private void OnOkClick(object? sender, RoutedEventArgs e)
		{
			ViewModel.ConfirmCommand.Execute(null);
			Close(true);
		}

		private void OnCancelClick(object? sender, RoutedEventArgs e)
		{
			ViewModel.CancelCommand.Execute(null);
			Close(false);
		}
	}
}
