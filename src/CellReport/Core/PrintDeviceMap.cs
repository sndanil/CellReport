using System;

namespace CellReport.Core
{
	public static class PrintDeviceMap
	{
		public const float MaxRasterDpi = 300f;

		public static float CapRasterDpi(float dpi)
		{
			if (dpi <= 0)
				return 96f;
			return dpi > MaxRasterDpi ? MaxRasterDpi : dpi;
		}

		/// <summary>
		/// Переводит прямоугольник из единиц Graphics (обычно сотые дюйма) в пиксели устройства.
		/// unitsPerHundredth — сколько этих единиц приходится на одну сотую дюйма.
		/// </summary>
		public static void ToDevicePixels(
			float x,
			float y,
			float width,
			float height,
			float unitsPerHundredth,
			float dpiX,
			float dpiY,
			out int deviceX,
			out int deviceY,
			out int deviceWidth,
			out int deviceHeight)
		{
			if (unitsPerHundredth < 0.05f)
				unitsPerHundredth = 1f;
			if (dpiX <= 0)
				dpiX = 96f;
			if (dpiY <= 0)
				dpiY = 96f;

			deviceX = (int)Math.Round(x / unitsPerHundredth * dpiX / 100f);
			deviceY = (int)Math.Round(y / unitsPerHundredth * dpiY / 100f);
			deviceWidth = (int)Math.Round(width / unitsPerHundredth * dpiX / 100f);
			deviceHeight = (int)Math.Round(height / unitsPerHundredth * dpiY / 100f);
		}
	}
}
