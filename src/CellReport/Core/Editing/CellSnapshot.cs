using System;
using CellReport.Core.Contents;

namespace CellReport.Core
{
	/// <summary>
	/// Снимок состояния одной ячейки для undo/redo в просмотрщике.
	/// </summary>
	public sealed class CellSnapshot
	{
		private CellSnapshot(CellLocation location)
		{
			Location = location;
		}

		public CellLocation Location { get; private set; }

		public bool Exists { get; private set; }

		public string StyleID { get; private set; }

		public CellSpan Span { get; private set; }

		public bool Editable { get; private set; }

		public string EditParam { get; private set; }

		public string EditPlaceholder { get; private set; }

		public string EditHint { get; private set; }

		public BaseContent Content { get; private set; }

		public static CellSnapshot Capture(CellManager manager, CellLocation location)
		{
			if (manager == null)
				throw new ArgumentNullException(nameof(manager));

			var snapshot = new CellSnapshot(location);
			if (!manager.IsCurrectLocation(location))
				return snapshot;

			var cell = manager.GetCell(location);
			if (cell == null)
				return snapshot;

			snapshot.Exists = true;
			snapshot.StyleID = cell.StyleID;
			snapshot.Span = cell.SpanInternal;
			snapshot.Editable = cell.Editable;
			snapshot.EditParam = cell.EditParam;
			snapshot.EditPlaceholder = cell.EditPlaceholder;
			snapshot.EditHint = cell.EditHint;
			snapshot.Content = cell.Content == null ? null : cell.Content.Clone(manager);
			return snapshot;
		}

		public void Restore(CellManager manager)
		{
			if (manager == null)
				throw new ArgumentNullException(nameof(manager));

			if (!manager.IsCurrectLocation(Location))
				return;

			if (!Exists)
			{
				var existing = manager.GetCell(Location);
				if (existing != null)
					manager.DeleteCell(Location);
				return;
			}

			var cell = manager.GetCell(Location);
			if (cell == null)
				cell = manager.CreateCell(Location);
			if (cell == null)
				return;

			cell.StyleID = StyleID;
			cell.Span = Span;
			cell.Editable = Editable;
			cell.EditParam = EditParam;
			cell.EditPlaceholder = EditPlaceholder;
			cell.EditHint = EditHint;
			cell.Content = Content == null ? null : Content.Clone(manager);
		}
	}
}
