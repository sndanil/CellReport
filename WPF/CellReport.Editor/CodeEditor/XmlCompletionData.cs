using System;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace CellReport.Editor.CodeEditor
{
	public class XmlCompletionData : ICompletionData
	{
		private readonly bool _isAttribute;

		public XmlCompletionData(string text, string description, bool isAttribute)
		{
			_isAttribute = isAttribute;
			Text = text;
			Description = description;
		}

		public System.Windows.Media.ImageSource Image => null;

		public string Text { get; }

		public object Content => Text + " (" + Description + ")";

		public object Description { get; }

		public double Priority { get; }

		public void Complete(TextArea textArea, ISegment completionSegment,
				EventArgs insertionRequestEventArgs)
		{
			if (_isAttribute)
			{
				textArea.Document.Replace(completionSegment, Text + "=\"\"");
				textArea.Caret.Offset = textArea.Caret.Offset - 1;
			}
			else
			{
				string element = Text + "></" + Text + ">";
				textArea.Document.Replace(completionSegment, element);
				textArea.Caret.Offset = textArea.Caret.Offset - (1 + Text.Length + 2);
			}
		}
	}
}