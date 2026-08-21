using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using CellReport.Localization;

namespace CellReport.Core
{
	[Serializable]
	public class TemplateStrings
	{
		private readonly Dictionary<string, Dictionary<string, string>> _cultures = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

		public string DefaultCulture { get; set; }

		public int Count
		{
			get
			{
				var count = 0;
				foreach (var table in _cultures.Values)
					count += table.Count;
				return count;
			}
		}

		public void Add(string culture, string id, string value)
		{
			if (string.IsNullOrEmpty(id))
				throw new ArgumentNullException("id");

			if (string.IsNullOrEmpty(culture))
				culture = DefaultCulture ?? string.Empty;

			Dictionary<string, string> table;
			if (!_cultures.TryGetValue(culture, out table))
			{
				table = new Dictionary<string, string>();
				_cultures[culture] = table;
			}
			table[id] = value;
		}

		public bool TryGet(string culture, string id, out string value)
		{
			value = null;

			Dictionary<string, string> table;
			if (!string.IsNullOrEmpty(culture) && _cultures.TryGetValue(culture, out table))
				return table.TryGetValue(id, out value);

			return false;
		}

		public bool TryGetWithFallback(string culture, string id, out string value)
		{
			if (TryGet(culture, id, out value))
				return true;

			if (!string.IsNullOrEmpty(DefaultCulture) && !string.Equals(DefaultCulture, culture, StringComparison.OrdinalIgnoreCase))
				return TryGet(DefaultCulture, id, out value);

			return false;
		}

		internal XElement Serialize()
		{
			if (_cultures.Count == 0 && string.IsNullOrEmpty(DefaultCulture))
				return null;

			var result = new XElement("Localization");
			if (!string.IsNullOrEmpty(DefaultCulture))
				result.Add(new XAttribute("DefaultCulture", DefaultCulture));

			foreach (var pair in _cultures)
			{
				var strings = new XElement("Strings", new XAttribute("Culture", pair.Key));
				foreach (var item in pair.Value)
					strings.Add(new XElement("String", new XAttribute("ID", item.Key), new XAttribute("Value", item.Value)));
				result.Add(strings);
			}

			return result;
		}

		internal static TemplateStrings Parse(XElement element)
		{
			var result = new TemplateStrings();
			if (element == null)
				return result;

			var defaultCulture = element.Attribute("DefaultCulture");
			if (defaultCulture != null)
				result.DefaultCulture = defaultCulture.Value;

			foreach (var strings in element.Elements("Strings"))
			{
				var cultureAttribute = strings.Attribute("Culture");
				var culture = cultureAttribute == null ? null : cultureAttribute.Value;
				foreach (var item in strings.Elements("String"))
				{
					var idAttribute = item.Attribute("ID");
					if (idAttribute != null && !string.IsNullOrEmpty(idAttribute.Value))
					{
						var valueAttribute = item.Attribute("Value");
						result.Add(culture, idAttribute.Value, valueAttribute == null ? string.Empty : valueAttribute.Value);
					}
				}
			}

			return result;
		}
	}

	public static class TemplateLocalizer
	{
		public static Func<string, string, string> Resolver;

		private static readonly Regex InlineKeyRegex = new Regex(@"#\p{L}[\p{L}\p{N}_\-./]*", RegexOptions.Compiled);
		private static readonly Regex FullKeyRegex = new Regex(@"^#\p{L}[\p{L}\p{N}_\-./]*$", RegexOptions.Compiled);

		public static bool IsKey(string text)
		{
			return !string.IsNullOrEmpty(text) && text[0] == '#';
		}

		public static bool IsFullKey(string text)
		{
			return !string.IsNullOrEmpty(text) && FullKeyRegex.IsMatch(text);
		}

		public static string ResolveInline(TemplateStrings strings, string text, string culture)
		{
			if (string.IsNullOrEmpty(text) || text.IndexOf('#') < 0)
				return text;

			return InlineKeyRegex.Replace(text, delegate (Match match)
			{
				return GetString(strings, match.Value, culture);
			});
		}

		public static string GetString(TemplateStrings strings, string key, string culture)
		{
			if (!IsKey(key))
				return key;

			if (Resolver != null)
			{
				var resolved = Resolver(key, culture);
				if (resolved != null)
					return resolved;
			}

			string value;
			if (strings != null && strings.TryGetWithFallback(culture, key.Substring(1), out value))
				return value;

			return LocalizationManager.GetLocalString(key);
		}
	}
}
