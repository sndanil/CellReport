using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CellReport.Core;
using CellReport.Export;
using CellReport.Export.Excel;
using CellReport.Export.Html;
using CellReport.Presenters;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CellReport.Avalonia.Services
{
	public static class ReportExportService
	{
		public static async Task ExportExcelAsync(Visual host, BaseDisplayPresenter presenter, bool openAfterExport)
		{
			if (presenter?.Renderer == null)
				return;

			var topLevel = TopLevel.GetTopLevel(host);
			if (topLevel?.StorageProvider == null)
				return;

			var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				Title = ReportLocalization.Get("Report/ExportToExcel"),
				SuggestedFileName = presenter.Manager.Properties.DocumentName + ".xlsx",
				FileTypeChoices = new[]
				{
					new FilePickerFileType("Excel") { Patterns = new[] { "*.xlsx" } }
				}
			});

			if (file == null)
				return;

			await ExportToFileAsync(new ExcelExporter(presenter.Manager, presenter.Renderer), file.Path.LocalPath, openAfterExport);
		}

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
		}

		private static async Task ExportToFileAsync(BaseExporter exporter, string path, bool openAfterExport)
		{
			try
			{
				await using var stream = File.Create(path);
				exporter.ExportTo(stream);

				if (openAfterExport)
				{
					try
					{
						Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
					}
					catch
					{
						// ignore open errors
					}
				}
			}
			catch (IOException)
			{
				throw;
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
