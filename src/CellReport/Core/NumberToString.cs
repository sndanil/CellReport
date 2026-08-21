using System;
using Nut;

namespace CellReport.Core
{
	public class CurrencyToString
	{
		public static Func<decimal, string, string, string> Converter { get; set; }

		public static string GetString(decimal val, string currency, string culture)
		{
			try
			{
                if (Converter != null)
                    return Converter(val, currency, culture);

                return NutFormatMoney(val, currency, culture);
            }
            catch
			{
				return string.Empty;
			}
		}

		private static string NutFormatMoney(decimal val, string nutCurrency, string language)
		{
			bool minus = val < 0;
			if (minus)
				val = -val;

			var result = val.ToText(nutCurrency, language);

			if (minus)
				result = "−" + result;

			if (result.Length > 0)
				result = char.ToUpper(result[0]) + result.Substring(1);

			return result;
		}
	};
}
