using System;

namespace CellReport.Core
{
	public static class FileResolver
	{
		public static event Func<string, byte[]> OnResolve;

		public static byte[] Resolve(string fileName)
		{
			var result = OnResolve?.Invoke(fileName);
			return result;
		}
	}
}
