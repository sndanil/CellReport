using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CellReport.Core;
using CellReport.Core.Contents;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Border = CellReport.Core.Border;

namespace CellReport.Editor.Import
{
	public static class ExcelImporter
	{
		private static Dictionary<int, CellStyle> _cellStyles = new Dictionary<int, CellStyle>();
		private static Dictionary<int, TextContentStyle> _textStyles = new Dictionary<int, TextContentStyle>();
		private static Dictionary<int, DecimalContentStyle> _decimalStyles = new Dictionary<int, DecimalContentStyle>();
		private static Dictionary<int, DateTimeContentStyle> _datetimeStyles = new Dictionary<int, DateTimeContentStyle>();

		public static CellManager Import(string fileName)
		{
			CellManager manager = null;
			try
			{
				using (ExcelPackage package = new ExcelPackage(new FileInfo(fileName)))
				{
					_cellStyles.Clear();
					foreach (var workSheet in package.Workbook.Worksheets)
					{
						manager = new CellManager(workSheet.Dimension.End.Row, workSheet.Dimension.End.Column);
						manager.Properties.DocumentName = workSheet.Name;
						for (int i = 0; i < manager.ColumnsCount; i++)
							manager.SetColumnWidth(i, FromExcelUnit(workSheet.Column(i + 1).Width));
						for (int i = 0; i < manager.RowsCount; i++)
							manager.SetRowHeight(i, UnitsConverter.ConvertValue((float)workSheet.Row(i + 1).Height, GraphicsUnit.Point, GraphicsUnit.Millimeter, 0));

						for (int rowIndex = 0; rowIndex < manager.RowsCount; rowIndex++)
						{
							for (int colIndex = 0; colIndex < manager.ColumnsCount; colIndex++)
							{
								var excelCell = workSheet.Cells[rowIndex + 1, colIndex + 1];
								CreateCell(excelCell, manager, rowIndex, colIndex, workSheet);
							}
						}

						break;
					}
				}
			}
			catch (Exception)
			{
			}

			return manager;
		}

		private static void CreateCell(ExcelRange excelCell, CellManager manager, int rowIndex, int colIndex, ExcelWorksheet workSheet)
		{
			Cell cell = null;
			var excelStyle = excelCell.Style;
			if (excelCell.Value is double)
			{
				var content = new DecimalContent(manager) { Value = Convert.ToDecimal((double)excelCell.Value) };
				cell = manager.CreateCell(rowIndex, colIndex);
				cell.Content = content;
				DecimalContentStyle style;
				if (!_decimalStyles.TryGetValue(excelCell.StyleID, out style))
				{
					style = new DecimalContentStyle(ConvertHAlignment(excelStyle.HorizontalAlignment), ConvertVAlignment(excelStyle.VerticalAlignment),
						ConvertFont(excelStyle), "Black", "Red", 0, excelStyle.Numberformat.Format, null, Direction.LeftToRight);
					style.ID = excelCell.StyleName + excelCell.StyleID;
					manager.AddStyle(style);
				}
				content.StyleID = style.ID;
			}
			else if (excelCell.Value is DateTime)
			{
				var content = new DateTimeContent(manager) { Value = (DateTime)excelCell.Value };
				cell = manager.CreateCell(rowIndex, colIndex);
				cell.Content = content;
				DateTimeContentStyle style;
				if (!_datetimeStyles.TryGetValue(excelCell.StyleID, out style))
				{
					style = new DateTimeContentStyle(ConvertHAlignment(excelStyle.HorizontalAlignment), ConvertVAlignment(excelStyle.VerticalAlignment),
						ConvertFont(excelStyle), "Black", 0, excelStyle.Numberformat.Format, null, Direction.LeftToRight);
					style.ID = excelCell.StyleName + excelCell.StyleID;
					manager.AddStyle(style);
				}
				content.StyleID = style.ID;
			}
			else if (!string.IsNullOrEmpty(excelCell.Text))
			{
				var content = new TextContent(manager) { Text = excelCell.Text };
				cell = manager.CreateCell(rowIndex, colIndex);
				cell.Content = content;
				TextContentStyle style;
				if (!_textStyles.TryGetValue(excelCell.StyleID, out style))
				{
					style = new TextContentStyle(ConvertHAlignment(excelStyle.HorizontalAlignment), ConvertVAlignment(excelStyle.VerticalAlignment),
						ConvertFont(excelStyle), "Black");
					style.ID = excelCell.StyleName + excelCell.StyleID;
					manager.AddStyle(style);
				}
				content.StyleID = style.ID;
			}
			if (excelCell.Merge)
			{
				int colSpan = 0;
				int rowSpan = 0;

				ExcelAddress cellAddress = new ExcelAddress(excelCell.Address);

				var cellsResult = (from c in workSheet.MergedCells
													 let addr = new ExcelAddress(c)
													 where cellAddress.Start.Row >= addr.Start.Row &&
																 cellAddress.End.Row <= addr.End.Row &&
																 cellAddress.Start.Column >= addr.Start.Column &&
																 cellAddress.End.Column <= addr.End.Column
													 select addr);

				if (cellsResult.Any())
				{
					var first = cellsResult.First();

					if (first.Start.Address == cellAddress.Start.Address)
					{
						if (first.Start.Column != first.End.Column)
							colSpan += first.End.Column - first.Start.Column;

						if (first.Start.Row != first.End.Row)
							rowSpan += first.End.Row - first.Start.Row;

						if (cell == null)
							cell = manager.CreateCell(rowIndex, colIndex);

						cell.Span = new CellSpan() { RowSpan = rowSpan, ColSpan = colSpan };
					}
					else
					{
						return;
					}
				}
			}
			if (excelCell.StyleID != 1)
			{
				if (cell == null)
					cell = manager.CreateCell(rowIndex, colIndex);

				if (!_cellStyles.TryGetValue(excelCell.StyleID, out var cellStyle))
				{
					var bordersArray = new[]
					{
						Border.Empty,
						Border.Empty,
						Border.Empty,
						Border.Empty,
					};

					if (excelCell.Style.Border.Top.Style != ExcelBorderStyle.None)
						bordersArray[0] = GetBorder(excelCell.Style.Border.Top);

					if (excelCell.Style.Border.Left.Style != ExcelBorderStyle.None)
						bordersArray[1] = GetBorder(excelCell.Style.Border.Left);

					if (excelCell.Style.Border.Right.Style != ExcelBorderStyle.None)
						bordersArray[2] = GetBorder(excelCell.Style.Border.Right);

					if (excelCell.Style.Border.Bottom.Style != ExcelBorderStyle.None)
						bordersArray[3] = GetBorder(excelCell.Style.Border.Bottom);

					var borders = new Borders(bordersArray[0], bordersArray[1], bordersArray[2], bordersArray[3]);

					cellStyle = new CellStyle("Transparent", borders, new Paddings(0));
					cellStyle.ID = excelCell.StyleName + excelCell.StyleID;
					manager.AddStyle(cellStyle);

					_cellStyles.Add(excelCell.StyleID, cellStyle);
				}
				cell.StyleID = cellStyle.ID;
			}
		}

		private static FontInfo ConvertFont(ExcelStyle excelStyle)
		{
			var fontStyle = FontStyle.Regular;
			if (excelStyle.Font.Bold)
				fontStyle |= FontStyle.Bold;
			if (excelStyle.Font.Italic)
				fontStyle |= FontStyle.Italic;
			if (excelStyle.Font.Strike)
				fontStyle |= FontStyle.Strikeout;
			if (excelStyle.Font.UnderLine)
				fontStyle |= FontStyle.Underline;

			return new FontInfo(excelStyle.Font.Name, excelStyle.Font.Size, fontStyle);
		}

		private static HorizontalAlignment ConvertHAlignment(ExcelHorizontalAlignment excelHorizontal)
		{
			switch (excelHorizontal)
			{
				default:
				case ExcelHorizontalAlignment.Left:
					return HorizontalAlignment.Left;
				case ExcelHorizontalAlignment.Right:
					return HorizontalAlignment.Right;
				case ExcelHorizontalAlignment.Justify:
					return HorizontalAlignment.Justify;
				case ExcelHorizontalAlignment.Center:
					return HorizontalAlignment.Center;
			}
		}

		private static VerticalAlignment ConvertVAlignment(ExcelVerticalAlignment excelVertical)
		{
			switch (excelVertical)
			{
				default:
				case ExcelVerticalAlignment.Top:
					return VerticalAlignment.Top;
				case ExcelVerticalAlignment.Center:
					return VerticalAlignment.Middle;
				case ExcelVerticalAlignment.Bottom:
					return VerticalAlignment.Bottom;
			}
		}

		private static Border GetBorder(ExcelBorderItem excelBorder)
		{
			return new Border(GetBorderWeight(excelBorder.Style), "Black", GetBorderStyle(excelBorder.Style));
		}

		private static float GetBorderWeight(ExcelBorderStyle borderStyle)
		{
			switch (borderStyle)
			{
				default:
					return 0.35f;
				case ExcelBorderStyle.Medium:
				case ExcelBorderStyle.MediumDashDot:
				case ExcelBorderStyle.MediumDashDotDot:
				case ExcelBorderStyle.MediumDashed:
					return 0.7f;
				case ExcelBorderStyle.Double:
					return 1f;
			}
		}

		private static LineStyle GetBorderStyle(ExcelBorderStyle borderStyle)
		{
			switch (borderStyle)
			{
				default:
					return LineStyle.Solid;
				case ExcelBorderStyle.Dotted:
					return LineStyle.Dot;
				case ExcelBorderStyle.Dashed:
					return LineStyle.Dash;
				case ExcelBorderStyle.DashDotDot:
					return LineStyle.DashDotDot;
			}
		}

		private static float FromExcelUnit(double excel)
		{
			var excelWidth = Convert.ToSingle(excel);
			return UnitsConverter.ConvertValue(excelWidth * 5.25f, GraphicsUnit.Point, GraphicsUnit.Millimeter, 0);
		}

	}
}
