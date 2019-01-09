using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellReport.Core
{
	public static class CommandLineHelper
	{
		public static bool HaveCommandLineArgument(string argName)
		{
#if DEBUG
			if (argName.Replace("-", "").ToLower() == "debug") return true;
#endif
			if (!argName.StartsWith("-"))
				argName = "-" + argName;

			var args = Environment.GetCommandLineArgs();

			return args.Any(a => a.Equals(argName, StringComparison.OrdinalIgnoreCase));
		}

	}
}
