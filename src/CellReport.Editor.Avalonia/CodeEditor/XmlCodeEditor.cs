using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Indentation;
using AvaloniaEdit.Search;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.Avalonia.CodeEditor.XHelper;
using CellReport.Editor.Avalonia.ViewModels;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace CellReport.Editor.Avalonia.CodeEditor;

public sealed class XmlCodeEditor
{
	private static readonly object SearchPanelButtonMarker = new();

	private readonly TextEditor _editor;
	private readonly Func<CellManager?> _designManager;
	private readonly ObservableCollection<SchemaError> _errors;
	private readonly XmlSchemaSet _schemas;
	private readonly XmlFoldingStrategy _foldingStrategy = new();
	private readonly FoldingManager _foldingManager;
	private readonly SearchPanel _searchPanel;
	private bool _active;
	private CellManager? _parsedManager;

	public event EventHandler? HistoryChanged;

	public XmlCodeEditor(TextEditor editor, Func<CellManager?> designManager, ObservableCollection<SchemaError> errors)
	{
		_editor = editor;
		_designManager = designManager;
		_errors = errors;

		editor.ShowLineNumbers = true;
		editor.Options.HighlightCurrentLine = true;
		editor.Options.IndentationSize = 2;
		editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
		editor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
		_searchPanel = SearchPanel.Install(editor);

		var schemaPath = Path.Combine(AppContext.BaseDirectory, "Schema.xsd");
		_schemas = new XmlSchemaSet();
		_schemas.Add("", XmlReader.Create(new StringReader(File.ReadAllText(schemaPath))));
		_schemas.Compile();
		XmlCodeModel.Information = XsdParser.AnalyseSchema(_schemas);

		_foldingManager = FoldingManager.Install(editor.TextArea);
		var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
		timer.Tick += (_, _) => Refresh();
		timer.Start();

		editor.TextArea.TextEntered += OnTextEntered;
		editor.TextArea.KeyDown += OnKeyDown;
		editor.TextChanged += (_, _) => HistoryChanged?.Invoke(this, EventArgs.Empty);
		editor.ActualThemeVariantChanged += (_, _) => ApplyThemeColors();
		ApplyThemeColors();
	}

	private static readonly Dictionary<string, string> DarkHighlightColors = new()
	{
		["Comment"] = "#6A9955",
		["CData"] = "#569CD6",
		["DocType"] = "#569CD6",
		["XmlDeclaration"] = "#569CD6",
		["XmlTag"] = "#569CD6",
		["AttributeName"] = "#9CDCFE",
		["AttributeValue"] = "#CE9178",
		["Entity"] = "#4EC9B0",
		["BrokenEntity"] = "#DCDCAA"
	};

	private readonly Dictionary<string, HighlightingBrush?> _highlightBrushes = new();

	private void ApplyThemeColors()
	{
		var dark = _editor.ActualThemeVariant == ThemeVariant.Dark;
		var foreground = dark ? Color.Parse("#D4D4D4") : Color.Parse("#1A1A1A");
		var background = dark ? Color.Parse("#1E1E1E") : Colors.White;
		_editor.Foreground = new SolidColorBrush(foreground);
		_editor.Background = new SolidColorBrush(background);
		_editor.TextArea.Foreground = _editor.Foreground;

		var definition = _editor.SyntaxHighlighting;
		if (definition == null)
			return;

		foreach (var color in definition.NamedHighlightingColors)
		{
			if (!_highlightBrushes.ContainsKey(color.Name))
				_highlightBrushes[color.Name] = color.Foreground;

			if (!dark)
			{
				color.Foreground = _highlightBrushes[color.Name];
				continue;
			}

			if (DarkHighlightColors.TryGetValue(color.Name, out var hex))
				color.Foreground = new SimpleHighlightingBrush(Color.Parse(hex));
		}
	}

	public void ShowSearch(bool replace)
	{
		var selection = _editor.TextArea.Selection;
		if (selection != null && !selection.IsEmpty && !selection.IsMultiline)
		{
			var selected = selection.GetText();
			if (!string.IsNullOrEmpty(selected))
				_searchPanel.SearchPattern = selected;
		}

		_searchPanel.IsReplaceMode = replace;
		if (!_searchPanel.IsOpened)
			_searchPanel.Open();

		// Шаблон панели создаётся при первом открытии, поэтому фокус в поле поиска
		// ставим после раскладки. От этого зависит CanExecute у кнопок «найти/закрыть».
		Dispatcher.UIThread.Post(() =>
		{
			if (!_searchPanel.IsOpened)
				return;

			WireSearchPanelButtons();
			_searchPanel.Reactivate();
		}, DispatcherPriority.Background);
	}

	/// <summary>
	/// В AvaloniaEdit команды поиска не поднимают CanExecuteChanged, поэтому кнопки панели
	/// навсегда остаются выключенными. Привязываем их напрямую к методам панели.
	/// </summary>
	private void WireSearchPanelButtons()
	{
		foreach (var button in _searchPanel.GetVisualDescendants().OfType<Button>())
		{
			if (ReferenceEquals(button.Tag, SearchPanelButtonMarker))
				continue;

			var command = button.Command;

			if (ReferenceEquals(command, SearchCommands.FindPrevious))
				button.Click += (_, _) => _searchPanel.FindPrevious();
			else if (ReferenceEquals(command, SearchCommands.FindNext))
				button.Click += (_, _) => _searchPanel.FindNext();
			else if (ReferenceEquals(command, SearchCommands.CloseSearchPanel))
				button.Click += (_, _) => _searchPanel.Close();
			else if (ReferenceEquals(command, SearchCommands.ReplaceNext))
				button.Click += (_, _) => _searchPanel.ReplaceNext();
			else if (ReferenceEquals(command, SearchCommands.ReplaceAll))
				button.Click += (_, _) => _searchPanel.ReplaceAll();
			else
				continue;

			button.Command = null;
			button.IsEnabled = true;
			button.Tag = SearchPanelButtonMarker;
		}
	}

	public void SelectTemplateRow(int row)
	{
		var document = _editor.Document;
		var rowIndex = -1;
		var selectStartLine = 0;
		var selectEndLine = 0;
		for (var lineNumber = 1; lineNumber <= document.LineCount; lineNumber++)
		{
			var line = document.GetLineByNumber(lineNumber);
			var text = document.GetText(line).Trim();
			if (text.Contains("<Row") && rowIndex != row && selectStartLine == 0)
			{
				rowIndex++;
				try
				{
					var rowElement = XElement.Parse(text.EndsWith("/>") ? text : text + "</Row>");
					var index = rowElement.Attribute("Index");
					if (index != null)
						rowIndex = Convert.ToInt32(index.Value);
				}
				catch (XmlException)
				{
				}
			}

			if (rowIndex == row && selectStartLine == 0)
				selectStartLine = lineNumber;

			if ((text.Contains("</Row") || (text.Contains("<Row") && text.EndsWith("/>"))) && selectStartLine != 0)
			{
				selectEndLine = lineNumber;
				break;
			}
		}

		if (selectStartLine == 0)
			return;

		if (selectEndLine == 0)
			selectEndLine = selectStartLine;

		var start = document.GetLineByNumber(selectStartLine);
		var end = document.GetLineByNumber(selectEndLine);
		_editor.Select(start.Offset, end.EndOffset - start.Offset);
		_editor.ScrollToLine(selectStartLine);
		_editor.Focus();
	}

	public bool CanUndo => _editor.Document?.UndoStack.CanUndo == true;

	public bool CanRedo => _editor.Document?.UndoStack.CanRedo == true;

	public void UndoChange()
	{
		if (CanUndo)
			_editor.Document.UndoStack.Undo();
	}

	public void RedoChange()
	{
		if (CanRedo)
			_editor.Document.UndoStack.Redo();
	}

	public void SetActive(bool active)
	{
		_active = active;
		if (active)
		{
			ApplyThemeColors();
			Refresh();
		}
	}

	private void Refresh()
	{
		if (!_active)
			return;

		_foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);
		Validate();
	}

	private void Validate()
	{
		_errors.Clear();
		try
		{
			var document = XDocument.Parse(_editor.Text);
			var schemaErrors = new List<SchemaError>();
			document.Validate(_schemas, (_, args) =>
			{
				schemaErrors.Add(new SchemaError
				{
					Message = args.Message,
					LineNumber = args.Exception?.LineNumber ?? 0,
					LinePosition = args.Exception?.LinePosition ?? 0
				});
			});

			foreach (var error in schemaErrors)
				_errors.Add(error);

			if (schemaErrors.Count == 0)
			{
				var template = new CellTemplate();
				template.LoadFromXml(_editor.Text);
				_parsedManager = template.Manager;
			}
		}
		catch (Exception ex)
		{
			_errors.Add(new SchemaError { Message = ex.Message });
		}
	}

	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Space || !e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
			return;

		e.Handled = true;
		var attribute = XmlParser.GetActiveAttributeName(_editor.Text, _editor.CaretOffset);
		if (attribute != null)
		{
			CompleteAttributeValue(attribute, insideValue: true);
			return;
		}

		if (!string.IsNullOrEmpty(XmlParser.GetActiveElementText(_editor.Text, _editor.CaretOffset)))
		{
			var currentElement = XmlParser.GetActiveElementStartPath(_editor.Text, _editor.CaretOffset);
			Complete(XmlCodeModel.Attributes(_editor.Text, _editor.CaretOffset, currentElement), XmlCompletionDataType.Attribute);
			return;
		}

		Complete(XmlCodeModel.Elements(XmlParser.GetParentElementPath(_editor.Text)), XmlCompletionDataType.Element);
	}

	private void OnTextEntered(object? sender, TextInputEventArgs e)
	{
		try
		{
			switch (e.Text)
			{
				case ">":
					CloseElement();
					break;
				case "/":
					RemoveExistingEndTag();
					break;
				case "<":
					Complete(XmlCodeModel.Elements(XmlParser.GetParentElementPath(_editor.Text)), XmlCompletionDataType.Element);
					break;
				case "\"":
					var quotedAttribute = XmlParser.GetActiveAttributeName(_editor.Text, _editor.CaretOffset);
					if (quotedAttribute != null)
						CompleteAttributeValue(quotedAttribute, insideValue: true);
					break;
				case " ":
					var activeAttribute = XmlParser.GetActiveAttributeName(_editor.Text, _editor.CaretOffset);
					if (activeAttribute == null)
					{
						var currentElement = XmlParser.GetActiveElementStartPath(_editor.Text, _editor.CaretOffset);
						Complete(XmlCodeModel.Attributes(_editor.Text, _editor.CaretOffset, currentElement), XmlCompletionDataType.Attribute);
					}
					break;
			}
		}
		catch (Exception)
		{
		}
	}

	private void CloseElement()
	{
		var offset = _editor.CaretOffset;
		var name = XmlParser.GetElementAtCursor(_editor.Text, offset - 1);
		if (string.IsNullOrWhiteSpace(name) || name == "!--")
			return;
		if (XmlParser.IsClosingElement(_editor.Text, offset - 1, name))
			return;

		var endElement = "</" + name + ">";
		var rightLength = Math.Max(0, Math.Min(endElement.Length + 50, _editor.Text.Length) - offset);
		var rightOfCursor = rightLength == 0 ? string.Empty : _editor.Text.Substring(offset, rightLength).TrimStart();
		if (rightOfCursor.StartsWith(endElement, StringComparison.Ordinal))
			return;

		_editor.Document.Insert(offset, endElement);
		_editor.CaretOffset = offset;
	}

	private void RemoveExistingEndTag()
	{
		var offset = _editor.CaretOffset;
		if (_editor.Text.Length <= offset || _editor.Text[offset] != '>')
			return;

		var name = XmlParser.GetElementAtCursor(_editor.Text, offset - 1);
		if (string.IsNullOrWhiteSpace(name))
			return;

		var expectedEndTag = "</" + name + ">";
		for (var i = offset + 1; i < _editor.Text.Length - expectedEndTag.Length + 1; i++)
		{
			if (char.IsWhiteSpace(_editor.Text[i]))
				continue;

			if (_editor.Text.Substring(i, expectedEndTag.Length) == expectedEndTag)
				_editor.Document.Remove(i, expectedEndTag.Length);
			break;
		}
	}

	private void CompleteAttributeValue(string attribute, bool insideValue)
	{
		var manager = _parsedManager ?? _designManager();
		var currentElement = XmlParser.GetActiveElementStartPath(_editor.Text, _editor.CaretOffset);
		if (manager == null || currentElement.Elements.Count == 0)
			return;

		var attributeInfo = XmlCodeModel.GetAttributes(currentElement)?.FirstOrDefault(item => item.Name == attribute);
		if (attributeInfo?.DataType != null && attributeInfo.DataType.Equals("boolean", StringComparison.OrdinalIgnoreCase))
		{
			Complete(Values("true", "false"), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
			return;
		}

		var elementName = currentElement.Elements.Last().Name;
		var parentElement = currentElement.Elements.Count >= 2
			? currentElement.Elements[currentElement.Elements.Count - 2].Name
			: null;
		switch (attribute)
		{
			case "Style":
				CompleteStyle(manager, elementName, insideValue);
				break;
			case "Position" when elementName is "Padding" or "Border":
				Complete(Values("All", "Top", "Left", "Right", "Bottom"), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "Family" when elementName == "Font":
				Complete(FontNames(), XmlCompletionDataType.AttributeValue, FontContent, insideValue);
				break;
			case "Horizontal" when elementName == "Alignment":
				Complete(Values("Left", "Center", "Right", "Justify"), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "Vertical" when elementName == "Alignment":
				Complete(Values("Top", "Middle", "Center", "Bottom"), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "Direction" when elementName is "TextContentStyle" or "DecimalContentStyle" or "DateTimeContentStyle":
				Complete(Values("LeftToRight", "TopToDown"), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "Color" when elementName is "CellStyle" or "Border" or "TextContentStyle" or "DecimalContentStyle" or "DateTimeContentStyle" or "LineContentStyle":
				Complete(ColorNames(), XmlCompletionDataType.AttributeValue, ColorContent, insideValue);
				break;
			case "Format" when elementName == "Param":
				Complete(Values(
					"currencyString", "currencyString|RUR", "currencyString|UAH", "currencyString|KZT",
					"currencyString|TJS", "currencyString|USD", "currencyString|EUR", "numberString",
					"#0", "00", "#,##0.000", "#,##0.00", "#,##0.0",
					"dd.MM.yyyy", "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss"), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "Target" when elementName == "Param":
				Complete(TargetValues(parentElement), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "Type" when elementName == "Param":
				Complete(Values("DateTime", "Decimal"), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "Expression" when elementName == "Param":
				Complete(manager.GetExpressions().Select(item => Pair(item.ID, item.GetType().Name)), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
		}
	}

	private void CompleteStyle(CellManager manager, string elementName, bool insideValue)
	{
		switch (elementName)
		{
			case "Cell":
				Complete(manager.GetStyles().OfType<CellStyle>().Select(style => Pair(style.ID, string.Empty)), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "TextContent":
				Complete(manager.GetStyles().OfType<TextContentStyle>()
					.Where(style => style.GetType() == typeof(TextContentStyle))
					.Select(style => Pair(style.ID, style.Font?.Family)), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "DecimalContent":
				Complete(manager.GetStyles().OfType<DecimalContentStyle>().Select(style => Pair(style.ID, style.Font?.Family)), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "DateTimeContent":
				Complete(manager.GetStyles().OfType<DateTimeContentStyle>().Select(style => Pair(style.ID, style.Font?.Family)), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
			case "Border":
				Complete(Values("None", "Solid", "Dash", "Dot", "DashDot", "DashDotDot"), XmlCompletionDataType.AttributeValue, valueStart: insideValue);
				break;
		}
	}

	private static IEnumerable<(string Text, string Description)> TargetValues(string? parent)
	{
		var common = new List<(string, string)>
		{
			Pair("Style.Color"),
			Pair("Style.Font.Family"),
			Pair("Style.Font.Size"),
			Pair("Style.Font.Style"),
			Pair("Style.HAlignment"),
			Pair("Style.VAlignment"),
			Pair("Style.Direction"),
			Pair("Style.Angle")
		};

		switch (parent)
		{
			case "TextContent":
				common.Insert(0, Pair("Text"));
				return common;
			case "DecimalContent":
				common.Insert(0, Pair("Value"));
				common.Add(Pair("Style.Format"));
				common.Add(Pair("Style.Culture"));
				common.Add(Pair("Style.NegativeColor"));
				return common;
			case "DateTimeContent":
				common.Insert(0, Pair("Value"));
				common.Add(Pair("Style.Format"));
				common.Add(Pair("Style.Culture"));
				return common;
			case "BarCodeContent":
				return Values("Type", "Code");
			case "ImageContent":
				return Values("Uri");
			default:
				return common;
		}
	}

	private void Complete(IEnumerable<(string Text, string Description)> items, XmlCompletionDataType type, Func<string, object>? content = null, bool valueStart = false)
	{
		var list = items.Where(item => !string.IsNullOrEmpty(item.Text)).ToList();
		if (list.Count == 0)
			return;

		var window = new CompletionWindow(_editor.TextArea)
		{
			Width = 320,
			CloseWhenCaretAtBeginning = false
		};
		if (valueStart)
			window.StartOffset = AttributeValueStart();

		var dark = _editor.ActualThemeVariant == ThemeVariant.Dark;
		window.CompletionList.Background = new SolidColorBrush(dark ? Color.Parse("#252526") : Colors.White);
		window.CompletionList.Foreground = new SolidColorBrush(dark ? Color.Parse("#D4D4D4") : Color.Parse("#1A1A1A"));
		foreach (var item in list)
			window.CompletionList.CompletionData.Add(new XmlCompletionData(item.Text, item.Description, type, content?.Invoke(item.Text)));
		window.Show();
	}

	private int AttributeValueStart()
	{
		var text = _editor.Text;
		var start = _editor.CaretOffset;
		while (start > 0 && text[start - 1] != '"')
			start--;

		return start;
	}

	private static IEnumerable<(string Text, string Description)> Values(params string[] values)
	{
		foreach (var value in values)
			yield return Pair(value);
	}

	private static (string Text, string Description) Pair(string? text, string? description = null)
	{
		return (text ?? string.Empty, description ?? string.Empty);
	}

	private static IEnumerable<(string Text, string Description)> FontNames()
	{
		foreach (var name in SKFontManager.Default.FontFamilies.OrderBy(item => item, StringComparer.CurrentCultureIgnoreCase))
			yield return Pair(name);
	}

	private static IEnumerable<(string Text, string Description)> ColorNames()
	{
		foreach (var property in typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static))
		{
			if (property.PropertyType == typeof(Color))
				yield return Pair(property.Name);
		}
	}

	private static object FontContent(string name)
	{
		return new TextBlock
		{
			Text = name,
			FontFamily = new FontFamily(name)
		};
	}

	private static object ColorContent(string name)
	{
		var panel = new StackPanel
		{
			Orientation = global::Avalonia.Layout.Orientation.Horizontal,
			Spacing = 8
		};
		var swatch = new global::Avalonia.Controls.Border
		{
			Width = 12,
			Height = 12,
			VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
		};
		var property = typeof(Colors).GetProperty(name);
		if (property?.GetValue(null) is Color color)
			swatch.Background = new SolidColorBrush(color);
		panel.Children.Add(swatch);
		panel.Children.Add(new TextBlock { Text = name, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center });
		return panel;
	}
}

internal static class XmlCodeModel
{
	public static List<XsdElementInformation> Information { get; set; } = new();

	public static List<(string Text, string Description)> Elements(XmlElementInformation? path)
	{
		return GetElements(path)?.Select(item => (item.Name ?? string.Empty, item.DataType ?? string.Empty)).ToList()
			?? new List<(string, string)>();
	}

	public static List<(string Text, string Description)> Attributes(string text, int offset, XmlElementInformation? path)
	{
		var result = new List<(string, string)>();
		XElement? current = null;
		var currentText = XmlParser.GetActiveElementText(text, offset);
		if (currentText != null)
		{
			currentText = currentText.EndsWith("/", StringComparison.Ordinal) ? currentText + ">" : currentText + "/>";
			current = XElement.Parse(currentText);
		}

		var attributes = GetAttributes(path);
		if (attributes == null)
			return result;

		foreach (var attribute in attributes)
		{
			if (current == null || current.Attributes().All(item => item.Name != attribute.Name))
				result.Add((attribute.Name ?? string.Empty, attribute.DataType ?? string.Empty));
		}

		return result;
	}

	public static List<XsdAttributeInformation>? GetAttributes(XmlElementInformation? path)
	{
		if (path == null)
			return null;

		if (path.IsEmpty)
			return Information.FirstOrDefault(item => item.IsRoot)?.Attributes;

		return Find(path)?.Attributes;
	}

	private static List<XsdElementInformation>? GetElements(XmlElementInformation? path)
	{
		if (path == null)
			return null;

		if (path.IsEmpty)
		{
			var root = Information.FirstOrDefault(item => item.IsRoot);
			return root == null ? null : new List<XsdElementInformation> { root };
		}

		return Find(path)?.Elements;
	}

	private static XsdElementInformation? Find(XmlElementInformation path)
	{
		var xpath = new StringBuilder("/");
		foreach (var element in path.Elements)
			xpath.Append('/').Append(element.Name);

		var key = xpath.ToString();
		return Information.FirstOrDefault(item => string.Equals(item.XPathLikeKey, key, StringComparison.OrdinalIgnoreCase));
	}
}
