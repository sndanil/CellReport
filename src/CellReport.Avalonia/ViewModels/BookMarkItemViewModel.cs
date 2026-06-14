using CellReport.Core;
using CellReport.Core.BookMarks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace CellReport.Avalonia.ViewModels
{
	public partial class BookMarkItemViewModel : ObservableObject
	{
		public BookMarkItemViewModel(BookMark bookMark)
		{
			BookMark = bookMark;
			Header = bookMark.GetText() ?? string.Empty;
			foreach (var child in bookMark.Child)
				Children.Add(new BookMarkItemViewModel(child));
		}

		public BookMark BookMark { get; }

		public string Header { get; }

		public ObservableCollection<BookMarkItemViewModel> Children { get; } = new();

		public CellLocation Location => BookMark.Location;
	}
}
