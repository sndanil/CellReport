using System;
using System.IO;
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
				var loader = new UriDataLoader(_ => new byte[0]);
				var data = loader.Load("file://" + path);

				CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, data);
			}
			finally
			{
				File.Delete(path);
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
		public void CellManager_GetDataByUri_UsesResourceLoader()
		{
			var manager = new CellManager();
			manager.AddResource("image0", new byte[] { 7, 8, 9 });

			CollectionAssert.AreEqual(new byte[] { 7, 8, 9 }, manager.GetDataByUri("Resource://image0"));
		}
	}
}
