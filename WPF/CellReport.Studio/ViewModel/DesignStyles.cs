using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using CellReport.Core.Contents;
using System.Windows.Media;

namespace CellReport.Studio.ViewModel
{
    public class DesignTextStyle: CellReport.Core.NotifyPropertyChanged
    {
        public DesignTextStyle()
        {
            ID = "NewStyle";
            Color = "Black";
            HAlignment = HorizontalAlignment.Left;
            VAlignment = VerticalAlignment.Middle;
            Font = new DesignFont();
            Angle = 0;
        }

        public DesignTextStyle(TextContentStyle style)
        {
            ID = style.ID;
            Color = style.Color;
            HAlignment = style.HAlignment;
            VAlignment = style.VAlignment;
            Font = new DesignFont(style.Font);
            Angle = style.Angle;
        }

        public string ID { get { return GetParam<string>("ID"); } set { SetParam("ID", value); } }
        public string Color { get { return GetParam<string>("Color"); } set { SetParam("Color", value); } }
        public HorizontalAlignment HAlignment 
        { 
            get { return GetParam<HorizontalAlignment>("HAlignment"); } 
            set 
            { 
                SetParam("HAlignment", value);
                OnPropertyChanged("Left");
                OnPropertyChanged("Center");
                OnPropertyChanged("Right");
            } 
        }
        public VerticalAlignment VAlignment 
        { 
            get { return GetParam<VerticalAlignment>("VAlignment"); } 
            set 
            { 
                SetParam("VAlignment", value);
                OnPropertyChanged("Top");
                OnPropertyChanged("Middle");
                OnPropertyChanged("Bottom");
            } 
        }
        public DesignFont Font { get { return GetParam<DesignFont>("Font"); } set { SetParam("Font", value); } }
        public float Angle { get { return GetParam<float>("Angle"); } set { SetParam("Angle", value); } }

        public bool Top 
        {
            get { return VAlignment == VerticalAlignment.Top; }
            set { VAlignment = VerticalAlignment.Top; }
        }

        public bool Middle
        {
            get { return VAlignment == VerticalAlignment.Middle; }
            set { VAlignment = VerticalAlignment.Middle; }
        }

        public bool Bottom
        {
            get { return VAlignment == VerticalAlignment.Bottom; }
            set { VAlignment = VerticalAlignment.Bottom; }
        }

        public bool Left
        {
            get { return HAlignment == HorizontalAlignment.Left; }
            set { HAlignment = HorizontalAlignment.Left; }
        }

        public bool Center
        {
            get { return HAlignment == HorizontalAlignment.Center; }
            set { HAlignment = HorizontalAlignment.Center; }
        }

        public bool Right
        {
            get { return HAlignment == HorizontalAlignment.Right; }
            set { HAlignment = HorizontalAlignment.Right; }
        }

        public CellReport.Core.Contents.TextContentStyle ToTextStyle()
        {
            return new Core.Contents.TextContentStyle(HAlignment, VAlignment, Font.ToFont(), Color, Angle, Direction.LeftToRight) { ID = ID };
        }
    }

    public class DesignFont: CellReport.Core.NotifyPropertyChanged
    {
        public DesignFont()
        {
            Family = "Arial";
            Size = 10;
            Style = Core.FontStyle.Regular;
        }

        public DesignFont(CellReport.Core.FontInfo font)
        {
            Family = font.Family;
            Size = font.Size;
            Style = font.Style;
        }

        public string Family { get { return GetParam<string>("Family"); } set { SetParam("Family", value); OnPropertyChanged("FontFamily"); } }
        public FontFamily FontFamily { get { return new FontFamily(Family); } set { Family = value.ToString(); } }
        public float Size { get { return GetParam<float>("Size"); } set { SetParam("Size", value); } }
        
        public Core.FontStyle Style 
        { 
            get { return GetParam<Core.FontStyle>("Style"); } 
            set 
            { 
                SetParam("Style", value);
                OnPropertyChanged("Bold");
                OnPropertyChanged("Italic");
                OnPropertyChanged("Strikeout");
                OnPropertyChanged("Underline");
            } 
        }

        public bool Bold { get { return Style.HasFlag(CellReport.Core.FontStyle.Bold); } set { Style = value ? (Style | Core.FontStyle.Bold) : (Style ^ Core.FontStyle.Bold); } }
        public bool Italic { get { return Style.HasFlag(CellReport.Core.FontStyle.Italic); } set { Style = value ? (Style | Core.FontStyle.Italic) : (Style ^ Core.FontStyle.Italic); } }
        public bool Strikeout { get { return Style.HasFlag(CellReport.Core.FontStyle.Strikeout); } set { Style = value ? (Style | Core.FontStyle.Strikeout) : (Style ^ Core.FontStyle.Strikeout); } }
        public bool Underline { get { return Style.HasFlag(CellReport.Core.FontStyle.Underline); } set { Style = value ? (Style | Core.FontStyle.Underline) : (Style ^ Core.FontStyle.Underline); } }

        public CellReport.Core.FontInfo ToFont()
        {
            return new Core.FontInfo(Family, Size, Style);
        }
    }

    public class DesignDecimalStyle: DesignTextStyle
    {
        public DesignDecimalStyle(): base()
        {
            HAlignment = HorizontalAlignment.Right;
            NegativeColor = "Red";
            Format = "#,##0.00;-#,##0.00;-";
        }

        public DesignDecimalStyle(DecimalContentStyle style)
            : base(style)
        {
            NegativeColor = style.NegativeColor;
            Format = style.Format;
        }

        public string NegativeColor { get { return GetParam<string>("NegativeColor"); } set { SetParam("NegativeColor", value); } }
        public string Format { get { return GetParam<string>("Format"); } set { SetParam("Format", value); } }

        public CellReport.Core.Contents.DecimalContentStyle ToDecimalStyle()
        {
            return new Core.Contents.DecimalContentStyle(HAlignment, VAlignment, Font.ToFont(), Color, NegativeColor, Angle, Format, "ru-RU", Direction.LeftToRight) { ID = ID };
        }
    }

    public class DesignDateTimelStyle : DesignTextStyle
    {
        public DesignDateTimelStyle()
            : base()
        {
            Format = "dd.MM.yyyy HH:mm";
        }

        public DesignDateTimelStyle(DateTimeContentStyle style)
            : base(style)
        {
            Format = style.Format;
        }

        public string Format { get { return GetParam<string>("Format"); } set { SetParam("Format", value); } }

        public CellReport.Core.Contents.DateTimeContentStyle ToDateTimeStyle()
        {
            return new Core.Contents.DateTimeContentStyle(HAlignment, VAlignment, Font.ToFont(), Color, Angle, Format, "ru-RU", Direction.LeftToRight) { ID = ID };
        }
    }
}
