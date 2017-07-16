using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace CellReport.Editor.CodeEditor
{
	public enum XmlCompletionDataType
	{
		Element,
		Attribute,
		AttributeValue
	}

	public class XmlCompletionData : ICompletionData
	{
		private readonly XmlCompletionDataType _type;
		private readonly Func<XmlCompletionData, object> _getContentFunc;

		public XmlCompletionData(string text, string description, XmlCompletionDataType type, Func<XmlCompletionData, object> getContent)
		{
			_type = type;
			Text = text;
			Description = description;
			_getContentFunc = getContent;
		}

		public System.Windows.Media.ImageSource Image => null;

		public string Text { get; }

		//public object Content => string.IsNullOrEmpty(Description?.ToString()) ? Text : Text + " (" + Description + ")";
		public object Content
		{
			get
			{
				if (_getContentFunc == null)
				{
					var panel = new StackPanel {Orientation = Orientation.Horizontal};
					panel.Children.Add(new TextBlock {Text = Text});
					panel.Children.Add(new TextBlock
					{
						Text = Description?.ToString(),
						Foreground = Brushes.Gray,
						Margin = new Thickness(10, 0, 0, 0)
					});

					return panel;
				}

				return _getContentFunc(this);
			}
		}

		public object Description { get; }

		public double Priority { get; }

		public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			switch (_type)
			{
				case XmlCompletionDataType.Element:
					string element = Text + "></" + Text + ">";
					textArea.Document.Replace(completionSegment, element);
					textArea.Caret.Offset = textArea.Caret.Offset - (1 + Text.Length + 2);
					break;
				case XmlCompletionDataType.Attribute:
					textArea.Document.Replace(completionSegment, Text + "=\"\"");
					textArea.Caret.Offset = textArea.Caret.Offset - 1;
					break;
				case XmlCompletionDataType.AttributeValue:
					var start = completionSegment.Offset;
					var end = completionSegment.EndOffset;
					if (textArea.Document.Text[start] == '"')
					{
						start--;
						end--;
					}
					while (textArea.Document.Text[start] != '"')
					{
						start--;
					}
					start++;
					while (textArea.Document.Text[end] != '"')
					{
						end++;
					}
					textArea.Document.Replace(new AnchorSegment(textArea.Document, start, end - start), Text);
					break;
			}
		}
	}
}