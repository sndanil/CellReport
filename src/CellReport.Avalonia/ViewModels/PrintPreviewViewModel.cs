using CellReport.Avalonia;
using CellReport.Avalonia.Services;
using CellReport.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;

namespace CellReport.Avalonia.ViewModels
{
	public partial class PrintPreviewViewModel : ObservableObject
	{
		private readonly AvaloniaPrintPresenter _presenter;
		private readonly Visual _host;
		private bool _applyPrinter;
		private string? _builtForPrinter;

		public PrintPreviewViewModel(AvaloniaPrintPresenter presenter, Visual host, string? printerName = null)
		{
			_presenter = presenter;
			_host = host;
			TotalPages = Math.Max(1, presenter.PageCount);
			CurrentPage = presenter.PageCount > 0 ? 1 : 0;
			_autoFitWidth = presenter.Manager.PrintSettings.AutoFitWidthPages > 0;

			foreach (var printer in WindowsPrinterService.GetInstalledPrinters())
				Printers.Add(printer);

			var selected = !string.IsNullOrWhiteSpace(printerName) && Printers.Contains(printerName)
				? printerName
				: WindowsPrinterService.GetDefaultPrinter();
			if (!string.IsNullOrEmpty(selected) && !Printers.Contains(selected))
				Printers.Insert(0, selected);
			else if (Printers.Count > 0 && string.IsNullOrEmpty(selected))
				selected = Printers[0];

			SelectedPrinter = selected;
			_builtForPrinter = selected;
			_applyPrinter = true;
			ShowPaper(WindowsPrinterService.GetPageSize(selected, presenter.Manager.PrintSettings.Landscape));
			UpdatePageImage();
		}

		public string Title => ReportLocalization.Get("Report/Preview");

		public string PrintLabel => ReportLocalization.Get("Report/Print");

		public string PrinterLabel => ReportLocalization.Get("Report/Print/Printer");

		public string PageSetupLabel => ReportLocalization.Get("Report/PageSettings");

		public string AutoFitLabel => ReportLocalization.Get("Report/AutoFitPage");

		public string[] ZoomOptions { get; } = { "Авто", "25%", "50%", "75%", "100%", "150%", "200%", "250%" };

		public ObservableCollection<string> Printers { get; } = new();

		[ObservableProperty]
		private string? _selectedPrinter;

		[ObservableProperty]
		private string _paperDescription = string.Empty;

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

		partial void OnSelectedPrinterChanged(string? value)
		{
			if (!_applyPrinter || string.Equals(value, _builtForPrinter, StringComparison.Ordinal))
				return;

			RebuildPages();
		}

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

		private void ShowPaper(PrinterPageSize page)
		{
			var name = string.IsNullOrWhiteSpace(page.PaperName)
				? ReportLocalization.Get("Report/Preview/CustomPaper")
				: page.PaperName;
			PaperDescription = string.Format(
				CultureInfo.CurrentCulture,
				ReportLocalization.Get("Report/Preview/Paper"),
				name,
				page.WidthMm.ToString("0.#", CultureInfo.CurrentCulture),
				page.HeightMm.ToString("0.#", CultureInfo.CurrentCulture));
		}

		private void RebuildPages()
		{
			var landscape = _presenter.Manager.PrintSettings.Landscape;
			var pageSize = WindowsPrinterService.GetPageSize(SelectedPrinter, landscape);
			_builtForPrinter = SelectedPrinter;
			ShowPaper(pageSize);
			var margins = _presenter.Manager.PrintSettings.Margins ?? new Margins(0);
			_presenter.BuildPages(pageSize.WidthMm, pageSize.HeightMm, 96f, margins);
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
			await ReportPrintService.PrintAsync(_host, _presenter.Manager, custom: false, printerName: SelectedPrinter);
		}

		[RelayCommand]
		private async Task PageSetupAsync()
		{
			if (await ReportPrintService.ShowPageSetupAsync(_host, _presenter.Manager))
				RebuildPages();
		}
	}
}
