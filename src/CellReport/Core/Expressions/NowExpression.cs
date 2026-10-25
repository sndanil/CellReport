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
        private bool _freeze = false;

        public bool Freeze
        {
            get { return _freeze; }
            set
            {
                if (_freeze != value)
                {
                    _freeze = value;
                    OnPropertyChanged("Freeze");
                }
            }
        }

        public override object GetValue(CellManager manager)
        {
            if (_freeze)
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
            var freeze = false;
            var freezeAttribute = element.Attribute("Freeze") ?? element.Attribute("Freez");
            if (freezeAttribute != null)
                freeze = bool.Parse(freezeAttribute.Value);

            return new NowExpression() { _freeze = freeze };
        }

        protected override XElement OnSerialize(string expressionName)
        {
            var result = base.OnSerialize("NowExpression");
            if (_freeze)
                result.Add(new XAttribute("Freeze", true));

            return result;
        }

        public override string ToString()
        {
            return "Текущее дата и время " + ID;
        }
    }
}
