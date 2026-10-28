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

		public Properties()
		{
		}

		private string _documentName = string.Empty;
		public string DocumentName
		{
			get
			{
				if (TemplateLocalizer.IsFullKey(_documentName) && _localization != null)
					return TemplateLocalizer.GetString(_localization, _documentName, _culture);
				return _documentName;
			}
			set
			{
				if (_documentName != value)
				{
					_documentName = value;
					OnPropertyChanged("DocumentName");
				}
			}
		}

		/// <summary>
		/// Имя документа как оно записано в шаблоне. В отличие от <see cref="DocumentName"/>
		/// не разрешает ключи локализации вида <c>#ключ</c>, поэтому подходит для редактирования.
		/// </summary>
		public string RawDocumentName
		{
			get { return _documentName; }
		}

		private string _culture;
		public string Culture
		{
			get { return _culture; }
			set
			{
				if (_culture != value)
				{
					_culture = value;
					OnPropertyChanged("Culture");
				}
			}
		}

		private TemplateStrings _localization = new TemplateStrings();
		public TemplateStrings Localization
		{
			get { return _localization; }
			set { _localization = value ?? new TemplateStrings(); }
		}

		private Column _defaultColumn = new Column { Width = DefaultColumnWidthConst, MarkerForShow = null, MarkerForHide = null };
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
				result.Localization = TemplateStrings.Parse(element.Element("Localization"));
				if (element.Attribute("DefaultColumnsCount") != null)
					result.DefaultColumnsCount = Convert.ToInt32(element.Attribute("DefaultColumnsCount").Value, CultureInfo.InvariantCulture);
				if (element.Attribute("DefaultRowsCount") != null)
					result.DefaultRowsCount = Convert.ToInt32(element.Attribute("DefaultRowsCount").Value, CultureInfo.InvariantCulture);

				var column = result.DefaultColumn;
				if (element.Attribute("DefaultColumnWidth") != null)
					column.Width = UnitsConverter.ParseUnit(element.Attribute("DefaultColumnWidth").Value, GraphicsUnit.Millimeter);
				result.DefaultColumn = column;

				if (element.Attribute("DefaultRowHeight") != null)
					result.DefaultRowHeight = UnitsConverter.ParseUnit(element.Attribute("DefaultRowHeight").Value, GraphicsUnit.Millimeter);
			}

			return result;
		}

		internal XElement Serialize()
		{
			var result = new XElement("Properties", new XElement("DocumentName", RawDocumentName));
			var localization = Localization.Serialize();
			if (localization != null)
				result.Add(localization);
			if (DefaultRowsCount != DefaultRowsCountConst)
				result.Add(new XAttribute("DefaultRowsCount", DefaultRowsCount.ToString(CultureInfo.InvariantCulture)));
			if (DefaultColumnsCount != DefaultColumnsCountConst)
				result.Add(new XAttribute("DefaultColumnsCount", DefaultColumnsCount.ToString(CultureInfo.InvariantCulture)));
			if (DefaultRowHeight != DefaultRowHeightConst)
				result.Add(new XAttribute("DefaultRowHeight", DefaultRowHeight.ToString(CultureInfo.InvariantCulture)));
			if (DefaultColumn.Width != DefaultColumnWidthConst)
				result.Add(new XAttribute("DefaultColumnWidth", DefaultColumn.Width.ToString(CultureInfo.InvariantCulture)));
			return result;
		}

		internal Properties Clone()
		{
			var result = Parse(Serialize());
			result.Culture = Culture;
			return result;
		}
	}
}
