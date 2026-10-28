using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CellReport.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;

namespace CellReport.Editor.Avalonia.ViewModels;

/// <summary>
/// Управление ресурсами шаблона (изображения внутри упакованного .tcrp),
/// а также встраивание внешних изображений (file://) в ресурсы.
/// </summary>
public partial class ResourcesViewModel : ObservableObject
{
	private const string ResourceScheme = "resources://";
	private const string FileScheme = "file://";

	private static readonly FilePickerFileType ImageFileType = new("Изображения")
	{
		Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" },
		MimeTypes = new[] { "image/*" }
	};

	private readonly MainWindowViewModel _owner;
	private CellManager? _manager;
	private bool _writing;

	public ResourcesViewModel(MainWindowViewModel owner)
	{
		_owner = owner;
		_owner.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(MainWindowViewModel.DocumentPath))
				Refresh();
		};
	}

	public ObservableCollection<ResourceRow> Resources { get; } = new();

	public ObservableCollection<ExternalImageRow> ExternalImages { get; } = new();

	[ObservableProperty]
	private ResourceRow? _selectedResource;

	[ObservableProperty]
	private string _summary = string.Empty;

	[ObservableProperty]
	private string _message = string.Empty;

	[ObservableProperty]
	private bool _hasMessage;

	[ObservableProperty]
	private string _renameText = string.Empty;

	[ObservableProperty]
	private bool _isXmlDocument;

	[ObservableProperty]
	private Bitmap? _preview;

	[ObservableProperty]
	private string _details = string.Empty;

	[ObservableProperty]
	private bool _hasSelection;

	[ObservableProperty]
	private bool _hasExternalImages;

	public void Load(CellManager? manager)
	{
		_manager = manager;
		Refresh();
	}

	public void Refresh()
	{
		var previousName = SelectedResource?.Name;
		Resources.Clear();
		ExternalImages.Clear();

		if (_manager == null)
		{
			Summary = "Шаблон не открыт";
			Message = string.Empty;
			HasMessage = false;
			IsXmlDocument = false;
			HasExternalImages = false;
			SelectedResource = null;
			return;
		}

		var usageByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		var externalRows = new List<ExternalImageRow>();

		foreach (var pair in _manager.GetUriReferenceCounts())
		{
			if (pair.Key.StartsWith(ResourceScheme, StringComparison.OrdinalIgnoreCase))
			{
				var name = pair.Key.Substring(ResourceScheme.Length);
				usageByName[name] = pair.Value;
				continue;
			}

			externalRows.Add(CreateExternalRow(pair.Key, pair.Value));
		}

		long totalBytes = 0;
		foreach (var name in _manager.GetResourceNames().OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase))
		{
			var data = _manager.GetResource(name);
			totalBytes += data.LongLength;

			int usage;
			usageByName.TryGetValue(name, out usage);
			Resources.Add(new ResourceRow(name, data, usage));
		}

		foreach (var row in externalRows.OrderBy(item => item.Uri, StringComparer.CurrentCultureIgnoreCase))
			ExternalImages.Add(row);

		Summary = "Ресурсов: " + Resources.Count
			+ (Resources.Count > 0 ? " · " + FormatSize(totalBytes) : string.Empty);
		IsXmlDocument = !_owner.IsPackageDocument;
		HasExternalImages = ExternalImages.Count > 0;

		SelectedResource = Resources.FirstOrDefault(item => string.Equals(item.Name, previousName, StringComparison.Ordinal))
			?? Resources.FirstOrDefault();

		EmbedAllCommand.NotifyCanExecuteChanged();
	}

	partial void OnSelectedResourceChanged(ResourceRow? value)
	{
		HasSelection = value != null;
		ReplaceCommand.NotifyCanExecuteChanged();
		ExtractCommand.NotifyCanExecuteChanged();
		RemoveCommand.NotifyCanExecuteChanged();
		RenameCommand.NotifyCanExecuteChanged();

		if (value == null || _manager == null)
		{
			Preview = null;
			Details = string.Empty;
			RenameText = string.Empty;
			return;
		}

		Preview = CreatePreview(_manager.GetResource(value.Name));
		Details = value.DimensionsText + " · " + value.SizeText + " · " + value.UsageText;
		RenameText = value.Name;
	}

	[RelayCommand]
	private async Task Add()
	{
		var window = _owner.Window;
		if (window == null || _manager == null)
			return;

		if (!await ConfirmPackageAsync("Добавить ресурс"))
			return;

		var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Добавить ресурсы",
			AllowMultiple = true,
			FileTypeFilter = new[] { ImageFileType }
		});

		if (files.Count == 0)
			return;

		var reserved = new HashSet<string>(_manager.GetResourceNames(), StringComparer.OrdinalIgnoreCase);
		var items = new List<KeyValuePair<string, byte[]>>();
		var skipped = 0;

		foreach (var file in files)
		{
			var data = await ReadAsync(file);
			if (data == null || !IsImage(data))
			{
				skipped++;
				continue;
			}

			var name = UniqueName(SanitizeName(file.Name), reserved);
			reserved.Add(name);
			items.Add(new KeyValuePair<string, byte[]>(name, data));
		}

		if (items.Count == 0)
		{
			Message = "Не удалось добавить: выбранные файлы не являются изображениями";
			HasMessage = true;
			return;
		}

		Commit(() =>
		{
			foreach (var item in items)
				_manager.AddResource(item.Key, item.Value);
		});

		Refresh();
		SelectedResource = Resources.FirstOrDefault(item => item.Name == items[items.Count - 1].Key) ?? SelectedResource;
		Message = "Добавлено ресурсов: " + items.Count
			+ (skipped > 0 ? " · пропущено: " + skipped : string.Empty);
		HasMessage = true;
	}

	[RelayCommand(CanExecute = nameof(HasSelectedResource))]
	private async Task Replace()
	{
		var row = SelectedResource;
		var window = _owner.Window;
		var manager = _manager;
		if (row == null || window == null || manager == null)
			return;

		if (!await ConfirmPackageAsync("Заменить ресурс"))
			return;

		var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = "Заменить ресурс: " + row.Name,
			AllowMultiple = false,
			FileTypeFilter = new[] { ImageFileType }
		});

		if (files.Count == 0)
			return;

		var data = await ReadAsync(files[0]);
		if (data == null || !IsImage(data))
		{
			Message = "Файл не является изображением";
			HasMessage = true;
			return;
		}

		Commit(() =>
		{
			manager.AddResource(row.Name, data);
			manager.InvalidateImageCaches(ResourceScheme + row.Name);
		});

		Refresh();
		SelectedResource = Resources.FirstOrDefault(item => item.Name == row.Name) ?? SelectedResource;
		Message = "Ресурс заменён: " + row.Name;
		HasMessage = true;
	}

	[RelayCommand(CanExecute = nameof(HasSelectedResource))]
	private async Task Extract()
	{
		var row = SelectedResource;
		var window = _owner.Window;
		var manager = _manager;
		if (row == null || window == null || manager == null)
			return;

		var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Сохранить ресурс",
			SuggestedFileName = row.Name,
			FileTypeChoices = new[]
			{
				new FilePickerFileType("Все файлы") { Patterns = new[] { "*.*" } }
			}
		});

		if (file == null)
			return;

		try
		{
			var data = manager.GetResource(row.Name);
			await using var stream = await file.OpenWriteAsync();
			await stream.WriteAsync(data, 0, data.Length);
			Message = "Ресурс сохранён: " + file.Name;
			HasMessage = true;
		}
		catch (Exception ex)
		{
			Message = "Не удалось сохранить ресурс: " + ex.Message;
			HasMessage = true;
		}
	}

	[RelayCommand(CanExecute = nameof(CanRemoveResource))]
	private void Remove()
	{
		var row = SelectedResource;
		var manager = _manager;
		if (row == null || manager == null)
			return;

		if (row.Usage > 0)
		{
			Message = "Ресурс используется (" + row.Usage + ") — удаление запрещено";
			HasMessage = true;
			return;
		}

		Commit(() =>
		{
			manager.RemoveResource(row.Name);
			manager.InvalidateImageCaches(ResourceScheme + row.Name);
		});

		Refresh();
		Message = "Ресурс удалён: " + row.Name;
		HasMessage = true;
	}

	[RelayCommand(CanExecute = nameof(HasSelectedResource))]
	private void Rename()
	{
		var row = SelectedResource;
		var manager = _manager;
		if (row == null || manager == null)
			return;

		var newName = (RenameText ?? string.Empty).Trim();
		if (newName.Length == 0)
		{
			Message = "Имя ресурса не может быть пустым";
			HasMessage = true;
			return;
		}

		if (string.Equals(newName, row.Name, StringComparison.Ordinal))
			return;

		if (!IsValidResourceName(newName, out var error))
		{
			Message = error;
			HasMessage = true;
			return;
		}

		if (manager.GetResourceNames().Any(item => string.Equals(item, newName, StringComparison.OrdinalIgnoreCase)))
		{
			Message = "Ресурс с именем «" + newName + "» уже существует";
			HasMessage = true;
			return;
		}

		var oldName = row.Name;
		Commit(() =>
		{
			var data = manager.GetResource(oldName);
			manager.AddResource(newName, data);
			manager.ReplaceUriReferences(ResourceScheme + oldName, ResourceScheme + newName);
			manager.RemoveResource(oldName);
		});

		Refresh();
		SelectedResource = Resources.FirstOrDefault(item => item.Name == newName) ?? SelectedResource;
		Message = "Ресурс переименован: " + oldName + " → " + newName;
		HasMessage = true;
	}

	internal async Task EmbedAsync(ExternalImageRow row)
	{
		var manager = _manager;
		var window = _owner.Window;
		if (manager == null || window == null)
			return;

		if (!await ConfirmPackageAsync("Встроить изображение"))
			return;

		var data = manager.GetDataByUri(row.Uri);
		if (data == null || data.Length == 0)
		{
			Message = "Файл не найден: " + row.FileName;
			HasMessage = true;
			return;
		}

		if (!IsImage(data))
		{
			Message = "Файл не является изображением: " + row.FileName;
			HasMessage = true;
			return;
		}

		var reserved = new HashSet<string>(manager.GetResourceNames(), StringComparer.OrdinalIgnoreCase);
		var name = UniqueName(SanitizeName(row.FileName), reserved);
		var replaced = 0;

		Commit(() =>
		{
			manager.AddResource(name, data);
			replaced = manager.ReplaceUriReferences(row.Uri, ResourceScheme + name);
		});

		Refresh();
		SelectedResource = Resources.FirstOrDefault(item => item.Name == name) ?? SelectedResource;
		Message = "Встроено: " + name + (replaced > 1 ? " · ссылок: " + replaced : string.Empty);
		HasMessage = true;
	}

	[RelayCommand(CanExecute = nameof(CanEmbedAll))]
	private async Task EmbedAll()
	{
		var manager = _manager;
		var window = _owner.Window;
		if (manager == null || window == null)
			return;

		if (!await ConfirmPackageAsync("Встроить изображения"))
			return;

		var reserved = new HashSet<string>(manager.GetResourceNames(), StringComparer.OrdinalIgnoreCase);
		var items = new List<ExternalImageRow>();
		var names = new List<string>();

		foreach (var row in ExternalImages.Where(item => item.CanEmbed).ToList())
		{
			var data = manager.GetDataByUri(row.Uri);
			if (data == null || data.Length == 0 || !IsImage(data))
				continue;

			var name = UniqueName(SanitizeName(row.FileName), reserved);
			reserved.Add(name);
			items.Add(row);
			names.Add(name);
		}

		if (items.Count == 0)
		{
			Message = "Нет доступных внешних изображений для встраивания";
			HasMessage = true;
			return;
		}

		var replaced = 0;
		Commit(() =>
		{
			for (var index = 0; index < items.Count; index++)
			{
				var data = manager.GetDataByUri(items[index].Uri);
				manager.AddResource(names[index], data);
				replaced += manager.ReplaceUriReferences(items[index].Uri, ResourceScheme + names[index]);
			}
		});

		Refresh();
		Message = "Встроено изображений: " + items.Count + " · ссылок: " + replaced;
		HasMessage = true;
	}

	[RelayCommand]
	private async Task SaveAsPackage()
	{
		if (await _owner.SaveAsPackageAsync())
			Refresh();
	}

	private bool HasSelectedResource() => SelectedResource != null;

	private bool CanRemoveResource() => SelectedResource != null && SelectedResource.Usage == 0;

	private bool CanEmbedAll() => ExternalImages.Any(item => item.CanEmbed);

	private ExternalImageRow CreateExternalRow(string uri, int usage)
	{
		var fileName = GetFileName(uri);
		var canEmbed = false;
		var status = "внешний адрес";

		if (uri.StartsWith(FileScheme, StringComparison.OrdinalIgnoreCase) && fileName.Length > 0)
		{
			var data = _manager?.GetDataByUri(uri);
			canEmbed = data != null && data.Length > 0;
			status = canEmbed ? "файл найден" : "файл не найден";
		}

		return new ExternalImageRow(this, uri, fileName, usage, canEmbed, status);
	}

	private async Task<bool> ConfirmPackageAsync(string action)
	{
		var window = _owner.Window;
		if (window == null || _owner.IsPackageDocument)
			return true;

		return await ConfirmDialog.AskAsync(
			window,
			action,
			"Ресурсы сохраняются только в упакованном шаблоне (.tcrp)." + Environment.NewLine
			+ "Текущий шаблон сохраняется как XML, и ресурсы в него не попадут." + Environment.NewLine
			+ "Продолжить?");
	}

	private void Commit(Action change)
	{
		if (_manager == null || _writing)
			return;

		_writing = true;
		try
		{
			_owner.CommitDocumentChange(change);
		}
		finally
		{
			_writing = false;
		}
	}

	private static async Task<byte[]?> ReadAsync(IStorageFile file)
	{
		try
		{
			await using var stream = await file.OpenReadAsync();
			using var memory = new MemoryStream();
			await stream.CopyToAsync(memory);
			return memory.ToArray();
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static Bitmap? CreatePreview(byte[] data)
	{
		if (data == null || data.Length == 0)
			return null;

		try
		{
			using var stream = new MemoryStream(data, writable: false);
			return new Bitmap(stream);
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static bool IsImage(byte[] data)
	{
		if (data == null || data.Length == 0)
			return false;

		try
		{
			using var bitmap = SKBitmap.Decode(data);
			return bitmap != null;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static string GetFileName(string uri)
	{
		var index = uri.IndexOf("://", StringComparison.Ordinal);
		var path = index >= 0 ? uri.Substring(index + 3) : uri;
		try
		{
			path = Uri.UnescapeDataString(path);
		}
		catch (Exception)
		{
			// оставляем как есть
		}

		return Path.GetFileName(path.Replace('/', Path.DirectorySeparatorChar));
	}

	private static string SanitizeName(string fileName)
	{
		var name = Path.GetFileName(fileName ?? string.Empty);
		foreach (var symbol in Path.GetInvalidFileNameChars())
			name = name.Replace(symbol, '_');

		name = name.Replace('/', '_').Replace('\\', '_').Trim();
		return name.Length == 0 ? "image" : name;
	}

	private static string UniqueName(string name, IEnumerable<string> reserved)
	{
		var items = reserved as ICollection<string> ?? reserved.ToList();
		if (!items.Any(item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase)))
			return name;

		var extension = Path.GetExtension(name);
		var stem = Path.GetFileNameWithoutExtension(name);
		for (var index = 2; ; index++)
		{
			var candidate = stem + " (" + index + ")" + extension;
			if (!items.Any(item => string.Equals(item, candidate, StringComparison.OrdinalIgnoreCase)))
				return candidate;
		}
	}

	private static bool IsValidResourceName(string name, out string error)
	{
		error = string.Empty;
		if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
			|| name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0 || name.IndexOf(':') >= 0)
		{
			error = "Имя ресурса содержит недопустимые символы";
			return false;
		}

		return true;
	}

	internal static string FormatSize(long bytes)
	{
		if (bytes >= 1024L * 1024L)
			return (bytes / (1024d * 1024d)).ToString("0.#") + " МБ";

		if (bytes >= 1024L)
			return (bytes / 1024d).ToString("0.#") + " КБ";

		return bytes + " Б";
	}

	internal static bool TryGetImageSize(byte[] data, out int width, out int height)
	{
		width = 0;
		height = 0;
		if (data == null || data.Length == 0)
			return false;

		try
		{
			using var bitmap = SKBitmap.Decode(data);
			if (bitmap == null)
				return false;

			width = bitmap.Width;
			height = bitmap.Height;
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}

public sealed class ResourceRow
{
	public ResourceRow(string name, byte[] data, int usage)
	{
		Name = name;
		Size = data.LongLength;
		SizeText = ResourcesViewModel.FormatSize(Size);
		Usage = usage;
		UsageText = usage == 0 ? "не используется" : "используется: " + usage;

		if (ResourcesViewModel.TryGetImageSize(data, out var width, out var height))
			DimensionsText = width + " × " + height;
		else
			DimensionsText = "не изображение";
	}

	public string Name { get; }

	public long Size { get; }

	public string SizeText { get; }

	public int Usage { get; }

	public string UsageText { get; }

	public string DimensionsText { get; }
}

public sealed class ExternalImageRow
{
	public ExternalImageRow(ResourcesViewModel owner, string uri, string fileName, int usage, bool canEmbed, string status)
	{
		Uri = uri;
		FileName = fileName;
		Usage = usage;
		UsageText = usage == 1 ? "используется: 1" : "используется: " + usage;
		StatusText = status;
		CanEmbed = canEmbed;

		EmbedCommand = new RelayCommand(() => _ = owner.EmbedAsync(this), () => CanEmbed);
	}

	public string Uri { get; }

	public string FileName { get; }

	public int Usage { get; }

	public string UsageText { get; }

	public string StatusText { get; }

	public bool CanEmbed { get; }

	public IRelayCommand EmbedCommand { get; }
}
