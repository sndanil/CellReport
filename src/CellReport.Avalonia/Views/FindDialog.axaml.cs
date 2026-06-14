using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CellReport.Avalonia.ViewModels;
using CellReport.Presenters;

namespace CellReport.Avalonia.Views
{
	public partial class FindDialog : Window
	{
		public FindDialog()
		{
			InitializeComponent();
		}

		public FindDialog(BaseDisplayPresenter presenter) : this()
		{
			DataContext = new FindDialogViewModel(presenter);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
