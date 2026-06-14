using CellReport.Core;
using CellReport.Presenters;
using CellReport.Presenters.Skia;
using SkiaSharp;
using System;

namespace CellReport.Avalonia
{
	public class AvaloniaRenderer : SkiaReportRenderer
	{
		public static new Size GetTextSize(Rectangle rect, string text, string fontFamily, float fontSize, FontStyle fontStyle)
		{
			return SkiaReportRenderer.GetTextSize(rect, text, fontFamily, fontSize, fontStyle);
		}
	}

	public static class AvaloniaRectHelper
	{
		public static Rectangle ToRecttangle(this global::Avalonia.Rect rect)
		{
			return new Rectangle(
				Convert.ToSingle(rect.X),
				Convert.ToSingle(rect.Y),
				Convert.ToSingle(rect.Width),
				Convert.ToSingle(rect.Height));
		}
	}
}
