using System;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Presenters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class OverflowLayoutTests
	{
		private const float ColumnWidth = 35f;
		private const float RowHeight = 20f;

		private static CellManager CreateManager(int rows, int columns)
		{
			var manager = new CellManager(rows, columns);
			for (var column = 0; column < columns; column++)
				manager.SetColumnWidth(column, ColumnWidth);
			for (var row = 0; row < rows; row++)
				manager.SetRowHeight(row, RowHeight);

			return manager;
		}

		private static TestGeometry CreateGeometry(int columns = 4, int rows = 1)
		{
			var manager = CreateManager(rows, columns);
			return new TestGeometry(manager);
		}

		private static TextContent AddText(CellManager manager, int row, int column, string text = "text")
		{
			var cell = manager.CreateCell(row, column);
			var content = new TextContent(manager) { Text = text };
			cell.Content = content;
			return content;
		}

		private static TextOverflowGeometry Compute(
			TestGeometry geometry,
			Cell cell,
			TextOverflow overflow,
			HorizontalAlignment alignment,
			float naturalWidth)
		{
			return new OverflowLayout(geometry).ComputeText(
				cell,
				overflow,
				alignment,
				VerticalAlignment.Top,
				new Size(naturalWidth, 12f),
				geometry.Pane);
		}

		[TestMethod]
		public void Auto_LeftAligned_StopsAtFirstBlockedCell()
		{
			var geometry = CreateGeometry();
			AddText(geometry.Manager, 0, 0);
			AddText(geometry.Manager, 0, 2, "blocker");

			var result = Compute(geometry, geometry.Manager.GetCell(0, 0), TextOverflow.Auto,
				HorizontalAlignment.Left, 200f);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(0f, result.ClipRect.Left);
			Assert.AreEqual(ColumnWidth * 2f, result.ClipRect.Right, "обрезка по левому краю занятой колонки 2");
		}

		[TestMethod]
		public void Auto_LeftAligned_NoBlocker_ExtendsToPaneEdge()
		{
			var geometry = CreateGeometry();
			AddText(geometry.Manager, 0, 0);

			var result = Compute(geometry, geometry.Manager.GetCell(0, 0), TextOverflow.Auto,
				HorizontalAlignment.Left, 200f);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(geometry.Pane.Right, result.ClipRect.Right);
		}

		[TestMethod]
		public void Auto_RightAligned_ExtendsLeft()
		{
			var geometry = CreateGeometry();
			AddText(geometry.Manager, 0, 2);
			AddText(geometry.Manager, 0, 0, "blocker");
			var cell = geometry.Manager.GetCell(0, 2);

			var result = Compute(geometry, cell, TextOverflow.Auto, HorizontalAlignment.Right, 200f);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(ColumnWidth, result.ClipRect.Left, "правый край блокирующей колонки 0");
			Assert.AreEqual(ColumnWidth * 3f, result.ClipRect.Right);
		}

		[TestMethod]
		public void RightMode_DoesNotUseLeftFreeSpace()
		{
			var geometry = CreateGeometry();
			AddText(geometry.Manager, 0, 1);
			var cell = geometry.Manager.GetCell(0, 1);

			var result = Compute(geometry, cell, TextOverflow.Right, HorizontalAlignment.Right, 200f);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(ColumnWidth, result.ClipRect.Left, "влево выходить нельзя");
			Assert.AreEqual(geometry.Pane.Right, result.ClipRect.Right);
		}

		[TestMethod]
		public void LeftMode_DoesNotUseRightFreeSpace()
		{
			var geometry = CreateGeometry();
			AddText(geometry.Manager, 0, 1);
			var cell = geometry.Manager.GetCell(0, 1);

			var result = Compute(geometry, cell, TextOverflow.Left, HorizontalAlignment.Left, 200f);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(0f, result.ClipRect.Left);
			Assert.AreEqual(ColumnWidth * 2f, result.ClipRect.Right, "вправо выходить нельзя");
		}

		[TestMethod]
		public void BothMode_UsesFreeSpaceOnBothSides()
		{
			var geometry = CreateGeometry(5);
			AddText(geometry.Manager, 0, 2);
			AddText(geometry.Manager, 0, 0, "left blocker");
			AddText(geometry.Manager, 0, 4, "right blocker");
			var cell = geometry.Manager.GetCell(0, 2);

			var result = Compute(geometry, cell, TextOverflow.Both, HorizontalAlignment.Center, 200f);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(ColumnWidth, result.ClipRect.Left);
			Assert.AreEqual(ColumnWidth * 4f, result.ClipRect.Right);
		}

		[TestMethod]
		public void Auto_Justify_FallsBackToClip()
		{
			var geometry = CreateGeometry();
			AddText(geometry.Manager, 0, 0);
			var cell = geometry.Manager.GetCell(0, 0);

			var result = Compute(geometry, cell, TextOverflow.Auto, HorizontalAlignment.Justify, 200f);

			Assert.IsFalse(result.NeedsOverflow);
			Assert.IsFalse(result.NoWrap);
			Assert.AreEqual(0f, result.ClipRect.Left);
			Assert.AreEqual(ColumnWidth, result.ClipRect.Right);
		}

		[TestMethod]
		public void TextFits_NoOverflow()
		{
			var geometry = CreateGeometry();
			AddText(geometry.Manager, 0, 0);
			var cell = geometry.Manager.GetCell(0, 0);

			var result = Compute(geometry, cell, TextOverflow.Auto, HorizontalAlignment.Left, 10f);

			Assert.IsFalse(result.NeedsOverflow);
			Assert.AreEqual(0f, result.LayoutRect.X);
			Assert.AreEqual(ColumnWidth, result.ClipRect.Right);
		}

		[TestMethod]
		public void Ellipsis_ClipsToCellAndRequestsEllipsis()
		{
			var geometry = CreateGeometry();
			AddText(geometry.Manager, 0, 0);
			var cell = geometry.Manager.GetCell(0, 0);

			var result = Compute(geometry, cell, TextOverflow.Ellipsis, HorizontalAlignment.Left, 200f);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.IsTrue(result.Ellipsis);
			Assert.IsTrue(result.NoWrap);
			Assert.AreEqual(0f, result.ClipRect.Left);
			Assert.AreEqual(ColumnWidth, result.ClipRect.Right);
		}

		[TestMethod]
		public void PaneBoundary_ClipsOverflow()
		{
			var geometry = CreateGeometry();
			AddText(geometry.Manager, 0, 0);
			var cell = geometry.Manager.GetCell(0, 0);
			var pane = new Rectangle(ColumnWidth, 0, ColumnWidth * 2f, RowHeight);

			var result = new OverflowLayout(geometry).ComputeText(
				cell, TextOverflow.Right, HorizontalAlignment.Left, VerticalAlignment.Top,
				new Size(200f, 12f), pane);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(ColumnWidth, result.ClipRect.Left);
			Assert.AreEqual(ColumnWidth * 3f, result.ClipRect.Right);
		}

		[TestMethod]
		public void MergedCell_OwnSpanDoesNotBlock()
		{
			var geometry = CreateGeometry(5);
			var cell = geometry.Manager.CreateCell(0, 0);
			cell.Span = new CellSpan { ColSpan = 1 };
			cell.Content = new TextContent(geometry.Manager) { Text = "merged" };
			AddText(geometry.Manager, 0, 3, "blocker");

			var result = Compute(geometry, cell, TextOverflow.Right, HorizontalAlignment.Left, 300f);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(ColumnWidth * 3f, result.ClipRect.Right, "собственный span не блокирует");
		}

		[TestMethod]
		public void MergedNeighbour_BlocksAtItsLeftEdge()
		{
			var geometry = CreateGeometry(5);
			AddText(geometry.Manager, 0, 0);
			var merged = geometry.Manager.CreateCell(0, 2);
			merged.Span = new CellSpan { ColSpan = 1 };
			merged.Content = new TextContent(geometry.Manager) { Text = "merged" };

			var result = Compute(geometry, geometry.Manager.GetCell(0, 0), TextOverflow.Right,
				HorizontalAlignment.Left, 300f);

			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(ColumnWidth * 2f, result.ClipRect.Right);
		}

		[TestMethod]
		public void ImageStretch_FillsCell()
		{
			var geometry = CreateGeometry(2);
			var cell = geometry.Manager.CreateCell(0, 0);

			var result = new OverflowLayout(geometry).ComputeImage(
				cell, ImageSizeMode.Stretch, 100f, ImageAnchor.Center, false,
				new Size(200f, 100f), geometry.Pane);

			Assert.AreEqual(0f, result.Rect.Left);
			Assert.AreEqual(0f, result.Rect.Top);
			Assert.AreEqual(ColumnWidth, result.Rect.Width);
			Assert.AreEqual(RowHeight, result.Rect.Height);
			Assert.IsFalse(result.NeedsOverflow);
		}

		[TestMethod]
		public void ImageActual_UsesNaturalSize_AndAnchor()
		{
			var geometry = CreateGeometry(2);
			var cell = geometry.Manager.CreateCell(0, 0);

			var result = new OverflowLayout(geometry).ComputeImage(
				cell, ImageSizeMode.Actual, 100f, ImageAnchor.BottomRight, true,
				new Size(200f, 100f), geometry.Pane);

			Assert.AreEqual(ColumnWidth - 200f, result.Rect.Left);
			Assert.AreEqual(RowHeight - 100f, result.Rect.Top);
			Assert.AreEqual(200f, result.Rect.Width);
			Assert.AreEqual(100f, result.Rect.Height);
			Assert.IsTrue(result.NeedsOverflow);
			Assert.AreEqual(geometry.Pane, result.ClipRect);
		}

		[TestMethod]
		public void ImageOverflow_ClipsToPane_OtherwiseToCell()
		{
			var geometry = CreateGeometry(2);
			var cell = geometry.Manager.CreateCell(0, 0);

			var withoutOverflow = new OverflowLayout(geometry).ComputeImage(
				cell, ImageSizeMode.Actual, 100f, ImageAnchor.Center, false,
				new Size(200f, 100f), geometry.Pane);
			var withOverflow = new OverflowLayout(geometry).ComputeImage(
				cell, ImageSizeMode.Actual, 100f, ImageAnchor.Center, true,
				new Size(200f, 100f), geometry.Pane);

			Assert.AreEqual(0f, withoutOverflow.ClipRect.Left);
			Assert.AreEqual(ColumnWidth, withoutOverflow.ClipRect.Right);
			Assert.AreEqual(geometry.Pane, withOverflow.ClipRect);
		}

		[TestMethod]
		public void FindOverflowSeed_ReturnsNearestContentCell()
		{
			var geometry = CreateGeometry(6);
			AddText(geometry.Manager, 0, 0, "far");
			AddText(geometry.Manager, 0, 2, "nearest");
			AddText(geometry.Manager, 0, 5);

			var layout = new OverflowLayout(geometry);
			var seed = layout.FindOverflowSeed(0, 4, 0);

			Assert.IsNotNull(seed);
			Assert.AreEqual(2, seed.Location.Column);
		}

		[TestMethod]
		public void FindOverflowSeed_RespectsFrozenColumns()
		{
			var geometry = CreateGeometry(6);
			AddText(geometry.Manager, 0, 0);

			var layout = new OverflowLayout(geometry);
			var seed = layout.FindOverflowSeed(0, 1, 1);

			Assert.IsNull(seed, "закреплённая колонка не может быть источником для прокручиваемой панели");
		}

		[TestMethod]
		public void FindOverflowSeedUp_ReturnsNearestContentCellAbove()
		{
			var geometry = CreateGeometry(2, 5);
			AddText(geometry.Manager, 0, 1);
			AddText(geometry.Manager, 2, 1);

			var layout = new OverflowLayout(geometry);
			var seed = layout.FindOverflowSeedUp(1, 4, 0);

			Assert.IsNotNull(seed);
			Assert.AreEqual(2, seed.Location.Row);
		}

		private sealed class TestGeometry : ICellGridGeometry
		{
			private readonly float[] _widths;
			private readonly float[] _heights;

			public TestGeometry(CellManager manager)
			{
				Manager = manager;
				_widths = new float[manager.ColumnsCount];
				_heights = new float[manager.RowsCount];
				for (var i = 0; i < _widths.Length; i++)
					_widths[i] = manager.GetColumnWidth(i);
				for (var i = 0; i < _heights.Length; i++)
					_heights[i] = manager.GetRowHeight(i);
			}

			public CellManager Manager { get; }

			public Rectangle Pane => new Rectangle(0, 0, TotalWidth, TotalHeight);

			public int RowsCount => Manager.RowsCount;

			public int ColumnsCount => Manager.ColumnsCount;

			private float TotalWidth
			{
				get
				{
					var width = 0f;
					for (var i = 0; i < _widths.Length; i++)
						width += _widths[i];
					return width;
				}
			}

			private float TotalHeight
			{
				get
				{
					var height = 0f;
					for (var i = 0; i < _heights.Length; i++)
						height += _heights[i];
					return height;
				}
			}

			public float GetLeftOffset(int column)
			{
				var offset = 0f;
				for (var i = 0; i < column && i < _widths.Length; i++)
					offset += _widths[i];
				return offset;
			}

			public float GetColumnWidth(int column)
			{
				return _widths[column];
			}

			public Rectangle GetCellRect(Cell cell)
			{
				var width = 0f;
				var height = 0f;
				for (var i = 0; i <= cell.Span.ColSpan; i++)
					width += _widths[cell.Location.Column + i];
				for (var i = 0; i <= cell.Span.RowSpan; i++)
					height += _heights[cell.Location.Row + i];

				return new Rectangle(GetLeftOffset(cell.Location.Column), GetTopOffset(cell.Location.Row), width, height);
			}

			public Rectangle GetCellRect(int row, int column)
			{
				return new Rectangle(GetLeftOffset(column), GetTopOffset(row), _widths[column], _heights[row]);
			}

			public Rectangle GetInnerCellRect(Cell cell)
			{
				return GetCellRect(cell);
			}

			public bool TryGetOccupiedCell(int row, int column, out Cell cell)
			{
				return Manager.TryGetOccupiedCell(row, column, out cell);
			}

			private float GetTopOffset(int row)
			{
				var offset = 0f;
				for (var i = 0; i < row && i < _heights.Length; i++)
					offset += _heights[i];
				return offset;
			}
		}
	}
}
