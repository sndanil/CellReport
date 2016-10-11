using CellReport.Core;
using CellReport.Core.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellReport.Export.Html
{
	internal class CssClass : Dictionary<string, string>
	{
		public string Name { get; private set; }

		public CssClass(string name)
		{
			Name = name;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			AppendTo(sb);
			return sb.ToString();
		}

		public void AppendTo(StringBuilder sb)
		{
			sb.AppendLine();
			sb.AppendLine(Name);
			sb.AppendLine("{");
			foreach (var pair in this)
			{
				sb.Append("\t");
				sb.Append(pair.Key);
				sb.Append(": ");
				sb.Append(pair.Value);
				sb.Append(";");
				sb.AppendLine();
			}
			sb.AppendLine("}");
		}

		private void AddCellStyle(CellStyle style)
		{
			if (style.HasColor)
				Add("background-color", HtmlHelper.GetColor(style.Color));
			if (!style.Borders.IsEmpty())
			{
				if (style.Borders.IsRectangle())
					Add("border", HtmlHelper.GetBorder(style.Borders.Top));
				else
				{
					Add("border-left", HtmlHelper.GetBorder(style.Borders.Left));
					Add("border-right", HtmlHelper.GetBorder(style.Borders.Right));
					Add("border-top", HtmlHelper.GetBorder(style.Borders.Top));
					Add("border-bottom", HtmlHelper.GetBorder(style.Borders.Bottom));
				}
			}
			if (!style.Paddings.IsEmpty())
			{
				if (style.Paddings.IsIdentical())
					Add("padding", HtmlHelper.GetDimension(style.Paddings.Top));
				else
				{
					Add("padding-left", HtmlHelper.GetDimension(style.Paddings.Left));
					Add("padding-right", HtmlHelper.GetDimension(style.Paddings.Right));
					Add("padding-top", HtmlHelper.GetDimension(style.Paddings.Top));
					Add("padding-bottom", HtmlHelper.GetDimension(style.Paddings.Bottom));
				}
			}
		}

		private void AddTextStyle(TextContentStyle style)
		{
			Add("text-align", style.HAlignment.ToString().ToLower());

			if (!string.IsNullOrEmpty(style.Color))
				Add("color", HtmlHelper.GetColor(style.Color));

			Add("font-size", HtmlHelper.GetDimension(style.Font.Size, GraphicsUnit.Point));
			Add("font-family", style.Font.Family);

			var textDecors = new List<string>();
			if ((style.Font.Style & FontStyle.Underline) == FontStyle.Underline)
				textDecors.Add("underline");
			if ((style.Font.Style & FontStyle.Strikeout) == FontStyle.Strikeout)
				textDecors.Add("line-through");
			if (textDecors.Any())
				Add("text-decoration", string.Join(" ", textDecors.ToArray()));

			if ((style.Font.Style & FontStyle.Bold) == FontStyle.Bold)
				Add("font-weight", "bold");
			if ((style.Font.Style & FontStyle.Italic) == FontStyle.Italic)
				Add("font-style", "italic");

			if (style.Angle != 0)
			{
				var hAlign = style.HAlignment;
				if (hAlign == HorizontalAlignment.Justify)
					hAlign = HorizontalAlignment.Left;
				var vAlign = style.VAlignment;
				if (vAlign == VerticalAlignment.Middle)
					vAlign = VerticalAlignment.Center;
				var transform = string.Format("rotate({0}deg)", style.Angle);
				var origin = string.Format("{0} {1}", hAlign.ToString().ToLower(), vAlign.ToString().ToLower());
				Add("transform", transform);
				Add("transform-origin", origin);
				Add("-ms-transform", transform);
				Add("-ms-transform-origin", origin);
				Add("-webkit-transform", transform);
				Add("-webkit-transform-origin", origin);
			}
		}

		public static CssClass GetFromStyle(string id, BaseStyle style)
		{
			var cls = new CssClass("." + id);
			if (style is CellStyle)
				cls.AddCellStyle((CellStyle)style);
			else if (style is TextContentStyle)
				cls.AddTextStyle((TextContentStyle)style);
			return cls;
		}

		public static CssClass GetFromUri(string id, string uri, CellManager manager)
		{
			var cls = new CssClass("." + id);
			cls.Add("background-size", "100% 100%");
			if (uri.ToUpper().StartsWith("RESOURCE") || uri.ToUpper().StartsWith("FILE"))
			{
				var data = manager.GetDataByUri(uri);
				cls.Add("background-image", string.Format("url({0})", HtmlHelper.GetEmbeddedImage(data)));
			}
			else if (uri.ToUpper().StartsWith("HTTP"))
				cls.Add("background-image", string.Format("url('{0}')", uri));
			return cls;
		}

		public static CssClass GetBodyClass()
		{
			var cls = new CssClass("html, body, td");
			cls.Add("padding", "0");
			cls.Add("margin", "0");
			return cls;
		}

		public static CssClass GetTableClass()
		{
			var cls = new CssClass("table");
			cls.Add("border-collapse", "collapse");
			cls.Add("border-spacing", "0");
			return cls;
		}

		public static CssClass GetImgClass()
		{
			var cls = new CssClass("img");
			cls.Add("display", "block");
			return cls;
		}
	}
}
