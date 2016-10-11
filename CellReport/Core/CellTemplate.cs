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
					(currCol.Marker == defCol.Marker) ? null : new XAttribute("Marker", currCol.Marker ?? string.Empty));
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
			var manager = new CellManager();
			foreach (var style in _manager.GetExtStyles())
			{
				var data = _manager.GetDataByUri(style.Uri);
				if (data != null)
				{
					var xDoc = XDocument.Load(new StreamReader(new MemoryStream(data)));
					var element = xDoc.Element("CellReportTemplate");
					ParseStyles(element, manager);
				}
			}
			result.Add(_manager.GetExtStyles().Select(s => new XElement("ExternalStyle", new XAttribute("Uri", s.Uri))));
			result.Add(_manager.GetStyles().Where(s => manager.GetStyles().Where(es => es.ID == s.ID && es.GetType() == s.GetType()).Count() == 0).Select(s => s.Serialize()));
			return result;
		}

		private XElement GetExpressions()
		{
			return new XElement("Expressions", _manager.GetExpressions().Select(e => e.Serialize()));
		}

		public void Load(string fileName)
		{
			using (var file = File.OpenRead(fileName))
				Load(file);
		}

		public void Load(Stream stream)
		{
			Load(stream, null);
		}

		public void Load(Stream stream, string docXml)
		{
			var type = TemplateType.Xml;
			var reader = new StreamReader(stream);
			char[] buffer = new char[2];
			reader.Read(buffer, 0, 2);
			var str = new string(buffer);
			type = str == "PK" ? TemplateType.Package : TemplateType.Xml;
			stream.Position = 0;

			var resources = new Dictionary<string, byte[]>();
			XDocument xDoc = null;
			if (type == TemplateType.Xml)
			{
				if (docXml == null)
					xDoc = XDocument.Load(new StreamReader(stream));
				else
					xDoc = XDocument.Parse(docXml);
			}
			else
			{
				using (Package package = ZipPackage.Open(stream, FileMode.Open, FileAccess.Read))
				{
					if (docXml == null)
					{
						Uri partUriDocument = PackUriHelper.CreatePartUri(new Uri("Content\\Document.xml",
							UriKind.Relative));
						xDoc = XDocument.Load(new StreamReader(package.GetPart(partUriDocument).GetStream()));
					}
					else
						xDoc = XDocument.Parse(docXml);
					foreach (var part in package.GetParts())
					{
						if (part.ContentType.ToUpper().StartsWith("IMAGE") && part.Uri.ToString().StartsWith("/Resources/"))
						{
							var name = part.Uri.ToString().Replace("/Resources/", string.Empty);
							var data = new byte[part.GetStream().Length];
							part.GetStream().Read(data, 0, data.Length);
							resources[name] = data;
						}
					}
				}
			}
			var templElement = xDoc.Element("CellReportTemplate");
			var properties = Properties.Parse(templElement.Element("Properties"));
			var tableElement = templElement.Element("Table");

			var rowsCount = Convert.ToInt32(tableElement.Attribute("RowsCount").Value);
			var columnsCount = Convert.ToInt32(tableElement.Attribute("ColumnsCount").Value);

			_manager = new CellManager(properties);
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
				var pageBreak = false;
				if (row.Attribute("PageBreak") != null)
					pageBreak = bool.Parse(row.Attribute("PageBreak").Value);
				if (pageBreak)
					_manager.AddPageBreakRow(rowIndex);
				if (row.Attribute("Height") != null)
				{
					int heightIndex = rowIndex;
					int span = 0;
					if (row.Attribute("Span") != null)
						span = Convert.ToInt32(row.Attribute("Span").Value);
					var height = UnitsConverter.ParseUnit(row.Attribute("Height").Value, GraphicsUnit.Millimeter);
					do
					{
						_manager.SetRowHeight(heightIndex, height);
						if (autoFit)
							_manager.SetAutoFitRow(heightIndex, true);
						heightIndex++;
						span--;
					} while (span >= 0);
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
				int span = 0;
				if (column.Attribute("Span") != null)
					span = Convert.ToInt32(column.Attribute("Span").Value);
				var hasWidth = (column.Attribute("Width") != null);
				var width = hasWidth ? UnitsConverter.ParseUnit(column.Attribute("Width").Value, GraphicsUnit.Millimeter) : 0;
				var hasMarker = (column.Attribute("Marker") != null);
				var marker = hasMarker ? column.Attribute("Marker").Value : null;
				do
				{
					if (hasWidth)
						_manager.SetColumnWidth(columnIndex, width);
					if (hasMarker)
						_manager.SetColumnMarker(columnIndex, string.IsNullOrEmpty(marker) ? null : marker);
					columnIndex++;
					span--;
				} while (span >= 0);
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

		public TemplateSection GetSectionByName(string name)
		{
			return _manager.GetSectionByName(name);
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
