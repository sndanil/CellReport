using System.Collections.Generic;
using System.Linq;
using CellReport.Core;
using CellReport.Core.Contents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class ResourceTests
	{
		[TestMethod]
		public void GetUriReferenceCounts_CountsCellsCollectionsAndPagers()
		{
			var manager = new CellManager(2, 2);

			CreateImage(manager, 0, 0, "resources://a.png");
			var collection = new CollectionContent(manager, 2);
			collection.Add(CreateImageContent(manager, "resources://a.png"));
			var paramImage = CreateImageContent(manager, "file://x.png");
			var param = new Param(manager, paramImage) { Name = "Uri", Target = "Uri", Value = "file://x.png" };
			paramImage.AddParam(param);
			collection.Add(paramImage);
			manager.CreateCell(0, 1).Content = collection;

			var pagerImage = CreateImageContent(manager, "resources://c.png");
			manager.PrintSettings.Headers.Add(new Pager { Left = pagerImage });

			var counts = manager.GetUriReferenceCounts().ToDictionary(item => item.Key, item => item.Value);

			Assert.AreEqual(2, counts["resources://a.png"], "свойство и содержимое коллекции");
			Assert.AreEqual(1, counts["file://x.png"], "параметр с Target=Uri");
			Assert.AreEqual(1, counts["resources://c.png"], "изображение в колонтитуле");
			Assert.AreEqual(3, counts.Count);
		}

		[TestMethod]
		public void ReplaceUriReferences_UpdatesPropertyAndParameter()
		{
			var manager = new CellManager(1, 2);
			manager.AddResource("a.png", new byte[] { 1 });
			manager.AddResource("b.png", new byte[] { 2 });

			var byProperty = CreateImage(manager, 0, 0, "resources://a.png");
			var byParam = CreateImageContent(manager, null);
			var param = new Param(manager, byParam) { Name = "Uri", Target = "Uri", Value = "resources://a.png" };
			byParam.AddParam(param);
			manager.CreateCell(0, 1).Content = byParam;

			var replaced = manager.ReplaceUriReferences("resources://a.png", "resources://b.png");

			Assert.AreEqual(2, replaced);
			Assert.AreEqual("resources://b.png", byProperty.Uri);
			Assert.AreEqual("resources://b.png", param.Value);
			CollectionAssert.AreEqual(new byte[] { 2 }, byProperty.GetImageData());

			Assert.AreEqual(0, manager.GetUriReferenceCounts().Count(item => item.Key == "resources://a.png"));
		}

		[TestMethod]
		public void InvalidateImageCaches_ReturnsNewResourceData()
		{
			var manager = new CellManager(1, 1);
			manager.AddResource("a.png", new byte[] { 1, 1 });
			var image = CreateImage(manager, 0, 0, "resources://a.png");

			CollectionAssert.AreEqual(new byte[] { 1, 1 }, image.GetImageData());

			manager.AddResource("a.png", new byte[] { 2, 2, 2 });
			CollectionAssert.AreEqual(new byte[] { 1, 1 }, image.GetImageData(), "до сброса кэша данные старые");

			Assert.AreEqual(1, manager.InvalidateImageCaches("resources://a.png"));
			CollectionAssert.AreEqual(new byte[] { 2, 2, 2 }, image.GetImageData());
		}

		private static ImageContent CreateImage(CellManager manager, int row, int column, string uri)
		{
			var image = CreateImageContent(manager, uri);
			manager.CreateCell(row, column).Content = image;
			return image;
		}

		private static ImageContent CreateImageContent(CellManager manager, string? uri)
		{
			return new ImageContent(manager) { Uri = uri ?? string.Empty };
		}
	}
}
