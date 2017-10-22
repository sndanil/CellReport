using System.Collections.ObjectModel;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;

namespace CellReport.Editor.ViewModel
{
	public class EditorModel
	{
		public static TabControl MainTabControl;
		public static TabItem CodeTab;
		public static TabItem DesignerTab;

		public static Panel ReportContainer;
		public static TextEditor Editor;
		public static Core.CellManager ReportManager;

		public static EditorState State { get; set; }
		public static ObservableCollection<SchemaError> CodeErrors = new ObservableCollection<SchemaError>();

		public static bool IsCodeMode => MainTabControl?.SelectedItem == CodeTab;

		public static bool IsDesignMode => MainTabControl?.SelectedItem == DesignerTab;
	}
}
