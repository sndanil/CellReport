namespace CellReport.Editor.Avalonia.ViewModels;

public sealed class SchemaError
{
	public int LineNumber { get; init; }

	public int LinePosition { get; init; }

	public string Message { get; init; } = string.Empty;

	public string Display => LineNumber > 0
		? "Строка " + LineNumber + ", " + LinePosition + ": " + Message
		: Message;
}
