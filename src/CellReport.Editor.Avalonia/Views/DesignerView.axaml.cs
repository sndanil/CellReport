using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CellReport.Avalonia;
using CellReport.Core;
using CellReport.Editor.Avalonia.ViewModels;
using CellReport.Presenters;
using System;
using System.ComponentModel;
using System.Linq;
using AvaloniaPoint = Avalonia.Point;

namespace CellReport.Editor.Avalonia.Views;

public partial class DesignerView : UserControl
{
	private const double MinInspectorWidth = 220;
	private const double MaxInspectorWidth = 720;

	private MainWindowViewModel? _viewModel;
	private BaseDisplayPresenter? _editingPresenter;
	private CellLocation _editingCell = CellLocation.None;
	private bool _applyingLayout;
	private bool _closingEditor;

	public DesignerView()
	{
		InitializeComponent();
		InspectorColumn.PropertyChanged += OnInspectorColumnChanged;
		DataContextChanged += OnDataContextChanged;

		InlineEditor.AddHandler(KeyDownEvent, OnInlineEditorKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
		InlineEditor.LostFocus += (_, _) => OnInlineEditorLostFocus();
		AddHandler(KeyDownEvent, OnDesignerKeyDown, RoutingStrategies.Bubble);
		ReportSurface.AddHandler(PointerPressedEvent, OnCanvasPointerPressed, RoutingStrategies.Tunnel);
		ReportSurface.PointerWheelChanged += (_, _) => CommitInlineEdit();
		ReportSurface.SizeChanged += (_, _) => CommitInlineEdit();
		DetachedFromVisualTree += (_, _) => CancelInlineEdit();
	}

	public PresentControl Surface => ReportSurface;

	private ColumnDefinition InspectorColumn => DesignerLayout.ColumnDefinitions[0];

	public void FitToWidth()
	{
		var presenter = ReportSurface.Presenter;
		if (presenter == null)
			return;

		var total = presenter.GetActualColumnsTotalWidth();
		var available = ReportSurface.Bounds.Width - 46; // заголовки строк и вертикальный скроллбар
		if (total <= 0 || available <= 0)
			return;

		presenter.Scale = Math.Clamp((float)(available / total), 0.25f, 5f);
		presenter.Update();
	}

	public void BeginInlineEdit()
	{
		var presenter = ReportSurface.Presenter;
		if (presenter == null)
			return;

		BeginInlineEdit(presenter.CurrentCell);
	}

	private void BeginInlineEdit(CellLocation location)
	{
		var presenter = ReportSurface.Presenter;
		if (_viewModel == null || presenter == null || !_viewModel.CanEditCellText(location))
			return;

		var cellControl = FindCellControl();
		if (cellControl == null)
			return;

		var offset = cellControl.TranslatePoint(new AvaloniaPoint(0, 0), ReportSurface);
		if (offset == null)
			return;

		var rect = presenter.GetCellRect(location);

		presenter.CurrentCell = location;
		_editingCell = location;
		SubscribePresenter(presenter);

		InlineEditor.Text = _viewModel.GetCellText(location);
		InlineEditor.Width = Math.Max(rect.Width, 48);
		InlineEditor.Height = Math.Max(rect.Height, 26);
		Canvas.SetLeft(InlineEditor, offset.Value.X + rect.X);
		Canvas.SetTop(InlineEditor, offset.Value.Y + rect.Y);
		InlineEditor.IsVisible = true;
		InlineEditor.Focus();
		InlineEditor.SelectAll();
	}

	private void CommitInlineEdit()
	{
		if (_viewModel == null || _editingCell.Equals(CellLocation.None))
			return;

		var location = _editingCell;
		var text = InlineEditor.Text ?? string.Empty;

		_closingEditor = true;
		try
		{
			HideInlineEditor();
		}
		finally
		{
			_closingEditor = false;
		}

		_viewModel.SetCellText(location, text);
		FindCellControl()?.Focus();
	}

	private void CancelInlineEdit()
	{
		if (_editingCell.Equals(CellLocation.None))
			return;

		_closingEditor = true;
		try
		{
			HideInlineEditor();
		}
		finally
		{
			_closingEditor = false;
		}

		FindCellControl()?.Focus();
	}

	private void HideInlineEditor()
	{
		InlineEditor.IsVisible = false;
		_editingCell = CellLocation.None;
		UnsubscribePresenter();
	}

	private void OnInlineEditorKeyDown(object? sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
			case Key.Escape:
				e.Handled = true;
				CancelInlineEdit();
				break;
			case Key.Enter when !e.KeyModifiers.HasFlag(KeyModifiers.Shift):
				e.Handled = true;
				CommitInlineEdit();
				break;
		}
	}

	private void OnInlineEditorLostFocus()
	{
		if (_closingEditor || _editingCell.Equals(CellLocation.None))
			return;

		CommitInlineEdit();
	}

	private void OnDesignerKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Source is TextBox)
			return;

		var presenter = ReportSurface.Presenter;
		if (presenter == null || _viewModel == null)
			return;

		if (e.Key == Key.F2 && _editingCell.Equals(CellLocation.None))
		{
			BeginInlineEdit(presenter.CurrentCell);
			e.Handled = true;
			return;
		}

		if (e.KeyModifiers != KeyModifiers.Control)
			return;

		switch (e.Key)
		{
			case Key.C:
				_viewModel.Inspector.CopyCommand.Execute(null);
				e.Handled = true;
				break;
			case Key.X:
				_viewModel.Inspector.CutCommand.Execute(null);
				e.Handled = true;
				break;
			case Key.V:
				_viewModel.Inspector.PasteCommand.Execute(null);
				e.Handled = true;
				break;
			case Key.Z:
				_viewModel.UndoCommand.Execute(null);
				e.Handled = true;
				break;
			case Key.Y:
				_viewModel.RedoCommand.Execute(null);
				e.Handled = true;
				break;
			case Key.B:
				_viewModel.ToggleBoldCommand.Execute(null);
				e.Handled = true;
				break;
			case Key.I:
				_viewModel.ToggleItalicCommand.Execute(null);
				e.Handled = true;
				break;
			case Key.U:
				_viewModel.ToggleUnderlineCommand.Execute(null);
				e.Handled = true;
				break;
		}
	}

	private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (e.ClickCount != 2 || e.Source is not CellControl cellControl)
			return;

		var presenter = ReportSurface.Presenter;
		if (presenter == null)
			return;

		var position = e.GetPosition(cellControl);
		var location = presenter.GetCellByDevicePoint((float)position.X, (float)position.Y, false);
		if (location.Row < 0 || location.Column < 0)
			return;

		BeginInlineEdit(location);
		e.Handled = true;
	}

	private CellControl? FindCellControl() => ReportSurface.GetVisualDescendants().OfType<CellControl>().FirstOrDefault();

	private void SubscribePresenter(BaseDisplayPresenter presenter)
	{
		UnsubscribePresenter();
		_editingPresenter = presenter;
		presenter.PropertyChanged += OnEditingPresenterPropertyChanged;
	}

	private void UnsubscribePresenter()
	{
		if (_editingPresenter == null)
			return;

		_editingPresenter.PropertyChanged -= OnEditingPresenterPropertyChanged;
		_editingPresenter = null;
	}

	private void OnEditingPresenterPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is "LeftColumn" or "TopRow" or "Scale")
			CommitInlineEdit();
	}

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
