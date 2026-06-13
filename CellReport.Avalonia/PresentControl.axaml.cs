using CellReport.Avalonia.ViewModels;
using CellReport.Core;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.ComponentModel;

namespace CellReport.Avalonia
{
	public partial class PresentControl : UserControl
	{
		private CellControl _cellControl;
		private ColumnResizerControl _columnsResizer;
		private RowResizerControl _rowsResizer;
		private AvaloniaDisplayPresenter _presenter;
		private PresentControlViewModel _viewModel;

		public PresentControl()
		{
			InitializeComponent();
			_cellControl = this.Find<CellControl>("cellControl");
			_columnsResizer = this.Find<ColumnResizerControl>("columnsResizer");
			_rowsResizer = this.Find<RowResizerControl>("rowsResizer");

			DataContextChanged += OnPresentControlDataContextChanged;
		}

		public new AvaloniaDisplayPresenter Presenter => _presenter;

		private void OnPresentControlDataContextChanged(object sender, System.EventArgs e)
		{
			if (_viewModel != null)
				_viewModel.PropertyChanged -= OnViewModelPropertyChanged;

			_viewModel = DataContext as PresentControlViewModel;
			if (_viewModel == null)
				return;

			_viewModel.PropertyChanged += OnViewModelPropertyChanged;
			if (_viewModel.Manager != null)
				AttachManager(_viewModel.Manager);
		}

		private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PresentControlViewModel.Manager) && _viewModel?.Manager != null)
				AttachManager(_viewModel.Manager);
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
			Invalidate();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		internal void Invalidate()
		{
			_cellControl?.InvalidateVisual();
			_columnsResizer?.InvalidateVisual();
			_rowsResizer?.InvalidateVisual();
		}
	}
}
