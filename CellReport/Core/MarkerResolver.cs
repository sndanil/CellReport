using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellReport.Core
{
	public static class MarkerResolver
	{
		public static event Func<string, bool> OnResolve;

		public static bool Resolve(string marker)
		{
			return (OnResolve == null) || string.IsNullOrEmpty(marker) || OnResolve(marker);
		}
	}
}
