using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using CellReport.Core;
using CellReport.Core.Contents;
using System.Globalization;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.IO;

namespace CellReport.Presenters.Wpf
{
    public static class RectHelper
    {
        static RectConverter _conv = new RectConverter();

        public static Rect ToRect(this CellReport.Core.Rectangle rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static CellReport.Core.Rectangle ToRecttangle(this Rect rect)
        {
            return new CellReport.Core.Rectangle(
                Convert.ToSingle(rect.X), Convert.ToSingle(rect.Y), 
                Convert.ToSingle(rect.Width), Convert.ToSingle(rect.Height));
        }
    }

    public class Renderer: BaseRenderer
    {
        public override void RenderLines(object context, string color, float lineWidth, LineStyle style, CellReport.Core.Point[] points)
        {
            if (style != LineStyle.None)
            {
                var dc = (DrawingContext)context;
                Geometry geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = ((StreamGeometry)geometry).Open())
                {
                    ctx.BeginFigure(new System.Windows.Point(points[0].X, points[0].Y), false, false);
                    ctx.PolyLineTo(Array.ConvertAll(points, p => new System.Windows.Point(p.X, p.Y)), true, false);
                }
                var pen = GetPen(color, Math.Round(lineWidth, MidpointRounding.AwayFromZero));
                //pen.DashStyle = new DashStyle()
                dc.DrawGeometry(null, pen, geometry);
            }
        }

        public override void RenderFillRect(object context, Rectangle rect, float angle, params string[] colors)
        {
            var dc = (DrawingContext)context;
            dc.DrawRectangle(GetBrush(colors[0]), null, rect.ToRect());
        }

        public override void RenderRect(object context, Rectangle rect, string color, LineStyle style, float lineWidth)
        {
            var dc = (DrawingContext)context;
            dc.DrawRectangle(null, GetPen(color, Math.Round(lineWidth, MidpointRounding.AwayFromZero)), rect.ToRect());
        }

        public override void RenderImage(object context, Rectangle cellRect, byte[] data)
        {
            if (data.Length == 0)
                return;

            var dc = (DrawingContext)context;
            BitmapImage image = new BitmapImage();
            MemoryStream stream = new MemoryStream(data);            
            image.BeginInit();            
            image.StreamSource = stream;            
            image.EndInit();
            dc.DrawImage(image, cellRect.ToRect());
        }

        public override void RenderText(object context, Rectangle textRect, string text, string color, CellReport.Core.Contents.HorizontalAlignment h, CellReport.Core.Contents.VerticalAlignment v, float angle, string fontFamily, float fontSize, CellReport.Core.FontStyle fontStyle, Direction direction)
        {
            var dc = (DrawingContext)context;
            
            var formattedText = new FormattedText(text, System.Threading.Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(fontFamily),
                UnitsConverter.ConvertValue(fontSize, GraphicsUnit.Point, GraphicsUnit.WpfPoint, 0),
                GetBrush(color), 
                new NumberSubstitution(), 
                TextFormattingMode.Display);

            if ((fontStyle & CellReport.Core.FontStyle.Bold) == CellReport.Core.FontStyle.Bold)
                formattedText.SetFontWeight(FontWeights.Bold);
            if ((fontStyle & CellReport.Core.FontStyle.Italic) == CellReport.Core.FontStyle.Italic)
                formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.MaxTextHeight = textRect.Height > 0 ? textRect.Height : 0.01;
            formattedText.MaxTextWidth = textRect.Width > 4 ? textRect.Width - 4 : 0.01;
            var position = new System.Windows.Point(textRect.X + 2, textRect.Y);
            switch (h)
            {
                case CellReport.Core.Contents.HorizontalAlignment.Left:
                    formattedText.TextAlignment = TextAlignment.Left;
                    break;
                case CellReport.Core.Contents.HorizontalAlignment.Center:
                    formattedText.TextAlignment = TextAlignment.Center;
                    break;
                case CellReport.Core.Contents.HorizontalAlignment.Right:
                    formattedText.TextAlignment = TextAlignment.Right;
                    break;
            }
            switch (v)
            {
                case CellReport.Core.Contents.VerticalAlignment.Middle:
                    position.Y = position.Y + (textRect.Height - formattedText.Height) / 2f;
                    break;
                case CellReport.Core.Contents.VerticalAlignment.Bottom:
                    position.Y = position.Y + textRect.Height - formattedText.Height;
                    break;
            }
            dc.DrawText(formattedText, position);
        }

        private Color GetColor(string color)
        {
            byte a = 0, r = 0, g = 0, b = 0;
            BaseStyle.ParseColor(color, out a, out r, out g, out b);
            return Color.FromArgb(a, r, g, b);
        }

        class PenInfo
        {
            public string Color { get; set; }
            public double Thickness { get; set; }

            public override bool Equals(object obj)
            {
                var other = obj as PenInfo;
                return other.Thickness.Equals(this.Thickness) && other.Color.Equals(this.Color);
            }

            public override int GetHashCode()
            {
                return Color.GetHashCode() + Thickness.GetHashCode();
            }
        }

        Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
        Dictionary<PenInfo, Pen> _pens = new Dictionary<PenInfo, Pen>();

        private Brush GetBrush(string color)
        {
            Brush brush = null;
            if (!_brushes.TryGetValue(color, out brush))
            {
                brush = new SolidColorBrush(GetColor(color));
                brush.Freeze();
                _brushes.Add(color, brush);
            }

            return brush;
        }

        private Pen GetPen(string color, double thickness)
        {
            var info = new PenInfo() { Color = color, Thickness = thickness };
            Pen pen = null;
            if (!_pens.TryGetValue(info, out pen))
            {
                pen = new Pen(GetBrush(color), thickness);
                _pens.Add(info, pen);
            }
            return pen;
        }
    }
}
