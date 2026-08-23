using System;
using System.Xml.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using Mustache;
using System.Collections.Generic;
using System.Linq;

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
						&& Math.Abs(Angle - other.Angle) < float.Epsilon
						&& Font.Equals(other.Font)
						&& GetType() == other.GetType();
			}
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
				direction = (Direction)Enum.Parse(typeof(Direction), element.Attribute("Direction")?.Value ?? "LeftToRight");
			float angle = 0;
			if (element.Attribute("Angle") != null)
				angle = float.Parse(element.Attribute("Angle")?.Value ?? "0", CultureInfo.InvariantCulture);
			var color = "Black";
			if (element.Attribute("Color") != null)
				color = element.Attribute("Color")?.Value;
			HorizontalAlignment halign = HorizontalAlignment.Left;
			VerticalAlignment valign = VerticalAlignment.Top;
			if (element.Element("Alignment") != null)
			{
				if (element.Element("Alignment")?.Attribute("Horizontal") != null)
					halign = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), element.Element("Alignment")?.Attribute("Horizontal")?.Value ?? "");
				if (element.Element("Alignment")?.Attribute("Vertical") != null)
					valign = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), element.Element("Alignment")?.Attribute("Vertical")?.Value ?? "");
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

		private Single ConvertToSingle(object value, Single defaultValue)
		{
			if (value == null)
				return defaultValue;

			try
			{
				return Convert.ToSingle(value);
			}
			catch
			{
				return defaultValue;
			}		
		}

		private int ConvertToInt(object value, int defaultValue)
		{
			if (value == null)
				return defaultValue;

			try
			{
				return Convert.ToInt32(value);
			}
			catch
			{
				return defaultValue;
			}
		}

		private int ConvertToEnum(Type enumType, object value, int defaultValue)
		{
			if (value == null)
				return defaultValue;

			try
			{
				if (value is string)
					value = Enum.Parse(enumType, (string)value);

				return Convert.ToInt32(value);
			}
			catch
			{
				return defaultValue;
			}
		}

		public override BaseStyle GetActualStyle()
		{
			var style = (TextContentStyle)GetStyle();
			try
			{
				var color = GetParamValueByTarget("Style.Color") as string ?? style.Color;
				var fontFamily = GetParamValueByTarget("Style.Font.Family") as string ?? style.Font.Family;

				var obj = GetParamValueByTarget("Style.Font.Size");
				var fontSize = ConvertToSingle(obj, style.Font.Size);

				obj = GetParamValueByTarget("Style.Font.Style");
				var fontStyle = (FontStyle)ConvertToEnum(typeof(FontStyle), obj, (int)style.Font.Style);

				obj = GetParamValueByTarget("Style.HAlignment");
				var hAlign = (HorizontalAlignment)ConvertToEnum(typeof(HorizontalAlignment), obj, (int)style.HAlignment);

				obj = GetParamValueByTarget("Style.VAlignment");
				var vAlign = (VerticalAlignment)ConvertToEnum(typeof(VerticalAlignment), obj, (int)style.VAlignment);

				obj = GetParamValueByTarget("Style.Direction");
				var direction = (Direction)ConvertToEnum(typeof(Direction), obj, (int)style.Direction);

				obj = GetParamValueByTarget("Style.Angle");
				var angle = ConvertToSingle(obj, style.Angle);

				var font = new FontInfo(fontFamily, fontSize, fontStyle);
				return new TextContentStyle(hAlign, vAlign, font, color, angle, direction);
			}
			catch
			{
				return style;
			}
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
			return GetText().ToUpper().Contains(text.ToUpper());
		}
	}

	[Serializable]
	public class TextContent : TextBasedContent
	{
		private string _text;
		private bool _autoSizeFont;

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
			get => _text;
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
			get => _autoSizeFont;
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

		internal new static TextContent Parse(CellManager manager, XElement element)
		{
			var textContent = new TextContent(manager);
			if (element.Element("Text") != null)
				textContent.Text = element.Element("Text")?.Value;
			if (element.Attribute("AutoSizeFont") != null)
				textContent.AutoSizeFont = Convert.ToBoolean(element.Attribute("AutoSizeFont")?.Value);

			// избавляемся от темплейта
			if (element.Element("Template") != null)
				textContent.Text = element.Element("Template")?.Value;

			if (!string.IsNullOrEmpty(textContent.Text))
			{
				var reg = new Regex(@"\[(?=[^\[]*\]).*?\]");
				var matches = reg.Matches(textContent.Text);
				foreach (Match match in matches)
				{
					if (textContent.Text.Contains($"[{match.ToString()}]"))
						continue;

					var paramName = match.ToString().Replace("]", string.Empty).Replace("[", string.Empty);
					textContent.AddParam(new Param(manager, textContent) { Name = paramName, IsVirtual = true });
				}
			}

			return textContent;
		}

		protected class FormatParameter
		{
			public string Name { get; set; }

			public string Format { get; set; }
		}


		public override string GetText()
		{
			string result;
			var text = Text ?? string.Empty;
			if (!CellManager.IsDesignMode && TemplateLocalizer.IsFullKey(text))
				return TemplateLocalizer.GetString(_manager.Properties.Localization, text, _manager.Properties.Culture);
			if (string.IsNullOrEmpty(Text))
			{
				var paramName = GetParamNameByTarget("Text");
				if (paramName == null)
					result = text;
				else
				{
					result = GetFormattedParamValueByName(paramName) ?? text;
					if (CellManager.IsDesignMode && string.IsNullOrEmpty(result))
						result = $"[{paramName}]";
				}
			}
			else
			{
				if (!CellManager.IsDesignMode)
					text = TemplateLocalizer.ResolveInline(_manager.Properties.Localization, text, _manager.Properties.Culture);

				var reg = new Regex(@"\[(?=[^\[]*\]).*?\]");
				var matches = reg.Matches(text);
				foreach (Match match in matches)
				{
					if (text.Contains($"[{match.ToString()}]"))
					{
						text = text.Replace($"[{match.ToString()}]", match.ToString());
						continue;
					}

					var paramName = match.ToString().Replace("]", string.Empty).Replace("[", string.Empty);
					var value = GetFormattedParamValueByName(paramName);
					if (CellManager.IsDesignMode && !string.IsNullOrEmpty(value))
						text = text.Replace(match.ToString(), value);

					if (!CellManager.IsDesignMode)
						text = text.Replace(match.ToString(), value ?? string.Empty);
				}

				if (Text.Contains("{{"))
				{
					var parameters = new List<FormatParameter>();
					var compiler = new FormatCompiler();
					compiler.PlaceholderFound += (sender, args) =>
					{
						parameters.Add(new FormatParameter
						{
							Name = args.Key,
							Format = args.Formatting == string.Empty ? null : args.Formatting
						});
					};
					try
					{
						var formatGenerator = compiler.Compile(text);
						formatGenerator.KeyNotFound += (sender, args) =>
						{
							args.Handled = true;
							var parameter = parameters.FirstOrDefault(p => p.Name == args.Key);
							if (parameter == null)
								return;

							args.Substitute = GetFormattedParamValueByName(parameter.Name, parameter.Format);
						};
						text = formatGenerator.Render(null);
					}
					catch (FormatException)
					{
					}
				}

				result = text;
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
			return result;
		}

		public override string ToString()
		{
			var text = Text ?? string.Empty;
			text = text.Length > 10 ? text.Substring(0, 10) + "..." : text;
			return $"Текст ({text})";
		}
	}
}
