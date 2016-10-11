using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

using CellReport.Core;
using CellReport.Core.Expressions;
using CellReport.Studio.ViewModel;

namespace CellReport.Studio.PropertiesWindows
{
	/// <summary>
	/// Interaction logic for PagerProperties.xaml
	/// </summary>
	public partial class PagerProperties : Window
	{
		public List<DesignPager> Templates { get; private set; }
		public List<BaseExpression> Expressions { get; private set; }
		public ObservableCollection<DesignPager> Pagers { get; private set; }

		public PagerProperties(IList<Pager> pagers)
		{
            var localPath = System.IO.Path.Combine(
                                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                                    "DefaultPagers.xml"); 
            var doc = XDocument.Load(localPath);
			var templates = new List<Pager>();
			Pager.ParsePagers(StudioCommands.Manager, doc.Root.Element("Templates"), templates);
			Templates = templates.Select(x => new DesignPager(x)).ToList();
			Expressions = doc.Root.Element("Expressions").Elements().Select(x => BaseExpression.Parse(x)).ToList();

			Pagers = new ObservableCollection<DesignPager>(pagers.Select(x => new DesignPager(x)));

			InitializeComponent();
		}

		private void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Add_Click(object sender, RoutedEventArgs e)
		{
			var template = lstTemplates.SelectedItem as DesignPager;
			if (template == null)
				return;
			Pagers.Add(template.Clone());
			lstPagers.SelectedIndex = Pagers.Count - 1;
		}

		private void Delete_Click(object sender, RoutedEventArgs e)
		{
			var pager = lstPagers.SelectedItem as DesignPager;
			if (pager == null)
				return;
			Pagers.Remove(pager);
			if (Pagers.Count > 0)
				lstPagers.SelectedIndex = 0;
		}

		private void Up_Click(object sender, RoutedEventArgs e)
		{
			MovePage(-1);
		}

		private void Down_Click(object sender, RoutedEventArgs e)
		{
			MovePage(1);
		}

		private void MovePage(int offset)
		{
			var index = lstPagers.SelectedIndex;
			if (index < 0)
				return;
			var newIndex = index + offset;
			if ((newIndex < 0) || (newIndex >= Pagers.Count))
				return;

			var pager = Pagers[index];
			Pagers.RemoveAt(index);
			Pagers.Insert(newIndex, pager);
			lstPagers.SelectedIndex = newIndex;
		}

		private void lstPagers_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_viewer.Invalidate();
		}

		private void Viewer_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			var pager = lstPagers.SelectedItem as DesignPager;
			if (pager != null)
				StudioCommands.Presenter.RenderPager(sender, e, pager.ToPager());
		}
	}
}
