using CellReport.Core;
using CellReport.Core.Contents;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CellReport.Presenters.Skia
{
	public static class SkiaChartRenderer
	{
		private static readonly SKColor[] Palette =
		{
			new SKColor(79, 129, 189),
			new SKColor(192, 80, 77),
			new SKColor(155, 187, 89),
			new SKColor(128, 100, 162),
			new SKColor(75, 172, 198),
			new SKColor(247, 150, 70),
			new SKColor(148, 138, 84),
			new SKColor(77, 130, 184)
		};

		public static void Render(SKCanvas canvas, Rectangle rect, ChartContent content)
		{
			Render(canvas, rect, content, false);
		}

		public static void Render(SKCanvas canvas, Rectangle rect, ChartContent content, bool invertColors)
		{
			var bounds = rect.ToRect();
			if (bounds.Width < 2 || bounds.Height < 2)
				return;

			canvas.Save();
			canvas.ClipRect(bounds);

			if (invertColors)
			{
				using (var layerPaint = new SKPaint { ColorFilter = SkiaColorFilters.CreateInvert() })
				{
					canvas.SaveLayer(layerPaint);
					RenderContent(canvas, bounds, content);
					canvas.Restore();
				}
			}
			else
				RenderContent(canvas, bounds, content);

			canvas.Restore();
		}

		private static void RenderContent(SKCanvas canvas, SKRect bounds, ChartContent content)
		{
			using (var bgPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.White })
				canvas.DrawRect(bounds, bgPaint);

			using (var borderPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.LightGray, StrokeWidth = 1 })
				canvas.DrawRect(bounds, borderPaint);

			var seriesList = content.GetSeries().ToList();
			if (seriesList.Count == 0)
			{
				canvas.Restore();
				return;
			}

			var legendHeight = content.ShowLegend ? 16f + seriesList.Count * 14f : 0f;
			var plotRect = new SKRect(
				bounds.Left + 6,
				bounds.Top + 6,
				bounds.Right - 6,
				bounds.Bottom - 6 - legendHeight);

			if (plotRect.Width < 4 || plotRect.Height < 4)
			{
				canvas.Restore();
				return;
			}

			var chartType = seriesList[0].Type;
			if (IsPieType(chartType))
				DrawPieChart(canvas, plotRect, content, seriesList[0], chartType);
			else
				DrawCartesianChart(canvas, plotRect, content, seriesList);

			if (content.ShowLegend)
				DrawLegend(canvas, new SKRect(bounds.Left, plotRect.Bottom + 4, bounds.Right, bounds.Bottom), seriesList);

			canvas.Restore();
		}

		private static bool IsPieType(ChartContent.SeriesChartType type)
		{
			return type == ChartContent.SeriesChartType.Pie
				|| type == ChartContent.SeriesChartType.Doughnut;
		}

		private static void DrawLegend(SKCanvas canvas, SKRect rect, List<ChartContent.Series> seriesList)
		{
			using (var textPaint = CreateTextPaint(10))
			{
			var x = rect.Left + 4;
			var y = rect.Top + 12;

			for (int i = 0; i < seriesList.Count; i++)
			{
				using (var colorPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = Palette[i % Palette.Length] })
				{
				canvas.DrawRect(new SKRect(x, y - 8, x + 10, y + 2), colorPaint);
				}
				canvas.DrawText(seriesList[i].Name ?? string.Empty, x + 14, y, textPaint);
				x += 80;
				if (x > rect.Right - 60)
				{
					x = rect.Left + 4;
					y += 14;
				}
			}
			}
		}

		private static void DrawCartesianChart(
			SKCanvas canvas,
			SKRect plotRect,
			ChartContent content,
			List<ChartContent.Series> seriesList)
		{
			var categories = GetCategories(content, seriesList);
			if (categories.Count == 0)
				return;

			var isHorizontal = seriesList.Any(s =>
				s.Type == ChartContent.SeriesChartType.Bar
				|| s.Type == ChartContent.SeriesChartType.StackedBar
				|| s.Type == ChartContent.SeriesChartType.StackedBar100
				|| s.Type == ChartContent.SeriesChartType.RangeBar);

			var isStacked = seriesList.Any(s =>
				s.Type == ChartContent.SeriesChartType.StackedColumn
				|| s.Type == ChartContent.SeriesChartType.StackedBar
				|| s.Type == ChartContent.SeriesChartType.StackedArea);

			var isPercentStacked = seriesList.Any(s =>
				s.Type == ChartContent.SeriesChartType.StackedColumn100
				|| s.Type == ChartContent.SeriesChartType.StackedBar100
				|| s.Type == ChartContent.SeriesChartType.StackedArea100);

			var axisPadLeft = 36f;
			var axisPadBottom = 22f;
			var chartArea = new SKRect(
				plotRect.Left + axisPadLeft,
				plotRect.Top + 4,
				plotRect.Right - 4,
				plotRect.Bottom - axisPadBottom);

			if (chartArea.Width < 4 || chartArea.Height < 4)
				return;

			var values = BuildValueMatrix(content, seriesList, categories.Count, isPercentStacked);
			var minValue = 0d;
			var maxValue = GetMaxValue(values, isStacked, isPercentStacked);
			if (maxValue <= minValue)
				maxValue = minValue + 1;

			DrawAxes(canvas, chartArea, minValue, maxValue, categories, isHorizontal);

			for (int s = 0; s < seriesList.Count; s++)
			{
				var series = seriesList[s];
				var color = Palette[s % Palette.Length];

				switch (series.Type)
				{
					case ChartContent.SeriesChartType.Line:
					case ChartContent.SeriesChartType.FastLine:
					case ChartContent.SeriesChartType.Spline:
					case ChartContent.SeriesChartType.StepLine:
						DrawLineSeries(canvas, chartArea, values[s], categories.Count, minValue, maxValue, color, isHorizontal);
						break;

					case ChartContent.SeriesChartType.Area:
					case ChartContent.SeriesChartType.SplineArea:
					case ChartContent.SeriesChartType.StackedArea:
					case ChartContent.SeriesChartType.StackedArea100:
						DrawAreaSeries(canvas, chartArea, values[s], categories.Count, minValue, maxValue, color, isHorizontal);
						break;

					case ChartContent.SeriesChartType.Point:
					case ChartContent.SeriesChartType.FastPoint:
					case ChartContent.SeriesChartType.Bubble:
						DrawPointSeries(canvas, chartArea, values[s], categories.Count, minValue, maxValue, color, isHorizontal);
						break;

					case ChartContent.SeriesChartType.Bar:
					case ChartContent.SeriesChartType.StackedBar:
					case ChartContent.SeriesChartType.StackedBar100:
						DrawBarSeries(canvas, chartArea, values, s, categories.Count, minValue, maxValue, color, isHorizontal, isStacked);
						break;

					default:
						DrawColumnSeries(canvas, chartArea, values, s, categories.Count, minValue, maxValue, color, isHorizontal, isStacked);
						break;
				}
			}
		}

		private static void DrawAxes(
			SKCanvas canvas,
			SKRect chartArea,
			double minValue,
			double maxValue,
			List<string> categories,
			bool isHorizontal)
		{
			using (var axisPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.Gray, StrokeWidth = 1 })
			using (var gridPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(220, 220, 220), StrokeWidth = 1 })
			using (var textPaint = CreateTextPaint(9))
			{
			canvas.DrawLine(chartArea.Left, chartArea.Top, chartArea.Left, chartArea.Bottom, axisPaint);
			canvas.DrawLine(chartArea.Left, chartArea.Bottom, chartArea.Right, chartArea.Bottom, axisPaint);

			const int gridLines = 4;
			for (int i = 0; i <= gridLines; i++)
			{
				var t = i / (float)gridLines;
				var value = minValue + (maxValue - minValue) * (1 - t);

				if (isHorizontal)
				{
					var x = chartArea.Left + chartArea.Width * t;
					canvas.DrawLine(x, chartArea.Top, x, chartArea.Bottom, gridPaint);
					canvas.DrawText(FormatValue(value), x + 2, chartArea.Bottom + 14, textPaint);
				}
				else
				{
					var y = chartArea.Top + chartArea.Height * t;
					canvas.DrawLine(chartArea.Left, y, chartArea.Right, y, gridPaint);
					canvas.DrawText(FormatValue(value), chartArea.Left - 34, y + 3, textPaint);
				}
			}

			var catCount = Math.Max(categories.Count, 1);
			for (int i = 0; i < categories.Count; i++)
			{
				var t = (i + 0.5f) / catCount;
				var label = categories[i];
				if (string.IsNullOrEmpty(label))
					continue;

				if (isHorizontal)
				{
					var y = chartArea.Top + chartArea.Height * t;
					canvas.DrawText(label, chartArea.Left - 34, y + 3, textPaint);
				}
				else
				{
					var x = chartArea.Left + chartArea.Width * t;
					canvas.DrawText(label, x - 8, chartArea.Bottom + 14, textPaint);
				}
			}
			}
		}

		private static void DrawColumnSeries(
			SKCanvas canvas,
			SKRect chartArea,
			double[][] values,
			int seriesIndex,
			int categoryCount,
			double minValue,
			double maxValue,
			SKColor color,
			bool isHorizontal,
			bool isStacked)
		{
			using (var paint = new SKPaint { Style = SKPaintStyle.Fill, Color = color, IsAntialias = true })
			{
			var seriesCount = values.Length;
			var slot = isHorizontal ? chartArea.Height / categoryCount : chartArea.Width / categoryCount;
			var barSize = slot / (isStacked ? 1 : seriesCount) * 0.7f;
			var range = maxValue - minValue;

			for (int c = 0; c < categoryCount; c++)
			{
				double value;
				double baseValue = minValue;

				if (isStacked)
				{
					value = 0;
					for (int s = 0; s <= seriesIndex; s++)
						value += values[s][c];
					baseValue = minValue;
					for (int s = 0; s < seriesIndex; s++)
						baseValue += values[s][c];
				}
				else
				{
					value = values[seriesIndex][c];
				}

				var v0 = (baseValue - minValue) / range;
				var v1 = (value - minValue) / range;

				if (isHorizontal)
				{
					var centerY = chartArea.Top + slot * (c + 0.5f);
					var y = centerY - barSize / 2 + (isStacked ? 0 : (seriesIndex - (seriesCount - 1) / 2f) * barSize);
					var x0 = chartArea.Left + (float)(chartArea.Width * v0);
					var x1 = chartArea.Left + (float)(chartArea.Width * v1);
					canvas.DrawRect(SKRect.Create(Math.Min(x0, x1), y, Math.Abs(x1 - x0), barSize), paint);
				}
				else
				{
					var centerX = chartArea.Left + slot * (c + 0.5f);
					var x = centerX - barSize / 2 + (isStacked ? 0 : (seriesIndex - (seriesCount - 1) / 2f) * barSize);
					var y0 = chartArea.Bottom - (float)(chartArea.Height * v0);
					var y1 = chartArea.Bottom - (float)(chartArea.Height * v1);
					canvas.DrawRect(SKRect.Create(x, Math.Min(y0, y1), barSize, Math.Abs(y1 - y0)), paint);
				}
			}
			}
		}

		private static void DrawBarSeries(
			SKCanvas canvas,
			SKRect chartArea,
			double[][] values,
			int seriesIndex,
			int categoryCount,
			double minValue,
			double maxValue,
			SKColor color,
			bool isHorizontal,
			bool isStacked)
		{
			DrawColumnSeries(canvas, chartArea, values, seriesIndex, categoryCount, minValue, maxValue, color, true, isStacked);
		}

		private static void DrawLineSeries(
			SKCanvas canvas,
			SKRect chartArea,
			double[] values,
			int categoryCount,
			double minValue,
			double maxValue,
			SKColor color,
			bool isHorizontal)
		{
			using (var paint = new SKPaint { Style = SKPaintStyle.Stroke, Color = color, StrokeWidth = 2, IsAntialias = true })
			using (var path = new SKPath())
			{
			var range = maxValue - minValue;
			var count = Math.Max(categoryCount, 1);

			for (int i = 0; i < categoryCount; i++)
			{
				var t = (i + 0.5f) / count;
				var normalized = (values[i] - minValue) / range;

				float x, y;
				if (isHorizontal)
				{
					x = chartArea.Left + (float)(chartArea.Width * normalized);
					y = chartArea.Top + chartArea.Height * t;
				}
				else
				{
					x = chartArea.Left + chartArea.Width * t;
					y = chartArea.Bottom - (float)(chartArea.Height * normalized);
				}

				if (i == 0)
					path.MoveTo(x, y);
				else
					path.LineTo(x, y);
			}

			canvas.DrawPath(path, paint);
			}
		}

		private static void DrawAreaSeries(
			SKCanvas canvas,
			SKRect chartArea,
			double[] values,
			int categoryCount,
			double minValue,
			double maxValue,
			SKColor color,
			bool isHorizontal)
		{
			using (var fillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = color.WithAlpha(120), IsAntialias = true })
			using (var path = new SKPath())
			{
			var range = maxValue - minValue;
			var count = Math.Max(categoryCount, 1);

			if (isHorizontal)
			{
				path.MoveTo(chartArea.Left, chartArea.Top);
				for (int i = 0; i < categoryCount; i++)
				{
					var t = (i + 0.5f) / count;
					var normalized = (values[i] - minValue) / range;
					path.LineTo(chartArea.Left + (float)(chartArea.Width * normalized), chartArea.Top + chartArea.Height * t);
				}
				path.LineTo(chartArea.Left, chartArea.Bottom);
			}
			else
			{
				path.MoveTo(chartArea.Left, chartArea.Bottom);
				for (int i = 0; i < categoryCount; i++)
				{
					var t = (i + 0.5f) / count;
					var normalized = (values[i] - minValue) / range;
					path.LineTo(chartArea.Left + chartArea.Width * t, chartArea.Bottom - (float)(chartArea.Height * normalized));
				}
				path.LineTo(chartArea.Right, chartArea.Bottom);
			}

			path.Close();
			canvas.DrawPath(path, fillPaint);
			}
			DrawLineSeries(canvas, chartArea, values, categoryCount, minValue, maxValue, color, isHorizontal);
		}

		private static void DrawPointSeries(
			SKCanvas canvas,
			SKRect chartArea,
			double[] values,
			int categoryCount,
			double minValue,
			double maxValue,
			SKColor color,
			bool isHorizontal)
		{
			using (var paint = new SKPaint { Style = SKPaintStyle.Fill, Color = color, IsAntialias = true })
			{
			var range = maxValue - minValue;
			var count = Math.Max(categoryCount, 1);

			for (int i = 0; i < categoryCount; i++)
			{
				var t = (i + 0.5f) / count;
				var normalized = (values[i] - minValue) / range;

				float x, y;
				if (isHorizontal)
				{
					x = chartArea.Left + (float)(chartArea.Width * normalized);
					y = chartArea.Top + chartArea.Height * t;
				}
				else
				{
					x = chartArea.Left + chartArea.Width * t;
					y = chartArea.Bottom - (float)(chartArea.Height * normalized);
				}

				canvas.DrawCircle(x, y, 3, paint);
			}
			}
		}

		private static void DrawPieChart(
			SKCanvas canvas,
			SKRect plotRect,
			ChartContent content,
			ChartContent.Series series,
			ChartContent.SeriesChartType chartType)
		{
			var xData = content.GetXDataBySeries(series) ?? Array.Empty<object>();
			var yData = content.GetYDataBySeries(series) ?? Array.Empty<object>();
			var count = Math.Min(xData.Length, yData.Length);
			if (count == 0)
				return;

			var values = new double[count];
			var total = 0d;
			for (int i = 0; i < count; i++)
			{
				values[i] = ToDouble(yData[i]);
				total += Math.Max(0, values[i]);
			}

			if (total <= 0)
				return;

			var centerX = plotRect.MidX;
			var centerY = plotRect.MidY;
			var radius = Math.Min(plotRect.Width, plotRect.Height) * 0.4f;
			var innerRadius = chartType == ChartContent.SeriesChartType.Doughnut ? radius * 0.55f : 0f;
			var startAngle = -90f;

			for (int i = 0; i < count; i++)
			{
				var sweep = (float)(values[i] / total * 360);
				using (var paint = new SKPaint { Style = SKPaintStyle.Fill, Color = Palette[i % Palette.Length], IsAntialias = true })
				using (var path = new SKPath())
				{
				var rect = new SKRect(centerX - radius, centerY - radius, centerX + radius, centerY + radius);

				if (innerRadius > 0)
				{
					path.ArcTo(rect, startAngle, sweep, false);
					var innerRect = new SKRect(centerX - innerRadius, centerY - innerRadius, centerX + innerRadius, centerY + innerRadius);
					path.ArcTo(innerRect, startAngle + sweep, -sweep, false);
					path.Close();
				}
				else
				{
					path.MoveTo(centerX, centerY);
					path.ArcTo(rect, startAngle, sweep, false);
					path.Close();
				}

				canvas.DrawPath(path, paint);
				}
				startAngle += sweep;
			}
		}

		private static List<string> GetCategories(ChartContent content, List<ChartContent.Series> seriesList)
		{
			var first = seriesList[0];
			var xData = content.GetXDataBySeries(first) ?? Array.Empty<object>();
			return xData.Select(ToLabel).ToList();
		}

		private static double[][] BuildValueMatrix(
			ChartContent content,
			List<ChartContent.Series> seriesList,
			int categoryCount,
			bool isPercentStacked)
		{
			var result = new double[seriesList.Count][];
			for (int s = 0; s < seriesList.Count; s++)
			{
				var yData = content.GetYDataBySeries(seriesList[s]) ?? Array.Empty<object>();
				result[s] = new double[categoryCount];
				for (int i = 0; i < categoryCount; i++)
					result[s][i] = i < yData.Length ? ToDouble(yData[i]) : 0;
			}

			if (isPercentStacked)
			{
				for (int c = 0; c < categoryCount; c++)
				{
					var sum = 0d;
					for (int s = 0; s < seriesList.Count; s++)
						sum += result[s][c];
					if (sum > 0)
					{
						for (int s = 0; s < seriesList.Count; s++)
							result[s][c] = result[s][c] / sum * 100;
					}
				}
			}

			return result;
		}

		private static double GetMaxValue(double[][] values, bool isStacked, bool isPercentStacked)
		{
			if (isPercentStacked)
				return 100;

			var categoryCount = values[0].Length;
			if (isStacked)
			{
				var max = 0d;
				for (int c = 0; c < categoryCount; c++)
				{
					var sum = 0d;
					for (int s = 0; s < values.Length; s++)
						sum += values[s][c];
					max = Math.Max(max, sum);
				}
				return max;
			}

			var flatMax = 0d;
			foreach (var row in values)
				foreach (var value in row)
					flatMax = Math.Max(flatMax, value);
			return flatMax;
		}

		private static SKPaint CreateTextPaint(float size)
		{
			return new SKPaint
			{
				Color = SKColors.Black,
				TextSize = size,
				IsAntialias = true,
				Typeface = SKTypeface.FromFamilyName("Segoe UI")
			};
		}

		private static string FormatValue(double value)
		{
			if (Math.Abs(value) >= 1000)
				return value.ToString("0", CultureInfo.InvariantCulture);
			if (Math.Abs(value - Math.Round(value)) < 0.01)
				return value.ToString("0", CultureInfo.InvariantCulture);
			return value.ToString("0.##", CultureInfo.InvariantCulture);
		}

		private static double ToDouble(object value)
		{
			if (value == null)
				return 0;
			if (value is double d)
				return d;
			if (value is decimal dec)
				return (double)dec;
			if (value is float f)
				return f;
			if (value is int i)
				return i;
			if (value is long l)
				return l;
			return Convert.ToDouble(value, CultureInfo.InvariantCulture);
		}

		private static string ToLabel(object value) => value?.ToString() ?? string.Empty;
	}
}
