using CellReport.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace CellReport.Avalonia.ViewModels
{
	public partial class GoToRowDialogViewModel : ObservableObject
	{
		public GoToRowDialogViewModel(int currentRow, int maxRow)
		{
			SelectedRow = Math.Clamp(currentRow + 1, 1, maxRow);
			MaxRow = maxRow;
		}

		public string Title => ReportLocalization.Get("Report/GoToRow/Title");

		public string RowNumberLabel => ReportLocalization.Get("Report/GoToRow/RowNumber");

		public string OkLabel => ReportLocalization.Get("Report/GoToRow/Ok");

		public string CancelLabel => ReportLocalization.Get("Report/GoToRow/Cancel");

		public int MaxRow { get; }

		[ObservableProperty]
		private int _selectedRow;

		public bool DialogResult { get; private set; }

		[RelayCommand]
		private void Confirm()
		{
			DialogResult = true;
		}

		[RelayCommand]
		private void Cancel()
		{
			DialogResult = false;
		}
	}
}
