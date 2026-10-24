using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.Rendering.SceneGraph;
using SkiaSharp;

namespace CellReport.Avalonia
{
	internal class CustomDrawOp : ICustomDrawOperation
	{
		private readonly Control _control;
		private readonly AvaloniaDisplayPresenter? _presenter;
		private readonly SKColor _background;

		public Rect Bounds { get; }

		public CustomDrawOp(Rect bounds, Control control, AvaloniaDisplayPresenter? presenter, SKColor background)
		{
			_control = control;
			_presenter = presenter;
			_background = background;
			Bounds = bounds;
		}

		public bool HitTest(global::Avalonia.Point p) => false;

		public bool Equals(ICustomDrawOperation? other) => false;

		public override bool Equals(object? obj) => false;

		public override int GetHashCode() => 0;

		public void Dispose()
		{
		}

		public void Render(ImmediateDrawingContext context)
		{
			var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
			if (leaseFeature == null)
				return;

			using var lease = leaseFeature.Lease();
			var canvas = lease.SkCanvas;
			canvas.Save();

			using (var paint = new SKPaint { Color = _background, Style = SKPaintStyle.Fill })
				canvas.DrawRect(_control.Bounds.WithX(0).WithY(0).ToSKRect(), paint);

			_presenter?.Render(_control, canvas);
			canvas.Restore();
		}
	}
}
