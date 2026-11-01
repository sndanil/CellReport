using System;
using CellReport.Core;
using CellReport.Core.Contents;

namespace CellReport.Presenters
{
	/// <summary>
	/// Геометрия сетки в тех же единицах, в которых рисует презентер.
	/// </summary>
	public interface ICellGridGeometry
	{
		int RowsCount { get; }

		int ColumnsCount { get; }

		float GetLeftOffset(int column);

		float GetColumnWidth(int column);

		Rectangle GetCellRect(Cell cell);

		Rectangle GetCellRect(int row, int column);

		Rectangle GetInnerCellRect(Cell cell);

		bool TryGetOccupiedCell(int row, int column, out Cell cell);
	}

	/// <summary>
	/// Готовые прямоугольники для рисования текста с переполнением.
	/// </summary>
	public sealed class TextOverflowGeometry
	{
		/// <summary>
		/// Текст не влезает в ячейку: рисовать через overlay-проход.
		/// </summary>
		public bool NeedsOverflow { get; set; }

		/// <summary>
		/// Нужна обрезка с многоточием.
		/// </summary>
		public bool Ellipsis { get; set; }

		/// <summary>
		/// Рисовать одной строкой (без переноса).
		/// </summary>
		public bool NoWrap { get; set; }

		/// <summary>
		/// Прямоугольник укладки текста (привязан к выравниванию, может выходить за ячейку).
		/// </summary>
		public Rectangle LayoutRect { get; set; }

		/// <summary>
		/// Прямоугольник, по которому обрезается текст.
		/// </summary>
		public Rectangle ClipRect { get; set; }
	}

	/// <summary>
	/// Готовые прямоугольники для рисования изображения.
	/// </summary>
	public sealed class ImageOverflowGeometry
	{
		public bool NeedsOverflow { get; set; }

		public Rectangle Rect { get; set; }

		public Rectangle ClipRect { get; set; }
	}

	/// <summary>
	/// Расчёт переполнения содержимого ячейки: разрешённые границы, блокирующие
	/// соседние ячейки и прямоугольники для рисования.
	/// </summary>
	public sealed class OverflowLayout
	{
		public const int DefaultMaxScanColumns = 64;
		public const int DefaultMaxLookBehindColumns = 32;

		private const float SizeEpsilon = 0.05f;

		private readonly ICellGridGeometry _grid;

		public OverflowLayout(ICellGridGeometry grid)
		{
			if (grid == null)
				throw new ArgumentNullException(nameof(grid));

			_grid = grid;
		}

		public int MaxScanColumns { get; set; } = DefaultMaxScanColumns;

		public int MaxLookBehindColumns { get; set; } = DefaultMaxLookBehindColumns;

		/// <summary>
		/// Есть ли в ячейке содержимое, которое блокирует переполнение соседей.
		/// </summary>
		public bool IsBlockingContent(Cell cell)
		{
			if (cell == null || cell.Content == null)
				return false;

			return HasVisibleContent(cell.Content);
		}

		/// <summary>
		/// Рассчитывает раскладку текста. Для <see cref="TextOverflow.Clip"/> возвращает
		/// геометрию без переполнения (её можно не использовать).
		/// </summary>
		public TextOverflowGeometry ComputeText(
			Cell cell,
			TextOverflow overflow,
			HorizontalAlignment hAlignment,
			VerticalAlignment vAlignment,
			Size naturalSize,
			Rectangle paneRect)
		{
			if (cell == null)
				throw new ArgumentNullException(nameof(cell));

			var result = new TextOverflowGeometry();
			var inner = _grid.GetInnerCellRect(cell);
			var cellRect = _grid.GetCellRect(cell);
			result.NoWrap = overflow != TextOverflow.Clip;
			result.Ellipsis = overflow == TextOverflow.Ellipsis;

			if (naturalSize.Width <= 0 || naturalSize.Height <= 0 || inner.Width <= 0 || inner.Height <= 0)
			{
				result.ClipRect = inner;
				return result;
			}

			var y = inner.Top;
			if (vAlignment == VerticalAlignment.Middle)
				y += (inner.Height - naturalSize.Height) / 2f;
			else if (vAlignment == VerticalAlignment.Bottom)
				y += inner.Height - naturalSize.Height;

			if (overflow == TextOverflow.Clip || (overflow == TextOverflow.Auto && hAlignment == HorizontalAlignment.Justify))
			{
				result.NoWrap = false;
				result.Ellipsis = false;
				result.LayoutRect = new Rectangle(inner.Left, y, inner.Width, naturalSize.Height);
				result.ClipRect = inner;
				result.NeedsOverflow = false;
				return result;
			}

			if (overflow == TextOverflow.Ellipsis)
			{
				result.LayoutRect = new Rectangle(inner.Left, y, inner.Width, naturalSize.Height);
				result.ClipRect = inner;
				result.NeedsOverflow = naturalSize.Width > inner.Width + SizeEpsilon;
				return result;
			}

			if (naturalSize.Width <= inner.Width + SizeEpsilon)
			{
				result.ClipRect = inner;
				result.NeedsOverflow = false;
				return result;
			}

			var layoutX = inner.Left;
			if (hAlignment == HorizontalAlignment.Right)
				layoutX = inner.Right - naturalSize.Width;
			else if (hAlignment == HorizontalAlignment.Center)
				layoutX = (inner.Left + inner.Right) / 2f - naturalSize.Width / 2f;

			result.LayoutRect = new Rectangle(layoutX, y, naturalSize.Width, naturalSize.Height);

			var allowLeft = overflow == TextOverflow.Left
					|| overflow == TextOverflow.Both
					|| (overflow == TextOverflow.Auto && hAlignment == HorizontalAlignment.Right);
			var allowRight = overflow == TextOverflow.Right
					|| overflow == TextOverflow.Both
					|| (overflow == TextOverflow.Auto && hAlignment != HorizontalAlignment.Right);

			var left = inner.Left;
			var right = inner.Right;

			if (allowLeft)
			{
				var blockerRight = FindLeftBlockerRight(cell, paneRect);
				left = blockerRight.HasValue ? Math.Max(paneRect.Left, blockerRight.Value) : paneRect.Left;
			}

			if (allowRight)
			{
				var blockerLeft = FindRightBlockerLeft(cell, paneRect);
				right = blockerLeft.HasValue ? Math.Min(paneRect.Right, blockerLeft.Value) : paneRect.Right;
			}

			left = Math.Max(left, paneRect.Left);
			right = Math.Min(right, paneRect.Right);

			if (right < left)
				right = left;

			result.ClipRect = new Rectangle(left, cellRect.Top, right - left, cellRect.Height);
			result.NeedsOverflow = naturalSize.Width > inner.Width + SizeEpsilon;
			return result;
		}

		/// <summary>
		/// Рассчитывает прямоугольник изображения с учётом режима размера и якоря.
		/// </summary>
		public ImageOverflowGeometry ComputeImage(
			Cell cell,
			ImageSizeMode sizeMode,
			float scale,
			ImageAnchor anchor,
			bool overflow,
			Size naturalSize,
			Rectangle paneRect)
		{
			if (cell == null)
				throw new ArgumentNullException(nameof(cell));

			var result = new ImageOverflowGeometry();
			var baseRect = GetImageBaseRect(cell);
			if (baseRect.Width <= 0 || baseRect.Height <= 0 || naturalSize.Width <= 0 || naturalSize.Height <= 0)
			{
				result.Rect = baseRect;
				result.ClipRect = overflow ? paneRect : baseRect;
				return result;
			}

			var width = baseRect.Width;
			var height = baseRect.Height;

			switch (sizeMode)
			{
				case ImageSizeMode.Fit:
				{
					var k = Math.Min(baseRect.Width / naturalSize.Width, baseRect.Height / naturalSize.Height);
					width = naturalSize.Width * k;
					height = naturalSize.Height * k;
					break;
				}
				case ImageSizeMode.Actual:
					width = naturalSize.Width;
					height = naturalSize.Height;
					break;
				case ImageSizeMode.Scale:
				{
					var k = scale > 0 ? scale / 100f : 1f;
					width = naturalSize.Width * k;
					height = naturalSize.Height * k;
					break;
				}
			}

			float fx, fy;
			GetAnchorFactors(anchor, out fx, out fy);
			var x = baseRect.Left + (baseRect.Width - width) * fx;
			var y = baseRect.Top + (baseRect.Height - height) * fy;

			result.Rect = new Rectangle(x, y, width, height);
			result.ClipRect = overflow ? paneRect : baseRect;
			result.NeedsOverflow = !ContainsRect(baseRect, result.Rect) && overflow;
			return result;
		}

		/// <summary>
		/// Ближайшая ячейка с содержимым слева от видимой области — потенциальный
		/// источник переполнения, прокрученный за край. Возвращает null, если такого нет.
		/// </summary>
		public Cell FindOverflowSeed(int row, int firstVisibleColumn, int frozenColumnsCount)
		{
			var minColumn = Math.Max(0, frozenColumnsCount);
			var scanned = 0;
			for (var column = firstVisibleColumn - 1; column >= minColumn && scanned < MaxLookBehindColumns; column--, scanned++)
			{
				if (_grid.GetColumnWidth(column) <= 0)
					continue;

				Cell cell;
				if (!_grid.TryGetOccupiedCell(row, column, out cell) || cell == null)
					continue;

				if (IsBlockingContent(cell))
					return cell;
			}

			return null;
		}

		/// <summary>
		/// Ближайшая ячейка с содержимым выше видимой области (для изображений,
		/// переполняющихся вверх).
		/// </summary>
		public Cell FindOverflowSeedUp(int column, int firstVisibleRow, int frozenRowsCount)
		{
			var minRow = Math.Max(0, frozenRowsCount);
			var scanned = 0;
			for (var row = firstVisibleRow - 1; row >= minRow && scanned < MaxLookBehindColumns; row--, scanned++)
			{
				Cell cell;
				if (!_grid.TryGetOccupiedCell(row, column, out cell) || cell == null)
					continue;

				if (IsBlockingContent(cell))
					return cell;
			}

			return null;
		}

		private static bool HasVisibleContent(BaseContent content)
		{
			var collection = content as CollectionContent;
			if (collection != null)
			{
				foreach (var item in collection.GetContents())
				{
					if (HasVisibleContent(item))
						return true;
				}
				return false;
			}

			var text = content as TextBasedContent;
			if (text != null)
				return !string.IsNullOrEmpty(text.GetTextForRender());

			return content is ImageContent
					|| content is BarCodeContent
					|| content is ChartContent
					|| content is LineContent;
		}

		private Cell FindBlockingSlot(Cell owner, int row, int column)
		{
			Cell cell;
			if (!_grid.TryGetOccupiedCell(row, column, out cell) || cell == null)
				return null;

			if (ReferenceEquals(cell, owner))
				return null;

			return IsBlockingContent(cell) ? cell : null;
		}

		private float? FindRightBlockerLeft(Cell cell, Rectangle paneRect)
		{
			var row = cell.Location.Row;
			var startColumn = cell.Location.Column + cell.Span.ColSpan + 1;
			var scanned = 0;
			for (var column = startColumn; column < _grid.ColumnsCount && scanned < MaxScanColumns; column++, scanned++)
			{
				if (_grid.GetColumnWidth(column) <= 0)
					continue;

				if (_grid.GetLeftOffset(column) >= paneRect.Right)
					break;

				var blocker = FindBlockingSlot(cell, row, column);
				if (blocker != null)
					return _grid.GetLeftOffset(blocker.Location.Column);
			}

			return null;
		}

		private float? FindLeftBlockerRight(Cell cell, Rectangle paneRect)
		{
			var row = cell.Location.Row;
			var scanned = 0;
			for (var column = cell.Location.Column - 1; column >= 0 && scanned < MaxScanColumns; column--, scanned++)
			{
				var width = _grid.GetColumnWidth(column);
				if (width <= 0)
					continue;

				if (_grid.GetLeftOffset(column) + width <= paneRect.Left)
					break;

				var blocker = FindBlockingSlot(cell, row, column);
				if (blocker != null)
					return _grid.GetCellRect(blocker).Right;
			}

			return null;
		}

		private Rectangle GetImageBaseRect(Cell cell)
		{
			var rect = _grid.GetInnerCellRect(cell);
			for (var i = 0; i < cell.Span.RowSpan; i++)
				rect.Height += _grid.GetCellRect(cell.Location.Row + i + 1, cell.Location.Column).Height;
			for (var i = 0; i < cell.Span.ColSpan; i++)
				rect.Width += _grid.GetCellRect(cell.Location.Row, cell.Location.Column + i + 1).Width;

			return rect;
		}

		private static void GetAnchorFactors(ImageAnchor anchor, out float fx, out float fy)
		{
			switch (anchor)
			{
				case ImageAnchor.TopLeft:
					fx = 0f; fy = 0f; break;
				case ImageAnchor.TopCenter:
					fx = 0.5f; fy = 0f; break;
				case ImageAnchor.TopRight:
					fx = 1f; fy = 0f; break;
				case ImageAnchor.MiddleLeft:
					fx = 0f; fy = 0.5f; break;
				case ImageAnchor.MiddleRight:
					fx = 1f; fy = 0.5f; break;
				case ImageAnchor.BottomLeft:
					fx = 0f; fy = 1f; break;
				case ImageAnchor.BottomCenter:
					fx = 0.5f; fy = 1f; break;
				case ImageAnchor.BottomRight:
					fx = 1f; fy = 1f; break;
				default:
					fx = 0.5f; fy = 0.5f; break;
			}
		}

		private static bool ContainsRect(Rectangle outer, Rectangle inner)
		{
			return inner.Left >= outer.Left - SizeEpsilon
					&& inner.Top >= outer.Top - SizeEpsilon
					&& inner.Right <= outer.Right + SizeEpsilon
					&& inner.Bottom <= outer.Bottom + SizeEpsilon;
		}
	}
}
