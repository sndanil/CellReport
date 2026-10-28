using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class TemplateLoadTests
	{
		[TestMethod]
		public void LoadFromXml_MissingRoot_ThrowsTemplateLoadException()
		{
			var template = new CellTemplate();

			var exception = Assert.ThrowsException<TemplateLoadException>(
				() => template.LoadFromXml("<Other />"));

			StringAssert.Contains(exception.Message, "CellReportTemplate");
		}

		[TestMethod]
		public void LoadFromXml_TableOverLimit_IsRejected()
		{
			var template = new CellTemplate
			{
				LoadOptions = new TemplateLoadOptions
				{
					MaxRows = 10,
					MaxColumns = 10,
					MaxCells = 100
				}
			};
			const string xml =
				"<CellReportTemplate><Properties/><Table RowsCount=\"11\" ColumnsCount=\"1\"/></CellReportTemplate>";

			Assert.ThrowsException<TemplateLoadException>(() => template.LoadFromXml(xml));
		}

		[TestMethod]
		public void LoadFromXml_XmlOverLimit_IsRejected()
		{
			var template = new CellTemplate
			{
				LoadOptions = new TemplateLoadOptions { MaxXmlBytes = 16 }
			};

			Assert.ThrowsException<TemplateLoadException>(
				() => template.LoadFromXml("<CellReportTemplate />"));
		}

		[TestMethod]
		public void LoadFromXml_Dtd_IsRejected()
		{
			var template = new CellTemplate();
			const string xml = "<!DOCTYPE x [<!ENTITY value \"expanded\">]>"
				+ "<CellReportTemplate><Properties/><Table RowsCount=\"1\" ColumnsCount=\"1\">"
				+ "<Row><Cell><TextContent><Text>&value;</Text></TextContent></Cell></Row>"
				+ "</Table></CellReportTemplate>";

			Assert.ThrowsException<TemplateLoadException>(() => template.LoadFromXml(xml));
		}
	}
}
