using Avalonia.Controls;
using CellReport.Avalonia;

namespace CellReport.Editor.Avalonia.Views;

public partial class DesignerView : UserControl
{
	public DesignerView()
	{
		InitializeComponent();
	}

	public PresentControl Surface => ReportSurface;
}
