using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CellReport.Avalonia.Viewer.ViewModels;
using System;

namespace CellReport.Avalonia.Viewer
{
	public partial class MainWindow : Window
	{
		private bool _closeConfirmed;
		private bool _closingInProgress;

		public MainWindow()
		{
			InitializeComponent();
			Opened += OnMainWindowOpened;
			Closing += OnClosing;
		}

		private async void OnMainWindowOpened(object? sender, EventArgs e)
		{
			if (DataContext is not MainWindowViewModel viewModel)
				return;

			var loaded = await viewModel.InitializeAsync(Program.StartupArgs, this);
			if (!loaded)
				Close();
		}

		private async void OnClosing(object? sender, WindowClosingEventArgs e)
		{
			if (_closeConfirmed || DataContext is not MainWindowViewModel viewModel)
				return;

			if (!viewModel.PresentControl.IsDirty)
				return;

			if (_closingInProgress)
			{
				e.Cancel = true;
				return;
			}

			e.Cancel = true;
			_closingInProgress = true;
			try
			{
				var dialog = new UnsavedChangesDialog();
				var result = await dialog.ShowDialog<int>(this);
				if (result == 0)
					return;

				if (result == 1 && !await viewModel.SaveForCloseAsync())
					return;

				_closeConfirmed = true;
				Close();
			}
			finally
			{
				_closingInProgress = false;
			}
		}

		private sealed class UnsavedChangesDialog : Window
		{
			public UnsavedChangesDialog()
			{
				Title = "Несохранённые изменения";
				Width = 440;
				Height = 170;
				WindowStartupLocation = WindowStartupLocation.CenterOwner;

				var panel = new StackPanel
				{
					Margin = new Thickness(16),
					Spacing = 12
				};

				panel.Children.Add(new TextBlock
				{
					Text = "Есть несохранённые значения. Сохранить их перед закрытием?",
					TextWrapping = TextWrapping.Wrap
				});

				var buttons = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Spacing = 8,
					HorizontalAlignment = HorizontalAlignment.Right
				};

				var save = new Button { Content = "Сохранить" };
				save.Click += (_, _) => Close(1);
				var discard = new Button { Content = "Не сохранять" };
				discard.Click += (_, _) => Close(2);
				var cancel = new Button { Content = "Отмена" };
				cancel.Click += (_, _) => Close(0);

				buttons.Children.Add(save);
				buttons.Children.Add(discard);
				buttons.Children.Add(cancel);
				panel.Children.Add(buttons);
				Content = panel;
			}
		}
	}
}
