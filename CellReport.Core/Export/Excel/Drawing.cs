using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Net.Mime;
using System.Xml.Linq;

using CellReport.Core;

namespace CellReport.Export.Excel
{
	internal class Drawing
	{
		private class ExcelImage
		{
			public CellLocation Location { get; set; }

			public CellSpan Span { get; set; }

			public byte[] ImageData { get; set; }

			internal void Save(Package package, int drawingId, int id, PackagePart drawingPart, XElement drawingElement)
			{
				var uri = new Uri(string.Format("/xl/media/image{0}_{1}.jpg", drawingId, id), UriKind.Relative);
				var rel = drawingPart.CreateRelationship(uri, TargetMode.Internal, ExcelExporter.schemaImageRel.ToString());
				var part = package.CreatePart(uri, MediaTypeNames.Image.Jpeg, CompressionOption.NotCompressed);
				part.GetStream().Write(this.ImageData, 0, this.ImageData.Length);

				var from = FillLocation(this.Location.Column, this.Location.Row,
					new XElement(ExcelExporter.schemaDrawing + "from"));
				var to = FillLocation(this.Location.Column + this.Span.ColSpan + 1, this.Location.Row + this.Span.RowSpan + 1,
					new XElement(ExcelExporter.schemaDrawing + "to"));
				var pic = new XElement(ExcelExporter.schemaDrawing + "pic",
					new XElement(ExcelExporter.schemaDrawing + "nvPicPr",
						new XElement(ExcelExporter.schemaDrawing + "cNvPr",
							new XAttribute("id", id),
							new XAttribute("name", string.Format("Image {0}", id))),
						new XElement(ExcelExporter.schemaDrawing + "cNvPicPr")),
					new XElement(ExcelExporter.schemaDrawing + "blipFill",
						new XElement(ExcelExporter.schemaDrawingMain + "blip",
							new XAttribute(XNamespace.Xmlns + "r", ExcelExporter.schemaRelationships),
							new XAttribute(ExcelExporter.schemaRelationships + "embed", rel.Id)),
						new XElement(ExcelExporter.schemaDrawingMain + "stretch",
							new XElement(ExcelExporter.schemaDrawingMain + "fillRect"))),
					new XElement(ExcelExporter.schemaDrawing + "spPr",
						new XElement(ExcelExporter.schemaDrawingMain + "prstGeom",
							new XAttribute("prst", "rect"))));
				var clientData = new XElement(ExcelExporter.schemaDrawing + "clientData");
				drawingElement.Add(new XElement(ExcelExporter.schemaDrawing + "twoCellAnchor",
					from, to, pic, clientData));
			}

			private XElement FillLocation(int col, int row, XElement element)
			{
				element.Add(new XElement(ExcelExporter.schemaDrawing + "col", col),
					new XElement(ExcelExporter.schemaDrawing + "colOff", 0),
					new XElement(ExcelExporter.schemaDrawing + "row", row),
					new XElement(ExcelExporter.schemaDrawing + "rowOff", 0));
				return element;
			}
		}

		private List<ExcelImage> _images = new List<ExcelImage>();

		public void AddImage(CellLocation location, CellSpan span, byte[] imageData)
		{
			_images.Add(new ExcelImage()
			{
				Location = location,
				Span = span,
				ImageData = imageData
			});
		}

		internal void Save(Package package, int id, PackagePart sheetPart, XElement sheetElement)
		{
			if (_images.Count == 0)
				return;

			var uri = new Uri(string.Format("/xl/drawings/drawing{0}.xml", id), UriKind.Relative);
			var part = package.CreatePart(uri,
				@"application/vnd.openxmlformats-officedocument.drawing+xml",
				CompressionOption.Maximum);
			var rel = sheetPart.CreateRelationship(uri, TargetMode.Internal, ExcelExporter.schemaDrawingRel.ToString());
			sheetElement.Add(new XElement(ExcelExporter.schemaMain + "drawing",
				new XAttribute(ExcelExporter.schemaRelationships + "id", rel.Id)));

			var root = new XElement(ExcelExporter.schemaDrawing + "wsDr",
				new XAttribute(XNamespace.Xmlns + "xdr", ExcelExporter.schemaDrawing),
				new XAttribute(XNamespace.Xmlns + "a", ExcelExporter.schemaDrawingMain));
			for (int i = 0; i < _images.Count; i++)
				_images[i].Save(package, id, i + 1, part, root);

			root.Save(new StreamWriter(part.GetStream()));
		}
	}
}
