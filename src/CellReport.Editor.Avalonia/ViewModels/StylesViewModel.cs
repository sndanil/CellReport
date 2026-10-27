using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.Avalonia.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CellReport.Editor.Avalonia.ViewModels;

/// <summary>
/// Управление стилями шаблона: список, создание, копирование, удаление и «сжатие»
/// по внешнему файлу стилей.
/// </summary>
public partial class StylesViewModel : ObservableObject
{
	private readonly MainWindowViewModel _owner;
	private CellManager? _manager;
	private bool _loading;
	private bool _writing;

	public StylesViewModel(MainWindowViewModel owner)
	{
		_owner = owner;
	}

	public ObservableCollection<StyleRow> Styles { get; } = new();

	public IReadOnlyList<StyleTypeChoice> StyleTypes => StyleTypeChoice.All;

	[ObservableProperty]
	private StyleRow? _selectedStyle;

	[ObservableProperty]
	private string _summary = string.Empty;

	[ObservableProperty]
	private string _message = string.Empty;

	[ObservableProperty]
	private bool _hasMessage;

	public void Load(CellManager? manager)
	{
		_manager = manager;
		Reload();
	}

	public void Reload()
	{
		var previous = SelectedStyle;
		_loading = true;
		try
		{
			Styles.Clear();
			Message = string.Empty;
			HasMessage = false;

			if (_manager == null)
			{
				Summary = "Шаблон не открыт";
				SelectedStyle = null;
				return;
			}

			var sources = GetSourceLabel();
			var template = 0;
			var external = 0;

			foreach (var style in _manager.GetTemplateStyles().OrderBy(item => item.ID, StringComparer.CurrentCulture))
			{
				Styles.Add(CreateRow(style, false, "шаблон"));
				template++;
			}

			foreach (var style in _manager.GetExternalStyles().OrderBy(item => item.ID, StringComparer.CurrentCulture))
			{
				Styles.Add(CreateRow(style, true, sources));
				external++;
			}

			Summary = "Стилей шаблона: " + template.ToString() + " · внешних: " + external.ToString();
			if (external > 0 && sources.Length > 0)
				Summary += " · " + sources;

			SelectedStyle = previous != null
				? Styles.FirstOrDefault(item => ReferenceEquals(item.Style, previous.Style)) ?? Styles.FirstOrDefault()
				: Styles.FirstOrDefault();
		}
		finally
		{
			_loading = false;
		}
	}

	private string GetSourceLabel()
	{
		if (_manager == null)
			return string.Empty;

		var names = _manager.GetExternalStyleSources()
			.Select(item => item.Uri ?? string.Empty)
			.Where(item => item.Length > 0)
			.Select(item => item.Replace("file://", string.Empty))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		return string.Join(", ", names);
	}

	private StyleRow CreateRow(BaseStyle style, bool isExternal, string source)
	{
		var usage = CountUsage(style);
		return new StyleRow(this, style, isExternal, source, usage);
	}

	private int CountUsage(BaseStyle style)
	{
		if (_manager == null)
			return 0;

		var count = 0;
		for (var row = 0; row < _manager.RowsCount; row++)
		{
			for (var column = 0; column < _manager.ColumnsCount; column++)
			{
				if (!_manager.TryGetCell(row, column, out var cell) || cell == null)
					continue;
				if (cell.Location.Row != row || cell.Location.Column != column)
					continue;

				if (style is CellStyle && string.Equals(cell.StyleID, style.ID, StringComparison.Ordinal))
					count++;

				count += CountContentUsage(cell.Content, style);
			}
		}

		var print = _manager.PrintSettings;
		if (print != null)
		{
			count += CountPagerUsage(print.Headers, style);
			count += CountPagerUsage(print.Footers, style);
		}

		return count;
	}

	private static int CountContentUsage(BaseContent? content, BaseStyle style)
	{
		if (content == null)
			return 0;

		if (content is CollectionContent collection)
			return collection.GetContents().Sum(item => CountContentUsage(item, style));

		if (!string.Equals(content.StyleID, style.ID, StringComparison.Ordinal))
			return 0;

		var current = content.GetStyle();
		return current != null && current.GetType() == style.GetType() ? 1 : 0;
	}

	private static int CountPagerUsage(IList<Pager>? pagers, BaseStyle style)
	{
		if (pagers == null)
			return 0;

		var count = 0;
		foreach (var pager in pagers)
		{
			count += CountContentUsage(pager.Left, style);
			count += CountContentUsage(pager.Center, style);
			count += CountContentUsage(pager.Right, style);
		}

		return count;
	}

	[RelayCommand]
	private async Task Create(string? typeKey)
	{
		var choice = StyleTypeChoice.Find(typeKey);
		if (choice == null || _manager == null)
			return;

		var style = choice.CreateDefault();
		var viewModel = new StyleEditorViewModel(_owner, style, isNew: true);
		if (await ShowEditorAsync(viewModel))
			RefreshAfterChange();
	}

	internal async Task EditAsync(StyleRow row)
	{
		if (row.IsExternal || _manager == null)
			return;

		var viewModel = new StyleEditorViewModel(_owner, row.Style, isNew: false);
		if (await ShowEditorAsync(viewModel))
			RefreshAfterChange();
	}

	internal void Copy(StyleRow row)
	{
		var manager = _manager;
		if (manager == null)
			return;

		var copy = BaseStyle.Parse(row.Style.Serialize());
		if (copy == null)
			return;

		copy.ID = NextCopyId(row.Style, manager);
		Commit(() => manager.AddStyle(copy));
		RefreshAfterChange();

		SelectedStyle = Styles.FirstOrDefault(item => ReferenceEquals(item.Style, copy)) ?? SelectedStyle;
		Message = "Стиль скопирован: " + copy.ID;
		HasMessage = true;
	}

	internal void Delete(StyleRow row)
	{
		var manager = _manager;
		if (manager == null || row.IsExternal)
			return;

		if (manager.IsStyleUsed(row.Style))
		{
			Message = "Стиль «" + row.Id + "» используется и не может быть удалён";
			HasMessage = true;
			return;
		}

		Commit(() => manager.RemoveStyle(row.Style));
		RefreshAfterChange();
	}

	/// <summary>
	/// Удаляет стили шаблона, которые уже есть во внешнем файле. Если идентификаторы
	/// различаются, ссылки в ячейках перенаправляются на внешний стиль.
	/// </summary>
	[RelayCommand]
	private void Compress()
	{
		var manager = _manager;
		if (manager == null)
			return;

		if (!manager.GetExternalStyles().Any())
		{
			Message = "Внешние стили не подключены — сжимать нечего";
			HasMessage = true;
			return;
		}

		StyleCompressionResult result = new StyleCompressionResult();
		Commit(() => result = manager.CompressStyles());

		if (result.Removed == 0)
		{
			Message = "Стилей, совпадающих с внешними, нет";
			HasMessage = true;
			return;
		}

		RefreshAfterChange();
		Message = "Сжато стилей: " + result.Removed.ToString()
			+ (result.Remapped > 0
				? " · перенаправлено стилей: " + result.Remapped.ToString() + " · изменено ссылок: " + result.References.ToString()
				: string.Empty);
		HasMessage = true;
	}
	private void RefreshAfterChange()
	{
		Reload();
		_owner.Inspector.OnCellChanged();
	}

	private async Task<bool> ShowEditorAsync(StyleEditorViewModel viewModel)
	{
		var window = _owner.Window;
		if (window == null)
			return false;

		var dialog = new StyleEditorWindow { DataContext = viewModel };
		return await dialog.ShowDialog<bool>(window);
	}

	private void Commit(Action change)
	{
		if (_manager == null || _writing)
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

	private static string NextCopyId(BaseStyle style, CellManager manager)
	{
		var baseId = string.IsNullOrEmpty(style.ID) ? "стиль" : style.ID;
		var index = 0;
		while (true)
		{
			var suffix = index == 0 ? " (копия)" : " (копия " + index.ToString() + ")";
			var candidate = baseId + suffix;
			if (!manager.GetStyles().Any(item => item.GetType() == style.GetType() && string.Equals(item.ID, candidate, StringComparison.Ordinal)))
				return candidate;

			index++;
		}
	}

	internal static bool SupportsEditing(BaseStyle style)
	{
		return style is TextContentStyle || style is CellStyle || style is LineContentStyle;
	}
}

public sealed class StyleRow
{
	private readonly StylesViewModel _owner;

	public StyleRow(StylesViewModel owner, BaseStyle style, bool isExternal, string source, int usage)
	{
		_owner = owner;
		Style = style;
		IsExternal = isExternal;
		Id = style.ID ?? string.Empty;
		TypeTitle = TypeTitleOf(style);
		SourceTitle = isExternal ? "внешний · " + source : "шаблон";
		UsageTitle = usage == 0 ? "не используется" : "используется · " + usage.ToString();
		CanDelete = !isExternal && usage == 0;

		EditCommand = new RelayCommand(() => _ = _owner.EditAsync(this), () => CanEdit);
		CopyCommand = new RelayCommand(() => _owner.Copy(this));
		DeleteCommand = new RelayCommand(() => _owner.Delete(this), () => CanDelete);
	}

	public BaseStyle Style { get; }

	public string Id { get; }

	public string TypeTitle { get; }

	public string SourceTitle { get; }

	public string UsageTitle { get; }

	public string Details => TypeTitle + " · " + SourceTitle + " · " + UsageTitle;

	public bool IsExternal { get; }

	public bool CanEdit => !IsExternal && StylesViewModel.SupportsEditing(Style);

	public bool CanDelete { get; }

	public IRelayCommand EditCommand { get; }

	public IRelayCommand CopyCommand { get; }

	public IRelayCommand DeleteCommand { get; }

	private static string TypeTitleOf(BaseStyle style)
	{
		if (style is DecimalContentStyle)
			return "число";
		if (style is DateTimeContentStyle)
			return "дата и время";
		if (style is CellStyle)
			return "ячейка";
		if (style is LineContentStyle)
			return "линия";
		if (style is BarCodeContentStyle)
			return "штрих-код";
		return "текст";
	}
}

public sealed class StyleTypeChoice
{
	public static IReadOnlyList<StyleTypeChoice> All { get; } = new[]
	{
		new StyleTypeChoice("text", "Текст"),
		new StyleTypeChoice("number", "Число"),
		new StyleTypeChoice("date", "Дата и время"),
		new StyleTypeChoice("cell", "Ячейка"),
		new StyleTypeChoice("line", "Линия")
	};

	public StyleTypeChoice(string key, string title)
	{
		Key = key;
		Title = title;
	}

	public string Key { get; }

	public string Title { get; }

	public BaseStyle CreateDefault()
	{
		switch (Key)
		{
			case "number":
				return new DecimalContentStyle(HorizontalAlignment.Right, VerticalAlignment.Middle,
					new FontInfo("Arial", 10), "Black", "Red", 0, "#,##0.00", string.Empty, Direction.LeftToRight);
			case "date":
				return new DateTimeContentStyle(HorizontalAlignment.Center, VerticalAlignment.Middle,
					new FontInfo("Arial", 10), "Black", 0, "dd.MM.yyyy", string.Empty, Direction.LeftToRight);
			case "cell":
				return new CellStyle("Transparent");
			case "line":
				return new LineContentStyle("Black", 0.25f);
			default:
				return new TextContentStyle(HorizontalAlignment.Left, VerticalAlignment.Middle,
					new FontInfo("Arial", 10), "Black");
		}
	}

	public static StyleTypeChoice? Find(string? key)
	{
		return All.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal));
	}
}
