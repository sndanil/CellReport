using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;
using CellReport.Editor.Avalonia.Editing;
using CellReport.Presenters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CellReport.Editor.Avalonia.ViewModels;

public partial class InspectorViewModel : ObservableObject
{
	private readonly MainWindowViewModel _owner;
	private int _syncDepth;
	private bool _refreshing;

	public InspectorViewModel(MainWindowViewModel owner)
	{
		_owner = owner;
		foreach (var code in CultureCodes)
			Cultures.Add(code);
		foreach (var format in ReportFormatPresets.All)
			FormatPresets.Add(format);
	}

	private static readonly string[] CultureCodes = CultureInfo.GetCultures(CultureTypes.AllCultures)
		.Where(item => !string.IsNullOrEmpty(item.Name))
		.Select(item => item.Name)
		.Distinct(StringComparer.OrdinalIgnoreCase)
		.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
		.ToArray();

	public ObservableCollection<ContentRow> Contents { get; } = new();

	public ObservableCollection<Param> Params { get; } = new();

	public ObservableCollection<TemplateSection> Sections { get; } = new();

	public ObservableCollection<StyleChoice> CellStyles { get; } = new();

	public ObservableCollection<StyleChoice> TextStyles { get; } = new();

	public ObservableCollection<StyleChoice> DecimalStyles { get; } = new();

	public ObservableCollection<StyleChoice> DateTimeStyles { get; } = new();

	public Choice[] ParamTargets { get; } =
	{
		new("", "Не установлено"),
		new("Text", "Текст"),
		new("Value", "Значение"),
		new("Code", "Штрих-код"),
		new("Type", "Тип штрих-кода"),
		new("Uri", "Адрес изображения"),
		new("Style", "Стиль"),
		new("Style.Color", "Цвет"),
		new("Style.NegativeColor", "Цвет отрицательного числа"),
		new("Style.HAlignment", "Выравнивание по горизонтали"),
		new("Style.VAlignment", "Выравнивание по вертикали"),
		new("Style.Direction", "Направление"),
		new("Style.Angle", "Угол наклона"),
		new("Style.Font.Size", "Размер шрифта"),
		new("Style.Font.Family", "Шрифт"),
		new("Style.Font.Style", "Стиль шрифта"),
		new("Style.Format", "Форматирование"),
		new("Style.Culture", "Культура")
	};

	public Choice[] ParamTypes { get; } =
	{
		new("", "Не установлено"),
		new("Decimal", "Число"),
		new("DateTime", "Дата и время")
	};

	public Choice[] BarcodeTypes { get; } =
	{
		new("Auto", "Авто"),
		new("EAN13", "EAN-13"),
		new("EAN8", "EAN-8"),
		new("UPCA", "UPC-A"),
		new("UPCE", "UPC-E"),
		new("Code39", "Code 39"),
		new("Code128", "Code 128"),
		new("QR", "QR"),
		new("PDF417", "PDF417"),
		new("UCC12", "UCC-12")
	};

	public Choice[] BarcodeOrientations { get; } =
	{
		new("Horizontal", "Горизонтально"),
		new("Vertical", "Вертикально")
	};

	public Choice[] ActionTypes { get; } =
	{
		new("All", "Все"),
		new("MouseDblClick", "Двойной клик"),
		new("MouseLeftClick", "Один клик")
	};

	/// <summary>
	/// Доступные выражения для привязки параметра.
	/// </summary>
	public ObservableCollection<Choice> Expressions { get; } = new();

	/// <summary>
	/// Коды культур для выбора.
	/// </summary>
	public ObservableCollection<string> Cultures { get; } = new();

	/// <summary>
	/// Готовые форматы под текущий тип значения.
	/// </summary>
	public ObservableCollection<string> FormatPresets { get; } = new();

	[ObservableProperty]
	private ContentRow? _selectedContent;

	[ObservableProperty]
	private Param? _selectedParam;

	[ObservableProperty]
	private bool _hasSelectedParam;

	[ObservableProperty]
	private TemplateSection? _selectedSection;

	[ObservableProperty]
	private bool _hasSelectedSection;

	[ObservableProperty]
	private StyleChoice? _selectedCellStyle;

	[ObservableProperty]
	private StyleChoice? _selectedTextStyle;

	[ObservableProperty]
	private StyleChoice? _selectedDecimalStyle;

	[ObservableProperty]
	private StyleChoice? _selectedDateTimeStyle;

	[ObservableProperty]
	private bool _hasTextContent;

	[ObservableProperty]
	private bool _hasDecimalContent;

	[ObservableProperty]
	private bool _hasDateTimeContent;

	[ObservableProperty]
	private bool _hasBarCodeContent;

	[ObservableProperty]
	private bool _hasImageContent;

	[ObservableProperty]
	private bool _hasActionContent;

	[ObservableProperty]
	private string _barcodeCode = string.Empty;

	[ObservableProperty]
	private Choice? _selectedBarcodeType;

	[ObservableProperty]
	private Choice? _selectedBarcodeOrientation;

	[ObservableProperty]
	private string _imageUri = string.Empty;

	[ObservableProperty]
	private string _imageColSpanText = "0";

	[ObservableProperty]
	private string _imageRowSpanText = "0";

	[ObservableProperty]
	private Choice? _selectedActionType;

	[ObservableProperty]
	private bool _isSelectedBarCode;

	[ObservableProperty]
	private bool _isSelectedImage;

	[ObservableProperty]
	private bool _isSelectedAction;

	[ObservableProperty]
	private string _paramName = string.Empty;

	[ObservableProperty]
	private string _paramValue = string.Empty;

	[ObservableProperty]
	private Choice? _selectedParamTarget;

	[ObservableProperty]
	private Choice? _selectedParamExpression;

	[ObservableProperty]
	private string _paramFormat = string.Empty;

	[ObservableProperty]
	private string _paramCulture = string.Empty;

	[ObservableProperty]
	private string _paramCultureName = string.Empty;

	[ObservableProperty]
	private string _paramFormatPreview = string.Empty;

	[ObservableProperty]
	private Choice? _selectedParamType;

	[ObservableProperty]
	private string _paramAssign = string.Empty;

	[ObservableProperty]
	private string _sectionName = string.Empty;

	[ObservableProperty]
	private string _sectionObjectName = string.Empty;

	[ObservableProperty]
	private string _sectionLeft = string.Empty;

	[ObservableProperty]
	private string _sectionTop = string.Empty;

	[ObservableProperty]
	private string _sectionRight = string.Empty;

	[ObservableProperty]
	private string _sectionBottom = string.Empty;

	[ObservableProperty]
	private bool _autoHeight;

	[ObservableProperty]
	private string _markerForShow = string.Empty;

	[ObservableProperty]
	private string _markerForHide = string.Empty;

	public void RefreshCommands() => NotifyCommands();

	public void ReloadDocument()
	{
		ReloadSections();
		ReloadExpressions();
		OnCellChanged();
	}

	private void ReloadExpressions()
	{
		var selected = SelectedParamExpression?.Value;
		Expressions.Clear();
		Expressions.Add(new Choice(string.Empty, "Не установлено"));

		var manager = _owner.Manager;
		if (manager != null)
		{
			foreach (var expression in manager.GetExpressions().OrderBy(item => item.ID, StringComparer.CurrentCulture))
			{
				var id = expression.ID ?? string.Empty;
				if (id.Length == 0)
					continue;

				Expressions.Add(new Choice(id, id + " · " + ExpressionTitle(expression)));
			}
		}

		if (!string.IsNullOrEmpty(selected) && Expressions.All(item => item.Value != selected))
			Expressions.Add(new Choice(selected, selected + " · не найдено"));
	}

	private static string ExpressionTitle(BaseExpression expression)
	{
		if (expression is CountExpression)
			return "счётчик";
		if (expression is SumExpression)
			return "сумма";
		if (expression is DecimalExpression)
			return "число";
		if (expression is NowExpression)
			return "дата и время";
		if (expression is PagesCountExpression)
			return "страниц";
		if (expression is DocumentNameExpression)
			return "имя документа";

		return expression.GetType().Name;
	}

	public void OnCellChanged()
	{
		if (_refreshing)
			return;

		_refreshing = true;
		try
		{
			ReloadContents();
			ReloadMarkers();
			ReloadStyles();
			NotifyCommands();
		}
		finally
		{
			_refreshing = false;
		}
	}

	public void ReloadSections()
	{
		var manager = _owner.Manager;
		var previousName = SelectedSection?.Name;
		var previousObject = SelectedSection?.ObjectName;
		TemplateSection? match = null;

		BeginSync();
		try
		{
			Sections.Clear();
			if (manager != null)
			{
				foreach (var section in manager.GetSections())
				{
					Sections.Add(section);
					if (match == null && section.Name == previousName && section.ObjectName == previousObject)
						match = section;
				}
			}

			SelectedSection = match;
		}
		finally
		{
			EndSync();
		}
	}

	private void ReloadContents()
	{
		var cell = CurrentCell();
		var previous = SelectedContent?.Content;
		ContentRow? match = null;
		var text = false;
		var number = false;
		var date = false;
		var barcode = false;
		var image = false;
		var action = false;

		BeginSync();
		try
		{
			Contents.Clear();
			foreach (var content in Enumerate(cell))
			{
				var row = new ContentRow(content, TitleOf(content));
				Contents.Add(row);
				if (match == null && ReferenceEquals(content, previous))
					match = row;

				text |= content is TextContent;
				number |= content is DecimalContent;
				date |= content is DateTimeContent;
				barcode |= content is BarCodeContent;
				image |= content is ImageContent;
				action |= content is ActionContent;
			}

			HasTextContent = text;
			HasDecimalContent = number;
			HasDateTimeContent = date;
			HasBarCodeContent = barcode;
			HasImageContent = image;
			HasActionContent = action;
			SelectedContent = match ?? Contents.FirstOrDefault();
		}
		finally
		{
			EndSync();
		}
	}

	private void ReloadMarkers()
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		BeginSync();
		try
		{
			if (manager == null || presenter == null || !CanEdit())
			{
				AutoHeight = false;
				MarkerForShow = string.Empty;
				MarkerForHide = string.Empty;
				return;
			}

			var location = presenter.CurrentCell;
			AutoHeight = manager.IsAutoFitRow(location.Row);
			MarkerForShow = manager.GetColumnMarkerForShow(location.Column) ?? string.Empty;
			MarkerForHide = manager.GetColumnMarkerForHide(location.Column) ?? string.Empty;
		}
		finally
		{
			EndSync();
		}
	}

	private void ReloadStyles()
	{
		BeginSync();
		try
		{
			FillStyles(CellStyles, typeof(CellStyle), Cell.DefaultStyle);
			FillStyles(TextStyles, typeof(TextContentStyle), TextBasedContent.DefaultStyle);
			FillStyles(DecimalStyles, typeof(DecimalContentStyle), DecimalContent.DefaultStyle);
			FillStyles(DateTimeStyles, typeof(DateTimeContentStyle), DateTimeContent.DefaultStyle);

			var cell = CurrentCell();
			SelectedCellStyle = FindStyle(CellStyles, cell?.Style ?? Cell.DefaultStyle);
			SelectedTextStyle = FindStyle(TextStyles, FindContent<TextContent>(cell)?.GetStyle());
			SelectedDecimalStyle = FindStyle(DecimalStyles, FindContent<DecimalContent>(cell)?.GetStyle());
			SelectedDateTimeStyle = FindStyle(DateTimeStyles, FindContent<DateTimeContent>(cell)?.GetStyle());
		}
		finally
		{
			EndSync();
		}
	}

	private void LoadParams()
	{
		var previous = SelectedParam;
		BeginSync();
		try
		{
			Params.Clear();
			var content = SelectedContent?.Content;
			if (content != null)
			{
				foreach (var param in content.GetParams())
					Params.Add(param);
			}

			SelectedParam = previous != null && Params.Contains(previous) ? previous : Params.FirstOrDefault();
		}
		finally
		{
			EndSync();
		}
	}

	private void LoadContentFields()
	{
		var content = SelectedContent?.Content;
		BeginSync();
		try
		{
			BarcodeCode = string.Empty;
			SelectedBarcodeType = null;
			SelectedBarcodeOrientation = null;
			ImageUri = string.Empty;
			ImageColSpanText = "0";
			ImageRowSpanText = "0";
			SelectedActionType = null;

			IsSelectedBarCode = content is BarCodeContent;
			IsSelectedImage = content is ImageContent;
			IsSelectedAction = content is ActionContent;

			switch (content)
			{
				case BarCodeContent barCode:
					BarcodeCode = barCode.GetCode();
					SelectedBarcodeType = FindChoice(BarcodeTypes, barCode.GetBarCodeType().ToString());
					SelectedBarcodeOrientation = FindChoice(BarcodeOrientations, barCode.Orientation.ToString());
					break;
				case ImageContent image:
					ImageUri = image.Uri ?? string.Empty;
					ImageColSpanText = image.ColSpan.ToString(CultureInfo.CurrentCulture);
					ImageRowSpanText = image.RowSpan.ToString(CultureInfo.CurrentCulture);
					break;
				case ActionContent action:
					SelectedActionType = FindChoice(ActionTypes, action.Type.ToString());
					break;
			}
		}
		finally
		{
			EndSync();
		}
	}

	private void LoadParamFields()
	{
		var param = SelectedParam;
		BeginSync();
		try
		{
			HasSelectedParam = param != null;
			ParamName = param?.Name ?? string.Empty;
			ParamValue = param?.Value?.ToString() ?? string.Empty;
			ParamFormat = param?.Format ?? string.Empty;
			ParamCulture = param?.Culture ?? string.Empty;
			ParamAssign = param?.Assign ?? string.Empty;
			SelectedParamTarget = FindChoice(ParamTargets, param?.Target);
			SelectedParamType = FindChoice(ParamTypes, param?.Type);
			SelectedParamExpression = FindExpression(param?.ExpressionID);

			UpdateParamFormatPreview();
			UpdateParamCultureName();
		}
		finally
		{
			EndSync();
		}
	}

	private Choice? FindExpression(string? expressionId)
	{
		var id = expressionId ?? string.Empty;
		var choice = Expressions.FirstOrDefault(item => item.Value == id);
		if (choice == null && id.Length > 0)
		{
			choice = new Choice(id, id + " · не найдено");
			Expressions.Add(choice);
		}

		return choice ?? Expressions.FirstOrDefault();
	}

	private void UpdateParamFormatPreview()
	{
		var format = (ParamFormat ?? string.Empty).Trim();
		if (format.Length == 0)
		{
			ParamFormatPreview = string.Empty;
			return;
		}

		if (format.StartsWith("currency", StringComparison.OrdinalIgnoreCase)
			|| format.StartsWith("number", StringComparison.OrdinalIgnoreCase))
		{
			var cultureCode = string.IsNullOrEmpty(ParamCulture) ? null : ParamCulture;
			string text;
			if (format.Equals("numberString", StringComparison.OrdinalIgnoreCase))
			{
				text = CurrencyToString.GetString(1234.5m, null, cultureCode);
			}
			else
			{
				var parts = format.Split('|');
				var currency = parts.Length > 1 ? parts[1] : "RUR";
				text = CurrencyToString.GetString(1234.5m, currency, cultureCode);
			}

			ParamFormatPreview = text.Length > 0 ? "Пример: " + text : "Формат: число прописью";
			return;
		}

		var culture = ResolveCulture(ParamCulture);
		try
		{
			// тип значения задаёт цель параметра, а не содержимое ячейки,
			// поэтому формат угадываем по самому шаблону
			ParamFormatPreview = LooksLikeDateFormat(format)
				? "Пример: " + DateTime.Now.ToString(format, culture)
				: "Пример: " + 1234.5m.ToString(format, culture);
		}
		catch (FormatException)
		{
			ParamFormatPreview = "Неверный формат";
		}
	}

	private static bool LooksLikeDateFormat(string format)
	{
		if (format.IndexOf('0') >= 0 || format.IndexOf('#') >= 0)
			return false;

		foreach (var symbol in format)
		{
			if ("dMyHhst".IndexOf(symbol) >= 0)
				return true;
		}

		return false;
	}

	private void UpdateParamCultureName()
	{
		var code = ParamCulture ?? string.Empty;
		if (code.Length == 0)
		{
			ParamCultureName = string.Empty;
			return;
		}

		try
		{
			ParamCultureName = CultureInfo.GetCultureInfo(code).DisplayName;
		}
		catch (CultureNotFoundException)
		{
			ParamCultureName = "Неизвестная культура";
		}
	}

	private static CultureInfo ResolveCulture(string? code)
	{
		if (string.IsNullOrEmpty(code))
			return CultureInfo.CurrentCulture;

		try
		{
			return CultureInfo.GetCultureInfo(code);
		}
		catch (CultureNotFoundException)
		{
			return CultureInfo.CurrentCulture;
		}
	}

	private void LoadSectionFields()
	{
		var section = SelectedSection;
		BeginSync();
		try
		{
			HasSelectedSection = section != null;
			SectionName = section?.Name ?? string.Empty;
			SectionObjectName = section?.ObjectName ?? string.Empty;
			SectionLeft = section == null ? string.Empty : section.LeftDesign.ToString();
			SectionTop = section == null ? string.Empty : section.TopDesign.ToString();
			SectionRight = section == null ? string.Empty : section.RightDesign.ToString();
			SectionBottom = section == null ? string.Empty : section.BottomDesign.ToString();
		}
		finally
		{
			EndSync();
		}
	}

	partial void OnSelectedContentChanged(ContentRow? value)
	{
		LoadParams();
		LoadContentFields();
		RemoveSelectedContentCommand.NotifyCanExecuteChanged();
	}

	partial void OnBarcodeCodeChanged(string value)
	{
		if (IsSyncing || SelectedContent?.Content is not BarCodeContent barCode)
			return;

		var next = value ?? string.Empty;
		if ((barCode.GetCode() ?? string.Empty) == next)
			return;

		_owner.CommitDocumentChange(() => barCode.Code = next);
	}

	partial void OnSelectedBarcodeTypeChanged(Choice? value)
	{
		if (IsSyncing || value == null || SelectedContent?.Content is not BarCodeContent barCode)
			return;

		BarCodeType type;
		if (!Enum.TryParse(value.Value, out type) || barCode.GetBarCodeType() == type)
			return;

		_owner.CommitDocumentChange(() => barCode.Type = type);
	}

	partial void OnSelectedBarcodeOrientationChanged(Choice? value)
	{
		if (IsSyncing || value == null || SelectedContent?.Content is not BarCodeContent barCode)
			return;

		Orientation orientation;
		if (!Enum.TryParse(value.Value, out orientation) || barCode.Orientation == orientation)
			return;

		_owner.CommitDocumentChange(() => barCode.Orientation = orientation);
	}

	partial void OnImageUriChanged(string value)
	{
		if (IsSyncing || SelectedContent?.Content is not ImageContent image)
			return;

		var next = value ?? string.Empty;
		if ((image.Uri ?? string.Empty) == next)
			return;

		_owner.CommitDocumentChange(() => image.Uri = next);
	}

	partial void OnImageColSpanTextChanged(string value) => SetImageSpan(value, (image, span) => image.ColSpan = span, image => image.ColSpan);

	partial void OnImageRowSpanTextChanged(string value) => SetImageSpan(value, (image, span) => image.RowSpan = span, image => image.RowSpan);

	private void SetImageSpan(string value, Action<ImageContent, int> assign, Func<ImageContent, int> read)
	{
		if (IsSyncing || SelectedContent?.Content is not ImageContent image)
			return;

		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var span) || span < 0)
			return;

		if (read(image) == span)
			return;

		_owner.CommitDocumentChange(() => assign(image, span));
	}

	partial void OnSelectedActionTypeChanged(Choice? value)
	{
		if (IsSyncing || value == null || SelectedContent?.Content is not ActionContent action)
			return;

		ActionType type;
		if (!Enum.TryParse(value.Value, out type) || action.Type == type)
			return;

		_owner.CommitDocumentChange(() => action.Type = type);
	}

	partial void OnSelectedParamChanged(Param? value)
	{
		LoadParamFields();
		DeleteParamCommand.NotifyCanExecuteChanged();
	}

	partial void OnSelectedSectionChanged(TemplateSection? value)
	{
		LoadSectionFields();
		DeleteSectionCommand.NotifyCanExecuteChanged();
		SetSectionBoundsCommand.NotifyCanExecuteChanged();
		if (IsSyncing || value == null)
			return;

		var presenter = _owner.Presenter;
		if (presenter == null)
			return;

		presenter.BeginUpdate();
		presenter.StartSelection = value.LeftTop;
		presenter.EndSelection = value.RightBottom;
		presenter.EndUpdate();
	}

	partial void OnSelectedCellStyleChanged(StyleChoice? value)
	{
		if (IsSyncing || value == null)
			return;

		ApplyCellStyle(value.Style);
	}

	partial void OnSelectedTextStyleChanged(StyleChoice? value)
	{
		if (IsSyncing || value == null)
			return;

		ApplyContentStyle<TextContent>(value.Style);
	}

	partial void OnSelectedDecimalStyleChanged(StyleChoice? value)
	{
		if (IsSyncing || value == null)
			return;

		ApplyContentStyle<DecimalContent>(value.Style);
	}

	partial void OnSelectedDateTimeStyleChanged(StyleChoice? value)
	{
		if (IsSyncing || value == null)
			return;

		ApplyContentStyle<DateTimeContent>(value.Style);
	}

	partial void OnParamNameChanged(string value) => SetParam(value, param => param.Name, (param, next) => param.Name = next);

	partial void OnParamValueChanged(string value) => SetParam(value, param => param.Value?.ToString(), (param, next) => param.Value = next);

	partial void OnSelectedParamExpressionChanged(Choice? value)
	{
		var param = SelectedParam;
		if (IsSyncing || param == null)
			return;

		var id = value?.Value ?? string.Empty;
		if ((param.ExpressionID ?? string.Empty) == id)
			return;

		var expression = string.IsNullOrEmpty(id) ? null : _owner.Manager?.GetExpressionByID(id);
		if (!string.IsNullOrEmpty(id) && expression == null)
			return;

		// через свойство Expression: оно сбрасывает и кэш выражения, и идентификатор
		_owner.CommitDocumentChange(() => param.Expression = expression);
		LoadParamFields();
	}

	partial void OnParamFormatChanged(string value)
	{
		SetParam(value, param => param.Format, (param, next) => param.Format = next);
		UpdateParamFormatPreview();
	}

	partial void OnParamCultureChanged(string value)
	{
		SetParam(value, param => param.Culture, (param, next) => param.Culture = next);
		UpdateParamCultureName();
		UpdateParamFormatPreview();
	}

	partial void OnParamAssignChanged(string value) => SetParam(value, param => param.Assign, (param, next) => param.Assign = next);

	partial void OnSelectedParamTargetChanged(Choice? value)
	{
		if (IsSyncing)
			return;

		SetParam(value?.Value ?? string.Empty, param => param.Target, (param, next) => param.Target = next);
		LoadParamFields();
	}

	partial void OnSelectedParamTypeChanged(Choice? value)
	{
		if (IsSyncing)
			return;

		SetParam(value?.Value ?? string.Empty, param => param.Type, (param, next) => param.Type = next);
		LoadParamFields();
	}

	partial void OnSectionNameChanged(string value)
	{
		var section = SelectedSection;
		if (IsSyncing || section == null || section.Name == value)
			return;

		_owner.CommitDocumentChange(() => section.Name = value);
	}

	partial void OnSectionObjectNameChanged(string value)
	{
		var section = SelectedSection;
		if (IsSyncing || section == null || section.ObjectName == value)
			return;

		_owner.CommitDocumentChange(() => section.ObjectName = value ?? string.Empty);
	}

	partial void OnSectionLeftChanged(string value) => SetSection(value, section => section.LeftDesign, (section, number) => section.LeftDesign = number);

	partial void OnSectionTopChanged(string value) => SetSection(value, section => section.TopDesign, (section, number) => section.TopDesign = number);

	partial void OnSectionRightChanged(string value) => SetSection(value, section => section.RightDesign, (section, number) => section.RightDesign = number);

	partial void OnSectionBottomChanged(string value) => SetSection(value, section => section.BottomDesign, (section, number) => section.BottomDesign = number);

	partial void OnAutoHeightChanged(bool value)
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (IsSyncing || manager == null || presenter == null || !CanEdit())
			return;

		var row = presenter.CurrentCell.Row;
		if (manager.IsAutoFitRow(row) == value)
			return;

		_owner.CommitDocumentChange(() => manager.SetAutoFitRow(row, value));
	}

	partial void OnMarkerForShowChanged(string value)
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (IsSyncing || manager == null || presenter == null || !CanEdit())
			return;

		var column = presenter.CurrentCell.Column;
		if ((manager.GetColumnMarkerForShow(column) ?? string.Empty) == value)
			return;

		_owner.CommitDocumentChange(() =>
			manager.SetColumnMarkerForShow(column, string.IsNullOrEmpty(value) ? null! : value));
	}

	partial void OnMarkerForHideChanged(string value)
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (IsSyncing || manager == null || presenter == null || !CanEdit())
			return;

		var column = presenter.CurrentCell.Column;
		if ((manager.GetColumnMarkerForHide(column) ?? string.Empty) == value)
			return;

		_owner.CommitDocumentChange(() =>
			manager.SetColumnMarkerForHide(column, string.IsNullOrEmpty(value) ? null! : value));
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private async Task CopyAsync()
	{
		var presenter = _owner.Presenter;
		if (presenter == null)
			return;

		await CellClipboard.CopyAsync(presenter, _owner.Window);
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private async Task CutAsync()
	{
		var presenter = _owner.Presenter;
		if (presenter == null)
			return;

		string data;
		_owner.BeginEdit();
		try
		{
			data = CellClipboard.Cut(presenter);
		}
		finally
		{
			_owner.EndEdit();
		}

		await CellClipboard.PublishAsync(data, _owner.Window);
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private async Task PasteAsync()
	{
		var presenter = _owner.Presenter;
		if (presenter == null)
			return;

		var data = await CellClipboard.ReadAsync(_owner.Window);
		if (string.IsNullOrEmpty(data))
			return;

		_owner.BeginEdit();
		try
		{
			CellClipboard.Paste(presenter, data);
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private void MergeCells()
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null)
			return;

		_owner.BeginEdit();
		try
		{
			if (presenter.HaveSelection())
			{
				var start = presenter.GetStartSelection();
				var end = presenter.GetEndSelection();
				var source = manager.GetCell(presenter.CurrentCell);
				var cell = source != null
					? manager.CreateCell(start.Row, start.Column, source)
					: manager.CreateCell(start);
				if (cell == null)
					return;

				cell.Span = new CellSpan
				{
					RowSpan = end.Row - start.Row,
					ColSpan = end.Column - start.Column
				};
				presenter.ClearSelection();
				presenter.CurrentCell = start;
			}
			else
			{
				var cell = manager.GetCell(presenter.CurrentCell);
				if (cell != null && (cell.Span.RowSpan != 0 || cell.Span.ColSpan != 0))
					cell.Span = new CellSpan();
			}
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private async Task InsertRowBeforeAsync()
	{
		await InsertRowsAsync(after: false);
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private async Task InsertRowAfterAsync()
	{
		await InsertRowsAsync(after: true);
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private async Task DeleteRowsAsync()
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null)
			return;

		var fit = await AskSectionsAsync();
		_owner.BeginEdit();
		try
		{
			if (presenter.HaveSelection())
			{
				var start = presenter.GetStartSelection();
				var end = presenter.GetEndSelection();
				presenter.ClearSelection();
				manager.DeleteRows(start.Row, end.Row - start.Row + 1, fit);
			}
			else
				manager.DeleteRows(presenter.CurrentCell.Row, 1, fit);

			if (presenter.CurrentCell.Row >= manager.RowsCount)
			{
				presenter.CurrentCell = new CellLocation
				{
					Row = manager.RowsCount - 1,
					Column = presenter.CurrentCell.Column
				};
			}
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private async Task InsertColumnBeforeAsync()
	{
		await InsertColumnsAsync(after: false);
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private async Task InsertColumnAfterAsync()
	{
		await InsertColumnsAsync(after: true);
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private async Task DeleteColumnsAsync()
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null)
			return;

		var fit = await AskSectionsAsync();
		_owner.BeginEdit();
		try
		{
			if (presenter.HaveSelection())
			{
				var start = presenter.GetStartSelection();
				var end = presenter.GetEndSelection();
				presenter.ClearSelection();
				manager.DeleteColumns(start.Column, end.Column - start.Column + 1, fit);
			}
			else
				manager.DeleteColumns(presenter.CurrentCell.Column, 1, fit);

			if (presenter.CurrentCell.Column >= manager.ColumnsCount)
			{
				presenter.CurrentCell = new CellLocation
				{
					Row = presenter.CurrentCell.Row,
					Column = manager.ColumnsCount - 1
				};
			}
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private void DeleteCell()
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null)
			return;

		_owner.BeginEdit();
		try
		{
			var start = presenter.CurrentCell;
			var end = presenter.CurrentCell;
			if (presenter.HaveSelection())
			{
				start = presenter.GetStartSelection();
				end = presenter.GetEndSelection();
				presenter.ClearSelection();
			}

			for (var column = start.Column; column <= end.Column; column++)
			{
				for (var row = start.Row; row <= end.Row; row++)
					manager.DeleteCell(row, column);
			}
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private void DeleteContent()
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null)
			return;

		_owner.BeginEdit();
		try
		{
			ForEachSelectedCell(presenter, (row, column) =>
			{
				var cell = manager.GetCell(row, column);
				if (cell?.Content != null)
					cell.Content = null!;
			});
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	[RelayCommand(CanExecute = nameof(CanRemoveSelectedContent))]
	private void RemoveSelectedContent()
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		var content = SelectedContent?.Content;
		if (manager == null || presenter == null || content == null)
			return;

		_owner.BeginEdit();
		try
		{
			ForEachSelectedCell(presenter, (row, column) =>
			{
				var cell = manager.GetCell(row, column);
				if (cell?.Content is CollectionContent collection)
				{
					collection.Remove(content);
					if (collection.Count == 0)
						cell.Content = null!;
				}
				else if (ReferenceEquals(cell?.Content, content))
					cell.Content = null!;
			});
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	private bool CanRemoveSelectedContent() => SelectedContent != null && CanEdit();

	[RelayCommand(CanExecute = nameof(CanAddText))]
	private void AddText() => AddContent(manager => new TextContent(manager));

	[RelayCommand(CanExecute = nameof(CanAddDecimal))]
	private void AddDecimal() => AddContent(manager => new DecimalContent(manager));

	[RelayCommand(CanExecute = nameof(CanAddDateTime))]
	private void AddDateTime() => AddContent(manager => new DateTimeContent(manager));

	[RelayCommand(CanExecute = nameof(CanAddBarCode))]
	private void AddBarCode() => AddContent(manager => new BarCodeContent(manager, BarCodeType.EAN13));

	[RelayCommand(CanExecute = nameof(CanAddImage))]
	private void AddImage() => AddContent(manager => new ImageContent(manager));

	[RelayCommand(CanExecute = nameof(CanAddAction))]
	private void AddAction() => AddContent(manager => new ActionContent(manager) { Type = ActionType.MouseDblClick });

	[RelayCommand(CanExecute = nameof(CanAddParam))]
	private void AddParam()
	{
		var manager = _owner.Manager;
		var content = SelectedContent?.Content;
		if (manager == null || content == null)
			return;

		_owner.BeginEdit();
		try
		{
			content.AddParam(new Param(manager, content) { Name = "Новый" });
		}
		finally
		{
			_owner.EndEdit();
		}

		SelectedParam = Params.LastOrDefault();
	}

	[RelayCommand(CanExecute = nameof(CanDeleteParam))]
	private void DeleteParam()
	{
		var content = SelectedContent?.Content;
		var param = SelectedParam;
		if (content == null || param == null)
			return;

		_owner.BeginEdit();
		try
		{
			content.RemoveParam(param);
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	private void AddSection()
	{
		var manager = _owner.Manager;
		if (manager == null)
			return;

		var section = new TemplateSection { Name = "Новая" };
		_owner.BeginEdit();
		try
		{
			manager.AddSection(section);
		}
		finally
		{
			_owner.EndEdit();
		}

		ReloadSections();
		SelectedSection = Sections.LastOrDefault();
	}

	[RelayCommand(CanExecute = nameof(CanDeleteSection))]
	private void DeleteSection()
	{
		var manager = _owner.Manager;
		var section = SelectedSection;
		if (manager == null || section == null)
			return;

		_owner.BeginEdit();
		try
		{
			manager.RemoveSection(section);
		}
		finally
		{
			_owner.EndEdit();
		}

		ReloadSections();
	}

	[RelayCommand(CanExecute = nameof(CanDeleteSection))]
	private void SetSectionBounds()
	{
		var presenter = _owner.Presenter;
		var section = SelectedSection;
		if (presenter == null || section == null)
			return;

		var start = presenter.HaveSelection() ? presenter.GetStartSelection() : presenter.CurrentCell;
		var end = presenter.HaveSelection() ? presenter.GetEndSelection() : presenter.CurrentCell;
		_owner.BeginEdit();
		try
		{
			section.Left = Math.Min(start.Column, end.Column);
			section.Top = Math.Min(start.Row, end.Row);
			section.Right = Math.Max(start.Column, end.Column);
			section.Bottom = Math.Max(start.Row, end.Row);
		}
		finally
		{
			_owner.EndEdit();
		}

		LoadSectionFields();
	}

	private async Task InsertRowsAsync(bool after)
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null)
			return;

		var fit = await AskSectionsAsync();
		var index = presenter.CurrentCell.Row + (after ? 1 : 0);
		_owner.BeginEdit();
		try
		{
			manager.InsertRows(index, 1, fit);
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	private async Task InsertColumnsAsync(bool after)
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null)
			return;

		var fit = await AskSectionsAsync();
		var index = presenter.CurrentCell.Column + (after ? 1 : 0);
		_owner.BeginEdit();
		try
		{
			manager.InsertColumns(index, 1, fit);
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	private void AddContent(Func<CellManager, BaseContent> create)
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null)
			return;

		_owner.BeginEdit();
		try
		{
			var cell = manager.GetCell(presenter.CurrentCell) ?? manager.CreateCell(presenter.CurrentCell);
			var content = create(manager);
			if (cell.Content is CollectionContent collection)
				collection.Add(content);
			else if (cell.Content != null)
			{
				var wrap = new CollectionContent(manager, 2);
				wrap.Add(content);
				wrap.Add(cell.Content);
				cell.Content = wrap;
			}
			else
				cell.Content = content;
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	private void ApplyCellStyle(BaseStyle style)
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null || style is not CellStyle cellStyle)
			return;

		var id = cellStyle.ID ?? string.Empty;
		_owner.BeginEdit();
		try
		{
			ForEachSelectedCell(presenter, (row, column) =>
			{
				var cell = manager.GetCell(row, column) ?? manager.CreateCell(row, column);
				if ((cell.StyleID ?? string.Empty) == id)
					return;

				cell.Style = cellStyle;
			});
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	private void ApplyContentStyle<T>(BaseStyle style) where T : BaseContent
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null)
			return;

		var id = style.ID ?? string.Empty;
		_owner.BeginEdit();
		try
		{
			ForEachSelectedCell(presenter, (row, column) =>
			{
				var content = FindContent<T>(manager.GetCell(row, column));
				if (content == null || (content.StyleID ?? string.Empty) == id)
					return;

				content.StyleID = id;
			});
		}
		finally
		{
			_owner.EndEdit();
		}
	}

	private bool CanEdit()
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (_owner.IsCodeMode || manager == null || presenter == null)
			return false;

		var cell = presenter.CurrentCell;
		return cell.Row >= 0 && cell.Column >= 0
			&& cell.Row < manager.RowsCount && cell.Column < manager.ColumnsCount;
	}

	private bool CanAddText() => CanEdit() && !HasTextContent;

	private bool CanAddDecimal() => CanEdit() && !HasDecimalContent;

	private bool CanAddDateTime() => CanEdit() && !HasDateTimeContent;

	private bool CanAddBarCode() => CanEdit() && !HasBarCodeContent;

	private bool CanAddImage() => CanEdit() && !HasImageContent;

	private bool CanAddAction() => CanEdit() && !HasActionContent;

	private bool CanAddParam() => SelectedContent != null;

	private bool CanDeleteParam() => SelectedParam != null;

	private bool CanDeleteSection() => SelectedSection != null;

	private Cell? CurrentCell()
	{
		var manager = _owner.Manager;
		var presenter = _owner.Presenter;
		if (manager == null || presenter == null || !CanEdit())
			return null;

		return manager.GetCell(presenter.CurrentCell);
	}

	private void SetParam(string value, Func<Param, string?> read, Action<Param, string> apply)
	{
		var param = SelectedParam;
		if (IsSyncing || param == null)
			return;

		if ((read(param) ?? string.Empty) == value)
			return;

		_owner.CommitDocumentChange(() => apply(param, value));
	}

	private void SetSection(string value, Func<TemplateSection, int> read, Action<TemplateSection, int> apply)
	{
		var section = SelectedSection;
		if (IsSyncing || section == null || !int.TryParse(value, out var number))
			return;

		if (read(section) == number)
			return;

		_owner.CommitDocumentChange(() => apply(section, number));
		RefreshSectionSelection(section);
	}

	private void RefreshSectionSelection(TemplateSection section)
	{
		var presenter = _owner.Presenter;
		if (presenter == null)
			return;

		presenter.BeginUpdate();
		presenter.StartSelection = section.LeftTop;
		presenter.EndSelection = section.RightBottom;
		presenter.EndUpdate();
	}

	private async Task<bool> AskSectionsAsync()
	{
		var window = _owner.Window;
		if (window == null)
			return false;

		return await ConfirmDialog.AskAsync(
			window,
			"Секции",
			"Изменить размеры или положение секции находящиеся в затронутых ячейках?");
	}

	private void NotifyCommands()
	{
		CopyCommand.NotifyCanExecuteChanged();
		CutCommand.NotifyCanExecuteChanged();
		PasteCommand.NotifyCanExecuteChanged();
		MergeCellsCommand.NotifyCanExecuteChanged();
		InsertRowBeforeCommand.NotifyCanExecuteChanged();
		InsertRowAfterCommand.NotifyCanExecuteChanged();
		DeleteRowsCommand.NotifyCanExecuteChanged();
		InsertColumnBeforeCommand.NotifyCanExecuteChanged();
		InsertColumnAfterCommand.NotifyCanExecuteChanged();
		DeleteColumnsCommand.NotifyCanExecuteChanged();
		DeleteCellCommand.NotifyCanExecuteChanged();
		DeleteContentCommand.NotifyCanExecuteChanged();
		RemoveSelectedContentCommand.NotifyCanExecuteChanged();
		AddTextCommand.NotifyCanExecuteChanged();
		AddDecimalCommand.NotifyCanExecuteChanged();
		AddDateTimeCommand.NotifyCanExecuteChanged();
		AddBarCodeCommand.NotifyCanExecuteChanged();
		AddImageCommand.NotifyCanExecuteChanged();
		AddActionCommand.NotifyCanExecuteChanged();
		AddParamCommand.NotifyCanExecuteChanged();
		DeleteParamCommand.NotifyCanExecuteChanged();
		AddSectionCommand.NotifyCanExecuteChanged();
		DeleteSectionCommand.NotifyCanExecuteChanged();
		SetSectionBoundsCommand.NotifyCanExecuteChanged();
	}

	private static void ForEachSelectedCell(BaseDisplayPresenter presenter, Action<int, int> action)
	{
		if (presenter.HaveSelection())
		{
			var start = presenter.GetStartSelection();
			var end = presenter.GetEndSelection();
			for (var row = start.Row; row <= end.Row; row++)
			{
				for (var column = start.Column; column <= end.Column; column++)
					action(row, column);
			}
		}
		else
			action(presenter.CurrentCell.Row, presenter.CurrentCell.Column);
	}

	private static IEnumerable<BaseContent> Enumerate(Cell? cell)
	{
		if (cell?.Content is CollectionContent collection)
			return collection.GetContents();

		if (cell?.Content != null)
			return new[] { cell.Content };

		return Array.Empty<BaseContent>();
	}

	private static T? FindContent<T>(Cell? cell) where T : BaseContent
	{
		foreach (var content in Enumerate(cell))
		{
			if (content.GetType() == typeof(T))
				return (T)content;
		}

		return null;
	}

	private void FillStyles(ObservableCollection<StyleChoice> target, Type type, BaseStyle fallback)
	{
		var next = new List<StyleChoice> { new(fallback, StyleTitle(fallback)) };
		var manager = _owner.Manager;
		if (manager != null)
		{
			foreach (var style in manager.GetStyles())
			{
				if (style.GetType() != type)
					continue;
				if ((style.ID ?? string.Empty) == (fallback.ID ?? string.Empty))
					continue;

				next.Add(new StyleChoice(style, StyleTitle(style)));
			}
		}

		if (SameStyles(target, next))
			return;

		target.Clear();
		foreach (var item in next)
			target.Add(item);
	}

	private static bool SameStyles(ObservableCollection<StyleChoice> current, List<StyleChoice> next)
	{
		if (current.Count != next.Count)
			return false;

		for (var i = 0; i < next.Count; i++)
		{
			if (current[i].Style.GetType() != next[i].Style.GetType())
				return false;
			if ((current[i].Style.ID ?? string.Empty) != (next[i].Style.ID ?? string.Empty))
				return false;
		}

		return true;
	}

	private static StyleChoice? FindStyle(IEnumerable<StyleChoice> styles, BaseStyle? style)
	{
		if (style == null)
			return styles.FirstOrDefault();

		var id = style.ID ?? string.Empty;
		return styles.FirstOrDefault(item => item.Style.GetType() == style.GetType() && (item.Style.ID ?? string.Empty) == id)
			?? styles.FirstOrDefault();
	}

	private static Choice? FindChoice(IEnumerable<Choice> choices, string? value)
	{
		var key = value ?? string.Empty;
		return choices.FirstOrDefault(item => item.Value == key) ?? choices.FirstOrDefault();
	}

	private static string TitleOf(BaseContent content)
	{
		if (content is TextContent)
			return "Текст";
		if (content is DecimalContent)
			return "Число";
		if (content is DateTimeContent)
			return "Дата и время";
		if (content is BarCodeContent)
			return "Штрих-код";
		if (content is ImageContent)
			return "Изображение";
		if (content is ActionContent)
			return "Действие";

		return content.GetType().Name;
	}

	private static string StyleTitle(BaseStyle style)
	{
		return string.IsNullOrEmpty(style.ID) ? "По умолчанию" : style.ID;
	}

	private void BeginSync() => _syncDepth++;

	private void EndSync()
	{
		if (_syncDepth > 0)
			_syncDepth--;
	}

	private bool IsSyncing => _syncDepth > 0;
}

public sealed class ContentRow
{
	public ContentRow(BaseContent content, string title)
	{
		Content = content;
		Title = title;
		Description = Describe(content);
		ParamsLabel = FormatParams(content.GetParams().Count());
	}

	public BaseContent Content { get; }

	public string Title { get; }

	public string Description { get; }

	public string ParamsLabel { get; }

	public bool HasDescription => Description.Length > 0;

	public bool HasParamsLabel => ParamsLabel.Length > 0;

	public override string ToString() => Title;

	private static string Describe(BaseContent content)
	{
		switch (content)
		{
			case TextContent text:
				return Shorten(text.Text);
			case DecimalContent number:
				return number.GetValue().ToString(CultureInfo.CurrentCulture);
			case DateTimeContent date:
				return date.GetValue().ToString("g", CultureInfo.CurrentCulture);
			case BarCodeContent barCode:
				return Shorten(barCode.GetCode());
			case ImageContent image:
				return Shorten(image.Uri);
			case ActionContent action:
				return action.Type.ToString();
			default:
				return string.Empty;
		}
	}

	private static string Shorten(string? text)
	{
		var value = (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
		return value.Length <= 60 ? value : value.Substring(0, 57) + "…";
	}

	private static string FormatParams(int count)
	{
		if (count <= 0)
			return string.Empty;

		var tail = count % 100;
		if (tail >= 11 && tail <= 14)
			return count + " параметров";

		switch (count % 10)
		{
			case 1:
				return count + " параметр";
			case 2:
			case 3:
			case 4:
				return count + " параметра";
			default:
				return count + " параметров";
		}
	}
}

public sealed class StyleChoice
{
	public StyleChoice(BaseStyle style, string title)
	{
		Style = style;
		Title = title;
	}

	public BaseStyle Style { get; }

	public string Title { get; }

	public override string ToString() => Title;
}

public sealed class Choice
{
	public Choice(string value, string title)
	{
		Value = value;
		Title = title;
	}

	public string Value { get; }

	public string Title { get; }

	public override string ToString() => Title;
}
