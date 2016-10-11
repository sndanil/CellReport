using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using CellReport.Core;

namespace CellReport.Studio.ViewModel
{
    public class DesignCellStyle: NotifyPropertyChanged
    {
        public DesignCellStyle()
        {
            ID = "NewStyle";
            Color = "Transparent";
            LeftBorder = new DesignBorder();
            TopBorder = new DesignBorder();
            RightBorder = new DesignBorder();
            BottomBorder = new DesignBorder();
        }

		public DesignCellStyle(CellStyle style)
		{
			ID = style.ID;
			Color = style.Color;
			LeftBorder = new DesignBorder(style.Borders.Left, style.Paddings.Left);
			TopBorder = new DesignBorder(style.Borders.Top, style.Paddings.Top);
			RightBorder = new DesignBorder(style.Borders.Right, style.Paddings.Right);
			BottomBorder = new DesignBorder(style.Borders.Bottom, style.Paddings.Bottom);
		}

        public string ID { get { return GetParam<string>("ID"); } set { SetParam("ID", value); } }
        public string Color { get { return GetParam<string>("Color"); } set { SetParam("Color", value); } }
        public DesignBorder TopBorder { get { return GetParam<DesignBorder>("TopBorder"); } set { SetParam("TopBorder", value); } }
        public DesignBorder LeftBorder { get { return GetParam<DesignBorder>("LeftBorder"); } set { SetParam("LeftBorder", value); } }
        public DesignBorder RightBorder { get { return GetParam<DesignBorder>("RightBorder"); } set { SetParam("RightBorder", value); } }
        public DesignBorder BottomBorder { get { return GetParam<DesignBorder>("BottomBorder"); } set { SetParam("BottomBorder", value); } }

		public CellStyle ToStyle()
		{
			return new CellStyle(Color,
				new Borders(TopBorder.ToBorder(), LeftBorder.ToBorder(), RightBorder.ToBorder(), BottomBorder.ToBorder()),
				new Paddings(LeftBorder.Padding, TopBorder.Padding, RightBorder.Padding, BottomBorder.Padding)) { ID = ID };
		}
    }

    public class DesignBorder: NotifyPropertyChanged
    {
        public DesignBorder()
        {
            Color = "Black";
			DesignStyle = DesignLineStyle.Styles[0];
			Padding = 0;
        }

		public DesignBorder(Border border, float padding)
		{
			Color = border.Color;
			DesignStyle = DesignLineStyle.Styles.FirstOrDefault(x => (x.Weight == border.Weight) && (x.Style == border.Style))
				?? DesignLineStyle.Styles[0];
			Padding = padding;
		}

        public string Color { get { return GetParam<string>("Color"); } set { SetParam("Color", value); } }

		public DesignLineStyle DesignStyle { get { return GetParam<DesignLineStyle>("DesignStyle"); } set { SetParam("DesignStyle", value); } }

		public float Padding { get { return GetParam<float>("Padding"); } set { SetParam("Padding", value); } }

        public Border ToBorder()
        {
			return new Core.Border(DesignStyle.Weight, Color, DesignStyle.Style);
        }
    }

	public class DesignLineStyle
	{
		public float Weight { get; set; }

		public float WeightWPF
		{
			get
			{
				return UnitsConverter.ConvertValue(Weight, GraphicsUnit.Millimeter, GraphicsUnit.WpfPoint, 0);
			}
			set
			{
				Weight = UnitsConverter.ConvertValue(value, GraphicsUnit.WpfPoint, GraphicsUnit.Millimeter, 0);
			}
		}

		public LineStyle Style { get; set; }

		public DesignLineStyle(float weightWPF, LineStyle style)
		{
			WeightWPF = weightWPF;
			Style = style;
		}

		private static List<DesignLineStyle> _styles = null;

		public static List<DesignLineStyle> Styles
		{
			get
			{
				if (_styles != null)
					return _styles;

				_styles = new List<DesignLineStyle>();
				_styles.Add(new DesignLineStyle(0, LineStyle.None));

				var weights = new float[] { 1, 2, 3 };
				var styles = Enum.GetValues(typeof(LineStyle)).Cast<LineStyle>()
					.Where(x => x != LineStyle.None).OrderBy(x => (int)x).ToList();
				foreach (var weight in weights)
					foreach (var style in styles)
						_styles.Add(new DesignLineStyle(weight, style));
				return _styles;
			}
		}
	}
}
