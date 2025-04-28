using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Linq;

namespace CellReport.Avalonia
{
    public class CellControl : Control
    {
        private readonly GlyphRun _noSkia;

        public DisplayPresenter Presenter
        {
            get;
            set;
        }

        public CellControl()
        {
            ClipToBounds = true;
            var text = "Current rendering API is not Skia";
            var glyphs = text.Select(ch => Typeface.Default.GlyphTypeface.GetGlyph(ch)).ToArray();
            _noSkia = new GlyphRun(Typeface.Default.GlyphTypeface, 12, text.AsMemory(), glyphs);
        }

        public override void Render(DrawingContext context)
        {
            context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), _noSkia, this, Presenter));
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }

        class CustomDrawOp : ICustomDrawOperation
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
}
