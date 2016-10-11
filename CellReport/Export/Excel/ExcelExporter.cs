using System;
using System.IO;
using System.IO.Packaging;
using System.Xml.Linq;

using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Export.Excel;
using CellReport.Presenters;

namespace CellReport.Export.Excel
{
    public class ExcelExporter: BaseExporter
    {
        internal static readonly XNamespace schemaMain = @"http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        internal static readonly XNamespace schemaRelationships = @"http://schemas.openxmlformats.org/officeDocument/2006/relationships";
		internal static readonly XNamespace schemaDrawing = @"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
		internal static readonly XNamespace schemaDrawingMain = @"http://schemas.openxmlformats.org/drawingml/2006/main";
		internal static readonly XNamespace schemaDrawingRel = @"http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing";
		internal static readonly XNamespace schemaImageRel = @"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";

		public const GraphicsUnit ImageUnit = GraphicsUnit.Pixel;
		public const float ImageDpi = 96f;
        internal BaseRenderer Renderer { get; private set; }

        public ExcelExporter(CellManager manager, BaseRenderer renderer)
            : base(manager)
        {
            Renderer = renderer ?? (BaseExporter.RendererFactory != null ? BaseExporter.RendererFactory.Create() : null);
        }

        public ExcelExporter(CellManager manager)
            : this(manager, null)
        {
        }

        public ExcelExporter(ReportDocument document)
            : this(document.Manager, null)
        {
        }

        public override string Name
        {
            get { return "Excel"; }
        }

        public override string Extension
        {
            get { return "xlsx"; }
        }

        public override void ExportTo(System.IO.Stream stream)
        {
            var workbook = new Workbook();
            var sheet = new Worksheet(Manager, this);
            workbook.Worksheets.Add(sheet);

            using (Package package = ZipPackage.Open(stream, FileMode.Create))
            {
                workbook.Save(package);
            }
        }
    }
}
