using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using CellReport.Editor.Avalonia.ViewModels;

namespace CellReport.Editor.Avalonia.Views;

public partial class StylesView : UserControl
{
	private StylesViewModel? _styles;

	public StylesView()
	{
		InitializeComponent();
		DataContextChanged += (_, _) => Attach();
	}

	private void Attach()
	{
		if (_styles != null)
			_styles.PropertyChanged -= OnStylesPropertyChanged;

		_styles = (DataContext as MainWindowViewModel)?.Styles;
		if (_styles != null)
			_styles.PropertyChanged += OnStylesPropertyChanged;

		ScrollToSelected();
	}

	private void OnStylesPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(StylesViewModel.SelectedStyle))
			Dispatcher.UIThread.Post(ScrollToSelected, DispatcherPriority.Background);
	}

	private void ScrollToSelected()
	{
		if (StylesList?.SelectedItem is { } item)
			StylesList.ScrollIntoView(item);
	}
}
