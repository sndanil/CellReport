using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Contents
{
	[Serializable]
	public class DecimalContentStyle : FormatContentStyle
	{
		public DecimalContentStyle(HorizontalAlignment h,
				VerticalAlignment v,
				FontInfo font,
				string color,
				string negativeColor,
				float angle,
				string format,
				string culture,
				Direction direction)
				: base(h, v, font, color, angle, format, culture, direction)
		{
			NegativeColor = negativeColor;
		}

		public string NegativeColor
		{
			get;
			private set;
		}

		public override bool Equals(object obj)
		{
			var other = obj as DecimalContentStyle;
			if (other != null)
				return base.Equals(obj) && this.NegativeColor.Equals(other.NegativeColor);
			else
				return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() + this.NegativeColor.GetHashCode();
		}

		internal new static DecimalContentStyle Parse(XElement element)
		{
			var dcs = FormatContentStyle.Parse(element);
			HorizontalAlignment halign = HorizontalAlignment.Right;
			if (element.Element("Alignment") != null)
			{
				if (element.Element("Alignment").Attribute("Horizontal") != null)
					halign = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment),
							element.Element("Alignment").Attribute("Horizontal").Value);
			}
			var negativeColor = dcs.Color;
			if (element.Attribute("NegativeColor") != null)
				negativeColor = element.Attribute("NegativeColor").Value;
			return new DecimalContentStyle(halign, dcs.VAlignment, dcs.Font, dcs.Color,
					negativeColor, dcs.Angle, dcs.Format, dcs.Culture, dcs.Direction);
		}

		protected override XElement OnSerialize(string styleName)
		{
			var result = base.OnSerialize(styleName ?? "DecimalContentStyle");
			result.Add(
					new XAttribute("NegativeColor", NegativeColor)
					);
			return result;
		}
	}

	[Serializable]
	public class DecimalContent : TextBasedContent
	{
		public new static readonly DecimalContentStyle DefaultStyle = new DecimalContentStyle(HorizontalAlignment.Right,
				VerticalAlignment.Middle, FontInfo.Default, "Black", "Black", 0, string.Empty, string.Empty, Direction.LeftToRight);

		public DecimalContent(CellManager manager)
				: base(manager)
		{
		}

		public decimal Value
		{
			get;
			set;
		}

		public decimal GetValue()
		{
			var valueAsObject = GetParamValueByTarget("Value");
			if (valueAsObject is string && decimal.TryParse(valueAsObject.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
				return result;
			try
			{
				return valueAsObject != null ? Convert.ToDecimal(valueAsObject) : Value;
			}
			catch (FormatException)
			{
				return 0;
			}
		}

		public override BaseStyle GetStyle()
		{
			return GetStyle<DecimalContentStyle>() ?? DefaultStyle;
		}

		public override BaseStyle GetActualStyle()
		{
			var style = this.GetStyle() as DecimalContentStyle;
			var actualStyle = base.GetActualStyle() as TextContentStyle;
			var format = GetParamValueByTarget("Style.Format") as string ?? style.Format;
			var culture = GetParamValueByTarget("Style.Culture") as string ?? style.Culture;
			var negativeColor = GetParamValueByTarget("Style.NegativeColor") as string ?? style.NegativeColor;
			return new DecimalContentStyle(actualStyle.HAlignment, actualStyle.VAlignment, actualStyle.Font,
				actualStyle.Color, negativeColor, actualStyle.Angle, format, culture, actualStyle.Direction);
		}

		public override string GetText()
		{
			var style = (DecimalContentStyle)GetActualStyle();
			try
			{
				string text;
				if (string.IsNullOrEmpty(style.Format))
					text = GetValue().ToString(CultureInfo.CurrentCulture);
				else if (string.IsNullOrEmpty(style.Culture))
					text = GetValue().ToString(style.Format);
				else
					text = GetValue().ToString(style.Format, CultureInfo.GetCultureInfo(style.Culture));
				return text;
			}
			catch (FormatException e)
			{
				return GetValue().ToString(CultureInfo.CurrentCulture);
			}
		}

		internal override string GetColor()
		{
			var actualStyle = (DecimalContentStyle)GetActualStyle();
			if (GetValue() < 0)
				return actualStyle.NegativeColor;
			else
				return actualStyle.Color;
		}

		internal new static DecimalContent Parse(CellManager manager, XElement element)
		{
			var decimalContent = new DecimalContent(manager);
			if (element.Element("Value") != null)
			{
				var value = 0m;
				decimal.TryParse(element.Element("Value").Value, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
				decimalContent.Value = value;
			}
			return decimalContent;
		}

		protected override XElement OnSerialize(string contentName)
		{
			var result = base.OnSerialize(contentName ?? "DecimalContent");
			if (Value != 0)
				result.Add(new XElement("Value", Value.ToString(CultureInfo.InvariantCulture)));
			return result;
		}

		public override string ToString()
		{
			return "Число";
		}

		public override bool FindText(string text, bool caseSensetive)
		{
			text = text.Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator, string.Empty);
			var ourText = GetText().Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator, string.Empty);
			if (caseSensetive)
			{
				return ourText.Contains(text);
			}
			else
			{
				return ourText.ToUpper().Contains(text.ToUpper());
			}
		}
	}
}
