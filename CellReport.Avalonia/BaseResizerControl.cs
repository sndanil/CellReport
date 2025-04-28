using Avalonia.Controls;
using Avalonia.Media;
using CellReport.Presenters;
using AvaloniaRect = global::Avalonia.Rect;
using AvaloniaPoint = global::Avalonia.Point;
using CellReport.Core;

namespace CellReport.Avalonia
{
    public class BaseResizerControl: Control
    {
        protected bool _isPreresize = false;

        public BaseDisplayPresenter Presenter
        {
            get;
            set;
        }

        protected Pen CreatePen()
        {
            this.TryFindResource("ResizersLineBrush", this.ActualThemeVariant, out var lineBrush);
            return new Pen((SolidColorBrush)lineBrush, 1);
        }

        public override void Render(DrawingContext drawingContext)
        {
            base.Render(drawingContext);
            this.TryFindResource("ResizersBackBrush", this.ActualThemeVariant, out var backBrush);
            drawingContext.FillRectangle((SolidColorBrush)backBrush, 
                new AvaloniaRect(0, 0, this.Bounds.Width, this.Bounds.Height));
        }

        protected void RenderText(DrawingContext dc, string text, AvaloniaRect bounds)
        {
            this.TryFindResource("ResizersTextBrush", this.ActualThemeVariant, out var textBrush);
            this.TryFindResource("ResizersFont", this.ActualThemeVariant, out var fontFamily);

            var formattedText = new FormattedText(text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface((FontFamily)fontFamily),
                10,
                (SolidColorBrush)textBrush
            );

            formattedText.MaxTextWidth = bounds.Width;
            formattedText.MaxTextHeight = bounds.Height;
            formattedText.TextAlignment = TextAlignment.Center;
            var position = new AvaloniaPoint(bounds.Left, bounds.Top + (bounds.Bottom - bounds.Top) / 2 - formattedText.Height / 2);
            dc.DrawText(formattedText, position);
        }

        protected void Select(CellLocation from, CellLocation to)
        {
            Presenter.BeginUpdate();
            Presenter.StartSelection = from;
            Presenter.EndSelection = to;
            Presenter.EndUpdate();
        }
    }
}
