using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Expressions
{
    [Serializable]
    public class DocumentNameExpression : BaseExpression
    {
        public DocumentNameExpression()
        {
        }

        public override object GetValue(CellManager manager)
        {
            return manager.Properties.DocumentName;
        }

        internal new static DocumentNameExpression Parse(XElement element)
        {
            return new DocumentNameExpression();
        }

        protected override XElement OnSerialize(string expressionName)
        {
            var result = base.OnSerialize(expressionName ?? "DocumentNameExpression");
            return result;
        }

        public override string ToString()
        {
            return "Название отчета " + ID;
        }
    }
}
