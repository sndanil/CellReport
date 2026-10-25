using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Expressions
{
	public enum Operation
	{
		Truncate,
		DecimalPart,
		DecimalPartAndShift,
		Multiply,
		Add
	}

	[Serializable]
	public class DecimalExpression : BaseExpression
	{
		private Operation _operation = Operation.Truncate;

		private decimal _secondValue = 0m;

		private string _prevExpression = null;

		public Operation Operation
		{
			get { return _operation; }
			set
			{
				if (_operation != value)
				{
					_operation = value;
					OnPropertyChanged("Operation");
				}
			}
		}

		public decimal SecondValue
		{
			get { return _secondValue; }
			set
			{
				if (_secondValue != value)
				{
					_secondValue = value;
					OnPropertyChanged("SecondValue");
				}
			}
		}

		public string PrevExpression
		{
			get { return _prevExpression; }
			set
			{
				if (_prevExpression != value)
				{
					_prevExpression = value;
					OnPropertyChanged("PrevExpression");
				}
			}
		}

		public override object GetValue(CellManager manager, object value)
		{
			if (value is decimal)
			{
				var decimalValue = Convert.ToDecimal(value);
				if (!string.IsNullOrEmpty(_prevExpression) && manager.GetExpressionByID(_prevExpression) != null)
				{
					decimalValue = Convert.ToDecimal(manager.GetExpressionByID(_prevExpression).GetValue(manager, decimalValue));
				}

				switch (_operation)
				{
					default:
					case Operation.Truncate:
						return Math.Truncate(decimalValue);
					case Operation.DecimalPart:
						return decimalValue - Math.Truncate(decimalValue);
					case Operation.DecimalPartAndShift:
						{
							var decimalPart = (decimalValue - Math.Truncate(decimalValue)) * 10m;
							while (decimalPart - Math.Truncate(decimalValue) > 0)
							{
								decimalPart *= 10m;
							}
							return decimalPart;
						}
					case Operation.Multiply:
						return decimalValue * _secondValue;
					case Operation.Add:
						return decimalValue + _secondValue;
				}
			}

			return 0m;
		}

		public override void Reset()
		{
		}

		internal new static DecimalExpression Parse(XElement element)
		{
			var operation = Operation.Truncate;
			if (element.Attribute("Operation") != null && Enum.TryParse<Operation>(element.Attribute("Operation").Value, out operation))
			{

			}

			var secondValue = 0m;
			if (element.Attribute("SecondValue") != null && decimal.TryParse(element.Attribute("SecondValue").Value, NumberStyles.Number, CultureInfo.InvariantCulture, out secondValue))
			{

			}
			var prevExpression = element.Attribute("PrevExpression") != null ? element.Attribute("PrevExpression").Value : null;

			return new DecimalExpression() { _operation = operation, _secondValue = secondValue, _prevExpression = prevExpression };
		}

		protected override XElement OnSerialize(string expressionName)
		{
			var result = base.OnSerialize(nameof(DecimalExpression));
			result.Add(new XAttribute("Operation", _operation.ToString()));

			if (_secondValue != 0)
				result.Add(new XAttribute("SecondValue", _secondValue.ToString(CultureInfo.InvariantCulture)));

			if (_prevExpression != null)
				result.Add(new XAttribute("PrevExpression", _prevExpression));

			return result;
		}

		public override string ToString()
		{
			return $"{ID} {_operation}";
		}
	}
}
