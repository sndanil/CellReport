using CellReport.Avalonia;
using CellReport.Avalonia.ViewModels;
using CellReport.Core;
using CellReport.Export;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace CellReport.Avalonia.Viewer.ViewModels
{
	public partial class MainWindowViewModel : ObservableObject
	{
		private Window? _window;
		private string? _filePath;
		private string? _documentName;

		public PresentControlViewModel PresentControl { get; } = new();

		[ObservableProperty]
		private string _title = "CellReport Viewer";

		public MainWindowViewModel()
		{
			PresentControl.PropertyChanged += OnPresentControlPropertyChanged;
			PresentControl.SaveRequested += SaveDocumentAsync;
		}

		public async Task<bool> InitializeAsync(string[] args, Window window)
		{
			_window = window;
			BaseExporter.RendererFactory = new AvaloniaRendererFactory();

			string? filePath = null;
			if (args != null && args.Length > 0 && File.Exists(args[0]))
				filePath = args[0];
			else
				filePath = await ShowOpenFileDialogAsync(window);

			if (string.IsNullOrEmpty(filePath))
				return false;

			LoadFromFile(filePath);
			ApplyStartupEditMode(args);
			return true;
		}

		private void ApplyStartupEditMode(string[] args)
		{
			if (args == null || PresentControl.Manager == null)
				return;

			foreach (var arg in args)
			{
				if (!arg.StartsWith("--edit=", StringComparison.OrdinalIgnoreCase))
					continue;

				var value = arg.Substring("--edit=".Length);
				if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
					PresentControl.Manager.EditSettings.Mode = CellEditMode.AllCells;
				else if (string.Equals(value, "marked", StringComparison.OrdinalIgnoreCase))
					PresentControl.Manager.EditSettings.Mode = CellEditMode.MarkedCells;
				return;
			}
		}

		public void LoadFromFile(string filePath)
		{
			var template = new CellTemplate();
			template.Load(filePath);
			_filePath = filePath;
			_documentName = template.Manager.Properties.DocumentName;
			PresentControl.Load(template.Manager);
			UpdateTitle();
		}

		private async Task<bool> SaveDocumentAsync()
		{
			if (_window == null)
				return false;

			var suggested = string.IsNullOrWhiteSpace(_documentName) ? "Отчёт" : _documentName;
			var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
			{
				Title = "Сохранить заполненный шаблон",
				SuggestedFileName = suggested + ".tcr",
				FileTypeChoices = new[]
				{
					new FilePickerFileType("CellReport шаблон") { Patterns = new[] { "*.tcr" } },
					new FilePickerFileType("CellReport шаблон упакованный") { Patterns = new[] { "*.tcrp" } }
				}
			});

			if (file == null)
				return false;

			var path = file.Path.LocalPath;
			if (string.IsNullOrEmpty(Path.GetExtension(path)))
				path += ".tcr";

			var type = path.EndsWith(".tcrp", StringComparison.OrdinalIgnoreCase)
				? TemplateType.Package
				: TemplateType.Xml;

			var temp = path + ".tmp";
			try
			{
				if (File.Exists(temp))
					File.Delete(temp);

				new CellTemplate(PresentControl.Manager!).Save(temp, type);
				File.Move(temp, path, overwrite: true);
			}
			catch (Exception)
			{
				try
				{
					if (File.Exists(temp))
						File.Delete(temp);
				}
				catch
				{
					// ignore cleanup errors
				}

				await ShowMessageAsync("Не удалось сохранить файл. Возможно, он занят другим приложением.");
				return false;
			}

			_filePath = path;
			_documentName = PresentControl.Manager?.Properties.DocumentName ?? _documentName;
			UpdateTitle();
			return true;
		}

		private void OnPresentControlPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(PresentControlViewModel.IsDirty))
				UpdateTitle();
		}

		private void UpdateTitle()
		{
			var name = string.IsNullOrWhiteSpace(_documentName)
				? (string.IsNullOrEmpty(_filePath) ? "CellReport Viewer" : Path.GetFileName(_filePath))
				: _documentName;
			Title = (name ?? "CellReport Viewer") + (PresentControl.IsDirty ? " *" : string.Empty);
		}

		private async Task ShowMessageAsync(string message)
		{
			if (_window == null)
				return;

			var dialog = new Window
			{
				Title = "CellReport",
				Width = 380,
				Height = 150,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				Content = new StackPanel
				{
					Margin = new global::Avalonia.Thickness(16),
					Spacing = 12,
					Children =
					{
						new TextBlock
						{
							Text = message,
							TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
						}
					}
				}
			};

			var panel = (StackPanel)dialog.Content;
			var ok = new Button
			{
				Content = "ОК",
				HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
			};
			ok.Click += (_, _) => dialog.Close();
			panel.Children.Add(ok);

			await dialog.ShowDialog(_window);
		}

		private static async Task<string?> ShowOpenFileDialogAsync(Window window)
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
