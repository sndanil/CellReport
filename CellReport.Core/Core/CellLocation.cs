using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CellReport.Core
{
    [Serializable]
    public struct CellLocation
    {
        public readonly static CellLocation None = new CellLocation() { Row = -1, Column = -1 };

        public int Row;
        public int Column;

        internal XElement Serialize(string name)
        {
            return new XElement(name, new XAttribute("Row", this.Row), new XAttribute("Column", this.Column));
        }

        internal static CellLocation Parse(XElement element)
        {
            var row = 0;
            var col = 0;

            if (element.Attribute("Row") != null)
                row = int.Parse(element.Attribute("Row").Value, System.Globalization.CultureInfo.InvariantCulture);
            if (element.Attribute("Column") != null)
                col = int.Parse(element.Attribute("Column").Value, System.Globalization.CultureInfo.InvariantCulture);

            return new CellLocation() { Row = row, Column = col };
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var cellLoc = (CellLocation)obj;
            return this.Row == cellLoc.Row && this.Column == cellLoc.Column;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
