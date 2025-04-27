using Avalonia.Controls;
using Avalonia.Media;
using CellReport.Presenters;
using AvaloniaRect = global::Avalonia.Rect;
using AvaloniaPoint = global::Avalonia.Point;

namespace CellReport.Avalonia
{
    public class BaseResizerControl: Control
    {
        protected static SolidColorBrush _lineBrush = new SolidColorBrush(Color.FromRgb(0xB1, 0xB5, 0xBA));
        protected static SolidColorBrush _backBrush = new SolidColorBrush(Color.FromRgb(0xDF, 0xE3, 0xE8));
        protected static SolidColorBrush _textBrush = new SolidColorBrush(Color.FromRgb(0x36, 0x36, 0x36));

        protected bool _isPreresize = false;

        public BaseDisplayPresenter Presenter
        {
            get;
            set;
        }

        protected Pen CreatePen()
        {
            return new Pen(_lineBrush, 1);
        }

        public override void Render(DrawingContext drawingContext)
        {
            base.Render(drawingContext);
            drawingContext.FillRectangle(_backBrush, new AvaloniaRect(0, 0, this.Bounds.Width, this.Bounds.Height));
        }

        protected void RenderText(DrawingContext dc, string text, AvaloniaRect bounds)
        {
            var formattedText = new FormattedText(text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft Sans Serif"),
                10,
                _textBrush
            );

            formattedText.MaxTextWidth = bounds.Width;
            formattedText.MaxTextHeight = bounds.Height;
            formattedText.TextAlignment = TextAlignment.Center;
            var position = new AvaloniaPoint(bounds.Left, bounds.Top + (bounds.Bottom - bounds.Top) / 2 - formattedText.Height / 2);
            dc.DrawText(formattedText, position);
        }

    }
}
