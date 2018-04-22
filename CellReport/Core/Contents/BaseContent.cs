using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace CellReport.Core.Contents
{
	[Serializable]
	public class BaseContent : NotifyPropertyChanged
	{
		[NonSerialized] protected CellManager _manager;
		private Param[] _params;
		[NonSerialized] private BaseStyle _style;
		private string _styleId;

		public string StyleID
		{
			get { return _styleId; }
			set
			{
				if (_styleId == value)
					return;
				_styleId = value;
				if (_style != null && _style.ID != _styleId)
					_style = null;
				OnPropertyChanged("StyleID");
			}
		}

		internal void SetManager(CellManager manager)
		{
			_manager = manager;
			if (_params != null)
			{
				foreach (var param in _params)
				{
					param.SetManager(manager);
					param.SetOwner(this);
				}
			}
		}

		protected T GetStyle<T>() where T : BaseStyle
		{
			var styleId = (GetParamValueByTarget("Style") as string);
			if (!string.IsNullOrEmpty(styleId) && styleId != StyleID)
				_style = null;
			styleId = styleId ?? StyleID;
			if (_style == null || styleId != _style.ID || CellManager.IsDesignMode)
				_style = _manager.GetStyleByID<T>(styleId);

			return (T)_style;
		}

		protected void SetStyle(BaseStyle style, BaseStyle defaultStyle)
		{
			if (ReferenceEquals(defaultStyle, style) || style == null)
				StyleID = string.Empty;
			else
				StyleID = style.ID;
		}

		public virtual BaseStyle GetStyle()
		{
			return null;
		}

		public virtual BaseStyle GetActualStyle()
		{
			return GetStyle();
		}

		public BaseContent(CellManager manager)
		{
			StyleID = string.Empty;
			_manager = manager;
		}

		public void AddParam(Param param)
		{
			var tempParams = new List<Param>();
			if (_params != null)
				tempParams.AddRange(_params);

			var sameParam = tempParams.FirstOrDefault(p => p.Name == param.Name && !string.IsNullOrEmpty(param.Name));
			if (sameParam != null)
				tempParams.Remove(sameParam);

			tempParams.Add(param);
			_params = tempParams.ToArray();
			OnPropertyChanged("Params");
		}

		public void RemoveParam(Param param)
		{
			if (_params == null)
				return;

			var tempParams = new List<Param>();
			if (_params != null)
				tempParams.AddRange(_params);

			tempParams.Remove(param);

			_params = tempParams.Count == 0 ? null : tempParams.ToArray();

			OnPropertyChanged("Params");
		}

		public virtual IEnumerable<Param> GetParams()
		{
			if (_params != null)
				foreach (var param in _params)
					yield return param;
		}

		internal virtual void ExecuteExpressions()
		{
			if (_params != null)
			{
				foreach (var param in _params)
					param.ExecuteExpression();
			}
		}

		internal virtual void ResetExpressions()
		{
			if (_params != null)
			{
				foreach (var param in _params)
					param.ResetExpression();
			}
		}

		public virtual void ReplaceExpressionId(string oldId, string newId)
		{
			if (_params != null)
			{
				foreach (var param in _params)
					param.ReplaceExpressionId(oldId, newId);
			}
		}

		public virtual void SetContentParam(string paramName, object value)
		{
			var param = _params?.FirstOrDefault(p => p.Name == paramName);
			if (param != null)
				param.Value = value;
		}

		public string GetParamNameByTarget(string target)
		{
			var param = _params?.FirstOrDefault(p => p.Target == target);
			return param?.Name;
		}

		public object GetParamValueByTarget(string target)
		{
			var param = _params?.FirstOrDefault(p => p.Target == target);
			return param?.Value;
		}

		public object GetParamValueByName(string name)
		{
			var param = _params?.FirstOrDefault(p => p.Name == name);
			return param?.Value;
		}

		public string GetFormattedParamValueByName(string name)
		{
			var param = _params?.FirstOrDefault(p => p.Name == name);
			if (param?.Value != null)
			{
				if (param.Value is DateTime || param.Type == nameof(DateTime))
				{
					var value = DateTime.MinValue;
					if (param.Value is string)
					{
						if (!DateTime.TryParseExact(param.Value?.ToString(), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
							&& !DateTime.TryParseExact(param.Value?.ToString(), param.Format ?? "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
							return string.Empty;
					}
					else
						value = Convert.ToDateTime(param.Value);

					if (string.IsNullOrEmpty(param.Format))
						return value.ToString(CultureInfo.CurrentCulture);
					if (string.IsNullOrEmpty(param.Culture))
						return value.ToString(param.Format);

					return value.ToString(param.Format, CultureInfo.GetCultureInfo(param.Culture));
				}
				if (param.Value is Decimal || param.Value is Int32 || param.Value is Int64 || param.Type == nameof(Decimal))
				{
					var value = 0m;
					if (param.Value is string)
					{
						if (!decimal.TryParse(param.Value?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
							return string.Empty;
					}
					else
						value = Convert.ToDecimal(param.Value);

					if (string.IsNullOrEmpty(param.Format))
						return value.ToString(CultureInfo.CurrentCulture);

					if (param.Format.StartsWith("currency", StringComparison.OrdinalIgnoreCase) || param.Format.StartsWith("number", StringComparison.InvariantCultureIgnoreCase))
					{
						if (!string.IsNullOrEmpty(param.Culture))
							NumberToString.CurrentCulture.Value = param.Culture;

						try
						{
							if (param.Format.Equals("numberString", StringComparison.OrdinalIgnoreCase))
								return CurrencyToString.GetString(value, true, "", "", "", "", "", "", true);

							var parts = param.Format.Split('|');
							if (parts.Length == 1)
								return CurrencyToString.GetString(value);

							return CurrencyToString.GetString(value, parts[1]);
						}
						finally
						{
							NumberToString.CurrentCulture.Value = null;
						}
					}

					if (string.IsNullOrEmpty(param.Culture))
						return value.ToString(param.Format);

					return value.ToString(param.Format, CultureInfo.GetCultureInfo(param.Culture));

				}

				return param.Value.ToString();
			}

			return null;
		}

		public virtual BaseContent Clone(CellManager manager)
		{
			var clone = (BaseContent)MemberwiseClone();
			clone._manager = manager;
			if (_params != null)
				clone._params = _params.Select(p => p.Clone(manager, clone)).ToArray();
			return clone;
		}

		public static BaseContent Parse(CellManager manager, XElement parentElement)
		{
			var contents = new CollectionContent(manager, 4);
			foreach (var contentElement in parentElement.Elements())
			{
				var content = ParseContent(manager, contentElement);
				if (content != null)
					contents.Add(content);
			}
			return contents.GetContents().Count() > 1 ? contents : contents.GetContents().FirstOrDefault();
		}

		public static BaseContent ParseContent(CellManager manager, XElement element)
		{
			BaseContent content = null;
			switch (element.Name.LocalName)
			{
				case "TextContent":
					content = TextContent.Parse(manager, element);
					break;
				case "DecimalContent":
					content = DecimalContent.Parse(manager, element);
					break;
				case "DateTimeContent":
					content = DateTimeContent.Parse(manager, element);
					break;
				case "LineContent":
					content = LineContent.Parse(manager, element);
					break;
				case "BarCodeContent":
					content = BarCodeContent.Parse(manager, element);
					break;
				case "ActionContent":
					content = ActionContent.Parse(manager, element);
					break;
				case "ImageContent":
					content = ImageContent.Parse(manager, element);
					break;
				case "ChartContent":
					content = ChartContent.Parse(manager, element);
					break;
				case "CollectionContent":
					content = CollectionContent.Parse(manager, element);
					break;
			}

			if (content != null)
			{
				if (element.Attribute("Style") != null)
					content.StyleID = element.Attribute("Style").Value;
				ParseParams(element, content, manager);
			}

			return content;
		}

		internal static void ParseParams(XElement element, BaseContent content, CellManager manager)
		{
			foreach (var paramElement in element.Elements("Param"))
				content.AddParam(Param.Parse(paramElement, manager, content));
		}

		public XElement Serialize()
		{
			return OnSerialize(null);
		}

		protected virtual XElement OnSerialize(string contentName)
		{
			var result = new XElement(contentName ?? "BaseContent");
			if (!string.IsNullOrEmpty(StyleID))
				result.Add(new XAttribute("Style", StyleID));
			foreach (var param in GetParams().Where(p => !p.IsVirtual))
				result.Add(param.Serialize());

			return result;
		}

		public override string ToString()
		{
			var parts = GetType().ToString().Split('.');
			return parts[parts.Length - 1];
		}
	}
}
