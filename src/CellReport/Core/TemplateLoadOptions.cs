using System;

namespace CellReport.Core
{
	[Serializable]
	public sealed class TemplateLoadOptions
	{
		public long MaxTemplateBytes { get; set; } = 100L * 1024L * 1024L;
		public long MaxXmlBytes { get; set; } = 10L * 1024L * 1024L;
		public long MaxResourceBytes { get; set; } = 20L * 1024L * 1024L;
		public long MaxTotalResourceBytes { get; set; } = 100L * 1024L * 1024L;
		public int MaxRows { get; set; } = 100000;
		public int MaxColumns { get; set; } = 10000;
		public long MaxCells { get; set; } = 10000000L;

		internal TemplateLoadOptions Clone()
		{
			return (TemplateLoadOptions)MemberwiseClone();
		}
	}

	[Serializable]
	public class TemplateLoadException : Exception
	{
		public TemplateLoadException(string message)
			: base(message)
		{
		}

		public TemplateLoadException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
