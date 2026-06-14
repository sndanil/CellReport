using CellReport.Core;
using CellReport.Export;
using CellReport.Presenters;

namespace CellReport.Avalonia
{
	public class AvaloniaRendererFactory : IRendererFactory
	{
		public BaseRenderer Create()
		{
			return new AvaloniaRenderer();
		}
	}
}
