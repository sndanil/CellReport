using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using System.Collections;

namespace CellReport.Core.Contents
{
    [Serializable]
    public class ChartContentStyle : BaseStyle
    {
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            var other = obj as ChartContentStyle;
            if (other != null)
                return base.Equals(obj);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal new static ChartContentStyle Parse(XElement element)
        {
            return new ChartContentStyle();
        }

        protected override XElement OnSerialize(string styleName)
        {
            var result = base.OnSerialize(styleName ?? "ChartContentStyle");
            return result;
        }
    }

    [Serializable]
    public class ChartContent : BaseContent
    {
        [Serializable]
        public enum SeriesChartType
        {
            Point = 0,
            FastPoint = 1,
            Bubble = 2,
            Line = 3,
            Spline = 4,
            StepLine = 5,
            FastLine = 6,
            Bar = 7,
            StackedBar = 8,
            StackedBar100 = 9,
            Column = 10,
            StackedColumn = 11,
            StackedColumn100 = 12,
            Area = 13,
            SplineArea = 14,
            StackedArea = 15,
            StackedArea100 = 16,
            Pie = 17,
            Doughnut = 18,
            Stock = 19,
            Candlestick = 20,
            Range = 21,
            SplineRange = 22,
            RangeBar = 23,
            RangeColumn = 24,
            Radar = 25,
            Polar = 26,
            ErrorBar = 27,
            BoxPlot = 28,
            Renko = 29,
            ThreeLineBreak = 30,
            Kagi = 31,
            PointAndFigure = 32,
            Funnel = 33,
            Pyramid = 34,
        }

        [Serializable]
        public class Series
        {
            private List<object> _xData = new List<object>();
            private List<object> _yData = new List<object>();

            public string Name { get; set; }
            public SeriesChartType Type { get; set; }
            public Type XDataType { get; set; }
            public Type YDataType { get; set; }

            public Series()
            {
                Type = SeriesChartType.Column;
                XDataType = typeof(string);
                YDataType = typeof(decimal);
            }

            public void AddXData(object data)
            {
                _xData.Add(data);
            }

            public void AddYData(object data)
            {
                _yData.Add(data);
            }

            public object[] GetXData()
            {
                return _xData.ToArray();
            }

            public object[] GetYData()
            {
                return _yData.ToArray();
            }
        }

        public static readonly ChartContentStyle DefaultStyle = new ChartContentStyle();

        private List<Series> _series = new List<Series>();

        public ChartContent(CellManager manager)
            : base(manager)
        {
        }

        public bool Use3D { get; set; }
        public bool ShowLegend { get; set; }
        public object ChartObject { get; set; }

		public override BaseStyle GetStyle()
		{
			return GetStyle<ChartContentStyle>() ?? DefaultStyle;
		}

        public IEnumerable<Series> GetSeries()
        {
            foreach (var series in _series)
                yield return series;
        }

        private object[] GetSeriesDataFromParam(string name)
        {
            var result = this.GetParamValueByTarget(name);
            var inenum = result as IEnumerable;
            if (inenum != null && !(result is string)) //TODO Hack !!!
            {
                var list = new List<object>();
                foreach (var item in inenum)
                    list.Add(item);
                return list.ToArray();
            }

            return null;
        }
        public object[] GetXDataBySeries(Series series)
        {
            var result = GetSeriesDataFromParam("Series." + series.Name + ".XData");
            return result ?? (from s in _series where s == series select s.GetXData()).FirstOrDefault();
        }

        public object[] GetYDataBySeries(Series series)
        {
            var result = GetSeriesDataFromParam("Series." + series.Name + ".YData");
            return result ?? (from s in _series where s == series select s.GetYData()).FirstOrDefault();
        }

        internal new static ChartContent Parse(CellManager manager, XElement element)
        {
            var content = new ChartContent(manager);
            if (element.Attribute("Use3D") != null)
                content.Use3D = bool.Parse(element.Attribute("Use3D").Value);
            if (element.Attribute("ShowLegend") != null)
                content.ShowLegend = bool.Parse(element.Attribute("ShowLegend").Value);
            foreach (var seriesElement in element.Elements("Series"))
            {
                var series = new Series();
                series.Name = seriesElement.Attribute("Name").Value;
                if (seriesElement.Attribute("Type") != null)
                    series.Type = (SeriesChartType)Enum.Parse(typeof(SeriesChartType), seriesElement.Attribute("Type").Value);
                if (seriesElement.Attribute("XDataType") != null)
                    series.XDataType = Type.GetType(seriesElement.Attribute("XDataType").Value);
                if (seriesElement.Attribute("YDataType") != null)
                    series.YDataType = Type.GetType(seriesElement.Attribute("YDataType").Value);
                var oldCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                foreach (var dataElement in seriesElement.Elements("Data"))
                {
                    series.AddXData(Convert.ChangeType(dataElement.Element("X").Value, series.XDataType));
                    series.AddYData(Convert.ChangeType(dataElement.Element("Y").Value, series.YDataType));
                }
                System.Threading.Thread.CurrentThread.CurrentCulture = oldCulture;

                content._series.Add(series);
            }
            return content;
        }

        protected override XElement OnSerialize(string contentName)
        {
            var result = base.OnSerialize(contentName ?? "ChartContent");
            if (Use3D)
                result.Add(new XAttribute("Use3D", Use3D));
            if (ShowLegend)
                result.Add(new XAttribute("ShowLegend", ShowLegend));
            foreach (var series in _series)
            {
                var seriesElement = new XElement("Series", 
                                new XAttribute("Name", series.Name), 
                                new XAttribute("Type", series.Type),
                                new XAttribute("XDataType", series.XDataType),
                                new XAttribute("YDataType", series.YDataType));
                var dataList = new List<XElement>();
                foreach (var item in GetXDataBySeries(series))
                    dataList.Add(new XElement("Data", new XElement("X", item)));
                int index = 0;
                foreach (var item in GetYDataBySeries(series))
                {
                    dataList[index].Add(new XElement("Y", item));
                    index++;
                }
                seriesElement.Add(dataList.ToArray());

                result.Add(seriesElement);
            }

            return result;
        }
    }
}
