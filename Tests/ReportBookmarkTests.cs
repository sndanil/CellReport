using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CellReport;
using CellReport.Core;
using CellReport.Core.BookMarks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class ReportBookmarkTests
	{
		private const string TemplatePattern = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<CellReportTemplate>
  <Properties>
    <DocumentName>Bookmarks</DocumentName>
  </Properties>
  <DisplaySettings Scale=""1"" />
  <PrintSettings Landscape=""false"" AutoFitWidthPages=""1"" Scale=""1"">
    <Margins Top=""0"" Left=""0"" Right=""0"" Bottom=""0"" />
  </PrintSettings>
  <Sections>
    <Section ID=""header""><LeftTop Row=""0"" Column=""0"" /><RightBottom Row=""0"" Column=""0"" /></Section>
    <Section ID=""group""><LeftTop Row=""1"" Column=""0"" /><RightBottom Row=""1"" Column=""0"" /></Section>
    <Section ID=""row""><LeftTop Row=""2"" Column=""0"" /><RightBottom Row=""2"" Column=""0"" /></Section>
  </Sections>
  <BookMarks>{BOOKMARKS}</BookMarks>
  <Table RowsCount=""3"" ColumnsCount=""1"">
    <Row><Cell Index=""0""><TextContent><Param Name=""Title"" Target=""Text"" Value="""" /></TextContent></Cell></Row>
    <Row><Cell Index=""0""><TextContent><Param Name=""Title"" Target=""Text"" Value="""" /></TextContent></Cell></Row>
    <Row><Cell Index=""0""><TextContent><Param Name=""Title"" Target=""Text"" Value="""" /></TextContent></Cell></Row>
  </Table>
  <Styles />
  <Expressions />
</CellReportTemplate>";

		private const string NestedRules = @"
    <BookMark SectionID=""header"" TextParamName=""Title"">
      <BookMark SectionID=""group"" TextParamName=""Title"" VisibleParamName=""Title"">
        <BookMark SectionID=""row"" TextParamName=""Title"" VisibleParamName=""Title"" />
      </BookMark>
    </BookMark>";

		[TestMethod]
		public void FitDocument_BuildsBookmarksForLastSection()
		{
			var report = CreateReport(NestedRules);

			report.AddSection("header", new Item("ОТЧЁТ"));
			report.AddSection("group", new Item("Группа 1"));
			report.AddSection("row", new Item("Строка 1.1"));
			report.AddSection("row", new Item("Строка 1.2"));
			report.AddSection("group", new Item("Группа 2"));
			report.AddSection("row", new Item("Строка 2.1"));
			report.AddSection("row", new Item("Строка 2.2"));
			report.FitDocument();

			var marks = Flatten(report.Manager.GetBookMarks());
			CollectionAssert.AreEqual(
				new[] { "ОТЧЁТ", "Группа 1", "Строка 1.1", "Строка 1.2", "Группа 2", "Строка 2.1", "Строка 2.2" },
				marks.Select(item => item.Name).ToArray());

			var root = report.Manager.GetBookMarks().Single();
			Assert.AreEqual(2, root.Child.Count);
			Assert.AreEqual(2, root.Child[1].Child.Count);
			Assert.AreEqual(6, root.Child[1].Child[1].Location.Row);
		}

		[TestMethod]
		public void GetBookMarks_BuildsLastSectionLazily()
		{
			var report = CreateReport(NestedRules);

			report.AddSection("header", new Item("ОТЧЁТ"));
			report.AddSection("group", new Item("Группа"));
			report.AddSection("row", new Item("Последняя"));

			var marks = Flatten(report.Manager.GetBookMarks());

			Assert.AreEqual(3, marks.Count);
			Assert.AreEqual("Последняя", marks[2].Name);
		}

		[TestMethod]
		public void BuildBookMarks_IsIdempotent()
		{
			var report = CreateReport(NestedRules);
			report.AddSection("header", new Item("ОТЧЁТ"));
			report.AddSection("group", new Item("Группа"));
			report.AddSection("row", new Item("Строка"));

			report.BuildBookMarks();
			report.BuildBookMarks();
			report.FitDocument();

			Assert.AreEqual(3, Flatten(report.Manager.GetBookMarks()).Count);
		}

		[TestMethod]
		public void AutoSections_AddsBookmarkForEverySection()
		{
			var report = CreateReport(string.Empty, settings =>
			{
				settings.AutoSections = true;
				settings.TitleFormat = "{Section}: {Value}";
			});

			report.AddSection("header", new Item("ОТЧЁТ"));
			report.AddSection("group", new Item("Группа 1"));
			report.AddSection("row", new Item("Строка 1"));
			report.FitDocument();

			var marks = Flatten(report.Manager.GetBookMarks());
			CollectionAssert.AreEqual(
				new[] { "header: ОТЧЁТ", "group: Группа 1", "row: Строка 1" },
				marks.Select(item => item.Name).ToArray());
			Assert.AreEqual(2, marks[2].Location.Row);
		}

		[TestMethod]
		public void AutoSections_DoesNotDuplicateExplicitRules()
		{
			var report = CreateReport(NestedRules, settings => settings.AutoSections = true);

			report.AddSection("header", new Item("ОТЧЁТ"));
			report.AddSection("group", new Item("Группа"));
			report.AddSection("row", new Item("Строка"));
			report.FitDocument();

			Assert.AreEqual(3, Flatten(report.Manager.GetBookMarks()).Count);
		}

		[TestMethod]
		public void SectionLevels_BuildTreeByDepth()
		{
			var report = CreateReport(string.Empty, settings =>
			{
				settings.AutoSections = true;
				settings.SetLevel("header", 1);
				settings.SetLevel("group", 2);
				settings.SetLevel("row", 3);
			});

			report.AddSection("header", new Item("ОТЧЁТ"));
			report.AddSection("group", new Item("Группа 1"));
			report.AddSection("row", new Item("Строка 1"));
			report.AddSection("row", new Item("Строка 2"));
			report.AddSection("group", new Item("Группа 2"));
			report.AddSection("row", new Item("Строка 3"));
			report.FitDocument();

			var root = report.Manager.GetBookMarks().Single();
			Assert.AreEqual("ОТЧЁТ", root.Name);
			Assert.AreEqual(2, root.Child.Count);
			Assert.AreEqual(2, root.Child[0].Child.Count);
			Assert.AreEqual(1, root.Child[1].Child.Count);
			Assert.AreEqual("Строка 3", root.Child[1].Child[0].Name);
		}

		[TestMethod]
		public void MaxDepth_SkipsDeeperBookmarks()
		{
			var report = CreateReport(string.Empty, settings =>
			{
				settings.AutoSections = true;
				settings.MaxDepth = 2;
				settings.SetLevel("header", 1);
				settings.SetLevel("group", 2);
				settings.SetLevel("row", 3);
			});

			report.AddSection("header", new Item("Отчёт"));
			report.AddSection("group", new Item("Группа"));
			report.AddSection("row", new Item("Строка"));
			report.FitDocument();

			var marks = Flatten(report.Manager.GetBookMarks());
			CollectionAssert.AreEqual(new[] { "Отчёт", "Группа" }, marks.Select(item => item.Name).ToArray());
		}

		[TestMethod]
		public void SkipEmpty_IgnoresSectionsWithoutTitle()
		{
			var report = CreateReport(string.Empty, settings =>
			{
				settings.AutoSections = true;
				settings.SkipEmpty = true;
			});

			report.AddSection("header", new Item("ОТЧЁТ"));
			report.AddSection("group", new Item(string.Empty));
			report.FitDocument();

			var marks = Flatten(report.Manager.GetBookMarks());
			Assert.AreEqual(1, marks.Count);
			Assert.AreEqual("ОТЧЁТ", marks[0].Name);
		}

		[TestMethod]
		public void Duplicates_SkipAndMergeConsecutive()
		{
			var skip = CreateReport(string.Empty, settings =>
			{
				settings.AutoSections = true;
				settings.Duplicates = BookmarkDuplicateMode.Skip;
			});
			skip.AddSection("group", new Item("Одинаковый"));
			skip.AddSection("group", new Item("Одинаковый"));
			skip.FitDocument();
			Assert.AreEqual(1, Flatten(skip.Manager.GetBookMarks()).Count);

			var merge = CreateReport(string.Empty, settings =>
			{
				settings.AutoSections = true;
				settings.Duplicates = BookmarkDuplicateMode.MergeConsecutive;
			});
			merge.AddSection("group", new Item("Первый"));
			merge.AddSection("group", new Item("Второй"));
			merge.AddSection("group", new Item("Второй"));
			merge.FitDocument();
			CollectionAssert.AreEqual(
				new[] { "Первый", "Второй" },
				Flatten(merge.Manager.GetBookMarks()).Select(item => item.Name).ToArray());
		}

		[TestMethod]
		public void Enabled_StopsBookmarkBuilding()
		{
			var report = CreateReport(NestedRules, settings => settings.Enabled = false);
			report.AddSection("header", new Item("ОТЧЁТ"));
			report.AddSection("group", new Item("Группа"));
			report.FitDocument();

			Assert.AreEqual(0, Flatten(report.Manager.GetBookMarks()).Count);
		}

		[TestMethod]
		public void FlushAtEnd_DisabledKeepsLastSectionWithoutBookmark()
		{
			var report = CreateReport(NestedRules, settings => settings.FlushAtEnd = false);
			report.AddSection("header", new Item("ОТЧЁТ"));
			report.AddSection("group", new Item("Группа"));
			report.FitDocument();

			var marks = Flatten(report.Manager.GetBookMarks());
			Assert.AreEqual(1, marks.Count);
			Assert.AreEqual("ОТЧЁТ", marks[0].Name);
		}

		[TestMethod]
		public void BookmarkSettings_RoundTrip()
		{
			var settings = new BookmarkSettings
			{
				Enabled = false,
				AutoSections = true,
				TitleParam = "Заголовок",
				TitleFormat = "{Section}: {Value}",
				SkipEmpty = true,
				MaxDepth = 3,
				Duplicates = BookmarkDuplicateMode.MergeConsecutive,
				FlushAtEnd = false
			};
			settings.SetLevel("group", 2);

			var template = new CellTemplate();
			template.Load(new MemoryStream(Encoding.UTF8.GetBytes(TemplatePattern
				.Replace("{BOOKMARKS}", string.Empty))));
			template.Manager.BookmarkSettings = settings;

			using var stream = new MemoryStream();
			template.Save(stream, TemplateType.Xml);
			stream.Position = 0;

			var reloaded = new CellTemplate();
			reloaded.Load(stream);
			var copy = reloaded.Manager.BookmarkSettings;

			Assert.IsFalse(copy.Enabled);
			Assert.IsTrue(copy.AutoSections);
			Assert.AreEqual("Заголовок", copy.TitleParam);
			Assert.AreEqual("{Section}: {Value}", copy.TitleFormat);
			Assert.IsTrue(copy.SkipEmpty);
			Assert.AreEqual(3, copy.MaxDepth);
			Assert.AreEqual(BookmarkDuplicateMode.MergeConsecutive, copy.Duplicates);
			Assert.IsFalse(copy.FlushAtEnd);
			Assert.AreEqual(2, copy.GetLevel("group"));
		}

		[TestMethod]
		public void BookmarkSettings_DefaultsWhenNodeMissing()
		{
			var template = new CellTemplate();
			template.Load(new MemoryStream(Encoding.UTF8.GetBytes(TemplatePattern
				.Replace("{BOOKMARKS}", string.Empty))));

			var settings = template.Manager.BookmarkSettings;
			Assert.IsTrue(settings.Enabled);
			Assert.IsFalse(settings.AutoSections);
			Assert.AreEqual(BookmarkSettings.DefaultTitleParam, settings.TitleParam);
			Assert.AreEqual(BookmarkSettings.DefaultTitleFormat, settings.TitleFormat);
			Assert.IsTrue(settings.FlushAtEnd);
			Assert.IsFalse(settings.HasLevels);
		}

		private static ReportDocument CreateReport(string bookmarks, System.Action<BookmarkSettings> configure = null)
		{
			var template = new CellTemplate();
			template.Load(new MemoryStream(Encoding.UTF8.GetBytes(TemplatePattern
				.Replace("{BOOKMARKS}", bookmarks))));
			configure?.Invoke(template.Manager.BookmarkSettings);
			return new ReportDocument(template);
		}

		private static List<BookMark> Flatten(IEnumerable<BookMark> marks)
		{
			var result = new List<BookMark>();
			foreach (var mark in marks)
			{
				result.Add(mark);
				result.AddRange(Flatten(mark.Child));
			}

			return result;
		}

		private sealed record Item(string Title);
	}
}
