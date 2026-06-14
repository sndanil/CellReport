using CellReport.Core;
using CellReport.Presenters;
using CellReport.Presenters.Skia;
using System;
using System.IO;

namespace CellReport.Export.Pdf
{
	public class PdfExporter : BaseExporter
	{
		private readonly BaseRenderer _renderer;
		private readonly Func<Rectangle, string, string, float, FontStyle, Size> _getTextSize;

		public PdfExporter(
			CellManager manager,
			BaseRenderer renderer,
			Func<Rectangle, string, string, float, FontStyle, Size> getTextSize = null)
				: base(manager)
		{
			if (renderer == null)
				throw new ArgumentNullException(nameof(renderer));

			_renderer = renderer;
			_getTextSize = getTextSize;
		}

		public PdfExporter(ReportDocument document, BaseRenderer renderer, Func<Rectangle, string, string, float, FontStyle, Size> getTextSize = null)
				: this(document.Manager, renderer, getTextSize)
		{
		}

		public override string Name
		{
			get { return "PDF"; }
		}

		public override string Extension
		{
			get { return "pdf"; }
		}

		public static PdfExporter CreateVectorExporter(CellManager manager)
		{
			return new PdfExporter(manager, new SkiaReportRenderer(), SkiaReportRenderer.GetTextSize);
		}

		public override void ExportTo(Stream stream)
		{
			var builder = new PrintPageImageBuilder(Manager, _renderer, _getTextSize);
			builder.ExportVectorPdf(stream);
		}
	}
}
