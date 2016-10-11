using System;
using System.Xml.Linq;

using CellReport.Core.Expressions;

namespace CellReport.Core.Contents
{
	[Serializable]
	public class Param : NotifyPropertyChanged
	{
		private object _value = null;
        [NonSerialized]
		private BaseExpression _expression = null;
        [NonSerialized]
		private CellManager _manager = null;
        [NonSerialized]
        private BaseContent _owner = null;
        private string _name;
        private string _target;
		private string _expressionId;
		private string _culture;
		private string _format;
		private string _type;

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

		public string Name
		{
			get { return _name; }
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
			if (Expression != null)
				Expression.Reset();
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
				if (Expression != null && Expression is SumExpression)
					return Expression.GetValue(_manager);

				if (!string.IsNullOrEmpty(Type) && _value != null)
				{
					var type = System.Type.GetType(Type);
					return Convert.ChangeType(_value, type);
				}
				else
					return _value;
			}
			set
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}

		public string Target
		{
			get { return _target; }
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
			get { return _expressionId; }
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
			get { return _culture; }
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
			get { return _format; }
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
			get { return _type; }
			set
			{
				if (_type != value)
				{
					_type = value;
					OnPropertyChanged("Type");
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
				Value = Value
			};
		}

		internal XElement Serialize()
		{
			return new XElement("Param",
							string.IsNullOrEmpty(Name) ? null : new XAttribute("Name", Name),
							string.IsNullOrEmpty(Target) ? null : new XAttribute("Target", Target),
							Value == null ? null : new XAttribute("Value", Value),
							Expression == null ? null : new XAttribute("Expression", ExpressionID),
							string.IsNullOrEmpty(Type) ? null : new XAttribute("Type", Type),
							string.IsNullOrEmpty(Format) ? null : new XAttribute("Format", Format),
							string.IsNullOrEmpty(Culture) ? null : new XAttribute("Culture", Culture)
							);
		}

		internal static Param Parse(XElement element, CellManager manager, BaseContent owner)
		{
			var param = new Param(manager, owner);
			if (element.Attribute("Name") != null)
				param.Name = element.Attribute("Name").Value;
			else
				param.Name = string.Empty;
			if (element.Attribute("Target") != null)
				param.Target = element.Attribute("Target").Value;
			if (element.Attribute("Expression") != null)
				param.ExpressionID = element.Attribute("Expression").Value;
			if (element.Attribute("Value") != null /*&& !string.IsNullOrEmpty(element.Attribute("Value").Value)*/)
				param.Value = element.Attribute("Value").Value;
			if (element.Attribute("Culture") != null)
				param.Culture = element.Attribute("Culture").Value;
			if (element.Attribute("Format") != null)
				param.Format = element.Attribute("Format").Value;
			if (element.Attribute("Type") != null)
				param.Type = element.Attribute("Type").Value;

			return param;
		}

		public override string ToString()
		{
			return Name ?? "Without name";
		}

        protected override void OnPropertyChanged(string name)
        {
            base.OnPropertyChanged(name);
            _owner.RaisePropertyChanged(name);
        }
	}
}
