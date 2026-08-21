using SkiaSharp;

namespace CellReport.Presenters.Skia
{
	internal static class SkiaColorFilters
	{
		public static SKColorFilter CreateInvert()
		{
			return SKColorFilter.CreateColorMatrix(new float[]
			{
				-1f,  0f,  0f, 0f, 1f,
				 0f, -1f,  0f, 0f, 1f,
				 0f,  0f, -1f, 0f, 1f,
				 0f,  0f,  0f, 1f, 0f
			});
		}
	}
}
