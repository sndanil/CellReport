using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

using CellReport.Studio.ViewModel;

namespace CellReport.Studio.PropertiesWindows
{
	/// <summary>
	/// Interaction logic for ResourcesEditor.xaml
	/// </summary>
	public partial class ResourcesEditor : Window
	{
		public ObservableCollection<DesignResource> DesignResources { get; private set; }

		public ResourcesEditor()
		{
			DesignResources = new ObservableCollection<DesignResource>(
				StudioCommands.Manager.GetResourceNames().OrderBy(x => x)
					.Select(x => new DesignResource(x, StudioCommands.Manager.GetResource(x))));

			InitializeComponent();
		}

		private void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Add_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog();
			dialog.Filter = "Image files (*.png; *.jpg)|*.png;*.jpg|All files (*.*)|*.*";
			if (dialog.ShowDialog() != true)
				return;

			var res = DesignResource.FromFile(dialog.FileName, DesignResources);
			if (res == null)
				return;
			
			DesignResources.Add(res);
			lstResources.SelectedIndex = DesignResources.Count - 1;
		}

		private void Rename_Click(object sender, RoutedEventArgs e)
		{
			RenameResource();
		}

		private void Delete_Click(object sender, RoutedEventArgs e)
		{
			var res = lstResources.SelectedItem as DesignResource;
			if (res == null)
				return;
			DesignResources.Remove(res);
			if (DesignResources.Count > 0)
				lstResources.SelectedIndex = 0;
		}

		private void txtName_LostFocus(object sender, RoutedEventArgs e)
		{
			var res = lstResources.SelectedItem as DesignResource;
			if (res == null)
				return;

			if ((res.NewName != res.Name) && !string.IsNullOrEmpty(res.NewName) &&
				DesignResources.All(x => x.Name != res.NewName))
					res.Name = res.NewName;
			res.IsEditMode = false;
		}

		private void ListBoxItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
				this.Dispatcher.BeginInvoke((Action)RenameResource);
		}

		private void RenameResource()
		{
			var res = lstResources.SelectedItem as DesignResource;
			if (res == null)
				return;
			res.NewName = res.Name;
			res.IsEditMode = true;
		}
	}
}
