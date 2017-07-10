using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CellReport.Core.Contents;
using System.Globalization;

namespace CellReport.Core
{
	[Flags]
	[Serializable]
	public enum FontStyle
	{
		Regular = 0,
		Bold = 1,
		Italic = 2,
		Underline = 4,
		Strikeout = 8,
	}

	[Serializable]
	public class FontInfo
	{
		public static readonly FontInfo Default = new FontInfo("Arial", 10);

		public FontInfo(string family, float size) : this(family, size, FontStyle.Regular)
		{
		}

		public FontInfo(string family, float size, FontStyle style)
		{
			if (family == null)
				throw new ArgumentNullException("family");

			Family = family;
			Size = size;
			Style = style;
		}

		public string Family
		{
			get;
			private set;
		}

		public float Size
		{
			get;
			private set;
		}

		public FontStyle Style
		{
			get;
			private set;
		}

		public override bool Equals(object obj)
		{
			if (base.Equals(obj))
				return true;

			var other = obj as FontInfo;
			if (other != null)
			{
				return this.Family == other.Family
						&& this.Size == other.Size
						&& this.Style == other.Style;
			}
			else
				return false;
		}

		public override int GetHashCode()
		{
			return this.Family.GetHashCode() + this.Size.GetHashCode() + this.Style.GetHashCode();
		}

		internal static FontInfo Parse(XElement element)
		{
			var font = FontInfo.Default;
			if (element != null)
			{
				string family = null;
				float size = 0;
				if (element.Attribute("Family") != null)
					family = element.Attribute("Family").Value;
				if (element.Attribute("Size") != null)
					size = float.Parse(element.Attribute("Size").Value, CultureInfo.InvariantCulture);
				var style = FontStyle.Regular;
				if (element.Attribute("Style") != null)
				{
					string[] styles = element.Attribute("Style").Value.Split(new char[] { ' ', ',', ';' },
							StringSplitOptions.RemoveEmptyEntries);
					foreach (var styleAsString in styles)
					{
						switch (styleAsString)
						{
							case "Bold": style |= FontStyle.Bold; break;
							case "Italic": style |= FontStyle.Italic; break;
							case "Strikeout": style |= FontStyle.Strikeout; break;
							case "Underline": style |= FontStyle.Underline; break;
						}
					}
				}
				if (!string.IsNullOrEmpty(family) && size > 0)
					font = new FontInfo(family, size, style);
			}

			return font;
		}

		internal XElement Serialize()
		{
			return new XElement("Font",
					new XAttribute("Family", Family),
					new XAttribute("Size", Size.ToString(CultureInfo.InvariantCulture)),
					new XAttribute("Style", Style));
		}
	}

	[Serializable]
	public abstract class BaseStyle
	{
		public string ID { get; set; }

		public static void ParseColor(string color, out byte a, out byte r, out byte g, out byte b)
		{
			if (color.StartsWith("#") && (color.Length == 7 || color.Length == 9))
			{
				if (color.Length == 7)
				{
					a = 255;
					r = byte.Parse(color.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
					g = byte.Parse(color.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
					b = byte.Parse(color.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
				}
				else
				{
					a = byte.Parse(color.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
					r = byte.Parse(color.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
					g = byte.Parse(color.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
					b = byte.Parse(color.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);
				}
			}
			else
				ColorTable.TryGetColor(color, out a, out r, out g, out b);
		}

		public static BaseStyle Parse(XElement element)
		{
			BaseStyle style = null;

			switch (element.Name.LocalName)
			{
				case "CellStyle": style = CellStyle.Parse(element); break;
				case "TextContentStyle": style = TextContentStyle.Parse(element); break;
				case "DecimalContentStyle": style = DecimalContentStyle.Parse(element); break;
				case "DateTimeContentStyle": style = DateTimeContentStyle.Parse(element); break;
				case "LineContentStyle": style = LineContentStyle.Parse(element); break;
			}

			if (style != null)
				style.ID = element.Attribute("ID").Value;

			return style;
		}

		public XElement Serialize()
		{
			return OnSerialize(null);
		}

		protected virtual XElement OnSerialize(string styleName)
		{
			return new XElement(styleName ?? "BaseStyle", new XAttribute("ID", ID));
		}

		public override string ToString()
		{
			return ID;
		}
	}

	[Serializable]
	public class ExternalStyle
	{
		public ExternalStyle(string uri)
		{
			Uri = uri;
		}

		public string Uri { get; private set; }
	}
}
