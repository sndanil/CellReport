using System;
using System.IO;

namespace CellReport.Core
{
	public delegate byte[] ResourceGetter(string name);

	public class UriDataLoader
	{
		private readonly ResourceGetter _getResource;

		public UriDataLoader(ResourceGetter getResource)
		{
			if (getResource == null)
				throw new ArgumentNullException(nameof(getResource));

			_getResource = getResource;
		}

		public byte[] Load(string uri)
		{
			if (string.IsNullOrEmpty(uri))
				return null;

			try
			{
				if (uri.StartsWith("RESOURCE", StringComparison.OrdinalIgnoreCase))
				{
					var name = uri.Substring(uri.IndexOf("://", StringComparison.Ordinal) + 3);
					return _getResource(name);
				}

				if (uri.StartsWith("FILE", StringComparison.OrdinalIgnoreCase))
					return LoadFileUri(uri);

				if (uri.StartsWith("HTTP", StringComparison.OrdinalIgnoreCase))
					return LoadHttpUri(uri);

				return null;
			}
			catch
			{
				return null;
			}
		}

		private static byte[] LoadFileUri(string uri)
		{
			var path = uri.Substring(uri.IndexOf("://", StringComparison.Ordinal) + 3);
			var resolved = FileResolver.Resolve(path);
			if (resolved != null)
				return resolved;
			if (File.Exists(path))
				return File.ReadAllBytes(path);
			if (File.Exists(Path.GetFileName(path)))
				return File.ReadAllBytes(Path.GetFileName(path));

			var localPath = Path.Combine(
				Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
				Path.GetFileName(path));
			if (File.Exists(localPath))
				return File.ReadAllBytes(localPath);

			return null;
		}

		private static byte[] LoadHttpUri(string uri)
		{
			using (var client = new System.Net.Http.HttpClient())
			{
				var result = client.GetAsync(uri).GetAwaiter().GetResult();
				return result.IsSuccessStatusCode
					? result.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
					: Array.Empty<byte>();
			}
		}
	}
}
