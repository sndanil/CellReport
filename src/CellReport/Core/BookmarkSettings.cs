using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace CellReport.Core
{
	/// <summary>
	/// Что делать с закладками, у которых совпадает заголовок.
	/// </summary>
	public enum BookmarkDuplicateMode
	{
		/// <summary>Оставлять все узлы.</summary>
		Keep,

		/// <summary>Не добавлять узел, если у соседей уже есть такой заголовок.</summary>
		Skip,

		/// <summary>Не добавлять узел, если предыдущий сосед называется так же.</summary>
		MergeConsecutive
	}

	/// <summary>
	/// Настройки построения закладок при формировании отчёта.
	/// </summary>
	[Serializable]
	public class BookmarkSettings : NotifyPropertyChanged
	{
		public const string DefaultTitleParam = "Title";

		public const string DefaultTitleFormat = "{Value}";

		private bool _enabled = true;
		private bool _autoSections;
		private string _titleParam = DefaultTitleParam;
		private string _titleFormat = DefaultTitleFormat;
		private bool _skipEmpty;
		private int _maxDepth;
		private BookmarkDuplicateMode _duplicates = BookmarkDuplicateMode.Keep;
		private bool _flushAtEnd = true;

		private readonly Dictionary<string, int> _levels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Строить закладки при формировании отчёта.
		/// </summary>
		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				if (_enabled != value)
				{
					_enabled = value;
					OnPropertyChanged("Enabled");
				}
			}
		}

		/// <summary>
		/// Добавлять закладку для каждой секции отчёта, у которой есть параметр заголовка.
		/// </summary>
		public bool AutoSections
		{
			get { return _autoSections; }
			set
			{
				if (_autoSections != value)
				{
					_autoSections = value;
					OnPropertyChanged("AutoSections");
				}
			}
		}

		/// <summary>
		/// Параметр, из которого берётся заголовок закладки.
		/// </summary>
		public string TitleParam
		{
			get { return _titleParam; }
			set
			{
				var normalized = value ?? string.Empty;
				if (_titleParam != normalized)
				{
					_titleParam = normalized;
					OnPropertyChanged("TitleParam");
				}
			}
		}

		/// <summary>
		/// Формат заголовка: {Value}, {Section}, {Index}, {Parent}.
		/// </summary>
		public string TitleFormat
		{
			get { return _titleFormat; }
			set
			{
				var normalized = value ?? string.Empty;
				if (_titleFormat != normalized)
				{
					_titleFormat = normalized;
					OnPropertyChanged("TitleFormat");
				}
			}
		}

		/// <summary>
		/// Не добавлять закладки с пустым заголовком.
		/// </summary>
		public bool SkipEmpty
		{
			get { return _skipEmpty; }
			set
			{
				if (_skipEmpty != value)
				{
					_skipEmpty = value;
					OnPropertyChanged("SkipEmpty");
				}
			}
		}

		/// <summary>
		/// Максимальная глубина дерева. 0 — без ограничения.
		/// </summary>
		public int MaxDepth
		{
			get { return _maxDepth; }
			set
			{
				var normalized = value < 0 ? 0 : value;
				if (_maxDepth != normalized)
				{
					_maxDepth = normalized;
					OnPropertyChanged("MaxDepth");
				}
			}
		}

		public BookmarkDuplicateMode Duplicates
		{
			get { return _duplicates; }
			set
			{
				if (_duplicates != value)
				{
					_duplicates = value;
					OnPropertyChanged("Duplicates");
				}
			}
		}

		/// <summary>
		/// Достраивать закладки последней секции при завершении формирования.
		/// </summary>
		public bool FlushAtEnd
		{
			get { return _flushAtEnd; }
			set
			{
				if (_flushAtEnd != value)
				{
					_flushAtEnd = value;
					OnPropertyChanged("FlushAtEnd");
				}
			}
		}

		public bool HasLevels
		{
			get { return _levels.Count > 0; }
		}

		/// <summary>
		/// Уровень секции в дереве закладок. 0 — уровень определяется вложенностью правил.
		/// </summary>
		public int GetLevel(string sectionName)
		{
			if (string.IsNullOrEmpty(sectionName))
				return 0;

			int level;
			return _levels.TryGetValue(sectionName, out level) ? level : 0;
		}

		public void SetLevel(string sectionName, int level)
		{
			if (string.IsNullOrEmpty(sectionName))
				return;

			if (level <= 0)
			{
				if (_levels.Remove(sectionName))
					NotifyLevels();
				return;
			}

			int current;
			if (_levels.TryGetValue(sectionName, out current) && current == level)
				return;

			_levels[sectionName] = level;
			NotifyLevels();
		}

		public void ClearLevels()
		{
			if (_levels.Count == 0)
				return;

			_levels.Clear();
			NotifyLevels();
		}

		public IEnumerable<KeyValuePair<string, int>> GetLevels()
		{
			var result = new List<KeyValuePair<string, int>>(_levels);
			result.Sort((left, right) =>
			{
				var byLevel = left.Value.CompareTo(right.Value);
				return byLevel != 0
					? byLevel
					: string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase);
			});
			return result;
		}

		public BookmarkSettings Clone()
		{
			var result = new BookmarkSettings
			{
				Enabled = Enabled,
				AutoSections = AutoSections,
				TitleParam = TitleParam,
				TitleFormat = TitleFormat,
				SkipEmpty = SkipEmpty,
				MaxDepth = MaxDepth,
				Duplicates = Duplicates,
				FlushAtEnd = FlushAtEnd
			};

			foreach (var pair in _levels)
				result._levels[pair.Key] = pair.Value;

			return result;
		}

		internal XElement Serialize()
		{
			var result = new XElement("BookmarkSettings");
			if (!Enabled)
				result.Add(new XAttribute("Enabled", false));
			if (AutoSections)
				result.Add(new XAttribute("AutoSections", true));
			if (!string.Equals(TitleParam, DefaultTitleParam, StringComparison.Ordinal))
				result.Add(new XAttribute("TitleParam", TitleParam ?? string.Empty));
			if (!string.Equals(TitleFormat, DefaultTitleFormat, StringComparison.Ordinal))
				result.Add(new XAttribute("TitleFormat", TitleFormat ?? string.Empty));
			if (SkipEmpty)
				result.Add(new XAttribute("SkipEmpty", true));
			if (MaxDepth > 0)
				result.Add(new XAttribute("MaxDepth", MaxDepth.ToString(CultureInfo.InvariantCulture)));
			if (Duplicates != BookmarkDuplicateMode.Keep)
				result.Add(new XAttribute("Duplicates", Duplicates.ToString()));
			if (!FlushAtEnd)
				result.Add(new XAttribute("FlushAtEnd", false));

			if (_levels.Count > 0)
			{
				var levels = new XElement("Levels");
				foreach (var pair in GetLevels())
				{
					levels.Add(new XElement("Level",
						new XAttribute("Section", pair.Key),
						new XAttribute("Depth", pair.Value.ToString(CultureInfo.InvariantCulture))));
				}

				result.Add(levels);
			}

			return result;
		}

		internal static BookmarkSettings Parse(XElement element)
		{
			var result = new BookmarkSettings();
			if (element == null)
				return result;

			result.Enabled = ReadBool(element, "Enabled", true);
			result.AutoSections = ReadBool(element, "AutoSections", false);
			result.SkipEmpty = ReadBool(element, "SkipEmpty", false);
			result.FlushAtEnd = ReadBool(element, "FlushAtEnd", true);

			var titleParam = element.Attribute("TitleParam");
			if (titleParam != null)
				result.TitleParam = titleParam.Value;

			var titleFormat = element.Attribute("TitleFormat");
			if (titleFormat != null)
				result.TitleFormat = titleFormat.Value;

			var maxDepth = element.Attribute("MaxDepth");
			int depth;
			if (maxDepth != null && int.TryParse(maxDepth.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out depth))
				result.MaxDepth = depth;

			var duplicates = element.Attribute("Duplicates");
			if (duplicates != null)
			{
				BookmarkDuplicateMode mode;
				if (Enum.TryParse(duplicates.Value, true, out mode))
					result.Duplicates = mode;
			}

			var levels = element.Element("Levels");
			if (levels != null)
			{
				foreach (var level in levels.Elements("Level"))
				{
					var section = level.Attribute("Section");
					if (section == null || string.IsNullOrEmpty(section.Value))
						continue;

					int value;
					var depthAttribute = level.Attribute("Depth");
					if (depthAttribute == null || !int.TryParse(depthAttribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
						value = 1;

					result.SetLevel(section.Value, value);
				}
			}

			return result;
		}

		private static bool ReadBool(XElement element, string name, bool defaultValue)
		{
			var attribute = element.Attribute(name);
			bool value;
			return attribute != null && bool.TryParse(attribute.Value, out value) ? value : defaultValue;
		}

		private void NotifyLevels()
		{
			OnPropertyChanged("Levels");
			OnPropertyChanged("HasLevels");
		}
	}
}
