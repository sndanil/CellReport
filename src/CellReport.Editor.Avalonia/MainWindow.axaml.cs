using Avalonia.Controls;
using Avalonia.Interactivity;
using CellReport.Avalonia.Services;
using CellReport.Editor.Avalonia.CodeEditor;
using CellReport.Editor.Avalonia.ViewModels;
using System;

namespace CellReport.Editor.Avalonia;

public partial class MainWindow : Window
{
	private XmlCodeEditor? _codeEditor;
	private bool _modeReady;
	private bool _modeGuard;

	public MainWindow()
	{
		InitializeComponent();
		Opened += OnMainWindowOpened;
	}

	private async void OnMainWindowOpened(object? sender, EventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		viewModel.ExitRequested += (_, _) => Close();
		viewModel.ReadCode = () => CodeEditor.Text;
		viewModel.CodeTextInvalidated += (_, _) => ShowCurrentCode(viewModel);
		_codeEditor = new XmlCodeEditor(CodeEditor, () => viewModel.Manager, viewModel.CodeErrors);
		await viewModel.InitializeAsync(Program.StartupArgs, this);
		_modeReady = true;
	}

	private async void OnModeChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (!_modeReady || _modeGuard || !ReferenceEquals(e.Source, ModeTabs))
			return;
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		if (ModeTabs.SelectedIndex == 1)
		{
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
		ModeTabs.SelectedIndex = 1;
		_modeGuard = false;
		await ReportExportService.ShowMessageAsync(this, "Код шаблона не применён." + Environment.NewLine + error);
	}

	private void OnErrorSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (!ReferenceEquals(e.Source, ErrorList) || ErrorList.SelectedItem is not SchemaError error || error.LineNumber <= 0)
			return;

		CodeEditor.Focus();
		CodeEditor.TextArea.Caret.Line = error.LineNumber;
		CodeEditor.TextArea.Caret.Column = Math.Max(1, error.LinePosition);
		CodeEditor.ScrollToLine(error.LineNumber);
	}

	private void ShowCurrentCode(MainWindowViewModel viewModel)
	{
		var xml = viewModel.ExportCode();
		if (xml != null)
			CodeEditor.Text = xml;
	}
}
