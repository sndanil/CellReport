using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CellReport.Core.Expressions
{
    [Serializable]
    public abstract class BaseExpression : NotifyPropertyChanged
    {
		private string _id;
		private bool _printMode;

        public abstract object GetValue(CellManager manager);

        public virtual void Reset()
        {
        }

        public string ID
		{
			get { return _id; }
			set
			{
				if (_id != value)
				{
					_id = value;
					OnPropertyChanged("ID");
				}
			}
		}
        
		internal bool PrintMode
		{
			get { return _printMode; }
			set
			{
				if (_printMode != value)
				{
					_printMode = value;
					OnPropertyChanged("PrintMode");
				}
			}
		}

        public static BaseExpression Parse(XElement element)
        {
            BaseExpression expr = null;
            switch (element.Name.LocalName)
            {
                case "CountExpression": expr = CountExpression.Parse(element); break;
                case "NowExpression": expr = NowExpression.Parse(element); break;
                case "PagesCountExpression": expr = PagesCountExpression.Parse(element); break;
                case "DocumentNameExpression": expr = DocumentNameExpression.Parse(element); break;
                case "SumExpression": expr = SumExpression.Parse(element); break;
            }

            if (expr != null)
                expr.ID = element.Attribute("ID").Value;

            return expr;
        }

        public XElement Serialize()
        {
            return OnSerialize(null);
        }

        protected virtual XElement OnSerialize(string expressionName)
        {
            var result = new XElement(expressionName ?? "BaseExpression");
            result.Add(new XAttribute("ID", ID));
            return result;
        }

        internal BaseExpression Clone()
        {
            return (BaseExpression)MemberwiseClone();
        }

        public override string ToString()
        {
            return ID;
        } 
    }
}
