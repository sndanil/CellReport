using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Avalonia.Media;
using CellReport.Core;
using CellReport.Core.BookMarks;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;
using ReportRange = CellReport.Core.Range;

namespace CellReport.Editor.Avalonia.Services;

public enum TemplateIssueSeverity
{
	Error,
	Warning
}

public sealed class TemplateIssue
{
	private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#E5484D"));
	private static readonly IBrush WarningBrush = new SolidColorBrush(Color.Parse("#D9A300"));

	public TemplateIssueSeverity Severity { get; init; }

	public string Category { get; init; } = string.Empty;

	public string Message { get; init; } = string.Empty;

	public CellLocation? Cell { get; init; }

	public bool HasCell => Cell is { } cell && !cell.Equals(CellLocation.None);

	public string SeverityText => Severity == TemplateIssueSeverity.Error ? "Ошибка" : "Предупреждение";

	public string Display => (HasCell
			? "Ячейка " + Cell!.Value.Row.ToString(CultureInfo.InvariantCulture)
				+ ", " + Cell.Value.Column.ToString(CultureInfo.InvariantCulture) + ": "
			: string.Empty)
		+ Message;

	public IBrush Brush => Severity == TemplateIssueSeverity.Error ? ErrorBrush : WarningBrush;
}

public static class TemplateValidator
{
	private static readonly Lazy<HashSet<string>> KnownCultures = new(() =>
		new HashSet<string>(
			CultureInfo.GetCultures(CultureTypes.AllCultures).Select(item => item.Name),
			StringComparer.OrdinalIgnoreCase));

	private static XmlSchemaSet? _schemas;
	private static bool _schemasLoaded;

	public static IReadOnlyList<TemplateIssue> Validate(CellManager? manager)
	{
		var issues = new List<TemplateIssue>();
		if (manager == null)
			return issues;

		ValidateExpressions(issues, manager);
		ValidateCells(issues, manager);
		ValidateSections(issues, manager);
		ValidatePrintSettings(issues, manager);
		ValidateDisplaySettings(issues, manager);
		ValidateLocalization(issues, manager);
		ValidateBookMarks(issues, manager);
		ValidateProperties(issues, manager);
		ValidateXml(issues, manager);

		return issues
			.OrderByDescending(item => item.Severity == TemplateIssueSeverity.Error)
			.ThenBy(item => item.Cell?.Row ?? int.MaxValue)
			.ThenBy(item => item.Cell?.Column ?? int.MaxValue)
			.ToList();
	}

	private static void ValidateExpressions(List<TemplateIssue> issues, CellManager manager)
	{
		var expressions = manager.GetExpressions().ToList();
		var byId = new Dictionary<string, BaseExpression>(StringComparer.OrdinalIgnoreCase);

		foreach (var expression in expressions)
		{
			var id = (expression.ID ?? string.Empty).Trim();
			if (id.Length == 0)
			{
				issues.Add(Error("Выражения", "У выражения не задан идентификатор"));
				continue;
			}

			if (byId.ContainsKey(id))
				issues.Add(Error("Выражения", "Идентификатор «" + id + "» используется несколькими выражениями"));
			else
				byId.Add(id, expression);
		}

		foreach (var expression in expressions.OfType<DecimalExpression>())
		{
			var previous = expression.PrevExpression;
			if (string.IsNullOrWhiteSpace(previous))
				continue;

			if (string.Equals(previous, expression.ID, StringComparison.OrdinalIgnoreCase))
				issues.Add(Error("Выражения", "Выражение «" + expression.ID + "» ссылается само на себя"));
			else if (!byId.TryGetValue(previous, out var target))
				issues.Add(Error("Выражения", "У выражения «" + expression.ID + "» не найдено предыдущее выражение «" + previous + "»"));
			else if (target is not DecimalExpression)
				issues.Add(Warning("Выражения", "Выражение «" + expression.ID + "» ссылается на «" + previous + "», которое не является числовым"));
		}

		foreach (var expression in expressions.OfType<SumExpression>().Where(item => string.IsNullOrWhiteSpace(item.ParamName)))
			issues.Add(Warning("Выражения", "У выражения «" + expression.ID + "» не задано имя параметра"));
	}

	private static void ValidateCells(List<TemplateIssue> issues, CellManager manager)
	{
		var expressionIds = new HashSet<string>(
			manager.GetExpressions().Select(item => item.ID ?? string.Empty),
			StringComparer.OrdinalIgnoreCase);

		for (var row = 0; row < manager.RowsCount; row++)
		{
			for (var column = 0; column < manager.ColumnsCount; column++)
			{
				if (!manager.TryGetCell(row, column, out var cell) || cell == null)
					continue;

				// у объединённых ячеек обходим только «владельца», иначе одна проблема задвоится
				if (cell.Location.Row != row || cell.Location.Column != column)
					continue;

				ValidateCell(issues, manager, cell, expressionIds);
			}
		}
	}

	private static void ValidateCell(
		List<TemplateIssue> issues,
		CellManager manager,
		Cell cell,
		HashSet<string> expressionIds)
	{
		var location = cell.Location;

		if (!string.IsNullOrEmpty(cell.StyleID) && manager.GetStyleByID<CellStyle>(cell.StyleID) == null)
			issues.Add(Error("Стили", "Стиль ячейки «" + cell.StyleID + "» не найден", location));

		if ((cell.Span.RowSpan != 0 || cell.Span.ColSpan != 0)
			&& (location.Row + cell.Span.RowSpan >= manager.RowsCount
				|| location.Column + cell.Span.ColSpan >= manager.ColumnsCount))
			issues.Add(Error("Ячейки", "Объединённая ячейка выходит за границы таблицы", location));

		foreach (var content in cell.GetContents())
		{
			ValidateContentStyle(issues, manager, content, location);

			foreach (var param in content.GetParams())
			{
				if (!string.IsNullOrEmpty(param.ExpressionID) && !expressionIds.Contains(param.ExpressionID))
					issues.Add(Error("Выражения",
						"В параметре «" + param.Name + "» указано несуществующее выражение «" + param.ExpressionID + "»",
						location));

				if (!string.IsNullOrEmpty(param.Culture) && !KnownCultures.Value.Contains(param.Culture))
					issues.Add(Warning("Локализация",
						"В параметре «" + param.Name + "» неизвестная культура «" + param.Culture + "»",
						location));
			}

			switch (content)
			{
				case ImageContent image when string.IsNullOrWhiteSpace(image.ActualUri):
					issues.Add(Warning("Содержимое", "У изображения не задан адрес", location));
					break;
				case BarCodeContent barCode when string.IsNullOrWhiteSpace(barCode.GetCode()):
					issues.Add(Warning("Содержимое", "У штрих-кода не задан код", location));
					break;
			}
		}
	}

	private static void ValidateContentStyle(
		List<TemplateIssue> issues,
		CellManager manager,
		BaseContent content,
		CellLocation location)
	{
		var styleId = content.StyleID;
		if (string.IsNullOrEmpty(styleId))
			return;

		var exists = content switch
		{
			TextContent => manager.GetStyleByID<TextContentStyle>(styleId) != null,
			DecimalContent => manager.GetStyleByID<DecimalContentStyle>(styleId) != null,
			DateTimeContent => manager.GetStyleByID<DateTimeContentStyle>(styleId) != null,
			BarCodeContent => manager.GetStyleByID<BarCodeContentStyle>(styleId) != null,
			_ => true
		};

		if (!exists)
			issues.Add(Error("Стили", "Стиль содержимого «" + styleId + "» не найден", location));
	}

	private static void ValidateSections(List<TemplateIssue> issues, CellManager manager)
	{
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var section in manager.GetSections())
		{
			var name = section.Name ?? string.Empty;
			if (string.IsNullOrWhiteSpace(name))
				issues.Add(Warning("Секции", "У секции не задано имя"));
			else if (!names.Add(name + "|" + (section.ObjectName ?? string.Empty)))
				issues.Add(Warning("Секции", "Секция «" + name + "» повторяется"));

			if (section.Left < 0 || section.Top < 0
				|| section.Right >= manager.ColumnsCount || section.Bottom >= manager.RowsCount)
				issues.Add(Error("Секции", "Секция «" + name + "» выходит за границы таблицы"));
		}
	}

	private static void ValidatePrintSettings(List<TemplateIssue> issues, CellManager manager)
	{
		var print = manager.PrintSettings;
		if (print == null)
			return;

		if (print.Copies <= 0)
			issues.Add(Warning("Печать", "Количество копий должно быть больше нуля"));
		if (print.Scale <= 0)
			issues.Add(Error("Печать", "Масштаб печати должен быть больше нуля"));
		if (print.AutoFitWidthPages < 0)
			issues.Add(Warning("Печать", "Количество страниц по ширине не может быть отрицательным"));

		var margins = print.Margins;
		if (margins != null
			&& (margins.Left < 0 || margins.Right < 0 || margins.Top < 0 || margins.Bottom < 0))
			issues.Add(Warning("Печать", "Поля страницы не могут быть отрицательными"));

		ValidateRanges(issues, print.HeaderFixedRows, "заголовка", manager.RowsCount);
		ValidateRanges(issues, print.FooterFixedRows, "подвала", manager.RowsCount);
	}

	private static void ValidateRanges(
		List<TemplateIssue> issues,
		IList<ReportRange>? ranges,
		string title,
		int rowsCount)
	{
		if (ranges == null)
			return;

		foreach (var range in ranges)
		{
			if (range.From > range.To)
				issues.Add(Warning("Печать", "У фиксированных строк " + title + " начало больше конца"));
			else if (range.From < 0 || range.To >= rowsCount)
				issues.Add(Warning("Печать", "Диапазон фиксированных строк " + title + " выходит за границы таблицы"));
		}
	}

	private static void ValidateDisplaySettings(List<TemplateIssue> issues, CellManager manager)
	{
		var display = manager.DisplaySettings;
		if (display == null)
			return;

		if (display.Scale <= 0)
			issues.Add(Error("Отображение", "Масштаб отображения должен быть больше нуля"));
		if (display.FrozenColumns < 0 || display.FrozenColumns >= manager.ColumnsCount)
			issues.Add(Warning("Отображение", "Закреплённые столбцы выходят за границы таблицы"));
		if (display.FrozenRows < 0 || display.FrozenRows >= manager.RowsCount)
			issues.Add(Warning("Отображение", "Закреплённые строки выходят за границы таблицы"));
		if (!string.IsNullOrEmpty(display.GridColor) && !Color.TryParse(display.GridColor, out _))
			issues.Add(Warning("Отображение", "Не удалось разобрать цвет сетки «" + display.GridColor + "»"));
	}

	private static void ValidateLocalization(List<TemplateIssue> issues, CellManager manager)
	{
		var strings = manager.Properties?.Localization;
		if (strings == null)
			return;

		var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var items = strings.GetStrings() ?? Array.Empty<TemplateString>();

		foreach (var item in items)
		{
			if (string.IsNullOrWhiteSpace(item.ID))
				issues.Add(Warning("Локализация", "У строки локализации не задан идентификатор"));
			if (string.IsNullOrWhiteSpace(item.Culture))
				issues.Add(Warning("Локализация", "У строки локализации не задана культура"));
			else if (!KnownCultures.Value.Contains(item.Culture))
				issues.Add(Warning("Локализация", "Неизвестная культура «" + item.Culture + "»"));
			if (string.IsNullOrEmpty(item.Value))
				issues.Add(Warning("Локализация", "У строки «" + item.ID + "» пустое значение"));
			if (!keys.Add((item.Culture ?? string.Empty) + "|" + (item.ID ?? string.Empty)))
				issues.Add(Warning("Локализация", "Строка «" + item.ID + "» для культуры «" + item.Culture + "» повторяется"));
		}

		if (items.Length > 0 && string.IsNullOrWhiteSpace(strings.DefaultCulture))
			issues.Add(Warning("Локализация", "Не задана культура локализации по умолчанию"));
		else if (!string.IsNullOrWhiteSpace(strings.DefaultCulture) && !KnownCultures.Value.Contains(strings.DefaultCulture))
			issues.Add(Warning("Локализация", "Неизвестная культура по умолчанию «" + strings.DefaultCulture + "»"));
	}

	private static void ValidateBookMarks(List<TemplateIssue> issues, CellManager manager)
	{
		ValidateBookmarkSettings(issues, manager);
		Walk(manager.GetBookMarks());

		void Walk(IEnumerable<BookMark> bookMarks)
		{
			foreach (var bookMark in bookMarks)
			{
				if (bookMark.SectionID == null || bookMark.SectionID.Length == 0)
				{
					if (string.IsNullOrWhiteSpace(bookMark.Name))
						issues.Add(Warning("Закладки", "У закладки не задано имя"));
				}
				else if (bookMark.TextParamName == null || bookMark.TextParamName.Length == 0)
					issues.Add(Warning("Закладки", "У правила закладки не задан параметр заголовка"));

				var location = bookMark.Location;
				if (!location.Equals(CellLocation.None)
					&& (location.Row < 0 || location.Column < 0
						|| location.Row >= manager.RowsCount || location.Column >= manager.ColumnsCount))
					issues.Add(Warning("Закладки", "Закладка «" + bookMark.Name + "» указывает за границы таблицы"));

				Walk(bookMark.Child);
			}
		}
	}

	private static void ValidateBookmarkSettings(List<TemplateIssue> issues, CellManager manager)
	{
		var settings = manager.BookmarkSettings;
		if (settings == null)
			return;

		var sections = new HashSet<string>(
			manager.GetSections().Select(item => item.Name ?? string.Empty),
			StringComparer.OrdinalIgnoreCase);

		foreach (var pair in settings.GetLevels())
		{
			if (!sections.Contains(pair.Key))
				issues.Add(Warning("Закладки", "Уровень задан для несуществующей секции «" + pair.Key + "»"));
		}

		if (!settings.Enabled)
			return;

		if (settings.AutoSections && string.IsNullOrWhiteSpace(settings.TitleParam))
			issues.Add(Warning("Закладки", "Для автозакладок не задан параметр заголовка"));

		var format = settings.TitleFormat ?? string.Empty;
		if (format.Length > 0 && !format.Contains("{Value}") && !format.Contains("{Section}"))
			issues.Add(Warning("Закладки", "Формат заголовка не содержит подстановок {Value} или {Section}"));
	}

	private static void ValidateProperties(List<TemplateIssue> issues, CellManager manager)
	{
		var properties = manager.Properties;
		if (properties == null)
			return;

		if (properties.DefaultColumnsCount <= 0)
			issues.Add(Warning("Документ", "Количество столбцов по умолчанию должно быть больше нуля"));
		if (properties.DefaultRowsCount <= 0)
			issues.Add(Warning("Документ", "Количество строк по умолчанию должно быть больше нуля"));
		if (properties.DefaultColumnWidth <= 0)
			issues.Add(Warning("Документ", "Ширина столбца по умолчанию должна быть больше нуля"));
		if (properties.DefaultRowHeight <= 0)
			issues.Add(Warning("Документ", "Высота строки по умолчанию должна быть больше нуля"));

		if (!string.IsNullOrWhiteSpace(properties.Culture) && !KnownCultures.Value.Contains(properties.Culture))
			issues.Add(Warning("Документ", "Неизвестная культура документа «" + properties.Culture + "»"));

		var rawName = properties.RawDocumentName;
		if (TemplateLocalizer.IsFullKey(rawName))
		{
			var resolved = TemplateLocalizer.GetString(properties.Localization, rawName, properties.Culture);
			if (string.IsNullOrEmpty(resolved) || string.Equals(resolved, rawName, StringComparison.Ordinal))
				issues.Add(Warning("Локализация", "Ключ имени документа «" + rawName + "» не найден в локализации шаблона"));
		}
	}

	private static void ValidateXml(List<TemplateIssue> issues, CellManager manager)
	{
		var schemas = GetSchemas();
		if (schemas == null)
			return;

		try
		{
			using var stream = new MemoryStream();
			new CellTemplate(manager).Save(stream, TemplateType.Xml);
			stream.Position = 0;

			var document = XDocument.Load(stream);
			var messages = new List<string>();
			document.Validate(schemas, (_, args) => messages.Add(args.Message));

			foreach (var message in messages.Take(5))
				issues.Add(Error("XML", "Схема: " + message));
		}
		catch (Exception ex)
		{
			issues.Add(Error("XML", "Не удалось проверить XML: " + ex.Message));
		}
	}

	private static XmlSchemaSet? GetSchemas()
	{
		if (_schemasLoaded)
			return _schemas;

		_schemasLoaded = true;
		try
		{
			var path = Path.Combine(AppContext.BaseDirectory, "Schema.xsd");
			if (!File.Exists(path))
				return null;

			var schemas = new XmlSchemaSet();
			using var reader = XmlReader.Create(path);
			schemas.Add(string.Empty, reader);
			schemas.Compile();
			_schemas = schemas;
		}
		catch (Exception)
		{
			_schemas = null;
		}

		return _schemas;
	}

	private static TemplateIssue Error(string category, string message, CellLocation? cell = null)
	{
		return new TemplateIssue
		{
			Severity = TemplateIssueSeverity.Error,
			Category = category,
			Message = message,
			Cell = cell
		};
	}

	private static TemplateIssue Warning(string category, string message, CellLocation? cell = null)
	{
		return new TemplateIssue
		{
			Severity = TemplateIssueSeverity.Warning,
			Category = category,
			Message = message,
			Cell = cell
		};
	}
}
