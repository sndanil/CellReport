using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using CellReport.Core;
using CellReport.Core.BookMarks;
using CellReport.Core.Contents;

namespace CellReport
{
	[Serializable]
	public class ReportDocument
	{
		[NonSerialized] private readonly CellTemplate _template;
		[NonSerialized] private int _rowNumberForAddSection;
		[NonSerialized] private int _rowNumberForJoinSection;
		[NonSerialized] private int _colNumberForJoinSection;
		[NonSerialized] private readonly List<Cell> _lastAddedCells = new List<Cell>();
		[NonSerialized] private string _lastSectionName;
		[NonSerialized] private readonly Dictionary<BookMark, BookMark> _lastByRule = new Dictionary<BookMark, BookMark>();
		[NonSerialized] private readonly Dictionary<BookMark, int> _depthByRule = new Dictionary<BookMark, int>();
		[NonSerialized] private readonly List<BookmarkLevelEntry> _levelStack = new List<BookmarkLevelEntry>();
		[NonSerialized] private readonly Dictionary<string, int> _sectionIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		[NonSerialized] private bool _buildingBookMarks;
		[NonSerialized] private bool _staticBookMarksAdded;

		[field: NonSerialized]
		public event Action<string> SectionAttached;

		[field: NonSerialized]
		public event Action<ISectionObject> SetDefaultSectionObject;

		public CellManager Manager { get; private set; }

		public string Culture
		{
			get { return Manager.Properties.Culture; }
			set { Manager.Properties.Culture = value; }
		}

		public TemplateStrings Localization
		{
			get { return Manager.Properties.Localization; }
		}

		public ReportDocument(CellTemplate template)
		{
			_template = template ?? throw new ArgumentNullException(nameof(template));
			CreateManager();
		}

		public ReportDocument(CellTemplate template, CellManager manager)
		{
			_template = template ?? throw new ArgumentNullException(nameof(template));
			if (manager != null)
				Manager = manager;
			else
				CreateManager();
		}

		public ReportDocument(string templateFileName)
		{
			_template = new CellTemplate();
			_template.Load(templateFileName);
			CreateManager();
		}

		public ReportDocument(System.IO.Stream stream)
		{
			_template = new CellTemplate();
			_template.Load(stream);
			CreateManager();
		}

		private void CreateManager()
		{
			var rowsCount = Math.Max(1, _template.GetMaxRowsCountInSections());
			var colsCount = Math.Max(1, _template.GetMaxColumnsCountInSections());
			Manager = new CellManager(rowsCount, colsCount, _template.Manager.Properties.DefaultRowHeight, _template.Manager.Properties.DefaultColumn)
			{
				DisplaySettings = _template.Manager.DisplaySettings.Clone(),
				ExportSettings = _template.Manager.ExportSettings.Clone(),
				TemplateDirectory = _template.Manager.TemplateDirectory,
				UriDataLoaderOptions = _template.Manager.UriDataLoaderOptions.Clone()
			};
			Manager.PrintSettings = _template.Manager.PrintSettings.Clone(Manager);
			Manager.Properties = _template.Manager.Properties.Clone();
			foreach (var resourceName in _template.Manager.GetResourceNames())
			{
				var resource = _template.Manager.GetResource(resourceName);
				Manager.AddResource(resourceName, (byte[])resource.Clone());
			}
			foreach (var expr in _template.Manager.GetExpressions())
				Manager.AddExpression(expr.Clone());
			foreach (var style in _template.Manager.GetStyles())
				Manager.AddStyle(style.Clone());
			//foreach (var style in _template.Manager.GetExtStyles())
			//	Manager.AddExternalStyle(style);

			Manager.BookmarkSettings = _template.Manager.BookmarkSettings.Clone();
			Manager.SetBookMarksCompletion(BuildBookMarks);
		}

		/// <summary>
		/// Достраивает закладки последней секции. Вызывается при завершении формирования
		/// отчёта и лениво — при первом чтении <see cref="CellManager.GetBookMarks"/>.
		/// </summary>
		public void BuildBookMarks()
		{
			if (_buildingBookMarks || Manager == null)
				return;

			var settings = Manager.BookmarkSettings;
			if (settings != null && (!settings.Enabled || !settings.FlushAtEnd))
				return;

			if (string.IsNullOrEmpty(_lastSectionName))
				return;

			_buildingBookMarks = true;
			try
			{
				CompleteSection();
			}
			finally
			{
				_buildingBookMarks = false;
			}
		}

		/// <summary>
		/// Завершает секцию, добавленную последней: её значения уже установлены,
		/// поэтому можно строить закладки по правилам шаблона.
		/// </summary>
		private void CompleteSection()
		{
			var settings = Manager.BookmarkSettings ?? new BookmarkSettings();
			var sectionName = _lastSectionName;
			_lastSectionName = null;

			if (!settings.Enabled)
				return;

			if (string.IsNullOrEmpty(sectionName))
			{
				AddStaticBookMarks();
				return;
			}

			var index = NextSectionIndex(sectionName);

			var hasExplicitRule = false;
			foreach (var rule in _template.Manager.GetBookMarks())
			{
				if (string.IsNullOrEmpty(rule.SectionID))
					continue;

				hasExplicitRule |= AddRule(rule, null, sectionName, index);
			}

			if (settings.AutoSections && !hasExplicitRule)
				AddAutoBookMark(settings, sectionName, index);
		}

		private void AddStaticBookMarks()
		{
			if (_staticBookMarksAdded)
				return;

			_staticBookMarksAdded = true;
			foreach (var item in _template.Manager.GetBookMarks())
			{
				if (string.IsNullOrEmpty(item.SectionID))
					Manager.AddBookMark(item.Clone());
			}
		}

		private int NextSectionIndex(string sectionName)
		{
			int current;
			_sectionIndexes.TryGetValue(sectionName, out current);
			current++;
			_sectionIndexes[sectionName] = current;
			return current;
		}

		private bool AddRule(BookMark rule, BookMark parentRule, string sectionName, int index)
		{
			var added = false;
			if (string.Equals(rule.SectionID, sectionName, StringComparison.Ordinal))
				added = AddRuleInstance(rule, parentRule, sectionName, index);

			// дочерние правила проверяются всегда: они описывают секции,
			// которые добавляются после родительской
			foreach (var child in rule.Child)
				added |= AddRule(child, rule, sectionName, index);

			return added;
		}

		private bool AddRuleInstance(BookMark rule, BookMark parentRule, string sectionName, int index)
		{
			var settings = Manager.BookmarkSettings ?? new BookmarkSettings();
			var titleParam = string.IsNullOrEmpty(rule.TextParamName) ? settings.TitleParam : rule.TextParamName;

			object value;
			CellLocation location;
			if (!TryGetParamValue(titleParam, out value, out location))
				return false;

			var rawValue = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
			if (RequiresValue(settings, rule) && string.IsNullOrWhiteSpace(rawValue))
				return false;

			var bookmark = new BookMark
			{
				SectionID = sectionName,
				Name = BuildTitle(settings, rule.Name, rawValue, sectionName, index, GetLastRuleName(parentRule)),
				Location = location,
				TextParamName = titleParam
			};

			return AttachBookMark(bookmark, rule, parentRule, sectionName);
		}

		private void AddAutoBookMark(BookmarkSettings settings, string sectionName, int index)
		{
			var titleParam = string.IsNullOrEmpty(settings.TitleParam) ? BookmarkSettings.DefaultTitleParam : settings.TitleParam;

			object value;
			CellLocation location;
			if (!TryGetParamValue(titleParam, out value, out location))
				return;

			var rawValue = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
			if (settings.SkipEmpty && string.IsNullOrWhiteSpace(rawValue))
				return;

			var bookmark = new BookMark
			{
				SectionID = sectionName,
				Name = BuildTitle(settings, null, rawValue, sectionName, index, null),
				Location = location,
				TextParamName = titleParam
			};

			AttachBookMark(bookmark, null, null, sectionName);
		}

		private static bool RequiresValue(BookmarkSettings settings, BookMark rule)
		{
			return !string.IsNullOrEmpty(rule.VisibleParamName) || settings.SkipEmpty;
		}

		private bool TryGetParamValue(string paramName, out object value, out CellLocation location)
		{
			value = null;
			location = CellLocation.None;
			if (string.IsNullOrEmpty(paramName))
				return false;

			foreach (var cell in _lastAddedCells)
			{
				var content = cell.Content;
				if (content == null)
					continue;

				foreach (var param in content.GetParams())
				{
					if (!string.Equals(param.Name, paramName, StringComparison.Ordinal))
						continue;

					value = param.Value;
					location = cell.Location;
					return true;
				}
			}

			return false;
		}

		private string GetLastRuleName(BookMark parentRule)
		{
			if (parentRule == null)
				return null;

			BookMark parent;
			return _lastByRule.TryGetValue(parentRule, out parent) ? parent.Name : null;
		}

		private static string BuildTitle(
			BookmarkSettings settings,
			string fallback,
			string value,
			string sectionName,
			int index,
			string parentName)
		{
			var format = string.IsNullOrEmpty(settings.TitleFormat)
				? BookmarkSettings.DefaultTitleFormat
				: settings.TitleFormat;

			var title = format
				.Replace("{Value}", value ?? string.Empty)
				.Replace("{Section}", sectionName ?? string.Empty)
				.Replace("{Index}", index.ToString(CultureInfo.InvariantCulture))
				.Replace("{Parent}", parentName ?? string.Empty)
				.Trim();

			if (title.Length == 0)
				title = fallback ?? string.Empty;
			if (title.Length == 0)
				title = sectionName ?? string.Empty;

			return title;
		}

		private bool AttachBookMark(BookMark bookmark, BookMark rule, BookMark parentRule, string sectionName)
		{
			var settings = Manager.BookmarkSettings ?? new BookmarkSettings();
			var depth = ResolveDepth(settings, sectionName, parentRule);
			if (settings.MaxDepth > 0 && depth > settings.MaxDepth)
				return false;

			var parent = ResolveParent(settings, depth, parentRule);

			bool add;
			var node = ApplyDuplicates(settings, parent, bookmark, out add);
			if (add)
			{
				if (parent == null)
					Manager.AddBookMark(node);
				else
					parent.Child.Add(node);
			}

			if (rule != null)
			{
				_lastByRule[rule] = node;
				_depthByRule[rule] = depth;
			}

			PushLevel(depth, node);
			return add;
		}

		private int ResolveDepth(BookmarkSettings settings, string sectionName, BookMark parentRule)
		{
			var level = settings.GetLevel(sectionName);
			if (level > 0)
				return level;

			int parentDepth;
			if (parentRule != null && _depthByRule.TryGetValue(parentRule, out parentDepth))
				return parentDepth + 1;

			return 1;
		}

		private BookMark ResolveParent(BookmarkSettings settings, int depth, BookMark parentRule)
		{
			if (settings.HasLevels)
			{
				for (var i = _levelStack.Count - 1; i >= 0; i--)
				{
					if (_levelStack[i].Level < depth)
						return _levelStack[i].BookMark;
				}

				return null;
			}

			BookMark parent;
			if (parentRule != null && _lastByRule.TryGetValue(parentRule, out parent))
				return parent;

			return null;
		}

		private BookMark ApplyDuplicates(BookmarkSettings settings, BookMark parent, BookMark bookmark, out bool add)
		{
			add = true;
			if (settings.Duplicates == BookmarkDuplicateMode.Keep)
				return bookmark;

			IList<BookMark> siblings = parent == null
				? (IList<BookMark>)Manager.BookMarksStorage
				: parent.Child;

			if (settings.Duplicates == BookmarkDuplicateMode.Skip)
			{
				for (var i = 0; i < siblings.Count; i++)
				{
					if (!string.Equals(siblings[i].Name, bookmark.Name, StringComparison.Ordinal))
						continue;

					add = false;
					return siblings[i];
				}

				return bookmark;
			}

			if (siblings.Count == 0)
				return bookmark;

			var last = siblings[siblings.Count - 1];
			if (!string.Equals(last.Name, bookmark.Name, StringComparison.Ordinal))
				return bookmark;

			add = false;
			return last;
		}

		private void PushLevel(int depth, BookMark bookmark)
		{
			for (var i = _levelStack.Count - 1; i >= 0; i--)
			{
				if (_levelStack[i].Level >= depth)
					_levelStack.RemoveAt(i);
			}

			_levelStack.Add(new BookmarkLevelEntry { Level = depth, BookMark = bookmark });
		}

		private struct BookmarkLevelEntry
		{
			public int Level;
			public BookMark BookMark;
		}

		public void AddPageBreak()
		{
			if (_rowNumberForAddSection > 0)
				Manager.AddPageBreakRow(_rowNumberForAddSection - 1);
		}

		private string GetObjectName(object obj)
		{
			string objectName = null;
			if (obj is ISectionObject sectionObject)
				objectName = sectionObject.GetObjectName();

			if (obj != null && objectName == null)
				objectName = (new DefaultSectionObject(obj)).GetObjectName();

			return objectName;
		}

		public void AddSection(string sectionName)
		{
			AddSection(sectionName, null);
		}

		protected void AddSection(string sectionName, string objectName)
		{
			CompleteSection();
			_lastSectionName = sectionName;
			var section = _template.GetSectionByName(sectionName, objectName);
			CreateCells(_rowNumberForAddSection, 0, section);
			_rowNumberForJoinSection = _rowNumberForAddSection;
			_rowNumberForAddSection += section.Height;
			_colNumberForJoinSection = section.Width;
			if (section.IsHeaderFixed)
				Manager.PrintSettings.HeaderFixedRows.Add(new Core.Range(_rowNumberForAddSection - section.Height, _rowNumberForAddSection - 1));
			if (section.IsFooterFixed)
				Manager.PrintSettings.FooterFixedRows.Add(new Core.Range(_rowNumberForAddSection - section.Height, _rowNumberForAddSection - 1));

			SectionAttached?.Invoke(sectionName);
		}

		public ReportDocument AddSection(string sectionName, object obj)
		{
			var objectName = GetObjectName(obj);

			if (!(obj is string) && obj is IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					AddSection(sectionName, objectName);
					ProcessSection(item);
				}
			}
			else
			{
				AddSection(sectionName, objectName);
				ProcessSection(obj);
			}

			return this;
		}

		private void ProcessSection(object obj)
		{
			if (obj != null)
			{
				if (obj is ISectionObject sectionObject)
					SetSectionObject(sectionObject);
				else
					SetSectionObject(new DefaultSectionObject(obj));
			}
		}

		public void JoinSection(string sectionName)
		{
			JoinSection(sectionName, null);
		}

		protected void JoinSection(string sectionName, string objectName)
		{
			CompleteSection();
			_lastSectionName = sectionName;
			var section = _template.GetSectionByName(sectionName, objectName);
			CreateCells(_rowNumberForJoinSection, _colNumberForJoinSection, section);
			_colNumberForJoinSection += section.Width;

			SectionAttached?.Invoke(sectionName);
		}

		public ReportDocument JoinSection(string sectionName, object obj)
		{
			var objectName = GetObjectName(obj);

			if (!(obj is string) && obj is IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					JoinSection(sectionName, objectName);
					ProcessSection(item);
				}
			}
			else
			{
				JoinSection(sectionName, objectName);
				ProcessSection(obj);
			}

			return this;
		}

		public void SetParameter(string paramName, object value)
		{
			foreach (var cell in _lastAddedCells)
			{
				cell.Content?.SetContentParam(paramName, value);
			}
		}

		public void SetSectionObject(ISectionObject obj)
		{
			foreach (var cell in _lastAddedCells)
			{
				foreach (var param in cell.Content?.GetParams() ?? new Param[0])
				{
					var value = obj.GetParamValue(param.Name) ?? obj.GetParamValue(param.Assign);

					if (value != null)
						SetParameter(param.Name, value);
				}
			}

			SetDefaultSectionObject?.Invoke(obj);
		}

		public void SetSectionObject(string prefix, ISectionObject obj)
		{
			foreach (var cell in _lastAddedCells)
			{
				foreach (var param in cell.Content?.GetParams() ?? new Param[0])
				{
					if (param.Name?.StartsWith(prefix) == true)
					{
						var value = obj.GetParamValue(param.Name.Replace(prefix, string.Empty));
						if (value != null)
							SetParameter(param.Name, value);
					}
					if (param.Assign?.StartsWith(prefix) == true)
					{
						var value = obj.GetParamValue(param.Assign.Replace(prefix, string.Empty));
						if (value != null)
							SetParameter(param.Name, value);
					}
				}
			}
		}

		public void FitDocument()
		{
			BuildBookMarks();
			Manager.RowsCount = Manager.ActualRowsCount;
			Manager.ColumnsCount = Manager.ActualColumnsCount;
		}

		private void CreateCells(int rowOffset, int colOffset, TemplateSection section)
		{
			_lastAddedCells.Clear();
			if (Manager.RowsCount < (rowOffset + section.Height))
			{
				if (Manager.RowsCount < 5000)
					Manager.RowsCount = Manager.RowsCount * 2; // rowOffset + section.Height;
				else
					Manager.RowsCount = Manager.RowsCount + section.Height * 100;
			}
			if (Manager.ColumnsCount < (colOffset + section.Width))
				Manager.ColumnsCount = colOffset + section.Width;

			Manager.BeginUpdate();
			try
			{
				foreach (var cell in section.GetCells(_template.Manager))
				{
					var sectionLocation = section.TranslateToSectionOffset(cell);
					var newCell = Manager.CreateCell(rowOffset + sectionLocation.Row, colOffset + sectionLocation.Column, cell);
					newCell.Content?.ExecuteExpressions();
					_lastAddedCells.Add(newCell);
				}
				foreach (var rowIndex in section.GetAutoFitRows(_template.Manager))
				{
					if (rowIndex >= section.Top && rowIndex <= section.Bottom)
						Manager.SetAutoFitRow((rowIndex - section.Top) + rowOffset, true);
				}
				int index = 0;
				foreach (var height in section.GetRowsHeights(_template.Manager))
				{
					Manager.SetRowHeight(index + rowOffset, height);
					index++;
				}
				index = 0;
				foreach (var width in section.GetColumnWidths(_template.Manager))
				{
					Manager.SetColumnWidth(index + colOffset, width);
					index++;
				}
			}
			finally
			{
				Manager.EndUpdate();
			}
		}
	}
}
