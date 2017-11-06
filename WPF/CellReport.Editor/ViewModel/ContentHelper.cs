using CellReport.Core.Contents;

namespace CellReport.Editor.ViewModel
{
	internal static class ContentHelper
	{
		public static void AddContent(BaseContent content)
		{
			var cell = EditorModel.State.Manager.GetCell(EditorModel.State.CurrentCell) ?? EditorModel.State.Manager.CreateCell(EditorModel.State.CurrentCell);

			if (cell.Content is CollectionContent collection1)
			{
				collection1.Add(content);
			}
			else if (cell.Content != null)
			{
				var collection = new CollectionContent(EditorModel.State.Manager, 2);
				collection.Add(content);
				collection.Add(cell.Content);
				cell.Content = collection;
			}
			else
				cell.Content = content;
		}
	}
}
