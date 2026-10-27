using System;
using System.IO;
using System.Linq;
using System.Text;
using CellReport.Core;
using CellReport.Core.Contents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class StyleCompressionTests
	{
		private string _directory = string.Empty;

		[TestInitialize]
		public void Initialize()
		{
			_directory = Path.Combine(Path.GetTempPath(), "cellreport-styles-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_directory);
		}

		[TestCleanup]
		public void Cleanup()
		{
			try
			{
				Directory.Delete(_directory, true);
			}
			catch
			{
				// временная папка может остаться — не влияет на тест
			}
		}

		[TestMethod]
		public void Load_MarksExternalStyles()
		{
			WriteExternalStyles();
			var template = LoadTemplate();

			var external = template.Manager.GetExternalStyles().ToList();
			var own = template.Manager.GetTemplateStyles().ToList();

			Assert.AreEqual(3, external.Count, "из внешнего файла должно загрузиться три стиля");
			Assert.AreEqual(4, own.Count, "в шаблоне четыре собственных стиля");
			Assert.IsTrue(own.All(style => !template.Manager.IsExternalStyle(style)));
			Assert.IsTrue(external.All(style => template.Manager.IsExternalStyle(style)));
			Assert.IsTrue(template.Manager.GetExternalStyleSources().Any(item => item.Uri == "file://DefaultStyles.xml"));

			using var stream = new MemoryStream();
			new CellTemplate(template.Manager).Save(stream, TemplateType.Xml);
			var saved = Encoding.UTF8.GetString(stream.ToArray());
			Assert.IsTrue(saved.Contains("<ExternalStyle"), "ссылка на внешний файл сохраняется");
			Assert.IsTrue(saved.Contains("ID=\"ext1\""), "дубль стиля из шаблона сохраняется до сжатия");
			Assert.IsTrue(saved.Contains("ID=\"keep\""), "собственный стиль сохраняется");
			Assert.IsFalse(saved.Contains("ID=\"ext2\""), "стиль только из внешнего файла не дублируется в шаблоне");
		}

		[TestMethod]
		public void Compress_RemovesStylesMatchingExternal()
		{
			WriteExternalStyles();
			var template = LoadTemplate();

			var result = template.Manager.CompressStyles();

			Assert.AreEqual(3, result.Removed, "удаляются дубль по ID и стили с тем же оформлением");
			Assert.AreEqual(2, result.Remapped, "стили с другими ID перенаправляются на внешние");
			Assert.AreEqual(2, result.References, "ссылки ячейки и содержимого перенаправлены");

			var own = template.Manager.GetTemplateStyles().Select(style => style.ID).OrderBy(id => id).ToArray();
			CollectionAssert.AreEqual(new[] { "keep" }, own);

			var cell = template.Manager.GetCell(0, 0);
			Assert.AreEqual("extCell", cell.StyleID, "стиль ячейки перенаправлен на внешний");
			Assert.AreEqual("ext2", cell.Content.StyleID, "стиль текста перенаправлен на внешний");

			Assert.IsNotNull(template.Manager.GetStyleByID<TextContentStyle>("ext2"), "внешний стиль текста остался доступен");
			Assert.IsNotNull(template.Manager.GetStyleByID<CellStyle>("extCell"), "внешний стиль ячейки остался доступен");
			Assert.IsNull(template.Manager.GetStyleByID<TextContentStyle>("local"));
			Assert.IsNull(template.Manager.GetStyleByID<CellStyle>("localCell"));

			using var stream = new MemoryStream();
			new CellTemplate(template.Manager).Save(stream, TemplateType.Xml);
			var saved = Encoding.UTF8.GetString(stream.ToArray());
			Assert.IsFalse(saved.Contains("ID=\"local\""), "удалённый стиль не должен попадать в шаблон");
			Assert.IsFalse(saved.Contains("ID=\"localCell\""), "удалённый стиль ячейки не должен попадать в шаблон");
			Assert.IsFalse(saved.Contains("ID=\"ext1\""), "дубль внешнего стиля удалён при сжатии");
			Assert.IsFalse(saved.Contains("ID=\"ext2\""), "внешний стиль не встраивается в шаблон");
			Assert.IsTrue(saved.Contains("ID=\"keep\""), "уникальный стиль шаблона сохраняется");
			Assert.IsTrue(saved.Contains("Style=\"ext2\""), "ссылки сохранены на внешний стиль");
		}

		private void WriteExternalStyles()
		{
			const string external = @"<?xml version=""1.0"" encoding=""utf-8""?>
<CellReportTemplate>
  <Styles>
    <TextContentStyle ID=""ext1"" Color=""Red"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Middle"" />
      <Font Family=""Arial"" Size=""10"" Style=""Regular"" />
    </TextContentStyle>
    <TextContentStyle ID=""ext2"" Color=""Blue"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Right"" Vertical=""Middle"" />
      <Font Family=""Verdana"" Size=""9"" Style=""Bold"" />
    </TextContentStyle>
    <CellStyle ID=""extCell"" Color=""#EEEEEE"" />
  </Styles>
</CellReportTemplate>";
			File.WriteAllText(Path.Combine(_directory, "DefaultStyles.xml"), external, new UTF8Encoding(false));
		}

		private CellTemplate LoadTemplate()
		{
			const string template = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Styles</DocumentName>
  </Properties>
  <DisplaySettings Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections />
  <BookMarks />
  <Table RowsCount=""1"" ColumnsCount=""1"">
    <Row>
      <Cell Index=""0"" Style=""localCell"">
        <TextContent Style=""local"">
          <Text>Текст</Text>
        </TextContent>
      </Cell>
    </Row>
  </Table>
  <Styles>
    <ExternalStyle Uri=""file://DefaultStyles.xml"" />
    <TextContentStyle ID=""ext1"" Color=""Red"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Left"" Vertical=""Middle"" />
      <Font Family=""Arial"" Size=""10"" Style=""Regular"" />
    </TextContentStyle>
    <TextContentStyle ID=""local"" Color=""Blue"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Right"" Vertical=""Middle"" />
      <Font Family=""Verdana"" Size=""9"" Style=""Bold"" />
    </TextContentStyle>
    <CellStyle ID=""localCell"" Color=""#EEEEEE"" />
    <TextContentStyle ID=""keep"" Color=""Green"" Direction=""LeftToRight"" Angle=""0"">
      <Alignment Horizontal=""Center"" Vertical=""Middle"" />
      <Font Family=""Arial"" Size=""12"" Style=""Italic"" />
    </TextContentStyle>
  </Styles>
  <Expressions />
</CellReportTemplate>";

			var path = Path.Combine(_directory, "template.tcr");
			File.WriteAllText(path, template, new UTF8Encoding(false));

			var result = new CellTemplate();
			result.Load(path);
			return result;
		}
	}
}
