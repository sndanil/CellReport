namespace CellReport.Core
{
	/// <summary>
	/// Чем Avalonia печатает на Windows. На Linux оба значения печатаются через PDF.
	/// </summary>
	public enum PrintEngine
	{
		Gdi = 0,
		Skia = 1
	}
}
