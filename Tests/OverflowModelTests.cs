using System.Xml.Linq;
using CellReport.Core;
using CellReport.Core.Contents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class OverflowModelTests
	{
		private static TextContentStyle ParseTextStyle(string xml)
		{
			return (TextContentStyle)BaseStyle.Parse(XElement.Parse(xml));
		}

		[TestMethod]
		public void TextStyle_ParsesOverflow()
		{
			var style = ParseTextStyle(
				"<TextContentStyle ID=\"tc\" Overflow=\"Auto\" Color=\"Black\" Angle=\"0\" Direction=\"LeftToRight\">"
				+ "<Alignment Horizontal=\"Left\" Vertical=\"Top\" /><Font Family=\"Arial\" Size=\"10\" /></TextContentStyle>");

			Assert.AreEqual(TextOverflow.Auto, style.Overflow);
		}

		[TestMethod]
		public void TextStyle_DefaultOverflow_IsClip()
		{
			var style = ParseTextStyle(
				"<TextContentStyle ID=\"tc\" Color=\"Black\" Angle=\"0\" Direction=\"LeftToRight\">"
				+ "<Font Family=\"Arial\" Size=\"10\" /></TextContentStyle>");

			Assert.AreEqual(TextOverflow.Clip, style.Overflow);
		}

		[TestMethod]
		public void TextStyle_UnknownOverflow_FallsBackToClip()
		{
			var style = ParseTextStyle(
				"<TextContentStyle ID=\"tc\" Overflow=\"Bogus\" Color=\"Black\" Angle=\"0\" Direction=\"LeftToRight\">"
				+ "<Font Family=\"Arial\" Size=\"10\" /></TextContentStyle>");

			Assert.AreEqual(TextOverflow.Clip, style.Overflow);
		}

		[TestMethod]
		public void TextStyle_SerializesOverflow_OnlyWhenNotDefault()
		{
			var style = new TextContentStyle(HorizontalAlignment.Left, VerticalAlignment.Top, FontInfo.Default, "Black");
			style.ID = "tc";
			Assert.IsNull(style.Serialize().Attribute("Overflow"));

			var auto = new TextContentStyle(HorizontalAlignment.Left, VerticalAlignment.Top, FontInfo.Default, "Black",
				TextOverflow.Auto);
			auto.ID = "tc";
			var attribute = auto.Serialize().Attribute("Overflow");
			Assert.IsNotNull(attribute);
			Assert.AreEqual("Auto", attribute.Value);

			var roundTrip = (TextContentStyle)BaseStyle.Parse(auto.Serialize());
			Assert.AreEqual(TextOverflow.Auto, roundTrip.Overflow);
		}

		[TestMethod]
		public void TextStyle_Overflow_AffectsEquality()
		{
			var clip = new TextContentStyle(HorizontalAlignment.Left, VerticalAlignment.Top, FontInfo.Default, "Black");
			var auto = new TextContentStyle(HorizontalAlignment.Left, VerticalAlignment.Top, FontInfo.Default, "Black",
				TextOverflow.Auto);

			Assert.AreNotEqual(clip, auto);
		}

		[TestMethod]
		public void DecimalStyle_ParsesOverflow()
		{
			var style = (DecimalContentStyle)BaseStyle.Parse(XElement.Parse(
				"<DecimalContentStyle ID=\"dc\" Overflow=\"Both\" NegativeColor=\"Red\" Color=\"Black\" Angle=\"0\" Direction=\"LeftToRight\">"
				+ "<Font Family=\"Arial\" Size=\"10\" /></DecimalContentStyle>"));

			Assert.AreEqual(TextOverflow.Both, style.Overflow);
		}

		[TestMethod]
		public void DateTimeStyle_ParsesOverflow()
		{
			var style = (DateTimeContentStyle)BaseStyle.Parse(XElement.Parse(
				"<DateTimeContentStyle ID=\"dt\" Overflow=\"Left\" Color=\"Black\" Angle=\"0\" Direction=\"LeftToRight\">"
				+ "<Font Family=\"Arial\" Size=\"10\" /></DateTimeContentStyle>"));

			Assert.AreEqual(TextOverflow.Left, style.Overflow);
		}

		[TestMethod]
		public void Param_OverridesOverflow()
		{
			var manager = new CellManager(1, 1);
			var style = new TextContentStyle(HorizontalAlignment.Left, VerticalAlignment.Top, FontInfo.Default, "Black");
			style.ID = "tc";
			manager.AddStyle(style);

			var content = new TextContent(manager) { Text = "value", StyleID = "tc" };
			content.AddParam(new Param(manager, content)
			{
				Name = "Переполнение",
				Target = "Style.Overflow",
				Value = "Auto"
			});

			var actual = (TextContentStyle)content.GetActualStyle();
			Assert.AreEqual(TextOverflow.Auto, actual.Overflow);
		}

		[TestMethod]
		public void ImageContent_ParsesLayoutAttributes()
		{
			var manager = new CellManager(1, 1);
			var content = (ImageContent)BaseContent.ParseContent(manager, XElement.Parse(
				"<ImageContent Uri=\"res://img\" ColSpan=\"1\" RowSpan=\"1\" SizeMode=\"Actual\" Scale=\"150\" Anchor=\"BottomRight\" Overflow=\"true\" />"));

			Assert.AreEqual(ImageSizeMode.Actual, content.SizeMode);
			Assert.AreEqual(150f, content.Scale);
			Assert.AreEqual(ImageAnchor.BottomRight, content.Anchor);
			Assert.IsTrue(content.Overflow);

			var serialized = content.Serialize();
			Assert.AreEqual("Actual", serialized.Attribute("SizeMode").Value);
			Assert.AreEqual("150", serialized.Attribute("Scale").Value);
			Assert.AreEqual("BottomRight", serialized.Attribute("Anchor").Value);
			Assert.AreEqual("true", serialized.Attribute("Overflow").Value);
		}

		[TestMethod]
		public void ImageContent_DefaultLayoutAttributes_NotSerialized()
		{
			var manager = new CellManager(1, 1);
			var content = (ImageContent)BaseContent.ParseContent(manager, XElement.Parse("<ImageContent />"));

			Assert.AreEqual(ImageSizeMode.Stretch, content.SizeMode);
			Assert.AreEqual(100f, content.Scale);
			Assert.AreEqual(ImageAnchor.Center, content.Anchor);
			Assert.IsFalse(content.Overflow);

			var serialized = content.Serialize();
			Assert.IsNull(serialized.Attribute("SizeMode"));
			Assert.IsNull(serialized.Attribute("Scale"));
			Assert.IsNull(serialized.Attribute("Anchor"));
			Assert.IsNull(serialized.Attribute("Overflow"));
		}
	}
}
