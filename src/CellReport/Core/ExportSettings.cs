using System;
using System.Xml.Linq;

namespace CellReport.Core
{
    [Serializable]
	public class ExportSettings : NotifyPropertyChanged
    {
		private bool _runExcelAfterExport = true;

		public bool RunExcelAfterExport
		{
			get { return _runExcelAfterExport; }
			set
			{
				if (_runExcelAfterExport != value)
				{
					_runExcelAfterExport = value;
					OnPropertyChanged("RunExcelAfterExport");
				}
			}
		}

		public ExportSettings()
        {
            RunExcelAfterExport = true;
        }

        internal static ExportSettings Parse(XElement element)
        {
            var result = new ExportSettings();

            if (element != null)
            {
                if (element.Attribute("RunExcelAfterExport") != null)
                    result.RunExcelAfterExport = bool.Parse(element.Attribute("RunExcelAfterExport").Value);
            }

            return result;
        }

        internal XElement Serialize()
        {
            return new XElement("ExportSettings",
                new XAttribute("RunExcelAfterExport", RunExcelAfterExport)
                );
        }

        internal ExportSettings Clone()
        {
            return Parse(Serialize());
        }
    }
}
