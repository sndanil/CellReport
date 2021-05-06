using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellReport.Core
{
    internal class ColorTable
    {
        class Color
        {
            public byte R { get; set; }
            public byte G { get; set; }
            public byte B { get; set; }
        }

        private static Dictionary<string, Color> _colors = new Dictionary<string, Color>();

        public static bool TryGetColor(string strColor, out byte a, out byte r, out byte g, out byte b)
        {
            a = r = g = b = 0;
            Color color = null;
            if (_colors.TryGetValue(strColor.ToUpper(), out color))
            {
                r = color.R;
                g = color.G;
                b = color.B;
                a = 255;
                return true;
            }

            return false;
        }

        static ColorTable()
        {
            _colors.Add("Black".ToUpper(), new Color() { R = 0x0, G = 0x0, B = 0x0 });
            _colors.Add("Navy".ToUpper(), new Color() { R = 0x0, G = 0x0, B = 0x80 });
            _colors.Add("DarkBlue".ToUpper(), new Color() { R = 0x0, G = 0x0, B = 0x8B });
            _colors.Add("MediumBlue".ToUpper(), new Color() { R = 0x0, G = 0x0, B = 0xCD });
            _colors.Add("Blue".ToUpper(), new Color() { R = 0x0, G = 0x0, B = 0xFF });
            _colors.Add("DarkGreen".ToUpper(), new Color() { R = 0x0, G = 0x64, B = 0x0 });
            _colors.Add("Green".ToUpper(), new Color() { R = 0x0, G = 0x80, B = 0x0 });
            _colors.Add("Teal".ToUpper(), new Color() { R = 0x0, G = 0x80, B = 0x80 });
            _colors.Add("DarkCyan".ToUpper(), new Color() { R = 0x0, G = 0x8B, B = 0x8B });
            _colors.Add("DeepSkyBlue".ToUpper(), new Color() { R = 0x0, G = 0xBF, B = 0xFF });
            _colors.Add("DarkTurquoise".ToUpper(), new Color() { R = 0x0, G = 0xCE, B = 0xD1 });
            _colors.Add("MediumSpringGreen".ToUpper(), new Color() { R = 0x0, G = 0xFA, B = 0x9A });
            _colors.Add("Lime".ToUpper(), new Color() { R = 0x0, G = 0xFF, B = 0x0 });
            _colors.Add("SpingGreen".ToUpper(), new Color() { R = 0x0, G = 0xFF, B = 0x7F });
            _colors.Add("Aqua".ToUpper(), new Color() { R = 0x0, G = 0xFF, B = 0xFF });
            _colors.Add("Cyan".ToUpper(), new Color() { R = 0x0, G = 0xFF, B = 0xFF });
            _colors.Add("MidnightBlue".ToUpper(), new Color() { R = 0x19, G = 0x19, B = 0x70 });
            _colors.Add("DodgerBlue".ToUpper(), new Color() { R = 0x1E, G = 0x90, B = 0xFF });
            _colors.Add("LightSeaGreen".ToUpper(), new Color() { R = 0x20, G = 0xB2, B = 0xAA });
            _colors.Add("ForestGreen".ToUpper(), new Color() { R = 0x22, G = 0x8B, B = 0x22 });
            _colors.Add("SeaGreen".ToUpper(), new Color() { R = 0x2E, G = 0x8B, B = 0x57 });
            _colors.Add("DarkSlateGray".ToUpper(), new Color() { R = 0x2F, G = 0x4F, B = 0x4F });
            _colors.Add("LimeGreen".ToUpper(), new Color() { R = 0x32, G = 0xCD, B = 0x32 });
            _colors.Add("MediumSeaGreen".ToUpper(), new Color() { R = 0x3C, G = 0xB3, B = 0x71 });
            _colors.Add("Turquoise".ToUpper(), new Color() { R = 0x40, G = 0xE0, B = 0xD0 });
            _colors.Add("RoyalBlue".ToUpper(), new Color() { R = 0x41, G = 0x69, B = 0xE1 });
            _colors.Add("SteelBlue".ToUpper(), new Color() { R = 0x46, G = 0x82, B = 0xB4 });
            _colors.Add("DarkSlateBlue".ToUpper(), new Color() { R = 0x48, G = 0x3D, B = 0x8B });
            _colors.Add("MediumTurquoise".ToUpper(), new Color() { R = 0x48, G = 0xD1, B = 0xCC });
            _colors.Add("Indigo".ToUpper(), new Color() { R = 0x4B, G = 0x0, B = 0x82 });
            _colors.Add("DarkOliveGreen".ToUpper(), new Color() { R = 0x55, G = 0x6B, B = 0x2F });
            _colors.Add("CadetBlue".ToUpper(), new Color() { R = 0x5F, G = 0x9E, B = 0xA0 });
            _colors.Add("CornflowerBlue".ToUpper(), new Color() { R = 0x64, G = 0x95, B = 0xED });
            _colors.Add("MediumAquaMarine".ToUpper(), new Color() { R = 0x66, G = 0xCD, B = 0xAA });
            _colors.Add("DimGray".ToUpper(), new Color() { R = 0x69, G = 0x69, B = 0x69 });
            _colors.Add("SlateBlue".ToUpper(), new Color() { R = 0x6A, G = 0x5A, B = 0xCD });
            _colors.Add("LightSlateGray".ToUpper(), new Color() { R = 0x77, G = 0x88, B = 0x99 });
            _colors.Add("MediumSlateBlue".ToUpper(), new Color() { R = 0x7B, G = 0x68, B = 0xEE });
            _colors.Add("LawnGreen".ToUpper(), new Color() { R = 0x7C, G = 0xFC, B = 0x0 });
            _colors.Add("Chartreuse".ToUpper(), new Color() { R = 0x7F, G = 0xFF, B = 0x0 });
            _colors.Add("Aquamarine".ToUpper(), new Color() { R = 0x7F, G = 0xFF, B = 0xD4 });
            _colors.Add("Maroon".ToUpper(), new Color() { R = 0x80, G = 0x0, B = 0x0 });
            _colors.Add("Purple".ToUpper(), new Color() { R = 0x80, G = 0x0, B = 0x80 });
            _colors.Add("Olive".ToUpper(), new Color() { R = 0x80, G = 0x80, B = 0x0 });
            _colors.Add("Gray".ToUpper(), new Color() { R = 0x80, G = 0x80, B = 0x80 });
            _colors.Add("SkyBlue".ToUpper(), new Color() { R = 0x87, G = 0xCE, B = 0xEB });
            _colors.Add("LightSkyBlue".ToUpper(), new Color() { R = 0x87, G = 0xCE, B = 0xFA });
            _colors.Add("BlueViolet".ToUpper(), new Color() { R = 0x8A, G = 0x2B, B = 0xE2 });
            _colors.Add("DarkRed".ToUpper(), new Color() { R = 0x8B, G = 0x0, B = 0x0 });
            _colors.Add("DarkMagenta".ToUpper(), new Color() { R = 0x8B, G = 0x0, B = 0x8B });
            _colors.Add("SaddleBrown".ToUpper(), new Color() { R = 0x8B, G = 0x45, B = 0x13 });
            _colors.Add("DarkSeaGreen".ToUpper(), new Color() { R = 0x8F, G = 0xBC, B = 0x8F });
            _colors.Add("LightGreen".ToUpper(), new Color() { R = 0x90, G = 0xEE, B = 0x90 });
            _colors.Add("MediumPurple".ToUpper(), new Color() { R = 0x93, G = 0x70, B = 0xD8 });
            _colors.Add("DarkViolet".ToUpper(), new Color() { R = 0x94, G = 0x0, B = 0xD3 });
            _colors.Add("PaleGreen".ToUpper(), new Color() { R = 0x98, G = 0xFB, B = 0x98 });
            _colors.Add("DarkOrchid".ToUpper(), new Color() { R = 0x99, G = 0x32, B = 0xCC });
            _colors.Add("YellowGreen".ToUpper(), new Color() { R = 0x9A, G = 0xCD, B = 0x32 });
            _colors.Add("Sienna".ToUpper(), new Color() { R = 0xA0, G = 0x52, B = 0x2D });
            _colors.Add("Brown".ToUpper(), new Color() { R = 0xA5, G = 0x2A, B = 0x2A });
            _colors.Add("DarkGray".ToUpper(), new Color() { R = 0xA9, G = 0xA9, B = 0xA9 });
            _colors.Add("LightBlue".ToUpper(), new Color() { R = 0xAD, G = 0xD8, B = 0xE6 });
            _colors.Add("Silver".ToUpper(), new Color() { R = 0xC0, G = 0xC0, B = 0xC0 });
            _colors.Add("Chocolate".ToUpper(), new Color() { R = 0xD2, G = 0x69, B = 0x1E });
            _colors.Add("Tan".ToUpper(), new Color() { R = 0xD2, G = 0xB4, B = 0x8C });
            _colors.Add("LightGrey".ToUpper(), new Color() { R = 0xD3, G = 0xD3, B = 0xD3 });
            _colors.Add("Plum".ToUpper(), new Color() { R = 0xDD, G = 0xA0, B = 0xDD });
            _colors.Add("Violet".ToUpper(), new Color() { R = 0xEE, G = 0x82, B = 0xEE });
            _colors.Add("Azure".ToUpper(), new Color() { R = 0xF0, G = 0xFF, B = 0xFF });
            _colors.Add("Red".ToUpper(), new Color() { R = 0xFF, G = 0x0, B = 0x0 });
            _colors.Add("Magenta".ToUpper(), new Color() { R = 0xFF, G = 0x0, B = 0xFF });
            _colors.Add("Orange".ToUpper(), new Color() { R = 0xFF, G = 0xA5, B = 0x0 });
            _colors.Add("LightPink".ToUpper(), new Color() { R = 0xFF, G = 0xB6, B = 0xC1 });
            _colors.Add("Pink".ToUpper(), new Color() { R = 0xFF, G = 0xC0, B = 0xCB });
            _colors.Add("Gold".ToUpper(), new Color() { R = 0xFF, G = 0xD7, B = 0x0 });
            _colors.Add("Snow".ToUpper(), new Color() { R = 0xFF, G = 0xFA, B = 0xFA });
            _colors.Add("Yellow".ToUpper(), new Color() { R = 0xFF, G = 0xFF, B = 0x0 });
            _colors.Add("LightYellow".ToUpper(), new Color() { R = 0xFF, G = 0xFF, B = 0xE0 });
            _colors.Add("Ivory".ToUpper(), new Color() { R = 0xFF, G = 0xFF, B = 0xF0 });
            _colors.Add("White".ToUpper(), new Color() { R = 0xFF, G = 0xFF, B = 0xFF });

            //_colors.Add("black".ToUpper(), new Color() { R = 0x0, G = 0x0, B = 0x0 });
            /*
GreenYellow 	#ADFF2F	 	Shades	Mix
MediumVioletRed 	#C71585	 	Shades	Mix
IndianRed  	#CD5C5C	 	Shades	Mix
Peru 	#CD853F	 	Shades	Mix
PaleVioletRed 	#D87093	 	Shades	Mix
Thistle 	#D8BFD8	 	Shades	Mix
Orchid 	#DA70D6	 	Shades	Mix
GoldenRod 	#DAA520	 	Shades	Mix
Crimson 	#DC143C	 	Shades	Mix
Gainsboro 	#DCDCDC	 	Shades	Mix
BurlyWood 	#DEB887	 	Shades	Mix
LightCyan 	#E0FFFF	 	Shades	Mix
Lavender 	#E6E6FA	 	Shades	Mix
DarkSalmon 	#E9967A	 	Shades	Mix
PaleGoldenRod 	#EEE8AA	 	Shades	Mix
LightCoral 	#F08080	 	Shades	Mix
Khaki 	#F0E68C	 	Shades	Mix
AliceBlue 	#F0F8FF	 	Shades	Mix
HoneyDew 	#F0FFF0	 	Shades	Mix
SandyBrown 	#F4A460	 	Shades	Mix
Wheat 	#F5DEB3	 	Shades	Mix
Beige 	#F5F5DC	 	Shades	Mix
WhiteSmoke 	#F5F5F5	 	Shades	Mix
MintCream 	#F5FFFA	 	Shades	Mix
GhostWhite 	#F8F8FF	 	Shades	Mix
Salmon 	#FA8072	 	Shades	Mix
AntiqueWhite 	#FAEBD7	 	Shades	Mix
Linen 	#FAF0E6	 	Shades	Mix
LightGoldenRodYellow 	#FAFAD2	 	Shades	Mix
OldLace 	#FDF5E6	 	Shades	Mix
Fuchsia 	#FF00FF	 	Shades	Mix
DeepPink 	#FF1493	 	Shades	Mix
OrangeRed 	#FF4500	 	Shades	Mix
Tomato 	#FF6347	 	Shades	Mix
HotPink 	#FF69B4	 	Shades	Mix
Coral 	#FF7F50	 	Shades	Mix
Darkorange 	#FF8C00	 	Shades	Mix
LightSalmon 	#FFA07A	 	Shades	Mix
PaleTurquoise 	#AFEEEE	 	Shades	Mix
LightSteelBlue 	#B0C4DE	 	Shades	Mix
PowderBlue 	#B0E0E6	 	Shades	Mix
FireBrick 	#B22222	 	Shades	Mix
DarkGoldenRod 	#B8860B	 	Shades	Mix
MediumOrchid 	#BA55D3	 	Shades	Mix
RosyBrown 	#BC8F8F	 	Shades	Mix
DarkKhaki 	#BDB76B	 	Shades	Mix
PeachPuff 	#FFDAB9	 	Shades	Mix
NavajoWhite 	#FFDEAD	 	Shades	Mix
Moccasin 	#FFE4B5	 	Shades	Mix
Bisque 	#FFE4C4	 	Shades	Mix
MistyRose 	#FFE4E1	 	Shades	Mix
BlanchedAlmond 	#FFEBCD	 	Shades	Mix
PapayaWhip 	#FFEFD5	 	Shades	Mix
LavenderBlush 	#FFF0F5	 	Shades	Mix
SeaShell 	#FFF5EE	 	Shades	Mix
Cornsilk 	#FFF8DC	 	Shades	Mix
LemonChiffon 	#FFFACD	 	Shades	Mix
FloralWhite 	#FFFAF0	 	Shades	Mix
             
             */
        }
    }
}
