using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CellReport.Studio.PropertiesWindows
{
    /// <summary>
    /// Interaction logic for FontControl.xaml
    /// </summary>
    public partial class FontControl : UserControl
    {
        private static List<FontFamily> _famylies = null;

        public FontControl()
        {
            InitializeComponent();

            if (_famylies == null)
                _famylies = Fonts.SystemFontFamilies.OrderBy(f => f.ToString()).ToList();

            fontComboBox.ItemsSource = _famylies;
        }
    }
}
