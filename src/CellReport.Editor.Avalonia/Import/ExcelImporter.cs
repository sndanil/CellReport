using CellReport.Core;
using CellReport.Core.Contents;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Border = CellReport.Core.Border;

namespace CellReport.Editor.Avalonia.Import;

public static class ExcelImporter
{
	public static CellManager? Import(string fileName)
	{
		var cellStyles = new Dictionary<int, CellStyle>();
		var textStyles = new Dictionary<int, TextContentStyle>();
		var decimalStyles = new Dictionary<int, DecimalContentStyle>();
		var dateTimeStyles = new Dictionary<int, DateTimeContentStyle>();

		try
		{
			using var package = new ExcelPackage(new FileInfo(fileName));
			foreach (var workSheet in package.Workbook.Worksheets)
			{
				if (workSheet.Dimension == null)
					return null;

				var manager = new CellManager(workSheet.Dimension.End.Row, workSheet.Dimension.End.Column);
				manager.Properties.DocumentName = workSheet.Name;
				for (var i = 0; i < manager.ColumnsCount; i++)
					manager.SetColumnWidth(i, FromExcelUnit(workSheet.Column(i + 1).Width));
				for (var i = 0; i < manager.RowsCount; i++)
					manager.SetRowHeight(i, UnitsConverter.ConvertValue((float)workSheet.Row(i + 1).Height, GraphicsUnit.Point, GraphicsUnit.Millimeter, 0));

				for (var rowIndex = 0; rowIndex < manager.RowsCount; rowIndex++)
				{
					for (var colIndex = 0; colIndex < manager.ColumnsCount; colIndex++)
					{
						var excelCell = workSheet.Cells[rowIndex + 1, colIndex + 1];
						CreateCell(excelCell, manager, rowIndex, colIndex, workSheet, cellStyles, textStyles, decimalStyles, dateTimeStyles);
					}
				}

				return manager;
			}
		}
		catch (Exception)
		{
			return null;
		}

		return null;
	}

	private static void CreateCell(
		ExcelRange excelCell,
		CellManager manager,
		int rowIndex,
		int colIndex,
		ExcelWorksheet workSheet,
		Dictionary<int, CellStyle> cellStyles,
		Dictionary<int, TextContentStyle> textStyles,
		Dictionary<int, DecimalContentStyle> decimalStyles,
		Dictionary<int, DateTimeContentStyle> dateTimeStyles)
	{
		Cell? cell = null;
		var excelStyle = excelCell.Style;
		if (excelCell.Value is double number)
		{
			var content = new DecimalContent(manager) { Value = Convert.ToDecimal(number) };
			cell = manager.CreateCell(rowIndex, colIndex);
			cell.Content = content;
			if (!decimalStyles.TryGetValue(excelCell.StyleID, out var style))
			{
				style = new DecimalContentStyle(
					ConvertHAlignment(excelStyle.HorizontalAlignment),
					ConvertVAlignment(excelStyle.VerticalAlignment),
					ConvertFont(excelStyle),
					"Black",
					"Red",
					0,
					excelStyle.Numberformat.Format,
					null,
					Direction.LeftToRight);
				style.ID = excelCell.StyleName + excelCell.StyleID;
				manager.AddStyle(style);
				decimalStyles.Add(excelCell.StyleID, style);
			}

			content.StyleID = style.ID;
		}
		else if (excelCell.Value is DateTime date)
		{
			var content = new DateTimeContent(manager) { Value = date };
			cell = manager.CreateCell(rowIndex, colIndex);
			cell.Content = content;
			if (!dateTimeStyles.TryGetValue(excelCell.StyleID, out var style))
			{
				style = new DateTimeContentStyle(
					ConvertHAlignment(excelStyle.HorizontalAlignment),
					ConvertVAlignment(excelStyle.VerticalAlignment),
					ConvertFont(excelStyle),
					"Black",
					0,
					excelStyle.Numberformat.Format,
					null,
					Direction.LeftToRight);
				style.ID = excelCell.StyleName + excelCell.StyleID;
				manager.AddStyle(style);
				dateTimeStyles.Add(excelCell.StyleID, style);
			}

			content.StyleID = style.ID;
		}
		else if (!string.IsNullOrEmpty(excelCell.Text))
		{
			var content = new TextContent(manager) { Text = excelCell.Text };
			cell = manager.CreateCell(rowIndex, colIndex);
			cell.Content = content;
			if (!textStyles.TryGetValue(excelCell.StyleID, out var style))
			{
				style = new TextContentStyle(
					ConvertHAlignment(excelStyle.HorizontalAlignment),
					ConvertVAlignment(excelStyle.VerticalAlignment),
					ConvertFont(excelStyle),
					"Black");
				style.ID = excelCell.StyleName + excelCell.StyleID;
				manager.AddStyle(style);
				textStyles.Add(excelCell.StyleID, style);
			}

			content.StyleID = style.ID;
		}

		if (excelCell.Merge)
		{
			var cellAddress = new ExcelAddress(excelCell.Address);
			var cellsResult = from address in workSheet.MergedCells
							  let merged = new ExcelAddress(address)
							  where cellAddress.Start.Row >= merged.Start.Row &&
									cellAddress.End.Row <= merged.End.Row &&
									cellAddress.Start.Column >= merged.Start.Column &&
									cellAddress.End.Column <= merged.End.Column
							  select merged;

			var first = cellsResult.FirstOrDefault();
			if (first != null)
			{
				if (first.Start.Address == cellAddress.Start.Address)
				{
					var colSpan = first.Start.Column == first.End.Column ? 0 : first.End.Column - first.Start.Column;
					var rowSpan = first.Start.Row == first.End.Row ? 0 : first.End.Row - first.Start.Row;
					cell ??= manager.CreateCell(rowIndex, colIndex);
					cell.Span = new CellSpan { RowSpan = rowSpan, ColSpan = colSpan };
				}
				else
				{
					return;
				}
			}
		}

		if (excelCell.StyleID == 1)
			return;

		cell ??= manager.CreateCell(rowIndex, colIndex);
		if (!cellStyles.TryGetValue(excelCell.StyleID, out var cellStyle))
		{
			var bordersArray = new[]
			{
				Border.Empty,
				Border.Empty,
				Border.Empty,
				Border.Empty
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
			cellStyle = new CellStyle("Transparent", borders, new Paddings(0))
			{
				ID = excelCell.StyleName + excelCell.StyleID
			};
			manager.AddStyle(cellStyle);
			cellStyles.Add(excelCell.StyleID, cellStyle);
		}

		cell.StyleID = cellStyle.ID;
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
			case ExcelHorizontalAlignment.Right:
				return HorizontalAlignment.Right;
			case ExcelHorizontalAlignment.Justify:
				return HorizontalAlignment.Justify;
			case ExcelHorizontalAlignment.Center:
				return HorizontalAlignment.Center;
			default:
				return HorizontalAlignment.Left;
		}
	}

	private static VerticalAlignment ConvertVAlignment(ExcelVerticalAlignment excelVertical)
	{
		switch (excelVertical)
		{
			case ExcelVerticalAlignment.Center:
				return VerticalAlignment.Middle;
			case ExcelVerticalAlignment.Bottom:
				return VerticalAlignment.Bottom;
			default:
				return VerticalAlignment.Top;
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
			case ExcelBorderStyle.Medium:
			case ExcelBorderStyle.MediumDashDot:
			case ExcelBorderStyle.MediumDashDotDot:
			case ExcelBorderStyle.MediumDashed:
				return 0.7f;
			case ExcelBorderStyle.Double:
				return 1f;
			default:
				return 0.35f;
		}
	}

	private static LineStyle GetBorderStyle(ExcelBorderStyle borderStyle)
	{
		switch (borderStyle)
		{
			case ExcelBorderStyle.Dotted:
				return LineStyle.Dot;
			case ExcelBorderStyle.Dashed:
				return LineStyle.Dash;
			case ExcelBorderStyle.DashDotDot:
				return LineStyle.DashDotDot;
			default:
				return LineStyle.Solid;
		}
	}

	private static float FromExcelUnit(double excel)
	{
		return UnitsConverter.ConvertValue(Convert.ToSingle(excel) * 5.25f, GraphicsUnit.Point, GraphicsUnit.Millimeter, 0);
	}
}
