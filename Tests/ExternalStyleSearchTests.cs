using System;
using System.IO;
using System.Text;
using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class ExternalStyleSearchTests
	{
		[TestMethod]
		public void Load_BareStyleName_FindsFileInParentDirectory()
		{
			var root = CreateDirectory();
			try
			{
				var styleName = "parent-style.xml";
				WriteStyle(Path.Combine(root, styleName), "from-parent");
				var templateDir = Path.Combine(root, "reports");
				Directory.CreateDirectory(templateDir);
				var templatePath = Path.Combine(templateDir, "template.tcr");
				WriteTemplate(templatePath, "file://" + styleName);

				var template = new CellTemplate();
				template.Load(templatePath);

				Assert.IsNotNull(template.Manager.GetStyleByID<CellStyle>("from-parent"));
			}
			finally
			{
				Directory.Delete(root, true);
			}
		}

		[TestMethod]
		public void Load_BareStyleName_PrefersFileNextToTemplate()
		{
			var root = CreateDirectory();
			try
			{
				var styleName = "shared-style.xml";
				WriteStyle(Path.Combine(root, styleName), "from-parent");
				var templateDir = Path.Combine(root, "reports");
				Directory.CreateDirectory(templateDir);
				WriteStyle(Path.Combine(templateDir, styleName), "from-template");
				var templatePath = Path.Combine(templateDir, "template.tcr");
				WriteTemplate(templatePath, "file://" + styleName);

				var template = new CellTemplate();
				template.Load(templatePath);

				Assert.IsNotNull(template.Manager.GetStyleByID<CellStyle>("from-template"));
				Assert.IsNull(template.Manager.GetStyleByID<CellStyle>("from-parent"));
			}
			finally
			{
				Directory.Delete(root, true);
			}
		}

		[TestMethod]
		public void Load_RelativeStylePath_DoesNotWalkParentDirectories()
		{
			var root = CreateDirectory();
			try
			{
				WriteStyle(Path.Combine(root, "shared-style.xml"), "from-parent");
				var templateDir = Path.Combine(root, "reports");
				Directory.CreateDirectory(templateDir);
				var templatePath = Path.Combine(templateDir, "template.tcr");
				WriteTemplate(templatePath, "file://nested/shared-style.xml");

				var template = new CellTemplate();
				template.Load(templatePath);

				Assert.IsNull(template.Manager.GetStyleByID<CellStyle>("from-parent"));
			}
			finally
			{
				Directory.Delete(root, true);
			}
		}

		[TestMethod]
		public void Load_BareStyleName_UsesApplicationDirectoryWhenTreeHasNoMatch()
		{
			var root = CreateDirectory();
			var styleName = "editor-style-" + Guid.NewGuid().ToString("N") + ".xml";
			var applicationStyle = Path.Combine(AppContext.BaseDirectory, styleName);
			try
			{
				WriteStyle(applicationStyle, "from-editor");
				var templatePath = Path.Combine(root, "template.tcr");
				WriteTemplate(templatePath, "file://" + styleName);

				var template = new CellTemplate();
				template.Load(templatePath);

				Assert.IsNotNull(template.Manager.GetStyleByID<CellStyle>("from-editor"));
			}
			finally
			{
				if (File.Exists(applicationStyle))
					File.Delete(applicationStyle);
				Directory.Delete(root, true);
			}
		}

		[TestMethod]
		public void Load_StreamTemplate_FallsBackToFileResolver()
		{
			var styleName = "host-style-" + Guid.NewGuid().ToString("N") + ".xml";
			Func<string, byte[]> resolver = name => name == styleName
				? Encoding.UTF8.GetBytes(StyleXml("from-host"))
				: null;

			FileResolver.OnResolve += resolver;
			try
			{
				var template = new CellTemplate();
				using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(TemplateXml("file://" + styleName))))
					template.Load(stream);

				Assert.IsNotNull(template.Manager.GetStyleByID<CellStyle>("from-host"));
			}
			finally
			{
				FileResolver.OnResolve -= resolver;
			}
		}

		[TestMethod]
		public void Load_StreamTemplate_FileResolverDoesNotOverrideFileNextToTemplate()
		{
			var root = CreateDirectory();
			var styleName = "host-style-" + Guid.NewGuid().ToString("N") + ".xml";
			Func<string, byte[]> resolver = name => name == styleName
				? Encoding.UTF8.GetBytes(StyleXml("from-host"))
				: null;

			FileResolver.OnResolve += resolver;
			try
			{
				WriteStyle(Path.Combine(root, styleName), "from-file");
				var templatePath = Path.Combine(root, "template.tcr");
				WriteTemplate(templatePath, "file://" + styleName);

				var template = new CellTemplate();
				template.Load(templatePath);

				Assert.IsNotNull(template.Manager.GetStyleByID<CellStyle>("from-file"));
				Assert.IsNull(template.Manager.GetStyleByID<CellStyle>("from-host"));
			}
			finally
			{
				FileResolver.OnResolve -= resolver;
				Directory.Delete(root, true);
			}
		}

		private static string CreateDirectory()
		{
			var path = Path.Combine(Path.GetTempPath(), "cellreport-styles-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(path);
			return path;
		}

		private static void WriteStyle(string path, string id)
		{
			File.WriteAllText(path, StyleXml(id), Encoding.UTF8);
		}

		private static void WriteTemplate(string path, string uri)
		{
			File.WriteAllText(path, TemplateXml(uri), Encoding.UTF8);
		}

		private static string StyleXml(string id)
		{
			return "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
				+ "<CellReportTemplate><Styles><CellStyle ID=\"" + id + "\" Color=\"Red\" /></Styles></CellReportTemplate>";
		}

		private static string TemplateXml(string uri)
		{
			return "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
				+ "<CellReportTemplate><Table RowsCount=\"1\" ColumnsCount=\"1\" />"
				+ "<Styles><ExternalStyle Uri=\"" + uri + "\" /></Styles></CellReportTemplate>";
		}
	}
}
