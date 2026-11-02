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

		/// <summary>
		/// Способ вычисления размера изображения внутри ячейки.
		/// </summary>
		public ImageSizeMode SizeMode { get; set; } = ImageSizeMode.Stretch;

		/// <summary>
		/// Множитель натурального размера в процентах (для <see cref="ImageSizeMode.Scale"/> и <see cref="ImageSizeMode.Actual"/>).
		/// </summary>
		public float Scale { get; set; } = 100f;

		/// <summary>
		/// Положение изображения относительно ячейки (для <see cref="ImageSizeMode.Fit"/>, <see cref="ImageSizeMode.Actual"/>, <see cref="ImageSizeMode.Scale"/>).
		/// </summary>
		public ImageAnchor Anchor { get; set; } = ImageAnchor.Center;

		/// <summary>
		/// Разрешить выход изображения за границы ячейки (клип только по панели/странице/полотну).
		/// </summary>
		public bool Overflow { get; set; }

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

			if (element.Attribute("SizeMode") != null)
			{
				ImageSizeMode sizeMode;
				if (Enum.TryParse(element.Attribute("SizeMode").Value, out sizeMode)
						&& Enum.IsDefined(typeof(ImageSizeMode), sizeMode))
					content.SizeMode = sizeMode;
			}

			if (element.Attribute("Scale") != null)
			{
				float scale;
				if (float.TryParse(element.Attribute("Scale").Value, NumberStyles.Float, CultureInfo.InvariantCulture, out scale) && scale > 0)
					content.Scale = scale;
			}

			if (element.Attribute("Anchor") != null)
			{
				ImageAnchor anchor;
				if (Enum.TryParse(element.Attribute("Anchor").Value, out anchor)
						&& Enum.IsDefined(typeof(ImageAnchor), anchor))
					content.Anchor = anchor;
			}

			if (element.Attribute("Overflow") != null)
				content.Overflow = Convert.ToBoolean(element.Attribute("Overflow").Value, CultureInfo.InvariantCulture);

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

			if (SizeMode != ImageSizeMode.Stretch)
				result.Add(new XAttribute("SizeMode", SizeMode));

			if (Math.Abs(Scale - 100f) > float.Epsilon)
				result.Add(new XAttribute("Scale", Scale.ToString(CultureInfo.InvariantCulture)));

			if (Anchor != ImageAnchor.Center)
				result.Add(new XAttribute("Anchor", Anchor));

			if (Overflow)
				result.Add(new XAttribute("Overflow", true));

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
