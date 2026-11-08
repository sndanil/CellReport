using CellReport.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace CellReport.Avalonia.ViewModels
{
	public partial class PrintOptionsDialogViewModel : ObservableObject
	{
		public PrintOptionsDialogViewModel(int totalPages, short defaultCopies, string? printerName = null)
		{
			TotalPages = totalPages;
			FromPage = 1;
			ToPage = totalPages;
			Copies = defaultCopies;

			foreach (var printer in PrinterService.GetInstalledPrinters())
				Printers.Add(printer);

			SelectedPrinter = !string.IsNullOrWhiteSpace(printerName) && Printers.Contains(printerName)
				? printerName
				: PrinterService.GetDefaultPrinter();
			if (!string.IsNullOrEmpty(SelectedPrinter) && !Printers.Contains(SelectedPrinter))
				Printers.Insert(0, SelectedPrinter);
			else if (Printers.Count > 0 && string.IsNullOrEmpty(SelectedPrinter))
				SelectedPrinter = Printers[0];
		}

		public string Title => ReportLocalization.Get("Report/CustomPrint");

		public string PrinterLabel => ReportLocalization.Get("Report/Print/Printer");

		public string PageRangeLabel => ReportLocalization.Get("Report/Print/PageRange");

		public string FromLabel => ReportLocalization.Get("Report/Print/From");

		public string ToLabel => ReportLocalization.Get("Report/Print/To");

		public string CopiesLabel => ReportLocalization.Get("Report/PageSetup/Copies");

		public string PrintLabel => ReportLocalization.Get("Report/Print");

		public string CancelLabel => ReportLocalization.Get("Report/GoToRow/Cancel");

		public string PageCountHint => string.Format(ReportLocalization.Get("Report/Print/OfPages"), TotalPages);

		public int TotalPages { get; }

		public ObservableCollection<string> Printers { get; } = new();

		[ObservableProperty]
		private string? _selectedPrinter;

		[ObservableProperty]
		private int _fromPage = 1;

		[ObservableProperty]
		private int _toPage = 1;

		[ObservableProperty]
		private int _copies = 1;

		public void NormalizePageRange()
		{
			FromPage = Math.Clamp(FromPage, 1, TotalPages);
			ToPage = Math.Clamp(ToPage, 1, TotalPages);
			if (FromPage > ToPage)
				(FromPage, ToPage) = (ToPage, FromPage);
		}
	}
}
