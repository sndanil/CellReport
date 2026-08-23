using CellReport.Avalonia.Services;
using CellReport.Avalonia.ViewModels;
using CellReport.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CellReport.Avalonia
{
	public partial class PresentControl : UserControl
	{
		private CellControl _cellControl;
		private ColumnResizerControl _columnsResizer;
		private RowResizerControl _rowsResizer;
		private ScrollBar _hScrollBar;
		private ScrollBar _vScrollBar;
		private SplitButton _printSplitButton;
		private AvaloniaDisplayPresenter? _presenter;
		private PresentControlViewModel? _viewModel;
		private bool _updatingScrollBars;
		private readonly List<MenuItem> _printerMenuItems = new();

		public PresentControl()
		{
			InitializeComponent();
			_cellControl = this.Find<CellControl>("cellControl");
			_columnsResizer = this.Find<ColumnResizerControl>("columnsResizer");
			_rowsResizer = this.Find<RowResizerControl>("rowsResizer");
			_hScrollBar = this.Find<ScrollBar>("hScrollBar");
			_vScrollBar = this.Find<ScrollBar>("vScrollBar");
			_printSplitButton = this.Find<SplitButton>("printSplitButton");

			_hScrollBar.PropertyChanged += OnHorizontalScrollBarChanged;
			_vScrollBar.PropertyChanged += OnVerticalScrollBarChanged;
			_columnsResizer.LayoutUpdated += OnColumnsResizerLayoutUpdated;

			DataContextChanged += OnPresentControlDataContextChanged;
		}

		public new AvaloniaDisplayPresenter? Presenter => _presenter;

		private void OnPresentControlDataContextChanged(object? sender, System.EventArgs e)
		{
			if (_viewModel != null)
				_viewModel.PropertyChanged -= OnViewModelPropertyChanged;

			_viewModel = DataContext as PresentControlViewModel;
			if (_viewModel == null)
				return;

			_viewModel.PropertyChanged += OnViewModelPropertyChanged;
			if (_viewModel.Manager != null)
				AttachManager(_viewModel.Manager);

			UpdateContextMenuDataContext();
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

			var flyout = new MenuFlyout();

			if (_viewModel.Printers.Count > 0)
			{
				_printerMenuItems.Clear();
				foreach (var printerName in _viewModel.Printers)
				{
					var item = new MenuItem { Header = printerName, Tag = printerName };
					item.Click += OnPrinterMenuClick;
					flyout.Items.Add(item);
					_printerMenuItems.Add(item);
				}

				UpdatePrinterCheckmarks();
				flyout.Items.Add(new Separator());
			}

			var customPrintItem = new MenuItem
			{
				Header = _viewModel.CustomPrintLabel,
				Command = _viewModel.CustomPrintCommand
			};
			var previewItem = new MenuItem
			{
				Header = _viewModel.PreviewLabel,
				Command = _viewModel.PrintPreviewCommand
			};
			var pageSetupItem = new MenuItem
			{
				Header = _viewModel.PageSetupLabel,
				Command = _viewModel.PageSetupCommand
			};

			flyout.Items.Add(customPrintItem);
			flyout.Items.Add(previewItem);
			flyout.Items.Add(pageSetupItem);

			_printSplitButton.Flyout = flyout;
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
		}

		public void AttachManager(CellManager manager)
		{
			if (manager == null)
				return;

			CellManager.IsDesignMode = false;
			_presenter = new AvaloniaDisplayPresenter(manager, this, _cellControl);
			_cellControl.Presenter = _presenter;
			_columnsResizer.Presenter = _presenter;
			_rowsResizer.Presenter = _presenter;

			_viewModel?.AttachPresenter(_presenter, this);
			Update();
		}

		private void UpdateResizerVisibility()
		{
			var showResizers = _presenter != null && _presenter.ShowResizers && !CellManager.IsDesignMode;
			_columnsResizer.IsVisible = showResizers;
			_rowsResizer.IsVisible = showResizers;
			UpdateRowResizerTopOffset();
		}

		private void OnColumnsResizerLayoutUpdated(object? sender, System.EventArgs e)
		{
			UpdateRowResizerTopOffset();
		}

		private void UpdateRowResizerTopOffset()
		{
			_rowsResizer.TopOffset = _columnsResizer.IsVisible ? _columnsResizer.Bounds.Height : 0;
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
			_cellControl?.InvalidateVisual();
			_columnsResizer?.InvalidateVisual();
			_rowsResizer?.InvalidateVisual();
		}
	}
}
