using CellReport.Avalonia.Services;
using CellReport.Avalonia.Views;
using CellReport.Core;
using CellReport.Presenters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace CellReport.Avalonia.ViewModels
{
	public partial class PresentControlViewModel : ObservableObject
	{
		private BaseDisplayPresenter? _presenter;
		private Visual? _host;
		private bool _syncing;

		public string[] ScaleOptions { get; } =
		{
			"50%", "75%", "85%", "100%", "125%", "150%", "175%", "200%", "300%", "500%"
		};

		public string ExportExcelLabel => ReportLocalization.Get("Report/ExportToExcel");

		public string ExportHtmlLabel => ReportLocalization.Get("Report/ExportToHtml");

		public string ExportImageLabel => ReportLocalization.Get("Report/ExportToImage");

		public string SaveLabel => ReportLocalization.Get("Report/Save");

		public string FindLabel => ReportLocalization.Get("Report/Search");

		public string GoToRowLabel => ReportLocalization.Get("Report/GoToLine");

		public string BookMarksLabel => ReportLocalization.Get("Report/BookMarks");

		public string ShowGridLabel => ReportLocalization.Get("Report/ShowHideGrid");

		public string ShowResizersLabel => ReportLocalization.Get("Report/ShowHideHeaders");

		public string ScaleLabel => ReportLocalization.Get("Report/Scale");

		public ObservableCollection<BookMarkItemViewModel> BookMarks { get; } = new();

		[ObservableProperty]
		private CellManager? _manager;

		[ObservableProperty]
		private bool _showGridLines;

		[ObservableProperty]
		private bool _showResizers;

		[ObservableProperty]
		private bool _showBookMarks;

		[ObservableProperty]
		private bool _showToolBar = true;

		[ObservableProperty]
		private int _selectedScaleIndex = 3;

		[ObservableProperty]
		private BookMarkItemViewModel? _selectedBookMark;

		public void Load(CellManager manager)
		{
			Manager = manager;
		}

		public void AttachPresenter(BaseDisplayPresenter presenter, Visual host)
		{
			if (_presenter != null)
				_presenter.ShowToolBarChanged -= OnPresenterShowToolBarChanged;

			_presenter = presenter;
			_host = host;
			_presenter.ShowToolBarChanged += OnPresenterShowToolBarChanged;

			LoadBookMarks();
			SyncFromPresenter();
		}

		public void SyncFromPresenter()
		{
			if (_presenter == null)
				return;

			_syncing = true;
			try
			{
				ShowGridLines = _presenter.ShowGridLines;
				ShowResizers = _presenter.ShowResizers;
				ShowBookMarks = _presenter.ShowBookMarks;
				ShowToolBar = _presenter.ShowToolBar;
				SelectedScaleIndex = GetScaleIndex(_presenter.Scale);
			}
			finally
			{
				_syncing = false;
			}
		}

		private void LoadBookMarks()
		{
			BookMarks.Clear();
			if (_presenter == null)
				return;

			foreach (var bookMark in _presenter.Manager.GetBookMarks())
				BookMarks.Add(new BookMarkItemViewModel(bookMark));
		}

		private int GetScaleIndex(float scale)
		{
			var percent = (int)Math.Round(scale * 100);
			for (int i = 0; i < ScaleOptions.Length; i++)
			{
				var optionPercent = int.Parse(ScaleOptions[i].TrimEnd('%'), CultureInfo.InvariantCulture);
				if (optionPercent == percent)
					return i;
			}

			return 3;
		}

		private void OnPresenterShowToolBarChanged(object? sender, System.EventArgs e)
		{
			if (_presenter == null)
				return;

			_syncing = true;
			try
			{
				ShowToolBar = _presenter.ShowToolBar;
			}
			finally
			{
				_syncing = false;
			}
		}

		partial void OnShowGridLinesChanged(bool value)
		{
			if (_syncing || _presenter == null)
				return;

			_presenter.ShowGridLines = value;
			_presenter.Update();
		}

		partial void OnShowResizersChanged(bool value)
		{
			if (_syncing || _presenter == null)
				return;

			_presenter.ShowResizers = value;
			_presenter.Update();
		}

		partial void OnShowBookMarksChanged(bool value)
		{
			if (_syncing || _presenter == null)
				return;

			_presenter.ShowBookMarks = value;
			_presenter.Update();
		}

		partial void OnSelectedScaleIndexChanged(int value)
		{
			if (_syncing || _presenter == null || value < 0 || value >= ScaleOptions.Length)
				return;

			var scaleStr = ScaleOptions[value].Replace("%", string.Empty);
			_presenter.Scale = float.Parse(scaleStr, CultureInfo.InvariantCulture) / 100f;
			_presenter.Update();
		}

		partial void OnSelectedBookMarkChanged(BookMarkItemViewModel? value)
		{
			if (value == null || _presenter == null)
				return;

			_presenter.TopRow = value.Location.Row;
			_presenter.CurrentCell = value.Location;
		}

		[RelayCommand]
		private async Task ExportExcelAsync()
		{
			if (_presenter == null || _host == null)
				return;

			await ReportExportService.ExportExcelAsync(_host, _presenter, openAfterExport: true);
		}

		[RelayCommand]
		private async Task ExportHtmlAsync()
		{
			if (_presenter == null || _host == null)
				return;

			await ReportExportService.ExportHtmlAsync(_host, _presenter, openAfterExport: true);
		}

		[RelayCommand]
		private async Task ExportImageAsync()
		{
			if (_presenter == null || _host == null)
				return;

			await ReportExportService.ExportImageAsync(_host, _presenter, openAfterExport: true);
		}

		[RelayCommand]
		private async Task SaveAsync()
		{
			if (_presenter == null || _host == null)
				return;

			await ReportExportService.SaveAsync(_host, _presenter);
		}

		[RelayCommand]
		private async Task FindAsync()
		{
			if (_presenter == null || _host == null)
				return;

			var window = TopLevel.GetTopLevel(_host) as Window;
			if (window == null)
				return;

			var dialog = new FindDialog(_presenter);
			await dialog.ShowDialog(window);
		}

		[RelayCommand]
		private async Task GoToRowAsync()
		{
			if (_presenter == null || _host == null)
				return;

			var window = TopLevel.GetTopLevel(_host) as Window;
			if (window == null)
				return;

			var viewModel = new GoToRowDialogViewModel(_presenter.CurrentCell.Row, _presenter.Manager.RowsCount);
			var dialog = new GoToRowDialog(viewModel);
			var result = await dialog.ShowDialog<bool?>(window);
			if (result == true && viewModel.DialogResult)
			{
				_presenter.CurrentCell = new CellLocation
				{
					Column = _presenter.CurrentCell.Column,
					Row = viewModel.SelectedRow - 1
				};
			}
		}

		[RelayCommand]
		private void ToggleBookMarks()
		{
			ShowBookMarks = !ShowBookMarks;
		}
	}
}
