using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CellReport.Core;
using CellReport.Core.Contents;
using CoreFontStyle = CellReport.Core.FontStyle;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CellReport.Editor.Avalonia.ViewModels;

/// <summary>
/// Редактор стиля содержимого ячейки. Стили неизменяемые, поэтому при применении
/// создаётся новый объект стиля и подменяет старый по идентификатору.
/// </summary>
public partial class StyleEditorViewModel : ObservableObject
{
	private readonly MainWindowViewModel _owner;
	private readonly BaseContent _content;
	private readonly BaseStyle? _original;
	private readonly Type _styleType;

	public StyleEditorViewModel(MainWindowViewModel owner, BaseContent content)
	{
		_owner = owner;
		_content = content;
		_original = content.GetStyle();
		_styleType = StyleTypeOf(content);

		OverrideCount = content.GetParams().Count(item => (item.Target ?? string.Empty).StartsWith("Style", StringComparison.Ordinal));

		if (_original == null || string.IsNullOrEmpty(_original.ID))
			_original = null;

		Id = _original?.ID ?? NextStyleId();
		Title = "Стиль: " + StyleTypeTitle(_styleType);

		var style = content.GetActualStyle() as TextContentStyle ?? TextBasedContent.DefaultStyle as TextContentStyle;
		if (content.GetActualStyle() is TextContentStyle textStyle)
			style = textStyle;

		FontFamily = style?.Font.Family ?? FontInfo.Default.Family;
		FontSizeText = (style?.Font.Size ?? FontInfo.Default.Size).ToString(CultureInfo.CurrentCulture);
		Bold = style != null && (style.Font.Style & CoreFontStyle.Bold) == CoreFontStyle.Bold;
		Italic = style != null && (style.Font.Style & CoreFontStyle.Italic) == CoreFontStyle.Italic;
		Underline = style != null && (style.Font.Style & CoreFontStyle.Underline) == CoreFontStyle.Underline;
		Strikeout = style != null && (style.Font.Style & CoreFontStyle.Strikeout) == CoreFontStyle.Strikeout;
		Color = style?.Color ?? "Black";
		AngleText = (style?.Angle ?? 0).ToString(CultureInfo.CurrentCulture);
		SelectedHorizontal = FindHorizontal(style?.HAlignment ?? HorizontalAlignment.Left);
		SelectedVertical = FindVertical(style?.VAlignment ?? VerticalAlignment.Middle);
		SelectedDirection = FindDirection(style?.Direction ?? Direction.LeftToRight);

		if (content.GetActualStyle() is DecimalContentStyle decimalStyle)
		{
			Format = decimalStyle.Format ?? string.Empty;
			Culture = decimalStyle.Culture ?? string.Empty;
			NegativeColor = decimalStyle.NegativeColor ?? "Red";
		}
		else if (content.GetActualStyle() is DateTimeContentStyle dateStyle)
		{
			Format = dateStyle.Format ?? string.Empty;
			Culture = dateStyle.Culture ?? string.Empty;
		}

		UpdatePreview();
	}

	public string Title { get; }

	/// <summary>
	/// Сколько параметров содержимого перекрывают оформление стиля.
	/// </summary>
	public int OverrideCount { get; }

	public bool HasOverrides => OverrideCount > 0;

	public string OverridesHint => "Убрать параметры оформления содержимого (" + OverrideCount + ")";

	[ObservableProperty]
	private bool _replaceOverrides = true;

	public bool IsText => _styleType == typeof(TextContentStyle);

	public bool IsDecimal => _styleType == typeof(DecimalContentStyle);

	public bool IsDate => _styleType == typeof(DateTimeContentStyle);

	public IReadOnlyList<string> Fonts => _owner.Fonts;

	public IReadOnlyList<string> FontSizes => _owner.FontSizes;

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

	public ObservableCollection<string> Cultures => _owner.Inspector.Cultures;

	public IReadOnlyList<string> FormatPresets => IsDecimal
		? ReportFormatPresets.Numbers
		: ReportFormatPresets.Dates;

	[ObservableProperty]
	private string _id = string.Empty;

	[ObservableProperty]
	private string _fontFamily = string.Empty;

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
	private IBrush _previewBrush = Brushes.Black;

	[ObservableProperty]
	private double _previewFontSize = 12;

	[ObservableProperty]
	private global::Avalonia.Media.FontWeight _previewWeight = global::Avalonia.Media.FontWeight.Normal;

	[ObservableProperty]
	private global::Avalonia.Media.FontStyle _previewFontStyle = global::Avalonia.Media.FontStyle.Normal;

	[ObservableProperty]
	private string _errorMessage = string.Empty;

	public bool HasError => ErrorMessage.Length > 0;

	[ObservableProperty]
	private string _cultureName = string.Empty;

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

		float size;
		if (!float.TryParse(FontSizeText, NumberStyles.Float, CultureInfo.CurrentCulture, out size) || size <= 0)
		{
			ErrorMessage = "Размер шрифта должен быть больше нуля";
			return false;
		}

		float angle;
		if (!float.TryParse(AngleText, NumberStyles.Float, CultureInfo.CurrentCulture, out angle))
			angle = 0;

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

		var existing = manager.GetStyles().FirstOrDefault(item => item.GetType() == _styleType && item.ID == id);
		if (existing != null && !ReferenceEquals(existing, _original))
		{
			ErrorMessage = "Стиль «" + id + "» уже существует";
			return false;
		}

		var style = BuildStyle(id, size, angle);
		if (style == null)
		{
			ErrorMessage = "Этот тип содержимого не поддерживает редактирование стиля";
			return false;
		}

		_owner.CommitDocumentChange(() =>
		{
			if (existing != null)
				manager.ReplaceStyle(style);
			else
				manager.AddStyle(style);

			_content.StyleID = id;

			if (ReplaceOverrides && HasOverrides)
			{
				foreach (var param in _content.GetParams()
					.Where(item => (item.Target ?? string.Empty).StartsWith("Style", StringComparison.Ordinal))
					.ToList())
				{
					_content.RemoveParam(param);
				}
			}
		});

		return true;
	}

	private BaseStyle? BuildStyle(string id, float size, float angle)
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
		var color = string.IsNullOrWhiteSpace(Color) ? "Black" : Color;

		BaseStyle style;
		if (IsText)
		{
			style = new TextContentStyle(horizontal, vertical, font, color, angle, direction);
		}
		else if (IsDecimal)
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
			return null;
		}

		style.ID = id;
		return style;
	}

	private void UpdatePreview()
	{
		PreviewText = FormatSample();

		float size;
		PreviewFontSize = float.TryParse(FontSizeText, NumberStyles.Float, CultureInfo.CurrentCulture, out size) && size > 0
			? size
			: 10;
		PreviewWeight = Bold ? global::Avalonia.Media.FontWeight.Bold : global::Avalonia.Media.FontWeight.Normal;
		PreviewFontStyle = Italic ? global::Avalonia.Media.FontStyle.Italic : global::Avalonia.Media.FontStyle.Normal;

		try
		{
			PreviewBrush = string.IsNullOrWhiteSpace(Color) ? Brushes.Black : new SolidColorBrush(global::Avalonia.Media.Color.Parse(Color));
		}
		catch (Exception)
		{
			PreviewBrush = Brushes.Black;
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

	private string NextStyleId()
	{
		var manager = _owner.Manager;
		var index = 1;
		while (manager != null && manager.GetStyles().Any(item => item.ID == "style" + index.ToString(CultureInfo.InvariantCulture)))
			index++;

		return "style" + index.ToString(CultureInfo.InvariantCulture);
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

	private static Type StyleTypeOf(BaseContent content)
	{
		if (content is DecimalContent)
			return typeof(DecimalContentStyle);
		if (content is DateTimeContent)
			return typeof(DateTimeContentStyle);
		return typeof(TextContentStyle);
	}

	private static string StyleTypeTitle(Type type)
	{
		if (type == typeof(DecimalContentStyle))
			return "число";
		if (type == typeof(DateTimeContentStyle))
			return "дата и время";
		return "текст";
	}
}
