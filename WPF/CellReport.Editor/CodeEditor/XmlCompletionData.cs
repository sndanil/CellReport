using System;
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

		public XmlCompletionData(string text, string description, XmlCompletionDataType type)
		{
			_type = type;
			Text = text;
			Description = description;
		}

		public System.Windows.Media.ImageSource Image => null;

		public string Text { get; }

		public object Content => string.IsNullOrEmpty(Description?.ToString()) ? Text : Text + " (" + Description + ")";

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