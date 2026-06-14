using CellReport.Export;
using CellReport.Presenters;
using CellReport.Presenters.Skia;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public sealed class TestAssemblySetup
	{
		[AssemblyInitialize]
		public static void Initialize(TestContext context)
		{
			BaseExporter.RendererFactory = new SkiaRendererFactory();
		}

		private sealed class SkiaRendererFactory : IRendererFactory
		{
			public BaseRenderer Create()
			{
				return new SkiaReportRenderer();
			}
		}
	}
}
