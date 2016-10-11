using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core
{
    [Serializable]
    public enum LineStyle
    {
        None = -1,
        Solid = 0,
        Dash = 1,
        Dot = 2,
        DashDot = 3,
        DashDotDot = 4
    }

    [Serializable]
    public class CellStyle : BaseStyle
    {
        public CellStyle(string color)
            : this(color, new Borders(Border.Empty), new Paddings(0))
        {
        }

        public CellStyle(string color, Borders borders, Paddings paddings)
        {
            if (color == null)
                throw new ArgumentNullException("color");
            if (borders == null)
                throw new ArgumentNullException("borders");
            if (paddings == null)
                throw new ArgumentNullException("paddings");

            Color = color;
            Borders = borders;
            Paddings = paddings;
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
                return true;

            var other = obj as CellStyle;
            if (other != null)
            {
                return this.Color == other.Color
                    && this.Borders.Equals(other.Borders);
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            return (Color != null ? Color.GetHashCode() : 0)
                + Borders.GetHashCode();
        }

        public string Color
        {
            get;
            private set;
        }

        public Borders Borders
        {
            get;
            private set;
        }

        public Paddings Paddings
        {
            get;
            private set;
        }

		public bool HasColor
		{
			get { return !string.IsNullOrEmpty(Color) && Color != "Transparent"; }
		}

        internal new static CellStyle Parse(XElement element)
        {
            var paddings = Paddings.Parse(element.Element("Paddings"));
            var bordersElement = element.Element("Borders");
            var borders = new Borders(Border.Empty);
            if (bordersElement != null)
            {
                var top = Border.Empty; var left = Border.Empty;
                var right = Border.Empty; var bottom = Border.Empty;
                var all = Border.Empty;
                foreach (var borderElement in bordersElement.Elements("Border"))
                {
                    if (borderElement.Attribute("Position") == null)
                        continue;
                    var style = LineStyle.Solid;
                    if (borderElement.Attribute("Style") != null)
                        style = (LineStyle)Enum.Parse(typeof(LineStyle), borderElement.Attribute("Style").Value);
                    var border = new Border(UnitsConverter.ParseUnit(borderElement.Attribute("Weight").Value, GraphicsUnit.Millimeter),
                                borderElement.Attribute("Color").Value, style);
                    switch (borderElement.Attribute("Position").Value)
                    {
                        case "All": all = border; break;
                        case "Top": top = border; break;
                        case "Left": left = border; break;
                        case "Right": right = border; break;
                        case "Bottom": bottom = border; break;
                    }
                }
                if (all.Equals(Border.Empty))
                    borders = new Borders(top, left, right, bottom);
                else
                    borders = new Borders(all);
            }
            var color = "Transparent";
            if (element.Attribute("Color") != null)
                color = element.Attribute("Color").Value;
            return new CellStyle(color, borders, paddings);
        }

        protected override XElement OnSerialize(string styleName)
        {
            var result = base.OnSerialize(styleName ?? "CellStyle");
            result.Add(
				HasColor ? new XAttribute("Color", Color) : null,
                Paddings.Serialize(), 
                Borders.Serialize()
                );
            return result;
        }
    }

    [Serializable]
    public class Paddings
    {
        public Paddings(float all)
            : this(all, all, all, all)
        {
        }

        public Paddings(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public float Left
        {
            get;
            private set;
        }

        public float Top
        {
            get;
            private set;
        }

        public float Right
        {
            get;
            private set;
        }

        public float Bottom
        {
            get;
            private set;
        }

        public bool IsEmpty()
        {
            return Top == 0 && Left == 0 && Right == 0 && Bottom == 0;
        }

		public bool IsIdentical()
		{
			return Left == Top && Top == Right && Right == Bottom;
		}

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            var paddings = obj as Paddings;
            if (paddings != null)
                return this.Left.Equals(paddings.Left)
                    && this.Top.Equals(paddings.Top)
                    && this.Right.Equals(paddings.Right)
                    && this.Bottom.Equals(paddings.Bottom);

            return false;
        }

        public override int GetHashCode()
        {
            return Left.GetHashCode() + Top.GetHashCode() + Right.GetHashCode() + Bottom.GetHashCode();
        }

        internal static Paddings Parse(XElement element)
        {
            var paddings = new Paddings(0);
            if (element != null)
            {
                var top = 0f; var left = 0f;
                var right = 0f; var bottom = 0f;
                var all = 0f;
                foreach (var paddingElement in element.Elements("Padding"))
                {
                    if (paddingElement.Attribute("Position") == null)
                        continue;
                    switch (paddingElement.Attribute("Position").Value)
                    {
                        case "All": all = UnitsConverter.ParseUnit(paddingElement.Value, GraphicsUnit.Millimeter); break;
                        case "Top": top = UnitsConverter.ParseUnit(paddingElement.Value, GraphicsUnit.Millimeter); break;
                        case "Left": left = UnitsConverter.ParseUnit(paddingElement.Value, GraphicsUnit.Millimeter); break;
                        case "Right": right = UnitsConverter.ParseUnit(paddingElement.Value, GraphicsUnit.Millimeter); break;
                        case "Bottom": bottom = UnitsConverter.ParseUnit(paddingElement.Value, GraphicsUnit.Millimeter); break;
                    }
                }
                if (all == 0)
                    paddings = new Paddings(top, left, right, bottom);
                else
                    paddings = new Paddings(all);
            }

            return paddings;
        }

        internal XElement Serialize()
        {
            if (IsEmpty())
                return null;

            if (Left == Top && Top == Right && Right == Bottom)
                return new XElement("Paddings", new XElement("Padding",
                    new XAttribute("Position", "All"), Left.ToString(CultureInfo.InvariantCulture)));
            else
            {
                var paddings = new XElement("Paddings");
                if (Top != 0)
                    paddings.Add(new XElement("Padding", new XAttribute("Position", "Top"), Top.ToString(CultureInfo.InvariantCulture)));
                if (Left != 0)
                    paddings.Add(new XElement("Padding", new XAttribute("Position", "Left"), Left.ToString(CultureInfo.InvariantCulture)));
                if (Right != 0)
                    paddings.Add(new XElement("Padding", new XAttribute("Position", "Right"), Right.ToString(CultureInfo.InvariantCulture)));
                if (Bottom != 0)
                    paddings.Add(new XElement("Padding", new XAttribute("Position", "Bottom"), Bottom.ToString(CultureInfo.InvariantCulture)));

                return paddings;
            }
        }
    }

    [Serializable]
    public class Borders
    {
        [Serializable]
        public enum BordersChain
        {
            None = 0,
            LeftTopRightBottom = 15,
            LeftTopRight = 14,
            TopRightBottom = 7,
            RightBottomLeft = 11,
            BottomLeftTop = 13,
            LeftTop = 12,
            TopRight = 6,
            RightBottom = 3,
            LeftBotom = 9,
            Left = 8,
            Top = 4,
            Right = 2,
            Bottom = 1
        }

        private BordersChain[] _chains = null;

        public Borders(Border all): this(all, all, all, all)
        {
        }

        public Borders(Border top, Border left, Border right, Border bottom)
        {
            if (top == null)
                throw new ArgumentNullException("top");
            if (left == null)
                throw new ArgumentNullException("left");
            if (right == null)
                throw new ArgumentNullException("right");
            if (bottom == null)
                throw new ArgumentNullException("bottom");

            Top = top;
            Left = left;
            Right = right;
            Bottom = bottom;
        }

        public Border Top
        {
            get;
            private set;
        }

        public Border Left
        {
            get;
            private set;
        }

        public Border Right
        {
            get;
            private set;
        }

        public Border Bottom
        {
            get;
            private set;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            var borders = obj as Borders;
            if (borders != null)
                return this.Left.Equals(borders.Left)
                    && this.Top.Equals(borders.Top)
                    && this.Right.Equals(borders.Right)
                    && this.Bottom.Equals(borders.Bottom);

            return false;
        }

        public override int GetHashCode()
        {
            return Left.GetHashCode() + Top.GetHashCode() + Right.GetHashCode() + Bottom.GetHashCode();
        }

        public bool IsEmpty()
        {
            return Left.Equals(Border.Empty) && Top.Equals(Border.Empty) 
                && Right.Equals(Border.Empty) && Bottom.Equals(Border.Empty);
        }

        public bool IsRectangle()
        {
            return Left.Equals(Top) && Top.Equals(Right) && Right.Equals(Bottom);
        }

        public Border GetFirstInChain(BordersChain chain)
        {
            if ((chain & BordersChain.Left) == BordersChain.Left)
                return Left;
            else if ((chain & BordersChain.Top) == BordersChain.Top)
                return Top;
            else if ((chain & BordersChain.Right) == BordersChain.Right)
                return Right;
            else if ((chain & BordersChain.Bottom) == BordersChain.Bottom)
                return Bottom;

            return Border.Empty;
        }

        public BordersChain[] GetChains()
        {
            if (_chains == null)
            {
                var chains = new List<BordersChain>(4);

                if (!IsEmpty())
                {
                    if (IsRectangle())
                        chains.Add(BordersChain.LeftTopRightBottom);
                    else
                    {
                        if (Left.Equals(Top) && Top.Equals(Right) && !Left.IsEmpty())
                        {
                            chains.Add(BordersChain.LeftTopRight);
                            chains.Add(BordersChain.Bottom);
                        }
                        else if (Top.Equals(Right) && Right.Equals(Bottom) && !Top.IsEmpty())
                        {
                            chains.Add(BordersChain.TopRightBottom);
                            chains.Add(BordersChain.Left);
                        }
                        else if (Right.Equals(Bottom) && Bottom.Equals(Left) && !Right.IsEmpty())
                        {
                            chains.Add(BordersChain.RightBottomLeft);
                            chains.Add(BordersChain.Top);
                        }
                        else if (Bottom.Equals(Left) && Left.Equals(Top) && !Bottom.IsEmpty())
                        {
                            chains.Add(BordersChain.BottomLeftTop);
                            chains.Add(BordersChain.Right);
                        }
                        else
                        {
                            if (Left.Equals(Top) && !Left.IsEmpty())
                                chains.Add(BordersChain.LeftTop);
                            if (Top.Equals(Right) && !Top.IsEmpty())
                                chains.Add(BordersChain.TopRight);
                            if (Right.Equals(Bottom) && !Right.IsEmpty())
                                chains.Add(BordersChain.RightBottom);
                            if (Left.Equals(Bottom) && !Left.IsEmpty())
                                chains.Add(BordersChain.LeftBotom);
                        }
                    }

                    if (!Left.IsEmpty() && !chains.Any(c => (c & BordersChain.Left) == BordersChain.Left))
                        chains.Add(BordersChain.Left);
                    if (!Top.IsEmpty() && !chains.Any(c => (c & BordersChain.Top) == BordersChain.Top))
                        chains.Add(BordersChain.Top);
                    if (!Right.IsEmpty() && !chains.Any(c => (c & BordersChain.Right) == BordersChain.Right))
                        chains.Add(BordersChain.Right);
                    if (!Bottom.IsEmpty() && !chains.Any(c => (c & BordersChain.Bottom) == BordersChain.Bottom))
                        chains.Add(BordersChain.Bottom);
                }

                _chains = chains.ToArray();
            }

            return _chains;
        }

        internal static Borders Parse(XElement element)
        {
            var borders = new Borders(Border.Empty);
            if (element != null)
            {
                var top = Border.Empty; var left = Border.Empty;
                var right = Border.Empty; var bottom = Border.Empty;
                var all = Border.Empty;
                foreach (var borderElement in element.Elements("Border"))
                {
                    if (borderElement.Attribute("Position") == null)
                        continue;
                    var style = LineStyle.Solid;
                    if (borderElement.Attribute("Style") != null)
                        style = (LineStyle)Enum.Parse(typeof(LineStyle), borderElement.Attribute("Style").Value);
                    var border = new Border(UnitsConverter.ParseUnit(borderElement.Attribute("Weight").Value, GraphicsUnit.Millimeter),
                                borderElement.Attribute("Color").Value, style);
                    switch (borderElement.Attribute("Position").Value)
                    {
                        case "All": all = border; break;
                        case "Top": top = border; break;
                        case "Left": left = border; break;
                        case "Right": right = border; break;
                        case "Bottom": bottom = border; break;
                    }
                }
                if (all.Equals(Border.Empty))
                    borders = new Borders(top, left, right, bottom);
                else
                    borders = new Borders(all);
            }

            return borders;
        }

        internal XElement Serialize()
        {
            if (IsEmpty())
                return null;

            if (!IsEmpty() && IsRectangle())
                return new XElement("Borders", new XElement("Border",
                    new XAttribute("Position", "All"), 
                    new XAttribute("Style", Top.Style),
                    new XAttribute("Color", Top.Color),
                    new XAttribute("Weight", Top.Weight.ToString(CultureInfo.InvariantCulture))
                    ));
            else
            {
                var borders = new XElement("Borders");
                if (!Top.IsEmpty())
                    borders.Add(new XElement("Border", 
                        new XAttribute("Position", "Top"),
                        new XAttribute("Style", Top.Style),
                        new XAttribute("Color", Top.Color),
                        new XAttribute("Weight", Top.Weight.ToString(CultureInfo.InvariantCulture))
                        ));
                if (!Left.IsEmpty())
                    borders.Add(new XElement("Border",
                        new XAttribute("Position", "Left"),
                        new XAttribute("Style", Left.Style),
                        new XAttribute("Color", Left.Color),
                        new XAttribute("Weight", Left.Weight.ToString(CultureInfo.InvariantCulture))
                        ));
                if (!Right.IsEmpty())
                    borders.Add(new XElement("Border",
                        new XAttribute("Position", "Right"),
                        new XAttribute("Style", Right.Style),
                        new XAttribute("Color", Right.Color),
                        new XAttribute("Weight", Right.Weight.ToString(CultureInfo.InvariantCulture))
                        ));
                if (!Bottom.IsEmpty())
                    borders.Add(new XElement("Border",
                        new XAttribute("Position", "Bottom"),
                        new XAttribute("Style", Bottom.Style),
                        new XAttribute("Color", Bottom.Color),
                        new XAttribute("Weight", Bottom.Weight.ToString(CultureInfo.InvariantCulture))
                        ));

                return borders;
            }
        }
    }

    [Serializable]
    public class Border
    {
        public static readonly Border Empty = new Border(0, "Transparent", LineStyle.None);

        public Border(float weight, string color, LineStyle style)
        {
            Style = style;
            Color = color;
            Weight = weight;
        }

        public float Weight
        {
            get;
            private set;
        }

        public string Color
        {
            get;
            private set;
        }

        public LineStyle Style
        {
            get;
            private set;
        }

        public bool IsEmpty()
        {
            return object.ReferenceEquals(this, Border.Empty);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            var border = obj as Border;
            if (border != null)
                return this.Weight == border.Weight
                    && this.Color == border.Color
                    && this.Style == border.Style;

            return false;
        }

        public override int GetHashCode()
        {
            return Color.GetHashCode() + (int)Style + Convert.ToInt32(Weight);
        }
    }
}
