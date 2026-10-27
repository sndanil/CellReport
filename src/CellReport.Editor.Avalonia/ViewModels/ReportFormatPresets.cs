using System.Collections.Generic;

namespace CellReport.Editor.Avalonia.ViewModels;

/// <summary>
/// Типовые форматы из реальных шаблонов отчётов (Base/Config) — подсказки для полей «Формат».
/// </summary>
public static class ReportFormatPresets
{
	public static IReadOnlyList<string> Numbers { get; } = new[]
	{
		"#,##0.00",
		"#,##0",
		"0.00",
		"0",
		"#0",
		"00",
		"#,##0.###",
		"0.###;-0.###;-",
		"0.00;-0.00;-",
		"0;-0;-",
		"#,##0;-#,##0;-",
		"#,##0.00;-#,##0.00;-",
		"#,##0.000;-#,##0.000;-",
		"#,##0.0;-#,##0.0;-",
		"#,##0;[Red]\\-#,##0;\\-",
		"#,##0.000;[Red]\\-#,##0.000;\\-",
		"#,##0.00;-#,##0.00; ",
		"#,##0;-#,##0; ",
		"#,##0.00;-#,##0.00;''",
		"#,##0;-#,##0;''",
		"#,##0.000;-#,##0.000; ",
		"0.00%",
		"0%",
		"numberString",
		"currencyString|RUR",
		"currencyString|UAH",
		"currencyString|KZT"
	};

	public static IReadOnlyList<string> Dates { get; } = new[]
	{
		"dd.MM.yyyy",
		"dd.MM.yyyy HH:mm",
		"dd.MM.yy",
		"dd.MM.yyyy HH:mm:ss",
		"dd.MM.yyyy'г. ('dddd')'",
		"dd MMMM yyyy",
		"dd",
		"MMMM",
		"yyyy",
		"HH:mm",
		"HH:mm dd.MM.yy"
	};

	/// <summary>
	/// Все подсказки: параметр может менять свойство любого типа, поэтому список общий.
	/// </summary>
	public static IReadOnlyList<string> All { get; } = BuildAll();

	private static IReadOnlyList<string> BuildAll()
	{
		var result = new List<string>(Numbers);
		foreach (var format in Dates)
		{
			if (!result.Contains(format))
				result.Add(format);
		}

		return result;
	}
}
