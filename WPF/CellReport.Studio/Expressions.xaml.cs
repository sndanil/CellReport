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
using CellReport.Studio.ViewModel;
using CellReport.Core.Expressions;

namespace CellReport.Studio
{
    /// <summary>
    /// Interaction logic for Expressions.xaml
    /// </summary>
    public partial class Expressions : Window
    {
        public Expressions()
        {
            InitializeComponent();

            _expressions.DataContext = Studio.ViewModel.StudioCommands.Values;
        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            _menu.IsOpen = true;
        }

        private void AddCount(object sender, RoutedEventArgs e)
        {
            StudioCommands.Manager.AddExpression(new CountExpression(1, 1) { ID = "Новое выражение " + StudioCommands.Values.Expressions.Count });
            StudioCommands.Values.Expressions = null;
        }

        private void AddNow(object sender, RoutedEventArgs e)
        {
            StudioCommands.Manager.AddExpression(new NowExpression() { ID = "Новое выражение " + StudioCommands.Values.Expressions.Count });
            StudioCommands.Values.Expressions = null;
        }

        private void AddDocName(object sender, RoutedEventArgs e)
        {
            StudioCommands.Manager.AddExpression(new DocumentNameExpression() { ID = "Новое выражение " + StudioCommands.Values.Expressions.Count });
            StudioCommands.Values.Expressions = null;
        }

        private void AddPagesCount(object sender, RoutedEventArgs e)
        {
            StudioCommands.Manager.AddExpression(new PagesCountExpression() { ID = "Новое выражение " + StudioCommands.Values.Expressions.Count });
            StudioCommands.Values.Expressions = null;
        }

        private void AddSum(object sender, RoutedEventArgs e)
        {
            StudioCommands.Manager.AddExpression(new SumExpression() { ID = "Новое выражение " + StudioCommands.Values.Expressions.Count });
            StudioCommands.Values.Expressions = null;
        }

        private void DelButtonClick(object sender, RoutedEventArgs e)
        {
            if (_exprList.SelectedItem == null)
                return;

            StudioCommands.Manager.RemoveExpression((BaseExpression)_exprList.SelectedItem);
            StudioCommands.Values.Expressions = null;
        }
    }
}
