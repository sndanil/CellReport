using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using CellReport.Core;
using CellReport.Editor.CodeEditor;
using CellReport.Editor.ViewModel;

namespace CellReport.Editor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			Core.CellManager.IsDesignMode = true;
			Export.BaseExporter.RendererFactory = new Presenters.WinForms.RendererFactory();

			XmlCodeCompleteHelper.ApplyTo(Editor);
			EditorModel.MainTabControl = MainTab;
			EditorModel.CodeTab = CodeTab;
			EditorModel.DesignerTab = DesignerTab;
			EditorModel.ReportContainer = ReportContainer;
			EditorModel.Editor = Editor;

			Loaded += (sender, args) =>
			{
				System.Windows.Forms.Application.EnableVisualStyles();

				if (Environment.GetCommandLineArgs().Length > 0)
				{
					try
					{
						var fileName = Environment.GetCommandLineArgs()[1];
						CellTemplate template = new CellTemplate();
						template.Load(fileName);
						EditorModel.FileName = fileName;

						EditorCommands.ApplyReport(template.Manager);
					}
					catch (Exception e)
					{
					}
				}
			};
		}

		private void TabsSwitched(object sender, SelectionChangedEventArgs e)
		{
			if (ReferenceEquals(e.AddedItems[0], DesignerTab))
				EditorCommands.SwitchEditorToDesignMode();

			if (ReferenceEquals(e.AddedItems[0], CodeTab))
				EditorCommands.SwitchEditorToCodeMode();
		}

	}
}
