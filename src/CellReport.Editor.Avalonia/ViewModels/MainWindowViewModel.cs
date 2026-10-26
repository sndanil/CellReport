using CellReport.Avalonia;
using CellReport.Avalonia.Services;
using CellReport.Avalonia.Views;
using CellReport.Avalonia.ViewModels;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.Avalonia.Editing;
using CellReport.Editor.Avalonia.Import;
using CellReport.Export;
using CellReport.Presenters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SkiaSharp;

namespace CellReport.Editor.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
	private readonly TemplateHistory _history = new();
	private CellManager? _manager;
	private BaseDisplayPresenter? _presenter;
	private Window? _window;
	private bool _applying;
	private bool _applyingCode;
	private bool _syncing;
	private int _editDepth;
	private bool _editDirty;
	private string? _filePath;
	private bool _documentModified;
	private bool _codeModified;
	private bool _suppressCodeTracking;
	private readonly DispatcherTimer _typingTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };

	public MainWindowViewModel()
	{
		Settings = new DocumentSettingsViewModel(this);
		Expressions = new ExpressionsViewModel(this);
		Inspector = new InspectorViewModel(this);
		Present.PresenterChanged += OnPresenterChanged;
		_typingTimer.Tick += (_, _) => FlushTypingHistory();
	}

	public PresentControlViewModel Present { get; } = new();

	public DocumentSettingsViewModel Settings { get; }

	public ExpressionsViewModel Expressions { get; }

	public InspectorViewModel Inspector { get; }

	public CellManager? Manager => _manager;

	public BaseDisplayPresenter? Presenter => _presenter;

	public Window? Window => _window;

	public bool IsCodeMode { get; private set; }

	public bool HasUnsavedChanges => _manager != null && (_documentModified || _codeModified);

	public Func<string?>? ReadCode { get; set; }

	public Func<bool>? CanUndoCode { get; set; }

	public Func<bool>? CanRedoCode { get; set; }

	public Action? UndoCode { get; set; }

	public Action? RedoCode { get; set; }

	public event EventHandler? CodeTextInvalidated;

	public ObservableCollection<SchemaError> CodeErrors { get; } = new();

	public event EventHandler? ExitRequested;

	public event EventHandler? ShowDesignerRequested;

	public event EventHandler? SettingsRequested;

	public event EventHandler<bool>? CodeSearchRequested;

	public event EventHandler? FindRowInCodeRequested;

	[ObservableProperty]
	private string _title = "Редактор отчетов";

	[ObservableProperty]
	private bool _invertReport;

	[ObservableProperty]
	private int _ribbonTabIndex;

	public bool IsHomeRibbon => RibbonTabIndex == 0;

	public bool IsCellsRibbon => RibbonTabIndex == 1;

	public bool IsContentRibbon => RibbonTabIndex == 2;

	partial void OnRibbonTabIndexChanged(int value)
	{
		OnPropertyChanged(nameof(IsHomeRibbon));
		OnPropertyChanged(nameof(IsCellsRibbon));
		OnPropertyChanged(nameof(IsContentRibbon));
	}

	[RelayCommand]
	private void SelectRibbonTab(string? index)
	{
		if (int.TryParse(index, out var tab))
			RibbonTabIndex = tab;
	}

	[ObservableProperty]
	private string _currentText = string.Empty;

	[ObservableProperty]
	private bool _isTextContent;

	[ObservableProperty]
	private bool _isTextableContent;

	[ObservableProperty]
	private string _fontFamily = string.Empty;

	[ObservableProperty]
	private string _fontSize = string.Empty;

	[ObservableProperty]
	private bool _fontBold;

	[ObservableProperty]
	private bool _fontItalic;

	[ObservableProperty]
	private bool _fontUnderline;

	[ObservableProperty]
	private bool _fontStrikeout;

	[ObservableProperty]
	private bool _autoFitFont;

	[ObservableProperty]
	private bool _alignLeft;

	[ObservableProperty]
	private bool _alignCenter;

	[ObservableProperty]
	private bool _alignRight;

	[ObservableProperty]
	private bool _alignTop;

	[ObservableProperty]
	private bool _alignMiddle;

	[ObservableProperty]
	private bool _alignBottom;

	public IReadOnlyList<string> Fonts { get; } = LoadFonts();

	public IReadOnlyList<string> FontSizes { get; } =
	[
		string.Empty, "7", "8", "9", "10", "11", "12", "14", "16", "18", "20", "22", "24", "28", "32", "36", "48", "72"
	];

	[ObservableProperty]
	private string _cellAddress = string.Empty;

	[ObservableProperty]
	private string _cellWidthText = string.Empty;

	[ObservableProperty]
	private string _cellHeightText = string.Empty;

	[ObservableProperty]
	private string _selectionStatus = string.Empty;

	public async Task InitializeAsync(string[] args, Window window)
	{
		_window = window;
		BaseExporter.RendererFactory = new AvaloniaRendererFactory();
		FileResolver.OnResolve += ResolveExternalFile;

		if (args.Length > 0 && File.Exists(args[0]))
		{
			try
			{
				OpenPath(args[0]);
				return;
			}
			catch (Exception ex)
			{
				await ShowErrorAsync("Не удалось открыть шаблон." + Environment.NewLine + ex.Message);
			}
		}

		CreateDocument();
	}

	[RelayCommand]
	private async Task Create()
	{
		if (!await ConfirmUnsavedChangesAsync())
			return;

		CreateDocument();
	}

	[RelayCommand]
	private async Task Open()
	{
		if (_window?.StorageProvider.CanOpen != true)
			return;

		var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Открыть шаблон",
			AllowMultiple = false,
			FileTypeFilter = new[]
			{
				new FilePickerFileType("CellReport шаблон")
				{
					Patterns = new[] { "*.tcr", "*.tcrp" }
				},
				new FilePickerFileType("Все файлы")
				{
					Patterns = new[] { "*.*" }
				}
			}
		});

		if (files.Count == 0)
			return;

		if (!await ConfirmUnsavedChangesAsync())
			return;

		try
		{
			OpenPath(files[0].Path.LocalPath);
		}
		catch (Exception ex)
		{
			await ShowErrorAsync("Не удалось открыть шаблон." + Environment.NewLine + ex.Message);
		}
	}

	[RelayCommand]
	private async Task Import()
	{
		if (_window?.StorageProvider.CanOpen != true)
			return;

		var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Импорт из Excel",
			AllowMultiple = false,
			FileTypeFilter = new[]
			{
				new FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } }
			}
		});

		if (files.Count == 0)
			return;

		if (!await ConfirmUnsavedChangesAsync())
			return;

		var source = files[0].Path.LocalPath;
		var manager = ExcelImporter.Import(source);
		if (manager == null)
		{
			await ShowErrorAsync("Не удалось импортировать шаблон, возможно файл занят другим приложением.");
			return;
		}

		ShowDesigner();
		var directory = Path.GetDirectoryName(source) ?? string.Empty;
		SetDocumentPath(Path.Combine(directory, Path.GetFileNameWithoutExtension(source) + ".tcr"));
		ApplyDocument(manager, resetHistory: true, CellLocation.None);
		MarkModified();
	}

	[RelayCommand]
	private async Task PrintPreview()
	{
		if (_manager == null || _window == null)
			return;

		await ReportPrintService.ShowPreviewAsync(_window, _manager);
		_presenter?.Update();
	}

	[RelayCommand]
	private async Task Print()
	{
		if (_manager == null || _window == null)
			return;

		await ReportPrintService.PrintAsync(_window, _manager, custom: true);
		_presenter?.Update();
	}

	[RelayCommand]
	private Task ExportExcel() => ExportAsync(ReportExportService.ExportExcelAsync);

	[RelayCommand]
	private Task ExportHtml() => ExportAsync(ReportExportService.ExportHtmlAsync);

	[RelayCommand]
	private Task ExportPdf() => ExportAsync(ReportExportService.ExportPdfAsync);

	[RelayCommand]
	private Task ExportImage() => ExportAsync(ReportExportService.ExportImageAsync);

	private async Task ExportAsync(Func<global::Avalonia.Visual, BaseDisplayPresenter, bool, Task> export)
	{
		if (_presenter == null || _window == null)
			return;

		FlushTypingHistory();
		await export(_window, _presenter, true);
		_presenter.Update();
	}

	private void ShowDesigner()
	{
		if (IsCodeMode)
		{
			IsCodeMode = false;
			NotifyMode();
		}

		ShowDesignerRequested?.Invoke(this, EventArgs.Empty);
	}

	[RelayCommand]
	private async Task Save()
	{
		await SaveDocumentAsync(saveAs: false);
	}

	[RelayCommand]
	private async Task SaveAs()
	{
		await SaveDocumentAsync(saveAs: true);
	}

	[RelayCommand(CanExecute = nameof(CanUndo))]
	private void Undo()
	{
		if (IsCodeMode)
		{
			UndoCode?.Invoke();
			NotifyHistory();
			return;
		}

		FlushTypingHistory();
		if (!_history.CanUndo || _manager == null)
			return;

		var cell = _presenter?.CurrentCell ?? CellLocation.None;
		ApplyDocument(_history.Undo(), resetHistory: false, cell);
		NotifyDocumentEdited();
	}

	[RelayCommand(CanExecute = nameof(CanRedo))]
	private void Redo()
	{
		if (IsCodeMode)
		{
			RedoCode?.Invoke();
			NotifyHistory();
			return;
		}

		FlushTypingHistory();
		if (!_history.CanRedo || _manager == null)
			return;

		var cell = _presenter?.CurrentCell ?? CellLocation.None;
		ApplyDocument(_history.Redo(), resetHistory: false, cell);
		NotifyDocumentEdited();
	}

	[RelayCommand]
	private void Exit()
	{
		ExitRequested?.Invoke(this, EventArgs.Empty);
	}

	private bool CanUndo() => IsCodeMode ? CanUndoCode?.Invoke() == true : _history.CanUndo;

	private bool CanRedo() => IsCodeMode ? CanRedoCode?.Invoke() == true : _history.CanRedo;

	public string? ExportCode()
	{
		if (_manager == null)
			return null;

		var stream = new MemoryStream();
		new CellTemplate(_manager).Save(stream, TemplateType.Xml);
		stream.Position = 0;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	public void EnterCodeMode()
	{
		if (IsCodeMode)
			return;

		IsCodeMode = true;
		NotifyMode();
	}

	public bool TryLeaveCodeMode(out string? error)
	{
		var xml = ReadCode?.Invoke() ?? string.Empty;
		var current = ExportCode();
		if (current == null)
		{
			error = "Шаблон не открыт.";
			return false;
		}

		if (!SameXml(xml, current))
		{
			if (!TryApplyXml(xml, resetHistory: false, out error))
				return false;
		}
		else
		{
			error = null;
			_codeModified = false;
			NotifyModified();
		}

		IsCodeMode = false;
		NotifyMode();
		return true;
	}

	private static bool SameXml(string left, string right)
	{
		if (string.Equals(left, right, StringComparison.Ordinal))
			return true;

		return string.Equals(NormalizeNewLines(left), NormalizeNewLines(right), StringComparison.Ordinal);
	}

	private static string NormalizeNewLines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

	private void NotifyMode()
	{
		Inspector.RefreshCommands();
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
		FindAndReplaceCommand.NotifyCanExecuteChanged();
		FindInCodeCommand.NotifyCanExecuteChanged();
		ShowSettingsCommand.NotifyCanExecuteChanged();
		NotifyFormatCommands();
		NotifyHistory();
	}

	[RelayCommand]
	private async Task Find()
	{
		if (_manager == null)
			return;

		if (IsCodeMode)
		{
			CodeSearchRequested?.Invoke(this, false);
			return;
		}

		if (_presenter == null || _window == null)
			return;

		var dialog = new FindDialog(_presenter);
		await dialog.ShowDialog(_window);
	}

	[RelayCommand(CanExecute = nameof(CanFindAndReplace))]
	private void FindAndReplace()
	{
		CodeSearchRequested?.Invoke(this, true);
	}

	private bool CanFindAndReplace() => IsCodeMode && _manager != null;

	[RelayCommand(CanExecute = nameof(CanFindInCode))]
	private void FindInCode()
	{
		FindRowInCodeRequested?.Invoke(this, EventArgs.Empty);
	}

	private bool CanFindInCode() => !IsCodeMode && _manager != null;

	private bool TryApplyXml(string xml, bool resetHistory, out string? error)
	{
		if (_manager == null)
		{
			error = "Шаблон не открыт.";
			return false;
		}

		try
		{
			var template = new CellTemplate();
			template.LoadFromXml(xml);
			CopyResources(_manager, template.Manager);
			var cell = _presenter?.CurrentCell ?? CellLocation.None;
			_applyingCode = true;
			try
			{
				ApplyDocument(template.Manager, resetHistory, cell);
				_codeModified = false;
				if (!resetHistory)
				{
					_history.Push(_manager);
					NotifyHistory();
				}

				MarkModified();
			}
			finally
			{
				_applyingCode = false;
			}

			error = null;
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static void CopyResources(CellManager from, CellManager to)
	{
		if (ReferenceEquals(from, to))
			return;

		foreach (var name in from.GetResourceNames())
		{
			var data = from.GetResource(name);
			if (data != null && data.Length > 0)
				to.AddResource(name, data);
		}
	}

	public void CommitDocumentChange(Action change)
	{
		BeginEdit();
		try
		{
			change();
			_editDirty = true;
		}
		finally
		{
			EndEdit();
		}

		Present.SyncFromPresenter();
	}

	public void BeginEdit()
	{
		if (_editDepth == 0)
		{
			_editDirty = false;
			_presenter?.BeginUpdate();
			_manager?.BeginUpdate();
		}

		_editDepth++;
	}

	public void EndEdit()
	{
		if (_editDepth == 0)
			return;

		_editDepth--;
		if (_editDepth > 0)
			return;

		_editDepth = 1;
		try
		{
			_manager?.EndUpdate();
			_presenter?.EndUpdate();
		}
		finally
		{
			_editDepth = 0;
		}

		if (_editDirty && _manager != null)
		{
			_typingTimer.Stop();
			_history.Push(_manager);
			NotifyDocumentEdited();
			NotifyHistory();
		}

		RefreshCellFields();
		_presenter?.Update();
	}

	private void CreateDocument()
	{
		SetDocumentPath(null);
		var manager = new CellManager();
		manager.AddExternalStyle(new ExternalStyle("file://DefaultStyles.xml"));
		ApplyDocument(manager, resetHistory: true, CellLocation.None);
		MarkSaved();
	}

	[RelayCommand(CanExecute = nameof(CanShowSettings))]
	private void ShowSettings()
	{
		SettingsRequested?.Invoke(this, EventArgs.Empty);
	}

	private bool CanShowSettings() => !IsCodeMode && _manager != null;

	private void OpenPath(string path)
	{
		SetDocumentPath(path);
		var template = new CellTemplate();
		template.Load(path);
		ApplyDocument(template.Manager, resetHistory: true, CellLocation.None);
		MarkSaved();
	}

	private async Task<bool> SaveDocumentAsync(bool saveAs)
	{
		if (_manager == null || _window == null)
			return false;

		FlushTypingHistory();

		if (IsCodeMode && !TryApplyXml(ReadCode?.Invoke() ?? string.Empty, resetHistory: false, out var codeError))
		{
			await ShowErrorAsync("Код шаблона не применён." + Environment.NewLine + codeError);
			return false;
		}

		var path = _filePath;
		if (saveAs || string.IsNullOrEmpty(path))
		{
			if (!_window.StorageProvider.CanSave)
				return false;

			var suggested = string.IsNullOrEmpty(path)
				? "Шаблон.tcr"
				: Path.GetFileName(path);
			var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				Title = saveAs ? "Сохранить шаблон как" : "Сохранить шаблон",
				SuggestedFileName = suggested,
				FileTypeChoices = new[]
				{
					new FilePickerFileType("CellReport шаблон") { Patterns = new[] { "*.tcr" } },
					new FilePickerFileType("CellReport шаблон упакованный") { Patterns = new[] { "*.tcrp" } }
				}
			});

			if (file == null)
				return false;

			path = file.Path.LocalPath;
			if (string.IsNullOrEmpty(Path.GetExtension(path)))
				path += ".tcr";
		}

		try
		{
			var type = path.EndsWith(".tcrp", StringComparison.OrdinalIgnoreCase)
				? TemplateType.Package
				: TemplateType.Xml;
			new CellTemplate(_manager).Save(path, type);
			SetDocumentPath(path);
			MarkSaved();
			return true;
		}
		catch (Exception ex)
		{
			await ShowErrorAsync("При сохранении произошла ошибка." + Environment.NewLine + ex.Message);
			return false;
		}
	}

	private void ApplyDocument(CellManager manager, bool resetHistory, CellLocation cell)
	{
		_typingTimer.Stop();
		_applying = true;
		if (_manager != null)
			_manager.PropertyChanged -= OnManagerPropertyChanged;

		_manager = manager;
		Present.Load(manager, designMode: true);

		if (_presenter != null && !cell.Equals(CellLocation.None))
			_presenter.CurrentCell = cell;

		_manager.PropertyChanged += OnManagerPropertyChanged;
		Settings.Load(_manager);
		Expressions.Load(_manager);
		RefreshCellFields();
		Inspector.ReloadDocument();
		_applying = false;

		if (resetHistory)
		{
			_history.Clear();
			_history.Push(_manager);
		}

		NotifyHistory();
		ShowSettingsCommand.NotifyCanExecuteChanged();
		if (IsCodeMode && !_applyingCode)
			CodeTextInvalidated?.Invoke(this, EventArgs.Empty);
	}

	private void OnPresenterChanged(object? sender, EventArgs e)
	{
		if (_presenter != null)
			_presenter.PropertyChanged -= OnPresenterPropertyChanged;

		_presenter = Present.Presenter;
		if (_presenter != null)
			_presenter.PropertyChanged += OnPresenterPropertyChanged;

		RefreshCellFields();
	}

	private void OnPresenterPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is "CurrentCell" or "EndSelection" or "StartSelection")
			RefreshCellFields();
	}

	private void OnManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (_applying || _manager == null || !CellManager.IsDesignMode)
			return;

		if (_editDepth > 0)
		{
			_editDirty = true;
			return;
		}

		Settings.ApplyExternalChange(sender, e.PropertyName);
		if (sender is DisplaySettings)
			Present.SyncFromPresenter();

		_typingTimer.Stop();
		_history.Push(_manager);
		NotifyDocumentEdited();
		NotifyHistory();
		RefreshCellFields();
		_presenter?.Update();
	}

	private void RefreshCellFields()
	{
		try
		{
			if (_manager == null || _presenter == null)
			{
				CellAddress = string.Empty;
				SelectionStatus = string.Empty;
				IsTextContent = false;
				_syncing = true;
				try
				{
					ClearFormat();
				}
				finally
				{
					_syncing = false;
				}

				return;
			}

			var location = _presenter.CurrentCell;
			if (location.Row < 0 || location.Column < 0
				|| location.Row >= _manager.RowsCount || location.Column >= _manager.ColumnsCount)
			{
				CellAddress = string.Empty;
				SelectionStatus = string.Empty;
				IsTextContent = false;
				_syncing = true;
				try
				{
					ClearFormat();
				}
				finally
				{
					_syncing = false;
				}

				return;
			}

			var content = GetTextContent(location);

			_syncing = true;
			try
			{
				var text = content?.Text ?? string.Empty;
				if (CurrentText != text)
					CurrentText = text;

				IsTextContent = content != null;
				ReadFormat();
				CellAddress = "Ячейка " + location.Row + ", " + location.Column;
				CellWidthText = _manager.GetColumnWidth(location.Column).ToString("0.##", CultureInfo.CurrentCulture);
				CellHeightText = _manager.GetRowHeight(location.Row).ToString("0.##", CultureInfo.CurrentCulture);
				SelectionStatus = BuildSelectionStatus();
			}
			finally
			{
				_syncing = false;
			}
		}
		finally
		{
			Inspector.OnCellChanged();
			NotifyFormatCommands();
		}
	}

	private string BuildSelectionStatus()
	{
		if (_manager == null || _presenter == null || !_presenter.HaveSelection())
			return string.Empty;

		var start = _presenter.GetStartSelection();
		var end = _presenter.GetEndSelection();
		float width = 0;
		float height = 0;
		for (var column = start.Column; column <= end.Column; column++)
			width += _manager.GetColumnWidth(column);
		for (var row = start.Row; row <= end.Row; row++)
			height += _manager.GetRowHeight(row);

		return "(" + width.ToString("0.##", CultureInfo.CurrentCulture)
			+ " x "
			+ height.ToString("0.##", CultureInfo.CurrentCulture)
			+ " мм)";
	}

	private TextContent? GetTextContent(CellLocation location)
	{
		if (_manager == null)
			return null;

		if (location.Row < 0 || location.Column < 0
			|| location.Row >= _manager.RowsCount || location.Column >= _manager.ColumnsCount)
			return null;

		var cell = _manager.GetCell(location);
		if (cell?.Content is TextContent text)
			return text;

		if (cell?.Content is CollectionContent collection)
			return collection.GetContents().OfType<TextContent>().FirstOrDefault();

		return null;
	}

	private TextBasedContent? GetTextBasedContent()
	{
		if (_manager == null || _presenter == null)
			return null;

		var location = _presenter.CurrentCell;
		if (location.Row < 0 || location.Column < 0
			|| location.Row >= _manager.RowsCount || location.Column >= _manager.ColumnsCount)
			return null;

		var content = _manager.GetCell(location)?.Content;
		if (content is CollectionContent collection)
			content = collection.GetContents().FirstOrDefault(item => item is TextBasedContent);

		return content as TextBasedContent;
	}

	private void ReadFormat()
	{
		var content = GetTextBasedContent();
		var style = content?.GetStyle() as TextContentStyle;
		IsTextableContent = style != null;
		if (content == null || style == null)
		{
			ClearFormat();
			return;
		}

		FontFamily = ReadParam(content, "Style.Font.Family") ?? style.Font.Family;
		FontSize = ReadParam(content, "Style.Font.Size") ?? Convert.ToInt32(style.Font.Size).ToString(CultureInfo.InvariantCulture);
		FontBold = HasFontStyle(content, style, nameof(FontStyle.Bold), FontStyle.Bold);
		FontItalic = HasFontStyle(content, style, nameof(FontStyle.Italic), FontStyle.Italic);
		FontUnderline = HasFontStyle(content, style, nameof(FontStyle.Underline), FontStyle.Underline);
		FontStrikeout = HasFontStyle(content, style, nameof(FontStyle.Strikeout), FontStyle.Strikeout);
		AutoFitFont = content is TextContent text && text.AutoSizeFont;
		AlignLeft = HasAlignment(content, "Style.HAlignment", style.HAlignment, HorizontalAlignment.Left);
		AlignCenter = HasAlignment(content, "Style.HAlignment", style.HAlignment, HorizontalAlignment.Center);
		AlignRight = HasAlignment(content, "Style.HAlignment", style.HAlignment, HorizontalAlignment.Right);
		AlignTop = HasAlignment(content, "Style.VAlignment", style.VAlignment, VerticalAlignment.Top);
		AlignMiddle = HasAlignment(content, "Style.VAlignment", style.VAlignment, VerticalAlignment.Middle);
		AlignBottom = HasAlignment(content, "Style.VAlignment", style.VAlignment, VerticalAlignment.Bottom);
	}

	private void ClearFormat()
	{
		IsTextableContent = false;
		FontFamily = string.Empty;
		FontSize = string.Empty;
		FontBold = false;
		FontItalic = false;
		FontUnderline = false;
		FontStrikeout = false;
		AutoFitFont = false;
		AlignLeft = false;
		AlignCenter = false;
		AlignRight = false;
		AlignTop = false;
		AlignMiddle = false;
		AlignBottom = false;
	}

	private static string? ReadParam(TextBasedContent content, string target)
	{
		var param = content.GetParams().FirstOrDefault(item => item.Target == target);
		return param?.Value as string;
	}

	private static bool HasFontStyle(TextBasedContent content, TextContentStyle style, string name, FontStyle part)
	{
		var param = ReadParam(content, "Style.Font.Style");
		if (param != null)
			return param.Contains(name);

		return style.Font.Style.HasFlag(part);
	}

	private static bool HasAlignment<T>(TextBasedContent content, string target, T styleValue, T requested)
		where T : struct
	{
		var param = ReadParam(content, target);
		if (param != null && Enum.TryParse(param, out T parsed))
			return parsed.Equals(requested);

		return styleValue.Equals(requested);
	}

	partial void OnFontFamilyChanged(string value)
	{
		if (_syncing)
			return;

		SetStyleParam("Style.Font.Family", "Шрифт", value, style => style.Font.Family);
	}

	partial void OnFontSizeChanged(string value)
	{
		if (_syncing)
			return;

		SetStyleParam("Style.Font.Size", "Шрифт.Размер", value, style => Convert.ToInt32(style.Font.Size).ToString(CultureInfo.InvariantCulture));
	}

	partial void OnFontBoldChanged(bool value) => SetFontStyle(value, nameof(FontStyle.Bold), FontStyle.Bold);

	partial void OnFontItalicChanged(bool value) => SetFontStyle(value, nameof(FontStyle.Italic), FontStyle.Italic);

	partial void OnFontUnderlineChanged(bool value) => SetFontStyle(value, nameof(FontStyle.Underline), FontStyle.Underline);

	partial void OnFontStrikeoutChanged(bool value) => SetFontStyle(value, nameof(FontStyle.Strikeout), FontStyle.Strikeout);

	partial void OnAutoFitFontChanged(bool value)
	{
		if (_syncing || _presenter == null)
			return;

		var content = GetTextContent(_presenter.CurrentCell);
		if (content == null || content.AutoSizeFont == value)
			return;

		EditContent(() => content.AutoSizeFont = value);
	}

	partial void OnAlignLeftChanged(bool value) => SetHorizontalAlignment(HorizontalAlignment.Left, value);

	partial void OnAlignCenterChanged(bool value) => SetHorizontalAlignment(HorizontalAlignment.Center, value);

	partial void OnAlignRightChanged(bool value) => SetHorizontalAlignment(HorizontalAlignment.Right, value);

	partial void OnAlignTopChanged(bool value) => SetVerticalAlignment(VerticalAlignment.Top, value);

	partial void OnAlignMiddleChanged(bool value) => SetVerticalAlignment(VerticalAlignment.Middle, value);

	partial void OnAlignBottomChanged(bool value) => SetVerticalAlignment(VerticalAlignment.Bottom, value);

	private void SetHorizontalAlignment(HorizontalAlignment alignment, bool value)
	{
		if (_syncing)
			return;

		SetStyleParam("Style.HAlignment", "Гор.Выравнивание", value ? alignment.ToString() : null, style => style.HAlignment.ToString(), value);
	}

	private void SetVerticalAlignment(VerticalAlignment alignment, bool value)
	{
		if (_syncing)
			return;

		SetStyleParam("Style.VAlignment", "Верт.Выравнивание", value ? alignment.ToString() : null, style => style.VAlignment.ToString(), value);
	}

	private void SetStyleParam(string target, string name, string? value, Func<TextContentStyle, string> styleValue, bool? enabled = null)
	{
		EditContent(() =>
		{
			var content = GetTextBasedContent();
			if (content?.GetStyle() is not TextContentStyle style || _manager == null)
				return;

			var baseline = styleValue(style);
			var param = content.GetParams().FirstOrDefault(item => item.Target == target);
			var matchesStyle = enabled == null
				? string.IsNullOrEmpty(value) || value == baseline
				: (baseline == value) == enabled.Value;
			if (matchesStyle)
			{
				if (param != null)
				{
					content.RemoveParam(param);
					_editDirty = true;
				}

				return;
			}

			if (param != null)
				param.Value = value;
			else
			{
				content.AddParam(new Param(_manager, content)
				{
					Name = name,
					Target = target,
					Value = value
				});
			}

			_editDirty = true;
		});
	}

	private void SetFontStyle(bool value, string name, FontStyle part)
	{
		if (_syncing)
			return;

		EditContent(() =>
		{
			var content = GetTextBasedContent();
			if (content?.GetStyle() is not TextContentStyle style || _manager == null)
				return;

			var baseStyle = style.Font.Style;
			var param = content.GetParams().FirstOrDefault(item => item.Target == "Style.Font.Style");
			if (value == baseStyle.HasFlag(part))
			{
				if (param == null)
					return;

				var paramValue = param.Value as string;
				if (paramValue?.Contains(name) == true)
				{
					paramValue = paramValue.Replace("," + name, string.Empty).Trim();
					if ((Enum.TryParse(paramValue, out FontStyle parsed) && parsed == baseStyle)
						|| string.IsNullOrEmpty(paramValue)
						|| paramValue == nameof(FontStyle.Regular))
						content.RemoveParam(param);
					else
						param.Value = paramValue;
				}
				else
					param.Value = param.Value + "," + name;
			}
			else if (param != null)
			{
				if (value)
					param.Value = param.Value + "," + name;
				else
					param.Value = (param.Value as string)?.Replace("," + name, string.Empty).Trim();
			}
			else
			{
				var created = new Param(_manager, content)
				{
					Name = "Шрифт.Стиль",
					Target = "Style.Font.Style",
					Value = baseStyle == FontStyle.Regular
						? baseStyle.ToString()
						: nameof(FontStyle.Regular) + "," + baseStyle
				};
				var paramValue = created.Value as string;
				if (value)
					created.Value = paramValue + "," + name;
				else
					created.Value = paramValue?.Replace("," + name, string.Empty).Trim();

				content.AddParam(created);
			}

			_editDirty = true;
		});
	}

	private void EditContent(Action edit)
	{
		if (_manager == null)
			return;

		BeginEdit();
		try
		{
			edit();
		}
		finally
		{
			EndEdit();
		}
	}

	[RelayCommand(CanExecute = nameof(CanFormatText))]
	private void ToggleBold() => FontBold = !FontBold;

	[RelayCommand(CanExecute = nameof(CanFormatText))]
	private void ToggleItalic() => FontItalic = !FontItalic;

	[RelayCommand(CanExecute = nameof(CanFormatText))]
	private void ToggleUnderline() => FontUnderline = !FontUnderline;

	private bool CanFormatText() => !IsCodeMode && IsTextableContent;

	private void NotifyFormatCommands()
	{
		ToggleBoldCommand.NotifyCanExecuteChanged();
		ToggleItalicCommand.NotifyCanExecuteChanged();
		ToggleUnderlineCommand.NotifyCanExecuteChanged();
	}

	private static IReadOnlyList<string> LoadFonts()
	{
		try
		{
			return new[] { string.Empty }
				.Concat(SKFontManager.Default.FontFamilies.OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase))
				.ToArray();
		}
		catch (Exception)
		{
			return [string.Empty, "Arial", "Times New Roman", "Courier New"];
		}
	}

	partial void OnCurrentTextChanged(string value)
	{
		if (_syncing || _presenter == null)
			return;

		var content = GetTextContent(_presenter.CurrentCell);
		if (content == null || content.Text == value)
			return;

		content.Text = value;
		_presenter.Update();
		NotifyDocumentEdited();
		QueueTypingHistory();
	}

	partial void OnCellWidthTextChanged(string value)
	{
		ApplySize(value, isWidth: true);
	}

	partial void OnCellHeightTextChanged(string value)
	{
		ApplySize(value, isWidth: false);
	}

	private void ApplySize(string value, bool isWidth)
	{
		if (_syncing || _manager == null || _presenter == null)
			return;

		if (!TryParseSize(value, out var size))
			return;

		var location = _presenter.CurrentCell;
		if (location.Row < 0 || location.Column < 0
			|| location.Row >= _manager.RowsCount || location.Column >= _manager.ColumnsCount)
			return;

		var current = isWidth
			? _manager.GetColumnWidth(location.Column)
			: _manager.GetRowHeight(location.Row);
		if (Math.Abs(current - size) < 0.001f)
			return;

		if (isWidth)
			CommitDocumentChange(() => _manager.SetColumnWidth(location.Column, size));
		else
			CommitDocumentChange(() => _manager.SetRowHeight(location.Row, size));
	}

	private static bool TryParseSize(string? text, out float value)
	{
		if (float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
			return true;

		return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}

	private void SetDocumentPath(string? path)
	{
		_filePath = path;
		ExternalStyleSearch.DocumentDirectory = string.IsNullOrEmpty(path)
			? null
			: Path.GetDirectoryName(Path.GetFullPath(path));
	}

	private void UpdateTitle()
	{
		var name = string.IsNullOrEmpty(_filePath)
			? "Редактор отчетов"
			: "Редактор отчетов — " + _filePath;
		Title = (HasUnsavedChanges ? "*" : string.Empty) + name;
	}

	public void NotifyHistory()
	{
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
		NotifyModified();
	}

	private void MarkSaved()
	{
		_documentModified = false;
		_codeModified = false;
		NotifyModified();
	}

	private void MarkModified()
	{
		_documentModified = true;
		NotifyModified();
	}

	private void NotifyDocumentEdited()
	{
		if (_documentModified)
			return;

		_documentModified = true;
		NotifyModified();
	}

	private void QueueTypingHistory()
	{
		_typingTimer.Stop();
		_typingTimer.Start();
	}

	private void FlushTypingHistory()
	{
		if (!_typingTimer.IsEnabled || _manager == null)
			return;

		_typingTimer.Stop();
		_history.Push(_manager);
		NotifyDocumentEdited();
		NotifyHistory();
	}

	private void NotifyModified()
	{
		OnPropertyChanged(nameof(HasUnsavedChanges));
		UpdateTitle();
	}

	public void BeginCodeUpdate() => _suppressCodeTracking = true;

	public void EndCodeUpdate()
	{
		_suppressCodeTracking = false;
		_codeModified = false;
		NotifyModified();
	}

	public void NotifyCodeEdited()
	{
		if (_suppressCodeTracking || _codeModified)
			return;

		_codeModified = true;
		NotifyModified();
	}

	public Task<bool> ConfirmCloseAsync() => ConfirmUnsavedChangesAsync();

	private async Task<bool> ConfirmUnsavedChangesAsync()
	{
		if (!HasUnsavedChanges || _window == null)
			return true;

		var message = string.IsNullOrEmpty(_filePath)
			? "В шаблоне есть несохранённые изменения." + Environment.NewLine + Environment.NewLine + "Сохранить их?"
			: "В шаблоне «" + Path.GetFileName(_filePath) + "» есть несохранённые изменения."
				+ Environment.NewLine + Environment.NewLine + "Сохранить их?";

		var choice = await ConfirmDialog.AskSaveAsync(_window, "Несохранённые изменения", message);
		if (choice == SaveChoice.Save)
			return await SaveDocumentAsync(saveAs: false);

		return choice == SaveChoice.Discard;
	}

	private byte[] ResolveExternalFile(string fileName)
	{
		if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
			return null!;

		var directory = Path.GetDirectoryName(_filePath);
		if (directory == null)
			return null!;

		var path = Path.Combine(directory, fileName);
		if (!File.Exists(path))
			return null!;

		return File.ReadAllBytes(path);
	}

	private async Task ShowErrorAsync(string message)
	{
		if (_window != null)
			await ReportExportService.ShowMessageAsync(_window, message);
	}
}
