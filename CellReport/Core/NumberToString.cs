using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace CellReport.Core
{
	public class NumberToString
	{
		public static string[] Hunds;

		public static string[] Tens;

		public static string[] Frac;

		public static string[] Female;

		public static string[] Thausand;

		public static string[] Million;

		public static string[] Milliard;

		public static string[] Trillion;

		public static string[] Trilliard;

		public static string Zero;

		public static string Minus;

		static NumberToString()
		{
			if (System.Globalization.CultureInfo.CurrentCulture.Name == "uk-UA")
			{
				Hunds = new[]
				{
					"", "сто ", "двісті ", "триста ", "чотириста ",
					"п’ятсот ", "шістсот ", "сімсот ", "вісімсот ", "дев’ятсот "
				};

				Tens = new[]
				{
					"", "десять ", "двадцять ", "тридцять ", "сорок ", "п’ятдесят ",
					"шістдесят ", "сімдесят ", "вісімдесят ", "дев’яносто "
				};

				Frac = new[]
				{
					"", "один ", "два ", "три ", "чотири ", "п’ять ", "шість ",
					"сім ", "вісім ", "дев’ять ", "десять ", "одинадцять ",
					"дванадцять ", "тринадцять ", "чотирнадцять ", "п’ятнадцять ",
					"шiстнадцять ", "сiмнадцять ", "вiсiмнадцять ", "дев'ятнадцять "
				};

				Female = new[]
				{
					"одна ", "дві "
				};

				Zero = "Нуль ";
				Minus = "Мінус ";

				Thausand = new[] { "тисяча", "тисячі", "тисяч" };
				Million = new[] { "мільйон", "мільйона", "мільйонів" };
				Milliard = new[] { "мільярд", "мільярда", "мільярдів" };
				Trillion = new[] { "трильйон", "трильйона", "трильйонів" };
				Trilliard = new[] { "триллиард", "триллиарда", "триллиардов" };

				return;
			}

			Hunds = new[]
			{
				"", "сто ", "двести ", "триста ", "четыреста ",
				"пятьсот ", "шестьсот ", "семьсот ", "восемьсот ", "девятьсот "
			};

			Tens = new[]
			{
			"", "десять ", "двадцать ", "тридцать ", "сорок ", "пятьдесят ",
			"шестьдесят ", "семьдесят ", "восемьдесят ", "девяносто "
			};

			Frac = new[]
			{
				"", "один ", "два ", "три ", "четыре ", "пять ", "шесть ",
				"семь ", "восемь ", "девять ", "десять ", "одиннадцать ",
				"двенадцать ", "тринадцать ", "четырнадцать ", "пятнадцать ",
				"шестнадцать ", "семнадцать ", "восемнадцать ", "девятнадцать "
			};

			Female = new[]
			{
				"одна ", "две "
			};

			Zero = "Ноль ";
			Minus = "Минус ";

			Thausand = new[] { "тысяча", "тысячи", "тысяч" };
			Million = new[] { "миллион", "миллиона", "миллионов" };
			Milliard = new[] { "миллиард", "миллиарда", "миллиардов" };
			Trillion = new[] { "триллион", "триллиона", "триллионов" };
			Trilliard = new[] { "триллиард", "триллиарда", "триллиардов" };
		}

		public static string GetString(long val, bool male, string one, string two, string five)
		{
			var frac20 = Frac.ToArray();

			var num = val % 1000;
			if (0 == num)
				return "";
			if (num < 0)
				throw new ArgumentOutOfRangeException("val", "Parameter can not be negative");
			if (!male)
			{
				frac20[1] = Female[0];
				frac20[2] = Female[1];
			}

			var r = new StringBuilder(Hunds[num / 100]);

			if (num % 100 < 20)
			{
				r.Append(frac20[num % 100]);
			}
			else
			{
				r.Append(Tens[num % 100 / 10]);
				r.Append(frac20[num % 10]);
			}

			r.Append(Case(num, one, two, five));

			if (r.Length != 0) r.Append(" ");
			return r.ToString();
		}

		public static string Case(long val, string one, string two, string five)
		{
			var t = (val % 100 > 20) ? val % 10 : val % 20;

			switch (t)
			{
				case 1: return one;
				case 2:
				case 3:
				case 4: return two;
				default: return five;
			}
		}
	};

	struct CurrencyInfo
	{
		public bool Male;
		public string SeniorOne;
		public string SeniorTwo;
		public string SeniorFive;
		public string JuniorOne;
		public string JuniorTwo;
		public string JuniorFive;
	};

	public class CurrencyToString
	{
		private static readonly Dictionary<string, CurrencyInfo> Currencies = new Dictionary<string, CurrencyInfo>();

		static CurrencyToString()
		{
			Register("RUR", true, "рубль", "рубля", "рублей", "копейка", "копейки", "копеек");
			Register("UAH", false, "гривня", "гривні", "гривень", "копійка", "копійки", "копійок");
			Register("KZT", false, "тенге", "тенге", "тенге", "тиын", "тиына", "тиынов");
			Register("EUR", true, "евро", "евро", "евро", "евроцент", "евроцента", "евроцентов");
			Register("USD", true, "доллар", "доллара", "долларов", "цент", "цента", "центов");
		}

		public static void Register(string currency, bool male,
				string seniorOne, string seniorTwo, string seniorFive,
				string juniorOne, string juniorTwo, string juniorFive)
		{
			CurrencyInfo info;
			info.Male = male;
			info.SeniorOne = seniorOne; info.SeniorTwo = seniorTwo; info.SeniorFive = seniorFive;
			info.JuniorOne = juniorOne; info.JuniorTwo = juniorTwo; info.JuniorFive = juniorFive;
			Currencies.Add(currency, info);
		}

		public static string GetString(decimal val)
		{
			if (System.Globalization.CultureInfo.CurrentCulture.Name == "uk-UA")
				return GetString(val, "UAH");

			if (System.Globalization.CultureInfo.CurrentCulture.Name == "kk-KZ")
				return GetString(val, "KZT");

			return GetString(val, "RUR");
		}

		public static string GetShortCurrencyName()
		{
			if (System.Globalization.CultureInfo.CurrentCulture.Name == "uk-UA")
				return "грн.";

			if (System.Globalization.CultureInfo.CurrentCulture.Name == "kk-KZ")
				return "тг.";

			return "руб.";
		}

		public static string GetString(decimal val, string currency)
		{
			if (!Currencies.ContainsKey(currency))
				throw new ArgumentOutOfRangeException("currency", "Currency \"" + currency + "\" does not register");

			var info = Currencies[currency];
			return GetString(val, info.Male,
					info.SeniorOne, info.SeniorTwo, info.SeniorFive,
					info.JuniorOne, info.JuniorTwo, info.JuniorFive);
		}

		public static string GetString(decimal val, bool male,
				string seniorOne, string seniorTwo, string seniorFive,
				string juniorOne, string juniorTwo, string juniorFive, bool withoutRemainder = false)
		{
			bool minus = false;
			if (val < 0)
			{
				val = -val;
				minus = true;
			}

			var n = (long)val;
			var remainder = Convert.ToInt64((val - n) * 100);

			var r = new StringBuilder();

			if (0 == n)
				r.Append(NumberToString.Zero);
			r.Append(n % 1000 != 0 ? NumberToString.GetString(n, male, seniorOne, seniorTwo, seniorFive) : seniorFive + " ");

			n /= 1000;

			r.Insert(0, NumberToString.GetString(n, false, NumberToString.Thausand[0], NumberToString.Thausand[1], NumberToString.Thausand[2]));
			n /= 1000;

			r.Insert(0, NumberToString.GetString(n, true, NumberToString.Million[0], NumberToString.Million[1], NumberToString.Million[2]));
			n /= 1000;

			r.Insert(0, NumberToString.GetString(n, true, NumberToString.Milliard[0], NumberToString.Milliard[1], NumberToString.Milliard[2]));
			n /= 1000;

			r.Insert(0, NumberToString.GetString(n, true, NumberToString.Trillion[0], NumberToString.Trillion[1], NumberToString.Trillion[2]));
			n /= 1000;

			r.Insert(0, NumberToString.GetString(n, true, NumberToString.Trilliard[0], NumberToString.Trilliard[1], NumberToString.Trilliard[2]));
			if (minus)
				r.Insert(0, NumberToString.Minus);

			if (remainder != 0 || !withoutRemainder)
				r.Append(remainder.ToString("00 "));

			r.Append(NumberToString.Case(remainder, juniorOne, juniorTwo, juniorFive));

			//Делаем первую букву заглавной
			r[0] = char.ToUpper(r[0]);

			return r.ToString();
		}
	};
}
