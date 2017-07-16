using System.Windows.Controls;
using ICSharpCode.AvalonEdit;

namespace CellReport.Editor.ViewModel
{
	public class EditorModel
	{
		public static TabControl MainTabControl;
		public static TabItem CodeTab;

		public static Panel ReportContainer;
		public static TextEditor Editor;
		public static Core.CellManager ReportManager;
		public static Presenters.Wpf.DisplayPresenter Presenter;
		public static string FileName;

		public static bool IsCodeMode => MainTabControl.SelectedItem == CodeTab;
	}
}
