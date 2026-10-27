namespace CellReport.Core
{
	/// <summary>
	/// Результат «сжатия» стилей шаблона по внешним файлам стилей.
	/// </summary>
	public class StyleCompressionResult
	{
		/// <summary>Сколько стилей удалено из шаблона.</summary>
		public int Removed { get; set; }

		/// <summary>Сколько стилей с другим идентификатором перенаправлено на внешний.</summary>
		public int Remapped { get; set; }

		/// <summary>Сколько ссылок в ячейках и колонтитулах изменено.</summary>
		public int References { get; set; }
	}
}
