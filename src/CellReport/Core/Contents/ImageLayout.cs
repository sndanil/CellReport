using System;

namespace CellReport.Core.Contents
{
	/// <summary>
	/// Способ вычисления размера изображения внутри ячейки.
	/// </summary>
	[Serializable]
	public enum ImageSizeMode
	{
		/// <summary>
		/// Текущее поведение: растянуть на всю ячейку (пропорции не сохраняются).
		/// </summary>
		Stretch = 0,

		/// <summary>
		/// Вписать в ячейку с сохранением пропорций.
		/// </summary>
		Fit = 1,

		/// <summary>
		/// Натуральный размер (пиксели изображения, переведённые в единицы отчёта).
		/// </summary>
		Actual = 2,

		/// <summary>
		/// Натуральный размер, умноженный на <see cref="Contents.ImageContent.Scale"/> (в процентах).
		/// </summary>
		Scale = 3
	}

	/// <summary>
	/// Положение изображения относительно ячейки.
	/// </summary>
	[Serializable]
	public enum ImageAnchor
	{
		TopLeft = 0,
		TopCenter = 1,
		TopRight = 2,
		MiddleLeft = 3,
		Center = 4,
		MiddleRight = 5,
		BottomLeft = 6,
		BottomCenter = 7,
		BottomRight = 8
	}
}
