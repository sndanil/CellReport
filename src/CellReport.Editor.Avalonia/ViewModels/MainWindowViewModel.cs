using CellReport.Avalonia;
using CellReport.Avalonia.Services;
using CellReport.Avalonia.ViewModels;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.Avalonia.Editing;
using CellReport.Export;
using CellReport.Presenters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace CellReport.Editor.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
	private readonly TemplateHistory _history = new();
	private CellManager? _manager;
	private BaseDisplayPresenter? _presenter;
	private Window? _window;
	private bool _applying;
	private bool _syncing;
	private int _editDepth;
	private bool _editDirty;
	private string? _filePath;

	public MainWindowViewModel()
	{
		Inspector = new InspectorViewModel(this);
		Present.PresenterChanged += OnPresenterChanged;
	}

	public PresentControlViewModel Present { get; } = new();

	public InspectorViewModel Inspector { get; }

	public CellManager? Manager => _manager;

	public BaseDisplayPresenter? Presenter => _presenter;

	public Window? Window => _window;

	public event EventHandler? ExitRequested;

	[ObservableProperty]
	private string _title = "Редактор отчетов";

	[ObservableProperty]
	private string _currentText = string.Empty;

	[ObservableProperty]
	private bool _isTextContent;

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
	private void Create()
	{
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
		if (!_history.CanUndo || _manager == null)
			return;

		var cell = _presenter?.CurrentCell ?? CellLocation.None;
		ApplyDocument(_history.Undo(), resetHistory: false, cell);
	}

	[RelayCommand(CanExecute = nameof(CanRedo))]
	private void Redo()
	{
		if (!_history.CanRedo || _manager == null)
			return;

		var cell = _presenter?.CurrentCell ?? CellLocation.None;
		ApplyDocument(_history.Redo(), resetHistory: false, cell);
	}

	[RelayCommand]
	private void Exit()
	{
		ExitRequested?.Invoke(this, EventArgs.Empty);
	}

	private bool CanUndo() => _history.CanUndo;

	private bool CanRedo() => _history.CanRedo;

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
			_history.Push(_manager);
			NotifyHistory();
		}

		RefreshCellFields();
		_presenter?.Update();
	}

	private void CreateDocument()
	{
		var manager = new CellManager();
		manager.AddExternalStyle(new ExternalStyle("file://DefaultStyles.xml"));
		_filePath = null;
		ApplyDocument(manager, resetHistory: true, CellLocation.None);
	}

	private void OpenPath(string path)
	{
		var template = new CellTemplate();
		template.Load(path);
		_filePath = path;
		ApplyDocument(template.Manager, resetHistory: true, CellLocation.None);
	}

	private async Task SaveDocumentAsync(bool saveAs)
	{
		if (_manager == null || _window == null)
			return;

		var path = _filePath;
		if (saveAs || string.IsNullOrEmpty(path))
		{
			if (!_window.StorageProvider.CanSave)
				return;

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
				return;

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
			_filePath = path;
			UpdateTitle();
		}
		catch (Exception ex)
		{
			await ShowErrorAsync("При сохранении произошла ошибка." + Environment.NewLine + ex.Message);
		}
	}

	private void ApplyDocument(CellManager manager, bool resetHistory, CellLocation cell)
	{
		_applying = true;
		if (_manager != null)
			_manager.PropertyChanged -= OnManagerPropertyChanged;

		_manager = manager;
		Present.Load(manager, designMode: true);

		if (_presenter != null && !cell.Equals(CellLocation.None))
			_presenter.CurrentCell = cell;

		_manager.PropertyChanged += OnManagerPropertyChanged;
		RefreshCellFields();
		Inspector.ReloadDocument();
		UpdateTitle();
		_applying = false;

		if (resetHistory)
		{
			_history.Clear();
			_history.Push(_manager);
		}

		NotifyHistory();
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
		if (_applying || _manager == null)
			return;

		if (_editDepth > 0)
		{
			_editDirty = true;
			return;
		}

		_history.Push(_manager);
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
				return;
			}

			var location = _presenter.CurrentCell;
			if (location.Row < 0 || location.Column < 0
				|| location.Row >= _manager.RowsCount || location.Column >= _manager.ColumnsCount)
			{
				CellAddress = string.Empty;
				SelectionStatus = string.Empty;
				IsTextContent = false;
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

	partial void OnCurrentTextChanged(string value)
	{
		if (_syncing || _presenter == null)
			return;

		var content = GetTextContent(_presenter.CurrentCell);
		if (content == null || content.Text == value)
			return;

		content.Text = value;
		_presenter.Update();
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
			_manager.SetColumnWidth(location.Column, size);
		else
			_manager.SetRowHeight(location.Row, size);

		_presenter.Update();
	}

	private static bool TryParseSize(string? text, out float value)
	{
		if (float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
			return true;

		return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}

	private void UpdateTitle()
	{
		Title = string.IsNullOrEmpty(_filePath)
			? "Редактор отчетов"
			: "Редактор отчетов " + _filePath;
	}

	private void NotifyHistory()
	{
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
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
