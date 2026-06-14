using CellReport.Core;
using SkiaSharp;
using System;

namespace CellReport.Presenters.Skia
{
	public static class SkiaRectHelper
	{
		public static SKRect ToRect(this Rectangle rect)
		{
			return new SKRect(rect.X, rect.Y, rect.Right, rect.Bottom);
		}

		public static Rectangle ToRectangle(this SKRect rect)
		{
			return new Rectangle(
				Convert.ToSingle(rect.Left),
				Convert.ToSingle(rect.Top),
				Convert.ToSingle(rect.Width),
				Convert.ToSingle(rect.Height));
		}
	}
}
