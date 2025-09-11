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
using Avalonia.Media.Imaging;
using System.Net.NetworkInformation;
using ZXing;

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

	public class AvaloniaRenderer : BaseRenderer
	{
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
					if (points.Length == 2)
					{
                        dc.DrawPoints(SKPointMode.Polygon, points.Select(p => new SKPoint(p.X, p.Y)).ToArray(), paint);
                    }
                    else
					{
						var path = new SKPath();
						path.MoveTo(points[0].X, points[0].Y);
						foreach (var point in points)
						{
							path.LineTo(point.X, point.Y);
						}

						if (points.Length == 5)
						{
							path.Close();
						}

						dc.DrawPath(path, paint);
					}
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

        public override void RenderBarCode(object context, 
			Rectangle cellRect, 
			BarCodeContent barcode, 
			string color, 
			string fontFamily, 
			float fontSize, 
			CellReport.Core.FontStyle fontStyle, 
			Orientation orientation)
        {
            if (cellRect.Width < 1 || cellRect.Height < 1)
                return;

            var writer = new ZXing.SkiaSharp.BarcodeWriter();
            var options = new ZXing.Common.EncodingOptions
            {
                Width = Convert.ToInt32(cellRect.Width),
                Height = Convert.ToInt32(cellRect.Height),
                Margin = 0,
                PureBarcode = true
            };

            switch (barcode.GetBarCodeType())
            {
                case BarCodeType.EAN13:
                    writer.Format = BarcodeFormat.EAN_13;
                    break;
                case BarCodeType.EAN8:
                    writer.Format = BarcodeFormat.EAN_8;
                    break;
                case BarCodeType.Code128:
                    writer.Format = BarcodeFormat.CODE_128;
                    options = new ZXing.OneD.Code128EncodingOptions
                    {
                        Width = Convert.ToInt32(cellRect.Width),
                        Height = Convert.ToInt32(cellRect.Height),
                        Margin = 0,
                        PureBarcode = true
                    };
                    break;
                case BarCodeType.QR:
                    writer.Format = BarcodeFormat.QR_CODE;
                    options = new ZXing.QrCode.QrCodeEncodingOptions
                    {
                        Width = Convert.ToInt32(cellRect.Width),
                        Height = Convert.ToInt32(cellRect.Height),
                        Margin = 0,
                        PureBarcode = true,
                        CharacterSet = "utf-8"
                    };
                    break;
                case BarCodeType.PDF417:
                    writer.Format = BarcodeFormat.PDF_417;
                    options = new ZXing.PDF417.PDF417EncodingOptions
                    {
                        Width = Convert.ToInt32(cellRect.Width),
                        Height = Convert.ToInt32(cellRect.Height),
                        Margin = 0,
                        PureBarcode = true,
                        CharacterSet = "utf-8"
                    };
                    break;
                case BarCodeType.UPCA:
                    writer.Format = BarcodeFormat.UPC_A;
                    break;
                case BarCodeType.UPCE:
                    writer.Format = BarcodeFormat.UPC_E;
                    break;
                case BarCodeType.Code39:
                    writer.Format = BarcodeFormat.CODE_39;
                    break;
                case BarCodeType.UCC12:
                    writer.Format = BarcodeFormat.QR_CODE;
                    break;
                case BarCodeType.Auto:
                    switch (barcode.GetCode().Length)
                    {
                        case 8:
                            writer.Format = BarcodeFormat.EAN_8;
                            break;
                        case 13:
                            writer.Format = BarcodeFormat.EAN_13;
                            break;
                        default:
                            writer.Format = BarcodeFormat.QR_CODE;
                            options = new ZXing.QrCode.QrCodeEncodingOptions
                            {
                                Width = Convert.ToInt32(cellRect.Width),
                                Height = Convert.ToInt32(cellRect.Height),
                                Margin = 0,
                                PureBarcode = true,
                                CharacterSet = "utf-8"
                            };
                            break;
                    }
                    break;
            }
            writer.Options = options;

            try
            {
                var result = (SKBitmap)writer.Write(barcode.GetCode());

                var dc = (SKCanvas)context;
                dc.DrawBitmap(result, cellRect.ToRect());
            }
            catch
            {
            }
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
