using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CellReport.Core.Contents
{
	[Serializable]
	public enum HorizontalAlignment
	{
		Left,
		Center,
		Right,
		Justify
	}

	[Serializable]
	public enum VerticalAlignment
	{
		Top = 0,
		Middle = 1,
		Center = 1,
		Bottom = 2
	}

	[Serializable]
	public enum Direction
	{
		LeftToRight,
		TopToDown
	}

	[Serializable]
	public class TextContentStyle : BaseStyle
	{
		public TextContentStyle(HorizontalAlignment h, VerticalAlignment v, FontInfo font, string color, float angle, Direction direction)
		{
			Direction = direction;
			HAlignment = h;
			VAlignment = v;

			Font = font ?? throw new ArgumentNullException(nameof(font));
			Color = color ?? throw new ArgumentNullException(nameof(color));
			Angle = angle;
		}

		public TextContentStyle(HorizontalAlignment h, VerticalAlignment v, FontInfo font, string color) :
				this(h, v, font, color, 0, Direction.LeftToRight)
		{
		}

		public HorizontalAlignment HAlignment
		{
			get;
		}

		public VerticalAlignment VAlignment
		{
			get;
		}

		public Direction Direction
		{
			get;
		}

		public FontInfo Font
		{
			get;
		}

		public string Color
		{
			get;
		}

		public float Angle
		{
			get;
		}

		public override bool Equals(object obj)
		{
			if (base.Equals(obj))
				return true;

			var other = obj as TextContentStyle;
			if (other != null)
			{
				return Color == other.Color
						&& Direction == other.Direction
						&& HAlignment == other.HAlignment
						&& VAlignment == other.VAlignment
						&& Angle == other.Angle
						&& Font.Equals(other.Font)
						&& GetType() == other.GetType();
			}
			else
				return false;
		}

		public override int GetHashCode()
		{
			return Color.GetHashCode() + HAlignment.GetHashCode() + VAlignment.GetHashCode()
					+ Font.GetHashCode() + Direction.GetHashCode() + Angle.GetHashCode();
		}

		internal new static TextContentStyle Parse(XElement element)
		{
			Direction direction = Direction.LeftToRight;
			if (element.Attribute("Direction") != null)
				direction = (Direction)Enum.Parse(typeof(Direction), element.Attribute("Direction").Value);
			float angle = 0;
			if (element.Attribute("Angle") != null)
				angle = float.Parse(element.Attribute("Angle").Value, CultureInfo.InvariantCulture);
			var color = "Black";
			if (element.Attribute("Color") != null)
				color = element.Attribute("Color").Value;
			HorizontalAlignment halign = HorizontalAlignment.Left;
			VerticalAlignment valign = VerticalAlignment.Top;
			if (element.Element("Alignment") != null)
			{
				if (element.Element("Alignment").Attribute("Horizontal") != null)
					halign = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment),
							element.Element("Alignment").Attribute("Horizontal").Value);
				if (element.Element("Alignment").Attribute("Vertical") != null)
					valign = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment),
							element.Element("Alignment").Attribute("Vertical").Value);
			}
			var font = FontInfo.Parse(element.Element("Font"));

			return new TextContentStyle(halign, valign, font, color, angle, direction);
		}

		protected override XElement OnSerialize(string styleName)
		{
			var result = base.OnSerialize(styleName ?? "TextContentStyle");
			result.Add(
					new XAttribute("Color", Color),
					new XAttribute("Direction", Direction),
					new XAttribute("Angle", Angle.ToString(CultureInfo.InvariantCulture)),
					new XElement("Alignment", new XAttribute("Horizontal", HAlignment), new XAttribute("Vertical", VAlignment)),
					Font.Serialize()
					);

			return result;
		}
	}

	[Serializable]
	public abstract class TextBasedContent : BaseContent
	{
		private const int MaxTextLengthForRender = 4096;
		public static readonly TextContentStyle DefaultStyle = new TextContentStyle(HorizontalAlignment.Center,
				VerticalAlignment.Middle, FontInfo.Default, "Black");

		public TextBasedContent(CellManager manager)
				: base(manager)
		{
		}

		public abstract string GetText();

		public virtual string GetTextForRender()
		{
			var result = GetText();
			result = result.Length > MaxTextLengthForRender ? result.Substring(0, MaxTextLengthForRender - 3) + "..." : result;

			return result;
		}

		public override BaseStyle GetActualStyle()
		{
			var style = (TextContentStyle)GetStyle();
			var color = GetParamValueByTarget("Style.Color") as string ?? style.Color;
			var fontFamily = GetParamValueByTarget("Style.Font.Family") as string ?? style.Font.Family;

			var obj = GetParamValueByTarget("Style.Font.Size");
			var fontSize = (obj != null) ? Convert.ToSingle(obj) : style.Font.Size;

			obj = GetParamValueByTarget("Style.Font.Style");
			if (obj is string)
				obj = Enum.Parse(typeof(FontStyle), (string)obj);
			var fontStyle = (obj != null) ? (FontStyle)Convert.ToInt32(obj) : style.Font.Style;

			obj = GetParamValueByTarget("Style.HAlignment");
			if (obj is string)
				obj = Enum.Parse(typeof(HorizontalAlignment), (string)obj);
			var hAlign = (obj != null) ? (HorizontalAlignment)Convert.ToInt32(obj) : style.HAlignment;

			obj = GetParamValueByTarget("Style.VAlignment");
			if (obj is string)
				obj = Enum.Parse(typeof(VerticalAlignment), (string)obj);
			var vAlign = (obj != null) ? (VerticalAlignment)Convert.ToInt32(obj) : style.VAlignment;

			obj = GetParamValueByTarget("Style.Direction");
			if (obj is string)
				obj = Enum.Parse(typeof(Direction), (string)obj);
			var direction = (obj != null) ? (Direction)Convert.ToInt32(obj) : style.Direction;

			obj = GetParamValueByTarget("Style.Angle");
			var angle = (obj != null) ? Convert.ToSingle(obj) : style.Angle;

			var font = new FontInfo(fontFamily, fontSize, fontStyle);
			return new TextContentStyle(hAlign, vAlign, font, color, angle, direction);
		}

		internal virtual string GetColor()
		{
			return ((TextContentStyle)GetActualStyle()).Color;
		}

		public virtual bool FindText(string text, bool caseSensetive)
		{
			if (caseSensetive)
			{
				return GetText().Contains(text);
			}
			else
			{
				return GetText().ToUpper().Contains(text.ToUpper());
			}
		}
	}

	[Serializable]
	public class TextContent : TextBasedContent
	{
		private string _text;
		private bool _autoSizeFont;
		private string _template;

		public TextContent(CellManager manager)
				: base(manager)
		{
		}

		public override BaseStyle GetStyle()
		{
			return GetStyle<TextContentStyle>() ?? DefaultStyle;
		}

		public string Text
		{
			get { return _text; }
			set
			{
				if (_text != value)
				{
					_text = value;
					OnPropertyChanged("Text");
				}
			}
		}

		public bool AutoSizeFont
		{
			get { return _autoSizeFont; }
			set
			{
				if (_autoSizeFont != value)
				{
					_autoSizeFont = value;
					//SetField(ref _autoSizeFont, value);
					OnPropertyChanged("AutoSizeFont");
				}
			}
		}

		public string Template
		{
			get { return _template; }
			set
			{
				if (_template != value)
				{
					_template = value;
					OnPropertyChanged("Template");
				}
			}
		}

		internal new static TextContent Parse(CellManager manager, XElement element)
		{
			var textContent = new TextContent(manager);
			if (element.Element("Text") != null)
				textContent.Text = element.Element("Text").Value;
			if (element.Attribute("AutoSizeFont") != null)
				textContent.AutoSizeFont = Convert.ToBoolean(element.Attribute("AutoSizeFont").Value);
			if (element.Element("Template") != null)
				textContent.Template = element.Element("Template").Value;
			return textContent;
		}

		public override string GetText()
		{
			string result = null;
			var template = GetParamValueByTarget("Template") as string ?? Template;
			if (string.IsNullOrEmpty(template))
				result = (GetParamValueByTarget("Text") as string ?? Text) ?? string.Empty;
			else
			{
				var reg = new Regex(@"\[(?=[^\[]*\]).*?\]");
				var matches = reg.Matches(template);
				foreach (Match match in matches)
				{
					var paramName = match.ToString().Replace("]", string.Empty).Replace("[", string.Empty);
					var value = GetFormattedParamValueByName(paramName);
					if (value != null)
						template = template.Replace(match.ToString(), value);
				}
				result = template;
			}

			return result;
		}

		protected override XElement OnSerialize(string contentName)
		{
			var result = base.OnSerialize(contentName ?? "TextContent");
			if (!string.IsNullOrEmpty(Text))
				result.Add(new XElement("Text", Text));
			if (AutoSizeFont)
				result.Add(new XAttribute("AutoSizeFont", "true"));
			if (!string.IsNullOrEmpty(Template))
				result.Add(new XElement("Template", Template));
			return result;
		}

		public override string ToString()
		{
			var text = Text ?? string.Empty;
			text = text.Length > 10 ? text.Substring(0, 10) + "..." : text;
			return string.Format("Текст ({0})", text);
		}
	}
}
