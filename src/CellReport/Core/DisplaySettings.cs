using System;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core
{
    [Serializable]
    public class DisplaySettings : NotifyPropertyChanged
    {
        private const string DefaultGridColor = "#D0D7E5";

        private float _scale = 1f;
        public float Scale 
        {
            get { return _scale; }
            set 
            {
                if (value > 0 && Math.Abs(_scale - value) > float.Epsilon)
                {
                    _scale = value;
                    OnPropertyChanged("Scale");
                }
            } 
        }

        private int _frozenColumns;
        public int FrozenColumns 
        {
            get { return _frozenColumns; }
            set
            {
                if (_frozenColumns != value)
                {
                    _frozenColumns = value;
                    OnPropertyChanged("FrozenColumns");
                }
            }
        }

        private int _frozenRows;
        public int FrozenRows
        {
            get { return _frozenRows; }
            set
            {
                if (_frozenRows != value)
                {
                    _frozenRows = value;
                    OnPropertyChanged("FrozenRows");
                }
            }
        }

        private bool _showGridLines;
        public bool ShowGridLines
        {
            get { return _showGridLines; }
            set
            {
                if (_showGridLines != value)
                {
                    _showGridLines = value;
                    OnPropertyChanged("ShowGridLines");
                }
            }
        }

        private bool _showDesignerGridLines = true;
        public bool ShowDesignerGridLines
        {
            get { return _showDesignerGridLines; }
            set
            {
                if (_showDesignerGridLines != value)
                {
                    _showDesignerGridLines = value;
                    OnPropertyChanged("ShowDesignerGridLines");
                }
            }
        }

        private bool _showBookMarks;
        public bool ShowBookMarks
        {
            get { return _showBookMarks; }
            set
            {
                if (_showBookMarks != value)
                {
                    _showBookMarks = value;
                    OnPropertyChanged("ShowBookMarks");
                }
            }
        }

        private bool _showResizers;
        public bool ShowResizers
        {
            get { return _showResizers; }
            set
            {
                if (_showResizers != value)
                {
                    _showResizers = value;
                    OnPropertyChanged("ShowResizers");
                }
            }
        }

        private bool _showDesignerResizers = true;
        public bool ShowDesignerResizers
        {
            get { return _showDesignerResizers; }
            set
            {
                if (_showDesignerResizers != value)
                {
                    _showDesignerResizers = value;
                    OnPropertyChanged("ShowDesignerResizers");
                }
            }
        }

        private string _gridColor = DefaultGridColor;
        public string GridColor 
        {
            get { return _gridColor; }
            set
            {
                if (_gridColor != value)
                {
                    _gridColor = value;
                    OnPropertyChanged("GridColor");
                }
            }
        }

        private bool _showCurrentRows = true;
        public bool ShowCurrentRows
        {
            get { return _showCurrentRows; }
            set
            {
                if (_showCurrentRows != value)
                {
                    _showCurrentRows = value;
                    OnPropertyChanged("ShowCurrentRows");
                }
            }
        }

        internal static DisplaySettings Parse(XElement element)
        {
            var result = new DisplaySettings();

            if (element != null)
            {
                if (element.Attribute("Scale") != null)
                    result.Scale = float.Parse(element.Attribute("Scale").Value, CultureInfo.InvariantCulture);
                if (element.Attribute("FrozenColumns") != null)
                    result.FrozenColumns = int.Parse(element.Attribute("FrozenColumns").Value, CultureInfo.InvariantCulture);
                if (element.Attribute("FrozenRows") != null)
                    result.FrozenRows = int.Parse(element.Attribute("FrozenRows").Value, CultureInfo.InvariantCulture);
                if (element.Attribute("ShowGridLines") != null)
                    result.ShowGridLines = bool.Parse(element.Attribute("ShowGridLines").Value);
                if (element.Attribute("ShowDesignerGridLines") != null)
                    result.ShowDesignerGridLines = bool.Parse(element.Attribute("ShowDesignerGridLines").Value);
                if (element.Attribute("ShowBookMarks") != null)
                    result.ShowBookMarks = bool.Parse(element.Attribute("ShowBookMarks").Value);
                if (element.Attribute("ShowResizers") != null)
                    result.ShowResizers = bool.Parse(element.Attribute("ShowResizers").Value);
                if (element.Attribute("ShowDesignerResizers") != null)
                    result.ShowDesignerResizers = bool.Parse(element.Attribute("ShowDesignerResizers").Value);
                if (element.Attribute("GridColor") != null)
                    result.GridColor = element.Attribute("GridColor").Value;
                if (element.Attribute("ShowCurrentRows") != null)
                    result.ShowCurrentRows = bool.Parse(element.Attribute("ShowCurrentRows").Value);
            }

            return result;
        }

        internal XElement Serialize()
        {
            return new XElement("DisplaySettings",
                new XAttribute("FrozenColumns", FrozenColumns),
                new XAttribute("FrozenRows", FrozenRows),
                new XAttribute("ShowGridLines", ShowGridLines),
                new XAttribute("ShowDesignerGridLines", ShowDesignerGridLines),
                new XAttribute("ShowCurrentRows", ShowCurrentRows),
                new XAttribute("ShowBookMarks", ShowBookMarks),
                new XAttribute("ShowResizers", ShowResizers),
                new XAttribute("ShowDesignerResizers", ShowDesignerResizers),
                new XAttribute("Scale", Scale.ToString(CultureInfo.InvariantCulture)),
                GridColor != DefaultGridColor ? new XAttribute("GidColor", GridColor) : null
                );
        }
    }
}
