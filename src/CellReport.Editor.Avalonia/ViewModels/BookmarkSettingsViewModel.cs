using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using CellReport.Core;
using CellReport.Core.BookMarks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CellReport.Editor.Avalonia.ViewModels;

/// <summary>
/// Настройки построения закладок в процессе формирования отчёта.
/// </summary>
public partial class BookmarkSettingsViewModel : ObservableObject
{
	private readonly MainWindowViewModel _owner;
	private CellManager? _manager;
	private bool _loading;
	private bool _writing;

	public BookmarkSettingsViewModel(MainWindowViewModel owner)
	{
		_owner = owner;
		Rules.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRules));
		Levels.CollectionChanged += (_, _) =>
		{
			OnPropertyChanged(nameof(HasLevels));
			AddLevelCommand.NotifyCanExecuteChanged();
		};
	}

	public ObservableCollection<BookmarkRuleEdit> Rules { get; } = new();

	public ObservableCollection<BookmarkLevelEdit> Levels { get; } = new();

	public ObservableCollection<string> Sections { get; } = new();

	public ObservableCollection<string> ParamNames { get; } = new();

	public IReadOnlyList<BookmarkDuplicateChoice> DuplicateChoices => BookmarkDuplicateChoice.All;

	public bool HasRules => Rules.Count > 0;

	public bool HasLevels => Levels.Count > 0;

	[ObservableProperty]
	private bool _enabled = true;

	[ObservableProperty]
	private bool _autoSections;

	[ObservableProperty]
	private string _titleParam = BookmarkSettings.DefaultTitleParam;

	[ObservableProperty]
	private string _titleFormat = BookmarkSettings.DefaultTitleFormat;

	[ObservableProperty]
	private bool _skipEmpty;

	[ObservableProperty]
	private string _maxDepthText = "0";

	[ObservableProperty]
	private bool _flushAtEnd = true;

	[ObservableProperty]
	private BookmarkDuplicateChoice _selectedDuplicate = BookmarkDuplicateChoice.All[0];

	public void Load(CellManager? manager)
	{
		_manager = manager;
		_loading = true;
		try
		{
			Sections.Clear();
			ParamNames.Clear();
			Rules.Clear();
			Levels.Clear();

			if (manager == null)
			{
				Enabled = true;
				AutoSections = false;
				TitleParam = BookmarkSettings.DefaultTitleParam;
				TitleFormat = BookmarkSettings.DefaultTitleFormat;
				SkipEmpty = false;
				MaxDepthText = "0";
				FlushAtEnd = true;
				SelectedDuplicate = BookmarkDuplicateChoice.All[0];
				return;
			}

			var settings = manager.BookmarkSettings ?? new BookmarkSettings();
			Enabled = settings.Enabled;
			AutoSections = settings.AutoSections;
			TitleParam = settings.TitleParam;
			TitleFormat = settings.TitleFormat;
			SkipEmpty = settings.SkipEmpty;
			MaxDepthText = settings.MaxDepth.ToString(CultureInfo.CurrentCulture);
			FlushAtEnd = settings.FlushAtEnd;
			SelectedDuplicate = BookmarkDuplicateChoice.Find(settings.Duplicates);

			foreach (var section in manager.GetSections())
			{
				var name = section.Name ?? string.Empty;
				if (name.Length > 0 && !Sections.Contains(name))
					Sections.Add(name);
			}

			foreach (var name in CollectParamNames(manager))
				ParamNames.Add(name);

			foreach (var pair in settings.GetLevels())
				Levels.Add(new BookmarkLevelEdit(this, RemoveLevel, pair.Key, pair.Value));

			LoadRules();
		}
		finally
		{
			_loading = false;
			AddLevelCommand.NotifyCanExecuteChanged();
		}
	}

	public void Commit(Action change)
	{
		if (_manager == null || _loading || _writing)
			return;

		_writing = true;
		try
		{
			_owner.CommitDocumentChange(change);
		}
		finally
		{
			_writing = false;
		}
	}

	private static IEnumerable<string> CollectParamNames(CellManager manager)
	{
		var names = new HashSet<string>(StringComparer.Ordinal);
		for (var row = 0; row < manager.RowsCount; row++)
		{
			for (var column = 0; column < manager.ColumnsCount; column++)
			{
				if (!manager.TryGetCell(row, column, out var cell) || cell?.Content == null)
					continue;

				foreach (var param in cell.Content.GetParams())
				{
					if (!string.IsNullOrEmpty(param.Name))
						names.Add(param.Name);
				}
			}
		}

		return names.OrderBy(item => item, StringComparer.CurrentCulture);
	}

	private void LoadRules()
	{
		Rules.Clear();
		if (_manager == null)
			return;

		foreach (var rule in _manager.GetBookMarks())
			AddRuleItems(rule, 0);
	}

	private void AddRuleItems(BookMark rule, int nesting)
	{
		Rules.Add(new BookmarkRuleEdit(this, rule, nesting));
		foreach (var child in rule.Child)
			AddRuleItems(child, nesting + 1);
	}

	[RelayCommand]
	private void AddRule()
	{
		if (_manager == null)
			return;

		var rule = new BookMark
		{
			SectionID = Sections.FirstOrDefault() ?? string.Empty,
			TextParamName = _manager.BookmarkSettings.TitleParam
		};

		Commit(() => _manager.AddBookMark(rule));
		LoadRules();
	}

	internal void RemoveRule(BookmarkRuleEdit edit)
	{
		if (_manager == null)
			return;

		Commit(() =>
		{
			var root = _manager.GetBookMarks().FirstOrDefault(item => ReferenceEquals(item, edit.Rule));
			if (root != null)
				_manager.RemoveBookMark(root);
			else
				RemoveFromChildren(_manager.GetBookMarks(), edit.Rule);
		});

		LoadRules();
	}

	private static bool RemoveFromChildren(IEnumerable<BookMark> items, BookMark target)
	{
		foreach (var item in items)
		{
			if (item.Child.Remove(target))
				return true;
			if (RemoveFromChildren(item.Child, target))
				return true;
		}

		return false;
	}

	[RelayCommand(CanExecute = nameof(CanAddLevel))]
	private void AddLevel()
	{
		if (_manager == null)
			return;

		var section = Sections.FirstOrDefault(item => !Levels.Any(level => string.Equals(level.Section, item, StringComparison.Ordinal)));
		if (section == null)
			return;

		Levels.Add(new BookmarkLevelEdit(this, RemoveLevel, section, 1));
		CommitLevels();
	}

	private bool CanAddLevel()
	{
		return Sections.Any(item => !Levels.Any(level => string.Equals(level.Section, item, StringComparison.Ordinal)));
	}

	private void RemoveLevel(BookmarkLevelEdit edit)
	{
		Levels.Remove(edit);
		CommitLevels();
	}

	internal void CommitLevels()
	{
		if (_manager == null || _loading || _writing)
			return;

		var snapshot = Levels.ToList();
		Commit(() =>
		{
			var settings = _manager.BookmarkSettings;
			settings.ClearLevels();
			foreach (var edit in snapshot)
			{
				int level;
				if (edit.TryGetLevel(out level))
					settings.SetLevel(edit.Section, level);
			}
		});
	}

	internal string GetLevelText(string? sectionName)
	{
		var level = _manager?.BookmarkSettings.GetLevel(sectionName ?? string.Empty) ?? 0;
		return level > 0 ? level.ToString(CultureInfo.CurrentCulture) : string.Empty;
	}

	internal void SetLevel(string? sectionName, int level)
	{
		if (_manager == null || string.IsNullOrEmpty(sectionName))
			return;

		var settings = _manager.BookmarkSettings;
		if (settings.GetLevel(sectionName) == level)
			return;

		var name = sectionName;
		Commit(() => _manager.BookmarkSettings.SetLevel(name, level));
	}

	internal void UpdateLevelRow(string? sectionName, int level)
	{
		if (string.IsNullOrEmpty(sectionName))
			return;

		var edit = Levels.FirstOrDefault(item => string.Equals(item.Section, sectionName, StringComparison.Ordinal));
		if (level <= 0)
		{
			if (edit != null)
				Levels.Remove(edit);
			return;
		}

		if (edit == null)
		{
			Levels.Add(new BookmarkLevelEdit(this, RemoveLevel, sectionName, level));
			return;
		}

		edit.SetLevelSilently(level);
	}

	partial void OnEnabledChanged(bool value) => WriteSetting(settings => settings.Enabled == value, settings => settings.Enabled = value);

	partial void OnAutoSectionsChanged(bool value) => WriteSetting(settings => settings.AutoSections == value, settings => settings.AutoSections = value);

	partial void OnSkipEmptyChanged(bool value) => WriteSetting(settings => settings.SkipEmpty == value, settings => settings.SkipEmpty = value);

	partial void OnFlushAtEndChanged(bool value) => WriteSetting(settings => settings.FlushAtEnd == value, settings => settings.FlushAtEnd = value);

	partial void OnTitleParamChanged(string value)
	{
		var normalized = value ?? string.Empty;
		WriteSetting(settings => settings.TitleParam == normalized, settings => settings.TitleParam = normalized);
	}

	partial void OnTitleFormatChanged(string value)
	{
		var normalized = value ?? string.Empty;
		WriteSetting(settings => settings.TitleFormat == normalized, settings => settings.TitleFormat = normalized);
	}

	partial void OnMaxDepthTextChanged(string value)
	{
		if (_manager == null)
			return;

		int depth;
		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out depth) || depth < 0)
			return;

		WriteSetting(settings => settings.MaxDepth == depth, settings => settings.MaxDepth = depth);
	}

	partial void OnSelectedDuplicateChanged(BookmarkDuplicateChoice value)
	{
		if (_manager == null)
			return;

		WriteSetting(settings => settings.Duplicates == value.Mode, settings => settings.Duplicates = value.Mode);
	}

	private void WriteSetting(Func<BookmarkSettings, bool> unchanged, Action<BookmarkSettings> assign)
	{
		if (_manager == null || _loading)
			return;

		var settings = _manager.BookmarkSettings;
		if (unchanged(settings))
			return;

		Commit(() => assign(_manager.BookmarkSettings));
	}
}

public sealed class BookmarkDuplicateChoice
{
	public static IReadOnlyList<BookmarkDuplicateChoice> All { get; } =
	[
		new(BookmarkDuplicateMode.Keep, "Оставлять все"),
		new(BookmarkDuplicateMode.Skip, "Пропускать повторяющиеся"),
		new(BookmarkDuplicateMode.MergeConsecutive, "Объединять одинаковые подряд")
	];

	public BookmarkDuplicateChoice(BookmarkDuplicateMode mode, string title)
	{
		Mode = mode;
		Title = title;
	}

	public BookmarkDuplicateMode Mode { get; }

	public string Title { get; }

	public static BookmarkDuplicateChoice Find(BookmarkDuplicateMode mode)
	{
		foreach (var choice in All)
		{
			if (choice.Mode == mode)
				return choice;
		}

		return All[0];
	}
}

/// <summary>
/// Одно правило закладки (узел шаблонного дерева BookMarks).
/// </summary>
public partial class BookmarkRuleEdit : ObservableObject
{
	private readonly BookmarkSettingsViewModel _owner;
	private readonly BookMark _rule;
	private bool _loading;

	public BookmarkRuleEdit(BookmarkSettingsViewModel owner, BookMark rule, int nestingLevel)
	{
		_owner = owner;
		_rule = rule;
		NestingLevel = nestingLevel;

		_loading = true;
		Section = rule.SectionID ?? string.Empty;
		Param = rule.TextParamName ?? string.Empty;
		RequiresValue = !string.IsNullOrEmpty(rule.VisibleParamName);
		LevelText = owner.GetLevelText(Section);
		_loading = false;

		RemoveCommand = new RelayCommand(() => owner.RemoveRule(this));
	}

	public BookMark Rule => _rule;

	public int NestingLevel { get; }

	public Thickness Indent => new Thickness(NestingLevel * 12, 0, 0, 0);

	public ObservableCollection<string> Sections => _owner.Sections;

	public ObservableCollection<string> ParamNames => _owner.ParamNames;

	public IRelayCommand RemoveCommand { get; }

	[ObservableProperty]
	private string _section = string.Empty;

	[ObservableProperty]
	private string _param = string.Empty;

	[ObservableProperty]
	private bool _requiresValue;

	[ObservableProperty]
	private string _levelText = string.Empty;

	partial void OnSectionChanged(string value)
	{
		if (_loading)
			return;

		var section = value ?? string.Empty;
		_owner.Commit(() => _rule.SectionID = section);
	}

	partial void OnParamChanged(string value)
	{
		if (_loading)
			return;

		var param = value ?? string.Empty;
		_owner.Commit(() =>
		{
			_rule.TextParamName = param;
			if (RequiresValue)
				_rule.VisibleParamName = param;
		});
	}

	partial void OnRequiresValueChanged(bool value)
	{
		if (_loading)
			return;

		var param = string.IsNullOrEmpty(Param) ? _owner.TitleParam : Param;
		_owner.Commit(() => _rule.VisibleParamName = value ? param : string.Empty);
	}

	partial void OnLevelTextChanged(string value)
	{
		if (_loading)
			return;

		int level;
		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out level) || level < 0)
			return;

		_owner.SetLevel(Section, level);
		_owner.UpdateLevelRow(Section, level);
	}
}

/// <summary>
/// Уровень секции в дереве закладок.
/// </summary>
public partial class BookmarkLevelEdit : ObservableObject
{
	private readonly BookmarkSettingsViewModel _owner;
	private readonly Action<BookmarkLevelEdit> _remove;
	private bool _loading;
	private bool _changingSilently;

	public BookmarkLevelEdit(BookmarkSettingsViewModel owner, Action<BookmarkLevelEdit> remove, string section, int level)
	{
		_owner = owner;
		_remove = remove;
		_loading = true;
		Section = section;
		LevelText = level.ToString(CultureInfo.CurrentCulture);
		_loading = false;

		RemoveCommand = new RelayCommand(() => _remove(this));
	}

	public ObservableCollection<string> Sections => _owner.Sections;

	public IRelayCommand RemoveCommand { get; }

	[ObservableProperty]
	private string _section = string.Empty;

	[ObservableProperty]
	private string _levelText = "1";

	public bool TryGetLevel(out int level)
	{
		return int.TryParse(LevelText, NumberStyles.Integer, CultureInfo.CurrentCulture, out level) && level > 0;
	}

	public void SetLevelSilently(int level)
	{
		_changingSilently = true;
		try
		{
			LevelText = level.ToString(CultureInfo.CurrentCulture);
		}
		finally
		{
			_changingSilently = false;
		}
	}

	partial void OnSectionChanged(string value)
	{
		if (_loading || _changingSilently)
			return;

		_owner.CommitLevels();
	}

	partial void OnLevelTextChanged(string value)
	{
		if (_loading || _changingSilently)
			return;

		_owner.CommitLevels();
	}
}
