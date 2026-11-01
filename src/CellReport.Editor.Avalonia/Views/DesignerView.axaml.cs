using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CellReport.Avalonia;
using CellReport.Core;
using CellReport.Editor.Avalonia.ViewModels;
using CellReport.Presenters;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using AvaloniaPoint = Avalonia.Point;

namespace CellReport.Editor.Avalonia.Views;

public partial class DesignerView : UserControl
{
	private const double MinInspectorWidth = 220;
	private const double MaxInspectorWidth = 720;

	private MainWindowViewModel? _viewModel;
	private BaseDisplayPresenter? _editingPresenter;
	private CellLocation _editingCell = CellLocation.None;
	private ContextMenu? _canvasMenu;
	private bool _applyingLayout;
	private bool _closingEditor;

	public DesignerView()
	{
		InitializeComponent();
		InspectorColumn.PropertyChanged += OnInspectorColumnChanged;
		DataContextChanged += OnDataContextChanged;

		InlineEditor.AddHandler(KeyDownEvent, OnInlineEditorKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
		InlineEditor.LostFocus += (_, _) => OnInlineEditorLostFocus();
		FormulaNumberTextBox.AddHandler(TextInputEvent, OnFormulaNumberTextInput, RoutingStrategies.Tunnel, handledEventsToo: true);
		FormulaNumberTextBox.KeyDown += OnFormulaNumberKeyDown;
		FormulaNumberTextBox.LostFocus += (_, _) => _viewModel?.CommitNumberText();
		ApplyFormulaBarMode();
		AddHandler(KeyDownEvent, OnDesignerKeyDown, RoutingStrategies.Bubble);
		ReportSurface.AddHandler(PointerPressedEvent, OnCanvasPointerPressed, RoutingStrategies.Tunnel);
		ReportSurface.PointerWheelChanged += (_, _) => CommitInlineEdit();
		ReportSurface.SizeChanged += (_, _) => CommitInlineEdit();
		ReportSurface.AttachedToVisualTree += (_, _) => Dispatcher.UIThread.Post(RebuildCanvasMenu, DispatcherPriority.Loaded);
		DetachedFromVisualTree += (_, _) => CancelInlineEdit();
		AttachedToVisualTree += (_, _) => RebuildCanvasMenu();
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
		presenter.EditingCell = location;
		presenter.Update();
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
		if (_editingPresenter != null)
		{
			_editingPresenter.EditingCell = CellLocation.None;
			_editingPresenter.Update();
		}
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
		if (e.Source is not CellControl cellControl)
			return;

		var presenter = ReportSurface.Presenter;
		if (presenter == null)
			return;

		var properties = e.GetCurrentPoint(cellControl).Properties;
		var position = e.GetPosition(cellControl);
		var location = presenter.GetCellByDevicePoint((float)position.X, (float)position.Y, false);
		if (location.Row < 0 || location.Column < 0)
			return;

		// как в Excel: правый клик сначала выделяет ячейку под курсором
		if (properties.IsRightButtonPressed)
		{
			presenter.CurrentCell = location;
			if (!ReferenceEquals(cellControl.ContextMenu, _canvasMenu))
				AttachCanvasMenu(cellControl);
			return;
		}

		if (e.ClickCount != 2)
			return;

		BeginInlineEdit(location);
		e.Handled = true;
	}

	private void RebuildCanvasMenu()
	{
		var cellControl = FindCellControl();
		if (_viewModel == null || cellControl == null)
			return;

		AttachCanvasMenu(cellControl);
	}

	private void AttachCanvasMenu(CellControl cellControl)
	{
		if (_viewModel == null)
			return;

		_canvasMenu = BuildCanvasMenu(_viewModel);
		cellControl.ContextMenu = _canvasMenu;
	}

	private ContextMenu BuildCanvasMenu(MainWindowViewModel viewModel)
	{
		var menu = new ContextMenu();
		var editItem = Item("Редактировать текст", null, "F2", () => BeginInlineEdit());

		menu.Opening += (_, _) =>
		{
			var presenter = ReportSurface.Presenter;
			editItem.IsEnabled = presenter != null && viewModel.CanEditCellText(presenter.CurrentCell);
		};

		menu.Items.Add(editItem);
		menu.Items.Add(new Separator());
		menu.Items.Add(Item("Вырезать", viewModel.Inspector.CutCommand, "Ctrl+X"));
		menu.Items.Add(Item("Копировать", viewModel.Inspector.CopyCommand, "Ctrl+C"));
		menu.Items.Add(Item("Вставить", viewModel.Inspector.PasteCommand, "Ctrl+V"));
		menu.Items.Add(new Separator());
		menu.Items.Add(Submenu("Добавить содержимое",
			Item("Текст", viewModel.Inspector.AddTextCommand),
			Item("Число", viewModel.Inspector.AddDecimalCommand),
			Item("Дата и время", viewModel.Inspector.AddDateTimeCommand),
			Item("Штрих-код", viewModel.Inspector.AddBarCodeCommand),
			Item("Изображение", viewModel.Inspector.AddImageCommand),
			Item("Действие", viewModel.Inspector.AddActionCommand)));
		menu.Items.Add(Item("Очистить содержимое", viewModel.Inspector.DeleteContentCommand));
		menu.Items.Add(Item("Удалить ячейку", viewModel.Inspector.DeleteCellCommand));
		menu.Items.Add(new Separator());
		menu.Items.Add(Item("Вставить строку до", viewModel.Inspector.InsertRowBeforeCommand));
		menu.Items.Add(Item("Вставить строку после", viewModel.Inspector.InsertRowAfterCommand));
		menu.Items.Add(Item("Удалить строку", viewModel.Inspector.DeleteRowsCommand));
		menu.Items.Add(new Separator());
		menu.Items.Add(Item("Вставить столбец до", viewModel.Inspector.InsertColumnBeforeCommand));
		menu.Items.Add(Item("Вставить столбец после", viewModel.Inspector.InsertColumnAfterCommand));
		menu.Items.Add(Item("Удалить столбец", viewModel.Inspector.DeleteColumnsCommand));
		menu.Items.Add(new Separator());
		menu.Items.Add(Item("Объединить ячейки", viewModel.Inspector.MergeCellsCommand));
		menu.Items.Add(new Separator());
		menu.Items.Add(Item("Свойства ячейки", null, null, ShowCellProperties));
		menu.Items.Add(Item("Свойства документа…", viewModel.ShowSettingsCommand));
		menu.Items.Add(Item("Найти…", viewModel.FindCommand, "Ctrl+F"));
		menu.Items.Add(new Separator());
		menu.Items.Add(Submenu("Печать и экспорт",
			Item("Печать", viewModel.Present.PrintCommand),
			Item("Предварительный просмотр", viewModel.Present.PrintPreviewCommand),
			Item("Экспорт в Excel", viewModel.Present.ExportExcelCommand),
			Item("Экспорт в HTML", viewModel.Present.ExportHtmlCommand),
			Item("Экспорт в PDF", viewModel.Present.ExportPdfCommand),
			Item("Экспорт в изображение", viewModel.Present.ExportImageCommand)));
		menu.Items.Add(Item("Сохранить", viewModel.SaveCommand, "Ctrl+S"));

		return menu;
	}

	private static MenuItem Item(string header, ICommand? command, string? gesture = null, Action? click = null)
	{
		var item = new MenuItem { Header = header, Command = command };
		if (gesture != null)
			item.InputGesture = KeyGesture.Parse(gesture);
		if (command == null && click != null)
			item.Click += (_, _) => click();
		return item;
	}

	private static MenuItem Submenu(string header, params MenuItem[] items)
	{
		var item = new MenuItem { Header = header };
		foreach (var child in items)
			item.Items.Add(child);
		return item;
	}

	private void ShowCellProperties()
	{
		if (_viewModel != null)
			_viewModel.IsInspectorCollapsed = false;

		InspectorTabs.SelectedIndex = 0;
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
		RebuildCanvasMenu();
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(MainWindowViewModel.InspectorWidth) or nameof(MainWindowViewModel.IsInspectorCollapsed))
			ApplyLayout();
		else if (e.PropertyName == nameof(MainWindowViewModel.IsFormulaBarExpanded))
			ApplyFormulaBarMode();
	}

	private void ApplyFormulaBarMode()
	{
		var expanded = _viewModel?.IsFormulaBarExpanded == true;
		FormulaTextBox.TextWrapping = expanded ? TextWrapping.Wrap : TextWrapping.NoWrap;
		FormulaTextBox.AcceptsReturn = expanded;
		FormulaTextBox.VerticalContentAlignment = expanded ? VerticalAlignment.Top : VerticalAlignment.Center;
		FormulaTextBox.MinHeight = expanded ? 72 : 26;
		FormulaTextBox.MaxHeight = expanded ? 160 : 26;
	}

	private void OnFormulaNumberTextInput(object? sender, TextInputEventArgs e)
	{
		if (string.IsNullOrEmpty(e.Text))
			return;

		foreach (var symbol in e.Text)
		{
			if (char.IsDigit(symbol) || symbol == '-' || symbol == '+' || symbol == ',' || symbol == '.' || symbol == ' ')
				continue;

			e.Handled = true;
			return;
		}
	}

	private void OnFormulaNumberKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter)
			return;

		e.Handled = true;
		_viewModel?.CommitNumberText();
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
