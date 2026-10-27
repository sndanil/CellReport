using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CellReport.Core;
using CellReport.Core.Contents;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreFontStyle = CellReport.Core.FontStyle;

namespace CellReport.Editor.Avalonia.ViewModels;

/// <summary>
/// Редактор стиля. Стили неизменяемые, поэтому при применении создаётся новый объект
/// и подменяет старый по идентификатору.
/// </summary>
public partial class StyleEditorViewModel : ObservableObject
{
	private readonly MainWindowViewModel _owner;
	private readonly BaseStyle _source;
	private readonly BaseStyle? _original;
	private readonly Type _styleType;

	public StyleEditorViewModel(MainWindowViewModel owner, BaseStyle style, bool isNew)
	{
		_owner = owner;
		_source = style;
		_original = isNew ? null : style;
		_styleType = style.GetType();

		IsText = style is TextContentStyle;
		IsDecimal = style is DecimalContentStyle;
		IsDate = style is DateTimeContentStyle;
		IsCell = style is CellStyle;
		IsLine = style is LineContentStyle;

		var textStyle = style as TextContentStyle;
		if (textStyle != null)
		{
			FontFamily = textStyle.Font.Family;
			FontSizeText = textStyle.Font.Size.ToString(CultureInfo.CurrentCulture);
			Bold = (textStyle.Font.Style & CoreFontStyle.Bold) == CoreFontStyle.Bold;
			Italic = (textStyle.Font.Style & CoreFontStyle.Italic) == CoreFontStyle.Italic;
			Underline = (textStyle.Font.Style & CoreFontStyle.Underline) == CoreFontStyle.Underline;
			Strikeout = (textStyle.Font.Style & CoreFontStyle.Strikeout) == CoreFontStyle.Strikeout;
			Color = textStyle.Color ?? "Black";
			AngleText = textStyle.Angle.ToString(CultureInfo.CurrentCulture);
			SelectedHorizontal = FindHorizontal(textStyle.HAlignment);
			SelectedVertical = FindVertical(textStyle.VAlignment);
			SelectedDirection = FindDirection(textStyle.Direction);
		}
		else if (style is CellStyle cellStyle)
		{
			Color = string.IsNullOrEmpty(cellStyle.Color) ? "Transparent" : cellStyle.Color;
		}
		else if (style is LineContentStyle lineStyle)
		{
			Color = lineStyle.Color ?? "Black";
			WidthText = lineStyle.Width.ToString(CultureInfo.CurrentCulture);
		}

		if (style is DecimalContentStyle decimalStyle)
		{
			Format = decimalStyle.Format ?? string.Empty;
			Culture = decimalStyle.Culture ?? string.Empty;
			NegativeColor = decimalStyle.NegativeColor ?? "Red";
		}
		else if (style is DateTimeContentStyle dateStyle)
		{
			Format = dateStyle.Format ?? string.Empty;
			Culture = dateStyle.Culture ?? string.Empty;
		}

		Id = _original == null || string.IsNullOrEmpty(_original.ID)
			? NextStyleId(_styleType)
			: _original.ID;

		Title = (isNew ? "Новый стиль: " : "Стиль: ") + StyleTypeTitle(_styleType);
		UpdatePreview();
	}

	public string Title { get; }

	public bool IsText { get; }

	public bool IsDecimal { get; }

	public bool IsDate { get; }

	public bool IsCell { get; }

	public bool IsLine { get; }

	public bool ShowTextPreview => !IsLine;

	public IReadOnlyList<string> Fonts => _owner.Fonts;

	public IReadOnlyList<string> FontSizes => _owner.FontSizes;

	public ObservableCollection<string> Cultures => _owner.Inspector.Cultures;

	public IReadOnlyList<string> FormatPresets => IsDecimal
		? ReportFormatPresets.Numbers
		: ReportFormatPresets.Dates;

	public Choice[] HorizontalAlignments { get; } =
	{
		new("Left", "По левому краю"),
		new("Center", "По центру"),
		new("Right", "По правому краю"),
		new("Justify", "По ширине")
	};

	public Choice[] VerticalAlignments { get; } =
	{
		new("Top", "По верху"),
		new("Middle", "По середине"),
		new("Bottom", "По низу")
	};

	public Choice[] Directions { get; } =
	{
		new("LeftToRight", "Слева направо"),
		new("TopToDown", "Сверху вниз")
	};

	[ObservableProperty]
	private string _id = string.Empty;

	[ObservableProperty]
	private string _fontFamily = FontInfo.Default.Family;

	[ObservableProperty]
	private string _fontSizeText = "10";

	[ObservableProperty]
	private bool _bold;

	[ObservableProperty]
	private bool _italic;

	[ObservableProperty]
	private bool _underline;

	[ObservableProperty]
	private bool _strikeout;

	[ObservableProperty]
	private string _color = "Black";

	[ObservableProperty]
	private string _angleText = "0";

	[ObservableProperty]
	private string _widthText = "0.25";

	[ObservableProperty]
	private Choice _selectedHorizontal;

	[ObservableProperty]
	private Choice _selectedVertical;

	[ObservableProperty]
	private Choice _selectedDirection;

	[ObservableProperty]
	private string _format = string.Empty;

	[ObservableProperty]
	private string _culture = string.Empty;

	[ObservableProperty]
	private string _negativeColor = "Red";

	[ObservableProperty]
	private string _previewText = string.Empty;

	[ObservableProperty]
	private string _cultureName = string.Empty;

	[ObservableProperty]
	private IBrush _previewBrush = Brushes.Black;

	[ObservableProperty]
	private IBrush _previewBackground = Brushes.White;

	[ObservableProperty]
	private IBrush _previewBorderBrush = Brushes.Transparent;

	[ObservableProperty]
	private Thickness _previewBorderThickness = new Thickness(0);

	[ObservableProperty]
	private double _previewFontSize = 12;

	[ObservableProperty]
	private FontWeight _previewWeight = FontWeight.Normal;

	[ObservableProperty]
	private global::Avalonia.Media.FontStyle _previewFontStyle = global::Avalonia.Media.FontStyle.Normal;

	[ObservableProperty]
	private string _errorMessage = string.Empty;

	public bool HasError => ErrorMessage.Length > 0;

	public bool CanEditText => IsText;

	public bool CanEditFormat => IsDecimal || IsDate;

	public bool CanEditNegativeColor => IsDecimal;

	public bool CanEditWidth => IsLine;

	partial void OnFontFamilyChanged(string value) => UpdatePreview();

	partial void OnFontSizeTextChanged(string value) => UpdatePreview();

	partial void OnBoldChanged(bool value) => UpdatePreview();

	partial void OnItalicChanged(bool value) => UpdatePreview();

	partial void OnUnderlineChanged(bool value) => UpdatePreview();

	partial void OnStrikeoutChanged(bool value) => UpdatePreview();

	partial void OnColorChanged(string value) => UpdatePreview();

	partial void OnFormatChanged(string value) => UpdatePreview();

	partial void OnCultureChanged(string value) => UpdatePreview();

	partial void OnNegativeColorChanged(string value) => UpdatePreview();

	partial void OnWidthTextChanged(string value) => UpdatePreview();

	partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

	public bool TryApply()
	{
		ErrorMessage = string.Empty;

		var manager = _owner.Manager;
		if (manager == null)
			return false;

		var id = (Id ?? string.Empty).Trim();
		if (id.Length == 0)
		{
			ErrorMessage = "Укажите идентификатор стиля";
			return false;
		}

		var existing = manager.GetStyles()
			.FirstOrDefault(item => item.GetType() == _styleType && string.Equals(item.ID, id, StringComparison.Ordinal));
		if (existing != null && !ReferenceEquals(existing, _original))
		{
			ErrorMessage = "Стиль «" + id + "» уже существует";
			return false;
		}

		float size = 0;
		if (IsText && (!float.TryParse(FontSizeText, NumberStyles.Float, CultureInfo.CurrentCulture, out size) || size <= 0))
		{
			ErrorMessage = "Размер шрифта должен быть больше нуля";
			return false;
		}

		float angle;
		if (!float.TryParse(AngleText, NumberStyles.Float, CultureInfo.CurrentCulture, out angle))
			angle = 0;

		float width = 0;
		if (IsLine && (!float.TryParse(WidthText, NumberStyles.Float, CultureInfo.CurrentCulture, out width) || width <= 0))
		{
			ErrorMessage = "Толщина линии должна быть больше нуля";
			return false;
		}

		if (!IsColorValid(Color, out var colorError))
		{
			ErrorMessage = colorError;
			return false;
		}

		if (IsDecimal && !IsColorValid(NegativeColor, out colorError))
		{
			ErrorMessage = colorError;
			return false;
		}

		var style = BuildStyle(id, size, angle, width);
		if (style == null)
		{
			ErrorMessage = "Этот тип стиля не поддерживает редактирование";
			return false;
		}

		_owner.CommitDocumentChange(() =>
		{
			if (_original == null)
			{
				manager.AddStyle(style);
				return;
			}

			var oldId = _original.ID ?? string.Empty;
			if (!string.Equals(oldId, id, StringComparison.Ordinal))
			{
				manager.ReplaceStyleReferences(_styleType, oldId, id);
				manager.RemoveStyle(_original);
			}

			manager.ReplaceStyle(style);
		});

		return true;
	}

	private BaseStyle? BuildStyle(string id, float size, float angle, float width)
	{
		var color = string.IsNullOrWhiteSpace(Color) ? "Black" : Color;
		BaseStyle style;

		if (IsCell)
		{
			// границы и отступы редактируются отдельно — сохраняем как есть
			var cell = (CellStyle)_source;
			style = new CellStyle(color, cell.Borders, cell.Paddings);
		}
		else if (IsLine)
		{
			style = new LineContentStyle(color, width);
		}
		else if (IsText)
		{
			var fontStyle = CoreFontStyle.Regular;
			if (Bold)
				fontStyle |= CoreFontStyle.Bold;
			if (Italic)
				fontStyle |= CoreFontStyle.Italic;
			if (Underline)
				fontStyle |= CoreFontStyle.Underline;
			if (Strikeout)
				fontStyle |= CoreFontStyle.Strikeout;

			var family = string.IsNullOrWhiteSpace(FontFamily) ? FontInfo.Default.Family : FontFamily;
			var font = new FontInfo(family, size, fontStyle);
			var horizontal = ParseEnum(SelectedHorizontal.Value, HorizontalAlignment.Left);
			var vertical = ParseEnum(SelectedVertical.Value, VerticalAlignment.Middle);
			var direction = ParseEnum(SelectedDirection.Value, Direction.LeftToRight);

			if (IsDecimal)
			{
				style = new DecimalContentStyle(horizontal, vertical, font, color,
					string.IsNullOrWhiteSpace(NegativeColor) ? color : NegativeColor,
					angle, Format ?? string.Empty, Culture ?? string.Empty, direction);
			}
			else if (IsDate)
			{
				style = new DateTimeContentStyle(horizontal, vertical, font, color,
					angle, Format ?? string.Empty, Culture ?? string.Empty, direction);
			}
			else
			{
				style = new TextContentStyle(horizontal, vertical, font, color, angle, direction);
			}
		}
		else
		{
			return null;
		}

		style.ID = id;
		return style;
	}

	private void UpdatePreview()
	{
		PreviewText = IsCell ? "Ячейка" : IsLine ? string.Empty : FormatSample();

		float size;
		PreviewFontSize = float.TryParse(FontSizeText, NumberStyles.Float, CultureInfo.CurrentCulture, out size) && size > 0
			? size
			: 10;
		PreviewWeight = Bold ? FontWeight.Bold : FontWeight.Normal;
		PreviewFontStyle = Italic ? global::Avalonia.Media.FontStyle.Italic : global::Avalonia.Media.FontStyle.Normal;

		PreviewBrush = IsCell ? Brushes.Black : CreateBrush(Color, Brushes.Black);

		if (IsCell)
		{
			PreviewBackground = string.IsNullOrWhiteSpace(Color)
				|| string.Equals(Color, "Transparent", StringComparison.OrdinalIgnoreCase)
				? Brushes.White
				: CreateBrush(Color, Brushes.White);
			PreviewBorderBrush = Brushes.Gray;
			PreviewBorderThickness = new Thickness(1);
		}
		else
		{
			PreviewBackground = Brushes.White;
			PreviewBorderBrush = Brushes.Transparent;
			PreviewBorderThickness = new Thickness(0);
		}

		try
		{
			CultureName = string.IsNullOrWhiteSpace(Culture) ? string.Empty : CultureInfo.GetCultureInfo(Culture).DisplayName;
		}
		catch (CultureNotFoundException)
		{
			CultureName = "Неизвестная культура";
		}
	}

	private static IBrush CreateBrush(string? color, IBrush fallback)
	{
		if (string.IsNullOrWhiteSpace(color))
			return fallback;

		try
		{
			return new SolidColorBrush(global::Avalonia.Media.Color.Parse(color));
		}
		catch (Exception)
		{
			return fallback;
		}
	}

	private string FormatSample()
	{
		var culture = string.IsNullOrWhiteSpace(Culture) ? CultureInfo.CurrentCulture : SafeCulture(Culture);
		var format = Format ?? string.Empty;

		if (IsDecimal
			&& (format.StartsWith("currency", StringComparison.OrdinalIgnoreCase)
				|| format.StartsWith("number", StringComparison.OrdinalIgnoreCase)))
		{
			var cultureCode = string.IsNullOrWhiteSpace(Culture) ? null : Culture;
			if (format.Equals("numberString", StringComparison.OrdinalIgnoreCase))
			{
				var numberText = CurrencyToString.GetString(1234.5m, null, cultureCode);
				return numberText.Length > 0 ? numberText : "Число прописью";
			}

			var parts = format.Split('|');
			var currencyText = CurrencyToString.GetString(1234.5m, parts.Length > 1 ? parts[1] : "RUR", cultureCode);
			return currencyText.Length > 0 ? currencyText : "Сумма прописью";
		}

		try
		{
			if (IsDecimal)
			{
				var sample = 1234.5m;
				return format.Length == 0 ? sample.ToString(culture) : sample.ToString(format, culture);
			}

			if (IsDate)
			{
				var sample = DateTime.Now;
				return format.Length == 0 ? sample.ToString(culture) : sample.ToString(format, culture);
			}
		}
		catch (FormatException)
		{
			return "Неверный формат";
		}

		return "Пример текста";
	}

	private static CultureInfo SafeCulture(string code)
	{
		try
		{
			return CultureInfo.GetCultureInfo(code);
		}
		catch (CultureNotFoundException)
		{
			return CultureInfo.CurrentCulture;
		}
	}

	private string NextStyleId(Type type)
	{
		var prefix = StyleIdPrefix(type);
		var manager = _owner.Manager;
		var index = 1;
		while (manager != null && manager.GetStyles().Any(item => item.GetType() == type && item.ID == prefix + index.ToString(CultureInfo.InvariantCulture)))
			index++;

		return prefix + index.ToString(CultureInfo.InvariantCulture);
	}

	private static string StyleIdPrefix(Type type)
	{
		if (type == typeof(DecimalContentStyle))
			return "number";
		if (type == typeof(DateTimeContentStyle))
			return "date";
		if (type == typeof(CellStyle))
			return "cell";
		if (type == typeof(LineContentStyle))
			return "line";
		return "text";
	}

	private static string StyleTypeTitle(Type type)
	{
		if (type == typeof(DecimalContentStyle))
			return "число";
		if (type == typeof(DateTimeContentStyle))
			return "дата и время";
		if (type == typeof(CellStyle))
			return "ячейка";
		if (type == typeof(LineContentStyle))
			return "линия";
		return "текст";
	}

	private static bool IsColorValid(string? value, out string error)
	{
		error = string.Empty;
		if (string.IsNullOrWhiteSpace(value))
			return true;

		if (global::Avalonia.Media.Color.TryParse(value, out _))
			return true;

		error = "Неверный цвет «" + value + "»";
		return false;
	}

	private Choice FindHorizontal(HorizontalAlignment value)
	{
		return HorizontalAlignments.FirstOrDefault(item => item.Value == value.ToString()) ?? HorizontalAlignments[0];
	}

	private Choice FindVertical(VerticalAlignment value)
	{
		var name = value == VerticalAlignment.Center ? nameof(VerticalAlignment.Middle) : value.ToString();
		return VerticalAlignments.FirstOrDefault(item => item.Value == name) ?? VerticalAlignments[1];
	}

	private Choice FindDirection(Direction value)
	{
		return Directions.FirstOrDefault(item => item.Value == value.ToString()) ?? Directions[0];
	}

	private static T ParseEnum<T>(string? value, T fallback) where T : struct
	{
		T result;
		return !string.IsNullOrEmpty(value) && Enum.TryParse(value, out result) ? result : fallback;
	}
}
