using System;
using System.Collections.Generic;

namespace CellReport.Core
{
	/// <summary>
	/// История правок ячеек в просмотрщике (без полного снимка шаблона).
	/// </summary>
	public sealed class CellEditHistory
	{
		private sealed class Entry
		{
			public CellSnapshot Before;
			public CellSnapshot After;
		}

		private readonly List<Entry> _undo = new List<Entry>();
		private readonly List<Entry> _redo = new List<Entry>();
		private int _limit = 100;

		public int Limit
		{
			get { return _limit; }
			set
			{
				_limit = value < 1 ? 1 : value;
				Trim();
			}
		}

		public bool CanUndo
		{
			get { return _undo.Count > 0; }
		}

		public bool CanRedo
		{
			get { return _redo.Count > 0; }
		}

		public void Push(CellSnapshot before, CellSnapshot after)
		{
			if (before == null || after == null)
				return;

			_undo.Add(new Entry { Before = before, After = after });
			_redo.Clear();
			Trim();
		}

		public CellLocation Undo(CellManager manager)
		{
			if (_undo.Count == 0)
				return CellLocation.None;

			var index = _undo.Count - 1;
			var entry = _undo[index];
			_undo.RemoveAt(index);
			entry.Before.Restore(manager);
			_redo.Add(entry);
			return entry.Before.Location;
		}

		public CellLocation Redo(CellManager manager)
		{
			if (_redo.Count == 0)
				return CellLocation.None;

			var index = _redo.Count - 1;
			var entry = _redo[index];
			_redo.RemoveAt(index);
			entry.After.Restore(manager);
			_undo.Add(entry);
			return entry.After.Location;
		}

		public void Clear()
		{
			_undo.Clear();
			_redo.Clear();
		}

		private void Trim()
		{
			while (_undo.Count > _limit)
				_undo.RemoveAt(0);
		}
	}
}
