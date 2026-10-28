using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using System.IO;

namespace CellReport.Core.Contents
{
	[Serializable]
	public class ImageContentStyle : BaseStyle
	{
		public ImageContentStyle()
		{
		}

		public override bool Equals(object obj)
		{
			if (base.Equals(obj))
				return true;

			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		internal new static ImageContentStyle Parse(XElement element)
		{
			return new ImageContentStyle();
		}

		protected override XElement OnSerialize(string styleName)
		{
			return base.OnSerialize(styleName ?? "ImageContentStyle");
		}
	}

	[Serializable]
	public class ImageContent : BaseContent
	{
		public static readonly ImageContentStyle DefaultStyle = new ImageContentStyle();

		private byte[] _cache = null;

		public ImageContent(CellManager manager)
			: base(manager)
		{
		}

		public override BaseStyle GetStyle()
		{
			return GetStyle<ImageContentStyle>() ?? DefaultStyle;
		}

		public string Uri { get; set; }

		public int ColSpan { get; set; }

		public int RowSpan { get; set; }

		public string ActualUri => (string) GetParamValueByTarget("Uri") ?? Uri;

		/// <summary>
		/// Сбрасывает загруженные байты изображения (например, после замены ресурса).
		/// </summary>
		public void InvalidateCache()
		{
			_cache = null;
		}

		public byte[] GetImageData()
		{
			var uri = ActualUri;
			if (!string.IsNullOrEmpty(uri) && _cache == null)
				_cache = _manager.GetDataByUri(uri);

			return _cache ?? new byte[0];
		}

		internal new static ImageContent Parse(CellManager manager, XElement element)
		{
			var content = new ImageContent(manager);
			if (element.Attribute("Uri") != null)
				content.Uri = element.Attribute("Uri").Value;

			if (element.Attribute("ColSpan") != null)
				content.ColSpan = int.Parse(element.Attribute("ColSpan").Value, NumberStyles.Number, CultureInfo.InvariantCulture);

			if (element.Attribute("RowSpan") != null)
				content.RowSpan = int.Parse(element.Attribute("RowSpan").Value, NumberStyles.Number, CultureInfo.InvariantCulture);

			return content;
		}

		protected override XElement OnSerialize(string contentName)
		{
			var result = base.OnSerialize(contentName ?? "ImageContent");
			if (!string.IsNullOrEmpty(Uri))
				result.Add(new XAttribute("Uri", Uri));

			if (ColSpan != 0)
				result.Add(new XAttribute("ColSpan", ColSpan.ToString(CultureInfo.InvariantCulture)));

			if (RowSpan != 0)
				result.Add(new XAttribute("RowSpan", RowSpan.ToString(CultureInfo.InvariantCulture)));

			return result;
		}

		public override string ToString()
		{
			var uri = Uri ?? string.Empty;
			uri = uri.Length > 10 ? uri.Substring(0, 10) + "..." : uri;
			return $"Изображение ({uri})";
		}
	}
}
