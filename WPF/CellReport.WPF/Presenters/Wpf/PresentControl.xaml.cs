using System;
using System.Windows;
using System.Windows.Controls;
using CellReport.Core;

namespace CellReport.Presenters.Wpf
{
	/// <summary>
	/// Interaction logic for PresentControl.xaml
	/// </summary>
	public partial class PresentControl : UserControl
	{
		private BaseDisplayPresenter _presenter;

		public PresentControl()
		{
			InitializeComponent();
		}

		public void SetPresenter(DisplayPresenter presenter)
		{
			_cellControl.Presenter = presenter;
			_columnResizer.Presenter = _cellControl.Presenter;
			_rowResizer.Presenter = _cellControl.Presenter;
			_presenter = _cellControl.Presenter;
		}

		internal void Update()
		{
			_hScrollBar.Maximum = _presenter.Manager.ColumnsCount;
			_vScrollBar.Maximum = _presenter.Manager.RowsCount;
			_vScrollBar.Value = _presenter.TopRow;
			_hScrollBar.Value = _presenter.LeftColumn;

			_columnResizerHost.Visibility = ShowResizers ? Visibility.Visible : Visibility.Collapsed;
			_rowResizerHost.Visibility = ShowResizers ? Visibility.Visible : Visibility.Collapsed;
			_columnResizer.Invalidate(true);
			_rowResizer.Invalidate(true);
			_cellControl.Invalidate(true);
		}

		private bool ShowResizers { get { return Manager.DisplaySettings.ShowResizers || Manager.DisplaySettings.ShowDesignerResizers && CellManager.IsDesignMode; } }
		private CellManager Manager { get { return _presenter.Manager; } }

		private void HScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_presenter.LeftColumn = Convert.ToInt32(e.NewValue);
		}

		private void VScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_presenter.TopRow = Convert.ToInt32(e.NewValue);
		}

		public WinForms.PresentControl GetWinFormsControl()
		{
			return _cellControl;
		}
	}
}
