using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using CellReport.Core;
using SkiaSharp;
using System;
using System.Linq;
using AvaloniaRect = global::Avalonia.Rect;

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
            context.FillRectangle(Brushes.White, new AvaloniaRect(0, 0, this.Bounds.Width, this.Bounds.Height));

            context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), _noSkia, this, Presenter));
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }
        
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (Presenter == null || e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }

            if (Presenter.StartSelection.Equals(Presenter.EndSelection))
            {
                Presenter.ClearSelection();
            }
            else
            {
                if (Presenter.EndSelection.Equals(CellLocation.None))
                {
                    Presenter.ClearSelection();
                }

                Presenter.Update();
            }

            Presenter.IsSelectionMode = false;
            Presenter.Update();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (Presenter == null || e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }

            var position = e.GetPosition(this);
            Presenter.RaiseClick(this, e, Convert.ToSingle(position.X), Convert.ToSingle(position.Y));
            Presenter.StartSelection = Presenter.GetCellByDevicePoint(Convert.ToSingle(position.X), Convert.ToSingle(position.Y), false);
            Presenter.IsSelectionMode = true;
            Presenter.Update();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (Presenter == null || e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }

            var position = e.GetPosition(this);
            Presenter.EndSelection = Presenter.GetCellByDevicePoint(Convert.ToSingle(position.X), Convert.ToSingle(position.Y), false);
            Presenter.Update();
        }
    }
}
