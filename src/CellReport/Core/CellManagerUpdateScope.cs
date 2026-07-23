using System;

namespace CellReport.Core
{
	public sealed class CellManagerUpdateScope : IDisposable
	{
		private CellManager _manager;
		private bool _disposed;

		internal CellManagerUpdateScope(CellManager manager)
		{
			_manager = manager ?? throw new ArgumentNullException(nameof(manager));
			_manager.BeginUpdate();
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;
			if (_manager != null)
			{
				_manager.EndUpdate();
				_manager = null;
			}
		}
	}
}
