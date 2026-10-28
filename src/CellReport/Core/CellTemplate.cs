using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using CellReport.Core.Contents;
using System.Globalization;
using CellReport.Core.Expressions;
using System.IO.Packaging;

namespace CellReport.Core
{
	public enum TemplateType
	{
		Xml,
		Package
	}

	[Serializable]
	public class CellTemplate
	{
		private CellManager _manager = null;
		private string _templateDirectory;
		private TemplateLoadOptions _loadOptions = new TemplateLoadOptions();
		private UriDataLoaderOptions _externalResourceOptions = new UriDataLoaderOptions();

		public TemplateLoadOptions LoadOptions
		{
			get { return _loadOptions; }
			set { _loadOptions = value ?? new TemplateLoadOptions(); }
		}

		public UriDataLoaderOptions ExternalResourceOptions
		{
			get { return _externalResourceOptions; }
			set { _externalResourceOptions = value ?? new UriDataLoaderOptions(); }
		}

		public string TemplateDirectory
		{
			get { return _templateDirectory; }
			set { _templateDirectory = value; }
		}

		public CellManager Manager
		{
			get { return _manager; }
		}

		public CellTemplate()
		{
		}

		public CellTemplate(CellManager manager)
		{
			_manager = manager;
		}

		public void Save(string fileName, TemplateType type)
		{
			using (var file = File.Create(fileName))
				Save(file, type);
		}

		public void Save(Stream stream, TemplateType type)
		{
			if (type == TemplateType.Package)
			{
				using (Package package = ZipPackage.Open(stream, FileMode.Create))
				{
					Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri("Content\\Document.xml", UriKind.Relative));
					PackagePart packagePartDocument = package.CreatePart(partUriDocument, System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);
					GetDocument().Save(new StreamWriter(packagePartDocument.GetStream()));
					package.CreateRelationship(packagePartDocument.Uri, TargetMode.Internal, "Document");
					foreach (var resName in _manager.GetResourceNames())
					{
						Uri partUri = PackUriHelper.CreatePartUri(new Uri("Resources\\" + resName, UriKind.Relative));
						var partPackage = package.CreatePart(partUri, System.Net.Mime.MediaTypeNames.Image.Jpeg, CompressionOption.NotCompressed);
						var data = _manager.GetResource(resName);
						partPackage.GetStream().Write(data, 0, data.Length);
						package.CreateRelationship(partPackage.Uri, TargetMode.Internal, "Resources");
					}
				}
			}
			else
				GetDocument().Save(new StreamWriter(stream));
		}

		private XDocument GetDocument()
		{
			XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
			doc.Add(new XElement("CellReportTemplate",
				Manager.Properties.Serialize(),
				Manager.ExportSettings.Serialize(),
				Manager.DisplaySettings.Serialize(),
				Manager.PrintSettings.Serialize(),
				Manager.BookmarkSettings.Serialize(),
				GetSections(),
				GetBookMarks(),
				GetTable(),
				GetStyles(),
				GetExpressions()
				));
			return doc;
		}

		private XElement GetSections()
		{
			var result = new XElement("Sections");
			foreach (var section in _manager.GetSections())
				result.Add(section.Serialize());
			return result;
		}

		private XElement GetBookMarks()
		{
			var result = new XElement("BookMarks");
			foreach (var item in _manager.GetBookMarks())
				result.Add(item.Serialize());
			return result;
		}

		private IEnumerable<XElement> GetColumns()
		{
			var lastIndex = -1;
			var defCol = Manager.Properties.DefaultColumn;
			for (int i = 0; i < Manager.ColumnsCount; i++)
			{
				var currCol = Manager.GetColumn(i);
				if (currCol.Equals(defCol))
					continue;
				yield return new XElement("Column",
					(lastIndex + 1) == i ? null : new XAttribute("Index", i),
					(currCol.Width == defCol.Width) ? null : new XAttribute("Width", currCol.Width.ToString(CultureInfo.InvariantCulture)),
					(currCol.MarkerForShow == defCol.MarkerForShow) ? null : new XAttribute("MarkerForShow", currCol.MarkerForShow ?? string.Empty),
					(currCol.MarkerForHide == defCol.MarkerForHide) ? null : new XAttribute("MarkerForHide", currCol.MarkerForHide ?? string.Empty));
				lastIndex = i;
			}
		}

		private bool AreCellsInRow(int row)
		{
			for (int i = 0; i < Manager.ActualColumnsCount; i++)
			{
				var cell = Manager.GetCell(row, i);
				if (cell != null && cell.Location.Row == row)
					return true;
			}

			return false;
		}

		private IEnumerable<XElement> GetCells(int row)
		{
			int lastCellIndex = -1;
			for (int i = 0; i < Manager.ActualColumnsCount; i++)
			{
				var cell = Manager.GetCell(row, i);
				if (cell != null && cell.Location.Row == row && cell.Location.Column == i)
				{
					var cellElement = cell.Serialize((lastCellIndex + 1) == i ? -1 : i);
					lastCellIndex = i;
					yield return cellElement;
				}
			}
		}

		private IEnumerable<XElement> GetRows()
		{
			var lastRowIndex = -1;
			for (int i = 0; i < Manager.RowsCount; i++)
			{
				XElement row = null;
				if (Manager.GetRowHeight(i) != Manager.Properties.DefaultRowHeight)
				{
					row = new XElement("Row",
						new XAttribute("Height", Manager.GetRowHeight(i).ToString(CultureInfo.InvariantCulture)));
				}

				if (Manager.IsAutoFitRow(i))
				{
					row = row ?? new XElement("Row");
					row.Add(new XAttribute("AutoFitHeight", Manager.IsAutoFitRow(i)));
				}

				if (AreCellsInRow(i))
				{
					row = row ?? new XElement("Row");
					row.Add(GetCells(i));
				}

				if (Manager.IsPageBreakRow(i))
				{
					row = row ?? new XElement("Row");
					row.Add(new XAttribute("PageBreak", true));
				}

				if ((i - 1) != lastRowIndex && row != null)
					row.Add(new XAttribute("Index", i));

				if (row != null)
				{
					lastRowIndex = i;
					yield return row;
				}
			}
		}

		private XElement GetTable()
		{
			return new XElement("Table",
				new XAttribute("RowsCount", Manager.RowsCount.ToString(CultureInfo.InvariantCulture)),
				new XAttribute("ColumnsCount", Manager.ColumnsCount.ToString(CultureInfo.InvariantCulture)),
				GetColumns(),
				GetRows()
				);
		}

		private XElement GetStyles()
		{
			var result = new XElement("Styles");
			result.Add(_manager.GetExtStyles().Select(s => new XElement("ExternalStyle", new XAttribute("Uri", s.Uri))));
			// во внешних файлах стили не дублируются в шаблоне
			result.Add(_manager.GetTemplateStyles().Select(s => s.Serialize()));
			return result;
		}

		private XElement GetExpressions()
		{
			return new XElement("Expressions", _manager.GetExpressions().Select(e => e.Serialize()));
		}

		public void Load(string fileName)
		{
			_templateDirectory = Path.GetDirectoryName(Path.GetFullPath(fileName));
			using (var file = File.OpenRead(fileName))
				Load(file);
		}

		public void Load(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			try
			{
				LoadFromStream(stream);
			}
			catch (TemplateLoadException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new TemplateLoadException("Failed to load the report template.", ex);
			}
		}

		public void LoadFromXml(string xml)
		{
			if (xml == null)
				throw new ArgumentNullException(nameof(xml));
			if (Encoding.UTF8.GetByteCount(xml) > _loadOptions.MaxXmlBytes)
				throw new TemplateLoadException("The template XML exceeds the configured size limit.");
			try
			{
				XDocument xDoc;
				using (var reader = new StringReader(xml))
					xDoc = LoadXml(reader);
				LoadFromXDoc(xDoc, new Dictionary<string, byte[]>());
			}
			catch (TemplateLoadException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new TemplateLoadException("Failed to parse the report template XML.", ex);
			}
		}


		private void LoadFromStream(Stream stream)
		{
			var templateData = ReadLimited(stream, _loadOptions.MaxTemplateBytes, "template");
			var type = templateData.Length >= 2 && templateData[0] == (byte)'P' && templateData[1] == (byte)'K'
				? TemplateType.Package
				: TemplateType.Xml;

			var resources = new Dictionary<string, byte[]>();
			XDocument xDoc = null;
			if (type == TemplateType.Xml)
			{
				if (templateData.LongLength > _loadOptions.MaxXmlBytes)
					throw new TemplateLoadException("The template XML exceeds the configured size limit.");
				using (var xmlStream = new MemoryStream(templateData, false))
					xDoc = LoadXml(xmlStream);
			}
			else
			{
				using (var packageStream = new MemoryStream(templateData, false))
				using (Package package = Package.Open(packageStream, FileMode.Open, FileAccess.Read))
				{
					Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri("Content\\Document.xml", UriKind.Relative));
					if (!package.PartExists(partUriDocument))
						throw new TemplateLoadException("The template package does not contain Content/Document.xml.");
					using (var documentStream = package.GetPart(partUriDocument).GetStream())
					using (var limitedDocumentStream = new MemoryStream(
						ReadLimited(documentStream, _loadOptions.MaxXmlBytes, "template XML"), false))
						xDoc = LoadXml(limitedDocumentStream);

					long totalResourceBytes = 0;
					foreach (var part in package.GetParts())
					{
						if (part.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase)
							&& part.Uri.ToString().StartsWith("/Resources/", StringComparison.OrdinalIgnoreCase))
						{
							var name = part.Uri.ToString().Substring("/Resources/".Length);
							byte[] data;
							using (var resourceStream = part.GetStream())
								data = ReadLimited(resourceStream, _loadOptions.MaxResourceBytes, "template resource");
							totalResourceBytes += data.LongLength;
							if (totalResourceBytes > _loadOptions.MaxTotalResourceBytes)
								throw new TemplateLoadException("Template resources exceed the configured total size limit.");
							resources[name] = data;
						}
					}
				}
			}
			LoadFromXDoc(xDoc, resources);
		}

		private void LoadFromXDoc(XDocument xDoc, Dictionary<string, byte[]> resources)
		{
			var templElement = xDoc.Element("CellReportTemplate");
			if (templElement == null)
				throw new TemplateLoadException("The root CellReportTemplate element is missing.");
			var properties = Properties.Parse(templElement.Element("Properties"));
			var tableElement = templElement.Element("Table");
			if (tableElement == null)
				throw new TemplateLoadException("The Table element is missing.");

			var rowsAttribute = tableElement.Attribute("RowsCount");
			var columnsAttribute = tableElement.Attribute("ColumnsCount");
			if (rowsAttribute == null || columnsAttribute == null)
				throw new TemplateLoadException("Table must define RowsCount and ColumnsCount.");
			int rowsCount;
			int columnsCount;
			if (!int.TryParse(rowsAttribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out rowsCount)
				|| !int.TryParse(columnsAttribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out columnsCount)
				|| rowsCount < 0 || columnsCount < 0)
				throw new TemplateLoadException("RowsCount and ColumnsCount must be non-negative integers.");
			if (rowsCount > _loadOptions.MaxRows || columnsCount > _loadOptions.MaxColumns
				|| (long)rowsCount * columnsCount > _loadOptions.MaxCells)
				throw new TemplateLoadException("The template table exceeds the configured dimensions.");

			_manager = new CellManager(properties);
			_manager.TemplateDirectory = _templateDirectory;
			_manager.UriDataLoaderOptions = _externalResourceOptions.Clone();
			_manager.RowsCount = rowsCount;
			_manager.ColumnsCount = columnsCount;
			foreach (KeyValuePair<string, byte[]> res in resources)
				_manager.AddResource(res.Key, res.Value);
			ParseColumns(tableElement);
			ParseStyles(templElement, _manager);
			ParseExpressions(templElement);

			ParseRows(tableElement);
			ParseSections(templElement);
			ParseBookMarks(templElement);
			_manager.PrintSettings = PrintSettings.Parse(_manager, templElement.Element("PrintSettings"));
			_manager.ExportSettings = ExportSettings.Parse(templElement.Element("ExportSettings"));
			_manager.DisplaySettings = DisplaySettings.Parse(templElement.Element("DisplaySettings"));
			_manager.BookmarkSettings = BookmarkSettings.Parse(templElement.Element("BookmarkSettings"));

		}

		private static byte[] ReadLimited(Stream stream, long limit, string description)
		{
			if (limit < 0)
				throw new ArgumentOutOfRangeException(nameof(limit));
			using (var output = new MemoryStream())
			{
				var buffer = new byte[81920];
				long total = 0;
				int read;
				while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					total += read;
					if (total > limit)
						throw new TemplateLoadException("The " + description + " exceeds the configured size limit.");
					output.Write(buffer, 0, read);
				}
				return output.ToArray();
			}
		}

		private XDocument LoadXml(Stream stream)
		{
			using (var reader = XmlReader.Create(stream, CreateXmlReaderSettings()))
				return XDocument.Load(reader);
		}

		private XDocument LoadXml(TextReader textReader)
		{
			using (var reader = XmlReader.Create(textReader, CreateXmlReaderSettings()))
				return XDocument.Load(reader);
		}

		private XmlReaderSettings CreateXmlReaderSettings()
		{
			return new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				MaxCharactersInDocument = _loadOptions.MaxXmlBytes
			};
		}

		private void ParseRows(XElement tableElement)
		{
			var rowIndex = 0;
			foreach (var row in tableElement.Elements("Row"))
			{
				if (row.Attribute("Index") != null)
					rowIndex = Convert.ToInt32(row.Attribute("Index").Value);
				var autoFit = false;
				if (row.Attribute("AutoFitHeight") != null)
					autoFit = bool.Parse(row.Attribute("AutoFitHeight").Value);
				if (autoFit)
					_manager.SetAutoFitRow(rowIndex, true);
				var pageBreak = false;
				if (row.Attribute("PageBreak") != null)
					pageBreak = bool.Parse(row.Attribute("PageBreak").Value);
				if (pageBreak)
					_manager.AddPageBreakRow(rowIndex);
				if (row.Attribute("Height") != null)
				{
					var height = UnitsConverter.ParseUnit(row.Attribute("Height").Value, GraphicsUnit.Millimeter);
					_manager.SetRowHeight(rowIndex, height);
				}

				var colIndex = 0;
				foreach (var cellElement in row.Elements("Cell"))
				{
					var cell = Cell.Parse(_manager, cellElement, rowIndex, ref colIndex);
					cell.Content = BaseContent.Parse(_manager, cellElement);
				}
				rowIndex++;
			}
		}

		private void ParseSections(XElement templElement)
		{
			if (templElement.Element("Sections") != null)
				foreach (var sectionElement in templElement.Element("Sections").Elements("Section"))
					_manager.AddSection(TemplateSection.Parse(sectionElement));
		}

		private void ParseBookMarks(XElement templElement)
		{
			if (templElement.Element("BookMarks") != null)
				foreach (var element in templElement.Element("BookMarks").Elements("BookMark"))
					_manager.AddBookMark(BookMarks.BookMark.Parse(element));
		}

		private void ParseColumns(XElement tableElement)
		{
			int columnIndex = 0;
			foreach (var column in tableElement.Elements("Column"))
			{
				if (column.Attribute("Index") != null)
					columnIndex = Convert.ToInt32(column.Attribute("Index").Value);
				var hasWidth = (column.Attribute("Width") != null);
				var width = hasWidth ? UnitsConverter.ParseUnit(column.Attribute("Width").Value, GraphicsUnit.Millimeter) : 0;
				if (hasWidth)
					_manager.SetColumnWidth(columnIndex, width);

				var hasMarker = (column.Attribute("MarkerForShow") != null);
				var marker = hasMarker ? column.Attribute("MarkerForShow").Value : null;
				if (hasMarker)
					_manager.SetColumnMarkerForShow(columnIndex, string.IsNullOrEmpty(marker) ? null : marker);

				hasMarker = (column.Attribute("MarkerForHide") != null);
				marker = hasMarker ? column.Attribute("MarkerForHide").Value : null;
				if (hasMarker)
					_manager.SetColumnMarkerForHide(columnIndex, string.IsNullOrEmpty(marker) ? null : marker);

				columnIndex++;
			}
		}

		private void ParseExpressions(XElement templElement)
		{
			if (templElement.Element("Expressions") != null)
			{
				foreach (var exprElement in templElement.Element("Expressions").Elements())
				{
					var expr = BaseExpression.Parse(exprElement);
					if (expr != null)
						_manager.AddExpression(expr);
				}
			}
		}

		internal void ParseStyles(XElement templElement, CellManager manager)
		{
			if (templElement.Element("Styles") != null)
			{
				foreach (var styleElement in templElement.Element("Styles").Elements())
					manager.AddStyle(BaseStyle.Parse(styleElement));

				foreach (var styleElement in templElement.Element("Styles").Elements("ExternalStyle"))
				{
					if (styleElement.Attribute("Uri") != null)
					{
						var uri = styleElement.Attribute("Uri").Value;
						manager.AddExternalStyle(new ExternalStyle(uri));
					}
				}
			}
		}

		public TemplateSection GetSectionByName(string name, string objectName)
		{
			return _manager.GetSection(name, objectName);
		}

		public int GetMaxColumnsCountInSections()
		{
			int max = 0;
			foreach (var item in _manager.GetSections())
				max = Math.Max(max, item.Width);
			return max;
		}

		public int GetMaxRowsCountInSections()
		{
			int max = 0;
			foreach (var item in _manager.GetSections())
				max = Math.Max(max, item.Height);
			return max;
		}
	}
}
