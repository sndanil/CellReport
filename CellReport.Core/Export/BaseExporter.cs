using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CellReport.Core;
using CellReport.Presenters;

namespace CellReport.Export
{
    public interface IRendererFactory
    {
        BaseRenderer Create();
    }

    public abstract class BaseExporter
    {
        public static IRendererFactory RendererFactory { get; set; }
 
        public BaseExporter(CellManager manager)
        {
            Manager = manager;
        }

        public CellManager Manager
        {
            get;
            private set;
        }

        public abstract string Extension
        {
            get;
        }

        public abstract string Name
        {
            get;
        }

        public abstract void ExportTo(Stream stream);
    }
}
