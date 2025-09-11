using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using CellReport.Core;
using CellReport.Core.Contents;
using ZXing;
using ZXing.OneD;
using ZXing.PDF417;
using ZXing.QrCode;
using FontStyle = CellReport.Core.FontStyle;
using Rectangle = CellReport.Core.Rectangle;

namespace CellReport.Presenters.WinForms
{
	public static class RectHelper
	{
		public static RectangleF ToRectangleF(this Core.Rectangle rect)
		{
			return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
		}

		public static System.Drawing.Rectangle ToRectangle(this Core.Rectangle rect)
		{
			return new System.Drawing.Rectangle(
					Convert.ToInt32(rect.X),
					Convert.ToInt32(rect.Y),
					Convert.ToInt32(rect.Width),
					Convert.ToInt32(rect.Height));
		}

		public static Core.Rectangle ToRecttangle(this RectangleF rect)
		{
			return new Core.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
		}

		public static Core.Rectangle ToCellRecttangle(this System.Drawing.Rectangle rect)
		{
			return new Core.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
		}
	}

	public class GdiPlusRenderer : BaseRenderer
	{
		private static readonly Image TextSizeImage = new Bitmap(10, 10);

		public static Core.Size GetTextSize(Core.Rectangle rect, string text, string fontFamily, float fontSize, Core.FontStyle fontStyle)
		{
			SizeF result = new SizeF(rect.Width, rect.Height);
			if ((int)rect.Width > 0)
			{
				var stringFormat = new StringFormat
				{
					FormatFlags = StringFormatFlags.MeasureTrailingSpaces,
					Alignment = StringAlignment.Near,
					LineAlignment = StringAlignment.Near
				};

				using (var font = new Font(fontFamily, fontSize, (System.Drawing.FontStyle)fontStyle))
				using (var g = Graphics.FromImage(TextSizeImage))
				{
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
					result = g.MeasureString(text, font, Convert.ToInt32(rect.Width), stringFormat);
				}

				if (result.Width > rect.Width)
				{
					var lastHeight = 0;
					var imageRect = new RectangleF(0, 0, rect.Width, Math.Max(rect.Height, 1) * 10);
					using (var bmp = new Bitmap((int)imageRect.Width, (int)imageRect.Height))
					{
						using (var font = new Font(fontFamily, fontSize, (System.Drawing.FontStyle)fontStyle))
						using (Graphics gfx = Graphics.FromImage(bmp))
						{
							gfx.DrawString(text, font, Brushes.Black, imageRect, stringFormat);
						}

						for (int y = 0; y < bmp.Height; y++)
						{
							for (int x = 0; x < bmp.Width; x++)
							{
								if (bmp.GetPixel(x, y).Name != "0")
								{
									lastHeight = y;
									break;
								}
							}
						}
					}

					return new Core.Size(rect.Width, lastHeight > 0 ? lastHeight + 1 : 0);
				}
			}

			return new Core.Size(result.Width, result.Height);
		}

		public override void RenderFillRect(object context, Core.Rectangle rect, float angle, params string[] colors)
		{
			if (rect.Width < 1 || rect.Height < 1)
				return;

			var g = (Graphics)context;
			var cellRect = rect.ToRectangleF();
			var color = GetColor(colors[0]);
			if (color != Color.Transparent)
			{
				//try
				//{
				using (var brush = new SolidBrush(color))
					g.FillRectangle(brush, cellRect);
				//}
				//catch (OverflowException)
				//{
				//    // TODO не могу поймать эту ошибку
				//}
			}
		}

		public override void RenderText(object context, Core.Rectangle textRect, string text, string color,
				HorizontalAlignment h, VerticalAlignment v, float angle, string fontFamily, float fontSize, Core.FontStyle fontStyle, Direction direction)
		{
			if (textRect.Width < 1 || textRect.Height < 1)
				return;

			var g = (Graphics)context;
			var stringFormat = new StringFormat
			{
				FormatFlags = StringFormatFlags.MeasureTrailingSpaces,
				Alignment = StringAlignment.Center,
				LineAlignment = StringAlignment.Center
			};
			var transformPoint = new PointF(textRect.X + textRect.Width / 2, textRect.Y + textRect.Height / 2);
			switch (h)
			{
				case HorizontalAlignment.Left:
					stringFormat.Alignment = StringAlignment.Near;
					transformPoint.X = textRect.X;
					break;
				case HorizontalAlignment.Right:
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
			if (Math.Abs(angle - 0) > float.Epsilon)
			{
				var gOldState = g.Save();
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
							g.DrawString(c.ToString(CultureInfo.InvariantCulture), font, brush, new RectangleF(textRect.X, startYPoint, textRect.Width, textHeight), stringFormat);
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

		public override void RenderRect(object context, Core.Rectangle rect, string color, LineStyle style, float lineWidth)
		{
			if (rect.Width < 1 || rect.Height < 1 || style == LineStyle.None)
				return;

			var g = (Graphics)context;
			using (var pen = new Pen(GetColor(color), lineWidth))
			{
				pen.DashStyle = (DashStyle)style;
				g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
			}
		}

		public override void RenderLines(object context, string color, float lineWidth, LineStyle style, Core.Point[] points)
		{
			if (style != LineStyle.None)
			{
				var g = (Graphics)context;
				using (var pen = new Pen(GetColor(color), lineWidth))
				{
                    pen.DashStyle = (DashStyle)style;
					if (points.Length == 5)
					{
						var path = new GraphicsPath();

						path.StartFigure();
						path.AddLines(Array.ConvertAll(points, p => new PointF(p.X, p.Y)));
						path.CloseFigure();
						g.DrawPath(pen, path);
					}
					else
					{
						g.DrawLines(pen, Array.ConvertAll(points, p => new PointF(p.X, p.Y)));
					}
				}
			}
		}

		public override void RenderImage(object context, Core.Rectangle cellRect, byte[] data)
		{
			if (cellRect.Width < 1 || cellRect.Height < 1)
				return;

			var g = (Graphics)context;
			if (data != null && data.Length > 0)
			{
				using (var img = Image.FromStream(new MemoryStream(data)))
					g.DrawImage(img, cellRect.ToRectangle());
			}
		}

		public override void RenderChart(object context, Core.Rectangle rect, ChartContent content)
		{
#if NET48
			var g = (Graphics)context;
			System.Windows.Forms.DataVisualization.Charting.Chart chart;
			if (content.ChartObject == null)
			{
				chart = new System.Windows.Forms.DataVisualization.Charting.Chart
				{
					AntiAliasing = System.Windows.Forms.DataVisualization.Charting.AntiAliasingStyles.All,
					TextAntiAliasingQuality = System.Windows.Forms.DataVisualization.Charting.TextAntiAliasingQuality.High
				};
				var area = chart.ChartAreas.Add("Default");
				area.Area3DStyle.Enable3D = content.Use3D;
				if (content.ShowLegend)
					chart.Legends.Add("Default");
				foreach (var series in content.GetSeries())
				{
					var chartSeries = chart.Series.Add(series.Name);
					chartSeries.ChartType = (System.Windows.Forms.DataVisualization.Charting.SeriesChartType)series.Type;
					var xval = content.GetXDataBySeries(series);
					var yval = content.GetYDataBySeries(series);
					chartSeries.Points.DataBindXY(xval, yval);
				}

				content.ChartObject = chart;
			}
			else
				chart = (System.Windows.Forms.DataVisualization.Charting.Chart)content.ChartObject;

			chart.Printing.PrintPaint(g, rect.ToRectangle());
#endif
		}

		protected Color GetColor(string color)
		{
			byte a, r, g, b;
			BaseStyle.ParseColor(color, out a, out r, out g, out b);
			return Color.FromArgb(a, r, g, b);
		}

		public override void SetClip(object context, Core.Rectangle rect)
		{
			if (context == null)
				return;

			var g = (Graphics)context;
			g.SetClip(rect.ToRectangleF());
		}

		public override byte[] CreateImage(int width, int height, Action<object> renderAction)
		{
			using (var bmp = new Bitmap(width, height))
			using (var g = Graphics.FromImage(bmp))
			using (var stream = new MemoryStream())
			{
				renderAction(g);
				bmp.Save(stream, ImageFormat.Png);
				return stream.ToArray();
			}
		}

		public override void RenderBarCode(object context, Rectangle cellRect, BarCodeContent barcode, string color, string fontFamily, float fontSize, FontStyle fontStyle, Orientation orientation)
		{
			if (cellRect.Width < 1 || cellRect.Height < 1)
				return;

			var writer = new ZXing.Windows.Compatibility.BarcodeWriter();
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
					options = new Code128EncodingOptions
					{
						Width = Convert.ToInt32(cellRect.Width),
						Height = Convert.ToInt32(cellRect.Height),
						Margin = 0,
						PureBarcode = true
					};
					break;
				case BarCodeType.QR:
					writer.Format = BarcodeFormat.QR_CODE;
					options = new QrCodeEncodingOptions
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
					options = new PDF417EncodingOptions
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
							options = new QrCodeEncodingOptions
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
				var result = writer.Write(barcode.GetCode());

				var g = (Graphics)context;
				using (var img = new Bitmap(result))
					g.DrawImage(img, cellRect.ToRectangle());
			}
			catch
			{
			}
		}
	}
}
