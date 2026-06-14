using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using CellReport.Export;
using CellReport.Presenters.Skia;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests.Helpers
{
	internal static class TestExportHelper
	{
		public static readonly SkiaReportRenderer Renderer = new SkiaReportRenderer();

		public static byte[] ExportToBytes(BaseExporter exporter)
		{
			using (var stream = new MemoryStream())
			{
				exporter.ExportTo(stream);
				return stream.ToArray();
			}
		}

		public static void AssertNotEmpty(byte[] data, string formatName)
		{
			Assert.IsNotNull(data);
			Assert.IsTrue(data.Length > 0, formatName + " export produced empty output.");
		}

		public static void AssertPdf(byte[] data)
		{
			AssertNotEmpty(data, "PDF");
			var header = Encoding.ASCII.GetString(data, 0, Math.Min(5, data.Length));
			Assert.AreEqual("%PDF-", header);
		}

		public static void AssertPng(byte[] data)
		{
			AssertNotEmpty(data, "PNG");
			Assert.AreEqual(0x89, data[0]);
			Assert.AreEqual(0x50, data[1]);
			Assert.AreEqual(0x4E, data[2]);
			Assert.AreEqual(0x47, data[3]);
		}

		public static void AssertZipPackage(byte[] data, string entryPath)
		{
			AssertNotEmpty(data, "ZIP");
			using (var stream = new MemoryStream(data))
			using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
			{
				Assert.IsNotNull(archive.GetEntry(entryPath), "Expected entry '" + entryPath + "' in archive.");
			}
		}

		public static void AssertHtml(byte[] data)
		{
			AssertNotEmpty(data, "HTML");
			var text = Encoding.UTF8.GetString(data);
			StringAssert.Contains(text, "<html");
		}
	}
}
