using CellReport.Avalonia;
using CellReport.Avalonia.ViewModels;
using CellReport.Core;
using CellReport.Export;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace CellReport.Avalonia.Viewer.ViewModels
{
	public partial class MainWindowViewModel : ObservableObject
	{
		public PresentControlViewModel PresentControl { get; } = new();

		[ObservableProperty]
		private string _title = "CellReport Viewer";

		public async Task<bool> InitializeAsync(string[] args, Window window)
		{
			BaseExporter.RendererFactory = new AvaloniaRendererFactory();

			string filePath = null;
			if (args != null && args.Length > 0 && File.Exists(args[0]))
				filePath = args[0];
			else
				filePath = await ShowOpenFileDialogAsync(window);

			if (string.IsNullOrEmpty(filePath))
				return false;

			LoadFromFile(filePath);
			return true;
		}

		public void LoadFromFile(string filePath)
		{
			var template = new CellTemplate();
			template.Load(filePath);
			PresentControl.Load(template.Manager);

			var documentName = template.Manager.Properties.DocumentName;
			Title = string.IsNullOrWhiteSpace(documentName)
				? Path.GetFileName(filePath)
				: documentName;
		}

		private static async Task<string> ShowOpenFileDialogAsync(Window window)
		{
			var storageProvider = window.StorageProvider;
			if (!storageProvider.CanOpen)
				return null;

			var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = "Открыть отчет",
				AllowMultiple = false,
				FileTypeFilter = new[]
				{
					new FilePickerFileType("CellReport templates")
					{
						Patterns = new[] { "*.tcr", "*.tcrp", "*.xml" }
					},
					new FilePickerFileType("All files")
					{
						Patterns = new[] { "*.*" }
					}
				}
			});

			return files.Count > 0 ? files[0].Path.LocalPath : null;
		}
	}
}
