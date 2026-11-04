using CellReport.Core;

namespace CellReport.Avalonia.Editing
{
	public sealed class CellEditEventArgs : System.EventArgs
	{
		public CellEditEventArgs(CellLocation location, CellSnapshot before, CellSnapshot after)
		{
			Location = location;
			Before = before;
			After = after;
		}

		public CellLocation Location { get; }

		public CellSnapshot Before { get; }

		public CellSnapshot After { get; }
	}
}
