using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CellReport.Avalonia.ViewModels;

namespace CellReport.Avalonia.Views
{
	public partial class PrintPreviewWindow : Window
	{
		public PrintPreviewWindow(AvaloniaPrintPresenter presenter)
		{
			InitializeComponent();
			DataContext = new PrintPreviewViewModel(presenter, this);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
