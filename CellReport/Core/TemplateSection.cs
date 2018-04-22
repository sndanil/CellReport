using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CellReport.Core
{
	[Serializable]
	public class TemplateSection : NotifyPropertyChanged
	{
		private CellLocation _leftTop;
		public CellLocation LeftTop
		{
			get => _leftTop;
			set
			{
				if (!_leftTop.Equals(value))
				{
					_leftTop = value;
					OnPropertyChanged("LeftTop");
				}
			}
		}

		private CellLocation _rightBottom;
		public CellLocation RightBottom
		{
			get => _rightBottom;
			set
			{
				if (!_rightBottom.Equals(value))
				{
					_rightBottom = value;
					OnPropertyChanged("RightBottom");
				}
			}
		}

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged("Name");
				}
			}
		}

		private bool _isHeaderFixed;
		public bool IsHeaderFixed
		{
			get => _isHeaderFixed;
			set
			{
				if (_isHeaderFixed != value)
				{
					_isHeaderFixed = value;
					OnPropertyChanged("IsHeaderFixed");
				}
			}
		}

		private bool _isFooterFixed;
		public bool IsFooterFixed
		{
			get => _isFooterFixed;
			set
			{
				if (_isFooterFixed != value)
				{
					_isFooterFixed = value;
					OnPropertyChanged("IsFooterFixed");
				}
			}
		}

		public int Top
		{
			get => Math.Min(LeftTop.Row, RightBottom.Row);
			set
			{
				_leftTop = new CellLocation { Row = value, Column = LeftTop.Column };
				OnPropertyChanged("Top");
			}
		}

		public int TopDesign
		{
			get => LeftTop.Row;
			set
			{
				_leftTop = new CellLocation { Row = value, Column = LeftTop.Column };
				OnPropertyChanged("Top");
			}
		}

		public int Left
		{
			get => Math.Min(LeftTop.Column, RightBottom.Column);
			set
			{
				_leftTop = new CellLocation { Row = LeftTop.Row, Column = value };
				OnPropertyChanged("Left");
			}
		}

		public int LeftDesign
		{
			get => LeftTop.Column;
			set
			{
				_leftTop = new CellLocation { Row = LeftTop.Row, Column = value };
				OnPropertyChanged("Left");
			}
		}

		public int Right
		{
			get => Math.Max(LeftTop.Column, RightBottom.Column);
			set
			{
				_rightBottom = new CellLocation { Row = RightBottom.Row, Column = value };
				OnPropertyChanged("Right");
			}
		}

		public int RightDesign
		{
			get => RightBottom.Column;
			set
			{
				_rightBottom = new CellLocation { Row = RightBottom.Row, Column = value };
				OnPropertyChanged("Right");
			}
		}

		public int Bottom
		{
			get => Math.Max(LeftTop.Row, RightBottom.Row);
			set
			{
				_rightBottom = new CellLocation { Row = value, Column = RightBottom.Column };
				OnPropertyChanged("Bottom");
			}
		}

		public int BottomDesign
		{
			get => RightBottom.Row;
			set
			{
				_rightBottom = new CellLocation { Row = value, Column = RightBottom.Column };
				OnPropertyChanged("Bottom");
			}
		}

		public int Height => Math.Abs(RightBottom.Row - LeftTop.Row) + 1;

		public int Width => Math.Abs(RightBottom.Column - LeftTop.Column) + 1;

		public IEnumerable<Cell> GetCells(CellManager manager)
		{
			Dictionary<Cell, bool> alreadyReturned = new Dictionary<Cell, bool>();
			for (int row = Top; row <= Bottom; row++)
				for (int col = Left; col <= Right; col++)
				{
					var cell = manager.GetCell(row, col);
					if (cell != null && !alreadyReturned.ContainsKey(cell))
					{
						alreadyReturned.Add(cell, true);
						yield return cell;
					}
				}
		}

		public IEnumerable<float> GetRowsHeights(CellManager manager)
		{
			for (int row = Top; row <= Bottom; row++)
				yield return manager.GetRowHeight(row);
		}

		public IEnumerable<int> GetAutoFitRows(CellManager manager)
		{
			for (int row = Top; row <= Bottom; row++)
				if (manager.IsAutoFitRow(row))
					yield return row;
		}

		public IEnumerable<float> GetColumnWidths(CellManager manager)
		{
			for (int col = Left; col <= Right; col++)
				yield return manager.GetColumnWidth(col);
		}

		public CellLocation TranslateToSectionOffset(Cell cell)
		{
			return new CellLocation { Row = cell.Location.Row - Top, Column = cell.Location.Column - Left };
		}

		internal XElement Serialize()
		{
			var result = new XElement("Section",
							new XAttribute("ID", Name), LeftTop.Serialize("LeftTop"), RightBottom.Serialize("RightBottom"));
			if (IsHeaderFixed)
				result.Add(new XAttribute("IsHeaderFixed", IsHeaderFixed));
			if (IsFooterFixed)
				result.Add(new XAttribute("IsFooterFixed", IsFooterFixed));
			return result;
		}

		internal static TemplateSection Parse(XElement element)
		{
			var section = new TemplateSection();

			section.Name = element.Attribute("ID").Value;
			section.LeftTop = CellLocation.Parse(element.Element("LeftTop"));
			section.RightBottom = CellLocation.Parse(element.Element("RightBottom"));

			if (element.Attribute("IsHeaderFixed") != null)
				section.IsHeaderFixed = bool.Parse(element.Attribute("IsHeaderFixed").Value);
			if (element.Attribute("IsFooterFixed") != null)
				section.IsFooterFixed = bool.Parse(element.Attribute("IsFooterFixed").Value);

			return section;
		}

		public override string ToString()
		{
			return $"{Name} [{Left}, {Top}] - [{Right}, {Bottom}]";
		}
	}
}
