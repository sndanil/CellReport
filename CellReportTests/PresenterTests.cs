using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using CellReport;
using CellReport.Core;
using CellReport.Presenters.WinForms;
using NUnit.Framework;

namespace CellReportTests
{
	[TestFixture]
	public class PresenterTests
	{
		[Test]
		public void Test1()
		{
			var doc = new ReportDocument("..\\..\\..\\Temp\\ОборотыДС.tcrp");
			doc.AddSection("1");
			doc.SetParameter("Наим", "Short Text");

			doc.AddSection("1");
			doc.SetParameter("Наим", "Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text Long Text");

			doc.AddSection("2");
			var str = "Приходный кассовый ордер 2Пко-1 (05.11.2012 1:10:12)";
			str += Environment.NewLine + "Это основание";
			str += Environment.NewLine + "Это комментарий";
			doc.SetParameter("Наим", str);

			var height1 = doc.Manager.GetRowHeight(0);
			var height2 = doc.Manager.GetRowHeight(1);

			var form = new ReportForm();
			var pesenter = new DisplayPresenter(doc.Manager, form);

			Assert.AreEqual(height1, doc.Manager.GetRowHeight(0));
			Assert.AreNotEqual(height2, doc.Manager.GetRowHeight(1));

			form.ShowDialog();
		}

		[Test]
		public void Test2()
		{
			var templateContent = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Алкомарки</DocumentName>
  </Properties>
  <ExportSettings RunExcelAfterExport=""true"" />
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowDesignerGridLines=""true"" ShowCurrentRows=""true"" ShowBookMarks=""false"" ShowResizers=""false"" ShowDesignerResizers=""true"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""10"" Left=""10"" Right=""10"" Bottom=""10"" />
  </PrintSettings>
  <Sections>
    <Section ID=""Этикетка"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""3"" Column=""2"" />
    </Section>
  </Sections>
  <BookMarks />
  <Table RowsCount=""10"" ColumnsCount=""5"">
    <Column Width=""2"" />
    <Column Width=""85"" />
    <Column Width=""2"" />
    <Row Height=""2"" />
    <Row Height=""10"">
      <Cell Index=""1"">
        <TextContent Style=""Текст10Центр"">
          <Param Name=""Наим"" Target=""Text"" Value=""Наим"" />
        </TextContent>
      </Cell>
    </Row>
    <Row Height=""25"">
      <Cell Index=""1"">
        <ImageContent>
          <Param Name=""Картинка"" Target=""Uri"" Value=""Resource://image0"" />
        </ImageContent>
      </Cell>
    </Row>
    <Row Height=""2"" />
  </Table>
  <Styles>
    <ExternalStyle Uri=""file://DefaultStyles.xml"" />
  </Styles>
  <Expressions />
</CellReportTemplate>";

			var cellTemplate = new CellTemplate();
			cellTemplate.Load(new MemoryStream(Encoding.UTF8.GetBytes(templateContent)));
			var report = new ReportDocument(cellTemplate);
			report.AddSection("Этикетка");
			report.SetParameter("Картинка", "Resource://image1");
			report.Manager.AddResource("image1", CreateImage(Color.Red, 100));
			report.AddSection("Этикетка");
			report.SetParameter("Картинка", "Resource://image2");
			report.Manager.AddResource("image2", CreateImage(Color.Blue, 100));
			//(new CellReport.Core.CellTemplate(report.Manager)).Save("d:\\ttt.tcrp", TemplateType.Package);

			var form = new ReportForm();
			var pesenter = new DisplayPresenter(report.Manager, form);

			form.ShowDialog();

		}

		public static byte[] CreateImage(Color color, int boxSize)
		{
			using (var bmp = new Bitmap(boxSize, boxSize))
			{
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.Clear(color);
				}

				var memStream = new MemoryStream();
				bmp.Save(memStream, ImageFormat.Png);
				return memStream.ToArray();
			}
		}

		[Test]
		public void Test3()
		{
			var template = new CellReport.Core.CellTemplate();
			template.Load("d:\\Downloads\\tmpAE03.tmp");
			var report = new ReportDocument(template, template.Manager);
			report.FitDocument();
		}
	}
}
