using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CellReport.Core;
using CellReport.Core.Contents;

namespace CellReport.Presenters
{
	public abstract class BasePresenter : NotifyPropertyChanged
	{
		private CellManager _manager = null;

		protected const GraphicsUnit DefaultUnit = GraphicsUnit.Millimeter;
		private GraphicsUnit _currentUnit = DefaultUnit;
		private float _deviceDpiX = 0;
		private float _deviceDpiY = 0;
		private float _deviceOffsetX = 0;
		private float _deviceOffsetY = 0;

		private int _topRow = 0;
		private int _leftColumn = 0;
		private int _frozenRowsCount = 0;
		private int _frozenColumnsCount = 0;
		private bool _updateMode = false;

		private Dictionary<int, float> _topOffsets = new Dictionary<int, float>();
		private Dictionary<int, float> _leftOffsets = new Dictionary<int, float>();

		public BasePresenter(CellManager manager)
		{
			Manager = manager;
		}

		public void BeginUpdate()
		{
			_updateMode = true;
		}

		public void EndUpdate()
		{
			_updateMode = false;
			Update();
		}

		public virtual void Update()
		{
		}

		protected bool IsInUpdateMode()
		{
			return _updateMode;
		}

		public BaseRenderer Renderer
		{
			get;
			set;
		}

		protected void ProcessFit()
		{
			if (!_manager.AutoFitted && !CellManager.IsDesignMode)
			{
				var alreadyFit = new HashSet<Cell>();
				foreach (var row in _manager.GetAutoFitRows())
				{
					for (int col = 0; col < _manager.ActualColumnsCount; col++)
					{
						if (_manager.GetCell(row, col) != null && _manager.GetCell(row, col).Content != null && !alreadyFit.Contains(_manager.GetCell(row, col)))
						{
							alreadyFit.Add(_manager.GetCell(row, col));
							var content = _manager.GetCell(row, col).Content;
							if (content is TextBasedContent)
								FitCell(row, col, (TextBasedContent)content);
							else if (content is CollectionContent)
							{
								foreach (var item in ((CollectionContent)content).GetContents())
									if (item is TextBasedContent)
										FitCell(row, col, (TextBasedContent)item);
							}
						}
					}
				}
				alreadyFit.Clear();
				for (int i = 0; i < _manager.RowsCount; i++)
				{
					for (int j = 0; j < _manager.ColumnsCount; j++)
					{
						var cell = _manager.GetCell(i, j);
						if (cell != null && !alreadyFit.Contains(cell))
						{
							alreadyFit.Add(cell);
							var content = cell.Content;
							if (content is TextContent)
								FitCellFontSize(i, j, (TextContent)content);
							else if (content is CollectionContent)
							{
								foreach (var item in ((CollectionContent)content).GetContents())
									if (item is TextContent)
										FitCellFontSize(i, j, (TextContent)item);
							}
						}
					}
				}
				_manager.AutoFitted = true;
			}
		}

		private void FitCellFontSize(int row, int col, TextContent content)
		{
			if (!content.AutoSizeFont)
				return;

			BeginUpdate();
			var text = content.GetTextForRender();
			var style = (TextContentStyle)content.GetActualStyle();
			if (style.Font.Size - 1 <= 0)
				return;

			var outerCellRect = GetCellRect(_manager.GetCell(row, col));
			var cellRect = GetInnerCellRect(_manager.GetCell(row, col));
			var size = GetTextSize(cellRect, text, style.Font.Family, style.Font.Size, style.Font.Style);
			var delta = outerCellRect.Height - cellRect.Height;
			if (cellRect.Height < size.Height)
			{
				if (content.GetParamValueByTarget("Style.Font.Size") == null)
					content.AddParam(new Param(_manager, content) { Target = "Style.Font.Size", Name = "Style.Font.Size" });
				content.SetContentParam("Style.Font.Size", style.Font.Size - 1);
				FitCellFontSize(row, col, content);
			}
			EndUpdate();
		}

		private void FitCell(int row, int col, TextBasedContent content)
		{
			BeginUpdate();
			var text = content.GetTextForRender();
			var style = (TextContentStyle)content.GetActualStyle();
			var outerCellRect = GetCellRect(_manager.GetCell(row, col));
			var cellRect = GetInnerCellRect(_manager.GetCell(row, col));
			var size = GetTextSize(cellRect, text, style.Font.Family, style.Font.Size, style.Font.Style);
			var delta = outerCellRect.Height - cellRect.Height;
			if (cellRect.Height < size.Height)
				_manager.SetRowHeight(row, UnitsConverter.ConvertValue(size.Height + delta, GraphicsUnit.Pixel, GraphicsUnit.Millimeter, DeviceDpiX));
			EndUpdate();
		}

		protected abstract Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle);

		protected virtual void OnManagerChanging()
		{
		}

		protected virtual void OnManagerChanged()
		{
		}

		public CellManager Manager
		{
			get { return _manager; }
			set
			{
				if (_manager != value)
				{
					OnManagerChanging();
					_manager = value;
					OnManagerChanged();
				}
			}
		}

		protected float DeviceOffsetX
		{
			get { return _deviceOffsetX; }
			set { _deviceOffsetX = value; }
		}

		protected float DeviceOffsetY
		{
			get { return _deviceOffsetY; }
			set { _deviceOffsetY = value; }
		}

		public abstract float Scale
		{
			get;
			set;
		}

		public int FrozenColumnsCount
		{
			get { return _frozenColumnsCount; }
			set
			{
				if (_manager.ColumnsCount > value)
					_frozenColumnsCount = value;
			}
		}

		public int FrozenRowsCount
		{
			get { return _frozenRowsCount; }
			set
			{
				if (_manager.RowsCount > value)
					_frozenRowsCount = value; ;
			}
		}

		public int LeftColumn
		{
			get { return _leftColumn; }
			set
			{
				var leftColumn = value < 0 ? 0 : (value >= _manager.ColumnsCount ? _manager.ColumnsCount - 1 : value);
				if (leftColumn != _leftColumn)
				{
					_leftColumn = leftColumn;
					Update();
				}
			}
		}

		public int TopRow
		{
			get { return _topRow; }
			set
			{
				var topRow = value < 0 ? 0 : (value >= _manager.RowsCount ? _manager.RowsCount - 1 : value);
				if (topRow != _topRow)
				{
					_topRow = topRow;
					Update();
				}
			}
		}

		public int LeftNoFrozenColumn
		{
			get { return _leftColumn < _frozenColumnsCount ? _frozenColumnsCount : _leftColumn; }
		}

		public int TopNoFrozenRow
		{
			get { return _topRow < _frozenRowsCount ? _frozenRowsCount : _topRow; }
		}

		public float DeviceDpiY
		{
			get { return _deviceDpiY; }
			set { _deviceDpiY = value; }
		}

		public float DeviceDpiX
		{
			get { return _deviceDpiX; }
			set { _deviceDpiX = value; }
		}

		public GraphicsUnit CurrentUnit
		{
			get { return _currentUnit; }
			set { _currentUnit = value; }
		}

		public abstract bool ShowGridLines { get; set; }
		public abstract bool ShowBookMarks { get; set; }

		public abstract string GridColor { get; set; }

		public Rectangle GetCellRect(Cell cell)
		{
			var rect = new Rectangle(GetLeftOffset(cell.Location.Column, DefaultUnit),
					GetTopOffset(cell.Location.Row, DefaultUnit),
					cell.Size.Width * Scale, cell.Size.Height * Scale);
			if (CurrentUnit != DefaultUnit)
			{
				rect = new Rectangle(
						UnitsConverter.ConvertValue(rect.X, DefaultUnit, CurrentUnit, DeviceDpiX),
						UnitsConverter.ConvertValue(rect.Y, DefaultUnit, CurrentUnit, DeviceDpiY),
						UnitsConverter.ConvertValue(rect.Width, DefaultUnit, CurrentUnit, DeviceDpiX),
						UnitsConverter.ConvertValue(rect.Height, DefaultUnit, CurrentUnit, DeviceDpiY));
			}

			return rect;
		}

		public Rectangle GetCellRect(CellLocation location)
		{
			if (Manager.GetCell(location) != null)
				return GetCellRect(Manager.GetCell(location));
			else
			{
				return new Rectangle(GetLeftOffset(location.Column), GetTopOffset(location.Row),
						GetColumnWidth(location.Column), GetRowHeight(location.Row));
			}
		}

		public Rectangle GetInnerCellRect(Cell cell)
		{
			var rect = new Rectangle(GetLeftOffset(cell.Location.Column, DefaultUnit),
					GetTopOffset(cell.Location.Row, DefaultUnit),
					cell.Size.Width * Scale, cell.Size.Height * Scale);
			rect.X += cell.Style.Paddings.Left * Scale;
			rect.Y += cell.Style.Paddings.Top * Scale;
			rect.Width -= (cell.Style.Paddings.Right + cell.Style.Paddings.Left) * Scale;
			rect.Height -= (cell.Style.Paddings.Bottom + cell.Style.Paddings.Top) * Scale;
			if (CurrentUnit != DefaultUnit)
			{
				rect = new Rectangle(
						UnitsConverter.ConvertValue(rect.X, DefaultUnit, CurrentUnit, DeviceDpiX),
						UnitsConverter.ConvertValue(rect.Y, DefaultUnit, CurrentUnit, DeviceDpiY),
						UnitsConverter.ConvertValue(rect.Width, DefaultUnit, CurrentUnit, DeviceDpiX),
						UnitsConverter.ConvertValue(rect.Height, DefaultUnit, CurrentUnit, DeviceDpiY));
			}

			return rect;
		}

		public float ConvertToDeviceX(float value)
		{
			return ConvertToDeviceX(value, DefaultUnit);
		}

		public float ConvertToDeviceY(float value)
		{
			return ConvertToDeviceY(value, DefaultUnit);
		}

		public float ConvertToDeviceX(float value, CellReport.Core.GraphicsUnit fromUnit)
		{
			return UnitsConverter.ConvertValue(value, fromUnit, CurrentUnit, DeviceDpiX) * Scale;
		}

		public float ConvertToDeviceY(float value, CellReport.Core.GraphicsUnit fromUnit)
		{
			return UnitsConverter.ConvertValue(value, fromUnit, CurrentUnit, DeviceDpiY) * Scale;
		}

		public float GetTopOffset(int row, CellReport.Core.GraphicsUnit unit)
		{
			float offset = 0;
			if (_topOffsets == null || !_topOffsets.TryGetValue(row, out offset))
			{
				for (int rowIndex = 0; rowIndex < FrozenRowsCount && rowIndex < row; rowIndex++)
					offset += Manager.GetRowHeight(rowIndex);
				for (int rowIndex = TopNoFrozenRow; rowIndex < row; rowIndex++)
					offset += Manager.GetRowHeight(rowIndex);
				for (int rowIndex = row; rowIndex < TopNoFrozenRow && (rowIndex > FrozenRowsCount || FrozenRowsCount == 0); rowIndex++)
					offset -= Manager.GetRowHeight(rowIndex);

				if (_topOffsets != null)
					_topOffsets.Add(row, offset);
			}
			if (DefaultUnit != unit)
				offset = UnitsConverter.ConvertValue(offset, DefaultUnit, CurrentUnit, DeviceDpiY);

			return offset * Scale + UnitsConverter.ConvertValue(DeviceOffsetY, CurrentUnit, unit, DeviceDpiY);
		}

		public float GetTopOffset(int row)
		{
			return GetTopOffset(row, CurrentUnit);
		}

		public float GetLeftOffset(int column, CellReport.Core.GraphicsUnit unit)
		{
			float offset = 0;
			if (_leftOffsets == null || !_leftOffsets.TryGetValue(column, out offset))
			{
				for (int columnIndex = 0; columnIndex < FrozenColumnsCount && columnIndex < column; columnIndex++)
					offset += Manager.GetColumnWidth(columnIndex);
				for (int columnIndex = LeftNoFrozenColumn; columnIndex < column; columnIndex++)
					offset += Manager.GetColumnWidth(columnIndex);
				for (int columnIndex = column; columnIndex < LeftNoFrozenColumn && (columnIndex > FrozenColumnsCount || FrozenColumnsCount == 0); columnIndex++)
					offset -= Manager.GetColumnWidth(columnIndex);

				if (_leftOffsets != null)
					_leftOffsets.Add(column, offset);
			}

			if (DefaultUnit != unit)
				offset = UnitsConverter.ConvertValue(offset, DefaultUnit, CurrentUnit, DeviceDpiX);

			return offset * Scale + UnitsConverter.ConvertValue(DeviceOffsetX, CurrentUnit, unit, DeviceDpiX);
		}

		public float GetLeftOffset(int column)
		{
			return GetLeftOffset(column, CurrentUnit);
		}

		public float GetColumnWidth(int col)
		{
			float width = Manager.GetColumnWidth(col);
			if (CurrentUnit != DefaultUnit)
				width = UnitsConverter.ConvertValue(width, DefaultUnit, CurrentUnit, DeviceDpiX);

			return width * Scale;
		}

		public void SetColumnWidth(int column, float width)
		{
			if (CurrentUnit != DefaultUnit)
				width = UnitsConverter.ConvertValue(width, CurrentUnit, DefaultUnit, DeviceDpiX);

			Manager.SetColumnWidth(column, width / Scale);
		}

		public float GetRowHeight(int row)
		{
			float height = Manager.GetRowHeight(row);
			if (CurrentUnit != DefaultUnit)
				height = UnitsConverter.ConvertValue(height, DefaultUnit, CurrentUnit, DeviceDpiY);

			return height * Scale;
		}

		public void SetRowHeight(int row, float height)
		{
			if (CurrentUnit != DefaultUnit)
				height = UnitsConverter.ConvertValue(height, CurrentUnit, DefaultUnit, DeviceDpiY);

			Manager.SetRowHeight(row, height / Scale);
		}

		//public float GetColumnsTotalWidth()
		//{
		//    float result = 0;
		//    for (int col = 0; col < Manager.ColumnsCount; col++)
		//        result += Manager.GetColumnWidth(col);

		//    return UnitsConverter.ConvertValue(result, DefaultUnit, CurrentUnit, DeviceDpiX) * Scale;
		//}

		/// <summary>
		/// Return total width of actual columns in the current unit without scale
		/// </summary>
		/// <returns></returns>
		public float GetActualColumnsTotalWidth()
		{
			float result = 0;
			for (int col = 0; col < Manager.ActualColumnsCount; col++)
				result += Manager.GetColumnWidth(col);

			return UnitsConverter.ConvertValue(result, DefaultUnit, CurrentUnit, DeviceDpiX);
		}

		public IEnumerable<int> GetColumnIndexes()
		{
			for (int column = 0; column < FrozenColumnsCount; column++)
				yield return column;
			for (int column = LeftNoFrozenColumn; column < Manager.ColumnsCount; column++)
				yield return column;
		}

		public IEnumerable<int> GetRowIndexes()
		{
			for (int row = 0; row < FrozenRowsCount; row++)
				yield return row;
			for (int row = TopNoFrozenRow; row < Manager.RowsCount; row++)
				yield return row;
		}

		public CellLocation GetCellByDevicePoint(float x, float y, bool fullVisible)
		{
			var column = -1;
			foreach (var colIndex in GetColumnIndexes())
			{
				var left = GetLeftOffset(colIndex) + GetColumnWidth(colIndex);
				if (left >= x)
				{
					column = fullVisible ? colIndex - 1 : colIndex;
					break;
				}
			}
			var row = -1;
			foreach (var rowIndex in GetRowIndexes())
			{
				var top = GetTopOffset(rowIndex) + GetRowHeight(rowIndex);
				if (top >= y)
				{
					row = fullVisible ? rowIndex - 1 : rowIndex;
					break;
				}
			}
			return new CellLocation { Column = column, Row = row };
		}

		protected void BeginRender()
		{
			_topOffsets = new Dictionary<int, float>();
			_leftOffsets = new Dictionary<int, float>();
		}

		protected void EndRender()
		{
			_topOffsets = null;
			_leftOffsets = null;
		}

		protected void RenderCell(object context, Cell cell)
		{
			RenderCellBorder(context, cell);
			if (cell.Content != null)
				RenderContent(context, cell.Content, cell);
		}

		private void RenderContent(object context, BaseContent content, Cell cell)
		{
			var cellRect = GetInnerCellRect(cell);
			if (content is CollectionContent)
			{
				foreach (var item in ((CollectionContent)content).GetContents())
					RenderContent(context, item, cell);
			}
			else if (content is TextBasedContent)
			{
				var textContent = (TextBasedContent)content;
				var style = (TextContentStyle)textContent.GetActualStyle();
				var fontSize = Scale * style.Font.Size;
				Renderer.RenderText(context, cellRect, textContent.GetTextForRender(), textContent.GetColor(), style.HAlignment,
					style.VAlignment, style.Angle, style.Font.Family,
					fontSize, style.Font.Style, style.Direction);
			}
			else if (content is LineContent)
			{
				var lineContent = (LineContent)content;
				var style = (LineContentStyle)lineContent.GetStyle();
				float width = ConvertToDeviceX(style.Width);
				float startX = ConvertToDeviceX(lineContent.StartX);
				float startY = ConvertToDeviceX(lineContent.StartY);
				float endX = ConvertToDeviceX(lineContent.EndX);
				float endY = ConvertToDeviceX(lineContent.EndY);
				Renderer.RenderLine(context, style.Color, width, startX + cellRect.X, startY + cellRect.Y,
												endX + cellRect.X, endY + cellRect.Y);
			}
			else if (content is BarCodeContent)
			{
				var barCode = (BarCodeContent)content;
				Renderer.RenderBarCode(context, cellRect, barCode, "Black", "Arial", 10f, FontStyle.Regular, barCode.Orientation);
			}
			else if (content is ImageContent)
			{
				var image = (ImageContent)content;
				Renderer.RenderImage(context, cellRect, image.GetImageData());
			}
			else if (content is ChartContent)
			{
				var chart = (ChartContent)content;
				Renderer.RenderChart(context, cellRect, chart);
			}
		}

		protected virtual void RenderCellBackgroung(object context, Cell cell)
		{
			var cellRect = GetCellRect(cell);
			Renderer.RenderFillRect(context, cellRect, 0, cell.Style.Color);
		}

		protected virtual void RenderGrid(object context, Rectangle rect)
		{
			if (ShowGridLines || Manager.DisplaySettings.ShowDesignerGridLines && CellManager.IsDesignMode)
			{
				Renderer.RenderLines(context, GridColor, 1, LineStyle.Solid, new Point[]{
										new Point(rect.X, rect.Bottom),
										new Point(rect.Right, rect.Bottom),
										new Point(rect.Right, rect.Top) });
			}
		}

		protected virtual void RenderCellBorder(object context, Cell cell)
		{
			var cellRect = GetCellRect(cell);
			if (!cell.Style.Borders.IsEmpty())
			{
				var borders = cell.Style.Borders;
				if (cell.Style.Borders.IsRectangle())
				{
					var width = ConvertToDeviceX(borders.Top.Weight);
					Renderer.RenderRect(context, cellRect, borders.Top.Color, borders.Left.Style, width);
				}
				else
				{
					foreach (var chain in borders.GetChains())
					{
						var firstInChain = borders.GetFirstInChain(chain);
						if (firstInChain.Equals(Border.Empty))
							continue;
						float width = ConvertToDeviceX(firstInChain.Weight);
						Point[] points = null;
						switch (chain)
						{
							case Borders.BordersChain.LeftTopRight:
								points = new Point[] {
																		new Point(cellRect.Left, cellRect.Bottom), new Point(cellRect.Left, cellRect.Top),
																		new Point(cellRect.Right, cellRect.Top), new Point(cellRect.Right, cellRect.Bottom) };
								break;
							case Borders.BordersChain.TopRightBottom:
								points = new Point[] {
																		new Point(cellRect.Left, cellRect.Top), new Point(cellRect.Right, cellRect.Top),
																		new Point(cellRect.Right, cellRect.Bottom), new Point(cellRect.Left, cellRect.Bottom) };
								break;
							case Borders.BordersChain.RightBottomLeft:
								points = new Point[] {
																		new Point(cellRect.Left, cellRect.Top), new Point(cellRect.Left, cellRect.Bottom),
																		new Point(cellRect.Right, cellRect.Bottom), new Point(cellRect.Right, cellRect.Top) };
								break;
							case Borders.BordersChain.BottomLeftTop:
								points = new Point[] {
																		new Point(cellRect.Right, cellRect.Bottom), new Point(cellRect.Left, cellRect.Bottom),
																		new Point(cellRect.Left, cellRect.Top), new Point(cellRect.Right, cellRect.Top) };
								break;
							case Borders.BordersChain.LeftTop:
								points = new Point[] { new Point(cellRect.Left, cellRect.Bottom),
																		new Point(cellRect.Left, cellRect.Top), new Point(cellRect.Right, cellRect.Top) };
								break;
							case Borders.BordersChain.TopRight:
								points = new Point[] { new Point(cellRect.Left, cellRect.Top),
																		new Point(cellRect.Right, cellRect.Top), new Point(cellRect.Right, cellRect.Bottom) };
								break;
							case Borders.BordersChain.RightBottom:
								points = new Point[] { new Point(cellRect.Left, cellRect.Bottom),
																		new Point(cellRect.Right, cellRect.Bottom), new Point(cellRect.Right, cellRect.Top) };
								break;
							case Borders.BordersChain.LeftBotom:
								points = new Point[] { new Point(cellRect.Left, cellRect.Top),
																		new Point(cellRect.Left, cellRect.Bottom), new Point(cellRect.Right, cellRect.Bottom) };
								break;
							case Borders.BordersChain.Left:
								points = new Point[] { new Point(cellRect.Left, cellRect.Top),
																		new Point(cellRect.Left, cellRect.Bottom) };
								break;
							case Borders.BordersChain.Top:
								points = new Point[] { new Point(cellRect.Left, cellRect.Top),
																		new Point(cellRect.Right, cellRect.Top) };
								break;
							case Borders.BordersChain.Right:
								points = new Point[] { new Point(cellRect.Right, cellRect.Top),
																		new Point(cellRect.Right, cellRect.Bottom) };
								break;
							case Borders.BordersChain.Bottom:
								points = new Point[] { new Point(cellRect.Left, cellRect.Bottom),
																		new Point(cellRect.Right, cellRect.Bottom) };
								break;
						}
						if (points != null)
							Renderer.RenderLines(context, firstInChain.Color, width, firstInChain.Style, points);
					}
				}
			}
		}
	}
}
