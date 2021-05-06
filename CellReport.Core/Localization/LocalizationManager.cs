using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellReport.Localization
{
	public class LocalizationManager
	{
		private static readonly Dictionary<string, string> DefaultStrings = new Dictionary<string, string>();

		public static Func<string, string> LocalResolver;

		public static string GetLocalString(string id)
		{
			if (LocalResolver != null)
				return LocalResolver(id);

			if (id.StartsWith("#"))
				id = id.Substring(1);

			if (DefaultStrings.TryGetValue(id, out var result))
				return result;

			return id;
		}

		public static void AddDefaultString(string id, string value)
		{
			DefaultStrings[id] = value;
		}
	}
}
