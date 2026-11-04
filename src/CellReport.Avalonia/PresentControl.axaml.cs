using CellReport.Avalonia.Editing;
using CellReport.Avalonia.Services;
using CellReport.Avalonia.ViewModels;
using CellReport.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace CellReport.Avalonia
{
	public partial class PresentControl : UserControl
	{
		public static readonly StyledProperty<ContentInversionMode> InvertContentModeProperty =
			AvaloniaProperty.Register<PresentControl, ContentInversionMode>(
				nameof(InvertContentMode),
				ContentInversionMode.Auto);
		private CellControl _cellControl;
		private ColumnResizerControl _columnsResizer;
		private RowResizerControl _rowsResizer;
		private ScrollBar _hScrollBar;
		private ScrollBar _vScrollBar;
		private global::Avalonia.Controls.Border _resizerCorner;
		private SplitButton _printSplitButton;
		private AvaloniaDisplayPresenter? _presenter;
		private CellManager? _watchedManager;
		private PresentControlViewModel? _viewModel;
		private bool _updatingScrollBars;
		private bool _invalidatePosted;
		private readonly List<MenuItem> _printerMenuItems = new();
		private readonly CellEditHistory _cellEditHistory = new();
		private CellInlineEditor? _inlineEditor;

		public PresentControl()
		{
			InitializeComponent();
			_cellControl = this.Find<CellControl>("cellControl");
			_columnsResizer = this.Find<ColumnResizerControl>("columnsResizer");
			_rowsResizer = this.Find<RowResizerControl>("rowsResizer");
			_hScrollBar = this.Find<ScrollBar>("hScrollBar");
			_vScrollBar = this.Find<ScrollBar>("vScrollBar");
			_resizerCorner = this.Find<global::Avalonia.Controls.Border>("resizerCorner");
			_printSplitButton = this.Find<SplitButton>("printSplitButton");

			var inlineEditor = this.Find<TextBox>("inlineEditor");
			if (inlineEditor != null)
				_inlineEditor = new CellInlineEditor(this, inlineEditor);

			ApplyContentInversion();

			_hScrollBar.PropertyChanged += OnHorizontalScrollBarChanged;
			_vScrollBar.PropertyChanged += OnVerticalScrollBarChanged;
			_cellControl.AddHandler(KeyDownEvent, OnCellControlKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
			if (_cellControl.ContextMenu != null)
				_cellControl.ContextMenu.Opening += OnCellContextMenuOpening;

			DataContextChanged += OnPresentControlDataContextChanged;
		}

		public event EventHandler<CellEditEventArgs>? CellEdited;

		public event EventHandler? CellEditStateChanged;

		public bool IsCellEditing => _inlineEditor?.IsActive == true;

		public string CellEditText
		{
			get => _inlineEditor?.EditorText ?? string.Empty;
			set
			{
				if (_inlineEditor != null)
					_inlineEditor.EditorText = value;
			}
		}

		public bool CanUndoCellEdit => _cellEditHistory.CanUndo;

		public bool CanRedoCellEdit => _cellEditHistory.CanRedo;

		public bool BeginCellEdit(CellLocation location, string? initialText = null)
		{
			return _inlineEditor != null && _inlineEditor.Begin(location, initialText);
		}

		public void CommitCellEdit()
		{
			_inlineEditor?.Commit();
		}

		public void CancelCellEdit()
		{
			_inlineEditor?.Cancel();
		}

		public bool UndoCellEdit()
		{
			if (_presenter == null || !_cellEditHistory.CanUndo)
				return false;

			var location = _cellEditHistory.Undo(_presenter.Manager);
			if (location.Equals(CellLocation.None))
				return false;

			_presenter.CurrentCell = location;
			_presenter.Update();
			CellEditStateChanged?.Invoke(this, EventArgs.Empty);
			return true;
		}

		public bool RedoCellEdit()
		{
			if (_presenter == null || !_cellEditHistory.CanRedo)
				return false;

			var location = _cellEditHistory.Redo(_presenter.Manager);
			if (location.Equals(CellLocation.None))
				return false;

			_presenter.CurrentCell = location;
			_presenter.Update();
			CellEditStateChanged?.Invoke(this, EventArgs.Empty);
			return true;
		}

		public void ClearCellEditHistory()
		{
			_cellEditHistory.Clear();
		}

		internal void OnCellEdited(CellEditEventArgs e)
		{
			_cellEditHistory.Push(e.Before, e.After);
			CellEdited?.Invoke(this, e);
			CellEditStateChanged?.Invoke(this, EventArgs.Empty);
		}

		internal void FocusCells()
		{
			_cellControl?.Focus();
		}

		private void OnCellControlKeyDown(object? sender, KeyEventArgs e)
		{
			if (e.KeyModifiers != KeyModifiers.Control || IsCellEditing)
				return;

			if (_presenter == null || _presenter.Manager.IsDesignMode)
				return;

			if (e.Key == Key.Z && UndoCellEdit())
				e.Handled = true;
			else if (e.Key == Key.Y && RedoCellEdit())
				e.Handled = true;
		}

		private void OnCellContextMenuOpening(object? sender, CancelEventArgs e)
		{
			var menu = _cellControl?.ContextMenu;
			if (menu == null)
				return;

			var canEdit = _presenter != null
				&& !_presenter.Manager.IsDesignMode
				&& _presenter.Manager.IsCellEditable(_presenter.CurrentCell);

			foreach (var item in menu.Items)
			{
				var menuItem = item as MenuItem;
				if (menuItem != null && Equals(menuItem.Tag, "editCell"))
					menuItem.IsEnabled = canEdit;
			}
		}

		private void EditCellMenuItem_Click(object? sender, RoutedEventArgs e)
		{
			if (_presenter == null || _presenter.Manager.IsDesignMode)
				return;

			BeginCellEdit(_presenter.CurrentCell);
		}

		public new AvaloniaDisplayPresenter? Presenter => _presenter;

		public ContentInversionMode InvertContentMode
		{
			get => GetValue(InvertContentModeProperty);
			set => SetValue(InvertContentModeProperty, value);
		}

		protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
		{
			base.OnPropertyChanged(change);
			if (change.Property == InvertContentModeProperty)
				ApplyContentInversion();
		}

		private void ApplyContentInversion()
		{
			if (_cellControl != null)
				_cellControl.InvertContentMode = InvertContentMode;
		}

		private void OnPresentControlDataContextChanged(object? sender, System.EventArgs e)
		{
			if (_viewModel != null)
			{
				_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
				_viewModel.Printers.CollectionChanged -= OnPrintersChanged;
			}

			_viewModel = DataContext as PresentControlViewModel;
			if (_viewModel == null)
				return;

			_viewModel.PropertyChanged += OnViewModelPropertyChanged;
			_viewModel.Printers.CollectionChanged += OnPrintersChanged;
			if (_viewModel.Manager != null)
				AttachManager(_viewModel.Manager);

			UpdateContextMenuDataContext();
			BuildPrintFlyout();
		}

		private void OnPrintersChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			BuildPrintFlyout();
		}

		private void UpdateContextMenuDataContext()
		{
			if (_cellControl.ContextMenu != null)
				_cellControl.ContextMenu.DataContext = _viewModel;
		}

		private void BuildPrintFlyout()
		{
			if (_viewModel == null)
				return;

			var flyout = _printSplitButton.Flyout as MenuFlyout;
			if (flyout == null)
				return;

			flyout.Items.Clear();
			_printerMenuItems.Clear();

			foreach (var printerName in _viewModel.Printers)
			{
				// Fluent MenuItem renders the check glyph only for ToggleType != None
				// (:checked:toggle / :checked:radio). WinForms ToolStripMenuItem.Checked
				// shows a check without extra setup.
				var item = new MenuItem
				{
					Header = printerName,
					Tag = printerName,
					ToggleType = MenuItemToggleType.CheckBox
				};
				item.Click += OnPrinterMenuClick;
				flyout.Items.Add(item);
				_printerMenuItems.Add(item);
			}

			if (_viewModel.Printers.Count > 0)
				flyout.Items.Add(new Separator());

			flyout.Items.Add(CreateActionMenuItem(_viewModel.CustomPrintLabel, _viewModel.CustomPrintCommand, MaterialIconKind.PrinterSettings));
			flyout.Items.Add(CreateActionMenuItem(_viewModel.PreviewLabel, _viewModel.PrintPreviewCommand, MaterialIconKind.FileEyeOutline));
			flyout.Items.Add(CreateActionMenuItem(_viewModel.PageSetupLabel, _viewModel.PageSetupCommand, MaterialIconKind.FileSettingsOutline));

			UpdatePrinterCheckmarks();
		}

		private static MenuItem CreateActionMenuItem(string header, System.Windows.Input.ICommand command, MaterialIconKind iconKind)
		{
			var icon = new MaterialIcon { Kind = iconKind };
			icon.Classes.Add("toolbar-icon");
			icon.Classes.Add("icon-muted");

			return new MenuItem
			{
				Header = header,
				Command = command,
				Icon = icon
			};
		}

		private void OnPrinterMenuClick(object? sender, RoutedEventArgs e)
		{
			if (sender is MenuItem menuItem && menuItem.Tag is string printerName && _viewModel != null)
			{
				_viewModel.SelectedPrinter = printerName;
				UpdatePrinterCheckmarks();
				_viewModel.PrintCommand.Execute(null);
			}
		}

		private void PrintSplitButton_Click(object? sender, RoutedEventArgs e)
		{
			// WinForms PresentControl: the print button starts printing with the selected
			// (or OS default) printer; the printer list is on the drop-down chevron only.
			_viewModel?.PrintCommand.Execute(null);
		}

		private void UpdatePrinterCheckmarks()
		{
			var selected = _viewModel?.SelectedPrinter;
			foreach (var item in _printerMenuItems)
			{
				item.IsChecked = item.Tag is string name && name == selected;
			}
		}

		private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PresentControlViewModel.Manager) && _viewModel?.Manager != null)
				AttachManager(_viewModel.Manager);
			else if (e.PropertyName is nameof(PresentControlViewModel.ShowResizers)
			         or nameof(PresentControlViewModel.ShowToolBar))
				UpdateResizerVisibility();

			if (e.PropertyName == nameof(PresentControlViewModel.SelectedPrinter))
                UpdatePrinterCheckmarks();
		}

		public void AttachManager(CellManager manager)
		{
			if (manager == null)
				return;

			_inlineEditor?.Cancel();
			_cellEditHistory.Clear();
			WatchManager(manager);

			var designMode = _viewModel?.DesignMode == true;
			manager.IsDesignMode = designMode;
			_cellControl.Presenter = null;
			_columnsResizer.Presenter = null;
			_rowsResizer.Presenter = null;
			_presenter = new AvaloniaDisplayPresenter(manager, this, _cellControl);
			_cellControl.Presenter = _presenter;
			_columnsResizer.Presenter = _presenter;
			_rowsResizer.Presenter = _presenter;
			if (designMode)
				_presenter.ShowToolBar = false;

			_viewModel?.AttachPresenter(_presenter, this);
			Update();
		}

		private void WatchManager(CellManager manager)
		{
			if (ReferenceEquals(_watchedManager, manager))
				return;

			if (_watchedManager != null)
				_watchedManager.PropertyChanged -= OnManagerSettingsChanged;

			_watchedManager = manager;
			_watchedManager.PropertyChanged += OnManagerSettingsChanged;
		}

		private void OnManagerSettingsChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is "ShowResizers" or "ShowDesignerResizers")
				UpdateResizerVisibility();
		}

		private void UpdateResizerVisibility()
		{
			var showResizers = ShouldShowResizers();
			_columnsResizer.IsVisible = showResizers;
			_rowsResizer.IsVisible = showResizers;
			_resizerCorner.IsVisible = showResizers;
		}

		private bool ShouldShowResizers()
		{
			if (_presenter == null)
				return false;

			var settings = _presenter.Manager.DisplaySettings;
			var designMode = _viewModel?.DesignMode == true || _presenter.Manager.IsDesignMode;
			return settings.ShowResizers || (settings.ShowDesignerResizers && designMode);
		}

		private void OnHorizontalScrollBarChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (_updatingScrollBars || _presenter == null || e.Property != ScrollBar.ValueProperty)
				return;

			_presenter.LeftColumn = (int)_hScrollBar.Value;
		}

		private void OnVerticalScrollBarChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (_updatingScrollBars || _presenter == null || e.Property != ScrollBar.ValueProperty)
				return;

			_presenter.TopRow = (int)_vScrollBar.Value;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		internal void Update()
		{
			if (_presenter == null)
				return;

			_viewModel?.SyncFromPresenter();
			UpdateResizerVisibility();

			_updatingScrollBars = true;
			try
			{
				_hScrollBar.Maximum = _presenter.Manager.ColumnsCount;
				_vScrollBar.Maximum = _presenter.Manager.RowsCount;
				_hScrollBar.Value = _presenter.LeftColumn;
				_vScrollBar.Value = _presenter.TopRow;
			}
			finally
			{
				_updatingScrollBars = false;
			}

			Invalidate();
		}

		internal void Invalidate()
		{
			InvalidateSurfaces();
			if (_invalidatePosted)
				return;

			_invalidatePosted = true;
			Dispatcher.UIThread.Post(() =>
			{
				_invalidatePosted = false;
				InvalidateSurfaces();
			}, DispatcherPriority.Render);
		}

		private void InvalidateSurfaces()
		{
			_cellControl?.InvalidateVisual();
			_columnsResizer?.InvalidateVisual();
			_rowsResizer?.InvalidateVisual();
		}
	}
}
