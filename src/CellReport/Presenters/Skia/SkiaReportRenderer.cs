using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Presenters;
using SkiaSharp;
using System;
using System.Linq;
using Topten.RichTextKit;
using ZXing;

namespace CellReport.Presenters.Skia
{
	public class SkiaReportRenderer : BaseRenderer
	{
		public static Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
		{
			if (string.IsNullOrEmpty(text) || rect.Width <= 0)
				return new Size(rect.Width, rect.Height);

			fontSize = UnitsConverter.ConvertValue(fontSize, GraphicsUnit.Point, GraphicsUnit.WpfPoint, 96);

			var rs = new RichString()
				.FontFamily(fontFamily)
				.FontSize(fontSize)
				.FontItalic(fontStyle.HasFlag(FontStyle.Italic))
				.FontWeight(fontStyle.HasFlag(FontStyle.Bold) ? (int)SKFontStyleWeight.Bold : (int)SKFontStyleWeight.Normal)
				.Add(text);

			rs.MaxWidth = rect.Width;
			return new Size(rs.MeasuredWidth, rs.MeasuredHeight);
		}

		private SKColor GetColor(string color)
		{
			byte a, r, g, b;
			BaseStyle.ParseColor(color, out a, out r, out g, out b);
			return new SKColor(r, g, b, a);
		}

		private static SKPathEffect GetDashEffect(LineStyle style)
		{
			switch (style)
			{
				case LineStyle.Dash:
					return SKPathEffect.CreateDash(new[] { 4f, 2f }, 0);
				case LineStyle.Dot:
					return SKPathEffect.CreateDash(new[] { 1f, 2f }, 0);
				case LineStyle.DashDot:
					return SKPathEffect.CreateDash(new[] { 4f, 2f, 1f, 2f }, 0);
				case LineStyle.DashDotDot:
					return SKPathEffect.CreateDash(new[] { 4f, 2f, 1f, 2f, 1f, 2f }, 0);
				default:
					return null;
			}
		}

		private SKPaint GetPaint(string color, float thickness, LineStyle style = LineStyle.Solid)
		{
			var paint = new SKPaint
			{
				Style = SKPaintStyle.Stroke,
				Color = GetColor(color)
			};

			if (thickness > 0)
				paint.StrokeWidth = (float)Math.Round(thickness, MidpointRounding.AwayFromZero);

			var dashEffect = GetDashEffect(style);
			if (dashEffect != null)
				paint.PathEffect = dashEffect;

			return paint;
		}

		public override void RenderLines(object context, string color, float lineWidth, LineStyle style, Point[] points)
		{
			if (style == LineStyle.None)
				return;

			var dc = (SKCanvas)context;
			using (var paint = GetPaint(color, lineWidth, style))
			{
				if (points.Length == 2)
				{
					dc.DrawPoints(SKPointMode.Polygon, points.Select(p => new SKPoint(p.X, p.Y)).ToArray(), paint);
				}
				else
				{
					using (var path = new SKPath())
					{
						path.MoveTo(points[0].X, points[0].Y);
						foreach (var point in points)
							path.LineTo(point.X, point.Y);

						if (points.Length == 5)
							path.Close();

						dc.DrawPath(path, paint);
					}
				}
			}
		}

		public override void RenderFillRect(object context, Rectangle rect, float angle, params string[] colors)
		{
			if (colors.Length == 0 || colors.First() == "Transparent")
				return;

			var dc = (SKCanvas)context;
			using (var paint = GetPaint(colors.First(), 0))
			{
				paint.Style = SKPaintStyle.Fill;
				dc.DrawRect(rect.ToRect(), paint);
			}
		}

		public override void RenderRect(object context, Rectangle rect, string color, LineStyle style, float lineWidth)
		{
			if (rect.Width < 1 || rect.Height < 1 || style == LineStyle.None)
				return;

			var dc = (SKCanvas)context;
			using (var paint = GetPaint(color, lineWidth, style))
			{
				dc.DrawRect(rect.ToRect(), paint);
			}
		}

		public override void RenderImage(object context, Rectangle cellRect, byte[] data)
		{
			if (cellRect.Width < 1 || cellRect.Height < 1 || data == null || data.Length == 0)
				return;

			var dc = (SKCanvas)context;
			using (var bitmap = SKBitmap.Decode(data))
			{
				if (bitmap == null)
					return;

				dc.DrawBitmap(bitmap, cellRect.ToRect());
			}
		}

		public override void RenderBarCode(object context,
			Rectangle cellRect,
			BarCodeContent barcode,
			string color,
			string fontFamily,
			float fontSize,
			FontStyle fontStyle,
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
				using (var result = (SKBitmap)writer.Write(barcode.GetCode()))
				{
					var dc = (SKCanvas)context;
					dc.DrawBitmap(result, cellRect.ToRect());
				}
			}
			catch
			{
			}
		}

		public override void RenderText(object context,
			Rectangle textRect,
			string text,
			string color,
			HorizontalAlignment h,
			VerticalAlignment v,
			float angle,
			string fontFamily,
			float fontSize,
			FontStyle fontStyle,
			Direction direction)
		{
			if (string.IsNullOrEmpty(text) || textRect.Width < 1 || textRect.Height < 1)
				return;

			textRect = new Rectangle(textRect.X + 3, textRect.Y + 1, textRect.Width - 6, textRect.Height - 2);

			var dc = (SKCanvas)context;
			fontSize = UnitsConverter.ConvertValue(fontSize, GraphicsUnit.Point, GraphicsUnit.WpfPoint, 96);

			dc.Save();
			dc.ClipRect(textRect.ToRect(), SKClipOperation.Intersect, antialias: false);

			TextAlignment alignment;
			switch (h)
			{
				case HorizontalAlignment.Left:
					alignment = TextAlignment.Left;
					break;
				case HorizontalAlignment.Center:
					alignment = TextAlignment.Center;
					break;
				case HorizontalAlignment.Right:
					alignment = TextAlignment.Right;
					break;
				default:
					alignment = TextAlignment.Auto;
					break;
			}

			var rs = new RichString()
				.FontFamily(fontFamily)
				.FontSize(fontSize)
				.FontItalic(fontStyle.HasFlag(FontStyle.Italic))
				.FontWeight(fontStyle.HasFlag(FontStyle.Bold) ? (int)SKFontStyleWeight.Bold : (int)SKFontStyleWeight.Normal)
				.Alignment(alignment)
				.Add(text);

			rs.MaxWidth = textRect.Width;
			rs.MaxHeight = textRect.Height + 5;

			var y = textRect.Y;
			if (v == VerticalAlignment.Middle)
				y += Math.Max(0, (textRect.Height - rs.MeasuredHeight) / 2);
			else if (v == VerticalAlignment.Bottom)
				y += Math.Max(0, textRect.Height - rs.MeasuredHeight);

			rs.Paint(dc, new SKPoint(textRect.X, y));
			dc.Restore();
		}

		public override void RenderChart(object context, Rectangle rect, ChartContent content)
		{
			var canvas = context as SKCanvas;
			if (canvas == null || rect.Width < 1 || rect.Height < 1 || content == null)
				return;

			SkiaChartRenderer.Render(canvas, rect, content);
		}

		public override void SetClip(object context, Rectangle rect)
		{
			var canvas = context as SKCanvas;
			if (canvas == null)
				return;

			canvas.ClipRect(rect.ToRect(), SKClipOperation.Intersect, antialias: false);
		}

		public override byte[] CreateImage(int width, int height, Action<object> renderAction)
		{
			if (width <= 0 || height <= 0)
				return new byte[0];

			using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
			{
				var canvas = surface.Canvas;
				canvas.Clear(SKColors.White);
				renderAction(canvas);

				using (var image = surface.Snapshot())
				using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
				{
					if (data == null)
						return new byte[0];
					return data.ToArray();
				}
			}
		}
	}
}
