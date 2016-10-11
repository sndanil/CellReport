using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Expressions
{
    [Serializable]
    public class NowExpression : BaseExpression
    {
        private DateTime _value = DateTime.Now;
        private bool _freez = false;

        public override object GetValue(CellManager manager)
        {
            if (_freez)
                return _value;
            else
                return DateTime.Now;
        }

        public override void Reset()
        {
            _value = DateTime.Now;
        }

        internal new static NowExpression Parse(XElement element)
        {
            var freez = false;
            if (element.Attribute("Freez") != null)
                freez = bool.Parse(element.Attribute("Freez").Value);

            return new NowExpression() { _freez = freez };
        }

        protected override XElement OnSerialize(string expressionName)
        {
            var result = base.OnSerialize("NowExpression");
            if (_freez)
                result.Add(new XAttribute("Freez", true));

            return result;
        }

        public override string ToString()
        {
            return "Текущее дата и время " + ID;
        }
    }
}
