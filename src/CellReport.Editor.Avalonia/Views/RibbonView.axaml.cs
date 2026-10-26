using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CellReport.Editor.Avalonia.ViewModels;
using System.Collections.Specialized;

namespace CellReport.Editor.Avalonia.Views;

public partial class RibbonView : UserControl
{
	private MainWindowViewModel? _viewModel;

	public RibbonView()
	{
		InitializeComponent();
		DataContextChanged += (_, _) => AttachViewModel();
		TabsStrip.AddHandler(PointerPressedEvent, OnTabPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
	}

	public void ShowFileMenu()
	{
		FileButton.Flyout?.ShowAt(FileButton);
	}

	private void OnTabPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (e.ClickCount != 2)
			return;

		_viewModel?.ToggleRibbonCommand.Execute(null);
		e.Handled = true;
	}

	private void AttachViewModel()
	{
		if (_viewModel != null)
			_viewModel.RecentFiles.CollectionChanged -= OnRecentFilesChanged;

		_viewModel = DataContext as MainWindowViewModel;
		if (_viewModel != null)
			_viewModel.RecentFiles.CollectionChanged += OnRecentFilesChanged;

		RebuildRecentMenu();
	}

	private void OnRecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildRecentMenu();

	private void RebuildRecentMenu()
	{
		RecentMenu.Items.Clear();

		if (_viewModel == null || _viewModel.RecentFiles.Count == 0)
		{
			RecentMenu.IsEnabled = false;
			return;
		}

		RecentMenu.IsEnabled = true;
		foreach (var entry in _viewModel.RecentFiles)
		{
			var item = new MenuItem
			{
				Header = entry.Title,
				Command = _viewModel.OpenRecentCommand,
				CommandParameter = entry.Path
			};
			ToolTip.SetTip(item, entry.Path);
			RecentMenu.Items.Add(item);
		}
	}
}
