using System;
using System.Linq;

namespace CellReport
{
	public interface ISectionObject
	{
		object GetParamValue(string param);

		string GetObjectName();
	}

	public class DefaultSectionObject : ISectionObject
	{
		private object _obj;

		public DefaultSectionObject(object obj)
		{
			_obj = obj;
		}

		public string GetObjectName()
		{
			return _obj.GetType().Name;
		}

		public object GetParamValue(string param)
		{
			var parts = (param ?? string.Empty).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 0)
			{
				if (parts[0] == "this" && parts.Length == 2)
				{
					switch (parts[1])
					{
						case "ToString()":
							return this.ToString();
						default:
							return GetParamValue(parts[1]);
					}
				}
			}

			var property = _obj.GetType().GetProperties().FirstOrDefault(p => p.Name == param);
			if (property != null)
				return property.GetValue(_obj);

			return null;
		}
	}
}