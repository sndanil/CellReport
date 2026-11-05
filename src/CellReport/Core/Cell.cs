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
		// ReSharper disable once InconsistentNaming
		private string _styleID;
		private BaseContent _content;
		private bool _editable;
		private string _editParam;
		private string _editPlaceholder;
		private string _editHint;

		internal void SetManager(CellManager manager)
		{
			_manager = manager;
			Content?.SetManager(manager);
			if (Content is CollectionContent collection)
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
			get => _span;
			set => _manager.SetSpansForCell(_location, value);
		}

		internal CellSpan SpanInternal
		{
			get => _span;
			set
			{
				_span = value;
				OnPropertyChanged("SpanInternal");
			}
		}

		public CellLocation Location => _location;

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

		// ReSharper disable once InconsistentNaming
		public string StyleID
		{
			get => _styleID;
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
			get
			{
				// в конструкторе стиль читается из менеджера каждый раз: так правка
				// стиля сразу видна во всех ячейках, которые на него ссылаются
				if (_manager.IsDesignMode)
					return _manager.GetStyleByID<CellStyle>(StyleID) ?? DefaultStyle;

				return _style ?? (_style = _manager.GetStyleByID<CellStyle>(StyleID) ?? DefaultStyle);
			}
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
			get => _content;
			set
			{
				if (_content != value)
				{
					SetField(ref _content, value);
					OnPropertyChanged("Content");
				}
			}
		}

		/// <summary>
		/// Разрешено ли заполнять ячейку вручную в просмотрщике.
		/// </summary>
		public bool Editable
		{
			get => _editable;
			set
			{
				if (_editable == value)
					return;
				_editable = value;
				OnPropertyChanged("Editable");
			}
		}

		/// <summary>
		/// Имя параметра, в который пишется ручной ввод. Пусто — определить автоматически.
		/// </summary>
		public string EditParam
		{
			get => _editParam;
			set
			{
				if (_editParam == value)
					return;
				_editParam = value;
				OnPropertyChanged("EditParam");
			}
		}

		/// <summary>
		/// Текст-плейсхолдер для пустой заполняемой ячейки.
		/// </summary>
		public string EditPlaceholder
		{
			get => _editPlaceholder;
			set
			{
				if (_editPlaceholder == value)
					return;
				_editPlaceholder = value;
				OnPropertyChanged("EditPlaceholder");
			}
		}

		/// <summary>
		/// Подсказка (тултип) для заполняемой ячейки.
		/// </summary>
		public string EditHint
		{
			get => _editHint;
			set
			{
				if (_editHint == value)
					return;
				_editHint = value;
				OnPropertyChanged("EditHint");
			}
		}

		public IEnumerable<BaseContent> GetContents()
		{
			if (Content != null)
			{
				if (Content is CollectionContent collection)
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
			return $"Cell [{Location.Row},{Location.Column}]";
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
			if (!string.IsNullOrEmpty(Style.ID))
				cellElement.Add(new XAttribute("Style", Style.ID));
			if (Editable)
				cellElement.Add(new XAttribute("Editable", true));
			if (!string.IsNullOrEmpty(EditParam))
				cellElement.Add(new XAttribute("EditParam", EditParam));
			if (!string.IsNullOrEmpty(EditPlaceholder))
				cellElement.Add(new XAttribute("EditPlaceholder", EditPlaceholder));
			if (!string.IsNullOrEmpty(EditHint))
				cellElement.Add(new XAttribute("EditHint", EditHint));
			return cellElement;
		}

		public static Cell Parse(CellManager manager, XElement element)
		{
			var cell = new Cell(manager, 0, 0);
			if (element.Attribute("Style") != null)
				cell.Style = manager.GetStyleByID<CellStyle>(element.Attribute("Style")?.Value);
			cell.SpanInternal = Parse(element);
			ParseEditAttributes(cell, element);
			return cell;
		}

		internal static Cell Parse(CellManager manager, XElement element, int row, ref int col)
		{
			if (element.Attribute("Index") != null)
				col = Convert.ToInt32(element.Attribute("Index")?.Value);
			var cell = manager.CreateCell(row, col);
			if (element.Attribute("Style") != null)
				cell.Style = manager.GetStyleByID<CellStyle>(element.Attribute("Style")?.Value);
			cell.Span = Parse(element);
			ParseEditAttributes(cell, element);
			col += (cell.Span.ColSpan + 1);
			return cell;
		}

		private static void ParseEditAttributes(Cell cell, XElement element)
		{
			if (element.Attribute("Editable") != null)
			{
				bool editable;
				if (bool.TryParse(element.Attribute("Editable").Value, out editable))
					cell.Editable = editable;
			}

			if (element.Attribute("EditParam") != null)
				cell.EditParam = element.Attribute("EditParam").Value;
			if (element.Attribute("EditPlaceholder") != null)
				cell.EditPlaceholder = element.Attribute("EditPlaceholder").Value;
			if (element.Attribute("EditHint") != null)
				cell.EditHint = element.Attribute("EditHint").Value;
		}

		private static CellSpan Parse(XElement element)
		{
			var span = new CellSpan();
			if (element.Attribute("ColSpan") != null)
				span.ColSpan = Convert.ToInt32(element.Attribute("ColSpan")?.Value);
			if (element.Attribute("RowSpan") != null)
				span.RowSpan = Convert.ToInt32(element.Attribute("RowSpan")?.Value);
			return span;
		}
	}
}
