using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CellReport.Core
{
	public class NumberToString
	{
		public static ThreadLocal<string> CurrentCulture = new ThreadLocal<string>(() => null);

		public string[] Hunds;

		public string[] Tens;

		public string[] Frac;

		public string[] Female;

		public string[] Thausand;

		public string[] Million;

		public string[] Milliard;

		public string[] Trillion;

		public string[] Trilliard;

		public string Zero;

		public string Minus;

		public NumberToString()
		{
			if (CurrentCulture.Value == "az")
			{
				Hunds = new[]
				{
					"", "yüz ", "iki yüz ", "üç yüz ", "dörd yüz ",
					"beş yüz ", "altı yüz ", "yeddi yüz ", "səksən yüz ", "doqquz yüz "
				};

				Tens = new[]
				{
					"", "on ", "iyirmi ", "otuz ", "qırx ", "əlli ",
					"altmış ", "yetmiş ", "səksən ", "doxsan "
				};

				Frac = new[]
				{
					"", "bir ", "iki ", "üç ", "dörd ", "beş ", "altı ", "yeddi ", "səkkiz ", "doqquz ", "on ",
					"on bir ", "on iki ", "on üç ", "on dörd ", "on beş ", "on altı ", "on yeddi ", "on səkkiz ", "on doqquz "
				};

				Female = new[]
				{
					"", "iki "
				};

				Zero = "Sıfır ";
				Minus = "Minus ";

				Thausand = new[] { "min", "min", "min" };
				Million = new[] { "milyon", "milyon", "milyon" };
				Milliard = new[] { "milyard", "milyard", "milyard" };
				Trillion = new[] { "trilyon", "trilyon", "trilyon" };
				Trilliard = new[] { "trilyon", "trilyon", "trilyon" };

				return;
			}

			if (CurrentCulture.Value == "uk-UA")
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

		public string GetString(long val, bool male, string one, string two, string five)
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

		public string Case(long val, string one, string two, string five)
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
			Register("KZT", false, "тенге", "тенге", "тенге", "тиын", "тиын", "тиын");
			Register("TJS", false, "сомони", "сомони", "сомони", "дирамов", "дирамов", "дирамов");
			Register("UZS", false, "сум", "сум", "сум", "тийин", "тийин", "тийин");
			Register("AZN", true, "manat", "manat", "manat", "qəpik", "qəpik", "qəpik");
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
			if (NumberToString.CurrentCulture.Value == "uk-UA")
				return GetString(val, "UAH");

			if (NumberToString.CurrentCulture.Value == "kk-KZ")
				return GetString(val, "KZT");

			if (NumberToString.CurrentCulture.Value == "tg-Cyrl-TJ")
				return GetString(val, "TJS");

			if (NumberToString.CurrentCulture.Value == "az")
				return GetString(val, "AZN");

			return GetString(val, "RUR");
		}

		public static string GetShortCurrencyName()
		{
			if (NumberToString.CurrentCulture.Value == "uk-UA")
				return "грн.";

			if (NumberToString.CurrentCulture.Value == "kk-KZ")
				return "тг.";

			if (NumberToString.CurrentCulture.Value == "tg-Cyrl-TJ")
				return "смн.";

			return "руб.";
		}

		public static string GetString(decimal val, string currency)
		{
			if (Currencies.TryGetValue(currency, out var info))
			{
				return GetString(val, info.Male,
						info.SeniorOne, info.SeniorTwo, info.SeniorFive,
						info.JuniorOne, info.JuniorTwo, info.JuniorFive);
			}
			else
				throw new ArgumentOutOfRangeException("currency", "Currency \"" + currency + "\" does not register");

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
			var numToStr = new NumberToString();

			if (0 == n)
				r.Append(numToStr.Zero);
			r.Append(n % 1000 != 0 ? numToStr.GetString(n, male, seniorOne, seniorTwo, seniorFive) : seniorFive + " ");

			n /= 1000;

			r.Insert(0, numToStr.GetString(n, false, numToStr.Thausand[0], numToStr.Thausand[1], numToStr.Thausand[2]));
			n /= 1000;

			r.Insert(0, numToStr.GetString(n, true, numToStr.Million[0], numToStr.Million[1], numToStr.Million[2]));
			n /= 1000;

			r.Insert(0, numToStr.GetString(n, true, numToStr.Milliard[0], numToStr.Milliard[1], numToStr.Milliard[2]));
			n /= 1000;

			r.Insert(0, numToStr.GetString(n, true, numToStr.Trillion[0], numToStr.Trillion[1], numToStr.Trillion[2]));
			n /= 1000;

			r.Insert(0, numToStr.GetString(n, true, numToStr.Trilliard[0], numToStr.Trilliard[1], numToStr.Trilliard[2]));
			if (minus)
				r.Insert(0, numToStr.Minus);

			if (remainder != 0 || !withoutRemainder)
				r.Append(remainder.ToString("00 "));

			r.Append(numToStr.Case(remainder, juniorOne, juniorTwo, juniorFive));

			//Делаем первую букву заглавной
			r[0] = char.ToUpper(r[0]);

			return r.ToString();
		}
	};
}
