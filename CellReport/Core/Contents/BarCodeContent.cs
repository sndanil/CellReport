using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

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
        UCC13 = 1,
        EAN8 = 2,
        UPCA = 3,
        UCC12 = 3,
        UPCE = 4,
        Code39 = 5,
        Code128
    }

    [Serializable]
    public class BarCodeContent : BaseContent
    {
        public static readonly BarCodeContentStyle DefaultStyle = new BarCodeContentStyle();
        private BarCodeDecoder _barCode = null;

        public BarCodeContent(CellManager manager, string code, BarCodeType type, Orientation orientation): base(manager)
        {
            if (type == BarCodeType.Auto)
            {
                switch (code.Length)
                {
                    case 8:
                        type = BarCodeType.EAN8;
                        break;
                    case 12:
                        type = BarCodeType.UPCA;
                        break;
                    case 13:
                        type = BarCodeType.EAN13;
                        break;
                    case 19:
                        type = BarCodeType.Code128;
                        break;
                    default:
                        type = BarCodeType.Code39;
                        break;
                }
            }
            Code = code;
            Type = type;
            Orientation = orientation;
        }

        public BarCodeContent(CellManager manager, BarCodeType type): base(manager)
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

        public string LineMask
        {
            get 
            {
                if (_barCode == null)
                    ProcessCode();

                return _barCode != null ? _barCode.LineMask : string.Empty; 
            }
        }

        public string GetCode()
        {
            return (GetParamValueByTarget("Code") as string ?? Code) ?? string.Empty;
        }

        private void ProcessCode()
        {
            var code = (GetParamValueByTarget("Code") as string ?? Code) ?? string.Empty;
            var type = Type;
            if (GetParamValueByTarget("Type") != null)
                type = (BarCodeType)Enum.Parse(typeof(BarCodeType), GetParamValueByTarget("Type").ToString());

            if (string.IsNullOrEmpty(code.Trim()))
                return;

            ProcessCode(code, type);
        }

        private void ProcessCode(string code, BarCodeType type)
        {
            switch (type)
            {
                case BarCodeType.EAN13: // BarCodeType.UCC13
                    _barCode = new EAN13(code);
                    break;
                case BarCodeType.EAN8: 
                    _barCode = new EAN8(code);
                    break;
                case BarCodeType.UPCA: // UCC12
                    _barCode = new UPCA(code);
                    break;
                case BarCodeType.UPCE:
                    _barCode = new UPCE(code);
                    break;
                case BarCodeType.Code128:
                    _barCode = new Code128(code);
                    break;
                case BarCodeType.Code39:
                    _barCode = new Code39(code);
                    break;
                default:
                case BarCodeType.Auto:
                    if (code != null)
                    {
                        switch (code.Length)
                        {
                            case 8:
                                type = BarCodeType.EAN8;
                                break;
                            case 12:
                                type = BarCodeType.UPCA;
                                break;
                            case 13:
                                type = BarCodeType.EAN13;
                                break;
                            default:
                                type = BarCodeType.Code128;
                                break;
                        }
                        ProcessCode(code, type);
                    }
                    break;
            }
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
            return string.Format("Штрихкод ({0})", code);
        }
    }

    [Serializable]
    internal class BarCodeDecoder
    {
        protected static readonly string[] EANCodeA = new string[] { "0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011" };
        protected static readonly string[] EANCodeB = new string[] { "0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111" };
        protected static readonly string[] EANCodeC = new string[] { "1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100" };
        protected static readonly string[] EANPattern = new string[] { "aaaaaa", "aababb", "aabbab", "aabbba", "abaabb", "abbaab", "abbbaa", "ababab", "ababba", "abbaba" };

        public string Code
        {
            get;
            protected set;
        }

        public string LineMask
        {
            get;
            protected set;
        }

        protected string GetMod10CheckSumm(string code)
        {
            int oddSum = 0;
            int evenSum = 0;
            for (int i = 0; i <= code.Length; i++)
            {
                if ((i % 2) == 0)
                    evenSum += int.Parse(Code[i].ToString(), CultureInfo.InvariantCulture);
                else
                    oddSum += int.Parse(Code[i].ToString(), CultureInfo.InvariantCulture) * 3;
            }
            int checkSum = 10 - (oddSum + evenSum) % 10;
            if (checkSum == 10)
                checkSum = 0;
            return checkSum.ToString();
        }
    }

    [Serializable]
    internal class EAN13 : BarCodeDecoder
    {
        public EAN13(string code)
        {
            if (code.Length < 12 || code.Length > 13)
                throw new ArgumentException("code");

            if (code.Length == 12)
                code += GetMod10CheckSumm(code);
            Code = code;
            LineMask = ProcessEAN13();
        }

        private string ProcessEAN13()
        {
            string pattern = EANPattern[int.Parse(Code[0].ToString(), CultureInfo.InvariantCulture)];
            string result = "101";
            for (int i = 0; i < 6; i++)
            {
                if (pattern[i] == 'a')
                    result += EANCodeA[int.Parse(Code[i + 1].ToString(), CultureInfo.InvariantCulture)];
                else
                    result += EANCodeB[int.Parse(Code[i + 1].ToString(), CultureInfo.InvariantCulture)];
            }
            result = result + "01010";
            int index = 1;
            while (index <= 5)
            {
                char ch4 = Code[index++ + 6];
                result += EANCodeC[int.Parse(ch4.ToString(), CultureInfo.InvariantCulture)];
            }
            index = int.Parse(Code[Code.Length - 1].ToString(), CultureInfo.InvariantCulture);
            return result + EANCodeC[index] + "101";
        }
    }

    [Serializable]
    internal class EAN8 : BarCodeDecoder
    {
        public EAN8(string code)
        {
            if (code.Length < 7 || code.Length > 8)
                throw new ArgumentException("code");

            Code = code;
            this.CheckDigit();
            LineMask = ProcessEAN8();
        }

        private void CheckDigit()
        {
            if (Code.Length == 7)
            {
                int oddSum = 0;
                int evenSum = 0;
                for (int i = 0; i < Code.Length; i++)
                {
                    if (i % 2 == 0)
                        evenSum += int.Parse(Code[i].ToString(), CultureInfo.InvariantCulture) * 3;
                    else
                        oddSum += int.Parse(Code[i].ToString(), CultureInfo.InvariantCulture);
                }
                var checkSum = 10 - (oddSum + evenSum) % 10;
                if (checkSum == 10)
                    checkSum = 0;
                Code += checkSum.ToString();
            }
        }

        private string ProcessEAN8()
        {
            string result = "101";
            for (int i = 0; i < (Code.Length / 2); i++)
                result += EANCodeA[int.Parse(Code[i].ToString(), CultureInfo.InvariantCulture)];
            result += "01010";
            for (int j = Code.Length / 2; j < Code.Length; j++)
                result += EANCodeC[int.Parse(Code[j].ToString(), CultureInfo.InvariantCulture)];
            return result + "101";
        }
    }

    [Serializable]
    internal class UPCA : BarCodeDecoder
    {
        private static readonly string[] UPCCodeA = new string[] { "0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011" };
        private static readonly string[] UPCCodeB = new string[] { "0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111" };
        private static readonly string[] UPCCodeC = new string[] { "1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100" };
        private static readonly string[] UPCPattern = new string[] { "aaaaaa", "aababb", "aabbab", "aabbba", "abaabb", "abbaab", "abbbaa", "ababab", "ababba", "abbaba" };

        public UPCA(string code)
        {
            if (code.Length != 12)
                throw new ArgumentException("code");

            Code = code;
            LineMask = ProcessUPCA();
        }

        private string ProcessUPCA()
        {
            string pattern = UPCPattern[int.Parse(Code[0].ToString(), CultureInfo.InvariantCulture)];
            var result = "101" + UPCCodeA[int.Parse(Code[0].ToString(), CultureInfo.InvariantCulture)];
            for (int i = 1; i < pattern.Length; i++)
            {
                if (pattern[i] == 'a')
                    result += UPCCodeA[int.Parse(Code[i].ToString(), CultureInfo.InvariantCulture)];
                else
                    result += UPCCodeB[int.Parse(Code[i].ToString(), CultureInfo.InvariantCulture)];
            }
            result += "01010";
            var index = 0;
            while (index < 5)
                result += UPCCodeC[int.Parse(Code[index++ + 6].ToString(), CultureInfo.InvariantCulture)];
            return result + UPCCodeC[int.Parse(Code[Code.Length - 1].ToString(), CultureInfo.InvariantCulture)] + "101";
        }
    }

    [Serializable]
    internal class UPCE : BarCodeDecoder
    {
        private static readonly string[] UPCECode0 = new string[] { "bbbaaa", "bbabaa", "bbaaba", "bbaaab", "babbaa", "baabba", "baaabb", "bababa", "babaab", "baabab" };
        private static readonly string[] UPCECode1 = new string[] { "aaabbb", "aababb", "aabbab", "aabbba", "abaabb", "abbaab", "abbbaa", "ababab", "ababba", "abbaba" };

        public UPCE(string code)
        {
            if (code.Length != 8 && code.Length != 12)
                throw new ArgumentException("code");
            Code = code;
            LineMask = ProcessUPCE();
        }

        private string ProcessUPCE()
        {
            int index = int.Parse(Code[Code.Length - 1].ToString(), CultureInfo.InvariantCulture);
            var pattern = "";
            if (Code[0] == '0')
                pattern = UPCECode0[index];
            else
                pattern = UPCECode1[index];
            var result = "101";
            index = 0;
            foreach (char ch in pattern)
            {
                int codeIndex = int.Parse(Code[index].ToString(), CultureInfo.InvariantCulture);
                switch (ch)
                {
                    case 'a':
                        result += EANCodeA[codeIndex];
                        break;
                    case 'b':
                        result += EANCodeB[codeIndex];
                        break;
                }
                index++;
            }
            return result + "01010" + "1";
        }
    }

    [Serializable]
    internal class Code39 : BarCodeDecoder
    {
        private bool _allowExtended;
        private static readonly Dictionary<char, string> C39Code = new Dictionary<char,string>();
        private static readonly Dictionary<string, string> ExtC39Translation = new Dictionary<string,string>();

        static Code39()
        {
            C39Code.Add('0', "101001101101");
            C39Code.Add('1', "110100101011");
            C39Code.Add('2', "101100101011");
            C39Code.Add('3', "110110010101");
            C39Code.Add('4', "101001101011");
            C39Code.Add('5', "110100110101");
            C39Code.Add('6', "101100110101");
            C39Code.Add('7', "101001011011");
            C39Code.Add('8', "110100101101");
            C39Code.Add('9', "101100101101");
            C39Code.Add('A', "110101001011");
            C39Code.Add('B', "101101001011");
            C39Code.Add('C', "110110100101");
            C39Code.Add('D', "101011001011");
            C39Code.Add('E', "110101100101");
            C39Code.Add('F', "101101100101");
            C39Code.Add('G', "101010011011");
            C39Code.Add('H', "110101001101");
            C39Code.Add('I', "101101001101");
            C39Code.Add('J', "101011001101");
            C39Code.Add('K', "110101010011");
            C39Code.Add('L', "101101010011");
            C39Code.Add('M', "110110101001");
            C39Code.Add('N', "101011010011");
            C39Code.Add('O', "110101101001");
            C39Code.Add('P', "101101101001");
            C39Code.Add('Q', "101010110011");
            C39Code.Add('R', "110101011001");
            C39Code.Add('S', "101101011001");
            C39Code.Add('T', "101011011001");
            C39Code.Add('U', "110010101011");
            C39Code.Add('V', "100110101011");
            C39Code.Add('W', "110011010101");
            C39Code.Add('X', "100101101011");
            C39Code.Add('Y', "110010110101");
            C39Code.Add('Z', "100110110101");
            C39Code.Add('-', "100101011011");
            C39Code.Add('.', "110010101101");
            C39Code.Add(' ', "100110101101");
            C39Code.Add('$', "100100100101");
            C39Code.Add('/', "100100101001");
            C39Code.Add('+', "100101001001");
            C39Code.Add('%', "101001001001");
            C39Code.Add('*', "100101101101");

            ExtC39Translation.Add(Convert.ToChar(0).ToString(), "%U");
            ExtC39Translation.Add(Convert.ToChar(1).ToString(), "$A");
            ExtC39Translation.Add(Convert.ToChar(2).ToString(), "$B");
            ExtC39Translation.Add(Convert.ToChar(3).ToString(), "$C");
            ExtC39Translation.Add(Convert.ToChar(4).ToString(), "$D");
            ExtC39Translation.Add(Convert.ToChar(5).ToString(), "$E");
            ExtC39Translation.Add(Convert.ToChar(6).ToString(), "$F");
            ExtC39Translation.Add(Convert.ToChar(7).ToString(), "$G");
            ExtC39Translation.Add(Convert.ToChar(8).ToString(), "$H");
            ExtC39Translation.Add(Convert.ToChar(9).ToString(), "$I");
            ExtC39Translation.Add(Convert.ToChar(10).ToString(), "$J");
            ExtC39Translation.Add(Convert.ToChar(11).ToString(), "$K");
            ExtC39Translation.Add(Convert.ToChar(12).ToString(), "$L");
            ExtC39Translation.Add(Convert.ToChar(13).ToString(), "$M");
            ExtC39Translation.Add(Convert.ToChar(14).ToString(), "$N");
            ExtC39Translation.Add(Convert.ToChar(15).ToString(), "$O");
            ExtC39Translation.Add(Convert.ToChar(0x10).ToString(), "$P");
            ExtC39Translation.Add(Convert.ToChar(0x11).ToString(), "$Q");
            ExtC39Translation.Add(Convert.ToChar(0x12).ToString(), "$R");
            ExtC39Translation.Add(Convert.ToChar(0x13).ToString(), "$S");
            ExtC39Translation.Add(Convert.ToChar(20).ToString(), "$T");
            ExtC39Translation.Add(Convert.ToChar(0x15).ToString(), "$U");
            ExtC39Translation.Add(Convert.ToChar(0x16).ToString(), "$V");
            ExtC39Translation.Add(Convert.ToChar(0x17).ToString(), "$W");
            ExtC39Translation.Add(Convert.ToChar(0x18).ToString(), "$X");
            ExtC39Translation.Add(Convert.ToChar(0x19).ToString(), "$Y");
            ExtC39Translation.Add(Convert.ToChar(0x1a).ToString(), "$Z");
            ExtC39Translation.Add(Convert.ToChar(0x1b).ToString(), "%A");
            ExtC39Translation.Add(Convert.ToChar(0x1c).ToString(), "%B");
            ExtC39Translation.Add(Convert.ToChar(0x1d).ToString(), "%C");
            ExtC39Translation.Add(Convert.ToChar(30).ToString(), "%D");
            ExtC39Translation.Add(Convert.ToChar(0x1f).ToString(), "%E");
            ExtC39Translation.Add("!", "/A");
            ExtC39Translation.Add("\"", "/B");
            ExtC39Translation.Add("#", "/C");
            ExtC39Translation.Add("$", "/D");
            ExtC39Translation.Add("%", "/E");
            ExtC39Translation.Add("&", "/F");
            ExtC39Translation.Add("'", "/G");
            ExtC39Translation.Add("(", "/H");
            ExtC39Translation.Add(")", "/I");
            ExtC39Translation.Add("*", "/J");
            ExtC39Translation.Add("+", "/K");
            ExtC39Translation.Add(",", "/L");
            ExtC39Translation.Add("/", "/O");
            ExtC39Translation.Add(":", "/Z");
            ExtC39Translation.Add(";", "%F");
            ExtC39Translation.Add("<", "%G");
            ExtC39Translation.Add("=", "%H");
            ExtC39Translation.Add(">", "%I");
            ExtC39Translation.Add("?", "%J");
            ExtC39Translation.Add("[", "%K");
            ExtC39Translation.Add(@"\", "%L");
            ExtC39Translation.Add("]", "%M");
            ExtC39Translation.Add("^", "%N");
            ExtC39Translation.Add("_", "%O");
            ExtC39Translation.Add("{", "%P");
            ExtC39Translation.Add("|", "%Q");
            ExtC39Translation.Add("}", "%R");
            ExtC39Translation.Add("~", "%S");
            ExtC39Translation.Add("`", "%W");
            ExtC39Translation.Add("@", "%V");
            ExtC39Translation.Add("a", "+A");
            ExtC39Translation.Add("b", "+B");
            ExtC39Translation.Add("c", "+C");
            ExtC39Translation.Add("d", "+D");
            ExtC39Translation.Add("e", "+E");
            ExtC39Translation.Add("f", "+F");
            ExtC39Translation.Add("g", "+G");
            ExtC39Translation.Add("h", "+H");
            ExtC39Translation.Add("i", "+I");
            ExtC39Translation.Add("j", "+J");
            ExtC39Translation.Add("k", "+K");
            ExtC39Translation.Add("l", "+L");
            ExtC39Translation.Add("m", "+M");
            ExtC39Translation.Add("n", "+N");
            ExtC39Translation.Add("o", "+O");
            ExtC39Translation.Add("p", "+P");
            ExtC39Translation.Add("q", "+Q");
            ExtC39Translation.Add("r", "+R");
            ExtC39Translation.Add("s", "+S");
            ExtC39Translation.Add("t", "+T");
            ExtC39Translation.Add("u", "+U");
            ExtC39Translation.Add("v", "+V");
            ExtC39Translation.Add("w", "+W");
            ExtC39Translation.Add("x", "+X");
            ExtC39Translation.Add("y", "+Y");
            ExtC39Translation.Add("z", "+Z");
            ExtC39Translation.Add(Convert.ToChar(0x7f).ToString(), "%T");
        }

        public Code39(string code)
        {
            Code = code;
            LineMask = ProcessCode39();
        }

        public Code39(string code, bool allowExtended): this(code)
        {
            _allowExtended = allowExtended;
        }

        private string ProcessCode39()
        {
            string formattedData = Code;
            if (_allowExtended)
                InsertExtendedCharsIfNeeded(ref formattedData);
            string result = C39Code['*'].ToString() + "0";
            foreach (char ch in formattedData)
            {
                string c39Code = null;
                if (C39Code.TryGetValue(ch, out c39Code))
                    result += (c39Code + "0");
                else
                    throw new ApplicationException("Invalid C39 code");
            }
            return result + C39Code['*'];
        }

        private void InsertExtendedCharsIfNeeded(ref string formattedData)
        {
            string result = string.Empty;
            foreach (char ch in Code)
            {
                if (C39Code.ContainsKey(ch))
                    result += ch;
                else
                    result += ExtC39Translation[ch.ToString()];
            }
            formattedData = result;
        }
    }

    internal class Code128 : BarCodeDecoder
    {
        public enum TYPES
        {
            DYNAMIC,
            A,
            B,
            C
        }

        class Code128Data
        {
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
            public string Value { get; set; }
            public string Encoding { get; set; }

            public string GetByIndex(int index)
            {
                switch (index)
                {
                    case 0: return Value;
                    case 1: return A;
                    case 2: return B;
                    case 3: return C;
                    default:
                    case 4: return Encoding;
                }
            }
        }

        private List<Code128Data> _data = new List<Code128Data>();
        private List<string> _formattedData;
        private Code128Data _startCharacter;
        private TYPES _type;

        public Code128(string code)
        {
            _formattedData = new List<string>();
            Code = code;
            Initialize();
            LineMask = GetEncoding();
        }

        public Code128(string code, TYPES type): this(code)
        {
            _type = type;
        }

        private void BreakUpDataForEncoding()
        {
            string item = "";
            foreach (char ch in Code)
            {
                if (char.IsNumber(ch))
                {
                    if (item == "")
                        item = item + ch;
                    else
                    {
                        item = item + ch;
                        this._formattedData.Add(item);
                        item = "";
                    }
                }
                else
                {
                    if (item != "")
                    {
                        this._formattedData.Add(item);
                        item = "";
                    }
                    this._formattedData.Add(ch.ToString());
                }
            }
            if (item != "")
                this._formattedData.Add(item);
        }

        private string CalculateCheckDigit()
        {
            uint checkSum = 0;
            for (var i = 0; i < this._formattedData.Count; i++)
            {
                var str = this._formattedData[i];
                var items = _data.Where(d => d.A == str).ToList();
                if (items.Count == 0)
                    items = _data.Where(d => d.B == str).ToList();
                if (items.Count == 0)
                    items = _data.Where(d => d.C == str).ToList();
                checkSum += (uint.Parse(items[0].Value) * ((i == 0) ? 1 : Convert.ToUInt32(i)));
            }
            return _data.Where(d => d.Value == (checkSum % 0x67).ToString()).First().Encoding;
        }

        private List<Code128Data> FindStartCodeCharacter(string s, ref int col)
        {
            var result = new List<Code128Data>();
            if (((s.Length > 1) && char.IsNumber(s[0])) && char.IsNumber(s[1]))
            {
                if (this._startCharacter == null)
                    result.Add(_startCharacter = _data.First(d => d.A == "START_C"));
                else
                    result.Add(_data.First(d => d.A == "CODE_C"));
                col = 1;
                return result;
            }
            bool findA = false;
            bool findB = false;
            foreach (var item in _data)
            {
                if (!findA && s == item.A)
                {
                    findA = true;
                    col = 2;
                    if (this._startCharacter == null)
                        result.Add(_startCharacter = _data.First(d => d.A == "START_A"));
                    else
                        result.Add(_data.First(d => d.B == "CODE_A"));
                    continue;
                } 
                else if (!findB && s == item.B)
                {
                    findB = true;
                    col = 1;
                    if (this._startCharacter == null)
                        result.Add(_startCharacter = _data.First(d => d.A == "START_B"));
                    else
                        result.Add(_data.First(d => d.B == "CODE_B"));
                    continue;
                } 
                else if (!findA || !findB)
                    continue;
                break;
            }
            if (result.Count == 0)
                throw new ApplicationException("Could not determine start character.");

            return result;
        }

        private string GetEncoding()
        {
            this.BreakUpDataForEncoding();
            this.InsertStartandCodeCharacters();
            this.CalculateCheckDigit();
            var result = new StringBuilder();
            foreach (var item in this._formattedData)
            {
                var data = _data.FirstOrDefault(d => d.A == item);
                if (data == null)
                {
                    data = _data.FirstOrDefault(d => d.B == item);
                    if (data == null)
                        data = _data.FirstOrDefault(d => d.C == item);
                }
                if (data == null)
                    throw new ApplicationException("Could not find encoding of a value( " + item + " ) in the formatted data.");
                
                result.Append(data.Encoding);
            }
            result.Append(CalculateCheckDigit());
            result.Append(_data.First(d => d.A == "STOP").Encoding);
            result.Append("11");

            return result.ToString();
        }

        private void Initialize()
        {
            _data.Add(new Code128Data() { Value = "0", A = " ", B = " ", C = "00", Encoding = "11011001100" });
            _data.Add(new Code128Data() { Value = "1", A = "!", B = "!", C = "01", Encoding = "11001101100" });
            _data.Add(new Code128Data() { Value = "2", A = "\"", B = "\"", C = "02", Encoding = "11001100110" });
            _data.Add(new Code128Data() { Value = "3", A = "#", B = "#", C = "03", Encoding = "10010011000" });
            _data.Add(new Code128Data() { Value = "4", A = "$", B = "$", C = "04", Encoding = "10010001100" });
            _data.Add(new Code128Data() { Value = "5", A = "%", B = "%", C = "05", Encoding = "10001001100" });
            _data.Add(new Code128Data() { Value = "6", A = "&", B = "&", C = "06", Encoding = "10011001000" });
            _data.Add(new Code128Data() { Value = "7", A = "'", B = "'", C = "07", Encoding = "10011000100" });
            _data.Add(new Code128Data() { Value = "8", A = "(", B = "(", C = "08", Encoding = "10001100100" });
            _data.Add(new Code128Data() { Value = "9", A = ")", B = ")", C = "09", Encoding = "11001001000" });
            _data.Add(new Code128Data() { Value = "10", A = "*", B = "*", C = "10", Encoding = "11001000100" });
            _data.Add(new Code128Data() { Value = "11", A = "+", B = "+", C = "11", Encoding = "11000100100" });
            _data.Add(new Code128Data() { Value = "12", A = ",", B = ",", C = "12", Encoding = "10110011100" });
            _data.Add(new Code128Data() { Value = "13", A = "-", B = "-", C = "13", Encoding = "10011011100" });
            _data.Add(new Code128Data() { Value = "14", A = ".", B = ".", C = "14", Encoding = "10011001110" });
            _data.Add(new Code128Data() { Value = "15", A = "/", B = "/", C = "15", Encoding = "10111001100" });
            _data.Add(new Code128Data() { Value = "16", A = "0", B = "0", C = "16", Encoding = "10011101100" });
            _data.Add(new Code128Data() { Value = "17", A = "1", B = "1", C = "17", Encoding = "10011100110" });
            _data.Add(new Code128Data() { Value = "18", A = "2", B = "2", C = "18", Encoding = "11001110010" });
            _data.Add(new Code128Data() { Value = "19", A = "3", B = "3", C = "19", Encoding = "11001011100" });
            _data.Add(new Code128Data() { Value = "20", A = "4", B = "4", C = "20", Encoding = "11001001110" });
            _data.Add(new Code128Data() { Value = "21", A = "5", B = "5", C = "21", Encoding = "11011100100" });
            _data.Add(new Code128Data() { Value = "22", A = "6", B = "6", C = "22", Encoding = "11001110100" });
            _data.Add(new Code128Data() { Value = "23", A = "7", B = "7", C = "23", Encoding = "11101101110" });
            _data.Add(new Code128Data() { Value = "24", A = "8", B = "8", C = "24", Encoding = "11101001100" });
            _data.Add(new Code128Data() { Value = "25", A = "9", B = "9", C = "25", Encoding = "11100101100" });
            _data.Add(new Code128Data() { Value = "26", A = ":", B = ":", C = "26", Encoding = "11100100110" });
            _data.Add(new Code128Data() { Value = "27", A = ";", B = ";", C = "27", Encoding = "11101100100" });
            _data.Add(new Code128Data() { Value = "28", A = "<", B = "<", C = "28", Encoding = "11100110100" });
            _data.Add(new Code128Data() { Value = "29", A = "=", B = "=", C = "29", Encoding = "11100110010" });
            _data.Add(new Code128Data() { Value = "30", A = ">", B = ">", C = "30", Encoding = "11011011000" });
            _data.Add(new Code128Data() { Value = "31", A = "?", B = "?", C = "31", Encoding = "11011000110" });
            _data.Add(new Code128Data() { Value = "32", A = "@", B = "@", C = "32", Encoding = "11000110110" });
            _data.Add(new Code128Data() { Value = "33", A = "A", B = "A", C = "33", Encoding = "10100011000" });
            _data.Add(new Code128Data() { Value = "34", A = "B", B = "B", C = "34", Encoding = "10001011000" });
            _data.Add(new Code128Data() { Value = "35", A = "C", B = "C", C = "35", Encoding = "10001000110" });
            _data.Add(new Code128Data() { Value = "36", A = "D", B = "D", C = "36", Encoding = "10110001000" });
            _data.Add(new Code128Data() { Value = "37", A = "E", B = "E", C = "37", Encoding = "10001101000" });
            _data.Add(new Code128Data() { Value = "38", A = "F", B = "F", C = "38", Encoding = "10001100010" });
            _data.Add(new Code128Data() { Value = "39", A = "G", B = "G", C = "39", Encoding = "11010001000" });
            _data.Add(new Code128Data() { Value = "40", A = "H", B = "H", C = "40", Encoding = "11000101000" });
            _data.Add(new Code128Data() { Value = "41", A = "I", B = "I", C = "41", Encoding = "11000100010" });
            _data.Add(new Code128Data() { Value = "42", A = "J", B = "J", C = "42", Encoding = "10110111000" });
            _data.Add(new Code128Data() { Value = "43", A = "K", B = "K", C = "43", Encoding = "10110001110" });
            _data.Add(new Code128Data() { Value = "44", A = "L", B = "L", C = "44", Encoding = "10001101110" });
            _data.Add(new Code128Data() { Value = "45", A = "M", B = "M", C = "45", Encoding = "10111011000" });
            _data.Add(new Code128Data() { Value = "46", A = "N", B = "N", C = "46", Encoding = "10111000110" });
            _data.Add(new Code128Data() { Value = "47", A = "O", B = "O", C = "47", Encoding = "10001110110" });
            _data.Add(new Code128Data() { Value = "48", A = "P", B = "O", C = "48", Encoding = "11101110110" });
            _data.Add(new Code128Data() { Value = "49", A = "Q", B = "Q", C = "49", Encoding = "11010001110" });
            _data.Add(new Code128Data() { Value = "50", A = "R", B = "R", C = "50", Encoding = "11000101110" });
            _data.Add(new Code128Data() { Value = "51", A = "S", B = "S", C = "51", Encoding = "11011101000" });
            _data.Add(new Code128Data() { Value = "52", A = "T", B = "T", C = "52", Encoding = "11011100010" });
            _data.Add(new Code128Data() { Value = "53", A = "U", B = "U", C = "53", Encoding = "11011101110" });
            _data.Add(new Code128Data() { Value = "54", A = "V", B = "V", C = "54", Encoding = "11101011000" });
            _data.Add(new Code128Data() { Value = "55", A = "W", B = "W", C = "55", Encoding = "11101000110" });
            _data.Add(new Code128Data() { Value = "56", A = "X", B = "X", C = "56", Encoding = "11100010110" });
            _data.Add(new Code128Data() { Value = "57", A = "Y", B = "Y", C = "57", Encoding = "11101101000" });
            _data.Add(new Code128Data() { Value = "58", A = "Z", B = "Z", C = "58", Encoding = "11101100010" });
            _data.Add(new Code128Data() { Value = "59", A = "[", B = "[", C = "59", Encoding = "11100011010" });
            _data.Add(new Code128Data() { Value = "60", A = "\\", B = "\\", C = "60", Encoding = "11101111010" });
            _data.Add(new Code128Data() { Value = "61", A = "]", B = "]", C = "61", Encoding = "11001000010" });
            _data.Add(new Code128Data() { Value = "62", A = "^", B = "^", C = "62", Encoding = "11110001010" });
            _data.Add(new Code128Data() { Value = "63", A = "_", B = "_", C = "63", Encoding = "10100110000" });
            _data.Add(new Code128Data() { Value = "64", A = "\0", B = "`", C = "64", Encoding = "10100001100" });
            _data.Add(new Code128Data() { Value = "65", A = Convert.ToChar(1).ToString(), B = "a", C = "65", Encoding = "10010110000" });
            _data.Add(new Code128Data() { Value = "66", A = Convert.ToChar(2).ToString(), B = "b", C = "66", Encoding = "10010000110" });
            _data.Add(new Code128Data() { Value = "67", A = Convert.ToChar(3).ToString(), B = "c", C = "67", Encoding = "10000101100" });
            _data.Add(new Code128Data() { Value = "68", A = Convert.ToChar(4).ToString(), B = "d", C = "68", Encoding = "10000100110" });
            _data.Add(new Code128Data() { Value = "69", A = Convert.ToChar(5).ToString(), B = "e", C = "69", Encoding = "10110010000" });
            _data.Add(new Code128Data() { Value = "70", A = Convert.ToChar(6).ToString(), B = "f", C = "70", Encoding = "10110000100" });
            _data.Add(new Code128Data() { Value = "71", A = Convert.ToChar(7).ToString(), B = "g", C = "71", Encoding = "10011010000" });
            _data.Add(new Code128Data() { Value = "72", A = Convert.ToChar(8).ToString(), B = "h", C = "72", Encoding = "10011000010" });
            _data.Add(new Code128Data() { Value = "73", A = Convert.ToChar(9).ToString(), B = "i", C = "73", Encoding = "10000110100" });
            _data.Add(new Code128Data() { Value = "74", A = Convert.ToChar(10).ToString(), B = "j", C = "74", Encoding = "10000110010" });
            _data.Add(new Code128Data() { Value = "75", A = Convert.ToChar(11).ToString(), B = "k", C = "75", Encoding = "11000010010" });
            _data.Add(new Code128Data() { Value = "76", A = Convert.ToChar(12).ToString(), B = "l", C = "76", Encoding = "11001010000" });
            _data.Add(new Code128Data() { Value = "77", A = Convert.ToChar(13).ToString(), B = "m", C = "77", Encoding = "11110111010" });
            _data.Add(new Code128Data() { Value = "78", A = Convert.ToChar(14).ToString(), B = "n", C = "78", Encoding = "11000010100" });
            _data.Add(new Code128Data() { Value = "79", A = Convert.ToChar(15).ToString(), B = "o", C = "79", Encoding = "10001111010" });
            _data.Add(new Code128Data() { Value = "80", A = Convert.ToChar(0x10).ToString(), B = "p", C = "80", Encoding = "10100111100" });
            _data.Add(new Code128Data() { Value = "81", A = Convert.ToChar(0x11).ToString(), B = "q", C = "81", Encoding = "10010111100" });
            _data.Add(new Code128Data() { Value = "82", A = Convert.ToChar(0x12).ToString(), B = "r", C = "82", Encoding = "10010011110" });
            _data.Add(new Code128Data() { Value = "83", A = Convert.ToChar(0x13).ToString(), B = "s", C = "83", Encoding = "10111100100" });
            _data.Add(new Code128Data() { Value = "84", A = Convert.ToChar(20).ToString(), B = "t", C = "84", Encoding = "10011110100" });
            _data.Add(new Code128Data() { Value = "85", A = Convert.ToChar(0x15).ToString(), B = "u", C = "85", Encoding = "10011110010" });
            _data.Add(new Code128Data() { Value = "86", A = Convert.ToChar(0x16).ToString(), B = "v", C = "86", Encoding = "11110100100" });
            _data.Add(new Code128Data() { Value = "87", A = Convert.ToChar(0x17).ToString(), B = "w", C = "87", Encoding = "11110010100" });
            _data.Add(new Code128Data() { Value = "88", A = Convert.ToChar(0x18).ToString(), B = "x", C = "88", Encoding = "11110010010" });
            _data.Add(new Code128Data() { Value = "89", A = Convert.ToChar(0x19).ToString(), B = "y", C = "89", Encoding = "11011011110" });
            _data.Add(new Code128Data() { Value = "90", A = Convert.ToChar(0x1a).ToString(), B = "z", C = "90", Encoding = "11011110110" });
            _data.Add(new Code128Data() { Value = "91", A = Convert.ToChar(0x1b).ToString(), B = "{", C = "91", Encoding = "11110110110" });
            _data.Add(new Code128Data() { Value = "92", A = Convert.ToChar(0x1c).ToString(), B = "|", C = "92", Encoding = "10101111000" });
            _data.Add(new Code128Data() { Value = "93", A = Convert.ToChar(0x1d).ToString(), B = "}", C = "93", Encoding = "10100011110" });
            _data.Add(new Code128Data() { Value = "94", A = Convert.ToChar(30).ToString(), B = "~", C = "94", Encoding = "10001011110" });
            _data.Add(new Code128Data() { Value = "95", A = Convert.ToChar(0x1f).ToString(), B = Convert.ToChar(0x7f).ToString(), C = "95", Encoding = "10111101000" });
            _data.Add(new Code128Data() { Value = "96", A = Convert.ToChar(0xca).ToString(), B = Convert.ToChar(0xca).ToString(), C = "96", Encoding = "10111100010" });
            _data.Add(new Code128Data() { Value = "97", A = Convert.ToChar(0xc9).ToString(), B = Convert.ToChar(0xc9).ToString(), C = "97", Encoding = "11110101000" });
            _data.Add(new Code128Data() { Value = "98", A = "SHIFT", B = "SHIFT", C = "98", Encoding = "11110100010" });
            _data.Add(new Code128Data() { Value = "99", A = "CODE_C", B = "CODE_C", C = "99", Encoding = "10111011110" });
            _data.Add(new Code128Data() { Value = "100", A = "CODE_B", B = Convert.ToChar(0xcb).ToString(), C = "CODE_B", Encoding = "10111101110" });
            _data.Add(new Code128Data() { Value = "101", A = Convert.ToChar(0xcb).ToString(), B = "CODE_A", C = "CODE_A", Encoding = "11101011110" });
            _data.Add(new Code128Data() { Value = "102", A = Convert.ToChar(200).ToString(), B = Convert.ToChar(200).ToString(), C = Convert.ToChar(200).ToString(), Encoding = "11110101110" });
            _data.Add(new Code128Data() { Value = "103", A = "START_A", B = "START_A", C = "START_A", Encoding = "11010000100" });
            _data.Add(new Code128Data() { Value = "104", A = "START_B", B = "START_B", C = "START_B", Encoding = "11010010000" });
            _data.Add(new Code128Data() { Value = "105", A = "START_C", B = "START_C", C = "START_C", Encoding = "11010011100" });
            _data.Add(new Code128Data() { Value = "", A = "STOP", B = "STOP", C = "STOP", Encoding = "11000111010" });
        }

        private void InsertStartandCodeCharacters()
        {
            switch (this._type)
            {
                case TYPES.A:
                    this._formattedData.Insert(0, "START_A");
                    return;

                case TYPES.B:
                    this._formattedData.Insert(0, "START_B");
                    return;

                case TYPES.C:
                    this._formattedData.Insert(0, "START_C");
                    return;

                case TYPES.DYNAMIC:
                    try
                    {
                        var str = "";
                        for (int i = 0; i < this._formattedData.Count; i++)
                        {
                            int index = 0;
                            var list = FindStartCodeCharacter(this._formattedData[i], ref index);
                            if (str == "" || !list.Any(item => item.A.EndsWith(str) || item.B.EndsWith(str) || item.C.EndsWith(str)))
                            {
                                var item = list[0];
                                bool flag2 = true;
                                while (flag2)
                                {
                                    try
                                    {
                                        str = item.GetByIndex(index).Split(new char[] { '_' })[1];
                                        flag2 = false;
                                        continue;
                                    }
                                    catch
                                    {
                                        flag2 = true;
                                        if (index++ > 5)
                                            throw new ApplicationException("No start character found in CurrentCodeSet.");
                                        continue;
                                    }
                                }
                                this._formattedData.Insert(i++, item.GetByIndex(index));
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        throw new ApplicationException("EC128-3: Could not insert start and code characters.\n Message: " + exception.Message);
                    }
                    return;
            }
            throw new  ApplicationException("Unknown start type in fixed type encoding.");
        }
    }
}
