using Avalonia.Controls;
using AvaloniaEdit;
using CellReport.Editor.Avalonia.ViewModels;
using System;

namespace CellReport.Editor.Avalonia.Views;

public partial class CodeView : UserControl
{
	public CodeView()
	{
		InitializeComponent();
	}

	public TextEditor Editor => CodeEditor;

	private void OnErrorSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (!ReferenceEquals(e.Source, ErrorList) || ErrorList.SelectedItem is not SchemaError error || error.LineNumber <= 0)
			return;

		Editor.Focus();
		Editor.TextArea.Caret.Line = error.LineNumber;
		Editor.TextArea.Caret.Column = Math.Max(1, error.LinePosition);
		Editor.ScrollToLine(error.LineNumber);
	}
}
