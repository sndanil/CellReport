using System;
using System.IO;
using System.Text;
using CellReport;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;
using CellReport.Localization;
using CellReportTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class TemplateLocalizationTests
	{
		private const string LocalizedTemplate = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>#docTitle</DocumentName>
    <Localization DefaultCulture=""ru"">
      <Strings Culture=""ru"">
        <String ID=""docTitle"" Value=""Отчёт"" />
        <String ID=""greeting"" Value=""Привет"" />
      </Strings>
      <Strings Culture=""en"">
        <String ID=""docTitle"" Value=""Report"" />
        <String ID=""greeting"" Value=""Hello"" />
      </Strings>
    </Localization>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowResizers=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections>
    <Section ID=""main"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""0"" />
    </Section>
  </Sections>
  <Table DefaultRowHeight=""5.291667"" DefaultColumnWidth=""17"" RowsCount=""1"" ColumnsCount=""1"">
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_main"">
          <Text>#greeting</Text>
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <TextContentStyle ID=""tc_main"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" />
    </TextContentStyle>
  </Styles>
  <Expressions />
</CellReportTemplate>";

		private const string PlainTemplate = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Plain</DocumentName>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowResizers=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections>
    <Section ID=""main"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""0"" />
    </Section>
  </Sections>
  <Table DefaultRowHeight=""5.291667"" DefaultColumnWidth=""17"" RowsCount=""1"" ColumnsCount=""1"">
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_main"">
          <Text>#test/shared</Text>
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <TextContentStyle ID=""tc_main"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" />
    </TextContentStyle>
  </Styles>
  <Expressions />
</CellReportTemplate>";

		private const string MixedTemplate = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Mixed</DocumentName>
    <Localization DefaultCulture=""ru"">
      <Strings Culture=""ru"">
        <String ID=""greeting"" Value=""Привет"" />
      </Strings>
      <Strings Culture=""en"">
        <String ID=""greeting"" Value=""Hello"" />
      </Strings>
    </Localization>
  </Properties>
  <DisplaySettings FrozenColumns=""0"" FrozenRows=""0"" ShowGridLines=""false"" ShowResizers=""false"" Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections>
    <Section ID=""main"">
      <LeftTop Row=""0"" Column=""0"" />
      <RightBottom Row=""0"" Column=""0"" />
    </Section>
  </Sections>
  <Table DefaultRowHeight=""5.291667"" DefaultColumnWidth=""17"" RowsCount=""1"" ColumnsCount=""1"">
    <Row>
      <Cell Index=""0"">
        <TextContent Style=""tc_main"">
          <Text>#greeting, [name]!</Text>
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <TextContentStyle ID=""tc_main"" Color=""Black"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Bottom"" />
      <Font Family=""Arial"" Size=""10"" />
    </TextContentStyle>
  </Styles>
  <Expressions />
</CellReportTemplate>";

		[TestCleanup]
		public void Cleanup()
		{
			TemplateLocalizer.Resolver = null;
		}

		[TestMethod]
		public void GetText_ResolvesKeyForDocumentCulture()
		{
			var report = TestTemplateFactory.LoadReport(LocalizedTemplate);
			report.AddSection("main");
			report.Culture = "en";

			Assert.AreEqual("Hello", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void GetText_WithoutCultureUsesDefaultCulture()
		{
			var report = TestTemplateFactory.LoadReport(LocalizedTemplate);
			report.AddSection("main");

			Assert.AreEqual("Привет", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void GetText_FallsBackToDefaultCultureForUnknownCulture()
		{
			var report = TestTemplateFactory.LoadReport(LocalizedTemplate);
			report.AddSection("main");
			report.Culture = "de";

			Assert.AreEqual("Привет", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void GetText_FallsBackToGlobalLocalizationManager()
		{
			LocalizationManager.AddDefaultString("test/shared", "Глобальное значение");
			var report = TestTemplateFactory.LoadReport(PlainTemplate);
			report.AddSection("main");

			Assert.AreEqual("Глобальное значение", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void GetText_ReturnsRawIdWhenNothingResolved()
		{
			var xml = PlainTemplate.Replace("#test/shared", "#test/unknown-key-42");
			var report = TestTemplateFactory.LoadReport(xml);
			report.AddSection("main");

			Assert.AreEqual("test/unknown-key-42", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void Resolver_OverridesAllSources()
		{
			TemplateLocalizer.Resolver = (key, culture) => culture == "en" ? "RESOLVED:" + key : null;
			var report = TestTemplateFactory.LoadReport(LocalizedTemplate);
			report.AddSection("main");

			report.Culture = "en";
			Assert.AreEqual("RESOLVED:#greeting", GetCellText(report.Manager, 0, 0));

			report.Culture = "ru";
			Assert.AreEqual("Привет", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void DesignMode_ShowsRawKey()
		{
			var report = TestTemplateFactory.LoadReport(LocalizedTemplate);
			report.Manager.IsDesignMode = true;
			report.AddSection("main");
			report.Culture = "en";

			Assert.AreEqual("#greeting", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void RoundTrip_PreservesLocalization()
		{
			var template = new CellTemplate();
			template.LoadFromXml(LocalizedTemplate);

			var reloaded = RoundTrip(template);
			Assert.AreEqual("ru", reloaded.Manager.Properties.Localization.DefaultCulture);

			var report = new ReportDocument(reloaded);
			report.AddSection("main");
			report.Culture = "en";

			Assert.AreEqual("Hello", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void ProgrammaticStrings_AreUsedAtRenderTime()
		{
			var template = new CellTemplate();
			template.LoadFromXml(PlainTemplate);
			template.Manager.Properties.Localization.DefaultCulture = "ru";
			template.Manager.Properties.Localization.Add("ru", "test/shared", "Из кода");

			var report = new ReportDocument(template);
			report.AddSection("main");

			Assert.AreEqual("Из кода", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void DocumentName_ResolvesKeyByCulture()
		{
			var template = new CellTemplate();
			template.LoadFromXml(LocalizedTemplate);
			var report = new ReportDocument(template);
			report.Culture = "en";

			Assert.AreEqual("Report", report.Manager.Properties.DocumentName);

			report.Culture = "ru";
			Assert.AreEqual("Отчёт", report.Manager.Properties.DocumentName);
		}

		[TestMethod]
		public void DocumentName_AutoResolvesViaProperty()
		{
			var template = new CellTemplate();
			template.LoadFromXml(LocalizedTemplate);
			template.Manager.Properties.Culture = "en";

			Assert.AreEqual("Report", template.Manager.Properties.DocumentName);
		}

		[TestMethod]
		public void DocumentName_RawKeyPreservedForSerialization()
		{
			var template = new CellTemplate();
			template.LoadFromXml(LocalizedTemplate);
			template.Manager.Properties.Culture = "en";

			Assert.AreEqual("Report", template.Manager.Properties.DocumentName);
			Assert.AreEqual("#docTitle", template.Manager.Properties.RawDocumentName);
		}

		[TestMethod]
		public void DocumentName_RoundTrip_PreservesRawKey()
		{
			var template = new CellTemplate();
			template.LoadFromXml(LocalizedTemplate);

			var reloaded = RoundTrip(template);
			Assert.AreEqual("#docTitle", reloaded.Manager.Properties.RawDocumentName);

			reloaded.Manager.Properties.Culture = "en";
			Assert.AreEqual("Report", reloaded.Manager.Properties.DocumentName);
		}

		[TestMethod]
		public void GetString_PlainTextPassesThroughUnchanged()
		{
			Assert.AreEqual("Обычный текст", TemplateLocalizer.GetString(null, "Обычный текст", "en"));
			Assert.AreEqual(null, TemplateLocalizer.GetString(null, null, "en"));
		}

		[TestMethod]
		public void MixedText_KeysResolvedBeforeParams()
		{
			var template = new CellTemplate();
			template.LoadFromXml(MixedTemplate);
			var report = new ReportDocument(template);
			report.AddSection("main");
			report.SetParameter("name", "World");
			report.Culture = "en";

			Assert.AreEqual("Hello, World!", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void MixedText_DefaultCultureFallbackWithParams()
		{
			var template = new CellTemplate();
			template.LoadFromXml(MixedTemplate);
			var report = new ReportDocument(template);
			report.AddSection("main");
			report.SetParameter("name", "Мир");

			Assert.AreEqual("Привет, Мир!", GetCellText(report.Manager, 0, 0));
		}

        [TestMethod]
        public void Resolver_InlineKeys_NotCorruptedByNumbering()
        {
            TemplateLocalizer.Resolver = (key, culture) => "RESOLVED";
            var xml = MixedTemplate.Replace("#greeting, [name]!", "#greeting, #1 [name]! #2");
            var report = TestTemplateFactory.LoadReport(xml);
            report.AddSection("main");
            report.SetParameter("name", "Тест");

            report.Culture = "en";
            Assert.AreEqual("RESOLVED, #1 Тест! #2", GetCellText(report.Manager, 0, 0));
        }

        [TestMethod]
        public void Resolver_ParamFromResolver()
        {
            TemplateLocalizer.Resolver = (key, culture) => "RESOLVED [name]";
            var report = TestTemplateFactory.LoadReport(MixedTemplate);
            report.AddSection("main");
            report.SetParameter("name", "Тест");

            Assert.AreEqual("RESOLVED Тест, Тест!", GetCellText(report.Manager, 0, 0));
        }

        [TestMethod]
		public void InlineKeys_NotCorruptedByNumbering()
		{
			var xml = MixedTemplate.Replace("#greeting, [name]!", "Пункт #1 [name]");
			var template = new CellTemplate();
			template.LoadFromXml(xml);
			var report = new ReportDocument(template);
			report.AddSection("main");
			report.SetParameter("name", "Тест");
			report.Culture = "en";

			Assert.AreEqual("Пункт #1 Тест", GetCellText(report.Manager, 0, 0));
		}

		[TestMethod]
		public void InlineKeys_NoKeysNoChange()
		{
			var template = new CellTemplate();
			template.LoadFromXml(MixedTemplate);
			var report = new ReportDocument(template);
			report.AddSection("main");
			report.SetParameter("name", "Мир");
			report.Culture = "ru";

			var text = GetCellText(report.Manager, 0, 0);
			Assert.AreEqual("Привет, Мир!", text);
		}

		private static CellTemplate RoundTrip(CellTemplate template)
		{
			using (var stream = new MemoryStream())
			{
				template.Save(stream, TemplateType.Xml);
				stream.Position = 0;
				var reloaded = new CellTemplate();
				reloaded.Load(stream);
				return reloaded;
			}
		}

		private static string GetCellText(CellManager manager, int row, int column)
		{
			var cell = manager.GetCell(row, column);
			Assert.IsNotNull(cell);
			Assert.IsInstanceOfType(cell.Content, typeof(TextContent));
			return ((TextContent)cell.Content).GetText();
		}
	}
}
