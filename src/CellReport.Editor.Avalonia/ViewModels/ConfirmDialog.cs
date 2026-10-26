using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace CellReport.Editor.Avalonia.ViewModels;

internal enum SaveChoice
{
	Save,
	Discard,
	Cancel
}

internal static class ConfirmDialog
{
	public static async Task<bool> AskAsync(Window owner, string title, string message)
	{
		var accepted = false;
		var window = CreateWindow(title);

		var yes = new Button { Content = "Да", MinWidth = 88, IsDefault = true };
		var no = new Button { Content = "Нет", MinWidth = 88, IsCancel = true };
		yes.Click += (_, _) =>
		{
			accepted = true;
			window.Close();
		};
		no.Click += (_, _) => window.Close();

		window.Content = CreateContent(message, yes, no);

		await window.ShowDialog(owner);
		return accepted;
	}

	public static async Task<SaveChoice> AskSaveAsync(Window owner, string title, string message)
	{
		var choice = SaveChoice.Cancel;
		var window = CreateWindow(title);

		var save = new Button { Content = "Сохранить", MinWidth = 110, IsDefault = true };
		var discard = new Button { Content = "Не сохранять", MinWidth = 120 };
		var cancel = new Button { Content = "Отмена", MinWidth = 88, IsCancel = true };

		save.Click += (_, _) =>
		{
			choice = SaveChoice.Save;
			window.Close();
		};
		discard.Click += (_, _) =>
		{
			choice = SaveChoice.Discard;
			window.Close();
		};
		cancel.Click += (_, _) => window.Close();

		window.Content = CreateContent(message, save, discard, cancel);

		await window.ShowDialog(owner);
		return choice;
	}

	private static Window CreateWindow(string title)
	{
		return new Window
		{
			Title = title,
			SizeToContent = SizeToContent.WidthAndHeight,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			CanResize = false,
			ShowInTaskbar = false
		};
	}

	private static Control CreateContent(string message, params Button[] buttons)
	{
		var buttonsPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Spacing = 8
		};

		foreach (var button in buttons)
			buttonsPanel.Children.Add(button);

		return new StackPanel
		{
			Margin = new global::Avalonia.Thickness(20, 16, 20, 16),
			Spacing = 16,
			Children =
			{
				new TextBlock
				{
					Text = message,
					MaxWidth = 420,
					TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
				},
				buttonsPanel
			}
		};
	}
}
