using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using CellReport.Editor.Avalonia.ViewModels;

namespace CellReport.Editor.Avalonia.CodeEditor;

/// <summary>
/// Левое поле редактора с отметками строк, в которых есть ошибки.
/// </summary>
public sealed class ErrorMargin : AbstractMargin
{
	private static readonly IBrush LightMarker = new SolidColorBrush(Color.Parse("#D32F2F"));
	private static readonly IBrush DarkMarker = new SolidColorBrush(Color.Parse("#FF6B6B"));

	private readonly IReadOnlyList<SchemaError> _errors;

	public ErrorMargin(IReadOnlyList<SchemaError> errors)
	{
		_errors = errors;
	}

	public void Invalidate() => InvalidateVisual();

	protected override Size MeasureOverride(Size availableSize) => new(12, 0);

	public override void Render(DrawingContext context)
	{
		var textView = TextView;
		if (textView is not { VisualLinesValid: true } || _errors.Count == 0)
			return;

		var lines = CollectErrorLines();
		if (lines.Count == 0)
			return;

		var brush = textView.ActualThemeVariant == ThemeVariant.Dark ? DarkMarker : LightMarker;
		foreach (var line in textView.VisualLines)
		{
			if (!lines.Contains(line.FirstDocumentLine.LineNumber))
				continue;

			var y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.LineMiddle)
				- textView.VerticalOffset;
			context.DrawEllipse(brush, null, new Point(Bounds.Width / 2, y), 3, 3);
		}
	}

	private HashSet<int> CollectErrorLines()
	{
		var lines = new HashSet<int>();
		foreach (var error in _errors)
		{
			if (error.LineNumber > 0)
				lines.Add(error.LineNumber);
		}

		return lines;
	}
}

/// <summary>
/// Волнистое подчёркивание строк с ошибками.
/// </summary>
public sealed class ErrorSquiggleRenderer : IBackgroundRenderer
{
	private static readonly IBrush LightSquiggle = new SolidColorBrush(Color.Parse("#D32F2F"));
	private static readonly IBrush DarkSquiggle = new SolidColorBrush(Color.Parse("#FF6B6B"));

	private readonly IReadOnlyList<SchemaError> _errors;

	public ErrorSquiggleRenderer(IReadOnlyList<SchemaError> errors)
	{
		_errors = errors;
	}

	public KnownLayer Layer => KnownLayer.Selection;

	public void Draw(TextView textView, DrawingContext context)
	{
		var document = textView.Document;
		if (document == null || _errors.Count == 0)
			return;

		var brush = textView.ActualThemeVariant == ThemeVariant.Dark ? DarkSquiggle : LightSquiggle;
		var pen = new Pen(brush, 1.2);
		foreach (var error in _errors)
		{
			if (error.LineNumber <= 0 || error.LineNumber > document.LineCount)
				continue;

			var line = document.GetLineByNumber(error.LineNumber);
			var segment = new SimpleSegment(line.Offset, Math.Max(line.Length, 1));
			foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
				DrawSquiggle(context, pen, rect);
		}
	}

	private static void DrawSquiggle(DrawingContext context, Pen pen, Rect rect)
	{
		if (rect.Width <= 0)
			return;

		const double step = 4;
		var y = rect.Bottom - 1;
		var up = true;

		var geometry = new StreamGeometry();
		using (var ctx = geometry.Open())
		{
			ctx.BeginFigure(new Point(rect.Left, y), false);
			for (var x = rect.Left + step; x <= rect.Right; x += step)
			{
				ctx.LineTo(new Point(x, up ? y - 2 : y));
				up = !up;
			}
		}

		context.DrawGeometry(null, pen, geometry);
	}
}
