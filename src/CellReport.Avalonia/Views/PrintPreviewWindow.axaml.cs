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
			DataContext = new PrintPreviewViewModel(presenter, this, printerName);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
