using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Expressions
{
    [Serializable]
    public class PagesCountExpression : BaseExpression
    {
        private int _value = 0;

        public PagesCountExpression()
        {
        }

        public void SetValue(int count)
        {
            _value = count;
        }

        public override object GetValue(CellManager manager)
        {
            return _value;
        }

        internal new static PagesCountExpression Parse(XElement element)
        {
            return new PagesCountExpression();
        }

        protected override XElement OnSerialize(string expressionName)
        {
            var result = base.OnSerialize(expressionName ?? "PagesCountExpression");
            return result;
        }

        public override string ToString()
        {
            return "Количество страниц " + ID;
        }
    }
}
