using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;

using CellReport.Core.Expressions;

namespace CellReport.Core.Contents
{
	[Serializable]
	public class Param : NotifyPropertyChanged
	{
		private object _value;
		[NonSerialized]
		private BaseExpression _expression;
		[NonSerialized]
		private CellManager _manager;
		[NonSerialized]
		private BaseContent _owner;
		private string _name;
		private string _target;
		private string _expressionId;
		private string _culture;
		private string _format;
		private string _type;
		private string _assign;

		internal void SetManager(CellManager manager)
		{
			_manager = manager;
		}

		internal void SetOwner(BaseContent content)
		{
			_owner = content;
		}

		public Param(CellManager manager, BaseContent owner)
		{
			_manager = manager;
			_owner = owner;
		}

		public bool IsVirtual { get; set; }

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged("Name");
				}
			}
		}

		internal void ExecuteExpression()
		{
			if (Expression != null)
				Value = Expression.GetValue(_manager);
		}

		internal void ResetExpression()
		{
			Expression?.Reset();
		}

		internal void ReplaceExpressionId(string oldId, string newId)
		{
			if (ExpressionID == oldId)
				ExpressionID = newId;
		}

		public object Value
		{
			get
			{
				if (Expression is SumExpression)
				{
					return Expression.GetValue(_manager);
				}

				if (!string.IsNullOrEmpty(Type) && _value != null)
				{
					var type = System.Type.GetType("System." + Type);
					if (type != null)
					{
						return Convert.ChangeType(_value, type);
					}
				}

				return _value;
			}
			set
			{
				if (Expression is DecimalExpression)
				{
					_value = Expression.GetValue(_manager, value);
				}
				else
				{
					_value = value;
				}

				OnPropertyChanged("Value");
			}
		}

		public string Target
		{
			get => _target;
			set
			{
				if (_target != value)
				{
					_target = value;
					OnPropertyChanged("Target");
				}
			}
		}

		public string ExpressionID
		{
			get => _expressionId;
			set
			{
				if (_expressionId != value)
				{
					_expressionId = value;
					OnPropertyChanged("ExpressionID");
				}
			}
		}

		public BaseExpression Expression
		{
			get
			{
				if (CellManager.IsDesignMode && !string.IsNullOrEmpty(ExpressionID))
					return _manager.GetExpressionByID(ExpressionID);

				if (_expression == null && !string.IsNullOrEmpty(ExpressionID))
					_expression = _manager.GetExpressionByID(ExpressionID);

				return _expression;
			}
			set
			{
				if (value != null)
				{
					_expression = value;
					ExpressionID = _expression.ID;
				}
				else
				{
					_expression = null;
					ExpressionID = string.Empty;
				}
			}
		}

		public string Culture
		{
			get => _culture;
			set
			{
				if (_culture != value)
				{
					_culture = value;
					OnPropertyChanged("Culture");
				}
			}
		}

		public string Format
		{
			get => _format;
			set
			{
				if (_format != value)
				{
					_format = value;
					OnPropertyChanged("Format");
				}
			}
		}

		public string Type
		{
			get => _type;
			set
			{
				if (_type != value)
				{
					_type = value;
					OnPropertyChanged("Type");
				}
			}
		}

		public string Assign
		{
			get => _assign;
			set
			{
				if (_assign != value)
				{
					_assign = value;
					OnPropertyChanged("Assign");
				}
			}
		}

		public Param Clone(CellManager manager, BaseContent owner)
		{
			return new Param(manager, owner)
			{
				Culture = Culture,
				ExpressionID = ExpressionID,
				Name = Name,
				Format = Format,
				Target = Target,
				Type = Type,
				Value = Value,
				Assign = Assign
			};
		}

		internal XElement Serialize()
		{
			return new XElement("Param",
							string.IsNullOrEmpty(Name) ? null : new XAttribute("Name", Name),
							string.IsNullOrEmpty(Target) ? null : new XAttribute("Target", Target),
							Value == null ? null : new XAttribute("Value", GetValueForSerialization()),
							string.IsNullOrEmpty(Assign) ? null : new XAttribute("Assign", Assign),
							Expression == null ? null : new XAttribute("Expression", ExpressionID),
							string.IsNullOrEmpty(Type) ? null : new XAttribute("Type", Type),
							string.IsNullOrEmpty(Format) ? null : new XAttribute("Format", Format),
							string.IsNullOrEmpty(Culture) ? null : new XAttribute("Culture", Culture)
							);
		}

		private string GetValueForSerialization()
		{
			if (Value is DateTime date)
			{
				return date.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
			}
			else if (Value is Decimal || Value is Int32 || Value is Int64 || Type == nameof(Decimal))
			{
				var value = 0m;
				if (Value is string str)
				{
					return str;
				}
				else
					value = Convert.ToDecimal(Value);

				return value.ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				return Value?.ToString();
			}
		}

		internal static Param Parse(XElement element, CellManager manager, BaseContent owner)
		{
			var param = new Param(manager, owner);
			if (element.Attribute("Name") != null)
				param.Name = element.Attribute("Name")?.Value;
			else
				param.Name = string.Empty;
			if (element.Attribute("Target") != null)
				param.Target = element.Attribute("Target")?.Value;
			if (element.Attribute("Expression") != null)
				param.ExpressionID = element.Attribute("Expression")?.Value;
			if (element.Attribute("Type") != null)
				param.Type = element.Attribute("Type")?.Value;
			if (element.Attribute("Value") != null)
				param.Value = GetValueForDeserialization(param, element.Attribute("Value")?.Value);
			if (element.Attribute("Culture") != null)
				param.Culture = element.Attribute("Culture")?.Value;
			if (element.Attribute("Format") != null)
				param.Format = element.Attribute("Format")?.Value;
			if (element.Attribute("Assign") != null)
				param.Assign = element.Attribute("Assign")?.Value;

			return param;
		}

		private static object GetValueForDeserialization(Param param, string value)
		{
			if (param.Type == nameof(DateTime))
			{
				var formats = new[] 
				{
					"yyyy-MM-ddTHH:mm:ss",
					"yyyy-MM-ddTHH:mm:sszzz"
				};
				DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
				return date;
			}
			else if (param.Type == nameof(Decimal))
			{
				if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
					return value;

				return number;
			}

			return value;
		}

		public override string ToString()
		{
			return Name ?? "Without name";
		}

		protected override void OnPropertyChanged(string name)
		{
			base.OnPropertyChanged(name);
			_owner.RaisePropertyChanged(this, new PropertyChangedEventArgs(name));
		}
	}
}
