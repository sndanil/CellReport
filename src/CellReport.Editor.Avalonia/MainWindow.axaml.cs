using Avalonia.Controls;
using Avalonia.Interactivity;
using CellReport.Avalonia;
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
		viewModel.ShowDesignerRequested += (_, _) => ShowDesigner();
		viewModel.CodeSearchRequested += (_, replace) => ShowCodeSearch(replace);
		viewModel.FindRowInCodeRequested += (_, _) => FindRowInCode();
		viewModel.ReadCode = () => CodePane.Editor.Text;
		viewModel.CodeTextInvalidated += (_, _) => ShowCurrentCode(viewModel);
		_codeEditor = new XmlCodeEditor(CodePane.Editor, () => viewModel.Manager, viewModel.CodeErrors);
		viewModel.CanUndoCode = () => _codeEditor.CanUndo;
		viewModel.CanRedoCode = () => _codeEditor.CanRedo;
		viewModel.UndoCode = () => _codeEditor.UndoChange();
		viewModel.RedoCode = () => _codeEditor.RedoChange();
		_codeEditor.HistoryChanged += (_, _) => viewModel.NotifyHistory();
		viewModel.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(MainWindowViewModel.InvertReport))
				ApplyReportInversion(viewModel);
		};
		ApplyReportInversion(viewModel);
		await viewModel.InitializeAsync(Program.StartupArgs, this);
		_modeReady = true;
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

	private async void OnModeChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (!_modeReady || _modeGuard || !ReferenceEquals(e.Source, ModeTabs))
			return;
		if (DataContext is not MainWindowViewModel viewModel)
			return;

		if (ReferenceEquals(ModeTabs.SelectedItem, CodeTab))
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
		ModeTabs.SelectedItem = CodeTab;
		_modeGuard = false;
		await ReportExportService.ShowMessageAsync(this, "Код шаблона не применён." + Environment.NewLine + error);
	}

	private void ShowCurrentCode(MainWindowViewModel viewModel)
	{
		var xml = viewModel.ExportCode();
		if (xml != null)
			CodePane.Editor.Text = xml;
	}
}
