using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
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

			Export.BaseExporter.RendererFactory = new Presenters.WinForms.RendererFactory();
			EditorModel.State = new EditorState();

			XmlCodeCompleteHelper.ApplyTo(Editor);
			EditorModel.MainTabControl = MainTab;
			EditorModel.CodeTab = CodeTab;
			EditorModel.DesignerTab = DesignerTab;
			EditorModel.ReportContainer = ReportContainer;
			EditorModel.Editor = Editor;

			DataContext = EditorModel.State;
			EditorModel.State.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == "FileName")
					Title = $"Редактор отчетов {EditorModel.State.FileName}";
			};

			FileResolver.OnResolve += fileName =>
			{
				if (EditorModel.State.FileName != null && File.Exists(EditorModel.State.FileName))
				{
					var directory = Path.GetDirectoryName(EditorModel.State.FileName);
					if (directory != null)
					{
						var path = Path.Combine(directory, fileName);
						if (File.Exists(path))
							return File.ReadAllBytes(path);
					}
				}

				return null;
			};

			SchemaErrors.ItemsSource = EditorModel.CodeErrors;

			Editor.TextArea.KeyUp += (sender, args) =>
			{
				if(args.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && EditorCommands.Find.CanExecute())
					EditorCommands.Find.Execute();
				if (args.Key == Key.H && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && EditorCommands.FindAndReplace.CanExecute())
					EditorCommands.FindAndReplace.Execute();
			};

			Loaded += (sender, args) =>
			{
				Application.EnableVisualStyles();

				if (Environment.GetCommandLineArgs().Length > 0)
				{
					try
					{
						var fileName = Environment.GetCommandLineArgs()[1];
						CellTemplate template = new CellTemplate();
						template.Load(fileName);
						EditorModel.State.FileName = fileName;

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
			if (e.AddedItems.Count > 0 && ReferenceEquals(e.AddedItems[0], DesignerTab))
				EditorCommands.SwitchEditorToDesignMode();

			if (e.AddedItems.Count > 0 && ReferenceEquals(e.AddedItems[0], CodeTab))
				EditorCommands.SwitchEditorToCodeMode();
		}

	}
}
