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
			LocalizationManager.AddDefaultString("Report/ExportToPdf", "Экспорт в PDF");
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

			LocalizationManager.AddDefaultString("Report/Edit/Mode", "Заполнение");
			LocalizationManager.AddDefaultString("Report/Edit/ReadOnly", "Только чтение");
			LocalizationManager.AddDefaultString("Report/Edit/Marked", "Только отмеченные ячейки");
			LocalizationManager.AddDefaultString("Report/Edit/All", "Все ячейки");
			LocalizationManager.AddDefaultString("Report/Edit/Frame", "Рамка");
			LocalizationManager.AddDefaultString("Report/Edit/Background", "Фон");
			LocalizationManager.AddDefaultString("Report/Edit/Placeholder", "Плейсхолдер");
			LocalizationManager.AddDefaultString("Report/Edit/Tooltip", "Подсказка при наведении");
			LocalizationManager.AddDefaultString("Report/Edit/TooltipFormat", "Можно заполнить: {0}");
			LocalizationManager.AddDefaultString("Report/Edit/DefaultPlaceholder", "Введите значение");
			LocalizationManager.AddDefaultString("Report/Edit/EditCell", "Заполнить ячейку");
			LocalizationManager.AddDefaultString("Report/Edit/InvalidNumber", "Введите число");
			LocalizationManager.AddDefaultString("Report/Edit/InvalidDate", "Введите дату в формате ДД.ММ.ГГГГ");
			LocalizationManager.AddDefaultString("Report/Edit/TooLong", "Слишком длинное значение");
			LocalizationManager.AddDefaultString("Report/Edit/NotEditable", "Ячейка недоступна для заполнения");
			LocalizationManager.AddDefaultString("Report/Edit/Computed", "Поле вычисляется и не может быть изменено");
			LocalizationManager.AddDefaultString("Report/Edit/Undo", "Отменить ввод");
			LocalizationManager.AddDefaultString("Report/Edit/Redo", "Повторить ввод");
			LocalizationManager.AddDefaultString("Report/Edit/NoFields", "В шаблоне нет заполняемых ячеек");

			LocalizationManager.AddDefaultString("Report/Print", "Печать");
			LocalizationManager.AddDefaultString("Report/CustomPrint", "Выборочная печать");
			LocalizationManager.AddDefaultString("Report/Preview", "Предварительный просмотр");
			LocalizationManager.AddDefaultString("Report/PageSettings", "Параметры страницы");
			LocalizationManager.AddDefaultString("Report/AutoFitPage", "Автоширина по странице");
			LocalizationManager.AddDefaultString("Report/Print/NotSupported", "Печать поддерживается только в Windows.");
			LocalizationManager.AddDefaultString("Report/Print/Printer", "Принтер");
			LocalizationManager.AddDefaultString("Report/Preview/Paper", "{0} · {1} × {2} мм");
			LocalizationManager.AddDefaultString("Report/Preview/CustomPaper", "Произвольная");
			LocalizationManager.AddDefaultString("Report/Print/PageRange", "Диапазон страниц");
			LocalizationManager.AddDefaultString("Report/Print/From", "с");
			LocalizationManager.AddDefaultString("Report/Print/To", "по");
			LocalizationManager.AddDefaultString("Report/Print/OfPages", "из {0}");
			LocalizationManager.AddDefaultString("Report/PageSetup/Margins", "Поля (мм)");
			LocalizationManager.AddDefaultString("Report/PageSetup/Left", "Слева");
			LocalizationManager.AddDefaultString("Report/PageSetup/Right", "Справа");
			LocalizationManager.AddDefaultString("Report/PageSetup/Top", "Сверху");
			LocalizationManager.AddDefaultString("Report/PageSetup/Bottom", "Снизу");
			LocalizationManager.AddDefaultString("Report/PageSetup/Landscape", "Альбомная ориентация");
			LocalizationManager.AddDefaultString("Report/PageSetup/Copies", "Копии");
		}

		public static string Get(string id) => LocalizationManager.GetLocalString(id);
	}
}
