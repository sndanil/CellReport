using System;

namespace CellReport.Core
{
	/// <summary>
	/// Режим заполнения ячеек в просмотрщике.
	/// </summary>
	public enum CellEditMode
	{
		/// <summary>Только чтение (по умолчанию).</summary>
		None = 0,
		/// <summary>Редактируются только ячейки с <see cref="Cell.Editable"/>.</summary>
		MarkedCells = 1,
		/// <summary>Редактируется любая поддерживаемая ячейка.</summary>
		AllCells = 2
	}

	/// <summary>
	/// Вид значения в редактируемой ячейке.
	/// </summary>
	public enum CellEditValueKind
	{
		Text,
		Decimal,
		DateTime
	}

	/// <summary>
	/// Причина, по которой ячейку нельзя редактировать, или ошибка ввода.
	/// </summary>
	public enum CellEditError
	{
		None = 0,
		NotEditable,
		Computed,
		UnsupportedContent,
		InvalidNumber,
		InvalidDate,
		TooLong
	}
}
