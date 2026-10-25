using CellReport.Core;
using ReportRange = CellReport.Core.Range;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace CellReport.Editor.Avalonia.ViewModels;

public partial class DocumentSettingsViewModel : ObservableObject
{
	private readonly MainWindowViewModel _owner;
	private CellManager? _manager;
	private bool _loading;
	private bool _writing;

	public DocumentSettingsViewModel(MainWindowViewModel owner)
	{
		_owner = owner;
	}

	public bool IsWriting => _writing;

	public ObservableCollection<RangeEdit> HeaderFixedRows { get; } = new();

	public ObservableCollection<RangeEdit> FooterFixedRows { get; } = new();

	public ObservableCollection<LocalizationEdit> Strings { get; } = new();

	[ObservableProperty]
	private string _documentName = string.Empty;

	[ObservableProperty]
	private string _culture = string.Empty;

	[ObservableProperty]
	private string _defaultCulture = string.Empty;

	[ObservableProperty]
	private string _defaultColumnsCountText = string.Empty;

	[ObservableProperty]
	private string _defaultRowsCountText = string.Empty;

	[ObservableProperty]
	private string _defaultColumnWidthText = string.Empty;

	[ObservableProperty]
	private string _defaultRowHeightText = string.Empty;

	[ObservableProperty]
	private string _defaultMarkerForShow = string.Empty;

	[ObservableProperty]
	private string _defaultMarkerForHide = string.Empty;

	[ObservableProperty]
	private bool _runExcelAfterExport;

	[ObservableProperty]
	private bool _landscape;

	[ObservableProperty]
	private string _copiesText = string.Empty;

	[ObservableProperty]
	private string _printScaleText = string.Empty;

	[ObservableProperty]
	private string _autoFitWidthPagesText = string.Empty;

	[ObservableProperty]
	private string _marginLeftText = string.Empty;

	[ObservableProperty]
	private string _marginRightText = string.Empty;

	[ObservableProperty]
	private string _marginTopText = string.Empty;

	[ObservableProperty]
	private string _marginBottomText = string.Empty;

	[ObservableProperty]
	private string _displayScaleText = string.Empty;

	[ObservableProperty]
	private string _frozenColumnsText = string.Empty;

	[ObservableProperty]
	private string _frozenRowsText = string.Empty;

	[ObservableProperty]
	private bool _showGridLines;

	[ObservableProperty]
	private bool _showDesignerGridLines;

	[ObservableProperty]
	private bool _showBookMarks;

	[ObservableProperty]
	private bool _showResizers;

	[ObservableProperty]
	private bool _showDesignerResizers;

	[ObservableProperty]
	private bool _showCurrentRows;

	[ObservableProperty]
	private string _gridColor = string.Empty;

	public void Load(CellManager? manager)
	{
		_manager = manager;
		_loading = true;
		try
		{
			if (manager == null)
			{
				Clear();
				return;
			}

			ReadProperties(manager);
			ReadPrint(manager);
			ReadDisplay(manager);
		}
		finally
		{
			_loading = false;
		}
	}

	public void ApplyExternalChange(object? sender, string? propertyName)
	{
		if (_writing || _loading || _manager == null)
			return;

		_loading = true;
		try
		{
			if (sender is DisplaySettings || propertyName is "DisplaySettings")
				ReadDisplay(_manager);
			else if (sender is PrintSettings or Margins || propertyName is "PrintSettings" or "Margins")
				ReadPrint(_manager);
			else if (sender is Properties or ExportSettings || propertyName is "Properties" or "ExportSettings" or "DocumentName")
				ReadProperties(_manager);
		}
		finally
		{
			_loading = false;
		}
	}

	private void Clear()
	{
		DocumentName = string.Empty;
		Culture = string.Empty;
		DefaultCulture = string.Empty;
		DefaultColumnsCountText = string.Empty;
		DefaultRowsCountText = string.Empty;
		DefaultColumnWidthText = string.Empty;
		DefaultRowHeightText = string.Empty;
		DefaultMarkerForShow = string.Empty;
		DefaultMarkerForHide = string.Empty;
		RunExcelAfterExport = false;
		Landscape = false;
		CopiesText = string.Empty;
		PrintScaleText = string.Empty;
		AutoFitWidthPagesText = string.Empty;
		MarginLeftText = string.Empty;
		MarginRightText = string.Empty;
		MarginTopText = string.Empty;
		MarginBottomText = string.Empty;
		DisplayScaleText = string.Empty;
		FrozenColumnsText = string.Empty;
		FrozenRowsText = string.Empty;
		ShowGridLines = false;
		ShowDesignerGridLines = false;
		ShowBookMarks = false;
		ShowResizers = false;
		ShowDesignerResizers = false;
		ShowCurrentRows = false;
		GridColor = string.Empty;
		HeaderFixedRows.Clear();
		FooterFixedRows.Clear();
		Strings.Clear();
	}

	private void ReadProperties(CellManager manager)
	{
		var properties = manager.Properties;
		DocumentName = properties.DocumentName ?? string.Empty;
		Culture = properties.Culture ?? string.Empty;
		DefaultCulture = properties.Localization?.DefaultCulture ?? string.Empty;
		DefaultColumnsCountText = properties.DefaultColumnsCount.ToString(CultureInfo.CurrentCulture);
		DefaultRowsCountText = properties.DefaultRowsCount.ToString(CultureInfo.CurrentCulture);
		DefaultColumnWidthText = Format(properties.DefaultColumnWidth);
		DefaultRowHeightText = Format(properties.DefaultRowHeight);
		DefaultMarkerForShow = properties.DefaultColumn.MarkerForShow ?? string.Empty;
		DefaultMarkerForHide = properties.DefaultColumn.MarkerForHide ?? string.Empty;
		RunExcelAfterExport = manager.ExportSettings.RunExcelAfterExport;
		Replace(Strings, properties.Localization?.GetStrings(), item => new LocalizationEdit(CommitLocalization, RemoveLocalization, item));
	}

	private void ReadPrint(CellManager manager)
	{
		var print = manager.PrintSettings;
		Landscape = print.Landscape;
		CopiesText = print.Copies.ToString(CultureInfo.CurrentCulture);
		PrintScaleText = Format(print.Scale);
		AutoFitWidthPagesText = print.AutoFitWidthPages.ToString(CultureInfo.CurrentCulture);
		var margins = print.Margins ?? new Margins(0);
		MarginLeftText = Format(margins.Left);
		MarginRightText = Format(margins.Right);
		MarginTopText = Format(margins.Top);
		MarginBottomText = Format(margins.Bottom);
		Replace(HeaderFixedRows, print.HeaderFixedRows, range => new RangeEdit(CommitHeaderFixedRows, RemoveHeaderFixedRow, range));
		Replace(FooterFixedRows, print.FooterFixedRows, range => new RangeEdit(CommitFooterFixedRows, RemoveFooterFixedRow, range));
	}

	private void ReadDisplay(CellManager manager)
	{
		var display = manager.DisplaySettings;
		DisplayScaleText = Format(display.Scale);
		FrozenColumnsText = display.FrozenColumns.ToString(CultureInfo.CurrentCulture);
		FrozenRowsText = display.FrozenRows.ToString(CultureInfo.CurrentCulture);
		ShowGridLines = display.ShowGridLines;
		ShowDesignerGridLines = display.ShowDesignerGridLines;
		ShowBookMarks = display.ShowBookMarks;
		ShowResizers = display.ShowResizers;
		ShowDesignerResizers = display.ShowDesignerResizers;
		ShowCurrentRows = display.ShowCurrentRows;
		GridColor = display.GridColor ?? string.Empty;
	}

	private void Write(Action change)
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

	partial void OnDocumentNameChanged(string value)
	{
		if (_loading || _manager == null || _manager.Properties.DocumentName == value)
			return;

		Write(() => _manager.Properties.DocumentName = value ?? string.Empty);
	}

	partial void OnCultureChanged(string value)
	{
		if (_loading || _manager == null || (_manager.Properties.Culture ?? string.Empty) == (value ?? string.Empty))
			return;

		Write(() => _manager.Properties.Culture = value ?? string.Empty);
	}

	partial void OnDefaultCultureChanged(string value) => CommitLocalization();

	partial void OnDefaultColumnsCountTextChanged(string value)
	{
		if (!TryInt(value, out var count) || _manager == null || _manager.Properties.DefaultColumnsCount == count)
			return;

		Write(() => _manager.Properties.DefaultColumnsCount = count);
	}

	partial void OnDefaultRowsCountTextChanged(string value)
	{
		if (!TryInt(value, out var count) || _manager == null || _manager.Properties.DefaultRowsCount == count)
			return;

		Write(() => _manager.Properties.DefaultRowsCount = count);
	}

	partial void OnDefaultColumnWidthTextChanged(string value)
	{
		if (!TryFloat(value, out var width) || _manager == null || Same(_manager.Properties.DefaultColumnWidth, width))
			return;

		Write(() => _manager.Properties.DefaultColumnWidth = width);
	}

	partial void OnDefaultRowHeightTextChanged(string value)
	{
		if (!TryFloat(value, out var height) || _manager == null || Same(_manager.Properties.DefaultRowHeight, height))
			return;

		Write(() => _manager.Properties.DefaultRowHeight = height);
	}

	partial void OnDefaultMarkerForShowChanged(string value) => WriteDefaultMarker(show: value, hide: null);

	partial void OnDefaultMarkerForHideChanged(string value) => WriteDefaultMarker(show: null, hide: value);

	private void WriteDefaultMarker(string? show, string? hide)
	{
		if (_loading || _manager == null)
			return;

		var column = _manager.Properties.DefaultColumn;
		var nextShow = show ?? column.MarkerForShow;
		var nextHide = hide ?? column.MarkerForHide;
		if ((column.MarkerForShow ?? string.Empty) == (nextShow ?? string.Empty)
			&& (column.MarkerForHide ?? string.Empty) == (nextHide ?? string.Empty))
			return;

		Write(() =>
		{
			var updated = _manager.Properties.DefaultColumn;
			updated.MarkerForShow = nextShow;
			updated.MarkerForHide = nextHide;
			_manager.Properties.DefaultColumn = updated;
		});
	}

	partial void OnRunExcelAfterExportChanged(bool value)
	{
		if (_loading || _manager == null || _manager.ExportSettings.RunExcelAfterExport == value)
			return;

		Write(() => _manager.ExportSettings.RunExcelAfterExport = value);
	}

	partial void OnLandscapeChanged(bool value)
	{
		if (_loading || _manager == null || _manager.PrintSettings.Landscape == value)
			return;

		Write(() => _manager.PrintSettings.Landscape = value);
	}

	partial void OnCopiesTextChanged(string value)
	{
		if (!short.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var copies)
			&& !short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out copies))
			return;
		if (copies < 1 || _manager == null || _manager.PrintSettings.Copies == copies)
			return;

		Write(() => _manager.PrintSettings.Copies = copies);
	}

	partial void OnPrintScaleTextChanged(string value)
	{
		if (!TryFloat(value, out var scale) || scale <= 0 || _manager == null || Same(_manager.PrintSettings.Scale, scale))
			return;

		Write(() => _manager.PrintSettings.Scale = scale);
	}

	partial void OnAutoFitWidthPagesTextChanged(string value)
	{
		if (!TryInt(value, out var pages) || pages < 0 || _manager == null || _manager.PrintSettings.AutoFitWidthPages == pages)
			return;

		Write(() => _manager.PrintSettings.AutoFitWidthPages = pages);
	}

	partial void OnMarginLeftTextChanged(string value) => WriteMargin(value, (margins, margin) => margins.Left = margin, margins => margins.Left);

	partial void OnMarginRightTextChanged(string value) => WriteMargin(value, (margins, margin) => margins.Right = margin, margins => margins.Right);

	partial void OnMarginTopTextChanged(string value) => WriteMargin(value, (margins, margin) => margins.Top = margin, margins => margins.Top);

	partial void OnMarginBottomTextChanged(string value) => WriteMargin(value, (margins, margin) => margins.Bottom = margin, margins => margins.Bottom);

	private void WriteMargin(string value, Action<Margins, float> assign, Func<Margins, float> read)
	{
		if (!TryFloat(value, out var margin) || _manager == null)
			return;

		var margins = _manager.PrintSettings.Margins ?? new Margins(0);
		if (Same(read(margins), margin))
			return;

		Write(() =>
		{
			if (_manager.PrintSettings.Margins == null)
				_manager.PrintSettings.Margins = new Margins(0);
			assign(_manager.PrintSettings.Margins, margin);
		});
	}

	partial void OnDisplayScaleTextChanged(string value)
	{
		if (!TryFloat(value, out var scale) || scale <= 0 || _manager == null || Same(_manager.DisplaySettings.Scale, scale))
			return;

		Write(() => _manager.DisplaySettings.Scale = scale);
	}

	partial void OnFrozenColumnsTextChanged(string value)
	{
		if (!TryInt(value, out var count) || count < 0 || _manager == null || _manager.DisplaySettings.FrozenColumns == count)
			return;

		Write(() => _manager.DisplaySettings.FrozenColumns = count);
	}

	partial void OnFrozenRowsTextChanged(string value)
	{
		if (!TryInt(value, out var count) || count < 0 || _manager == null || _manager.DisplaySettings.FrozenRows == count)
			return;

		Write(() => _manager.DisplaySettings.FrozenRows = count);
	}

	partial void OnShowGridLinesChanged(bool value) => WriteDisplay(display => display.ShowGridLines == value, display => display.ShowGridLines = value);

	partial void OnShowDesignerGridLinesChanged(bool value) => WriteDisplay(display => display.ShowDesignerGridLines == value, display => display.ShowDesignerGridLines = value);

	partial void OnShowBookMarksChanged(bool value) => WriteDisplay(display => display.ShowBookMarks == value, display => display.ShowBookMarks = value);

	partial void OnShowResizersChanged(bool value) => WriteDisplay(display => display.ShowResizers == value, display => display.ShowResizers = value);

	partial void OnShowDesignerResizersChanged(bool value) => WriteDisplay(display => display.ShowDesignerResizers == value, display => display.ShowDesignerResizers = value);

	partial void OnShowCurrentRowsChanged(bool value) => WriteDisplay(display => display.ShowCurrentRows == value, display => display.ShowCurrentRows = value);

	partial void OnGridColorChanged(string value)
	{
		if (_loading || _manager == null || (_manager.DisplaySettings.GridColor ?? string.Empty) == (value ?? string.Empty))
			return;

		Write(() => _manager.DisplaySettings.GridColor = value ?? string.Empty);
	}

	private void WriteDisplay(Func<DisplaySettings, bool> unchanged, Action<DisplaySettings> assign)
	{
		if (_loading || _manager == null || unchanged(_manager.DisplaySettings))
			return;

		Write(() => assign(_manager.DisplaySettings));
	}

	[RelayCommand]
	private void AddHeaderFixedRow() => AddRange(HeaderFixedRows, CommitHeaderFixedRows, RemoveHeaderFixedRow);

	[RelayCommand]
	private void AddFooterFixedRow() => AddRange(FooterFixedRows, CommitFooterFixedRows, RemoveFooterFixedRow);

	[RelayCommand]
	private void AddLocalization()
	{
		if (_manager == null)
			return;

		Strings.Add(new LocalizationEdit(CommitLocalization, RemoveLocalization, null));
		CommitLocalization();
	}

	private void AddRange(ObservableCollection<RangeEdit> target, Action commit, Action<RangeEdit> remove)
	{
		if (_manager == null)
			return;

		target.Add(new RangeEdit(commit, remove, null));
		commit();
	}

	private void RemoveHeaderFixedRow(RangeEdit edit) => Remove(HeaderFixedRows, edit, CommitHeaderFixedRows);

	private void RemoveFooterFixedRow(RangeEdit edit) => Remove(FooterFixedRows, edit, CommitFooterFixedRows);

	private void RemoveLocalization(LocalizationEdit edit) => Remove(Strings, edit, CommitLocalization);

	private void Remove<T>(ObservableCollection<T> target, T edit, Action commit)
	{
		if (_loading || !target.Remove(edit))
			return;

		commit();
	}

	private void CommitHeaderFixedRows() => CommitRanges(print => print.HeaderFixedRows, HeaderFixedRows);

	private void CommitFooterFixedRows() => CommitRanges(print => print.FooterFixedRows, FooterFixedRows);

	private void CommitRanges(Func<PrintSettings, IList<ReportRange>> list, ObservableCollection<RangeEdit> source)
	{
		if (_loading || _writing || _manager == null)
			return;

		Write(() =>
		{
			var target = list(_manager.PrintSettings);
			target.Clear();
			foreach (var edit in source)
				target.Add(new ReportRange(edit.From, edit.To));
		});
	}

	private void CommitLocalization()
	{
		if (_loading || _writing || _manager == null)
			return;

		Write(() =>
		{
			var items = new List<TemplateString>();
			foreach (var row in Strings)
			{
				items.Add(new TemplateString
				{
					Culture = row.Culture,
					ID = row.Id,
					Value = row.Value
				});
			}

			var localization = _manager.Properties.Localization ?? new TemplateStrings();
			localization.ReplaceAll(DefaultCulture, items);
			_manager.Properties.Localization = localization;
		});
	}

	private static void Replace<TSource, TEdit>(ObservableCollection<TEdit> target, IEnumerable<TSource>? source, Func<TSource, TEdit> create)
	{
		target.Clear();
		if (source == null)
			return;

		foreach (var item in source)
			target.Add(create(item));
	}

	private static string Format(float value) => value.ToString("0.##", CultureInfo.CurrentCulture);

	private static bool Same(float left, float right) => Math.Abs(left - right) < 0.001f;

	private static bool TryInt(string? text, out int value)
	{
		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
			return true;

		return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
	}

	private static bool TryFloat(string? text, out float value)
	{
		if (float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
			return true;

		return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
	}
}


public partial class RangeEdit : ObservableObject
{
	private readonly Action _changed;
	private readonly Action<RangeEdit> _remove;
	private bool _loading;

	public RangeEdit(Action changed, Action<RangeEdit> remove, ReportRange? range)
	{
		_changed = changed;
		_remove = remove;
		RemoveCommand = new RelayCommand(() => _remove(this));
		_loading = true;
		From = range?.From ?? 0;
		To = range?.To ?? 0;
		FromText = From.ToString(CultureInfo.CurrentCulture);
		ToText = To.ToString(CultureInfo.CurrentCulture);
		_loading = false;
	}

	public IRelayCommand RemoveCommand { get; }

	public int From { get; private set; }

	public int To { get; private set; }

	[ObservableProperty]
	private string _fromText = "0";

	[ObservableProperty]
	private string _toText = "0";

	partial void OnFromTextChanged(string value)
	{
		if (_loading || !TryInt(value, out var from) || From == from)
			return;

		From = from;
		_changed();
	}

	partial void OnToTextChanged(string value)
	{
		if (_loading || !TryInt(value, out var to) || To == to)
			return;

		To = to;
		_changed();
	}

	private static bool TryInt(string? text, out int value)
	{
		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
			return true;

		return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
	}
}

public partial class LocalizationEdit : ObservableObject
{
	private readonly Action _changed;
	private readonly Action<LocalizationEdit> _remove;
	private bool _loading;

	public LocalizationEdit(Action changed, Action<LocalizationEdit> remove, TemplateString? item)
	{
		_changed = changed;
		_remove = remove;
		RemoveCommand = new RelayCommand(() => _remove(this));
		_loading = true;
		Culture = item?.Culture ?? string.Empty;
		Id = item?.ID ?? string.Empty;
		Value = item?.Value ?? string.Empty;
		_loading = false;
	}

	public IRelayCommand RemoveCommand { get; }

	[ObservableProperty]
	private string _culture = string.Empty;

	[ObservableProperty]
	private string _id = string.Empty;

	[ObservableProperty]
	private string _value = string.Empty;

	partial void OnCultureChanged(string value) => Commit();

	partial void OnIdChanged(string value) => Commit();

	partial void OnValueChanged(string value) => Commit();

	private void Commit()
	{
		if (!_loading)
			_changed();
	}
}
