using CellReport.Avalonia.ViewModels;
using CellReport.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using System.ComponentModel;

namespace CellReport.Avalonia
{
	public partial class PresentControl : UserControl
	{
		private CellControl _cellControl;
		private ColumnResizerControl _columnsResizer;
		private RowResizerControl _rowsResizer;
		private ScrollBar _hScrollBar;
		private ScrollBar _vScrollBar;
		private AvaloniaDisplayPresenter? _presenter;
		private PresentControlViewModel? _viewModel;
		private bool _updatingScrollBars;

		public PresentControl()
		{
			InitializeComponent();
			_cellControl = this.Find<CellControl>("cellControl");
			_columnsResizer = this.Find<ColumnResizerControl>("columnsResizer");
			_rowsResizer = this.Find<RowResizerControl>("rowsResizer");
			_hScrollBar = this.Find<ScrollBar>("hScrollBar");
			_vScrollBar = this.Find<ScrollBar>("vScrollBar");

			_hScrollBar.PropertyChanged += OnHorizontalScrollBarChanged;
			_vScrollBar.PropertyChanged += OnVerticalScrollBarChanged;

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
		}

		private void UpdateContextMenuDataContext()
		{
			if (_cellControl.ContextMenu != null)
				_cellControl.ContextMenu.DataContext = _viewModel;
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
