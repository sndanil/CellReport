using CellReport.Avalonia.Services;
using CellReport.Avalonia.ViewModels;
using CellReport.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Material.Icons;
using Material.Icons.Avalonia;
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
			ApplyContentInversion();

			_hScrollBar.PropertyChanged += OnHorizontalScrollBarChanged;
			_vScrollBar.PropertyChanged += OnVerticalScrollBarChanged;

			DataContextChanged += OnPresentControlDataContextChanged;
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
				var item = new MenuItem { Header = printerName, Tag = printerName };
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
			if (_printSplitButton.Flyout != null && !_printSplitButton.Flyout.IsOpen)
				_printSplitButton.Flyout.ShowAt(_printSplitButton);
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
