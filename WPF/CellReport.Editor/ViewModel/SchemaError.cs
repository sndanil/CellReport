using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellReport.Editor.ViewModel
{
	public class SchemaError
	{
		public int LineNumber { get; set; }

		public int LinePosition { get; set; }

		public string Message { get; set; }

		public string Error { get; set; }
	}
}
