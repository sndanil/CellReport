using System;
using System.Collections.Generic;

using CellReport.Core;
using CellReport.Presenters;

namespace CellReport.Export.Image
{
	public class ImagePresenter : BasePresenter
	{
		private const GraphicsUnit ImageUnit = GraphicsUnit.Pixel;
		private const float ImageDpi = 96f;
		private const string ImageBackground = "#FFFFFF";

		private int _imageWidth;
		private int _imageHeight;

		public ImagePresenter(CellManager manager, BaseRenderer renderer)
						: base(manager)
		{
			Renderer = renderer;
			DeviceDpiX = ImageDpi;
			DeviceDpiY = ImageDpi;
			CurrentUnit = ImageUnit;

			Scale = 1;
			ShowGridLines = false;
			ShowBookMarks = false;
			GridColor = "#D0D7E5";
		}

		protected override Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
		{
			return Size.Empty;
		}

		public override float Scale { get; set; }

		public override bool ShowGridLines { get; set; }

		public override bool ShowBookMarks { get; set; }

		public override string GridColor { get; set; }

		public byte[] Render()
		{
			this.BeginRender();

			var width = 0f;
			for (int i = 0; i < Manager.ColumnsCount; i++)
				width += Manager.GetColumnWidth(i);
			var height = 0f;
			for (int i = 0; i < Manager.RowsCount; i++)
				height += Manager.GetRowHeight(i);

			width = UnitsConverter.ConvertValue(width, DefaultUnit, CurrentUnit, DeviceDpiX);
			height = UnitsConverter.ConvertValue(height, DefaultUnit, CurrentUnit, DeviceDpiY);
			_imageWidth = (int)Math.Ceiling(width) + 1;
			_imageHeight = (int)Math.Ceiling(height) + 1;
			var data = Renderer.CreateImage(_imageWidth, _imageHeight, RenderImage);
			this.EndRender();
			return data;
		}

		private void RenderImage(object context)
		{
			Renderer.RenderFillRect(context, new Rectangle(0, 0, _imageWidth, _imageHeight), 0, ImageBackground);

			var alreadyPaintedCells = new Dictionary<Cell, bool>();
			for (int row = 0; row < Manager.RowsCount; row++)
			{
				var top = GetTopOffset(row);
				for (int col = 0; col < Manager.ColumnsCount; col++)
				{
					var left = GetLeftOffset(col);
					var cell = Manager.GetCell(row, col);
					if (cell == null)
						RenderGrid(context, GetCellRect(new CellLocation() { Column = col, Row = row }));
					else if (!alreadyPaintedCells.ContainsKey(cell))
					{
						alreadyPaintedCells.Add(cell, true);
						RenderGrid(context, GetCellRect(cell));
						RenderCellBackgroung(context, cell);
					}
				}
			}
			alreadyPaintedCells.Clear();
			for (int row = 0; row < Manager.RowsCount; row++)
			{
				var top = GetTopOffset(row);
				for (int col = 0; col < Manager.ColumnsCount; col++)
				{
					var left = GetLeftOffset(col);
					var cell = Manager.GetCell(row, col);
					if ((cell != null) && !alreadyPaintedCells.ContainsKey(cell))
					{
						alreadyPaintedCells.Add(cell, true);
						RenderCell(context, cell);
					}
				}
			}
		}
	}
}
