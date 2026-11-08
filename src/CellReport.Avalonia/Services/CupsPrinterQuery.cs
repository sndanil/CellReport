using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CellReport.Avalonia.Services
{
	internal static class CupsPrinterQuery
	{
		private static readonly Regex PrinterLine = new Regex(
			@"^printer\s+(.+?)\s+(?:is|disabled|now)\b",
			RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private static readonly Regex DefaultLine = new Regex(
			@"^system default destination:\s*(.+?)\s*$",
			RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private static readonly Regex CustomMillimetres = new Regex(
			@"^Custom\.(\d+(?:\.\d+)?)x(\d+(?:\.\d+)?)mm$",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex CustomUnits = new Regex(
			@"^Custom\.(\d+(?:\.\d+)?)x(\d+(?:\.\d+)?)$",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex PointSize = new Regex(
			@"^w(\d+(?:\.\d+)?)h(\d+(?:\.\d+)?)$",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Dictionary<string, PaperMm> KnownPaper = new Dictionary<string, PaperMm>(StringComparer.OrdinalIgnoreCase)
		{
			{ "A3", new PaperMm(297f, 420f) },
			{ "A4", new PaperMm(210f, 297f) },
			{ "A5", new PaperMm(148f, 210f) },
			{ "A6", new PaperMm(105f, 148f) },
			{ "Letter", new PaperMm(215.9f, 279.4f) },
			{ "Legal", new PaperMm(215.9f, 355.6f) },
			{ "Tabloid", new PaperMm(279.4f, 431.8f) },
			{ "Executive", new PaperMm(184.2f, 266.7f) },
			{ "B5", new PaperMm(176f, 250f) },
			{ "Statement", new PaperMm(139.7f, 215.9f) }
		};

		public static IReadOnlyList<string> ParsePrinters(string lpstatOutput)
		{
			var printers = new List<string>();
			if (string.IsNullOrEmpty(lpstatOutput))
				return printers;

			var lines = lpstatOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var match = PrinterLine.Match(line.Trim());
				if (!match.Success)
					continue;

				var name = match.Groups[1].Value.Trim();
				if (name.Length > 0 && !printers.Contains(name))
					printers.Add(name);
			}

			return printers;
		}

		public static string? ParseDefaultPrinter(string lpstatOutput)
		{
			if (string.IsNullOrEmpty(lpstatOutput))
				return null;

			var lines = lpstatOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var match = DefaultLine.Match(line.Trim());
				if (match.Success)
					return match.Groups[1].Value.Trim();
			}

			return null;
		}

		public static string? ParseDefaultMedia(string lpoptionsOutput)
		{
			if (string.IsNullOrEmpty(lpoptionsOutput))
				return null;

			var lines = lpoptionsOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var trimmed = line.Trim();
				if (!trimmed.StartsWith("PageSize/", StringComparison.Ordinal) &&
				    !trimmed.StartsWith("PageSize ", StringComparison.Ordinal))
					continue;

				var colon = trimmed.IndexOf(':');
				if (colon < 0 || colon == trimmed.Length - 1)
					continue;

				var options = trimmed.Substring(colon + 1).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var option in options)
				{
					if (option.StartsWith("*", StringComparison.Ordinal) && option.Length > 1)
						return option.Substring(1);
				}
			}

			return null;
		}

		public static bool TryGetPaperMillimetres(string? mediaName, out float widthMm, out float heightMm)
		{
			widthMm = 0;
			heightMm = 0;
			if (string.IsNullOrWhiteSpace(mediaName))
				return false;

			PaperMm known;
			if (KnownPaper.TryGetValue(mediaName, out known))
			{
				widthMm = known.Width;
				heightMm = known.Height;
				return true;
			}

			var customMm = CustomMillimetres.Match(mediaName);
			if (customMm.Success)
				return ReadPair(customMm, out widthMm, out heightMm);

			var points = PointSize.Match(mediaName);
			if (points.Success && ReadPair(points, out widthMm, out heightMm))
			{
				widthMm = widthMm * 25.4f / 72f;
				heightMm = heightMm * 25.4f / 72f;
				return true;
			}

			var custom = CustomUnits.Match(mediaName);
			if (custom.Success)
				return ReadPair(custom, out widthMm, out heightMm);

			return false;
		}

		private static bool ReadPair(Match match, out float width, out float height)
		{
			var widthOk = float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out width);
			var heightOk = float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out height);
			return widthOk && heightOk && width > 0 && height > 0;
		}

		private readonly struct PaperMm
		{
			public PaperMm(float width, float height)
			{
				Width = width;
				Height = height;
			}

			public float Width { get; }

			public float Height { get; }
		}
	}
}
