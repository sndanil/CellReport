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
            var name = manager.Properties.DocumentName;
            if (TemplateLocalizer.IsKey(name))
                return TemplateLocalizer.GetString(manager.Properties.Localization, name, manager.Properties.Culture);
            return name;
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
