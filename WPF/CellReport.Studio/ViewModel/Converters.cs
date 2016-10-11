using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace CellReport.Studio.ViewModel
{
    public class WPFUnitsConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch(parameter as string)
            {
                case "VAlign":
                    switch ((CellReport.Core.Contents.VerticalAlignment)value)
	                {
                        default:
                        case CellReport.Core.Contents.VerticalAlignment.Top:
                            return System.Windows.VerticalAlignment.Top;
                        case CellReport.Core.Contents.VerticalAlignment.Center:
                            return System.Windows.VerticalAlignment.Center;
                        case CellReport.Core.Contents.VerticalAlignment.Bottom:
                            return System.Windows.VerticalAlignment.Bottom;
                    }
                case "HAlign":
                    switch ((CellReport.Core.Contents.HorizontalAlignment)value)
                    {
                        default:
                        case CellReport.Core.Contents.HorizontalAlignment.Left:
                            return System.Windows.HorizontalAlignment.Left;
                        case CellReport.Core.Contents.HorizontalAlignment.Center:
                            return System.Windows.HorizontalAlignment.Center;
                        case CellReport.Core.Contents.HorizontalAlignment.Right:
                            return System.Windows.HorizontalAlignment.Right;
                    }
                case "FontStyle":
                    return ((CellReport.Core.FontStyle)value).HasFlag(CellReport.Core.FontStyle.Italic) ? System.Windows.FontStyles.Italic : System.Windows.FontStyles.Normal;
                case "FontWeight":
                    return ((CellReport.Core.FontStyle)value).HasFlag(CellReport.Core.FontStyle.Bold) ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
                case "FontFamily":
                    return new FontFamily(value.ToString());
                case "PointToWPF":
                    return CellReport.Core.UnitsConverter.ConvertValue(System.Convert.ToSingle(value), Core.GraphicsUnit.Point, Core.GraphicsUnit.WpfPoint, 0);
				case "DashStyle":
					switch ((CellReport.Core.LineStyle)value)
					{
						default:
						case CellReport.Core.LineStyle.None:
							return null;
						case CellReport.Core.LineStyle.Solid:
							return DashStyles.Solid.Dashes;
						case CellReport.Core.LineStyle.Dash:
							return DashStyles.Dash.Dashes;
						case CellReport.Core.LineStyle.Dot:
							return DashStyles.Dot.Dashes;
						case CellReport.Core.LineStyle.DashDot:
							return DashStyles.DashDot.Dashes;
						case CellReport.Core.LineStyle.DashDotDot:
							return DashStyles.DashDotDot.Dashes;
					}
                default:
                    return CellReport.Core.UnitsConverter.ConvertValue(System.Convert.ToSingle(value), Core.GraphicsUnit.Millimeter, Core.GraphicsUnit.WpfPoint, 0);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return CellReport.Core.UnitsConverter.ConvertValue(System.Convert.ToSingle(value), Core.GraphicsUnit.WpfPoint, Core.GraphicsUnit.Millimeter, 0);
        }

    }

    public class DecimalStyleConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var style = value as CellReport.Core.Contents.DecimalContentStyle;
            if (style == null)
                return null;

            switch (parameter.ToString())
            {
                case "Positive":
                    return (1234567890.1234567890123456789m).ToString(style.Format);
                case "Negative":
                    return (-1234567890.1234567890123456789m).ToString(style.Format);
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
            var style = value as CellReport.Core.Contents.DateTimeContentStyle;
            if (style == null)
                return null;

            if (string.IsNullOrEmpty(style.Format))
                return (new DateTime(2010, 6, 26, 16, 15, 5)).ToString();
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
