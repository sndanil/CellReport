using Avalonia;
using Avalonia.Media;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Presenters;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia.Skia;
using SkiaSharp;
using Topten.RichTextKit;
using Avalonia.Media.TextFormatting;

namespace CellReport.Avalonia
{
	public static class RectHelper
	{
		public static SKRect ToRect(this CellReport.Core.Rectangle rect)
		{
			return new SKRect(rect.X, rect.Y, rect.Right, rect.Bottom);
		}

		public static CellReport.Core.Rectangle ToRecttangle(this SKRect rect)
		{
			return new CellReport.Core.Rectangle(
					Convert.ToSingle(rect.Left), Convert.ToSingle(rect.Top),
					Convert.ToSingle(rect.Width), Convert.ToSingle(rect.Height));
		}

		public static CellReport.Core.Rectangle ToRecttangle(this global::Avalonia.Rect rect)
		{
			return new CellReport.Core.Rectangle(
					Convert.ToSingle(rect.X), Convert.ToSingle(rect.Y),
					Convert.ToSingle(rect.Width), Convert.ToSingle(rect.Height));
		}
	}

	public class Renderer : BaseRenderer
	{
		Dictionary<string, Brush> _brushes = new Dictionary<string, Brush>();
		Dictionary<PaintInfo, SKPaint> _paints = new Dictionary<PaintInfo, SKPaint>();

		class PaintInfo
		{
			public string Color { get; set; }
			public double Thickness { get; set; }

			public override bool Equals(object obj)
			{
				var other = obj as PaintInfo;
				return other.Thickness.Equals(this.Thickness) && other.Color.Equals(this.Color);
			}

			public override int GetHashCode()
			{
				return Color.GetHashCode() + Thickness.GetHashCode();
			}
		}

		private SKColor GetColor(string color)
		{
			byte a = 0, r = 0, g = 0, b = 0;
			BaseStyle.ParseColor(color, out a, out r, out g, out b);
			return new SKColor(r, g, b, a);
		}

		private Brush GetBrush(string color)
		{
			Brush brush = null;
			if (!_brushes.TryGetValue(color, out brush))
			{
				//brush = new SolidColorBrush(GetColor(color));
				//_brushes.Add(color, brush);
			}

			return brush;
		}

		private SKPaint GetPaint(string color, float thickness, LineStyle style = LineStyle.Solid)
		{
			SKPaint paint = new SKPaint();
			switch (style)
			{
				case LineStyle.Dash:
					paint.PathEffect = SKPathEffect.CreateDash(new float[] { 1 }, 20);
					break;
				case LineStyle.Dot:
					paint.PathEffect = SKPathEffect.CreateDash(new float[] { }, 20);
					break;
				case LineStyle.DashDot:
					paint.PathEffect = SKPathEffect.CreateDash(new float[] { }, 20);
					break;
				case LineStyle.DashDotDot:
					paint.PathEffect = SKPathEffect.CreateDash(new float[] { }, 20);
					break;
			}
			paint.Style = SKPaintStyle.Stroke;
			if (thickness > 0)
			{
				paint.StrokeWidth = (float)Math.Round(thickness, MidpointRounding.AwayFromZero);
			}
			paint.Color = GetColor(color);

			return paint;
		}

		public override void RenderLines(object context, string color, float lineWidth, LineStyle style, CellReport.Core.Point[] points)
		{
			if (style != LineStyle.None)
			{
				var dc = (SKCanvas)context;
				using (var paint = GetPaint(color, lineWidth, style))
				{
					dc.DrawPoints(SKPointMode.Polygon, points.Select(p => new SKPoint(p.X, p.Y)).ToArray(), paint);
				}
			}
		}

		public override void RenderFillRect(object context, Rectangle rect, float angle, params string[] colors)
		{
			if (colors.First() == "Transparent")
			{
				return;
			}

			var dc = (SKCanvas)context;
			using (var paint = GetPaint(colors.First(), 0))
			{
				paint.Style = SKPaintStyle.StrokeAndFill;
				dc.DrawRect(rect.ToRect(), paint);
			}
		}

		public override void RenderRect(object context, Rectangle rect, string color, LineStyle style, float lineWidth)
		{
			var dc = (SKCanvas)context;
			using (var paint = GetPaint(color, lineWidth, style))
			{
				dc.DrawRect(rect.ToRect(), paint);
			}
		}

		public override void RenderImage(object context, Rectangle cellRect, byte[] data)
		{
			if (data.Length == 0)
				return;

			var dc = (SKCanvas)context;
			//MemoryStream stream = new MemoryStream(data);
			//var image = new global::Avalonia.Media.Imaging.Bitmap(stream);
			//dc.DrawImage(image.PlatformImpl, 1, new Rect(0, 0, image.Size.Width, image.Size.Height), cellRect.ToRect());
		}

		public override void RenderText(object context,
			Rectangle textRect,
			string text,
			string color,
			CellReport.Core.Contents.HorizontalAlignment h,
			CellReport.Core.Contents.VerticalAlignment v,
			float angle,
			string fontFamily,
			float fontSize,
			CellReport.Core.FontStyle fontStyle,
			Direction direction
			)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			textRect = new Rectangle(textRect.X + 3, textRect.Y + 1, textRect.Width - 6, textRect.Height - 2);

			var dc = (SKCanvas)context;

			fontSize = UnitsConverter.ConvertValue(fontSize, GraphicsUnit.Point, GraphicsUnit.WpfPoint, 96);

			dc.Save();
			dc.ClipRect(textRect.ToRect());

			Topten.RichTextKit.TextAlignment rsa = Topten.RichTextKit.TextAlignment.Auto;
            switch (h)
            {
                case HorizontalAlignment.Left:
                    rsa = Topten.RichTextKit.TextAlignment.Left;
                    break;
                case HorizontalAlignment.Center:
                    rsa = Topten.RichTextKit.TextAlignment.Center;
                    break;
                case HorizontalAlignment.Right:
                    rsa = Topten.RichTextKit.TextAlignment.Right;
                    break;
            }

            var rs = new RichString()
				.Alignment(Topten.RichTextKit.TextAlignment.Center)
				.FontFamily(fontFamily)
				.FontSize(fontSize)
				.FontItalic(fontStyle.HasFlag(CellReport.Core.FontStyle.Italic))
				.FontWeight(fontStyle.HasFlag(CellReport.Core.FontStyle.Bold) ? (int)SKFontStyleWeight.Bold: (int)SKFontStyleWeight.Normal)
                .Alignment(rsa)
                .Add(text);

			rs.MaxWidth = textRect.Width;
            rs.MaxHeight = textRect.Height + 5;

            var position = new SKPoint(textRect.X, textRect.Y);
			rs.Paint(dc, position);
			dc.Restore();
		}
	}
}
