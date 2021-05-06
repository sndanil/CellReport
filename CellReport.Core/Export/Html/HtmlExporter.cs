using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Presenters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CellReport.Export.Html
{
	public class HtmlExporter : BaseExporter
	{
		private Styles _styles = null;

		public override string Extension
		{
			get { return "html"; }
		}

		public override string Name
		{
			get { return "HTML"; }
		}

		internal BaseRenderer Renderer { get; private set; }

		public HtmlExporter(CellManager manager, BaseRenderer renderer)
            : base(manager)
        {
			Renderer = renderer ?? (BaseExporter.RendererFactory != null ? BaseExporter.RendererFactory.Create() : null);
        }

		public override void ExportTo(Stream stream)
		{
			_styles = new Styles(Manager);

			var style = new XElement("style", new XAttribute("type", "text/css"));
			var doc = new XDocument(new XDocumentType("html", null, null, null), new XElement("html",
				new XElement("head",
					new XElement("title", Manager.Properties.DocumentName),
					style),
				new XElement("body",
					GetTable())));
			style.Add(_styles.ToString());

			var xws = new XmlWriterSettings();
			xws.OmitXmlDeclaration = true;
			xws.Indent = true;

			using (var xw = XmlWriter.Create(stream, xws))
				doc.Save(xw);
		}

		private XElement GetTable()
		{
			var table = new XElement("table");
			var colgroup = new XElement("colgroup");
			for (var col = 0; col < Manager.ColumnsCount; col++)
			{
				var width = HtmlHelper.GetDimension(Manager.GetColumnWidth(col));
				colgroup.Add(new XElement("col",
					new XAttribute("style", string.Format("width:{0};", width))));
			}
			table.Add(colgroup);

			var exportedCells = new Dictionary<Cell, bool>();
			for (var row = 0; row < Manager.RowsCount; row++)
			{
				var height = HtmlHelper.GetDimension(Manager.GetRowHeight(row));
				var tr = new XElement("tr",
					new XAttribute("style", string.Format("height:{0};", height)));
				table.Add(tr);
				for (var col = 0; col < Manager.ColumnsCount; col++)
				{
					var cell = Manager.GetCell(row, col);
					if (cell == null || !exportedCells.ContainsKey(cell))
					{
						var td = new XElement("td");
						tr.Add(td);
						if (cell == null)
							continue;

						exportedCells.Add(cell, true);
						if (cell.Span.ColSpan > 0)
							td.Add(new XAttribute("colspan", cell.Span.ColSpan + 1));
						if (cell.Span.RowSpan > 0)
							td.Add(new XAttribute("rowspan", cell.Span.RowSpan + 1));

						var id = _styles.Add(cell.Style);
						td.Add(new XAttribute("class", id));
						WriteContent(td, cell.Content, cell.Size);
					}
				}
			}
			return table;
		}

		private void WriteContent(XElement td, BaseContent content, Size size)
		{
			if (content == null)
				return;

			if (content is CollectionContent)
			{
				foreach (var item in ((CollectionContent)content).GetContents())
					WriteContent(td, item, size);
			}
			else if (content is TextBasedContent)
			{
				// add vertical alignment to td
				var textStyle = (TextContentStyle)content.GetActualStyle();
				if (td.Attribute("style") == null && (textStyle.VAlignment == VerticalAlignment.Top || textStyle.VAlignment == VerticalAlignment.Bottom))
					td.Add(new XAttribute("style", string.Format("vertical-align:{0};", textStyle.VAlignment.ToString().ToLower())));

				var div = new XElement("div");
				var text = ((TextBasedContent)content).GetTextForRender();
				if (textStyle.Direction == Direction.TopToDown)
				{
					for (int i = 0; i < text.Length; i++)
					{
						div.Add(text[i]);
						if (i < text.Length - 1)
							div.Add(new XElement("br"));
					}
				}
				else
					div.Add(text);
				td.Add(div);

				var style = content.GetActualStyle();
				var id = _styles.Add(style);
				div.Add(new XAttribute("class", id));

				if (content is DecimalContent)
				{
					var decContent = (DecimalContent)content;
					var value = decContent.GetValue();
					var decStyle = (DecimalContentStyle)decContent.GetActualStyle();
					if ((value < 0) && !string.IsNullOrEmpty(decStyle.Format) && (decStyle.Color != decStyle.NegativeColor))
					{
						var color = string.Format("color:{0};", HtmlHelper.GetColor(decStyle.NegativeColor));
						div.Add(new XAttribute("style", color));
					}
				}
			}
			else if (content is ImageContent)
			{
				var imgContent = (ImageContent)content;
				var uri = imgContent.ActualUri;
				if (string.IsNullOrEmpty(uri))
					return;

				var id = _styles.Add(uri);
				var clsAttr = td.Attribute("class");
				if (clsAttr == null)
				{
					clsAttr = new XAttribute("class", id);
					td.Add(clsAttr);
				}
				else
					clsAttr.Value += " " + id;
			}
			else if (content is BarCodeContent)
			{
				if (Renderer == null)
					return;

				var barCode = (BarCodeContent)content;
				var width = (int)UnitsConverter.ConvertValue(size.Width,
					HtmlHelper.DefaultUnit, HtmlHelper.HtmlUnit, HtmlHelper.HtmlDpi);
				var height = (int)UnitsConverter.ConvertValue(size.Height,
					HtmlHelper.DefaultUnit, HtmlHelper.HtmlUnit, HtmlHelper.HtmlDpi);
				Action<object> renderAction = (context) =>
				{
					var rect = new Rectangle(0, 0, width, height);
					Renderer.RenderBarCode(context, rect, barCode,
						"Black", "Arial", 10f, FontStyle.Regular, barCode.Orientation);
				};
				var imgData = Renderer.CreateImage(width, height, renderAction);
				if ((imgData != null) && (imgData.Length > 0))
				{
					var img = new XElement("img", new XAttribute("src", HtmlHelper.GetEmbeddedImage(imgData)));
					td.Add(img);
				}
			}
		}
	}
}
