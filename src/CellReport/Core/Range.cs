using System;
using System.Xml.Linq;

namespace CellReport.Core
{
    [Serializable]
    public class Range
    {
        private int _from = 0;
        private int _to = 0;

        public int From
        {
            get { return _from; }
            set { _from = value; }
        }

        public int To
        {
            get { return _to; }
            set { _to = value; }
        }

        public Range(int from, int to)
        {
            _from = from;
            _to = to;
        }

        public bool IsIn(int value)
        {
            return _from <= value && value <= _to;
        }

        public bool IsIn(Range value)
        {
            return _from <= value.From && value.To <= _to;
        }

        internal void Serialize(XElement element)
        {
            element.Add(new XAttribute("From", this._from), new XAttribute("To", this._to));
        }

        internal XElement Serialize(string name)
        {
            return new XElement(name, new XAttribute("From", this._from), new XAttribute("To", this._to));
        }

        internal static Range Parse(XElement element)
        {
            var from = 0;
            var to = 0;

            if (element.Attribute("From") != null)
                from = int.Parse(element.Attribute("From").Value, System.Globalization.CultureInfo.InvariantCulture);
            if (element.Attribute("To") != null)
                to = int.Parse(element.Attribute("To").Value, System.Globalization.CultureInfo.InvariantCulture);

            return new Range(from, to);
        }

    }
}
