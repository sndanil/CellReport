using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;

namespace CellReport.Core
{
	[Serializable]
	public class Margins : NotifyPropertyChanged
	{
		private float _left;
		private float _right;
		private float _top;
		private float _bottom;

		public Margins() : this(0)
		{
		}

		public Margins(float all)
				: this(all, all, all, all)
		{
		}

		public Margins(float left, float right, float top, float bottom)
		{
			_left = left;
			_right = right;
			_top = top;
			_bottom = bottom;
		}

		public float Bottom
		{
			get { return _bottom; }
			set
			{
				if (_bottom != value)
				{
					_bottom = value;
					OnPropertyChanged("Bottom");
				}
			}
		}

		public float Left
		{
			get { return _left; }
			set
			{
				if (_left != value)
				{
					_left = value;
					OnPropertyChanged("Left");
				}
			}
		}

		public float Right
		{
			get { return _right; }
			set
			{
				if (_right != value)
				{
					_right = value;
					OnPropertyChanged("Right");
				}
			}
		}

		public float Top
		{
			get { return _top; }
			set
			{
				if (_top != value)
				{
					_top = value;
					OnPropertyChanged("Top");
				}
			}
		}
	}

	[Serializable]
	public class Pager
	{
		public float Height { get; set; }

		public int FromPage { get; set; }

		public int ToPage { get; set; }

		public BaseContent Left { get; set; }

		public BaseContent Center { get; set; }

		public BaseContent Right { get; set; }

		public Pager Clone(CellManager manager)
		{
			return new Pager()
			{
				Height = Height,
				FromPage = FromPage,
				ToPage = ToPage,
				Left = Left == null ? null : Left.Clone(manager),
				Center = Center == null ? null : Center.Clone(manager),
				Right = Right == null ? null : Right.Clone(manager)
			};
		}

		public static void ParsePagers(CellManager manager, XElement element, IList<Pager> result)
		{
			foreach (var pagerElement in element.Elements("Pager"))
			{
				var pager = new Pager();
				if (pagerElement.Attribute("Height") != null)
					pager.Height = UnitsConverter.ParseUnit(pagerElement.Attribute("Height").Value, GraphicsUnit.Millimeter);
				if (pagerElement.Attribute("FromPage") != null)
					pager.FromPage = int.Parse(pagerElement.Attribute("FromPage").Value, CultureInfo.InvariantCulture);
				if (pagerElement.Attribute("ToPage") != null)
					pager.ToPage = int.Parse(pagerElement.Attribute("ToPage").Value, CultureInfo.InvariantCulture);
				if (pagerElement.Element("Left") != null)
					pager.Left = BaseContent.Parse(manager, pagerElement.Element("Left"));
				if (pagerElement.Element("Center") != null)
					pager.Center = BaseContent.Parse(manager, pagerElement.Element("Center"));
				if (pagerElement.Element("Right") != null)
					pager.Right = BaseContent.Parse(manager, pagerElement.Element("Right"));
				result.Add(pager);
			}
		}

		private XElement GetContents(BaseContent content)
		{
			return content == null ? null : content.Serialize();
		}

		internal XElement Serialize()
		{
			return new XElement("Pager",
				new XAttribute("Height", Height.ToString(CultureInfo.InvariantCulture)),
				new XAttribute("FromPage", FromPage),
				new XAttribute("ToPage", ToPage),
				new XElement("Left", GetContents(Left)),
				new XElement("Center", GetContents(Center)),
				new XElement("Right", GetContents(Right)));
		}

		/// <summary>
		/// Функция для добавления выражений, связанных с колонтитулом, в отчет.
		/// </summary>
		public void AddExpressions(CellManager manager, List<BaseExpression> expressions)
		{
			var ids = new List<string>();
			if (Left != null)
				ids.AddRange(GetContentExpressions(Left));
			if (Center != null)
				ids.AddRange(GetContentExpressions(Center));
			if (Right != null)
				ids.AddRange(GetContentExpressions(Right));
			ids = ids.Distinct().ToList();

			foreach (var id in ids)
			{
				var newExpr = expressions.FirstOrDefault(x => x.ID == id);
				manager.ReplaceExpression(newExpr);
				if (newExpr.ID != id)
				{
					if (Left != null)
						Left.ReplaceExpressionId(id, newExpr.ID);
					if (Center != null)
						Center.ReplaceExpressionId(id, newExpr.ID);
					if (Right != null)
						Right.ReplaceExpressionId(id, newExpr.ID);
				}
			}
		}

		private List<string> GetContentExpressions(BaseContent content)
		{
			var result = new List<string>();
			if (content is CollectionContent)
				result.AddRange(((CollectionContent)content).GetContents().SelectMany(x => GetContentExpressions(x)));
			else
				result.AddRange(content.GetParams().Where(x => !string.IsNullOrEmpty(x.ExpressionID)).Select(x => x.ExpressionID));
			return result.Distinct().ToList();
		}
	}

	[Serializable]
	public class PrintSettings : NotifyPropertyChanged
	{
		private bool _landscape = false;
		private short _copies = 1;
		private int _autoFitWidthPages = 1;
		private float _scale = 1;
		private Margins _margins = null;
		private IList<Pager> _headers = null;
		private IList<Pager> _footers = null;

		public PrintSettings()
		{
			Margins = new Margins(10);
			Headers = new List<Pager>();
			Footers = new List<Pager>();
			HeaderFixedRows = new List<Range>();
			FooterFixedRows = new List<Range>();
		}

		public short Copies
		{
			get { return _copies; }
			set
			{
				if (_copies != value)
				{
					_copies = value;
					OnPropertyChanged(nameof(Copies));
				}
			}
		}

		public bool Landscape
		{
			get { return _landscape; }
			set
			{
				if (_landscape != value)
				{
					_landscape = value;
					OnPropertyChanged("Landscape");
				}
			}
		}

		public int AutoFitWidthPages
		{
			get { return _autoFitWidthPages; }
			set
			{
				if (_autoFitWidthPages != value)
				{
					_autoFitWidthPages = value;
					OnPropertyChanged("AutoFitWidthPages");
				}
			}
		}

		public float Scale
		{
			get { return _scale; }
			set
			{
				if (_scale != value)
				{
					_scale = value;
					OnPropertyChanged("Scale");
				}
			}
		}

		public Margins Margins
		{
			get { return _margins; }
			set { SetField(ref _margins, value); }
		}

		public IList<Pager> Headers
		{
			get { return _headers; }
			set
			{
				if (_headers != value)
				{
					_headers = value;
					OnPropertyChanged("Headers");
				}
			}
		}

		public IList<Pager> Footers
		{
			get { return _footers; }
			set
			{
				if (_footers != value)
				{
					_footers = value;
					OnPropertyChanged("Footers");
				}
			}
		}

		public IList<Range> HeaderFixedRows
		{
			get;
			set;
		}

		public IList<Range> FooterFixedRows
		{
			get;
			set;
		}

		internal PrintSettings Clone(CellManager manager)
		{
			return new PrintSettings()
			{
				AutoFitWidthPages = AutoFitWidthPages,
				Copies = Copies,
				Landscape = Landscape,
				Margins = Margins,
				Scale = Scale,
				Footers = Footers.Select(f => f.Clone(manager)).ToList(),
				Headers = Headers.Select(f => f.Clone(manager)).ToList()
			};
		}

		internal static PrintSettings Parse(CellManager manager, XElement element)
		{
			var result = new PrintSettings();

			if (element != null)
			{
				if (element.Attribute("Landscape") != null)
					result.Landscape = bool.Parse(element.Attribute("Landscape").Value);
				if (element.Attribute("Copies") != null)
					result.Copies = short.Parse(element.Attribute("Copies").Value, CultureInfo.InvariantCulture);
				if (element.Attribute("Scale") != null)
					result.Scale = float.Parse(element.Attribute("Scale").Value, CultureInfo.InvariantCulture);
				if (element.Attribute("AutoFitWidthPages") != null)
					result.AutoFitWidthPages = int.Parse(element.Attribute("AutoFitWidthPages").Value, CultureInfo.InvariantCulture);
				if (element.Element("Margins") != null)
				{
					var marginsElement = element.Element("Margins");
					float left = 0; float top = 0; float right = 0; float bottom = 0;
					if (marginsElement.Attribute("Left") != null)
						left = UnitsConverter.ParseUnit(marginsElement.Attribute("Left").Value, GraphicsUnit.Millimeter);
					if (marginsElement.Attribute("Top") != null)
						top = UnitsConverter.ParseUnit(marginsElement.Attribute("Top").Value, GraphicsUnit.Millimeter);
					if (marginsElement.Attribute("Right") != null)
						right = UnitsConverter.ParseUnit(marginsElement.Attribute("Right").Value, GraphicsUnit.Millimeter);
					if (marginsElement.Attribute("Bottom") != null)
						bottom = UnitsConverter.ParseUnit(marginsElement.Attribute("Bottom").Value, GraphicsUnit.Millimeter);
					if (marginsElement.Attribute("All") != null)
						left = top = right = bottom = UnitsConverter.ParseUnit(marginsElement.Attribute("All").Value, GraphicsUnit.Millimeter);
					result.Margins = new Margins(left, right, top, bottom);
				}
				if (element.Element("Headers") != null)
					Pager.ParsePagers(manager, element.Element("Headers"), result.Headers);
				if (element.Element("Footers") != null)
					Pager.ParsePagers(manager, element.Element("Footers"), result.Footers);
				if (element.Element("HeaderFixedRows") != null)
				{
					foreach (var rangeElement in element.Element("HeaderFixedRows").Elements("Range"))
						result.HeaderFixedRows.Add(Range.Parse(rangeElement));
				}
				if (element.Element("FooterFixedRows") != null)
				{
					foreach (var rangeElement in element.Element("FooterFixedRows").Elements("Range"))
						result.FooterFixedRows.Add(Range.Parse(rangeElement));
				}
			}

			return result;
		}

		internal XElement Serialize()
		{
			return new XElement("PrintSettings",
					new XAttribute("Landscape", Landscape),
					Copies > 1 ? null : new XAttribute("Copies", Copies),
					new XAttribute("AutoFitWidthPages", AutoFitWidthPages),
					new XAttribute("Scale", Scale.ToString(CultureInfo.InvariantCulture)),
					new XElement("Margins",
								 new XAttribute("Top", Margins.Top.ToString(CultureInfo.InvariantCulture)),
								 new XAttribute("Left", Margins.Left.ToString(CultureInfo.InvariantCulture)),
								 new XAttribute("Right", Margins.Right.ToString(CultureInfo.InvariantCulture)),
								 new XAttribute("Bottom", Margins.Bottom.ToString(CultureInfo.InvariantCulture))),
					Headers.Count == 0 ? null : new XElement("Headers", Headers.Select(h => h.Serialize())),
					HeaderFixedRows.Count == 0 ? null : new XElement("HeaderFixedRows", HeaderFixedRows.Select(r => r.Serialize("Range"))),
					Footers.Count == 0 ? null : new XElement("Footers", Footers.Select(f => f.Serialize())),
					FooterFixedRows.Count == 0 ? null : new XElement("FooterFixedRows", FooterFixedRows.Select(r => r.Serialize("Range")))
					);
		}
	}
}
