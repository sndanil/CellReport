using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CellReport.Core;
using CellReport.Presenters;
using AvaloniaRect = global::Avalonia.Rect;
using AvaloniaPoint = global::Avalonia.Point;

namespace CellReport.Avalonia
{
	public class BaseResizerControl : Control
	{
		protected bool _isPreresize;

		public BaseDisplayPresenter? Presenter { get; set; }

		protected IBrush GetBrush(string key, Color fallback)
		{
			if (this.TryFindResource(key, ActualThemeVariant, out var resource) && resource is IBrush brush)
				return brush;
			return new SolidColorBrush(fallback);
		}

		protected Pen CreatePen()
		{
			return new Pen(GetBrush("CellReport.Grid.HeaderBorder", Color.FromRgb(0xB1, 0xB5, 0xBA)), 1);
		}

		protected Pen CreateHeaderSeparatorPen()
		{
			return new Pen(GetBrush("CellReport.Grid.HeaderSeparator", Color.FromRgb(0x9E, 0xB6, 0xCE)), 1);
		}

		public override void Render(DrawingContext drawingContext)
		{
			base.Render(drawingContext);
			drawingContext.FillRectangle(
				GetBrush("CellReport.Grid.HeaderBackground", Color.FromRgb(0xDF, 0xE3, 0xE8)),
				new AvaloniaRect(0, 0, Bounds.Width, Bounds.Height));
		}

		protected void RenderText(DrawingContext dc, string text, AvaloniaRect bounds)
		{
			if (bounds.Height <= 0.1 || bounds.Width <= 0.1)
				return;

			var textBrush = GetBrush("CellReport.Grid.HeaderForeground", Color.FromRgb(0x36, 0x36, 0x36));
			this.TryFindResource("ResizersFont", ActualThemeVariant, out var fontFamily);
			if (!(fontFamily is FontFamily))
				fontFamily = FontFamily.Default;

			var formattedText = new FormattedText(text,
				System.Globalization.CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				new Typeface((FontFamily)fontFamily),
				10,
				textBrush
			);

			formattedText.MaxTextWidth = bounds.Width;
			formattedText.MaxTextHeight = bounds.Height;
			formattedText.TextAlignment = TextAlignment.Center;
			var position = new AvaloniaPoint(
				bounds.Left,
				bounds.Top + (bounds.Bottom - bounds.Top) / 2 - formattedText.Height / 2);
			dc.DrawText(formattedText, position);
		}

		protected void DrawCurrentCellHighlight(DrawingContext dc, AvaloniaRect bounds)
		{
			if (bounds.Width <= 0.1 || bounds.Height <= 0.1)
				return;

			IBrush brush = new LinearGradientBrush
			{
				StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
				EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
				GradientStops =
				{
					new GradientStop(Color.FromRgb(0xFF, 0xD3, 0x59), 0),
					new GradientStop(Color.FromRgb(0xFF, 0xEF, 0x71), 1)
				}
			};

			if (this.TryFindResource("CellReport.Grid.SelectedHeader", ActualThemeVariant, out var headerBrush)
				&& headerBrush is IBrush selected)
				brush = selected;

			dc.DrawRectangle(brush, null, bounds);
		}

		protected bool IsCurrentRow(int row)
		{
			if (Presenter == null)
				return false;

			var cell = Presenter.Manager.GetCell(Presenter.CurrentCell);
			return row == Presenter.CurrentCell.Row
				|| (cell != null && row >= cell.Location.Row && row <= cell.Location.Row + cell.Span.RowSpan);
		}

		protected bool IsCurrentColumn(int column)
		{
			if (Presenter == null)
				return false;

			var cell = Presenter.Manager.GetCell(Presenter.CurrentCell);
			return column == Presenter.CurrentCell.Column
				|| (cell != null && column >= cell.Location.Column && column <= cell.Location.Column + cell.Span.ColSpan);
		}

		protected void Select(CellLocation from, CellLocation to)
		{
			if (Presenter == null)
				return;

			Presenter.BeginUpdate();
			Presenter.StartSelection = from;
			Presenter.EndSelection = to;
			Presenter.EndUpdate();
		}
	}
}
