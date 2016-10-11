using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Expressions
{
    [Serializable]
    public class SumExpression : BaseExpression
    {
        private CellLocation _start;
        private CellLocation _end;
		private string _paramName;
		private bool _perPage;

        public string ParamName
        {
			get { return _paramName; }
			set
			{
				if (_paramName != value)
				{
					_paramName = value;
					OnPropertyChanged("ParamName");
				}
			}
		}

        public bool PerPage
        {
			get { return _perPage; }
			set
			{
				if (_perPage != value)
				{
					_perPage = value;
					OnPropertyChanged("PerPage");
				}
			}
		}

        public SumExpression()
        {
        }

        public void SetRange(CellLocation start, CellLocation end)
        {
            _start = start;
            _end = end;
        }

        public override object GetValue(CellManager manager)
        {
            var sum = 0m;
            if (PrintMode && PerPage)
            {
                for (int row = _start.Row; row <= _end.Row; row++)
                {
                    for (int col = _start.Column; col <= _end.Column; col++)
                    {
                        var cell = manager.GetCell(row, col);
                        if (cell != null && cell.Content != null)
                            sum += cell.Content.GetParams().Where(p => p.Name == ParamName).Select(p => Convert.ToDecimal(p.Value)).Sum();
                    }
                }
            }
            else
            {
                for (int row = 0; row < manager.RowsCount; row++)
                {
                    for (int col = 0; col < manager.ColumnsCount; col++)
                    {
                        var cell = manager.GetCell(row, col);
                        if (cell != null && cell.Content != null)
                            sum += cell.Content.GetParams().Where(p => p.Name == ParamName).Select(p => Convert.ToDecimal(p.Value)).Sum();
                    }
                }
            }
            return sum;
        }

        internal new static SumExpression Parse(XElement element)
        {
            var result = new SumExpression() { ParamName = element.Attribute("ParamName").Value };
            if (element.Attribute("PerPage") != null)
                result.PerPage = Convert.ToBoolean(element.Attribute("PerPage").Value);
            
            return result;
        }

        protected override XElement OnSerialize(string expressionName)
        {
            var result = base.OnSerialize(expressionName ?? "SumExpression");
            result.Add(new XAttribute("ParamName", ParamName));
            if (PerPage)
                result.Add(new XAttribute("PerPage", PerPage));
            return result;
        }

        public override string ToString()
        {
            return "Сумма " + ID;
        }
    }
}
