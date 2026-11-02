using CellReport.Core;
using CellReport.Core.Contents;

namespace CellReport.Presenters
{
	/// <summary>
	/// Параметры рисования текста с переполнением.
	/// </summary>
	public sealed class TextRenderOptions
	{
		/// <summary>
		/// Прямоугольник укладки текста (может выходить за ячейку).
		/// </summary>
		public Rectangle LayoutRect { get; set; }

		/// <summary>
		/// Прямоугольник обрезки. Пустой — обрезать только по канве.
		/// </summary>
		public Rectangle ClipRect { get; set; }

		/// <summary>
		/// Рисовать одной строкой, без переноса.
		/// </summary>
		public bool NoWrap { get; set; }

		/// <summary>
		/// Обрезать с многоточием по ширине <see cref="ClipRect"/>.
		/// </summary>
		public bool Ellipsis { get; set; }
	}

	/// <summary>
	/// Параметры рисования изображения с переполнением.
	/// </summary>
	public sealed class ImageRenderOptions
	{
		/// <summary>
		/// База для устаревшего fallback-рисования (старые рендереры).
		/// </summary>
		public Rectangle CellRect { get; set; }

		/// <summary>
		/// Итоговый прямоугольник изображения.
		/// </summary>
		public Rectangle Rect { get; set; }

		/// <summary>
		/// Прямоугольник обрезки.
		/// </summary>
		public Rectangle ClipRect { get; set; }
	}
}
