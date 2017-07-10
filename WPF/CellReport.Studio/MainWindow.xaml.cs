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
using Microsoft.Windows.Controls.Ribbon;

namespace CellReport.Studio
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : RibbonWindow, CellReport.Studio.ViewModel.IViewUpdater
	{
		public MainWindow()
		{
			CellReport.Core.CellManager.IsDesignMode = true;
			InitializeComponent();
			Studio.ViewModel.StudioCommands.Root = _root;
			Studio.ViewModel.StudioCommands.Updater = this;
			CellReport.Export.BaseExporter.RendererFactory = new CellReport.Presenters.WinForms.RendererFactory();
		}

		public void Update(bool isNew)
		{
			_view.DataContext = Studio.ViewModel.StudioCommands.DisplaySettings;
			_print.DataContext = Studio.ViewModel.StudioCommands.PrintSettings;
			_margins.DataContext = Studio.ViewModel.StudioCommands.PrintSettings.Margins;
			_advanced.DataContext = Studio.ViewModel.StudioCommands.ExportSettings;
			_properties.DataContext = Studio.ViewModel.StudioCommands.Properties;
			_defaultProperties.DataContext = Studio.ViewModel.StudioCommands.Properties;

			_marker.DataContext = Studio.ViewModel.StudioCommands.Values;
			_cells.DataContext = Studio.ViewModel.StudioCommands.Values;

			_contents.DataContext = Studio.ViewModel.StudioCommands.Values;
			_sections.DataContext = Studio.ViewModel.StudioCommands.Values;
			_currentCell.DataContext = Studio.ViewModel.StudioCommands.Values;
			_styles.DataContext = Studio.ViewModel.StudioCommands.Values;
			_scale.DataContext = Studio.ViewModel.StudioCommands.Values;

			if (isNew)
				StateManager.Init();
		}
	}
}
