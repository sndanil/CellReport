using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CellReport.Editor.Avalonia.Services;

public sealed class EditorSettings
{
	private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

	public double InspectorWidth { get; set; } = 300;

	public bool InspectorCollapsed { get; set; }

	public double WindowWidth { get; set; } = 1100;

	public double WindowHeight { get; set; } = 720;

	public bool WindowMaximized { get; set; } = true;

	public List<string> RecentFiles { get; set; } = new();

	public string Theme { get; set; } = "Default";

	public bool RibbonCollapsed { get; set; }

	private static string FilePath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"CellReport",
		"editor-settings.json");

	public static EditorSettings Load()
	{
		try
		{
			var path = FilePath;
			if (!File.Exists(path))
				return new EditorSettings();

			return JsonSerializer.Deserialize<EditorSettings>(File.ReadAllText(path)) ?? new EditorSettings();
		}
		catch
		{
			return new EditorSettings();
		}
	}

	public void Save()
	{
		try
		{
			var path = FilePath;
			var directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			File.WriteAllText(path, JsonSerializer.Serialize(this, SerializerOptions));
		}
		catch
		{
			// настройки интерфейса не критичны для работы редактора
		}
	}
}
