using Avalonia.Controls;
using CellReport.Core;
using CellReport.Presenters;
using System;
using System.Collections.Generic;
using System.Text;

namespace CellReport.Avalonia
{
	public class DisplayPresenter : BaseDisplayPresenter
	{
		//private readonly Panel _control;
		private readonly PresentControl _presentControl;

		public override float Scale { get; set; }
		public override bool ShowGridLines { get; set; }
		public override bool ShowBookMarks { get; set; }
		public override string GridColor { get; set; }

		public DisplayPresenter(CellManager manager, PresentControl control): base(manager)
		{
			_presentControl = control;
			Renderer = new Renderer();

			DeviceDpiX = 96;
			DeviceDpiY = 96;
			CurrentUnit = GraphicsUnit.WpfPoint;
		}

		protected override Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
		{
			return new Size(rect.Width, rect.Height);
		}

		protected override Rectangle GetHostBounds()
		{
			return _presentControl.Bounds.ToRecttangle();
		}

		public override void Update()
		{
			if (!IsInUpdateMode())
			{
				_presentControl.Invalidate();
			}
		}

		public override void Render(object sender, object context)
		{
			this.BeginRender();
			var control = sender as Control;
			var bounds = control.Bounds.ToRecttangle();
			RenderFrozenCells(context, bounds);
			var x = GetLeftOffset(LeftNoFrozenColumn);
			var y = GetTopOffset(TopNoFrozenRow);
			RenderCellsRange(context, bounds, TopNoFrozenRow, Manager.RowsCount, LeftNoFrozenColumn, Manager.ColumnsCount);
			if (IsCurreCellInFrozen())
			{
				RenderCurrentCell(context, bounds);
			}
			else
			{
				RenderCurrentCell(context, bounds);
			}
			RenderSelection(context);
			this.EndRender();
		}
	}
}
