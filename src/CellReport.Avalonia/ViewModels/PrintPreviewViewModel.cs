using CellReport.Avalonia;
using CellReport.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace CellReport.Avalonia.ViewModels
{
	public partial class PrintPreviewViewModel : ObservableObject
	{
		private readonly AvaloniaPrintPresenter _presenter;
		private readonly Visual _host;
		private readonly string? _printerName;

		public PrintPreviewViewModel(AvaloniaPrintPresenter presenter, Visual host, string? printerName = null)
		{
			_presenter = presenter;
			_host = host;
			_printerName = printerName;
			TotalPages = Math.Max(1, presenter.PageCount);
			CurrentPage = presenter.PageCount > 0 ? 1 : 0;
			AutoFitWidth = presenter.Manager.PrintSettings.AutoFitWidthPages > 0;
			UpdatePageImage();
		}

		public string Title => ReportLocalization.Get("Report/Preview");

		public string PrintLabel => ReportLocalization.Get("Report/Print");

		public string PageSetupLabel => ReportLocalization.Get("Report/PageSettings");

		public string AutoFitLabel => ReportLocalization.Get("Report/AutoFitPage");

		public string[] ZoomOptions { get; } = { "Авто", "25%", "50%", "75%", "100%", "150%", "200%", "250%" };

		public int TotalPages { get; private set; }

		[ObservableProperty]
		private int _currentPage;

		[ObservableProperty]
		private int _selectedZoomIndex;

		[ObservableProperty]
		private bool _autoFitWidth;

		[ObservableProperty]
		private Bitmap? _pageImage;

		[ObservableProperty]
		private double _zoomScale = 1.0;

		public bool CanGoPrevious => CurrentPage > 1;

		public bool CanGoNext => CurrentPage < TotalPages;

		partial void OnCurrentPageChanged(int value)
		{
			UpdatePageImage();
			OnPropertyChanged(nameof(CanGoPrevious));
			OnPropertyChanged(nameof(CanGoNext));
		}

		partial void OnSelectedZoomIndexChanged(int value) => ApplyZoom();

		partial void OnAutoFitWidthChanged(bool value)
		{
			_presenter.Manager.PrintSettings.AutoFitWidthPages = value ? 1 : 0;
			RebuildPages();
		}

		private void ApplyZoom()
		{
			if (SelectedZoomIndex == 0)
			{
				ZoomScale = 1.0;
				return;
			}

			var text = ZoomOptions[SelectedZoomIndex].TrimEnd('%');
			if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
				ZoomScale = percent / 100.0;
		}

		private void UpdatePageImage()
		{
			PageImage?.Dispose();
			PageImage = null;

			if (CurrentPage < 1 || CurrentPage > _presenter.PageImages.Count)
				return;

			var bytes = _presenter.PageImages[CurrentPage - 1];
			if (bytes.Length == 0)
				return;

			using var stream = new MemoryStream(bytes);
			PageImage = new Bitmap(stream);
		}

		private void RebuildPages()
		{
			var pageSize = WindowsPrinterService.GetPageSize(_printerName, _presenter.Manager.PrintSettings.Landscape);
			_presenter.BuildPages(pageSize.WidthMm, pageSize.HeightMm);
			TotalPages = Math.Max(1, _presenter.PageCount);
			if (CurrentPage > TotalPages)
				CurrentPage = TotalPages;
			UpdatePageImage();
			OnPropertyChanged(nameof(TotalPages));
			OnPropertyChanged(nameof(CanGoPrevious));
			OnPropertyChanged(nameof(CanGoNext));
		}

		[RelayCommand]
		private void FirstPage()
		{
			CurrentPage = 1;
		}

		[RelayCommand]
		private void PreviousPage()
		{
			if (CurrentPage > 1)
				CurrentPage--;
		}

		[RelayCommand]
		private void NextPage()
		{
			if (CurrentPage < TotalPages)
				CurrentPage++;
		}

		[RelayCommand]
		private void LastPage()
		{
			CurrentPage = TotalPages;
		}

		[RelayCommand]
		private async Task PrintAsync()
		{
			await ReportPrintService.PrintAsync(_host, _presenter.Manager, custom: false, printerName: _printerName);
		}

		[RelayCommand]
		private async Task PageSetupAsync()
		{
			if (await ReportPrintService.ShowPageSetupAsync(_host, _presenter.Manager))
				RebuildPages();
		}
	}
}
