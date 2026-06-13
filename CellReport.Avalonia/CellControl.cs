using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CellReport.Core;
using System;

namespace CellReport.Avalonia
{
	public class CellControl : Control
	{
		public AvaloniaDisplayPresenter Presenter { get; set; }

		public CellControl()
		{
			ClipToBounds = true;
			Focusable = true;
		}

		public override void Render(DrawingContext context)
		{
			context.FillRectangle(Brushes.White, new Rect(0, 0, Bounds.Width, Bounds.Height));
			context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), this, Presenter));
		}

		protected override void OnPointerReleased(PointerReleasedEventArgs e)
		{
			base.OnPointerReleased(e);

			if (Presenter == null || e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
				return;

			if (Presenter.StartSelection.Equals(Presenter.EndSelection))
				Presenter.ClearSelection();
			else if (Presenter.EndSelection.Equals(CellLocation.None))
				Presenter.ClearSelection();

			Presenter.IsSelectionMode = false;
			Presenter.Update();
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);

			if (Presenter == null || e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
				return;

			var position = e.GetPosition(this);
			Presenter.RaiseClick(this, e, Convert.ToSingle(position.X), Convert.ToSingle(position.Y));
			Presenter.StartSelection = Presenter.GetCellByDevicePoint(Convert.ToSingle(position.X), Convert.ToSingle(position.Y), false);
			Presenter.IsSelectionMode = true;
			Presenter.Update();
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);

			if (Presenter == null || !Presenter.IsSelectionMode || e.GetCurrentPoint(this).Properties.IsLeftButtonPressed != true)
				return;

			var position = e.GetPosition(this);
			Presenter.EndSelection = Presenter.GetCellByDevicePoint(Convert.ToSingle(position.X), Convert.ToSingle(position.Y), false);
			Presenter.Update();
		}
	}
}
