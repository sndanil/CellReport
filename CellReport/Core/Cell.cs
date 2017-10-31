using System;
using System.Collections.Generic;
using CellReport.Core.Contents;
using System.Xml.Linq;

namespace CellReport.Core
{
	[Serializable]
	public struct CellSpan
	{
		public int RowSpan;
		public int ColSpan;
	}

	[Serializable]
	public class Cell : NotifyPropertyChanged
	{
		public static readonly CellStyle DefaultStyle = new CellStyle("Transparent");
		[NonSerialized]
		private CellManager _manager;
		private CellSpan _span;
		private CellLocation _location;
		[NonSerialized]
		private CellStyle _style;
		private string _styleID;
		private BaseContent _content;

		internal void SetManager(CellManager manager)
		{
			_manager = manager;
			if (Content != null)
				Content.SetManager(manager);
			var collection = Content as CollectionContent;
			if (collection != null)
			{
				foreach (var content in collection.GetContents())
					content.SetManager(manager);
			}
		}

		internal Cell(CellManager manager, int row, int column)
		{
			_manager = manager;
			_location.Row = row;
			_location.Column = column;
		}

		public CellSpan Span
		{
			get { return _span; }
			set { _manager.SetSpansForCell(_location, value); }
		}

		internal CellSpan SpanInternal
		{
			get { return _span; }
			set
			{
				_span = value;
				OnPropertyChanged("SpanInternal");
			}
		}

		public CellLocation Location
		{
			get { return _location; }
		}

		public Size Size
		{
			get
			{
				float width = 0;
				float height = 0;

				for (int i = 0; i <= _span.ColSpan; i++)
					width += _manager.GetColumnWidth(_location.Column + i);

				for (int i = 0; i <= _span.RowSpan; i++)
					height += _manager.GetRowHeight(_location.Row + i);

				return new Size { Height = height, Width = width };
			}
		}

		public string StyleID
		{
			get { return _styleID; }
			set
			{
				if (_styleID == value)
					return;
				_styleID = value;
				if (_style != null && _style.ID != _styleID)
					_style = null;
				OnPropertyChanged("StyleID");
			}
		}

		public CellStyle Style
		{
			get { return _style ?? (_style = _manager.GetStyleByID<CellStyle>(StyleID) ?? DefaultStyle); }
			set
			{
				if (ReferenceEquals(DefaultStyle, value) || value == null)
					StyleID = string.Empty;
				else
					StyleID = value.ID;
			}
		}

		public BaseContent Content
		{
			get { return _content; }
			set
			{
				if (_content != value)
				{
					SetField(ref _content, value);
					OnPropertyChanged("Content");
				}
			}
		}

		public IEnumerable<BaseContent> GetContents()
		{
			if (Content != null)
			{
				var collection = Content as CollectionContent;
				if (collection != null)
				{
					foreach (var item in collection.GetContents())
						yield return item;
				}
				else
					yield return Content;
			}
		}

		public override string ToString()
		{
			return string.Format("Cell [{0},{1}]", Location.Row, Location.Column);
		}

		public XElement Serialize(int index = -1)
		{
			var cellElement = new XElement("Cell", (index >= 0) ? new XAttribute("Index", index) : null);
			if (Span.RowSpan != 0)
				cellElement.Add(new XAttribute("RowSpan", Span.RowSpan));
			if (Span.ColSpan != 0)
				cellElement.Add(new XAttribute("ColSpan", Span.ColSpan));
			if (Content != null)
				cellElement.Add(Content.Serialize());
			if (!string.IsNullOrEmpty(this.Style.ID))
				cellElement.Add(new XAttribute("Style", Style.ID));
			return cellElement;
		}

		public static Cell Parse(CellManager manager, XElement element)
		{
			var cell = new Cell(manager, 0, 0);
			if (element.Attribute("Style") != null)
				cell.Style = manager.GetStyleByID<CellStyle>(element.Attribute("Style").Value);
			cell.SpanInternal = Parse(manager, cell, element);
			return cell;
		}

		internal static Cell Parse(CellManager manager, XElement element, int row, ref int col)
		{
			if (element.Attribute("Index") != null)
				col = Convert.ToInt32(element.Attribute("Index").Value);
			var cell = manager.CreateCell(row, col);
			if (element.Attribute("Style") != null)
				cell.Style = manager.GetStyleByID<CellStyle>(element.Attribute("Style").Value);
			cell.Span = Parse(manager, cell, element);
			col += (cell.Span.ColSpan + 1);
			return cell;
		}

		private static CellSpan Parse(CellManager manager, Cell cell, XElement element)
		{
			var span = new CellSpan();
			if (element.Attribute("ColSpan") != null)
				span.ColSpan = Convert.ToInt32(element.Attribute("ColSpan").Value);
			if (element.Attribute("RowSpan") != null)
				span.RowSpan = Convert.ToInt32(element.Attribute("RowSpan").Value);
			return span;
		}
	}
}
