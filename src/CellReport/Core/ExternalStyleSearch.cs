using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CellReport.Core
{
	public static class ExternalStyleSearch
	{
		public static string DocumentDirectory { get; set; }

		internal static byte[] TryLoad(string uri, string templateDirectory)
		{
			var fileName = BareFileName(uri);
			if (fileName == null)
				return null;

			var start = string.IsNullOrEmpty(templateDirectory) ? DocumentDirectory : templateDirectory;
			var found = FindUpwards(start, fileName) ?? FindInApplicationDirectory(fileName);
			if (found == null)
				return null;

			try
			{
				return File.ReadAllBytes(found);
			}
			catch
			{
				return null;
			}
		}

		internal static string BareFileName(string uri)
		{
			if (string.IsNullOrEmpty(uri))
				return null;

			var path = uri;
			var scheme = uri.IndexOf("://", StringComparison.Ordinal);
			if (scheme >= 0)
			{
				if (!uri.StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
					return null;
				path = uri.Substring(scheme + 3);
			}

			if (path.Length == 0 || Path.IsPathRooted(path))
				return null;
			if (path.IndexOf('/') >= 0 || path.IndexOf('\\') >= 0)
				return null;

			return path;
		}

		private static string FindUpwards(string startDirectory, string fileName)
		{
			var directory = startDirectory;
			while (!string.IsNullOrEmpty(directory))
			{
				var candidate = Path.Combine(directory, fileName);
				if (FileExists(candidate))
					return candidate;

				DirectoryInfo parent = null;
				try
				{
					parent = Directory.GetParent(directory);
				}
				catch
				{
					return null;
				}

				directory = parent == null ? null : parent.FullName;
			}

			return null;
		}

		private static string FindInApplicationDirectory(string fileName)
		{
			var seen = new List<string>();
			foreach (var directory in ApplicationDirectories())
			{
				if (string.IsNullOrEmpty(directory))
					continue;

				string full;
				try
				{
					full = Path.GetFullPath(directory);
				}
				catch
				{
					continue;
				}

				if (seen.Exists(item => string.Equals(item, full, StringComparison.OrdinalIgnoreCase)))
					continue;

				seen.Add(full);
				var candidate = Path.Combine(full, fileName);
				if (FileExists(candidate))
					return candidate;
			}

			return null;
		}

		private static IEnumerable<string> ApplicationDirectories()
		{
			yield return AppContext.BaseDirectory;

			var entry = Assembly.GetEntryAssembly();
			if (entry != null)
				yield return Path.GetDirectoryName(entry.Location);

			yield return Path.GetDirectoryName(typeof(ExternalStyleSearch).Assembly.Location);
		}

		private static bool FileExists(string path)
		{
			try
			{
				return File.Exists(path);
			}
			catch
			{
				return false;
			}
		}
	}
}
