using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CellReport.Avalonia
{
	public partial class PresentControl : UserControl
	{
		public PresentControl()
		{
			this.InitializeComponent();

			var cellControl = this.Find<CellControl>("cellControl");
			var columnsResizer = this.Find<ColumnResizerControl>("columnsResizer");
			var cellTemplate = new CellReport.Core.CellTemplate();
			cellTemplate.Load("D:\\Projects\\Kayala-soft\\Base\\Modules\\Torg12Form\\Торг12.tcr");
			cellControl.Presenter = new DisplayPresenter(cellTemplate.Manager, this);
			columnsResizer.Presenter = cellControl.Presenter;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		internal void Invalidate()
		{
			var cellControl = this.Find<CellControl>("cellControl");
			var columnsResizer = this.Find<ColumnResizerControl>("columnsResizer");

			cellControl.InvalidateVisual();
			columnsResizer.InvalidateVisual();
		}
	}
}
