using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Packaging;
using System.Xml.Linq;
using CellReport.Core;
using System.Globalization;
using System.IO;
using CellReport.Core.Contents;

namespace CellReport.Export.Excel
{
	internal class Worksheet
	{
		private CellManager _manager = null;
		private ExcelExporter _exporter;

		private char[] _letters = new char[] { 'A', 'B', 'C' , 'D', 'E', 'F',
						'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q',
						'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
		private Styles _styles = new Styles();
		private Drawing _drawing = new Drawing();

		public Worksheet(CellManager manager, ExcelExporter exporter)
		{
			_manager = manager;
			_exporter = exporter;
		}

		internal void Save(Package package, int id, PackagePart workbookPart, XElement sheetsElement)
		{
			var uri = new Uri("/xl/worksheets/sheet" + id + ".xml", UriKind.Relative);
			var part = package.CreatePart(uri,
					@"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml",
					CompressionOption.Maximum);
			var rel = workbookPart.CreateRelationship(uri, TargetMode.Internal,
					"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet");
			var name = _manager.Properties.DocumentName;
			if (string.IsNullOrEmpty(name))
				name = "Sheet" + id;
			sheetsElement.Add(
					new XElement(ExcelExporter.schemaMain + "sheet",
							new XAttribute("name", name.Length > 31 ? name.Substring(0, 31) : name),
							new XAttribute("sheetId", id),
							new XAttribute(ExcelExporter.schemaRelationships + "id", rel.Id))
					);
			var cols = new XElement(ExcelExporter.schemaMain + "cols");
			var colWidths = new List<float>();
			var actualColIndex = 0;
			for (int colIndex = 0; colIndex < _manager.ColumnsCount; colIndex++)
			{
				// Единица измерения ширины колонки в Excel равна ширине символа "0" для шрифта, который используется
				// в дефолтном стиле ячейки книги Excel.
				// 5.25 - ширина символа "0" для шрифта Calibry 11 (в points).
				var width = UnitsConverter.ConvertValue(_manager.GetColumnWidth(colIndex),
						GraphicsUnit.Millimeter, GraphicsUnit.Point, 0) / 5.25f;
				colWidths.Add(width);
				if (width == 0)
					continue;
				cols.Add(new XElement(ExcelExporter.schemaMain + "col",
					new XAttribute("min", (actualColIndex + 1)),
					new XAttribute("max", (actualColIndex + 1)),
										new XAttribute("width", width.ToString(CultureInfo.InvariantCulture)),
										new XAttribute("customWidth", 1))
										);
				actualColIndex++;
			}
			var data = new XElement(ExcelExporter.schemaMain + "sheetData");
			var mergeCells = new XElement(ExcelExporter.schemaMain + "mergeCells");
			int mergeCount = 0;
			var exportedCells = new HashSet<Cell>();
			for (int rowIndex = 0; rowIndex < _manager.RowsCount; rowIndex++)
			{
				var height = UnitsConverter.ConvertValue(_manager.GetRowHeight(rowIndex),
						GraphicsUnit.Millimeter, GraphicsUnit.Point, 0);
				var row = new XElement(ExcelExporter.schemaMain + "row");
				row.Add(new XAttribute("r", (rowIndex + 1)),
						new XAttribute("ht", height.ToString(CultureInfo.InvariantCulture)),
						new XAttribute("customHeight", 1)
						);
				actualColIndex = -1;
				for (int colIndex = 0; colIndex < _manager.ActualColumnsCount; colIndex++)
				{
					if (colWidths[colIndex] == 0)
						continue;

					actualColIndex++;
					var cell = _manager.GetCell(rowIndex, colIndex);
					if (cell == null)
						continue;

					var c = new XElement(ExcelExporter.schemaMain + "c",
						new XAttribute("r", GetColumnName(actualColIndex) + (rowIndex + 1)));
					row.Add(c);
					if (exportedCells.Contains(cell))
					{
						c.Add(new XAttribute("s", _styles.AddStyles(cell.Style)));
						continue;
					}

					var loc = new CellLocation() { Row = rowIndex, Column = actualColIndex };
					var span = new CellSpan()
					{
						RowSpan = cell.Span.RowSpan,
						ColSpan = colWidths.Where((x, i) =>
							(i >= colIndex + 1) && (i <= colIndex + cell.Span.ColSpan) && (x > 0)).Count()
					};
					var styleContent = WriteContent(loc, span, cell.Size, cell.Content, c);
					string style = null;
					if (styleContent != null)
						style = _styles.AddStyles(cell.Style, styleContent);
					else
						style = _styles.AddStyles(cell.Style);
					c.Add(new XAttribute("s", style));
					if (span.ColSpan != 0 || span.RowSpan != 0)
					{
						var mergeRef = GetColumnName(loc.Column) + (rowIndex + 1);
						mergeRef += ":" + GetColumnName(loc.Column + span.ColSpan) + (rowIndex + span.RowSpan + 1);
						mergeCount++;
						mergeCells.Add(new XElement(ExcelExporter.schemaMain + "mergeCell",
								new XAttribute("ref", mergeRef))
						);
					}
					exportedCells.Add(cell);
				}
				data.Add(row);
			}
			mergeCells.Add(new XAttribute("count", mergeCount));
			if (mergeCount == 0)
				mergeCells = null;
			var pageMargins = new XElement(ExcelExporter.schemaMain + "pageMargins",
					new XAttribute("left", UnitsConverter.ConvertValue(_manager.PrintSettings.Margins.Left, GraphicsUnit.Millimeter, GraphicsUnit.Inch, 0).ToString(CultureInfo.InvariantCulture)),
					new XAttribute("right", UnitsConverter.ConvertValue(_manager.PrintSettings.Margins.Right, GraphicsUnit.Millimeter, GraphicsUnit.Inch, 0).ToString(CultureInfo.InvariantCulture)),
					new XAttribute("top", UnitsConverter.ConvertValue(_manager.PrintSettings.Margins.Top, GraphicsUnit.Millimeter, GraphicsUnit.Inch, 0).ToString(CultureInfo.InvariantCulture)),
					new XAttribute("bottom", UnitsConverter.ConvertValue(_manager.PrintSettings.Margins.Bottom, GraphicsUnit.Millimeter, GraphicsUnit.Inch, 0).ToString(CultureInfo.InvariantCulture)),
					new XAttribute("header", "0"),
					new XAttribute("footer", "0")
					);
			var pageSetup = new XElement(ExcelExporter.schemaMain + "pageSetup",
					new XAttribute("paperSize", "9"), // A4
					_manager.PrintSettings.Landscape ? new XAttribute("orientation", "landscape") : null);

			var root = new XElement(ExcelExporter.schemaMain + "worksheet",
					new XAttribute(XNamespace.Xmlns + "r", ExcelExporter.schemaRelationships),
					cols,
					data,
					mergeCells,
					pageMargins,
					pageSetup
					);
			_drawing.Save(package, id, part, root);

			root.Save(new StreamWriter(part.GetStream()));
			_styles.Save(package, workbookPart);
		}

		private string GetColumnName(int column)
		{
			var result = _letters[column % _letters.Length].ToString();
			if (column / _letters.Length > 0)
			{
				if ((column / _letters.Length - 1) < _letters.Length)
					result = _letters[column / _letters.Length - 1] + result;
				else
				{
					var div = column / _letters.Length - 1;
					result = _letters[div / _letters.Length - 1].ToString() + _letters[div % _letters.Length].ToString() + result;
				}
			}

			return result;
		}

		private BaseStyle WriteContent(CellLocation loc, CellSpan span, Size size, BaseContent content, XElement contentElement)
		{
			if (content == null)
				return null;

			if (content is CollectionContent)
			{
				foreach (var item in ((CollectionContent)content).GetContents())
				{
					var result = WriteContent(loc, span, size, item, contentElement);
					if (result != null)
						return result;
				}
			}
			else if (content is TextContent)
			{
				var text = ((TextContent)content).GetTextForRender();
				if (text.Any(c => !System.Xml.XmlConvert.IsXmlChar(c)))
					text = new string(text.Where(System.Xml.XmlConvert.IsXmlChar).ToArray());

				contentElement.Add(new XAttribute("t", "inlineStr"), 
					new XElement(ExcelExporter.schemaMain + "is", new XElement(ExcelExporter.schemaMain + "t", text))
					);
				return content.GetActualStyle();
			}
			else if (content is DecimalContent)
			{
				var decContent = (DecimalContent)content;
				contentElement.Add(new XElement(ExcelExporter.schemaMain + "v",
								decContent.GetValue().ToString(CultureInfo.InvariantCulture))
						);
				// Excel в качестве цвета отрицательных чисел понимает только набор предопределенных цветов
				// (56 штук с названиями Black, Red, Green, Color 34, Color 48, ...).
				// Полный набор цветов: http://dmcritchie.mvps.org/excel/colors.htm
				// При экспорте нужно использовать первые 8 цветов из набора, а остальные игнорировать.
				// Имя цвета нужно подставить в формат числа перед форматом отрицательного числа.
				var decStyle = (DecimalContentStyle)decContent.GetActualStyle();
				var format = decStyle.Format;
				if (!string.IsNullOrEmpty(format) && (decStyle.Color != decStyle.NegativeColor))
				{
					var hexColor = "#" + Styles.ConvertColor(decStyle.NegativeColor);
					var colorName = Styles.FindColorName(hexColor);
					if (!string.IsNullOrEmpty(colorName))
					{
						// формат числа состоит из 1-3 частей, разделенных ";".
						// 1-я часть формат положительного числа, 2-я - отрицательного, 3-я - нуля.
						var parts = format.Split(';').ToList();
						if (parts.Count == 1)
							parts.Add("-" + parts[0]);
						parts[1] = string.Format("[{0}]{1}", colorName, parts[1]);
						format = string.Join(";", parts.ToArray());
					}
				}
				return new FormatContentStyle(decStyle.HAlignment, decStyle.VAlignment, decStyle.Font,
					decStyle.Color, decStyle.Angle, format, decStyle.Culture, decStyle.Direction);
			}
			else if (content is DateTimeContent)
			{
				contentElement.Add(new XElement(ExcelExporter.schemaMain + "v",
								((DateTimeContent)content).GetValue().ToOADate().ToString(CultureInfo.InvariantCulture))
						);
				return content.GetActualStyle();
			}
			else if (content is ImageContent)
			{
				var imgContent = (ImageContent)content;
				var imgData = imgContent.GetImageData();
				if (imgData.Length > 0)
					_drawing.AddImage(loc, span, imgData);
				return imgContent.GetStyle();
			}
			else if (content is BarCodeContent)
			{
				if (_exporter.Renderer == null)
					return null;

				var barCode = (BarCodeContent)content;
				var width = (int)UnitsConverter.ConvertValue(size.Width,
					GraphicsUnit.Millimeter, ExcelExporter.ImageUnit, ExcelExporter.ImageDpi);
				var height = (int)UnitsConverter.ConvertValue(size.Height,
					GraphicsUnit.Millimeter, ExcelExporter.ImageUnit, ExcelExporter.ImageDpi);
				Action<object> renderAction = (context) =>
				{
					var rect = new Rectangle(0, 0, width, height);
					_exporter.Renderer.RenderBarCode(context, rect, barCode,
						"Black", "Arial", 10f, FontStyle.Regular, barCode.Orientation);
				};
				var imgData = _exporter.Renderer.CreateImage(width, height, renderAction);
				if ((imgData != null) && (imgData.Length > 0))
					_drawing.AddImage(loc, span, imgData);
				return barCode.GetStyle();
			}

			return null;
		}
	}
}
