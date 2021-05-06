using System;
using System.Xml;
using System.IO;
using System.IO.Packaging;
using System.Xml.Linq;
using System.Collections.Generic;

namespace CellReport.Export.Excel
{
	internal class Workbook
	{
        private List<Worksheet> _worksheets = new List<Worksheet>();

        public void Save(Package package)
        {
            var sheetsElement = new XElement(ExcelExporter.schemaMain + "sheets");
            var root = new XElement(ExcelExporter.schemaMain + "workbook", 
                new XAttribute(XNamespace.Xmlns + "r", ExcelExporter.schemaRelationships),
                new XElement(ExcelExporter.schemaMain + "bookViews",
                    new XElement(ExcelExporter.schemaMain + "workbookView")),
                sheetsElement
            );

            var uri = new Uri("/xl/workbook.xml", UriKind.Relative);
            var part = package.CreatePart(uri, 
                @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml",
                CompressionOption.Maximum);
            package.CreateRelationship(uri, TargetMode.Internal,
                @"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument");

            int id = 1;
            foreach (var worksheet in _worksheets)
            {
                worksheet.Save(package, id, part, sheetsElement);
                id++;
            }

            root.Save(new StreamWriter(part.GetStream()), SaveOptions.DisableFormatting);
        }

        public IList<Worksheet> Worksheets { get { return _worksheets; } }
	} 
}
