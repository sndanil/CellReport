using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CellReport.Core.Contents;
using HorizontalAlignment = CellReport.Core.Contents.HorizontalAlignment;
using VerticalAlignment = CellReport.Core.Contents.VerticalAlignment;

namespace CellReport.Editor.ViewModel
{
	[ValueConversion(typeof(bool), typeof(Visibility))]
	public sealed class BoolToVisibilityConverter : IValueConverter
	{
		public Visibility TrueValue { get; set; }
		public Visibility FalseValue { get; set; }

		public BoolToVisibilityConverter()
		{
			// set defaults
			TrueValue = Visibility.Visible;
			FalseValue = Visibility.Collapsed;
		}

		public object Convert(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			if (!(value is bool))
				return null;
			return (bool)value ? TrueValue : FalseValue;
		}

		public object ConvertBack(object value, Type targetType,
			object parameter, CultureInfo culture)
		{
			if (Equals(value, TrueValue))
				return true;
			if (Equals(value, FalseValue))
				return false;
			return null;
		}
	}

	[ValueConversion(typeof(bool), typeof(bool))]
	public class InvertBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool booleanValue = (bool)value;
			return !booleanValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool booleanValue = (bool)value;
			return !booleanValue;
		}
	}

	public class WpfUnitsConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch (parameter as string)
			{
				case "VAlign":
					if (value != null)
						switch ((VerticalAlignment) value)
						{
							default:
							//case VerticalAlignment.Top:
								return System.Windows.VerticalAlignment.Top;
							case VerticalAlignment.Center:
								return System.Windows.VerticalAlignment.Center;
							case VerticalAlignment.Bottom:
								return System.Windows.VerticalAlignment.Bottom;
						}
					break;
				case "HAlign":
					if (value != null)
						switch ((HorizontalAlignment) value)
						{
							default:
							//case HorizontalAlignment.Left:
								return System.Windows.HorizontalAlignment.Left;
							case HorizontalAlignment.Center:
								return System.Windows.HorizontalAlignment.Center;
							case HorizontalAlignment.Right:
								return System.Windows.HorizontalAlignment.Right;
						}
					break;
				case "FontStyle":
					return value != null && ((Core.FontStyle)value).HasFlag(Core.FontStyle.Italic) ? System.Windows.FontStyles.Italic : System.Windows.FontStyles.Normal;
				case "FontWeight":
					return value != null && ((Core.FontStyle)value).HasFlag(Core.FontStyle.Bold) ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
				case "FontFamily":
					if (value != null) return new FontFamily(value.ToString());
					break;
				case "PointToWPF":
					return Core.UnitsConverter.ConvertValue(System.Convert.ToSingle(value), Core.GraphicsUnit.Point, Core.GraphicsUnit.WpfPoint, 0);
				case "DashStyle":
					if (value != null)
						switch ((Core.LineStyle) value)
						{
							default:
							//case Core.LineStyle.None:
								return null;
							case Core.LineStyle.Solid:
								return DashStyles.Solid.Dashes;
							case Core.LineStyle.Dash:
								return DashStyles.Dash.Dashes;
							case Core.LineStyle.Dot:
								return DashStyles.Dot.Dashes;
							case Core.LineStyle.DashDot:
								return DashStyles.DashDot.Dashes;
							case Core.LineStyle.DashDotDot:
								return DashStyles.DashDotDot.Dashes;
						}
					break;
				default:
					return Core.UnitsConverter.ConvertValue(System.Convert.ToSingle(value), Core.GraphicsUnit.Millimeter, Core.GraphicsUnit.WpfPoint, 0);
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Core.UnitsConverter.ConvertValue(System.Convert.ToSingle(value), Core.GraphicsUnit.WpfPoint, Core.GraphicsUnit.Millimeter, 0);
		}

	}

	public class DecimalStyleConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is DecimalContentStyle style))
				return null;

			switch (parameter?.ToString())
			{
				case "Positive":
					return (1234.12345m).ToString(style.Format);
				case "Negative":
					return (-1234.12345m).ToString(style.Format);
				case "Zero":
					return (0m).ToString(style.Format);
			}
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}

	}

	public class DateTimeStyleConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is DateTimeContentStyle style))
				return null;

			if (string.IsNullOrEmpty(style.Format))
				return (new DateTime(2010, 6, 26, 16, 15, 5)).ToString(CultureInfo.CurrentCulture);
			if (string.IsNullOrEmpty(style.Culture))
				return (new DateTime(2010, 6, 26, 16, 15, 5)).ToString(style.Format);

			return (new DateTime(2010, 6, 26, 16, 15, 5)).ToString(style.Format, CultureInfo.GetCultureInfo(style.Culture));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}

	}
}
