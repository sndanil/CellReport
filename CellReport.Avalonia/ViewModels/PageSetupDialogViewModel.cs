using CellReport.Avalonia.Services;
using CellReport.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CellReport.Avalonia.ViewModels
{
	public partial class PageSetupDialogViewModel : ObservableObject
	{
		public PageSetupDialogViewModel(PrintSettings settings)
		{
			MarginLeft = settings.Margins.Left;
			MarginRight = settings.Margins.Right;
			MarginTop = settings.Margins.Top;
			MarginBottom = settings.Margins.Bottom;
			Landscape = settings.Landscape;
			Copies = settings.Copies;
			Scale = settings.Scale;
		}

		public string Title => ReportLocalization.Get("Report/PageSettings");

		public string MarginsLabel => ReportLocalization.Get("Report/PageSetup/Margins");

		public string LeftLabel => ReportLocalization.Get("Report/PageSetup/Left");

		public string RightLabel => ReportLocalization.Get("Report/PageSetup/Right");

		public string TopLabel => ReportLocalization.Get("Report/PageSetup/Top");

		public string BottomLabel => ReportLocalization.Get("Report/PageSetup/Bottom");

		public string LandscapeLabel => ReportLocalization.Get("Report/PageSetup/Landscape");

		public string CopiesLabel => ReportLocalization.Get("Report/PageSetup/Copies");

		public string ScaleLabel => ReportLocalization.Get("Report/Scale");

		public string OkLabel => ReportLocalization.Get("Report/GoToRow/Ok");

		public string CancelLabel => ReportLocalization.Get("Report/GoToRow/Cancel");

		[ObservableProperty]
		private double _marginLeft;

		[ObservableProperty]
		private double _marginRight;

		[ObservableProperty]
		private double _marginTop;

		[ObservableProperty]
		private double _marginBottom;

		[ObservableProperty]
		private bool _landscape;

		[ObservableProperty]
		private int _copies = 1;

		[ObservableProperty]
		private double _scale = 1.0;

		public void ApplyTo(PrintSettings settings)
		{
			settings.Margins = new Margins((float)MarginLeft, (float)MarginRight, (float)MarginTop, (float)MarginBottom);
			settings.Landscape = Landscape;
			settings.Copies = (short)Copies;
			settings.Scale = (float)Scale;
		}
	}
}
