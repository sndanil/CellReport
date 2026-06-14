using CellReport.Localization;

namespace CellReport.Avalonia.Services
{
	public static class ReportLocalization
	{
		static ReportLocalization()
		{
			LocalizationManager.AddDefaultString("Report/ExportToExcel", "Экспорт в Excel");
			LocalizationManager.AddDefaultString("Report/ExportToHtml", "Экспорт в HTML");
			LocalizationManager.AddDefaultString("Report/ExportToImage", "Экспорт в изображение");
			LocalizationManager.AddDefaultString("Report/Search", "Поиск");
			LocalizationManager.AddDefaultString("Report/GoToLine", "Перейти к строке");
			LocalizationManager.AddDefaultString("Report/Save", "Сохранить");
			LocalizationManager.AddDefaultString("Report/BookMarks", "Закладки");
			LocalizationManager.AddDefaultString("Report/ShowHideGrid", "Отобразить или скрыть сетку");
			LocalizationManager.AddDefaultString("Report/ShowHideHeaders", "Отобразить или скрыть заголовки строк и столбцов");
			LocalizationManager.AddDefaultString("Report/Scale", "Масштаб");

			LocalizationManager.AddDefaultString("Report/Search/Title", "Поиск");
			LocalizationManager.AddDefaultString("Report/Search/FindWhat", "Найти");
			LocalizationManager.AddDefaultString("Report/Search/MatchCase", "Учитывать регистр");
			LocalizationManager.AddDefaultString("Report/Search/All", "Найти все");
			LocalizationManager.AddDefaultString("Report/Search/Next", "Найти далее");
			LocalizationManager.AddDefaultString("Report/Search/Close", "Закрыть");
			LocalizationManager.AddDefaultString("Report/Search/LookOver", "Просматривать");
			LocalizationManager.AddDefaultString("Report/Search/LookOver/ByRows", "По строкам");
			LocalizationManager.AddDefaultString("Report/Search/LookOver/ByColumns", "По столбцам");
			LocalizationManager.AddDefaultString("Report/Search/Direction", "Направление");
			LocalizationManager.AddDefaultString("Report/Search/Direction/Forward", "Вперед");
			LocalizationManager.AddDefaultString("Report/Search/Direction/Backward", "Назад");
			LocalizationManager.AddDefaultString("Report/Search/Result/Content", "Содержимое");
			LocalizationManager.AddDefaultString("Report/Search/Result/Row", "Строка");
			LocalizationManager.AddDefaultString("Report/Search/Result/Column", "Столбец");
			LocalizationManager.AddDefaultString("Report/Search/NotFound", "Искомая комбинация не найдена.");
			LocalizationManager.AddDefaultString("Report/GoToRow/Title", "Перейти к строке");
			LocalizationManager.AddDefaultString("Report/GoToRow/RowNumber", "Номер строки");
			LocalizationManager.AddDefaultString("Report/GoToRow/Ok", "ОК");
			LocalizationManager.AddDefaultString("Report/GoToRow/Cancel", "Отмена");
			LocalizationManager.AddDefaultString("Report/Export/Error", "Невозможно сохранить файл. Возможно, файл занят другим приложением.");
			LocalizationManager.AddDefaultString("Report/Export/OpenError", "Не удалось запустить приложение, ассоциированное с файлом.");
		}

		public static string Get(string id) => LocalizationManager.GetLocalString(id);
	}
}
