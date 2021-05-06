using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Packaging;
using System.Xml.Linq;
using System.IO;
using CellReport.Core;
using CellReport.Core.Contents;
using System.Globalization;

namespace CellReport.Export.Excel
{
	internal class Styles
	{
		class StylesChain
		{
			private readonly BaseStyle[] _styles;

			public StylesChain(params BaseStyle[] styles)
			{
				_styles = styles;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(this, obj))
					return true;

				var other = obj as StylesChain;
				if (other != null)
				{
					if (other._styles.Length == _styles.Length)
					{
						foreach (var style in _styles)
						{
							var wasEquals = false;
							foreach (var otherStyle in other._styles)
								wasEquals = wasEquals || style.Equals(otherStyle);
							if (!wasEquals)
								return false;
						}
						return true;
					}
				}

				return false;
			}

			public override int GetHashCode()
			{
				int result = 0;
				foreach (var item in _styles)
					result += item.GetHashCode();
				return result;
			}

			public IEnumerable<BaseStyle> GetStyles()
			{
				return _styles;
			}
		}

		class ExcelFont
		{
			public FontInfo Font { get; set; }
			public string Color { get; set; }

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(this, obj))
					return true;

				var other = obj as ExcelFont;
				if (other != null)
					return other.Font.Equals(Font) && other.Color.Equals(Color);

				return false;
			}

			public override int GetHashCode()
			{
				return Font.GetHashCode() + Color.GetHashCode();
			}
		}

		private static readonly Dictionary<string, string> ExcelColors = new Dictionary<string, string>
		{
			{"#FF000000", "Black"}, {"#FFFFFFFF", "White"}, {"#FFFF0000", "Red"}, {"#FF00FF00", "Green"},
			{"#FF0000FF", "Blue"}, {"#FFFFFF00", "Yellow"}, {"#FFFF00FF", "Magenta"}, {"#FF00FFFF", "Cyan"}
		};

		public static string ConvertColor(string color)
		{
			byte a; byte r; byte g; byte b;
			BaseStyle.ParseColor(color, out a, out r, out g, out b);
			return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", a, r, g, b);
		}

		public static string FindColorName(string color)
		{
			color = color.ToUpper();
			return ExcelColors.ContainsKey(color) ? ExcelColors[color] : null;
		}

		// Создание коллекции стилей с пустым первым стилем, который будет использоваться
		// в качестве стиля по умолчанию для всех ячеек без контента.
		readonly List<StylesChain> _chains = new List<StylesChain> { new StylesChain() };

		public string AddStyles(params BaseStyle[] styles)
		{
			var chain = new StylesChain(styles);
			var index = _chains.IndexOf(chain);
			if (index < 0)
			{
				index = _chains.Count;
				_chains.Add(chain);
			}

			return index.ToString(CultureInfo.InvariantCulture);
		}

		private bool HasFormat(FormatContentStyle formatStyle)
		{
			if (formatStyle as DecimalContentStyle != null)
				return formatStyle.Format.Contains('.');
			return !string.IsNullOrEmpty(formatStyle.Format);
		}

		internal void Save(Package package, PackagePart workbookPart)
		{
			var uri = new Uri("/xl/styles.xml", UriKind.Relative);
			var part = package.CreatePart(uri, @"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml", CompressionOption.Maximum);
			workbookPart.CreateRelationship(uri, TargetMode.Internal, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles");
			var fontsList = new List<ExcelFont>();
			var fillsList = new List<string>();
			var bordersList = new List<Borders>();
			var numFormatsList = new List<string>();
			foreach (var chain in _chains)
			{
				foreach (var style in chain.GetStyles())
				{
					var textStyle = style as TextContentStyle;
					if (textStyle != null)
					{
						var excelFont = new ExcelFont { Font = textStyle.Font, Color = textStyle.Color };
						if (fontsList.IndexOf(excelFont) < 0)
							fontsList.Add(excelFont);
					}
					var cellStyle = style as CellStyle;
					if (cellStyle != null && !cellStyle.Borders.IsEmpty() && bordersList.IndexOf(cellStyle.Borders) < 0)
						bordersList.Add(cellStyle.Borders);
					if (cellStyle != null && cellStyle.HasColor && fillsList.IndexOf(cellStyle.Color) < 0)
						fillsList.Add(cellStyle.Color);
					var formatStyle = style as FormatContentStyle;
					if (formatStyle != null && HasFormat(formatStyle) && numFormatsList.IndexOf(formatStyle.Format) < 0)
						numFormatsList.Add(formatStyle.Format);
				}
			}
			var numFmts = new XElement(ExcelExporter.schemaMain + "numFmts", new XAttribute("count", numFormatsList.Count));
			int index = 1;
			foreach (var format in numFormatsList)
			{
				numFmts.Add(
						new XElement(ExcelExporter.schemaMain + "numFmt",
								new XAttribute("numFmtId", index), new XAttribute("formatCode", format.Replace("-", "\\-")))
						);
				index++;
			}

			var fonts = new XElement(ExcelExporter.schemaMain + "fonts",
					new XAttribute("count", fontsList.Count + 1),
					new XElement(ExcelExporter.schemaMain + "font",
									new XElement(ExcelExporter.schemaMain + "sz", new XAttribute("val", 11)),
									new XElement(ExcelExporter.schemaMain + "name", new XAttribute("val", "Calibri")))
					);
			foreach (var excelFont in fontsList)
			{
				var font = new XElement(ExcelExporter.schemaMain + "font",
								new XElement(ExcelExporter.schemaMain + "sz", new XAttribute("val", excelFont.Font.Size.ToString(CultureInfo.InvariantCulture))),
								new XElement(ExcelExporter.schemaMain + "name", new XAttribute("val", excelFont.Font.Family))
								);
				if ((excelFont.Font.Style & FontStyle.Bold) == FontStyle.Bold)
					font.Add(new XElement(ExcelExporter.schemaMain + "b"));
				if ((excelFont.Font.Style & FontStyle.Italic) == FontStyle.Italic)
					font.Add(new XElement(ExcelExporter.schemaMain + "i"));
				if ((excelFont.Font.Style & FontStyle.Underline) == FontStyle.Underline)
					font.Add(new XElement(ExcelExporter.schemaMain + "u"));
				if ((excelFont.Font.Style & FontStyle.Strikeout) == FontStyle.Strikeout)
					font.Add(new XElement(ExcelExporter.schemaMain + "strike"));
				font.Add(new XElement(ExcelExporter.schemaMain + "color", new XAttribute("rgb", ConvertColor(excelFont.Color))));
				fonts.Add(font);
			}

			var fills = new XElement(ExcelExporter.schemaMain + "fills", new XAttribute("count", 1),
					new XElement(ExcelExporter.schemaMain + "fill",
							new XElement(ExcelExporter.schemaMain + "patternFill", new XAttribute("patternType", "none"))
							),
					new XElement(ExcelExporter.schemaMain + "fill",
							new XElement(ExcelExporter.schemaMain + "patternFill", new XAttribute("patternType", "gray125"))
							)
					);
			foreach (var color in fillsList)
			{
				fills.Add(new XElement(ExcelExporter.schemaMain + "fill",
						new XElement(ExcelExporter.schemaMain + "patternFill", new XAttribute("patternType", "solid"),
								new XElement(ExcelExporter.schemaMain + "fgColor", new XAttribute("rgb", ConvertColor(color))),
								new XElement(ExcelExporter.schemaMain + "bgColor", new XAttribute("indexed", 64))
								)
						)
						);
			}

			var borders = new XElement(ExcelExporter.schemaMain + "borders", new XAttribute("count", bordersList.Count + 1),
					new XElement(ExcelExporter.schemaMain + "border",
							new XElement(ExcelExporter.schemaMain + "left"),
							new XElement(ExcelExporter.schemaMain + "right"),
							new XElement(ExcelExporter.schemaMain + "top"),
							new XElement(ExcelExporter.schemaMain + "bottom"),
							new XElement(ExcelExporter.schemaMain + "diagonal")
							)
					);
			foreach (var border in bordersList)
			{
				borders.Add(new XElement(ExcelExporter.schemaMain + "border",
								FillBorder(border.Left, new XElement(ExcelExporter.schemaMain + "left")),
								FillBorder(border.Right, new XElement(ExcelExporter.schemaMain + "right")),
								FillBorder(border.Top, new XElement(ExcelExporter.schemaMain + "top")),
								FillBorder(border.Bottom, new XElement(ExcelExporter.schemaMain + "bottom")),
								new XElement(ExcelExporter.schemaMain + "diagonal")
								)
						);
			}

			var cellStyleXfs = new XElement(ExcelExporter.schemaMain + "cellStyleXfs",
					new XAttribute("count", 1),
					new XElement(ExcelExporter.schemaMain + "xf",
									new XAttribute("fontId", 0), new XAttribute("fillId", 0),
									new XAttribute("borderId", 0), new XAttribute("numFmtId", 0),
									new XAttribute("applyNumberFormat", 0), new XAttribute("applyBorder", 0),
									new XAttribute("applyAlignment", 0), new XAttribute("applyProtection", 0)
							)
					);
			var cellXfs = new XElement(ExcelExporter.schemaMain + "cellXfs");
			foreach (var chain in _chains)
			{
				var xf = new XElement(ExcelExporter.schemaMain + "xf", new XAttribute("xfId", 0));
				foreach (var style in chain.GetStyles())
				{
					var textStyle = style as TextContentStyle;
					if (textStyle != null)
					{
						var excelFont = new ExcelFont { Font = textStyle.Font, Color = textStyle.Color };
						var fontId = fontsList.IndexOf(excelFont) + 1;
						xf.Add(new XAttribute("fontId", fontId), new XAttribute("applyFont", 1));
						xf.Add(new XAttribute("applyAlignment", 1),
								new XElement(ExcelExporter.schemaMain + "alignment", new XAttribute("wrapText", 1),
										new XAttribute("vertical", textStyle.VAlignment == VerticalAlignment.Top ?
												"top" : (textStyle.VAlignment == VerticalAlignment.Bottom ? "bottom" : "center")),
										new XAttribute("horizontal", textStyle.HAlignment == HorizontalAlignment.Right ?
												"right" : (textStyle.HAlignment == HorizontalAlignment.Left ? "left" : "center"))
						));
					}
					var cellStyle = style as CellStyle;
					if (cellStyle != null && !cellStyle.Borders.IsEmpty())
					{
						var borderId = bordersList.IndexOf(cellStyle.Borders) + 1;
						xf.Add(new XAttribute("borderId", borderId));
						var fillId = fillsList.IndexOf(cellStyle.Color);
						fillId = fillId < 0 ? 0 : fillId + 2;
						xf.Add(new XAttribute("fillId", fillId));
						if (fillId > 0)
							xf.Add(new XAttribute("applyFill", 1));
					}
					var formatStyle = style as FormatContentStyle;
					if (formatStyle != null && HasFormat(formatStyle))
					{
						xf.Add(new XAttribute("numFmtId", numFormatsList.IndexOf(formatStyle.Format) + 1),
														new XAttribute("applyNumberFormat", 1));
					}
				}
				cellXfs.Add(xf);
			}
			cellXfs.Add(new XAttribute("count", _chains.Count));
			var cellStyles = new XElement(ExcelExporter.schemaMain + "cellStyles",
					new XAttribute("count", 1),
					new XElement(ExcelExporter.schemaMain + "cellStyle",
							new XAttribute("name", "CellReportStyle"),
							new XAttribute("xfId", 0)
							)
					);
			var root = new XElement(ExcelExporter.schemaMain + "styleSheet",
					numFmts,
					fonts,
					fills,
					borders,
					cellStyleXfs,
					cellXfs,
					cellStyles,
					new XElement(ExcelExporter.schemaMain + "dxfs", new XAttribute("count", 0)),
					new XElement(ExcelExporter.schemaMain + "tableStyles", new XAttribute("count", 0),
							new XAttribute("defaultTableStyle", "TableStyleMedium9"),
							new XAttribute("defaultPivotStyle", "PivotStyleLight16")
							)
					);
			root.Save(new StreamWriter(part.GetStream()));
		}

		private XElement FillBorder(Border border, XElement element)
		{
			if (!border.IsEmpty() && border.Style != LineStyle.None)
			{
				string style;
				switch (border.Style)
				{
					case LineStyle.Dot:
						style = "hair";
						if (border.Weight > 0.5)
							style = "dotted";
						break;
					case LineStyle.Dash:
						style = "dashed";
						if (border.Weight > 0.5)
							style = "mediumdashed";
						break;
					case LineStyle.DashDot:
						style = "dashdot";
						if (border.Weight > 0.5)
							style = "mediumdashdot";
						break;
					case LineStyle.DashDotDot:
						style = "dashdotdot";
						if (border.Weight > 0.5)
							style = "mediumdashdotdot";
						break;
					default:
						style = "thick";
						if (border.Weight < 0.5)
							style = "thin";
						else if (border.Weight < 1)
							style = "medium";
						break;
				}
				byte a; byte r; byte g; byte b;
				BaseStyle.ParseColor(border.Color, out a, out r, out g, out b);
				var colorStr = $"{a:X2}{r:X2}{g:X2}{b:X2}";
				element.Add(new XElement(ExcelExporter.schemaMain + "color", new XAttribute("rgb", colorStr)));
				element.Add(new XAttribute("style", style));
			}

			return element;
		}
	}
}
