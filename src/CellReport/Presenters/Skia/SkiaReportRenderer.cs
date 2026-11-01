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
		private static readonly object ImageRenderLock = new object();

		public bool InvertColors { get; set; }

		public float Dpi { get; set; } = 96f;

		/// <summary>
		/// Сглаживание текста. Для печати на термопринтере отключается,
		/// чтобы полутона не превращались в «грязные» точки.
		/// </summary>
		public bool TextAntialias { get; set; } = true;

		public static Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
		{
			return GetTextSize(rect, text, fontFamily, fontSize, fontStyle, 96f);
		}

		public static Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle, float dpi)
		{
			if (string.IsNullOrEmpty(text) || rect.Width <= 0)
				return new Size(rect.Width, rect.Height);

			fontSize = UnitsConverter.ConvertValue(fontSize, GraphicsUnit.Point, GraphicsUnit.Pixel, dpi);

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
			return TransformColor(new SKColor(r, g, b, a));
		}

		protected SKColor TransformColor(SKColor color)
		{
			if (!InvertColors)
				return color;

			return new SKColor(
				(byte)(255 - color.Red),
				(byte)(255 - color.Green),
				(byte)(255 - color.Blue),
				color.Alpha);
		}

		private static readonly SKPathEffect _dashEffect = SKPathEffect.CreateDash(new[] { 4f, 2f }, 0);
		private static readonly SKPathEffect _dotEffect = SKPathEffect.CreateDash(new[] { 1f, 2f }, 0);
		private static readonly SKPathEffect _dashDotEffect = SKPathEffect.CreateDash(new[] { 4f, 2f, 1f, 2f }, 0);
		private static readonly SKPathEffect _dashDotDotEffect = SKPathEffect.CreateDash(new[] { 4f, 2f, 1f, 2f, 1f, 2f }, 0);

		private static SKPathEffect GetDashEffect(LineStyle style)
		{
			switch (style)
			{
				case LineStyle.Dash:
					return _dashEffect;
				case LineStyle.Dot:
					return _dotEffect;
				case LineStyle.DashDot:
					return _dashDotEffect;
				case LineStyle.DashDotDot:
					return _dashDotDotEffect;
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
					if (InvertColors)
					{
						using (var paint = new SKPaint { ColorFilter = SkiaColorFilters.CreateInvert() })
							dc.DrawBitmap(result, cellRect.ToRect(), paint);
					}
					else
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

			var scale = Dpi / 96f;
			textRect = new Rectangle(
				textRect.X + 3 * scale,
				textRect.Y + 1 * scale,
				textRect.Width - 6 * scale,
				textRect.Height - 2 * scale);

			var dc = (SKCanvas)context;
			fontSize = UnitsConverter.ConvertValue(fontSize, GraphicsUnit.Point, GraphicsUnit.Pixel, Dpi);

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
				.Alignment(alignment);

			rs.Add(text);

			rs.MaxWidth = textRect.Width;
			rs.MaxHeight = textRect.Height + 5 * scale;

			var y = textRect.Y;
			if (v == VerticalAlignment.Middle)
				y += Math.Max(0, (textRect.Height - rs.MeasuredHeight) / 2);
			else if (v == VerticalAlignment.Bottom)
				y += Math.Max(0, textRect.Height - rs.MeasuredHeight);

			var paintOptions = new TextPaintOptions
			{
				IsAntialias = TextAntialias
			};
			if (!TextAntialias)
			{
				paintOptions.Hinting = SKFontHinting.Full;
				paintOptions.SubpixelPositioning = false;
				paintOptions.Edging = SKFontEdging.Alias;
			}

			if (InvertColors)
			{
				using (var layerPaint = new SKPaint { ColorFilter = SkiaColorFilters.CreateInvert() })
				{
					dc.SaveLayer(textRect.ToRect(), layerPaint);
					rs.Paint(dc, new SKPoint(textRect.X, y), paintOptions);
					dc.Restore();
				}
			}
			else
				rs.Paint(dc, new SKPoint(textRect.X, y), paintOptions);

			dc.Restore();
		}

		public override bool SupportsOverflow
		{
			get { return true; }
		}

		public override Size GetImageSize(byte[] data)
		{
			if (data == null || data.Length == 0)
				return Size.Empty;

			try
			{
				using (var bitmap = SKBitmap.Decode(data))
				{
					if (bitmap == null)
						return Size.Empty;

					return new Size(bitmap.Width, bitmap.Height);
				}
			}
			catch
			{
				return Size.Empty;
			}
		}

		public override Size MeasureText(string text, string fontFamily, float fontSize, FontStyle fontStyle, float dpi)
		{
			return GetTextSize(new Rectangle(0, 0, 1000000f, 1000000f), text, fontFamily, fontSize, fontStyle, dpi);
		}

		public override object SaveClip(object context)
		{
			var canvas = context as SKCanvas;
			if (canvas != null)
				canvas.Save();

			return null;
		}

		public override void RestoreClip(object context, object state)
		{
			var canvas = context as SKCanvas;
			if (canvas != null)
				canvas.Restore();
		}

		public override void RenderText(object context, TextRenderOptions options, string text, string color,
			HorizontalAlignment h, VerticalAlignment v, float angle, string fontFamily, float fontSize,
			FontStyle fontStyle, Direction direction)
		{
			if (options == null || string.IsNullOrEmpty(text))
				return;

			var layout = options.LayoutRect;
			var clip = options.ClipRect;
			if (layout.Width < 1 || layout.Height < 1 || clip.Width < 1 || clip.Height < 1)
				return;

			var dc = context as SKCanvas;
			if (dc == null)
				return;

			fontSize = UnitsConverter.ConvertValue(fontSize, GraphicsUnit.Point, GraphicsUnit.Pixel, Dpi);

			dc.Save();
			dc.ClipRect(clip.ToRect(), SKClipOperation.Intersect, antialias: false);

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
				.Alignment(alignment);

			rs.Add(text);

			if (options.Ellipsis)
			{
				rs.MaxWidth = Math.Max(1f, clip.Width);
				rs.EllipsisEnabled = true;
			}
			else if (options.NoWrap)
				rs.MaxWidth = 1000000f;
			else
				rs.MaxWidth = Math.Max(1f, layout.Width);

			rs.MaxLines = 1;

			var y = layout.Y;
			if (v == VerticalAlignment.Middle)
				y += Math.Max(0f, (layout.Height - rs.MeasuredHeight) / 2f);
			else if (v == VerticalAlignment.Bottom)
				y += Math.Max(0f, layout.Height - rs.MeasuredHeight);

			var paintOptions = new TextPaintOptions
			{
				IsAntialias = TextAntialias
			};
			if (!TextAntialias)
			{
				paintOptions.Hinting = SKFontHinting.Full;
				paintOptions.SubpixelPositioning = false;
				paintOptions.Edging = SKFontEdging.Alias;
			}

			if (InvertColors)
			{
				using (var layerPaint = new SKPaint { ColorFilter = SkiaColorFilters.CreateInvert() })
				{
					dc.SaveLayer(clip.ToRect(), layerPaint);
					rs.Paint(dc, new SKPoint(layout.X, y), paintOptions);
					dc.Restore();
				}
			}
			else
				rs.Paint(dc, new SKPoint(layout.X, y), paintOptions);

			dc.Restore();
		}

		public override void RenderImage(object context, ImageRenderOptions options, byte[] data)
		{
			if (options == null || data == null || data.Length == 0)
				return;

			var rect = options.Rect;
			var clip = options.ClipRect;
			if (rect.Width < 1 || rect.Height < 1 || clip.Width < 1 || clip.Height < 1)
				return;

			var dc = context as SKCanvas;
			if (dc == null)
				return;

			using (var bitmap = SKBitmap.Decode(data))
			{
				if (bitmap == null)
					return;

				dc.Save();
				dc.ClipRect(clip.ToRect(), SKClipOperation.Intersect, antialias: false);
				if (InvertColors)
				{
					using (var paint = new SKPaint { ColorFilter = SkiaColorFilters.CreateInvert() })
						dc.DrawBitmap(bitmap, rect.ToRect(), paint);
				}
				else
					dc.DrawBitmap(bitmap, rect.ToRect());

				dc.Restore();
			}
		}

		public override void RenderChart(object context, Rectangle rect, ChartContent content)
		{
			var canvas = context as SKCanvas;
			if (canvas == null || rect.Width < 1 || rect.Height < 1 || content == null)
				return;

			var scale = Dpi / 96f;
			if (Math.Abs(scale - 1f) < 0.001f)
			{
				SkiaChartRenderer.Render(canvas, rect, content, InvertColors);
				return;
			}

			canvas.Save();
			canvas.Scale(scale, scale);
			SkiaChartRenderer.Render(
				canvas,
				new Rectangle(rect.X / scale, rect.Y / scale, rect.Width / scale, rect.Height / scale),
				content,
				InvertColors);
			canvas.Restore();
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

			// RichTextKit/Skia font resolution can deadlock when independent reports
			// initialize and render text concurrently. Keep image rendering process-safe.
			lock (ImageRenderLock)
			{
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
}
