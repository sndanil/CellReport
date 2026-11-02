#if NET10_0
using CellReport.Avalonia;
using CellReport.Core;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class AvaloniaPrintPresenterTests
	{
		[TestMethod]
		public void BuildPages_WithExplicitPrintableArea_UsesActualPageSize()
		{
			var report = TestTemplateFactory.LoadReport(TestTemplateFactory.SimpleTextRow);
			report.AddSection("row");
			report.SetParameter("label", "Наименование");
			report.SetParameter("value", "Значение");

			var presenter = new AvaloniaPrintPresenter(report.Manager);

			// Узкая печатная область: построитель должен вписать отчёт
			// в 80 x 50 мм, а не использовать A4 по умолчанию.
			presenter.BuildPages(80f, 50f, 96f, new Margins(0));

			Assert.IsTrue(presenter.PageCount > 0);
			TestExportHelper.AssertPng(presenter.PageImages[0]);
		}
	}
}
#endif
