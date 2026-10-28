using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace CellReport.Core
{
	public delegate byte[] ResourceGetter(string name);

	[Serializable]
	public sealed class UriDataLoaderOptions
	{
		public bool AllowFile { get; set; }
		public bool AllowFileResolver { get; set; }
		public bool AllowHttp { get; set; }
		public bool ThrowOnError { get; set; }
		public string AllowedFileRoot { get; set; }
		public IList<string> AllowedHttpHosts { get; set; } = new List<string>();
		public long MaxDataBytes { get; set; } = 10L * 1024L * 1024L;
		public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(10);

		internal UriDataLoaderOptions Clone()
		{
			return new UriDataLoaderOptions
			{
				AllowFile = AllowFile,
				AllowFileResolver = AllowFileResolver,
				AllowHttp = AllowHttp,
				ThrowOnError = ThrowOnError,
				AllowedFileRoot = AllowedFileRoot,
				AllowedHttpHosts = new List<string>(AllowedHttpHosts ?? new List<string>()),
				MaxDataBytes = MaxDataBytes,
				HttpTimeout = HttpTimeout
			};
		}
	}

	public class UriDataLoader
	{
		private readonly ResourceGetter _getResource;
		private readonly UriDataLoaderOptions _options;
		private readonly HttpMessageHandler _httpMessageHandler;

		public UriDataLoader(ResourceGetter getResource)
			: this(getResource, null)
		{
		}

		public UriDataLoader(ResourceGetter getResource, UriDataLoaderOptions options)
			: this(getResource, options, null)
		{
		}

		internal UriDataLoader(
			ResourceGetter getResource,
			UriDataLoaderOptions options,
			HttpMessageHandler httpMessageHandler)
		{
			if (getResource == null)
				throw new ArgumentNullException(nameof(getResource));

			_getResource = getResource;
			_options = options ?? new UriDataLoaderOptions();
			_httpMessageHandler = httpMessageHandler;
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
				if (_options.ThrowOnError)
					throw;
				return null;
			}
		}

		private byte[] LoadFileUri(string uri)
		{
			if (!_options.AllowFile)
				throw new InvalidOperationException("Loading file resources is disabled.");

			var separatorIndex = uri.IndexOf("://", StringComparison.Ordinal);
			if (separatorIndex < 0)
				throw new InvalidOperationException("Invalid file URI.");
			var requestedPath = Uri.UnescapeDataString(uri.Substring(separatorIndex + 3));

			if (_options.AllowFileResolver)
			{
				var resolved = FileResolver.Resolve(requestedPath);
				if (resolved != null)
					return ValidateSize(resolved);
			}

			if (string.IsNullOrEmpty(_options.AllowedFileRoot))
				throw new InvalidOperationException("AllowedFileRoot must be configured for file resources.");

			var root = Path.GetFullPath(_options.AllowedFileRoot);
			var path = Path.IsPathRooted(requestedPath)
				? Path.GetFullPath(requestedPath)
				: Path.GetFullPath(Path.Combine(root, requestedPath));
			if (!IsUnderRoot(path, root))
				throw new InvalidOperationException("The file resource is outside the configured root.");
			if (!File.Exists(path))
				return null;

			using (var stream = File.OpenRead(path))
				return ReadLimited(stream);
		}

		private byte[] LoadHttpUri(string uri)
		{
			if (!_options.AllowHttp)
				throw new InvalidOperationException("Loading HTTP resources is disabled.");

			Uri parsed;
			if (!Uri.TryCreate(uri, UriKind.Absolute, out parsed)
				|| (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
				throw new InvalidOperationException("Invalid HTTP URI.");
			if (!IsAllowedHost(parsed.Host))
				throw new InvalidOperationException("The HTTP host is not allowed.");

			var handler = _httpMessageHandler ?? new HttpClientHandler { AllowAutoRedirect = false };
			using (var client = new HttpClient(handler) { Timeout = _options.HttpTimeout })
			using (var result = client.GetAsync(parsed, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
			{
				if (!result.IsSuccessStatusCode)
					return Array.Empty<byte>();
				if (result.Content.Headers.ContentLength.HasValue
					&& result.Content.Headers.ContentLength.Value > _options.MaxDataBytes)
					throw new InvalidOperationException("The HTTP resource exceeds the configured size limit.");
				using (var stream = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
					return ReadLimited(stream);
			}
		}

		private bool IsAllowedHost(string host)
		{
			if (_options.AllowedHttpHosts == null)
				return false;
			foreach (var allowedHost in _options.AllowedHttpHosts)
			{
				if (string.Equals(host, allowedHost, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		private byte[] ReadLimited(Stream stream)
		{
			using (var output = new MemoryStream())
			{
				var buffer = new byte[81920];
				long total = 0;
				int read;
				while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					total += read;
					if (total > _options.MaxDataBytes)
						throw new InvalidOperationException("The resource exceeds the configured size limit.");
					output.Write(buffer, 0, read);
				}
				return output.ToArray();
			}
		}

		private byte[] ValidateSize(byte[] data)
		{
			if (data.LongLength > _options.MaxDataBytes)
				throw new InvalidOperationException("The resource exceeds the configured size limit.");
			return data;
		}

		private static bool IsUnderRoot(string path, string root)
		{
			var comparison = Path.DirectorySeparatorChar == '\\'
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;
			var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
				+ Path.DirectorySeparatorChar;
			return path.StartsWith(normalizedRoot, comparison)
				|| string.Equals(path, root, comparison);
		}
	}
}
