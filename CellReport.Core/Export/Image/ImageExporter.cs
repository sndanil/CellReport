using System;
using System.IO;

using CellReport.Core;
using CellReport.Presenters;

namespace CellReport.Export.Image
{
	public class ImageExporter : BaseExporter
	{
		private ImagePresenter _presenter = null;

		public ImageExporter(CellManager manager, BaseRenderer renderer)
            : base(manager)
        {
            if (renderer == null && BaseExporter.RendererFactory != null)
                renderer = BaseExporter.RendererFactory.Create();

			_presenter = new ImagePresenter(manager, renderer);
        }

        public ImageExporter(ReportDocument document)
            : this(document.Manager, null)
        {
        }

        public ImageExporter(CellManager manager)
            : this(manager, null)
        {
        }
        
        public override string Name
		{
			get { return "Image"; }
		}

		public override string Extension
		{
			get { return "png"; }
		}

		public override void ExportTo(Stream stream)
		{
			var data = _presenter.Render();
			if ((data != null) && (data.Length > 0))
				stream.Write(data, 0, data.Length);
		}
	}
}
