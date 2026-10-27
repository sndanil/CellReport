using Avalonia.Controls;
using Avalonia.Interactivity;
using CellReport.Editor.Avalonia.ViewModels;

namespace CellReport.Editor.Avalonia.Views;

public partial class StyleEditorWindow : Window
{
	public StyleEditorWindow()
	{
		InitializeComponent();
	}

	private void OnApplyClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is StyleEditorViewModel viewModel && viewModel.TryApply())
			Close(true);
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}
