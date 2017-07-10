using System.Windows.Controls;
using ICSharpCode.AvalonEdit;

namespace CellReport.Editor.ViewModel
{
	public class EditorModel
	{
		public static Panel ReportContainer;
		public static TextEditor Editor;
		public static Core.CellManager ReportManager;
		public static Presenters.Wpf.DisplayPresenter Presenter;
		public static string FileName;
	}
}
