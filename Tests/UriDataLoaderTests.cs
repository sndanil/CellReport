using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class UriDataLoaderTests
	{
		[TestMethod]
		public void Load_ResourceUri_ReturnsRegisteredBytes()
		{
			var loader = new UriDataLoader(name => name == "logo" ? new byte[] { 1, 2, 3 } : new byte[0]);
			var data = loader.Load("Resource://logo");

			CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, data);
		}

		[TestMethod]
		public void Load_ResourceUri_IsCaseInsensitive()
		{
			var loader = new UriDataLoader(name => new byte[] { 9 });
			var data = loader.Load("resource://icon");

			CollectionAssert.AreEqual(new byte[] { 9 }, data);
		}

		[TestMethod]
		public void Load_FileUri_ReturnsFileContent()
		{
			var path = Path.GetTempFileName();
			try
			{
				File.WriteAllBytes(path, new byte[] { 4, 5, 6 });
				var loader = new UriDataLoader(_ => new byte[0], new UriDataLoaderOptions
				{
					AllowFile = true,
					AllowedFileRoot = Path.GetDirectoryName(path)
				});
				var data = loader.Load("file://" + path);

				CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, data);
			}
			finally
			{
				File.Delete(path);
			}
		}

		[TestMethod]
		public void Load_FileUri_IsDeniedByDefault()
		{
			var loader = new UriDataLoader(_ => new byte[0]);

			Assert.IsNull(loader.Load("file://secret.txt"));
		}

		[TestMethod]
		public void Load_FileUri_OutsideRoot_ThrowsInStrictMode()
		{
			var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			try
			{
				var loader = new UriDataLoader(_ => new byte[0], new UriDataLoaderOptions
				{
					AllowFile = true,
					AllowedFileRoot = root,
					ThrowOnError = true
				});

				Assert.ThrowsException<InvalidOperationException>(
					() => loader.Load("file://../secret.txt"));
			}
			finally
			{
				Directory.Delete(root);
			}
		}

		[TestMethod]
		public void Load_EmptyUri_ReturnsNull()
		{
			var loader = new UriDataLoader(_ => new byte[0]);
			Assert.IsNull(loader.Load(null));
			Assert.IsNull(loader.Load(string.Empty));
		}

		[TestMethod]
		public void Load_UnknownScheme_ReturnsNull()
		{
			var loader = new UriDataLoader(_ => new byte[0]);
			Assert.IsNull(loader.Load("ftp://example.com/file.bin"));
		}

		[TestMethod]
		public void Load_HttpUri_AllowedHost_ReturnsBoundedContent()
		{
			var loader = new UriDataLoader(
				_ => new byte[0],
				new UriDataLoaderOptions
				{
					AllowHttp = true,
					AllowedHttpHosts = new[] { "assets.example" },
					MaxDataBytes = 3,
					ThrowOnError = true
				},
				new StubHttpHandler(new byte[] { 1, 2, 3 }));

			var data = loader.Load("https://assets.example/logo.bin");

			CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, data);
		}

		[TestMethod]
		public void Load_HttpUri_UnlistedHost_IsRejected()
		{
			var loader = new UriDataLoader(
				_ => new byte[0],
				new UriDataLoaderOptions
				{
					AllowHttp = true,
					AllowedHttpHosts = new[] { "assets.example" },
					ThrowOnError = true
				},
				new StubHttpHandler(new byte[] { 1 }));

			Assert.ThrowsException<InvalidOperationException>(
				() => loader.Load("https://internal.example/metadata"));
		}

		[TestMethod]
		public void CellManager_GetDataByUri_UsesResourceLoader()
		{
			var manager = new CellManager();
			manager.AddResource("image0", new byte[] { 7, 8, 9 });

			CollectionAssert.AreEqual(new byte[] { 7, 8, 9 }, manager.GetDataByUri("Resource://image0"));
		}

		private sealed class StubHttpHandler : HttpMessageHandler
		{
			private readonly byte[] _content;

			public StubHttpHandler(byte[] content)
			{
				_content = content;
			}

			protected override Task<HttpResponseMessage> SendAsync(
				HttpRequestMessage request,
				CancellationToken cancellationToken)
			{
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new ByteArrayContent(_content)
				});
			}
		}
	}
}
