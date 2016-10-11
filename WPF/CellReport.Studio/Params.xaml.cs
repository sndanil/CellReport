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

namespace CellReport.Studio
{
    /// <summary>
    /// Interaction logic for Params.xaml
    /// </summary>
    public partial class Params : Window
    {
        private CellReport.Core.Contents.BaseContent _content = null;
        private CellReport.Core.CellManager _manager = null;

        public Params(CellReport.Core.Contents.BaseContent content, CellReport.Core.CellManager manager)
        {
            _content = content;
            _manager = manager;
            InitializeComponent();

            _params.ItemsSource = content.GetParams();
            _expressions.ItemsSource = Studio.ViewModel.StudioCommands.Values.Expressions;

            _params.SelectedItem = content.GetParams().FirstOrDefault();
        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            var param = new Core.Contents.Param(_manager, _content);
            _content.AddParam(param);
            _params.ItemsSource = _content.GetParams();
            _params.SelectedItem = param;
            _name.Focus();
        }

        private void DelButtonClick(object sender, RoutedEventArgs e)
        {
            if (_params.SelectedItem == null)
                return;

            var index = _content.GetParams().ToList().IndexOf((Core.Contents.Param)_params.SelectedItem);
            index = Math.Min(index, _content.GetParams().Count() - 2);
            _content.RemoveParam((Core.Contents.Param)_params.SelectedItem);
            _params.ItemsSource = _content.GetParams();
            _params.SelectedItem = index >= 0 ? _content.GetParams().ToList()[index] : null;
        }
    }
}
