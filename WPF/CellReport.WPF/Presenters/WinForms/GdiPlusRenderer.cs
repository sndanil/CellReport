using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using CellReport.Core;
using System.Drawing.Drawing2D;
using CellReport.Core.Contents;
using System.Windows.Forms;

namespace CellReport.Presenters.WinForms
{
    public static class RectHelper
    {
        public static RectangleF ToRectangleF(this CellReport.Core.Rectangle rect)
        {
            return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static System.Drawing.Rectangle ToRectangle(this CellReport.Core.Rectangle rect)
        {
            return new System.Drawing.Rectangle(
                Convert.ToInt32(rect.X), 
                Convert.ToInt32(rect.Y), 
                Convert.ToInt32(rect.Width), 
                Convert.ToInt32(rect.Height));
        }

        public static CellReport.Core.Rectangle ToRecttangle(this RectangleF rect)
        {
            return new CellReport.Core.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static CellReport.Core.Rectangle ToCellRecttangle(this System.Drawing.Rectangle rect)
        {
            return new CellReport.Core.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    public class GdiPlusRenderer : BaseRenderer
    {
        public GdiPlusRenderer(BasePresenter presenter)
            : base(presenter)
        {
        }

        public static CellReport.Core.Size GetTextSize(string text, string fontFamily, float fontSize)
        {
            SizeF result;
            using (Font font = new Font(fontFamily, fontSize))
                result = TextRenderer.MeasureText(text, font);

            result.Height = UnitsConverter.ConvertValue(result.Height, CellReport.Core.GraphicsUnit.Pixel,
                CellReport.Core.GraphicsUnit.Millimeter, 96);
            result.Width = UnitsConverter.ConvertValue(result.Width, CellReport.Core.GraphicsUnit.Pixel,
                CellReport.Core.GraphicsUnit.Millimeter, 96);

            return new CellReport.Core.Size(result.Width, result.Height);
        }

        public override void RenderFillRect(object context, CellReport.Core.Rectangle rect, float angle, params string[] colors)
        {
            var g = (Graphics)context;
            var cellRect = rect.ToRectangleF();
            var color = GetColor(colors[0]);
            if (color != Color.Transparent)
            {
                using (var brush = new SolidBrush(color))
                    g.FillRectangle(brush, cellRect);
            }
        }

        public override void RenderText(object context, CellReport.Core.Rectangle textRect, string text, string color,
            CellReport.Core.Contents.HorizontalAlignment h, VerticalAlignment v, float angle,
            string fontFamily, float fontSize, CellReport.Core.FontStyle fontStyle, Direction direction)
        {
            if (textRect.Width == 0 || textRect.Height == 0)
                return;

            var g = (Graphics)context;
            StringFormat stringFormat = new StringFormat();
            stringFormat.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;
            //stringFormat.FormatFlags = StringFormatFlags.DirectionVertical;
            var transformPoint = new PointF(textRect.X + textRect.Width / 2, textRect.Y + textRect.Height / 2);
            switch (h)
            {
                case CellReport.Core.Contents.HorizontalAlignment.Left:
                    stringFormat.Alignment = StringAlignment.Near;
                    transformPoint.X = textRect.X;
                    break;
                case CellReport.Core.Contents.HorizontalAlignment.Right:
                    stringFormat.Alignment = StringAlignment.Far;
                    transformPoint.X = textRect.Right;
                    break;
            }
            switch (v)
            {
                case VerticalAlignment.Top:
                    stringFormat.LineAlignment = StringAlignment.Near;
                    transformPoint.Y = textRect.Y;
                    break;
                case VerticalAlignment.Bottom:
                    stringFormat.LineAlignment = StringAlignment.Far;
                    transformPoint.Y = textRect.Bottom;
                    break;
            }
            if (angle != 0)
            {
                GraphicsState gOldState = g.Save();
                g.TranslateTransform(-transformPoint.X, -transformPoint.Y, MatrixOrder.Append);
                g.RotateTransform(angle, MatrixOrder.Append);
                g.TranslateTransform(transformPoint.X, transformPoint.Y, MatrixOrder.Append);

                using (var font = new Font(fontFamily, fontSize, (System.Drawing.FontStyle)fontStyle))
                using (var brush = new SolidBrush(GetColor(color)))
                    g.DrawString(text, font, brush, textRect.ToRectangleF(), stringFormat);

                g.Restore(gOldState);
            }
            else
            {
                if (direction == Direction.TopToDown)
                {
                    using (var font = new Font(fontFamily, fontSize, (System.Drawing.FontStyle)fontStyle))
                    using (var brush = new SolidBrush(GetColor(color)))
                    {
                        var textHeight = g.MeasureString(text, font).Height;
                        var totalTextHeight = textHeight * text.Length;
                        var startYPoint = textRect.Y;
                        switch (v)
                        {
                            case VerticalAlignment.Middle:
                                startYPoint = textRect.Height / 2 - totalTextHeight / 2 + textRect.Y;
                                break;
                            case VerticalAlignment.Bottom:
                                startYPoint = textRect.Bottom - totalTextHeight;
                                break;
                        }
                        var oldClip = g.ClipBounds;
                        var newClip = RectangleF.Intersect(g.ClipBounds, textRect.ToRectangleF());
                        if (newClip == RectangleF.Empty)
                            return;
                        g.SetClip(newClip);
                        foreach (var c in text)
                        {
                            g.DrawString(c.ToString(), font, brush, new RectangleF(textRect.X, startYPoint, textRect.Width, textHeight), stringFormat);
                            startYPoint += textHeight;
                        }
                        g.SetClip(oldClip);
                    }
                }
                else
                {
                    using (var font = new Font(fontFamily, fontSize, (System.Drawing.FontStyle)fontStyle))
                    using (var brush = new SolidBrush(GetColor(color)))
                        g.DrawString(text, font, brush, textRect.ToRectangleF(), stringFormat);
                }
            }
        }

        public override void RenderRect(object context, CellReport.Core.Rectangle rect, string color, LineStyle style, float lineWidth)
        {
            var g = (Graphics)context;
            using (var pen = new Pen(GetColor(color), lineWidth))
            {
                pen.DashStyle = (System.Drawing.Drawing2D.DashStyle)style;
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        public override void RenderLines(object context, string color, float lineWidth, LineStyle style, CellReport.Core.Point[] points)
        {
            var g = (Graphics)context;
            using (var pen = new Pen(GetColor(color), lineWidth))
            {
                pen.DashStyle = (System.Drawing.Drawing2D.DashStyle)style;
                g.DrawLines(pen, Array.ConvertAll(points, p => new PointF(p.X, p.Y)));
            }
        }

        //public override void RenderLine(object context, string color, float lineWidth, float x1, float y1, float x2, float y2)
        //{
        //    var g = (Graphics)context;
        //    //var oldSmoothMode = g.SmoothingMode;
        //    //g.SmoothingMode = SmoothingMode.AntiAlias;
        //    using (var pen = new Pen(GetColor(color), lineWidth))
        //        g.DrawLine(pen, x1, y1, x2, y2);
        //    //g.SmoothingMode = oldSmoothMode;
        //}

        public override void RenderImage(object context, CellReport.Core.Rectangle cellRect, byte[] data)
        {
            var g = (Graphics)context;
            if (data != null && data.Length > 0)
            {
                using(Image img = Image.FromStream(new System.IO.MemoryStream(data)))                
                    g.DrawImage(img, cellRect.ToRectangle());
            }
        }

        protected Color GetColor(string color)
        {
            byte a = 0, r = 0, g = 0, b = 0;
            BaseStyle.ParseColor(color, out a, out r, out g, out b);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
