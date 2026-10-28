using CellReport.Core;
using System;
using System.IO;

namespace CellReport.Editor.Avalonia.Editing;

internal sealed class DocumentSession
{
	public string? FilePath { get; private set; }

	public void SetPath(string? path)
	{
		FilePath = string.IsNullOrEmpty(path) ? null : Path.GetFullPath(path);
	}

	public void ConfigureManager(CellManager manager)
	{
		if (manager == null)
			throw new ArgumentNullException(nameof(manager));

		manager.IsDesignMode = true;
		var directory = FilePath == null ? null : Path.GetDirectoryName(FilePath);
		manager.UriDataLoaderOptions = new UriDataLoaderOptions
		{
			AllowFile = !string.IsNullOrEmpty(directory),
			AllowedFileRoot = directory,
			MaxDataBytes = 20L * 1024L * 1024L
		};
	}

	public byte[]? ResolveExternalFile(string fileName)
	{
		if (FilePath == null || !File.Exists(FilePath) || string.IsNullOrEmpty(fileName))
			return null;

		var directory = Path.GetDirectoryName(FilePath);
		if (directory == null)
			return null;

		var root = Path.GetFullPath(directory);
		var path = Path.GetFullPath(Path.Combine(root, fileName));
		var comparison = Path.DirectorySeparatorChar == '\\'
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;
		var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
			+ Path.DirectorySeparatorChar;
		if (!path.StartsWith(rootPrefix, comparison) && !string.Equals(path, root, comparison))
			return null;

		return File.Exists(path) ? File.ReadAllBytes(path) : null;
	}
}
