using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellReport.Core
{
    public enum GraphicsUnit
    {
        // Summary:
        //     Specifies the world coordinate system unit as the unit of measure.
        //World = 0,
        //
        // Summary:
        //     Specifies the unit of measure of the display device. Typically pixels for
        //     video displays, and 1/100 inch for printers.
        Display = 1,
        //
        // Summary:
        //     Specifies a device pixel as the unit of measure.
        Pixel = 2,
        //
        // Summary:
        //     Specifies a printer's point (1/72 inch) as the unit of measure.
        Point = 3,
        //
        // Summary:
        //     Specifies the inch as the unit of measure.
        Inch = 4,
        //
        // Summary:
        //     Specifies the document unit (1/300 inch) as the unit of measure.
        //Document = 5,
        Millimeter = 6,
        WpfPoint = 255
    }

    public static class UnitsConverter
    {
        public static float ParseUnit(string valueAsString, GraphicsUnit outputUnit)
        {
            var numberPart = new StringBuilder(valueAsString.Length);
            var unitPart = new StringBuilder(valueAsString.Length);
            foreach (char item in valueAsString)
            {
                if (char.IsDigit(item) || char.IsPunctuation(item))
                    numberPart.Append(item);
                else if (char.IsLetter(item))
                    unitPart.Append(item);
            }
            var value = float.Parse(numberPart.ToString(), System.Globalization.CultureInfo.InvariantCulture);
            switch (unitPart.ToString().ToLower())
            {
                case "pt":
                    return ConvertValue(value, GraphicsUnit.Point, outputUnit, 0);
                case "mm":
                    return ConvertValue(value, GraphicsUnit.Millimeter, outputUnit, 0);
                default:
                    return value;
            }
        }

        public static float ConvertValue(float value, GraphicsUnit fromUnit, GraphicsUnit toUnit, float dpi)
        {
            switch (fromUnit)
            {
                case GraphicsUnit.Pixel:
                    switch (toUnit)
                    {
                        case GraphicsUnit.Millimeter:
                            return value / dpi * 25.4f;
                        case GraphicsUnit.Pixel:
                            return value;
                        default:
                            throw new ArgumentException("toUnit");
                    }
                case GraphicsUnit.Millimeter:
                    switch (toUnit)
                    {
                        case GraphicsUnit.Pixel:
                            return value / 25.4f * dpi;
                        case GraphicsUnit.WpfPoint:
                            return value / 25.4f * 96;
                        case GraphicsUnit.Display:
                            return value / 25.4f * 100;
                        case GraphicsUnit.Millimeter:
                            return value;
                        case GraphicsUnit.Point:
                            return value / 25.4f * 72;
                        case GraphicsUnit.Inch:
                            return value / 25.4f;
                        default:
                            throw new ArgumentException("toUnit");
                    }
                case GraphicsUnit.WpfPoint:
                    switch (toUnit)
                    {
                        case GraphicsUnit.Millimeter:
                            return value / 96 * 25.4f;
                        case GraphicsUnit.WpfPoint:
                            return value;
                        default:
                            throw new ArgumentException("toUnit");
                    }
                case GraphicsUnit.Display:
                    switch (toUnit)
                    {
                        case GraphicsUnit.Millimeter:
                            return value / 100 * 25.4f;
                        case GraphicsUnit.Display:
                            return value;
                        default:
                            throw new ArgumentException("toUnit");
                    }
                case GraphicsUnit.Point:
                    switch (toUnit)
                    {
                        case GraphicsUnit.WpfPoint:
                            return value / 72 * 96;
                        case GraphicsUnit.Millimeter:
                            return value / 72 * 25.4f;
                        case GraphicsUnit.Display:
                            return value / 72 * 100;
                        case GraphicsUnit.Pixel:
                            return value / 72f * dpi;
                        case GraphicsUnit.Inch:
                            return value / 72;
                        default:
                            throw new ArgumentException("toUnit");
                    }
                default:
                    throw new ArgumentException("fromUnit");
            }
        }
    }
}
