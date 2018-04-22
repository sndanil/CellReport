using System;
using System.Collections.Generic;
using System.Linq;
using CellReport.Core;
using CellReport.Core.Contents;

namespace CellReport.Presenters
{
	public delegate void ActionHandler(Cell cell, ActionContent content, ActionType type);

	public enum PresenterState
	{
		Normal,
		ColumnResize,
		RowResize
	}

	public abstract class BaseDisplayPresenter : BasePresenter
	{
		private bool _showToolBar = true;
		private PresenterState _state = PresenterState.Normal;

		private CellLocation _currentCell;
		private CellLocation _startSelection = CellLocation.None;
		private CellLocation _endSelection = CellLocation.None;

		protected BaseDisplayPresenter(CellManager manager)
				: base(manager)
		{
		}

		protected override void OnManagerChanging()
		{
			base.OnManagerChanging();
			if (Manager != null)
				Manager.PropertyChanged -= ManagerPropertyChanged;
		}

		protected override void OnManagerChanged()
		{
			base.OnManagerChanged();

			FrozenColumnsCount = Manager.DisplaySettings.FrozenColumns;
			FrozenRowsCount = Manager.DisplaySettings.FrozenRows;
			Scale = Manager.DisplaySettings.Scale;

			Manager.PropertyChanged += ManagerPropertyChanged;
		}

		private void ManagerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (sender is DisplaySettings)
			{
				Update();
				return;
			}

			if (sender is CellManager)
			{
				switch (e.PropertyName)
				{
					case "RowsCount":
					case "ColumnsCount":
					case "RowHeight":
					case "ColumnWidth":
						Update();
						break;
				}
			}
		}

		public override bool ShowGridLines
		{
			get => Manager.DisplaySettings.ShowGridLines;
			set => Manager.DisplaySettings.ShowGridLines = value;
		}

		public override bool ShowBookMarks
		{
			get => Manager.DisplaySettings.ShowBookMarks;
			set => Manager.DisplaySettings.ShowBookMarks = value;
		}

		public override string GridColor
		{
			get => Manager.DisplaySettings.GridColor;
			set => Manager.DisplaySettings.GridColor = value;
		}

		public bool ShowResizers
		{
			get => Manager.DisplaySettings.ShowResizers;
			set => Manager.DisplaySettings.ShowResizers = value;
		}

		public bool ShowToolBar
		{
			get => _showToolBar;
			set
			{
				_showToolBar = value;
				ShowToolBarChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public PresenterState State
		{
			get => _state;
			set
			{
				if (_state != value)
				{
					_state = value;
					OnPropertyChanged("State");
				}
			}
		}

		public override float Scale
		{
			get => Manager.DisplaySettings.Scale;
			set => Manager.DisplaySettings.Scale = value;
		}

		public CellLocation CurrentCell
		{
			get => _currentCell;
			set
			{
				var row = value.Row < 0 ? 0 : (value.Row >= Manager.RowsCount ? Manager.RowsCount - 1 : value.Row);
				var column = value.Column < 0 ? 0 : (value.Column >= Manager.ColumnsCount ? Manager.ColumnsCount - 1 : value.Column);
				if (_currentCell.Row != row || _currentCell.Column != column)
				{
					var bounds = GetHostBounds();
					var rightBottomLocation = GetCellByDevicePoint(bounds.Width, bounds.Height, true);
					if (column >= FrozenColumnsCount)
					{
						if (column < LeftColumn)
							LeftColumn = column;
						else
						{
							while (column > rightBottomLocation.Column && rightBottomLocation.Column > 0)
							{
								var oldLeft = LeftColumn;
								LeftColumn++;
								if (oldLeft == LeftColumn)
									break;
								rightBottomLocation = GetCellByDevicePoint(bounds.Width, bounds.Height, true);
							}
						}
					}
					if (row >= FrozenRowsCount)
					{
						BeginUpdate();
						if (row < TopRow)
							TopRow = row;
						else
						{
							while (row > rightBottomLocation.Row && rightBottomLocation.Row > 0)
							{
								var oldTop = TopRow;
								TopRow++;
								if (oldTop == TopRow)
									break;
								rightBottomLocation = GetCellByDevicePoint(bounds.Width, bounds.Height, true);
							}
						}
						EndUpdate();
					}
					_currentCell.Row = row;
					_currentCell.Column = column;
					StartSelection = CellLocation.None;
					EndSelection = CellLocation.None;
					OnPropertyChanged("CurrentCell");
					Update();
				}
			}
		}

		public bool IsSelectionMode { get; set; }

		public CellLocation StartSelection
		{
			get => _startSelection;
			set
			{
				if (!_startSelection.Equals(value) && (Manager.IsCurrectLocation(value) || value.Equals(CellLocation.None)))
				{
					_startSelection = value;
					OnPropertyChanged("StartSelection");
				}
			}
		}

		public CellLocation EndSelection
		{
			get => _endSelection;
			set
			{
				if (!_endSelection.Equals(value) && Manager.IsCurrectLocation(value) || value.Equals(CellLocation.None))
				{
					_endSelection = value;
					OnPropertyChanged("EndSelection");
				}
			}
		}

		public bool HaveSelection()
		{
			return !StartSelection.Equals(CellLocation.None) && !EndSelection.Equals(CellLocation.None);
		}

		public void ClearSelection()
		{
			StartSelection = EndSelection = CellLocation.None;
		}

		public bool IsCurreCellInFrozen()
		{
			return FrozenColumnsCount > CurrentCell.Column || FrozenRowsCount > CurrentCell.Row;
		}

		protected virtual void SetClip(object context, Rectangle clip)
		{
		}

		protected void RenderFrozenCells(object context, Rectangle bounds)
		{
			var width = GetLeftOffset(FrozenColumnsCount);
			var height = GetTopOffset(FrozenRowsCount);
			SetClip(context, new Rectangle(0, 0, width, height + 1));
			RenderCellsRange(context, bounds, 0, FrozenRowsCount, 0, FrozenColumnsCount);

			SetClip(context, new Rectangle(width, 0, bounds.Right - width, height + 1));
			RenderCellsRange(context, bounds, 0, FrozenRowsCount, LeftNoFrozenColumn, Manager.ColumnsCount);

			SetClip(context, new Rectangle(0, height, width + 1, bounds.Bottom - height));
			RenderCellsRange(context, bounds, TopNoFrozenRow, Manager.RowsCount, 0, FrozenColumnsCount);
		}

		protected CellLocation RenderCellsRange(object context, Rectangle bounds, int fromRow, int toRow, int fromCol, int toCol)
		{
			var rightBottom = new CellLocation { Row = fromRow, Column = fromCol };

			var alreadyPaintedCells = new Dictionary<Cell, bool>();
			for (int row = fromRow; row < toRow; row++)
			{
				float top = GetTopOffset(row);
				rightBottom.Row = row;
				for (int col = fromCol; col < toCol; col++)
				{
					float left = GetLeftOffset(col);
					rightBottom.Column = col;
					if (left > bounds.Right)
						break;
					var cell = Manager.GetCell(row, col);
					if (cell != null)
					{
						if (!alreadyPaintedCells.ContainsKey(cell))
						{
							alreadyPaintedCells.Add(cell, true);
							RenderGrid(context, GetCellRect(cell));
							RenderCellBackgroung(context, cell);
						}
					}
					else
						RenderGrid(context, GetCellRect(new CellLocation() { Column = col, Row = row }));
				}
				if (top > bounds.Bottom)
					break;
			}
			alreadyPaintedCells.Clear();
			for (int row = fromRow; row < toRow; row++)
			{
				float top = GetTopOffset(row);
				for (int col = fromCol; col < toCol; col++)
				{
					float left = GetLeftOffset(col);
					if (left > bounds.Right)
						break;
					var cell = Manager.GetCell(row, col);
					if (cell != null)
					{
						if (!alreadyPaintedCells.ContainsKey(cell))
						{
							alreadyPaintedCells.Add(cell, true);
							RenderCell(context, cell);
						}
					}
				}
				if (top > bounds.Bottom)
					break;
			}

			return rightBottom;
		}

		protected abstract Rectangle GetHostBounds();

		protected void MoveCursorLeft()
		{
			var cell = Manager.GetCell(CurrentCell);
			if (cell == null || cell.Span.ColSpan == 0)
				CurrentCell = new CellLocation() { Row = CurrentCell.Row, Column = CurrentCell.Column - 1 };
			else
			{
				CurrentCell = new CellLocation()
				{
					Row = CurrentCell.Row,
					Column = cell.Location.Column - 1
				};
			}
		}

		protected void MoveCursotRight()
		{
			var cell = Manager.GetCell(CurrentCell);
			if (cell == null || cell.Span.ColSpan == 0)
				CurrentCell = new CellLocation() { Row = CurrentCell.Row, Column = CurrentCell.Column + 1 };
			else
			{
				CurrentCell = new CellLocation()
				{
					Row = CurrentCell.Row,
					Column = cell.Location.Column + cell.Span.ColSpan + 1
				};
			}
		}

		protected void MoveCursorUp()
		{
			var cell = Manager.GetCell(CurrentCell);
			if (cell == null || cell.Span.RowSpan == 0)
				CurrentCell = new CellLocation() { Row = CurrentCell.Row - 1, Column = CurrentCell.Column };
			else
			{
				CurrentCell = new CellLocation()
				{
					Row = cell.Location.Row - 1,
					Column = CurrentCell.Column
				};
			}
		}

		protected void MoveCursorDown()
		{
			var cell = Manager.GetCell(CurrentCell);
			if (cell == null || cell.Span.RowSpan == 0)
				CurrentCell = new CellLocation() { Row = CurrentCell.Row + 1, Column = CurrentCell.Column };
			else
			{
				CurrentCell = new CellLocation()
				{
					Row = cell.Location.Row + cell.Span.RowSpan + 1,
					Column = CurrentCell.Column
				};
			}
		}

		protected void MovePageUp()
		{
			var bounds = GetHostBounds();
			var bottomCellLocation = GetCellByDevicePoint(bounds.Width, bounds.Height, true);
			bottomCellLocation.Row = bottomCellLocation.Row >= 0 ? bottomCellLocation.Row : Manager.RowsCount - 1;
			CurrentCell = new CellLocation()
			{
				Column = CurrentCell.Column,
				Row = CurrentCell.Row - (bottomCellLocation.Row - TopRow)
			};
		}

		protected void MovePageDown()
		{
			var bounds = GetHostBounds();
			var bottomCellLocation = GetCellByDevicePoint(bounds.Width, bounds.Height, true);
			bottomCellLocation.Row = bottomCellLocation.Row >= 0 ? bottomCellLocation.Row : Manager.RowsCount - 1;
			CurrentCell = new CellLocation()
			{
				Column = CurrentCell.Column,
				Row = CurrentCell.Row + (bottomCellLocation.Row - TopRow)
			};
		}

		protected void MoveToLastColumn()
		{
			CurrentCell = new CellLocation() { Column = Manager.ColumnsCount - 1, Row = CurrentCell.Row };
		}

		protected void MoveToFirstColumn()
		{
			CurrentCell = new CellLocation() { Column = 0, Row = CurrentCell.Row };
			LeftColumn = 0;
		}

		protected void MoveToLastRow()
		{
			CurrentCell = new CellLocation() { Column = CurrentCell.Column, Row = Manager.RowsCount - 1 };
		}

		protected void MoveToFirstRow()
		{
			CurrentCell = new CellLocation() { Column = CurrentCell.Column, Row = 0 };
			TopRow = 0;
		}

		public void RaiseClick(object sender, EventArgs e, float x, float y)
		{
			DoAction(GetCellByDevicePoint(x, y, false), ActionType.MouseLeftClick);
			OnClick(sender, e);
		}

		public void RaiseDblClick(object sender, EventArgs e, float x, float y)
		{
			DoAction(GetCellByDevicePoint(x, y, false), ActionType.MouseDblClick);
			OnDblClick(sender, e);
		}

		public bool RaiseProcessKey(object sender, EventArgs e)
		{
			return OnProcessKey(sender, e);
		}

		protected void DoAction(CellLocation location, ActionType type)
		{
			if (location.Row >= 0 && location.Row < Manager.RowsCount
					&& location.Column >= 0 && location.Column < Manager.ColumnsCount)
			{
				var cell = Manager.GetCell(location);
				if (cell != null)
				{
					if (cell.Content is ActionContent)
						DoAction(cell, (ActionContent)cell.Content, type);
					else if (cell.Content is CollectionContent)
					{
						foreach (var content in ((CollectionContent)cell.Content).GetContents())
							if (content is ActionContent)
								DoAction(cell, (ActionContent)content, type);
					}
				}
			}
		}

		private object GetContentData(BaseContent content)
		{
			if (content is TextBasedContent)
				return ((TextBasedContent)content).GetText();
			if (content is ImageContent)
				return ((ImageContent)content).GetImageData();
			if (content is BarCodeContent)
				return ((BarCodeContent)content).GetCode();

			return null;
		}

		private object GetCellData(Cell cell)
		{
			if (cell != null)
			{
				if (cell.Content is CollectionContent)
				{
					foreach (var content in ((CollectionContent)cell.Content).GetContents())
					{
						var data = GetContentData(content);
						if (data != null)
							return data;
					}
				}
				else
					return GetContentData(cell.Content);
			}
			return null;
		}

		protected object GetCurrentForClipBoard()
		{
			var cell = Manager.GetCell(CurrentCell);
			return GetCellData(cell);
		}

		protected string GetSelectionForClipboard()
		{
			if (!HaveSelection())
			{
				var cell = Manager.GetCell(CurrentCell);
				return GetCellData(cell) as string;
			}

			var start = GetStartSelection();
			var end = GetEndSelection();
			var data = new string[end.Row - start.Row + 1][];
			var processedCells = new Dictionary<Cell, bool>();
			for (int row = start.Row; row <= end.Row; row++)
			{
				data[row - start.Row] = new string[end.Column - start.Column + 1];
				for (int col = start.Column; col <= end.Column; col++)
				{
					var cell = Manager.GetCell(row, col);
					if ((cell == null) || processedCells.ContainsKey(cell))
						continue;

					data[row - start.Row][col - start.Column] = GetCellData(cell) as string;
					processedCells.Add(cell, true);
				}
			}

			return string.Join(Environment.NewLine, data.Select(x => string.Join("\t", x)).ToArray());
		}

		private void DoAction(Cell cell, ActionContent content, ActionType type)
		{
			if ((content.Type == ActionType.All || content.Type == type) && Action != null)
				Action(cell, content, type);
		}

		protected void RenderCurrentCell(object context, Rectangle bounds)
		{
			var rect = GetCellRect(CurrentCell);
			if (Manager.DisplaySettings.ShowCurrentRows)
				Renderer.RenderFillRect(context, new Rectangle(0, rect.Y, bounds.Width, rect.Height), 0, new[] { "#38B7DBFF" });
			Renderer.RenderLines(context, "#DC0000FF", 3, LineStyle.Solid,
					new[] {
										new Point(rect.Right - 3, rect.Bottom),
										new Point(rect.Left, rect.Bottom),
										new Point(rect.Left, rect.Top),
										new Point(rect.Right, rect.Top),
										new Point(rect.Right, rect.Bottom - 3)
					});
			//Renderer.RenderRect(context, rect, "#DC0000FF", LineStyle.Solid, 3);
			Renderer.RenderRect(context, new Rectangle(rect.Right - 1, rect.Bottom - 1, 3, 3), "#DC0000FF", LineStyle.Solid, 2);
		}

		public CellLocation GetStartSelection()
		{
			return new CellLocation() { Row = Math.Min(StartSelection.Row, EndSelection.Row), Column = Math.Min(StartSelection.Column, EndSelection.Column) };
		}

		public CellLocation GetEndSelection()
		{
			return new CellLocation() { Row = Math.Max(StartSelection.Row, EndSelection.Row), Column = Math.Max(StartSelection.Column, EndSelection.Column) };
		}

		protected void RenderSelection(object context)
		{
			if (!StartSelection.Equals(CellLocation.None) && !EndSelection.Equals(CellLocation.None))
			{
				var startRect = GetCellRect(GetStartSelection());
				var endRect = GetCellRect(GetEndSelection());
				Renderer.RenderFillRect(context, new Rectangle(startRect.X, startRect.Y, endRect.Width + (endRect.X - startRect.X),
						endRect.Height + (endRect.Y - startRect.Y)), 0, new[] { "#A0B7DBFF" });
			}
		}

		public virtual void Render(object sender, object context)
		{
		}

		public virtual void RenderPager(object sender, object context, Pager pager)
		{
		}

		protected virtual void OnClick(object sender, EventArgs e) { }
		protected virtual void OnDblClick(object sender, EventArgs e) { }
		protected virtual bool OnProcessKey(object sender, EventArgs e) { return false; }

		public event EventHandler ShowToolBarChanged;
		public event ActionHandler Action;
	}
}
