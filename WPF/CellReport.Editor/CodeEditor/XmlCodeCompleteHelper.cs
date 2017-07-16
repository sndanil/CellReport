using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.CodeEditor.XHelper;
using CellReport.Editor.ViewModel;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Indentation;
using Orientation = System.Windows.Controls.Orientation;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace CellReport.Editor.CodeEditor
{
	public class XmlCodeCompleteHelper
	{
		public static List<XsdElementInformation> XsdInformation { get; set; }
		public static TextEditor TextEditor;
		public static DispatcherTimer ErrorsFoundTimer;
		public static CellManager TempManager;

		public static void ApplyTo(TextEditor textEditor)
		{
			TempManager = null;
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
			textEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
			var foldingManager = FoldingManager.Install(textEditor.TextArea);

			DispatcherTimer foldingUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
			foldingUpdateTimer.Tick += (sender, args) =>
			{
				foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
			};
			foldingUpdateTimer.Start();

			ErrorsFoundTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
			ErrorsFoundTimer.Tick += (sender, args) =>
			{
				if (EditorModel.IsCodeMode)
				{
					try
					{
						var doc = XDocument.Parse(TextEditor.Text);
						var wasErrorrs = false;
						doc.Validate(schemas, (o, eventArgs) =>
						{
							Console.WriteLine("{0}", eventArgs.Message);
						});
						if (!wasErrorrs)
						{
							var celltemplate = new CellTemplate();
							celltemplate.LoadFromXml(TextEditor.Text);
							TempManager = celltemplate.Manager;
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
			};
			ErrorsFoundTimer.Start();

			textEditor.TextChanged += TextEditorOnTextChanged;
			textEditor.TextArea.TextEntered += TextAreaOnTextEntered;
			textEditor.TextArea.KeyDown += TextAreaOnKeyDown;
			textEditor.TextArea.Caret.PositionChanged += TextEditorOnTextChanged;
		}

		static void TextAreaOnKeyDown(object sender, KeyEventArgs e)
		{
			var manager = TempManager ?? EditorModel.ReportManager;
			if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			{
				e.Handled = true;
				var currentElement = XmlParser.GetActiveElementStartPath(TextEditor.Text, TextEditor.CaretOffset);
				if (currentElement != null)
				{
					var attribute = XmlParser.GetActiveAttributeName(TextEditor.Text, TextEditor.CaretOffset);
					if (attribute != null)
					{
						var attributeXsd = GetAttributesByPath(currentElement)?.FirstOrDefault(a => a.Name == attribute);
						if (attributeXsd != null)
						{
							switch (attributeXsd.DataType.ToLower())
							{
								case "boolean":
									InvokeCompletionWindow(
										new List<Tuple<string, string>> { new Tuple<string, string>("true", string.Empty), new Tuple<string, string>("false", string.Empty) }, 
										XmlCompletionDataType.AttributeValue);
									return;
							}
						}
						var elementName = currentElement.Elements.Last().Name;
						switch (attribute)
						{
							case "Style":
								switch (elementName)
								{
									case "Cell":
										InvokeCompletionWindow(
											manager.GetStyles().OfType<CellStyle>()
												.Select(s => new Tuple<string, string>(s.ID, string.Empty)).ToList(), XmlCompletionDataType.AttributeValue);
										break;
									case "TextContent":
										InvokeCompletionWindow(
											manager.GetStyles().OfType<TextContentStyle>().Where(s => s.GetType() == typeof(TextContentStyle))
												.Select(s => new Tuple<string, string>(s.ID, s.Font.Family)).ToList(), XmlCompletionDataType.AttributeValue);
										break;
									case "DecimalContent":
										InvokeCompletionWindow(
											manager.GetStyles().OfType<DecimalContentStyle>()
												.Select(s => new Tuple<string, string>(s.ID, s.Font.Family)).ToList(), XmlCompletionDataType.AttributeValue);
										break;
									case "DateTimeContent":
										InvokeCompletionWindow(
											manager.GetStyles().OfType<DateTimeContentStyle>()
												.Select(s => new Tuple<string, string>(s.ID, s.Font.Family)).ToList(), XmlCompletionDataType.AttributeValue);
										break;
									case "Border":
										InvokeCompletionWindow(
											new List<Tuple<string, string>>
											{
												new Tuple<string, string>("None", string.Empty),
												new Tuple<string, string>("Solid", string.Empty),
												new Tuple<string, string>("Dash", string.Empty),
												new Tuple<string, string>("Dot", string.Empty),
												new Tuple<string, string>("DashDot", string.Empty),
												new Tuple<string, string>("DashDotDot", string.Empty)
											},
											XmlCompletionDataType.AttributeValue);
										break;
								}
								break;
							case "Position":
								switch (elementName)
								{
									case "Padding":
									case "Border":
										InvokeCompletionWindow(
											new List<Tuple<string, string>>
											{
												new Tuple<string, string>("All", string.Empty),
												new Tuple<string, string>("Top", string.Empty),
												new Tuple<string, string>("Left", string.Empty),
												new Tuple<string, string>("Right", string.Empty),
												new Tuple<string, string>("Bottom", string.Empty)
											},
											XmlCompletionDataType.AttributeValue);
										break;
								}
								break;
							case "Family":
								if (elementName == "Font")
								{
									InvokeCompletionWindow(
										Fonts.SystemFontFamilies.Select(f => new Tuple<string, string>(f.ToString(), string.Empty)).ToList(), 
										XmlCompletionDataType.AttributeValue,
										data =>
										{
											var panel = new StackPanel { Orientation = Orientation.Horizontal };
											panel.Children.Add(new TextBlock
											{
												Text = data.Text,
												FontFamily = new FontFamily(data.Text)
											});

											return panel;
										});
								}
								break;
							case "Horizontal":
								if (elementName == "Alignment")
								{
									InvokeCompletionWindow(
										new List<Tuple<string, string>> { new Tuple<string, string>("Left", string.Empty), new Tuple<string, string>("Center", string.Empty),
										new Tuple<string, string>("Right", string.Empty), new Tuple<string, string>("Justify", string.Empty) },
										XmlCompletionDataType.AttributeValue);
								}
								break;
							case "Vertical":
								if (elementName == "Alignment")
								{
									InvokeCompletionWindow(
										new List<Tuple<string, string>> { new Tuple<string, string>("Top", string.Empty), new Tuple<string, string>("Middle", string.Empty),
											new Tuple<string, string>("Center", string.Empty), new Tuple<string, string>("Bottom", string.Empty) },
										XmlCompletionDataType.AttributeValue);
								}
								break;
							case "Direction":
								switch (elementName)
								{
									case "TextContentStyle":
									case "DecimalContentStyle":
									case "DateTimeContentStyle":
										InvokeCompletionWindow(
											new List<Tuple<string, string>> { new Tuple<string, string>("LeftToRight", string.Empty), new Tuple<string, string>("TopToDown", string.Empty) },
											XmlCompletionDataType.AttributeValue);
										break;
								}
								break;
							case "Color":
								switch (elementName)
								{
									case "CellStyle":
									case "Border":
									case "TextContentStyle":
									case "DecimalContentStyle":
									case "DateTimeContentStyle":
									case "LineContentStyle":
										InvokeCompletionWindow(
											typeof(Colors).GetProperties(BindingFlags.Static| BindingFlags.Public)
											.Select(p => new Tuple<string, string>(p.Name, string.Empty)).ToList(), 
											XmlCompletionDataType.AttributeValue,
											data =>
											{
												var panel = new StackPanel { Orientation = Orientation.Horizontal };
												panel.Children.Add(new Rectangle
												{
													Width = 10,
													Height = 10,
													Fill = (SolidColorBrush)new BrushConverter().ConvertFromString(data.Text)
												});
												panel.Children.Add(new TextBlock
												{
													Text = data.Text,
													Margin = new Thickness(10, 0, 0, 0)
												});

												return panel;
											});
										break;
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
							var activeAttribute = XmlParser.GetActiveAttributeName(TextEditor.Text, TextEditor.CaretOffset);
							if (activeAttribute == null)
							{
								var currentElement = XmlParser.GetActiveElementStartPath(TextEditor.Text, TextEditor.CaretOffset);
								var attributeautocompletelist = ProvidePossibleAttributesAutocomplete(currentElement);
								InvokeCompletionWindow(attributeautocompletelist, XmlCompletionDataType.Attribute);
							}
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

		public static List<XsdElementInformation> GetElementsByPath(XmlElementInformation path)
		{
			if (path.IsEmpty)
			{
				var xsdResultForGivenElementPath = XsdInformation.FirstOrDefault(x => x.IsRoot);

				if (xsdResultForGivenElementPath != null)
				{
					return new List<XsdElementInformation> { xsdResultForGivenElementPath };
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
				return xsdResultForGivenElementPath?.Elements;
			}

			return null;
		}

		public static List<Tuple<string, string>> ProvidePossibleElementsAutocomplete(XmlElementInformation path)
		{
			return GetElementsByPath(path)?.Select(e => new Tuple<string, string>(e.Name, e.DataType)).ToList() ?? new List<Tuple<string, string>>();
		}

		public static List<XsdAttributeInformation> GetAttributesByPath(XmlElementInformation path)
		{
			if (path.IsEmpty)
			{
				var xsdResultForGivenElementPath = XsdInformation.FirstOrDefault(x => x.IsRoot);
				return xsdResultForGivenElementPath?.Attributes;
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
				return xsdResultForGivenElementPath?.Attributes;
			}
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

			var attributes = GetAttributesByPath(path);
			if (attributes != null)
			{
				foreach (var xsdInformationAttribute in attributes)
				{
					if (currentElement == null || currentElement.Attributes().All(a => a.Name != xsdInformationAttribute.Name))
						result.Add(new Tuple<string, string>(xsdInformationAttribute.Name, xsdInformationAttribute.DataType));
				}
			}

			return result;
		}

		private static void InvokeCompletionWindow(List<Tuple<string, string>> elementAutocompleteList, XmlCompletionDataType type, Func<XmlCompletionData, object> getContent = null)
		{
			var completionWindow = new CompletionWindow(TextEditor.TextArea) { Width = 300 };
			IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
			if (elementAutocompleteList.Any())
			{
				foreach (var autocompleteelement in elementAutocompleteList)
				{
					data.Add(new XmlCompletionData(autocompleteelement.Item1, autocompleteelement.Item2, type, getContent));
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
