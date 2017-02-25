using System;
using System.Collections.Generic;
using System.Linq;
using CellReport.Core;

namespace CellReport.Presenters
{
	public abstract class BasePrintPresenter : BasePresenter
	{
		private readonly EventHandler<DoCellsEventArgs> _emptyAction;
		private readonly EventHandler<DoCellsEventArgs> _renderGridAction;
		private readonly EventHandler<DoCellsEventArgs> _renderCellAction;

		protected class DoCellsEventArgs : EventArgs
		{
			public Cell Cell { get; set; }
			public int Row { get; set; }
			public int Col { get; set; }
			public object Context { get; set; }
			public Dictionary<Cell, bool> AlreadyPaintedCells { get; set; }
		}

		private float _scale = 1f;

		protected BasePrintPresenter(CellManager manager)
				: base(manager)
		{
			_emptyAction = (sender, e) =>
			{
				if (e.Cell != null && !e.AlreadyPaintedCells.ContainsKey(e.Cell))
					e.AlreadyPaintedCells.Add(e.Cell, true);
			};
			_renderGridAction = (sender, e) =>
			{
				if (e.Cell != null && !e.AlreadyPaintedCells.ContainsKey(e.Cell))
				{
					e.AlreadyPaintedCells.Add(e.Cell, true);
					RenderGrid(e.Context, GetCellRect(e.Cell));
					RenderCellBackgroung(e.Context, e.Cell);
				}
				else
					RenderGrid(e.Context, GetCellRect(new CellLocation { Column = e.Col, Row = e.Row }));
			};
			_renderCellAction = (sender, e) =>
			{
				if (e.Cell != null && !e.AlreadyPaintedCells.ContainsKey(e.Cell))
				{
					e.AlreadyPaintedCells.Add(e.Cell, true);
					RenderCell(e.Context, e.Cell);
				}
			};
		}

		public override float Scale
		{
			get { return _scale; }
			set { _scale = value; }
		}

		internal int CurrentPage
		{
			get;
			set;
		}

		public int CountOfPrintedPages
		{
			get;
			private set;
		}

		public int CalcCountOfPages { get; protected set; }

		public override string GridColor
		{
			get { return string.Empty; }
			set { }
		}

		public override bool ShowGridLines
		{
			get { return false; }
			set { }
		}

		public override bool ShowBookMarks
		{
			get { return false; }
			set { }
		}

		internal Pager GetCurrentHeader()
		{
			return GetCurrentPager(Manager.PrintSettings.Headers);
		}

		internal Pager GetCurrentFooter()
		{
			return GetCurrentPager(Manager.PrintSettings.Footers);
		}

		private Pager GetCurrentPager(IEnumerable<Pager> pagers)
		{
			return pagers.FirstOrDefault(pager => pager.FromPage <= CurrentPage && (pager.ToPage == 0 || pager.ToPage >= CurrentPage));
		}

		protected void BeforePrinting()
		{
			LeftColumn = 0;
			TopRow = 0;
			CountOfPrintedPages = 0;
			CurrentPage = 0;
			foreach (var pager in Manager.PrintSettings.Headers)
			{
				if (pager.Left != null)
					pager.Left.ResetExpressions();
				if (pager.Center != null)
					pager.Center.ResetExpressions();
				if (pager.Right != null)
					pager.Right.ResetExpressions();
			}
			foreach (var pager in Manager.PrintSettings.Footers)
			{
				if (pager.Left != null)
					pager.Left.ResetExpressions();
				if (pager.Center != null)
					pager.Center.ResetExpressions();
				if (pager.Right != null)
					pager.Right.ResetExpressions();
			}
		}

		protected abstract void OnPrint(bool useCustom);

		public virtual void Print()
		{
			BeforePrinting();
			OnPrint(false);
		}

		public virtual void CustomPrint()
		{
			BeforePrinting();
			OnPrint(true);
		}

		public abstract void ShowCustomPage();

		public abstract void ShowPreviewDialog();

		protected float GetCurrentPageWidth(Rectangle bounds)
		{
			float width = 0;
			int col;
			for (col = LeftColumn; col < Manager.ActualColumnsCount; col++)
			{
				if (Math.Round((width + GetColumnWidth(col))) > Math.Round(bounds.Width))
					break;
				width += GetColumnWidth(col);
			}

			col = col == Manager.ActualColumnsCount ? col - 1 : col;
			float maxWeight = 0;
			for (int row = TopRow; row < Manager.ActualRowsCount; row++)
			{
				if ((GetTopOffset(row) + GetRowHeight(row)) > bounds.Bottom)
					break;
				if (Manager.GetCell(row, col) != null)
				{
					var weight = UnitsConverter.ConvertValue(Manager.GetCell(row, col).Style.Borders.Right.Weight,
							DefaultUnit, CurrentUnit, DeviceDpiY);
					maxWeight = Math.Max(weight * 2, maxWeight);
				}
			}

			return width + maxWeight;
		}

		protected CellLocation DoCellsRange(object context, Dictionary<Cell, bool> alreadyPaintedCells, Rectangle bounds, EventHandler<DoCellsEventArgs> action)
		{
			var rightBottom = new CellLocation { Row = TopRow, Column = LeftColumn };

			for (int row = TopRow; row < Manager.ActualRowsCount; row++)
			{
				float top = GetTopOffset(row);
				if (Math.Round(top + GetRowHeight(row)) > Math.Round(bounds.Bottom) && row != TopRow)
					break;
				for (int col = LeftColumn; col < Manager.ActualColumnsCount; col++)
				{
					float left = GetLeftOffset(col);
					if (Math.Round((left + GetColumnWidth(col))) > Math.Round(bounds.Right))
						break;
					rightBottom.Column = col;
					var cell = Manager.GetCell(row, col);
					if (action != null)
						action(this, new DoCellsEventArgs { Cell = cell, Row = row, Col = col, Context = context, AlreadyPaintedCells = alreadyPaintedCells });
				}
				if (Math.Round(top + GetRowHeight(row)) > Math.Round(bounds.Bottom))
					break;
				rightBottom.Row = row;
				if (Manager.IsPageBreakRow(row))
					break;
			}

			return rightBottom;
		}

		protected CellLocation RenderCellsRange(object context, Rectangle bounds, EventHandler<DoCellsEventArgs> renderGridAction, EventHandler<DoCellsEventArgs> renderCellAction)
		{
			var alreadyPaintedCells = new Dictionary<Cell, bool>();
			var rightBottom = DoCellsRange(context, alreadyPaintedCells, bounds, renderGridAction);
			alreadyPaintedCells.Clear();
			DoCellsRange(context, alreadyPaintedCells, bounds, renderCellAction);

			return rightBottom;
		}

		protected void SetExpressionPrintMode(bool enable)
		{
			foreach (var expression in Manager.GetExpressions())
				expression.PrintMode = enable;
		}

		protected void ExecuteExpressions(CellLocation start, CellLocation end)
		{
			foreach (var expression in Manager.GetExpressions())
			{
				var pagesCountExpr = expression as Core.Expressions.PagesCountExpression;
				if (pagesCountExpr != null)
					pagesCountExpr.SetValue(CalcCountOfPages);
				var sumExpr = expression as Core.Expressions.SumExpression;
				if (sumExpr != null)
					sumExpr.SetRange(start, end);
			}
			foreach (var pager in Manager.PrintSettings.Headers)
			{
				if (pager.Left != null)
					pager.Left.ExecuteExpressions();
				if (pager.Center != null)
					pager.Center.ExecuteExpressions();
				if (pager.Right != null)
					pager.Right.ExecuteExpressions();
			}
			foreach (var pager in Manager.PrintSettings.Footers)
			{
				if (pager.Left != null)
					pager.Left.ExecuteExpressions();
				if (pager.Center != null)
					pager.Center.ExecuteExpressions();
				if (pager.Right != null)
					pager.Right.ExecuteExpressions();
			}
			CountOfPrintedPages++;
		}

		protected void RecalcScale(Rectangle bounds)
		{
			if (Manager.PrintSettings.AutoFitWidthPages > 0)
			{
				var totalWidth = GetActualColumnsTotalWidth();
				if (totalWidth > bounds.Width)
					Scale = bounds.Width * Manager.PrintSettings.AutoFitWidthPages / totalWidth;
			}
			else
				Scale = Manager.PrintSettings.Scale;
		}

		protected Rectangle RenderHeaderAndFooter(object context, Rectangle bounds, EventHandler<DoCellsEventArgs> renderGridAction, EventHandler<DoCellsEventArgs> renderCellAction)
		{
			var header = GetCurrentHeader();
			if (header != null)
			{
				Renderer.RenderPager(context, header, new Rectangle(bounds.X, bounds.Y, bounds.Width, ConvertToDeviceY(header.Height)), Scale);
				bounds.Y += ConvertToDeviceY(header.Height);
				bounds.Height -= ConvertToDeviceY(header.Height);
			}
			var footer = GetCurrentFooter();
			if (footer != null)
			{
				Renderer.RenderPager(context, footer, new Rectangle(bounds.X, bounds.Bottom - ConvertToDeviceY(footer.Height), bounds.Width, ConvertToDeviceY(footer.Height)), Scale);
				bounds.Height -= ConvertToDeviceY(footer.Height);
			}

			return bounds;
		}

		protected Rectangle RenderHeaderFixedRows(object context, Rectangle bounds,
				EventHandler<DoCellsEventArgs> renderGridAction,
				EventHandler<DoCellsEventArgs> renderCellAction)
		{
			if (Manager.PrintSettings.HeaderFixedRows.Count == 0)
				return bounds;

			DeviceOffsetX = bounds.X;
			DeviceOffsetY = bounds.Y;

			var startRow = int.MaxValue;
			var endRow = int.MinValue;
			DoCellsRange(context, null, bounds, (sender, e) =>
			{
				startRow = Math.Min(e.Row, startRow);
				endRow = Math.Max(e.Row, endRow);
			});
			var pageRange = new Range(startRow, endRow);
			foreach (var range in Manager.PrintSettings.HeaderFixedRows)
			{
				if (!pageRange.IsIn(range))
				{
					var fixedBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, 0);
					for (int i = range.From; i <= range.To; i++)
					{
						bounds.Y += ConvertToDeviceY(Manager.GetRowHeight(i));
						bounds.Height -= ConvertToDeviceY(Manager.GetRowHeight(i));
						fixedBounds.Height += ConvertToDeviceY(Manager.GetRowHeight(i));
					}
					var oldTop = TopRow;
					TopRow = range.From;
					RenderCellsRange(context, fixedBounds, renderGridAction, renderCellAction);
					TopRow = oldTop;
				}
			}

			return bounds;
		}

		protected Rectangle RenderFooterFixedRows(object context, Rectangle bounds,
				EventHandler<DoCellsEventArgs> renderGridAction,
				EventHandler<DoCellsEventArgs> renderCellAction)
		{
			if (Manager.PrintSettings.FooterFixedRows.Count == 0)
				return bounds;

			DeviceOffsetX = bounds.X;
			DeviceOffsetY = bounds.Y;

			var startRow = int.MaxValue;
			var endRow = int.MinValue;
			DoCellsRange(context, null, bounds, (sender, e) =>
			{
				startRow = Math.Min(e.Row, startRow);
				endRow = Math.Max(e.Row, endRow);
			});
			var pageRange = new Range(startRow, endRow);
			float startPoint = bounds.Bottom;
			foreach (var range in Manager.PrintSettings.FooterFixedRows)
				if (!pageRange.IsIn(range))
					for (int i = range.From; i <= range.To; i++)
						startPoint -= ConvertToDeviceY(Manager.GetRowHeight(i));
			foreach (var range in Manager.PrintSettings.FooterFixedRows)
			{
				if (!pageRange.IsIn(range))
				{
					var fixedBounds = new Rectangle(bounds.X, startPoint, bounds.Width, 0);
					for (int i = range.From; i <= range.To; i++)
					{
						bounds.Height -= ConvertToDeviceY(Manager.GetRowHeight(i));
						fixedBounds.Height += ConvertToDeviceY(Manager.GetRowHeight(i));
					}
					startPoint += fixedBounds.Height;
					var oldTop = TopRow;
					TopRow = range.From;

					DeviceOffsetX = fixedBounds.X;
					DeviceOffsetY = fixedBounds.Y;

					RenderCellsRange(context, fixedBounds, renderGridAction, renderCellAction);
					TopRow = oldTop;
				}
			}

			return bounds;
		}

		protected bool RenderPageAndCheckHasMorePage(object context, Rectangle bounds)
		{
			CellLocation rightBottom;
			return RenderPageAndCheckHasMorePage(context, bounds, _renderGridAction, _renderCellAction, out rightBottom);
		}

		protected bool RenderPageAndCheckHasMorePage(object context, Rectangle bounds,
				EventHandler<DoCellsEventArgs> renderGridAction,
				EventHandler<DoCellsEventArgs> renderCellAction,
				out CellLocation rightBottom)
		{
			rightBottom = new CellLocation { Row = 0, Column = 0 };
			if (Manager.ActualColumnsCount == 0 || Manager.ActualRowsCount == 0)
				return false;

			bounds = RenderHeaderAndFooter(context, bounds, renderGridAction, renderCellAction);
			bounds = RenderHeaderFixedRows(context, bounds, renderGridAction, renderCellAction);
			bounds = RenderFooterFixedRows(context, bounds, renderGridAction, renderCellAction);

			Renderer.SetClip(context, new Rectangle(bounds.X, bounds.Y, GetCurrentPageWidth(bounds), bounds.Height));

			DeviceOffsetX = bounds.X;
			DeviceOffsetY = bounds.Y;
			try
			{
				rightBottom = RenderCellsRange(context, bounds, renderGridAction, renderCellAction);
				if (rightBottom.Column < Manager.ActualColumnsCount - 1)
				{
					LeftColumn = rightBottom.Column + 1;
					return true;
				}
				else if (rightBottom.Row == Manager.ActualRowsCount - 1 && rightBottom.Column == Manager.ActualColumnsCount - 1)
				{
					return false;
				}
				else
				{
					LeftColumn = 0;
					TopRow = rightBottom.Row + 1;
					return true;
				}
			}
			finally
			{
				CurrentPage++;
			}
		}

		protected bool CalcPageEndCheckHasMorePage(Rectangle bounds, out CellLocation rightBottom)
		{
			return RenderPageAndCheckHasMorePage(null, bounds, _emptyAction, _emptyAction, out rightBottom);
		}

		protected int CalcPages(Rectangle bounds)
		{
			int result = 0;
			CellLocation rightBottom;
			var pageBounds = bounds;
			do
			{
				BeginRender();
				result++;
				EndRender();
			}
			while (CalcPageEndCheckHasMorePage(pageBounds, out rightBottom));

			return result;
		}
	}
}
