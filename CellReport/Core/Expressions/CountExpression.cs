using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Expressions
{
	[Serializable]
	public class CountExpression : BaseExpression
	{
		private int _start = 0;
		private int _value = 0;
		private int _increment = 1;

		public int Start
		{
			get { return _start; }
			set
			{
				if (_start != value)
				{
					_start = value;
					OnPropertyChanged("Start");
				}
			}
		}

		public int Increment
		{
			get { return _increment; }
			set
			{
				if (_increment != value)
				{
					_increment = value;
					OnPropertyChanged("Increment");
				}
			}
		}

		public CountExpression(int start, int increment)
		{
			_start = _value = start;
			_increment = increment;
		}

		public override object GetValue(CellManager manager)
		{
			int result = _value;
			_value += _increment;
			return result;
		}

		public override void Reset()
		{
			_value = _start;
		}

		internal new static CountExpression Parse(XElement element)
		{
			int start = 0;
			if (element.Attribute("Start") != null)
				start = int.Parse(element.Attribute("Start").Value, CultureInfo.InvariantCulture);
			int increment = 1;
			if (element.Attribute("Increment") != null)
				increment = int.Parse(element.Attribute("Increment").Value, CultureInfo.InvariantCulture);

			return new CountExpression(start, increment);
		}

		protected override XElement OnSerialize(string expressionName)
		{
			var result = base.OnSerialize(expressionName ?? "CountExpression");
			result.Add(new XAttribute("Start", _start), new XAttribute("Increment", _increment));
			return result;
		}

		public override string ToString()
		{
			return "Счетчик " + ID;
		}
	}
}
