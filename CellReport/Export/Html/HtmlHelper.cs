using CellReport.Core;
using System;
using System.Globalization;

namespace CellReport.Export.Html
{
	internal class HtmlHelper
	{
		public const GraphicsUnit DefaultUnit = GraphicsUnit.Millimeter;
		public const GraphicsUnit HtmlUnit = GraphicsUnit.Pixel;
		public const float HtmlDpi = 96f;

		public static string GetDimension(float value, GraphicsUnit unit = DefaultUnit)
		{
			value = UnitsConverter.ConvertValue(value, unit, HtmlUnit, HtmlDpi);
			// Chrome не показывает border с шириной < 1,
			// поэтому нужно значения из отрезка [0.5, 1] преобразовать в 1.
			if (value >= 0.5 && value < 1)
				value = 1;
			return value.ToString(CultureInfo.InvariantCulture) + "px";
		}

		public static string GetColor(string color)
		{
			byte a, r, g, b;
			BaseStyle.ParseColor(color, out a, out r, out g, out b);
			return string.Format("rgba({0}, {1}, {2}, {3})", r, g, b, a);
		}

		public static string GetBorderStyle(LineStyle style)
		{
			switch (style)
			{
				case LineStyle.None:
				default:
					return "none";
				case LineStyle.Solid:
				case LineStyle.DashDot:
				case LineStyle.DashDotDot:
					return "solid";
				case LineStyle.Dash:
					return "dashed";
				case LineStyle.Dot:
					return "dotted";
			}
		}

		public static string GetBorder(Border border)
		{
			var width = GetDimension(border.Weight);
			var style = GetBorderStyle(border.Style);
			var color = GetColor(border.Color);
			return width + " " + style + " " + color;
		}

		public static string GetEmbeddedImage(byte[] data)
		{
			var base64 = Convert.ToBase64String(data);
			return "data:image/png;base64," + base64;
		}
	}
}
