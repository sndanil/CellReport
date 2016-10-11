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
using System.Windows.Shapes;

namespace CellReport.Studio.PropertiesWindows
{
    /// <summary>
    /// Interaction logic for DecimalStyle.xaml
    /// </summary>
    public partial class DecimalStyle : Window
    {
        public DecimalStyle()
        {
            InitializeComponent();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
