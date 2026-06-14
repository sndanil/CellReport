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
		private readonly AvaloniaDisplayPresenter _presenter;

		public Rect Bounds { get; }

		public CustomDrawOp(Rect bounds, Control control, AvaloniaDisplayPresenter presenter)
		{
			_control = control;
			_presenter = presenter;
			Bounds = bounds;
		}

		public bool HitTest(global::Avalonia.Point p) => false;

		public bool Equals(ICustomDrawOperation other) => false;

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

			using (var paint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill })
				canvas.DrawRect(_control.Bounds.WithX(0).WithY(0).ToSKRect(), paint);

			_presenter?.Render(_control, canvas);
			canvas.Restore();
		}
	}
}
