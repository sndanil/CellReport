using Avalonia.Controls;
using Avalonia.Interactivity;
using CellReport.Avalonia;
using CellReport.Avalonia.Services;
using CellReport.Editor.Avalonia.CodeEditor;
using CellReport.Editor.Avalonia.ViewModels;
using CellReport.Editor.Avalonia.Views;
using System;

namespace CellReport.Editor.Avalonia;

public partial class MainWindow : Window
{
	private XmlCodeEditor? _codeEditor;
	private SettingsWindow? _settingsWindow;
	private CheckWindow? _checkWindow;
	private bool _modeReady;
	private bool _modeGuard;
	private bool _closeConfirmed;

	public MainWindow()
	{
		InitializeComponent();
		Opened += OnMainWindowOpened;
		Closing += OnWindowClosing;
		Closed += OnWindowClosed;
	}

	private void OnWindowClosed(object? sender, EventArgs e)
	{
		_settingsWindow?.Close();
		_checkWindow?.Close();
		_codeEditor = null;
		if (DataContext is IDisposable disposable)
			disposable.Dispose();
	}

	private async void OnMainWindowOpened(object? sender, EventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		viewModel.ExitRequested += (_, _) => Close();
		viewModel.ShowDesignerRequested += (_, _) => ShowDesigner();
		viewModel.SettingsRequested += (_, _) => ShowSettings();
		viewModel.AboutRequested += async (_, _) => await new AboutWindow().ShowDialog(this);
		viewModel.FileMenuRequested += (_, _) => Ribbon.ShowFileMenu();
		viewModel.CheckRequested += (_, _) => ShowTemplateCheck();
		viewModel.FitToWidthRequested += (_, _) => DesignerPane.FitToWidth();
		viewModel.CodeSearchRequested += (_, replace) => ShowCodeSearch(replace);
		viewModel.FindRowInCodeRequested += (_, _) => FindRowInCode();
		viewModel.ReadCode = () => CodePane.Editor.Text;
		viewModel.CodeTextInvalidated += (_, _) => ShowCurrentCode(viewModel);
		_codeEditor = new XmlCodeEditor(CodePane.Editor, () => viewModel.Manager, viewModel.CodeErrors);
		viewModel.CanUndoCode = () => _codeEditor.CanUndo;
		viewModel.CanRedoCode = () => _codeEditor.CanRedo;
		viewModel.UndoCode = () => _codeEditor.UndoChange();
		viewModel.RedoCode = () => _codeEditor.RedoChange();
		_codeEditor.HistoryChanged += (_, _) =>
		{
			viewModel.NotifyCodeEdited();
			viewModel.NotifyHistory();
		};
		viewModel.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(MainWindowViewModel.InvertReport))
				ApplyReportInversion(viewModel);
		};
		ApplyReportInversion(viewModel);
		ApplyWindowState(viewModel);
		await viewModel.InitializeAsync(Program.StartupArgs, this);
		_modeReady = true;
	}

	private void ApplyWindowState(MainWindowViewModel viewModel)
	{
		if (viewModel.WindowMaximized)
		{
			WindowState = WindowState.Maximized;
			return;
		}

		WindowState = WindowState.Normal;
		Width = viewModel.WindowWidth;
		Height = viewModel.WindowHeight;
	}

	private void ApplyReportInversion(MainWindowViewModel viewModel)
	{
		DesignerPane.Surface.InvertContentMode = viewModel.InvertReport
			? ContentInversionMode.Always
			: ContentInversionMode.Never;
	}

	private void ShowCodeSearch(bool replace)
	{
		if (!ReferenceEquals(ModeTabs.SelectedItem, CodeTab))
			ModeTabs.SelectedItem = CodeTab;
		_codeEditor?.ShowSearch(replace);
	}

	private void FindRowInCode()
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		var row = viewModel.Presenter?.CurrentCell.Row ?? 0;
		if (!ReferenceEquals(ModeTabs.SelectedItem, CodeTab))
			ModeTabs.SelectedItem = CodeTab;
		_codeEditor?.SelectTemplateRow(row);
	}

	private void ShowDesigner()
	{
		_modeGuard = true;
		ModeTabs.SelectedItem = DesignerTab;
		_modeGuard = false;
		_codeEditor?.SetActive(false);
	}

	private void ShowSettings()
	{
		if (_settingsWindow != null)
		{
			_settingsWindow.Activate();
			return;
		}

		if (DataContext is not MainWindowViewModel viewModel)
			return;

		var window = new SettingsWindow { DataContext = viewModel };
		window.Closed += (_, _) => _settingsWindow = null;
		_settingsWindow = window;
		window.Show(this);
	}

	private void CloseSettingsWindow()
	{
		if (_settingsWindow == null)
			return;

		_settingsWindow.Close();
		_settingsWindow = null;
	}

	private void ShowTemplateCheck()
	{
		if (_checkWindow != null)
		{
			_checkWindow.Refresh();
			_checkWindow.Activate();
			return;
		}

		if (DataContext is not MainWindowViewModel viewModel)
			return;

		var window = new CheckWindow { DataContext = new CheckViewModel(viewModel) };
		window.Closed += (_, _) => _checkWindow = null;
		_checkWindow = window;
		window.Show(this);
	}

	private async void OnModeChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (!_modeReady || _modeGuard || !ReferenceEquals(e.Source, ModeTabs))
			return;
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		if (ReferenceEquals(ModeTabs.SelectedItem, CodeTab))
		{
			CloseSettingsWindow();
			ShowCurrentCode(viewModel);
			viewModel.EnterCodeMode();
			_codeEditor?.SetActive(true);
			return;
		}

		if (!viewModel.IsCodeMode)
			return;

		if (viewModel.TryLeaveCodeMode(out var error))
		{
			_codeEditor?.SetActive(false);
			return;
		}

		_modeGuard = true;
		ModeTabs.SelectedItem = CodeTab;
		_modeGuard = false;
		await ReportExportService.ShowMessageAsync(this, "Код шаблона не применён." + Environment.NewLine + error);
	}

	private void ShowCurrentCode(MainWindowViewModel viewModel)
	{
		var xml = viewModel.ExportCode();
		if (xml == null)
			return;

		viewModel.BeginCodeUpdate();
		try
		{
			CodePane.Editor.Text = xml;
		}
		finally
		{
			viewModel.EndCodeUpdate();
		}
	}

	private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		viewModel.SaveWindowState(WindowState == WindowState.Maximized, Width, Height);
		if (_closeConfirmed || !viewModel.HasUnsavedChanges)
			return;

		e.Cancel = true;
		if (!await viewModel.ConfirmCloseAsync())
			return;

		_closeConfirmed = true;
		Close();
	}
}
