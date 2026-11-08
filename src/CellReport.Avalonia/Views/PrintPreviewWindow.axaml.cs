using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CellReport.Avalonia.ViewModels;

namespace CellReport.Avalonia.Views
{
	public partial class PrintPreviewWindow : Window
	{
		public PrintPreviewWindow(AvaloniaPrintPresenter presenter, string? printerName = null)
		{
			InitializeComponent();
			var model = new PrintPreviewViewModel(presenter, this, printerName);
			DataContext = model;
			var viewer = this.FindControl<ScrollViewer>("PreviewViewer");
			if (viewer == null)
				return;

			void Fit() => model.FitToViewport(viewer.Bounds.Width - 48, viewer.Bounds.Height - 48);
			viewer.SizeChanged += (_, _) => Fit();
			model.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName is nameof(PrintPreviewViewModel.PageImage)
				    or nameof(PrintPreviewViewModel.SelectedZoomIndex))
					Fit();
			};
			Opened += (_, _) => Fit();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
