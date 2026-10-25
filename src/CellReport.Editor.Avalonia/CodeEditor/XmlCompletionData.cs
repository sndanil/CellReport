using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;

namespace CellReport.Editor.Avalonia.CodeEditor;

public enum XmlCompletionDataType
{
	Element,
	Attribute,
	AttributeValue
}

public sealed class XmlCompletionData : ICompletionData
{
	private readonly XmlCompletionDataType _type;

	public XmlCompletionData(string text, string? description, XmlCompletionDataType type, object? content)
	{
		Text = text;
		Description = description ?? string.Empty;
		_type = type;
		Content = content ?? text;
	}

	public IImage? Image => null;

	public string Text { get; }

	public object Content { get; }

	public object Description { get; }

	public double Priority => 0;

	public override string ToString() => Text;

	public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
	{
		switch (_type)
		{
			case XmlCompletionDataType.Element:
				var element = Text + "></" + Text + ">";
				textArea.Document.Replace(completionSegment, element);
				textArea.Caret.Offset -= 1 + Text.Length + 2;
				break;
			case XmlCompletionDataType.Attribute:
				textArea.Document.Replace(completionSegment, Text + "=\"\"");
				textArea.Caret.Offset -= 1;
				break;
			case XmlCompletionDataType.AttributeValue:
				var start = completionSegment.Offset;
				var end = completionSegment.EndOffset;
				var documentText = textArea.Document.Text;
				if (start < documentText.Length && documentText[start] == '"')
				{
					start--;
					end--;
				}

				while (start > 0 && documentText[start] != '"')
					start--;
				start++;
				while (end < documentText.Length && documentText[end] != '"')
					end++;

				textArea.Document.Replace(new AnchorSegment(textArea.Document, start, Math.Max(0, end - start)), Text);
				break;
		}
	}
}
