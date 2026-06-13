using CellReport.Core;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CellReport.Avalonia.ViewModels
{
	public partial class PresentControlViewModel : ObservableObject
	{
		[ObservableProperty]
		private CellManager? _manager;

		public void Load(CellManager manager)
		{
			Manager = manager;
		}
	}
}
