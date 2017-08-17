using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using CellReport.Core;
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
		public static Presenters.Wpf.DisplayPresenter Presenter;
		public static string FileName;

		public static ObservableCollection<SchemaError> CodeErrors = new ObservableCollection<SchemaError>();

		public static bool IsCodeMode => MainTabControl?.SelectedItem == CodeTab;

		public static bool IsDesignMode => MainTabControl?.SelectedItem == DesignerTab;

		public IList<CellStyle> CellStyles
		{
			get
			{
				if (Presenter == null)
					return new List<CellStyle>();
				var result = ReportManager.GetStyles().Where(s => s.GetType() == typeof(CellStyle)).Select(s => (CellStyle)s).ToList();
				result.Insert(0, Cell.DefaultStyle);
				return result;
			}
		}
	}
}
