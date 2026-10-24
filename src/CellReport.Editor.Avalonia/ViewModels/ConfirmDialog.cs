using Avalonia.Controls;
using Avalonia.Layout;
using System.Threading.Tasks;

namespace CellReport.Editor.Avalonia.ViewModels;

internal static class ConfirmDialog
{
	public static async Task<bool> AskAsync(Window owner, string title, string message)
	{
		var accepted = false;
		var window = new Window
		{
			Title = title,
			Width = 440,
			Height = 170,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			CanResize = false
		};

		var yes = new Button { Content = "Да", Width = 88, IsDefault = true };
		var no = new Button { Content = "Нет", Width = 88, IsCancel = true };
		yes.Click += (_, _) =>
		{
			accepted = true;
			window.Close();
		};
		no.Click += (_, _) => window.Close();

		window.Content = new StackPanel
		{
			Margin = new global::Avalonia.Thickness(16),
			Spacing = 16,
			Children =
			{
				new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap },
				new StackPanel
				{
					Orientation = Orientation.Horizontal,
					HorizontalAlignment = HorizontalAlignment.Right,
					Spacing = 8,
					Children = { yes, no }
				}
			}
		};

		await window.ShowDialog(owner);
		return accepted;
	}
}
