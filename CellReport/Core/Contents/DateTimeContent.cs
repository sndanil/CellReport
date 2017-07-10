using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Contents
{
	[Serializable]
	public class FormatContentStyle : TextContentStyle
	{
		public FormatContentStyle(HorizontalAlignment h,
						VerticalAlignment v,
						FontInfo font,
						string color,
						float angle,
						string format,
						string culture,
						Direction direction)
						: base(h, v, font, color, angle, direction)
		{
			Format = format;
			if (!string.IsNullOrEmpty(culture))
				Culture = culture;
			else
				Culture = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
		}

		public string Format
		{
			get;
			protected set;
		}

		public string Culture
		{
			get;
			private set;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj))
				return true;

			var other = obj as FormatContentStyle;
			if (other != null)
				return base.Equals(obj) && this.Format.Equals(other.Format)
						&& this.Culture.Equals(other.Culture);
			else
				return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() + this.Culture.GetHashCode()
					+ this.Format.GetHashCode();
		}

		internal new static FormatContentStyle Parse(XElement element)
		{
			var tcs = TextContentStyle.Parse(element);
			var format = string.Empty;
			if (element.Attribute("Format") != null)
				format = element.Attribute("Format").Value;
			var culture = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
			if (element.Attribute("Culture") != null)
				culture = element.Attribute("Culture").Value;
			return new FormatContentStyle(tcs.HAlignment, tcs.VAlignment, tcs.Font, tcs.Color,
								tcs.Angle, format, culture, tcs.Direction);
		}

		protected override XElement OnSerialize(string styleName)
		{
			var result = base.OnSerialize(styleName ?? "FormatContentStyle");
			result.Add(
					new XAttribute("Format", Format),
					new XAttribute("Culture", Culture)
					);
			return result;
		}
	}

	[Serializable]
	public class DateTimeContentStyle : FormatContentStyle
	{
		public DateTimeContentStyle(HorizontalAlignment h,
				VerticalAlignment v,
				FontInfo font,
				string color,
				float angle,
				string format,
				string culture,
				Direction direction)
			: base(h, v, font, color, angle, format, culture, direction)
		{
			if (!string.IsNullOrEmpty(this.Format))
				return;
			var cultureInfo = CultureInfo.GetCultureInfo(this.Culture);
			if (cultureInfo == null)
				return;
			var dateFormat = cultureInfo.DateTimeFormat;
			this.Format = dateFormat.ShortDatePattern + " " + dateFormat.LongTimePattern; // "G" specifier
		}

		internal new static DateTimeContentStyle Parse(XElement element)
		{
			var fcs = FormatContentStyle.Parse(element);
			return new DateTimeContentStyle(fcs.HAlignment, fcs.VAlignment, fcs.Font, fcs.Color,
				fcs.Angle, fcs.Format, fcs.Culture, fcs.Direction);
		}

		protected override XElement OnSerialize(string styleName)
		{
			return base.OnSerialize(styleName ?? "DateTimeContentStyle");
		}
	}

	[Serializable]
	public class DateTimeContent : TextBasedContent
	{
		public new static readonly DateTimeContentStyle DefaultStyle = new DateTimeContentStyle(HorizontalAlignment.Center,
				VerticalAlignment.Middle, FontInfo.Default, "Black", 0, string.Empty, string.Empty, Direction.LeftToRight);

		private DateTime _value = DateTime.MinValue;

		public DateTimeContent(CellManager manager)
				: base(manager)
		{
		}

		public override BaseStyle GetStyle()
		{
			return GetStyle<DateTimeContentStyle>() ?? DefaultStyle;
		}

		public override BaseStyle GetActualStyle()
		{
			var style = this.GetStyle() as DateTimeContentStyle;
			var actualStyle = base.GetActualStyle() as TextContentStyle;
			var format = GetParamValueByTarget("Style.Format") as string ?? style.Format;
			var culture = GetParamValueByTarget("Style.Culture") as string ?? style.Culture;
			return new DateTimeContentStyle(actualStyle.HAlignment, actualStyle.VAlignment, actualStyle.Font,
				actualStyle.Color, actualStyle.Angle, format, culture, actualStyle.Direction);
		}

		public override string GetText()
		{
			var style = (DateTimeContentStyle)GetActualStyle();
			var text = string.Empty;
			if (string.IsNullOrEmpty(style.Format))
				text = GetValue().ToString();
			else if (string.IsNullOrEmpty(style.Culture))
				text = GetValue().ToString(style.Format);
			else
				text = GetValue().ToString(style.Format, CultureInfo.GetCultureInfo(style.Culture));

			return text;
		}

		public DateTime GetValue()
		{
			var valueAsObject = GetParamValueByTarget("Value");
			DateTime value = DateTime.Now;
			if ((valueAsObject is string) && DateTime.TryParse(valueAsObject.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
				return value;

			try
			{
				return valueAsObject != null ? Convert.ToDateTime(valueAsObject) : Value;
			}
			catch (FormatException)
			{
				return DateTime.MinValue;
			}
		}

		public DateTime Value
		{
			get { return _value; }
			set { _value = value; }
		}

		internal new static DateTimeContent Parse(CellManager manager, XElement element)
		{
			var dateTimeContent = new DateTimeContent(manager);
			if (element.Element("Value") != null)
				dateTimeContent.Value = DateTime.Parse(element.Element("Value").Value, CultureInfo.InvariantCulture);
			return dateTimeContent;
		}

		protected override XElement OnSerialize(string contentName)
		{
			var result = base.OnSerialize(contentName ?? "DateTimeContent");
			if (Value != DateTime.MinValue)
				result.Add(new XElement("Value", Value.ToString(CultureInfo.InvariantCulture)));
			return result;
		}

		public override string ToString()
		{
			return "Дата и время";
		}
	}
}
