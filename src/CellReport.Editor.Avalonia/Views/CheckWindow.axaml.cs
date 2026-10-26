using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CellReport.Editor.Avalonia.ViewModels;

namespace CellReport.Editor.Avalonia.Views;

public partial class CheckWindow : Window
{
	public CheckWindow()
	{
		InitializeComponent();
	}

	public void Refresh()
	{
		if (DataContext is CheckViewModel viewModel)
			viewModel.RecheckCommand.Execute(null);
	}

	private void OnIssueDoubleTapped(object? sender, TappedEventArgs e)
	{
		if (DataContext is CheckViewModel viewModel)
			viewModel.Navigate(viewModel.SelectedIssue);
	}

	private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
