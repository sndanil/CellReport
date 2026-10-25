using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.Avalonia.ViewModels;
using System;
using System.Globalization;
using AvaloniaPoint = Avalonia.Point;
using MediaFontStyle = Avalonia.Media.FontStyle;
using ReportBorder = CellReport.Core.Border;
using ReportFontStyle = CellReport.Core.FontStyle;
using ReportHAlign = CellReport.Core.Contents.HorizontalAlignment;
using ReportVAlign = CellReport.Core.Contents.VerticalAlignment;

namespace CellReport.Editor.Avalonia.Controls;

public class StylePreview : Control
{
	public static readonly StyledProperty<StyleChoice?> ChoiceProperty =
		AvaloniaProperty.Register<StylePreview, StyleChoice?>(nameof(Choice));

	static StylePreview()
	{
		AffectsRender<StylePreview>(ChoiceProperty);
	}

	public StyleChoice? Choice
	{
		get => GetValue(ChoiceProperty);
		set => SetValue(ChoiceProperty, value);
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
		if (bounds.Width < 2 || bounds.Height < 2)
			return;

		context.FillRectangle(BrushOf("#FFFFFFFF"), bounds);
		context.DrawRectangle(new Pen(BrushOf("#FFB1B5BA"), 1), bounds);

		var style = Choice?.Style;
		if (style is CellStyle cell)
			DrawCell(context, bounds, cell, Choice?.Title);
		else if (style is DecimalContentStyle number)
			DrawTextStyle(context, bounds, number, SampleNumber(number, 1234.12345m), SampleNumber(number, -1234.12345m));
		else if (style is DateTimeContentStyle date)
			DrawTextStyle(context, bounds, date, SampleDate(date), null);
		else if (style is TextContentStyle text)
			DrawTextStyle(context, bounds, text, string.IsNullOrEmpty(Choice?.Title) ? "Текст" : Choice.Title, null);
		else
			DrawLabel(context, bounds, "Стиль", BrushOf("#FF616161"), 12, FontWeight.Normal, MediaFontStyle.Normal, ReportHAlign.Left, ReportVAlign.Middle);
	}

	private static void DrawCell(DrawingContext context, Rect bounds, CellStyle style, string? title)
	{
		var inner = bounds.Deflate(new Thickness(6, 4));
		context.FillRectangle(BrushOf(style.Color), inner);
		DrawEdge(context, style.Borders.Top, inner.TopLeft, inner.TopRight);
		DrawEdge(context, style.Borders.Bottom, inner.BottomLeft, inner.BottomRight);
		DrawEdge(context, style.Borders.Left, inner.TopLeft, inner.BottomLeft);
		DrawEdge(context, style.Borders.Right, inner.TopRight, inner.BottomRight);
		var fill = ParseColor(style.Color);
		var text = IsDark(fill) ? BrushOf("#FFFFFFFF") : BrushOf("#FF1A1A1A");
		DrawLabel(context, inner, string.IsNullOrEmpty(title) ? "Стиль" : title, text, 12, FontWeight.Normal, MediaFontStyle.Normal, ReportHAlign.Left, ReportVAlign.Middle);
	}

	private static void DrawTextStyle(DrawingContext context, Rect bounds, TextContentStyle style, string primary, string? secondary)
	{
		var inner = bounds.Deflate(new Thickness(6, 2));
		var weight = style.Font.Style.HasFlag(ReportFontStyle.Bold) ? FontWeight.Bold : FontWeight.Normal;
		var slant = style.Font.Style.HasFlag(ReportFontStyle.Italic) ? MediaFontStyle.Italic : MediaFontStyle.Normal;
		var size = Math.Clamp(style.Font.Size, 9, 14);
		if (string.IsNullOrEmpty(secondary))
		{
			DrawLabel(context, inner, primary, BrushOf(style.Color), size, weight, slant, style.HAlignment, style.VAlignment, style.Font.Family);
			return;
		}

		var top = new Rect(inner.X, inner.Y, inner.Width, inner.Height / 2);
		var bottom = new Rect(inner.X, inner.Y + inner.Height / 2, inner.Width, inner.Height / 2);
		DrawLabel(context, top, primary, BrushOf(style.Color), size, weight, slant, style.HAlignment, ReportVAlign.Middle, style.Font.Family);
		var negative = style is DecimalContentStyle number ? number.NegativeColor : style.Color;
		DrawLabel(context, bottom, secondary, BrushOf(negative), size, weight, slant, style.HAlignment, ReportVAlign.Middle, style.Font.Family);
	}

	private static void DrawEdge(DrawingContext context, ReportBorder border, AvaloniaPoint start, AvaloniaPoint end)
	{
		if (border.Style == LineStyle.None || border.Weight <= 0)
			return;

		var pen = new Pen(BrushOf(border.Color), Math.Max(1, border.Weight), Dash(border.Style));
		context.DrawLine(pen, start, end);
	}

	private static void DrawLabel(
		DrawingContext context,
		Rect bounds,
		string text,
		IBrush brush,
		double size,
		FontWeight weight,
		MediaFontStyle slant,
		ReportHAlign horizontal,
		ReportVAlign vertical,
		string? family = null)
	{
		if (string.IsNullOrEmpty(text) || bounds.Width < 2 || bounds.Height < 2)
			return;

		var typeface = new Typeface(new FontFamily(string.IsNullOrEmpty(family) ? "Segoe UI" : family), slant, weight);
		var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush);
		formatted.MaxTextWidth = bounds.Width;
		formatted.MaxTextHeight = bounds.Height;
		formatted.Trimming = TextTrimming.CharacterEllipsis;

		var x = horizontal switch
		{
			ReportHAlign.Center or ReportHAlign.Justify => bounds.X + Math.Max(0, (bounds.Width - formatted.Width) / 2),
			ReportHAlign.Right => bounds.Right - formatted.Width,
			_ => bounds.X
		};
		var y = vertical switch
		{
			ReportVAlign.Middle or ReportVAlign.Center => bounds.Y + Math.Max(0, (bounds.Height - formatted.Height) / 2),
			ReportVAlign.Bottom => bounds.Bottom - formatted.Height,
			_ => bounds.Y
		};
		context.DrawText(formatted, new AvaloniaPoint(x, y));
	}

	private static string SampleNumber(DecimalContentStyle style, decimal value)
	{
		try
		{
			var culture = string.IsNullOrEmpty(style.Culture)
				? CultureInfo.CurrentCulture
				: CultureInfo.GetCultureInfo(style.Culture);
			return string.IsNullOrEmpty(style.Format) ? value.ToString(culture) : value.ToString(style.Format, culture);
		}
		catch (FormatException)
		{
			return value.ToString(CultureInfo.CurrentCulture);
		}
	}

	private static string SampleDate(DateTimeContentStyle style)
	{
		var sample = new DateTime(2010, 6, 26, 16, 15, 5);
		try
		{
			if (string.IsNullOrEmpty(style.Format))
				return sample.ToString(CultureInfo.CurrentCulture);

			var culture = string.IsNullOrEmpty(style.Culture)
				? CultureInfo.CurrentCulture
				: CultureInfo.GetCultureInfo(style.Culture);
			return sample.ToString(style.Format, culture);
		}
		catch (FormatException)
		{
			return sample.ToString(CultureInfo.CurrentCulture);
		}
	}

	private static IDashStyle? Dash(LineStyle style)
	{
		return style switch
		{
			LineStyle.Dash => DashStyle.Dash,
			LineStyle.Dot => DashStyle.Dot,
			LineStyle.DashDot => DashStyle.DashDot,
			LineStyle.DashDotDot => DashStyle.DashDotDot,
			_ => null
		};
	}

	private static IBrush BrushOf(string? color)
	{
		return new SolidColorBrush(ParseColor(color));
	}

	private static Color ParseColor(string? color)
	{
		if (!string.IsNullOrWhiteSpace(color) && Color.TryParse(color, out var parsed))
			return parsed;

		return Colors.Transparent;
	}

	private static bool IsDark(Color color)
	{
		return color.A > 0 && color.R * 0.3 + color.G * 0.59 + color.B * 0.11 < 140;
	}
}
