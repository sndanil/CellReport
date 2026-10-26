using Avalonia;
using Avalonia.Controls;
using CellReport.Avalonia;
using CellReport.Editor.Avalonia.ViewModels;
using System;
using System.ComponentModel;

namespace CellReport.Editor.Avalonia.Views;

public partial class DesignerView : UserControl
{
	private const double MinInspectorWidth = 220;
	private const double MaxInspectorWidth = 720;

	private MainWindowViewModel? _viewModel;
	private bool _applyingLayout;

	public DesignerView()
	{
		InitializeComponent();
		InspectorColumn.PropertyChanged += OnInspectorColumnChanged;
		DataContextChanged += OnDataContextChanged;
	}

	public PresentControl Surface => ReportSurface;

	private ColumnDefinition InspectorColumn => DesignerLayout.ColumnDefinitions[0];

	private void OnDataContextChanged(object? sender, EventArgs e)
	{
		if (_viewModel != null)
			_viewModel.PropertyChanged -= OnViewModelPropertyChanged;

		_viewModel = DataContext as MainWindowViewModel;
		if (_viewModel != null)
			_viewModel.PropertyChanged += OnViewModelPropertyChanged;

		ApplyLayout();
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(MainWindowViewModel.InspectorWidth) or nameof(MainWindowViewModel.IsInspectorCollapsed))
			ApplyLayout();
	}

	private void ApplyLayout()
	{
		if (_viewModel == null || _applyingLayout)
			return;

		_applyingLayout = true;
		try
		{
			var collapsed = _viewModel.IsInspectorCollapsed;
			var width = collapsed ? 0 : Math.Clamp(_viewModel.InspectorWidth, MinInspectorWidth, MaxInspectorWidth);
			InspectorColumn.MinWidth = collapsed ? 0 : MinInspectorWidth;
			InspectorColumn.Width = new GridLength(width);
		}
		finally
		{
			_applyingLayout = false;
		}
	}

	private void OnInspectorColumnChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (_applyingLayout || _viewModel == null || e.Property != ColumnDefinition.WidthProperty)
			return;

		if (_viewModel.IsInspectorCollapsed)
			return;

		var width = InspectorColumn.Width;
		if (width.GridUnitType != GridUnitType.Pixel)
			return;

		_viewModel.InspectorWidth = Math.Clamp(width.Value, MinInspectorWidth, MaxInspectorWidth);
	}
}
