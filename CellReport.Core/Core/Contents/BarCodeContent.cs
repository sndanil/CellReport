using System;
using System.Xml.Linq;

namespace CellReport.Core.Contents
{
	[Serializable]
	public class BarCodeContentStyle : BaseStyle
	{
		protected override XElement OnSerialize(string styleName)
		{
			return base.OnSerialize("BarCodeContentStyle");
		}
	}

	[Serializable]
	public enum Orientation
	{
		Horizontal,
		Vertical
	}

	[Serializable]
	public enum BarCodeType
	{
		Auto = 0,
		EAN13 = 1,
		UPCA = 2,
		EAN8 = 3,
		Code39 = 4,
		Code128 = 5,
		QR = 6,
		PDF417 = 7,
		UPCE = 8,
		UCC12 = 9
	}

	[Serializable]
	public class BarCodeContent : BaseContent
	{
		public static readonly BarCodeContentStyle DefaultStyle = new BarCodeContentStyle();

		public BarCodeContent(CellManager manager, string code, BarCodeType type, Orientation orientation) : base(manager)
		{
			Code = code;
			Type = type;
			Orientation = orientation;
		}

		public BarCodeContent(CellManager manager, BarCodeType type) : base(manager)
		{
			Type = type;
		}

		public BarCodeType Type
		{
			get;
			private set;
		}

		public Orientation Orientation
		{
			get;
			private set;
		}

		public string Code
		{
			get;
			private set;
		}

		public string GetCode()
		{
			return (GetParamValueByTarget("Code")?.ToString() ?? Code) ?? string.Empty;
		}

		public BarCodeType GetBarCodeType()
		{
			var type = Type;
			if (GetParamValueByTarget("Type") != null)
				type = (BarCodeType)Enum.Parse(typeof(BarCodeType), GetParamValueByTarget("Type").ToString());

			return type;
		}

		public override BaseStyle GetStyle()
		{
			return GetStyle<BarCodeContentStyle>() ?? DefaultStyle;
		}

		internal new static BarCodeContent Parse(CellManager manager, XElement element)
		{
			BarCodeType type = BarCodeType.Auto;
			if (element.Attribute("Type") != null)
				type = (BarCodeType)Enum.Parse(typeof(BarCodeType), element.Attribute("Type").Value);
			Orientation orientation = Orientation.Horizontal;
			if (element.Attribute("Orientation") != null)
				orientation = (Orientation)Enum.Parse(typeof(Orientation), element.Attribute("Orientation").Value);
			string code = null;
			if (element.Element("Code") != null)
				code = element.Element("Code").Value;
			return new BarCodeContent(manager, code, type, orientation);
		}

		protected override XElement OnSerialize(string contentName)
		{
			var result = base.OnSerialize(contentName ?? "BarCodeContent");
			result.Add(
					new XAttribute("Type", Type),
					new XAttribute("Orientation", Orientation),
					new XElement("Code", Code));
			return result;
		}

		public override string ToString()
		{
			var code = Code ?? string.Empty;
			code = code.Length > 10 ? code.Substring(0, 10) + "..." : code;
			return $"Штрихкод ({code})";
		}
	}
}
