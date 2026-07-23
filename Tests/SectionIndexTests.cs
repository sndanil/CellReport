using System.Collections.Generic;
using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class SectionIndexTests
	{
		[TestMethod]
		public void Find_ReturnsSectionByObjectNameToken()
		{
			var index = new Dictionary<SectionLookupKey, TemplateSection>();
			var section = new TemplateSection { Name = "Section", ObjectName = "Stores,Products,Partners" };
			SectionIndex.Register(index, section);

			Assert.AreSame(section, SectionIndex.Find(index, "Section", "Products", false));
		}

		[TestMethod]
		public void Find_FallsBackToDefaultSectionWithoutObjectName()
		{
			var index = new Dictionary<SectionLookupKey, TemplateSection>();
			var defaultSection = new TemplateSection { Name = "Row" };
			var typedSection = new TemplateSection { Name = "Row", ObjectName = "Detail" };
			SectionIndex.Register(index, defaultSection);
			SectionIndex.Register(index, typedSection);

			Assert.AreSame(defaultSection, SectionIndex.Find(index, "Row", null, false));
			Assert.AreSame(typedSection, SectionIndex.Find(index, "Row", "Detail", false));
		}

		[TestMethod]
		public void Find_StrictMode_SkipsDefaultSectionFallback()
		{
			var index = new Dictionary<SectionLookupKey, TemplateSection>();
			var defaultSection = new TemplateSection { Name = "Row" };
			SectionIndex.Register(index, defaultSection);

			Assert.IsNull(SectionIndex.Find(index, "Row", "Missing", true));
		}

		[TestMethod]
		public void Unregister_RemovesAllKeysForSection()
		{
			var index = new Dictionary<SectionLookupKey, TemplateSection>();
			var section = new TemplateSection { Name = "Section", ObjectName = "A,B" };
			SectionIndex.Register(index, section);

			SectionIndex.Unregister(index, section);

			Assert.AreEqual(0, index.Count);
		}
	}
}
