using System;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core
{
	[Serializable]
	public class Properties : NotifyPropertyChanged
	{
		private const int DefaultColumnsCountConst = 5;
		private const int DefaultRowsCountConst = 10;
		private const float DefaultRowHeightConst = 20;
		private const float DefaultColumnWidthConst = 50;
		private const string DefaultColumnMarkerConst = null;

		public Properties()
		{
		}

		private string _documentName = string.Empty;
		public string DocumentName
		{
			get { return _documentName; }
			set
			{
				if (_documentName != value)
				{
					_documentName = value;
					OnPropertyChanged("DocumentName");
				}
			}
		}

		private Column _defaultColumn = new Column { Width = DefaultColumnWidthConst, Marker = DefaultColumnMarkerConst };
		public Column DefaultColumn
		{
			get { return _defaultColumn; }
			set { _defaultColumn = value; }
		}

		public float DefaultColumnWidth
		{
			get { return _defaultColumn.Width; }
			set
			{
				if (_defaultColumn.Width != value)
				{
					_defaultColumn.Width = value;
					OnPropertyChanged("DefaultColumnWidth");
				}
			}
		}

		private float _defaultRowHeight = DefaultRowHeightConst;
		public float DefaultRowHeight
		{
			get { return _defaultRowHeight; }
			set
			{
				if (_defaultRowHeight != value)
				{
					_defaultRowHeight = value;
					OnPropertyChanged("DefaultRowHeight");
				}
			}
		}

		private int _defaultColumnsCount = DefaultColumnsCountConst;
		public int DefaultColumnsCount
		{
			get { return _defaultColumnsCount; }
			set
			{
				if (_defaultColumnsCount != value)
				{
					_defaultColumnsCount = value;
					OnPropertyChanged("DefaultColumnsCount");
				}
			}
		}

		private int _defaultRowsCount = DefaultRowsCountConst;
		public int DefaultRowsCount
		{
			get { return _defaultRowsCount; }
			set
			{
				if (_defaultRowsCount != value)
				{
					_defaultRowsCount = value;
					OnPropertyChanged("DefaultRowsCount");
				}
			}
		}

		internal static Properties Parse(XElement element)
		{
			var result = new Properties();
			if (element != null)
			{
				if (element.Element("DocumentName") != null)
					result.DocumentName = element.Element("DocumentName").Value;
				if (element.Attribute("DefaultColumnsCount") != null)
					result.DefaultColumnsCount = Convert.ToInt32(element.Attribute("DefaultColumnsCount").Value, CultureInfo.InvariantCulture);
				if (element.Attribute("DefaultRowsCount") != null)
					result.DefaultRowsCount = Convert.ToInt32(element.Attribute("DefaultRowsCount").Value, CultureInfo.InvariantCulture);

				var column = result.DefaultColumn;
				if (element.Attribute("DefaultColumnWidth") != null)
					column.Width = UnitsConverter.ParseUnit(element.Attribute("DefaultColumnWidth").Value, GraphicsUnit.Millimeter);
				if (element.Attribute("DefaultColumnMarker") != null)
					column.Marker = element.Attribute("DefaultColumnMarker").Value;
				result.DefaultColumn = column;

				if (element.Attribute("DefaultRowHeight") != null)
					result.DefaultRowHeight = UnitsConverter.ParseUnit(element.Attribute("DefaultRowHeight").Value, GraphicsUnit.Millimeter);
			}

			return result;
		}

		internal XElement Serialize()
		{
			var result = new XElement("Properties", new XElement("DocumentName", DocumentName));
			if (DefaultRowsCount != DefaultRowsCountConst)
				result.Add(new XAttribute("DefaultRowsCount", DefaultRowsCount.ToString(CultureInfo.InvariantCulture)));
			if (DefaultColumnsCount != DefaultColumnsCountConst)
				result.Add(new XAttribute("DefaultColumnsCount", DefaultColumnsCount.ToString(CultureInfo.InvariantCulture)));
			if (DefaultRowHeight != DefaultRowHeightConst)
				result.Add(new XAttribute("DefaultRowHeight", DefaultRowHeight.ToString(CultureInfo.InvariantCulture)));
			if (DefaultColumn.Width != DefaultColumnWidthConst)
				result.Add(new XAttribute("DefaultColumnWidth", DefaultColumn.Width.ToString(CultureInfo.InvariantCulture)));
			if (DefaultColumn.Marker != DefaultColumnMarkerConst)
				result.Add(new XAttribute("DefaultColumnMarker", DefaultColumn.Marker.ToString()));
			return result;
		}
	}
}
