using CellReport.Export;
using CellReport.Presenters.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellReport.Presenters.WinForms
{
    public class RendererFactory : IRendererFactory
    {
        public CellReport.Presenters.BaseRenderer Create()
        {
            return new GdiPlusRenderer();
        }
    }
}
