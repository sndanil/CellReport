using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Contents
{
    [Serializable]
    public class LineContentStyle : BaseStyle
    {
        public LineContentStyle(string color, float width)
        {
            if (color == null)
                throw new ArgumentNullException("color");

            Color = color;
            Width = width;
        }

        public string Color
        {
            get;
            private set;
        }

        public float Width
        {
            get;
            private set;
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
                return true;

            var other = obj as LineContentStyle;
            if (other != null)
                return this.Color.Equals(other.Color) && this.Width.Equals(other.Width);

            return false;
        }

        public override int GetHashCode()
        {
            return this.Color.GetHashCode() + this.Width.GetHashCode();
        }

        internal new static LineContentStyle Parse(XElement element)
        {
            float width = UnitsConverter.ParseUnit(element.Attribute("Width").Value, GraphicsUnit.Millimeter);
            var color = element.Attribute("Color").Value;
            return new LineContentStyle(color, width);
        }

        protected override XElement OnSerialize(string styleName)
        {
            var result = base.OnSerialize(styleName ?? "LineContentStyle");
            result.Add(
                new XAttribute("Color", Color),
                new XAttribute("Width", Width.ToString(CultureInfo.InvariantCulture)));

            return result;
        }
    }

    [Serializable]
    public class LineContent : BaseContent
    {
        public static readonly LineContentStyle DefaultStyle = new LineContentStyle("Black", 0.25f);

        public LineContent(CellManager manager)
            : base(manager)
        {
        }

        public float StartX
        {
            get;
            set;
        }

        public float StartY
        {
            get;
            set;
        }

        public float EndX
        {
            get;
            set;
        }

        public float EndY
        {
            get;
            set;
        }

		public override BaseStyle GetStyle()
		{
			return GetStyle<LineContentStyle>() ?? DefaultStyle;
		}

        internal new static LineContent Parse(CellManager manager, XElement element)
        {
            var lineContent = new LineContent(manager);
            lineContent.StartX = UnitsConverter.ParseUnit(element.Attribute("StartX").Value, GraphicsUnit.Millimeter);
            lineContent.StartY = UnitsConverter.ParseUnit(element.Attribute("StartY").Value, GraphicsUnit.Millimeter);
            lineContent.EndX = UnitsConverter.ParseUnit(element.Attribute("EndX").Value, GraphicsUnit.Millimeter);
            lineContent.EndY = UnitsConverter.ParseUnit(element.Attribute("EndY").Value, GraphicsUnit.Millimeter);

            return lineContent;
        }
    }
}
