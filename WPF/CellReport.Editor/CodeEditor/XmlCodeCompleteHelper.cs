using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.CodeEditor.XHelper;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;

namespace CellReport.Editor.CodeEditor
{
	public class XmlCodeCompleteHelper
	{
		public static List<XsdElementInformation> XsdInformation { get; set; }
		public static TextEditor TextEditor;

		public static void ApplyTo(TextEditor textEditor)
		{
			TextEditor = textEditor;
			TextEditor.ShowLineNumbers = true;
			TextEditor.Options.HighlightCurrentLine = true;
			TextEditor.Options.IndentationSize = 2;

			var schemaFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Schema.xsd");

			XmlSchemaSet schemas = new XmlSchemaSet();
			schemas.Add("", XmlReader.Create(new StringReader(File.ReadAllText(schemaFileName))));
			schemas.Compile();
			XsdInformation = XsdParser.AnalyseSchema(schemas);

			var foldingStrategy = new XmlFoldingStrategy();
			textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
			var foldingManager = FoldingManager.Install(textEditor.TextArea);

			DispatcherTimer foldingUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
			foldingUpdateTimer.Tick += (sender, args) =>
			{
				foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
			};
			foldingUpdateTimer.Start();

			textEditor.TextChanged += TextEditorOnTextChanged;
			textEditor.TextArea.TextEntered += TextAreaOnTextEntered;
			textEditor.TextArea.KeyDown += TextAreaOnKeyDown;
			textEditor.TextArea.Caret.PositionChanged += TextEditorOnTextChanged;
		}

		static void TextAreaOnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			{
				e.Handled = true;
				var currentElement = XmlParser.GetActiveElementStartPath(TextEditor.Text, TextEditor.CaretOffset);
				if (currentElement != null)
				{
					var attribute = XmlParser.GetActiveAttributeName(TextEditor.Text, TextEditor.CaretOffset);
					if (attribute != null)
					{
						var elementName = currentElement.Elements.Last().Name;
						switch (attribute)
						{
							case "Style":
								switch (elementName)
								{
									case "Cell":
										InvokeCompletionWindow(
											ViewModel.EditorModel.ReportManager.GetStyles().OfType<CellStyle>()
												.Select(s => new Tuple<string, string>(s.ID, string.Empty)).ToList(), XmlCompletionDataType.AttributeValue);
										break;
									case "TextContent":
										InvokeCompletionWindow(
											ViewModel.EditorModel.ReportManager.GetStyles().OfType<TextContentStyle>().Where(s => s.GetType() == typeof(TextContentStyle))
												.Select(s => new Tuple<string, string>(s.ID, s.Font.Family)).ToList(), XmlCompletionDataType.AttributeValue);
										break;
									case "DecimalContent":
										InvokeCompletionWindow(
											ViewModel.EditorModel.ReportManager.GetStyles().OfType<DecimalContentStyle>()
												.Select(s => new Tuple<string, string>(s.ID, s.Font.Family)).ToList(), XmlCompletionDataType.AttributeValue);
										break;
									case "DateTimeContent":
										InvokeCompletionWindow(
											ViewModel.EditorModel.ReportManager.GetStyles().OfType<DateTimeContentStyle>()
												.Select(s => new Tuple<string, string>(s.ID, s.Font.Family)).ToList(), XmlCompletionDataType.AttributeValue);
										break;
								}
								break;
							case "Family":
								if (elementName == "Font")
								{
									InvokeCompletionWindow(
										Fonts.SystemFontFamilies.Select(f => new Tuple<string, string>(f.ToString(), string.Empty)).ToList(), XmlCompletionDataType.AttributeValue);
								}
								break;
						}
					}
				}
			}
		}

		private static void TextAreaOnTextEntered(object sender, TextCompositionEventArgs textCompositionEventArgs)
		{
			try
			{
				switch (textCompositionEventArgs.Text)
				{
					case ">":
						{
							//auto-insert closing element
							int offset = TextEditor.CaretOffset;
							string s = XmlParser.GetElementAtCursor(TextEditor.Text, offset - 1);
							if (!string.IsNullOrWhiteSpace(s) && "!--" != s)
							{
								if (!XmlParser.IsClosingElement(TextEditor.Text, offset - 1, s))
								{
									string endElement = "</" + s + ">";
									var rightOfCursor = TextEditor.Text.Substring(offset, Math.Max(0, Math.Min(endElement.Length + 50, TextEditor.Text.Length) - offset - 1)).TrimStart();
									if (!rightOfCursor.StartsWith(endElement))
									{
										TextEditor.TextArea.Document.Insert(offset, endElement);
										TextEditor.CaretOffset = offset;
									}
								}
							}
							break;
						}
					case "/":
						{
							int offset = TextEditor.CaretOffset;
							if (TextEditor.Text.Length > offset + 2 && TextEditor.Text[offset] == '>')
							{
								//remove closing tag if exist
								string s = XmlParser.GetElementAtCursor(TextEditor.Text, offset - 1);
								if (!string.IsNullOrWhiteSpace(s))
								{
									//search closing end tag. Element must be empty (whitespace allowed)  
									//"<hallo>  </hallo>" --> enter '/' --> "<hallo/>  "
									string expectedEndTag = "</" + s + ">";
									for (int i = offset + 1; i < TextEditor.Text.Length - expectedEndTag.Length + 1; i++)
									{
										if (!char.IsWhiteSpace(TextEditor.Text[i]))
										{
											if (TextEditor.Text.Substring(i, expectedEndTag.Length) == expectedEndTag)
											{
												//remove already existing endTag
												TextEditor.Document.Remove(i, expectedEndTag.Length);
											}
											break;
										}
									}
								}
							}
							break;
						}
					case "<":
						var parentElement = XmlParser.GetParentElementPath(TextEditor.Text);
						var elementAutocompleteList = ProvidePossibleElementsAutocomplete(parentElement);

						InvokeCompletionWindow(elementAutocompleteList, XmlCompletionDataType.Element);

						break;
					case " ":
						{
							var currentElement = XmlParser.GetActiveElementStartPath(TextEditor.Text, TextEditor.CaretOffset);
							var attributeautocompletelist = ProvidePossibleAttributesAutocomplete(currentElement);
							InvokeCompletionWindow(attributeautocompletelist, XmlCompletionDataType.Attribute);
							break;
						}

				}
			}
			catch (Exception ex)
			{

			}

			if (textCompositionEventArgs.Text.Length > 0)
			{
				char symbol = textCompositionEventArgs.Text[0];
				if (!(char.IsLetterOrDigit(symbol) || char.IsPunctuation(symbol)))
				{
					textCompositionEventArgs.Handled = true;
				}
			}
		}

		public static List<Tuple<string, string>> ProvidePossibleElementsAutocomplete(XmlElementInformation path)
		{
			List<Tuple<string, string>> result = new List<Tuple<string, string>>();

			if (path.IsEmpty)
			{
				var xsdResultForGivenElementPath = XsdInformation.FirstOrDefault(x => x.IsRoot);

				if (xsdResultForGivenElementPath != null)
				{
					result.Add(new Tuple<string, string>(xsdResultForGivenElementPath.Name, xsdResultForGivenElementPath.DataType));
				}
			}
			else
			{
				StringBuilder xpath = new StringBuilder();
				xpath.Append("/");
				foreach (var element in path.Elements)
				{
					xpath.Append("/" + element.Name);
				}

				var xsdResultForGivenElementPath = XsdInformation.FirstOrDefault(x => x.XPathLikeKey.ToLowerInvariant() == xpath.ToString().ToLowerInvariant());

				if (xsdResultForGivenElementPath != null)
				{
					foreach (var xsdInformationElement in xsdResultForGivenElementPath.Elements)
					{
						result.Add(new Tuple<string, string>(xsdInformationElement.Name, xsdInformationElement.DataType));
					}
				}
			}


			return result;
		}

		public static List<Tuple<string, string>> ProvidePossibleAttributesAutocomplete(XmlElementInformation path)
		{
			List<Tuple<string, string>> result = new List<Tuple<string, string>>();

			XElement currentElement = null;
			var currentElementText = XmlParser.GetActiveElementText(TextEditor.Text, TextEditor.CaretOffset);
			if (currentElementText != null)
			{
				if (currentElementText.EndsWith("/"))
					currentElementText += ">";
				else
					currentElementText += "/>";
				currentElement = XElement.Parse(currentElementText);
			}

			XsdElementInformation xsdResultForGivenElementPath = null;
			if (path.IsEmpty)
			{
				xsdResultForGivenElementPath = XsdInformation.FirstOrDefault(x => x.IsRoot);
			}
			else
			{
				StringBuilder xpath = new StringBuilder();
				xpath.Append("/");
				foreach (var element in path.Elements)
				{
					xpath.Append("/" + element.Name);
				}

				xsdResultForGivenElementPath = XsdInformation.FirstOrDefault(x => x.XPathLikeKey.ToLowerInvariant() == xpath.ToString().ToLowerInvariant());
			}

			if (xsdResultForGivenElementPath != null)
			{
				foreach (var xsdInformationAttribute in xsdResultForGivenElementPath.Attributes)
				{
					if (currentElement == null || currentElement.Attributes().All(a => a.Name != xsdInformationAttribute.Name))
						result.Add(new Tuple<string, string>(xsdInformationAttribute.Name, xsdInformationAttribute.DataType));
				}
			}

			return result;
		}

		private static void InvokeCompletionWindow(List<Tuple<string, string>> elementAutocompleteList, XmlCompletionDataType type)
		{
			var completionWindow = new CompletionWindow(TextEditor.TextArea) { Width = 300 };
			IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
			if (elementAutocompleteList.Any())
			{
				foreach (var autocompleteelement in elementAutocompleteList)
				{
					data.Add(new XmlCompletionData(autocompleteelement.Item1, autocompleteelement.Item2, type));
				}
				completionWindow.Show();
				completionWindow.Closed += delegate { completionWindow = null; };
			}
		}

		private static void TextEditorOnTextChanged(object sender, EventArgs eventArgs)
		{

		}
	}
}
