using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace CellReport.Studio.ViewModel
{
    public class StyleTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (item.GetType() == typeof(CellReport.Core.Contents.TextContentStyle))
                return element.FindResource("textStyleTemplate") as DataTemplate;
            else if (item.GetType() == typeof(CellReport.Core.Contents.DecimalContentStyle))
                return element.FindResource("decimalStyleTemplate") as DataTemplate;
            else if (item.GetType() == typeof(CellReport.Core.Contents.DateTimeContentStyle))
                return element.FindResource("dateTimeStyleTemplate") as DataTemplate;
            else
                return element.FindResource("allStyleTemplate") as DataTemplate;
        }
    }
}
