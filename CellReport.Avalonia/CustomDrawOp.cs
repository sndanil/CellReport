using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace CellReport.Avalonia
{
    internal class CustomDrawOp : ICustomDrawOperation
    {
        private readonly IImmutableGlyphRunReference _noSkia;
        private readonly Control _control;
        private readonly DisplayPresenter _presenter;

        public Rect Bounds { get; }
        public bool HitTest(global::Avalonia.Point p) => false;
        public bool Equals(ICustomDrawOperation other) => false;

        public CustomDrawOp(Rect bounds, GlyphRun noSkia, Control control, DisplayPresenter presenter)
        {
            _control = control;
            _presenter = presenter;
            Bounds = bounds;
        }

        public void Dispose()
        {
            // No-op
        }

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
                context.DrawGlyphRun(Brushes.Black, _noSkia);
            else
            {
                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;
                canvas.Save();

                using (var paint = new SKPaint() { Color = SKColors.White, Style = SKPaintStyle.StrokeAndFill })
                {
                    canvas.DrawRect(_control.Bounds.WithX(0).WithY(0).ToSKRect(), paint);
                }

                if (_presenter != null)
                    _presenter.Render(_control, canvas);

                canvas.Restore();
            }
        }
    }
}
