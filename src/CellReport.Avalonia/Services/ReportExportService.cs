using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CellReport.Core;
using CellReport.Export;
using CellReport.Export.Excel;
using CellReport.Export.Html;
using CellReport.Export.Image;
using CellReport.Export.Pdf;
using CellReport.Presenters;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CellReport.Avalonia.Services
{
	public static class ReportExportService
	{
		public static Task ExportExcelAsync(Visual host, BaseDisplayPresenter presenter, bool openAfterExport)
			=> ExportAsync(host, presenter,
				() => new ExcelExporter(presenter.Manager, presenter.Renderer),
				ReportLocalization.Get("Report/ExportToExcel"),
				presenter.Manager.Properties.DocumentName + ".xlsx",
				new[] { new FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } } },
				openAfterExport);

		public static Task ExportHtmlAsync(Visual host, BaseDisplayPresenter presenter, bool openAfterExport)
			=> ExportAsync(host, presenter,
				() => new HtmlExporter(presenter.Manager, presenter.Renderer),
				ReportLocalization.Get("Report/ExportToHtml"),
				presenter.Manager.Properties.DocumentName + ".html",
				new[] { new FilePickerFileType("HTML") { Patterns = new[] { "*.html" } } },
				openAfterExport);

		public static Task ExportImageAsync(Visual host, BaseDisplayPresenter presenter, bool openAfterExport)
			=> ExportAsync(host, presenter,
				() => new ImageExporter(presenter.Manager, presenter.Renderer),
				ReportLocalization.Get("Report/ExportToImage"),
				presenter.Manager.Properties.DocumentName + ".png",
				new[] { new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } } },
				openAfterExport);

		public static Task ExportPdfAsync(Visual host, BaseDisplayPresenter presenter, bool openAfterExport)
			=> ExportAsync(host, presenter,
				() => new PdfExporter(presenter.Manager, presenter.Renderer, AvaloniaRenderer.GetTextSize),
				ReportLocalization.Get("Report/ExportToPdf"),
				presenter.Manager.Properties.DocumentName + ".pdf",
				new[] { new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } } },
				openAfterExport);

		public static async Task SaveAsync(Visual host, BaseDisplayPresenter presenter)
		{
			if (presenter?.Renderer == null)
				return;

			var topLevel = TopLevel.GetTopLevel(host);
			if (topLevel?.StorageProvider == null)
				return;

			var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				Title = ReportLocalization.Get("Report/Save"),
				SuggestedFileName = presenter.Manager.Properties.DocumentName + ".xlsx",
				FileTypeChoices = new[]
				{
					new FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } },
					new FilePickerFileType("HTML") { Patterns = new[] { "*.html" } },
					new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } },
					new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } },
					new FilePickerFileType("CellReport template") { Patterns = new[] { "*.tcr" } },
					new FilePickerFileType("CellReport package") { Patterns = new[] { "*.tcrp" } }
				}
			});

			if (file == null)
				return;

			var path = file.Path.LocalPath;
			try
			{
				if (path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
				{
					await ExportToFileAsync(new ExcelExporter(presenter.Manager, presenter.Renderer), path, false);
				}
				else if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
				{
					await ExportToFileAsync(new HtmlExporter(presenter.Manager, presenter.Renderer), path, false);
				}
				else if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
				{
					await ExportToFileAsync(new ImageExporter(presenter.Manager, presenter.Renderer), path, false);
				}
				else if (path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
				{
					await ExportToFileAsync(new PdfExporter(presenter.Manager, presenter.Renderer, AvaloniaRenderer.GetTextSize), path, false);
				}
				else
				{
					var template = new CellTemplate(presenter.Manager);
					template.Save(path, path.EndsWith(".tcrp", StringComparison.OrdinalIgnoreCase) ? TemplateType.Package : TemplateType.Xml);
				}
			}
			catch (IOException)
			{
				await ShowMessageAsync(host, ReportLocalization.Get("Report/Export/Error"));
			}
			catch (Win32Exception)
			{
				await ShowMessageAsync(host, ReportLocalization.Get("Report/Export/OpenError"));
			}
		}

		private static async Task ExportAsync(
			Visual host,
			BaseDisplayPresenter presenter,
			Func<BaseExporter> createExporter,
			string title,
			string suggestedFileName,
			FilePickerFileType[] fileTypes,
			bool openAfterExport)
		{
			if (presenter?.Renderer == null)
				return;

			var topLevel = TopLevel.GetTopLevel(host);
			if (topLevel?.StorageProvider == null)
				return;

			var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				Title = title,
				SuggestedFileName = suggestedFileName,
				FileTypeChoices = fileTypes
			});

			if (file == null)
				return;

			try
			{
				await ExportToFileAsync(createExporter(), file.Path.LocalPath, openAfterExport);
			}
			catch (IOException)
			{
				await ShowMessageAsync(host, ReportLocalization.Get("Report/Export/Error"));
			}
			catch (Win32Exception)
			{
				await ShowMessageAsync(host, ReportLocalization.Get("Report/Export/OpenError"));
			}
		}

		private static async Task ExportToFileAsync(BaseExporter exporter, string path, bool openAfterExport)
		{
			await Task.Run(() =>
			{
				using (DesignModeScope.Suspend(exporter.Manager))
					ExportToFile(exporter, path, openAfterExport);
			});
		}

		private static void ExportToFile(BaseExporter exporter, string path, bool openAfterExport)
		{
			using (var stream = File.Create(path))
				exporter.ExportTo(stream);

			if (!openAfterExport)
				return;

			try
			{
				Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
			}
			catch (Win32Exception)
			{
				throw;
			}
			catch
			{
				// ignore other open errors
			}
		}

		public static async Task ShowMessageAsync(Visual host, string message)
		{
			var window = TopLevel.GetTopLevel(host) as Window;
			if (window == null)
				return;

			var dialog = new Window
			{
				Title = "CellReport",
				Width = 360,
				Height = 140,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Content = new StackPanel
				{
					Margin = new global::Avalonia.Thickness(16),
					Spacing = 12,
					Children =
					{
						new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
						new Button
						{
							Content = "OK",
							HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
							Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => { })
						}
					}
				}
			};

			if (dialog.Content is StackPanel panel && panel.Children[1] is Button okButton)
				okButton.Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => dialog.Close());

			await dialog.ShowDialog(window);
		}
	}
}
