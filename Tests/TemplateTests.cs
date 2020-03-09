using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using CellReport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CellReport.Core;

namespace CellReportTests
{
	[TestClass]
	public class TemplateTests
	{
		[TestMethod]
		public void Test1()
		{
			CellManager manager = new CellManager();

			manager.RowsCount = 30;
			manager.ColumnsCount = 20;

			Assert.AreEqual(30, manager.RowsCount);
			Assert.AreEqual(20, manager.ColumnsCount);
		}

		[TestMethod]
		public void Test2()
		{
			CellManager manager = new CellManager(10, 10);

			var cell = manager.CreateCell(2, 3);

			Assert.AreEqual(2, cell.Location.Row);
			Assert.AreEqual(3, cell.Location.Column);
		}

		[TestMethod]
		public void Test3()
		{
			CellManager manager = new CellManager(10, 10);

			var cell = manager.CreateCell(2, 3);
			cell.Span = new CellSpan() {RowSpan = 2, ColSpan = 3};

			Assert.AreEqual(2, cell.Location.Row);
			Assert.AreEqual(3, cell.Location.Column);

			Assert.AreEqual(2, cell.Span.RowSpan);
			Assert.AreEqual(3, cell.Span.ColSpan);

			Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(2, 3)));

			cell = manager.CreateCell(0, 0);
			cell.Span = new CellSpan() {RowSpan = 1, ColSpan = 1};

			Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(0, 0)));
			Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(1, 1)));
			Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(0, 1)));
			Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(1, 0)));
		}

		[TestMethod]
		public void Test4()
		{
			CellManager manager = new CellManager(10, 10, 50f, new Column());

			var cell = manager.CreateCell(2, 3);
			manager.SetRowHeight(2, 100);
			manager.SetColumnWidth(3, 120);

			Assert.AreEqual(100, cell.Size.Height);
			Assert.AreEqual(120, cell.Size.Width);

			Assert.AreEqual(100, manager.GetRowHeight(2));
			Assert.AreEqual(120, manager.GetColumnWidth(3));

			Assert.AreEqual(50, manager.GetRowHeight(1));
			Assert.AreEqual(50, manager.GetColumnWidth(1));

			cell.Span = new CellSpan {ColSpan = 1, RowSpan = 1};

			Assert.AreEqual(150, cell.Size.Height);
			Assert.AreEqual(170, cell.Size.Width);
		}

		[TestMethod]
		public void Test5()
		{
			CellManager manager = new CellManager();

			var style = new CellStyle("#FFFFFF");
			var cell = manager.CreateCell(2, 3);
			cell.Style = style;

			var style2 = new CellStyle("#FFFFFF");
			var cell2 = manager.CreateCell(3, 3);
			cell2.Style = style2;

			Assert.IsTrue(object.ReferenceEquals(cell.Style, cell2.Style));
		}

		[TestMethod]
		public void Test6()
		{
			var templateContent = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Экспорт в Excel</DocumentName>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowResizers=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
    <Headers />
    <Footers />
  </PrintSettings>
  <Sections>
    <Section ID=""header"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""0"" />
    </Section>
    <Section ID=""cell"">
      <LeftTop Row=""1"" Column=""0"" />
      <RightBottom Row=""1"" Column=""0"" />
    </Section>
  </Sections>
  <Table DefaultRowHeight=""5.291667"" DefaultColumnWidth=""17"" RowsCount=""2"" ColumnsCount=""7"">
    <Column Index=""0"" Width=""70.64375"" />
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_header"">
          <Param Name=""header"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
    </Row>
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <TextContentStyle ID=""tc_header"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" Style=""Bold"" />
    </TextContentStyle>
    <TextContentStyle ID=""tc_cell"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" />
    </TextContentStyle>
  </Styles>
  <Expressions />
</CellReportTemplate>";

			var cellTemplate = new CellTemplate();
			cellTemplate.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(templateContent)));
			var report = new CellReport.ReportDocument(cellTemplate);
			report.AddSection("header");
			report.SetParameter("header", "h111");
			report.JoinSection("header");
			report.SetParameter("header", "h222");
			report.JoinSection("header");
			report.SetParameter("header", "h333");

			report.AddSection("cell");
			report.SetParameter("cell", "c111");
			report.JoinSection("cell");
			report.SetParameter("cell", "c222");
			report.JoinSection("cell");
			report.SetParameter("cell", "c333");

			report.AddSection("cell");
			report.SetParameter("cell", "c111");
			report.JoinSection("cell");
			report.SetParameter("cell", "c222");
			report.JoinSection("cell");
			report.SetParameter("cell", "c333");

			var excel = new CellReport.Export.Excel.ExcelExporter(report.Manager);
			using (var file = System.IO.File.Create("..\\..\\..\\temp\\test6.xlsx"))
				excel.ExportTo(file);

			var formatter = new BinaryFormatter();
			var stream = new MemoryStream();
			formatter.Serialize(stream, report);
		}

		[TestMethod]
		public void Test7()
		{
			var templateContent = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName></DocumentName>
  </Properties>
  <ExportSettings RunExcelAfterExport=""false"" />
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""true"" ShowBookMarks=""false"" ShowResizers=""true"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections>
    <Section ID=""Секция 0"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""4"" />
    </Section>
  </Sections>
  <BookMarks />
  <Table RowsCount=""10"" ColumnsCount=""5"">
    <Row Height=""6.770833"" />
    <Row Height=""6.50625"" />
    <Row Height=""5.977083"" />
    <Row Height=""5.183334"" />
    <Row Height=""7.3"" />
    <Row Height=""8.09375"" />
    <Row Height=""8.622917"" />
    <Row Height=""8.358334"" />
    <Row Height=""6.241667"" />
    <Row Height=""4.125"" />
  </Table>
  <Styles>
    <ExternalStyle Uri=""file://DefaultStyles.xml"" />
  </Styles>
  <Expressions />
</CellReportTemplate>";

			var cellTemplate = new CellTemplate();
			cellTemplate.Load(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(templateContent)));
			var report = new CellReport.ReportDocument(cellTemplate);
			report.AddSection("Секция 0");
			report.AddSection("Секция 0");
			report.SetParameter("cell", "c111");
			report.JoinSection("Секция 0");
			report.SetParameter("cell", "c222");
			report.JoinSection("Секция 0");
			report.SetParameter("cell", "c333");
		}

		[TestMethod]
		public void Test8()
		{
			var templateContent = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Экспорт в Excel</DocumentName>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowResizers=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
    <Headers />
    <Footers />
  </PrintSettings>
  <Sections>
    <Section ID=""cell"">
      <LeftTop Row=""1"" Column=""0"" />
      <RightBottom Row=""1"" Column=""5"" />
    </Section>
  </Sections>
  <Table DefaultRowHeight=""5.291667"" DefaultColumnWidth=""17"" RowsCount=""2"" ColumnsCount=""7"">
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Row>
    </Row>
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell1"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""1"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell2"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""2"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell3"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""3"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell4"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""4"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell5"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""5"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell6"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <TextContentStyle ID=""tc_header"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" Style=""Bold"" />
    </TextContentStyle>
    <TextContentStyle ID=""tc_cell"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" />
    </TextContentStyle>
  </Styles>
  <Expressions />
</CellReportTemplate>";

			var cellTemplate = new CellTemplate();
			cellTemplate.Load(new MemoryStream(Encoding.UTF8.GetBytes(templateContent)));
			var report = new ReportDocument(cellTemplate);
			for (int i = 0; i < 100; i++)
			{
				report.AddSection("cell");
				for (int j = 1; j <= 6; j++)
					report.SetParameter("cell" + j, Guid.NewGuid().ToString());
			}
			var formatter = new BinaryFormatter();
			byte[] data;

			var stopWatch = new System.Diagnostics.Stopwatch();
			stopWatch.Start();
			cellTemplate = new CellTemplate(report.Manager);
			using (var stream = new MemoryStream())
			{
				cellTemplate.Save(stream, TemplateType.Package);
				data = stream.ToArray();
			}
			stopWatch.Stop();
			System.Diagnostics.Debug.WriteLine("Template save " + stopWatch.Elapsed + " Length " + data.Length);
			stopWatch.Reset();
			stopWatch.Start();
			cellTemplate = new CellTemplate();
			using (var stream = new MemoryStream(data))
			{
				cellTemplate.Load(stream);
			}
			stopWatch.Stop();
			System.Diagnostics.Debug.WriteLine("Template load " + stopWatch.Elapsed);
			stopWatch.Reset();
			stopWatch.Start();
			using (var stream = new MemoryStream())
			{
				formatter.Serialize(stream, report);
				data = stream.ToArray();
			}
			stopWatch.Stop();
			System.Diagnostics.Debug.WriteLine("Binary save " + stopWatch.Elapsed + " Length " + data.Length);
			stopWatch.Reset();
			stopWatch.Start();
			using (var stream = new MemoryStream(data))
			{
				report = (ReportDocument) formatter.Deserialize(stream);
			}
			stopWatch.Stop();
			System.Diagnostics.Debug.WriteLine("Binary load " + stopWatch.Elapsed);
		}

		[TestMethod]
		public void Test9()
		{
			var templateContent = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Экспорт в Excel</DocumentName>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowResizers=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
    <Headers />
    <Footers />
  </PrintSettings>
  <Sections>
    <Section ID=""cell"">
      <LeftTop Row=""1"" Column=""0"" />
      <RightBottom Row=""1"" Column=""5"" />
    </Section>
  </Sections>
  <Table DefaultRowHeight=""5.291667"" DefaultColumnWidth=""17"" RowsCount=""2"" ColumnsCount=""7"">
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Column Width=""70.64375"" />
    <Row>
    </Row>
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell1"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
      <Cell Index=""1"">
        <TextContent Style=""tc_cell"">
          <Param Name=""cell1"" Target=""Text"" Value="""" />
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <TextContentStyle ID=""tc_header"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" Style=""Bold"" />
    </TextContentStyle>
    <TextContentStyle ID=""tc_cell"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" />
    </TextContentStyle>
  </Styles>
  <Expressions />
</CellReportTemplate>";

			var cellTemplate = new CellTemplate();
			cellTemplate.Load(new MemoryStream(Encoding.UTF8.GetBytes(templateContent)));
			var report = new ReportDocument(cellTemplate);
			// 1000 - 467 040
			for (int i = 0; i < 1000; i++)
			{
				report.AddSection("cell");
				for (int j = 1; j <= 6; j++)
					report.SetParameter("cell" + j, Guid.NewGuid().ToString());
			}
			var formatter = new BinaryFormatter();
			byte[] data;
			using (var stream = new MemoryStream())
			{
				formatter.Serialize(stream, report);
				data = stream.ToArray();
			}
			using (var stream = new MemoryStream(data))
			{
				report = (ReportDocument) formatter.Deserialize(stream);
			}
		}

		[TestMethod]
		public void Test10()
		{
			var templateContent = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName></DocumentName>
  </Properties>
  <ExportSettings RunExcelAfterExport=""true"" />
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowDesignerGridLines=""true"" ShowCurrentRows=""true"" ShowBookMarks=""false"" ShowResizers=""false"" ShowDesignerResizers=""true"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""10"" Left=""10"" Right=""10"" Bottom=""10"" />
  </PrintSettings>
  <Sections>
    <Section ID=""Test"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""2"" />
    </Section>
  </Sections>
  <BookMarks />
  <Table RowsCount=""1"" ColumnsCount=""5"">
    <Column Width=""23.0125"" />
    <Column Width=""123.5542"" />
    <Column Width=""146.5729"" />
    <Column Width=""10.84167"" />
    <Column Width=""8.460415"" />
    <Row Height=""6.241667"">
      <Cell>
        <TextContent>
          <Param Name=""Число"" Target=""Text"" />
        </TextContent>
      </Cell>
      <Cell>
        <TextContent>
          <Param Name=""Число"" Target=""Text"" Value=""1"" Type=""Decimal"" Format=""currencyString|RUR"" />
          <Param Name=""Гор.Выравнивание"" Target=""Style.HAlignment"" Value=""Left"" />
        </TextContent>
      </Cell>
      <Cell>
        <TextContent>
          <Param Name=""Число"" Target=""Text"" Value=""1"" Type=""Decimal"" Format=""currencyString|AZN"" Culture=""az"" />
          <Param Name=""Гор.Выравнивание"" Target=""Style.HAlignment"" Value=""Left"" />
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <ExternalStyle Uri=""file://DefaultStyles.xml"" />
  </Styles>
  <Expressions />
</CellReportTemplate>";

			var cellTemplate = new CellTemplate();
			cellTemplate.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(templateContent)));
			var report = new ReportDocument(cellTemplate);
			for (int i = 0; i < 10000; i++)
			{
				report.AddSection("Test");
				report.SetParameter("Число", i + 0.23m);
			}

			var excel = new CellReport.Export.Excel.ExcelExporter(report.Manager);
			using (var file = System.IO.File.Create("..\\..\\..\\temp\\test10.xlsx"))
				excel.ExportTo(file);
		}

		[TestMethod]
		public void Test11()
		{
			var templateContent = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName></DocumentName>
  </Properties>
  <ExportSettings RunExcelAfterExport=""true"" />
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowDesignerGridLines=""true"" ShowCurrentRows=""true"" ShowBookMarks=""false"" ShowResizers=""false"" ShowDesignerResizers=""true"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""10"" Left=""10"" Right=""10"" Bottom=""10"" />
  </PrintSettings>
  <Sections>
    <Section ID=""Test"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""2"" />
    </Section>
  </Sections>
  <BookMarks />
  <Table RowsCount=""1"" ColumnsCount=""5"">
    <Column Width=""23.0125"" />
    <Column Width=""123.5542"" />
    <Column Width=""146.5729"" />
    <Column Width=""10.84167"" />
    <Column Width=""8.460415"" />
    <Row Height=""6.241667"">
      <Cell ColSpan=""3"">
        <DecimalContent>
          <Param Name=""Количество"" Target=""Value"" />
        </DecimalContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <ExternalStyle Uri=""file://DefaultStyles.xml"" />
  </Styles>
  <Expressions>
    <SumExpression ID=""КоличествоВсего"" ParamName=""Количество"" />
	</Expressions>
	 </CellReportTemplate>";

			var cellTemplate = new CellTemplate();
			cellTemplate.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(templateContent)));
			var report = new ReportDocument(cellTemplate);
			report.AddSection("Test");
			report.SetParameter("Количество", 1);

			var value = (decimal) ((CellReport.Core.Expressions.SumExpression)report.Manager.GetExpressionByID("КоличествоВсего")).GetValue(report.Manager);
			Assert.AreEqual(1m, value);
		}

		[TestMethod]
		public void Test12()
		{
			var templateContent = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName></DocumentName>
  </Properties>
  <ExportSettings RunExcelAfterExport=""true"" />
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowDesignerGridLines=""true"" ShowCurrentRows=""true"" ShowBookMarks=""false"" ShowResizers=""false"" ShowDesignerResizers=""true"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""10"" Left=""10"" Right=""10"" Bottom=""10"" />
  </PrintSettings>
  <Sections>
    <Section ID=""Test"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""2"" />
    </Section>
  </Sections>
  <BookMarks />
  <Table RowsCount=""1"" ColumnsCount=""5"">
    <Column Width=""23.0125"" />
    <Column Width=""123.5542"" />
    <Column Width=""146.5729"" />
    <Column Width=""10.84167"" />
    <Column Width=""8.460415"" />
    <Row Height=""6.241667"">
      <Cell>
      </Cell>
      <Cell>
        <TextContent>
          <Param Name=""Text"" Target=""Text"" />
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <ExternalStyle Uri=""file://DefaultStyles.xml"" />
  </Styles>
  <Expressions />
</CellReportTemplate>";

			var cellTemplate = new CellTemplate();
			cellTemplate.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(templateContent)));
			var report = new ReportDocument(cellTemplate);
			report.AddSection("Test");
			report.SetParameter("Text", "System.ArgumentException: \"\", шестнадцатеричное значение 0x01, является недопустимым знаком.");

			var excel = new CellReport.Export.Excel.ExcelExporter(report.Manager);
			using (var file = System.IO.File.Create("..\\..\\..\\temp\\test12.xlsx"))
				excel.ExportTo(file);
		}
	}
}
