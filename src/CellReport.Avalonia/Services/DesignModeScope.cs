using CellReport.Core;
using System;

namespace CellReport.Avalonia.Services
{
	public static class DesignModeScope
	{
		public static IDisposable Suspend(CellManager manager)
		{
			if (manager == null)
				throw new ArgumentNullException(nameof(manager));
			return new Suspension(manager);
		}

		private sealed class Suspension : IDisposable
		{
			private readonly CellManager _manager;
			private readonly bool _previous;
			private bool _disposed;

			public Suspension(CellManager manager)
			{
				_manager = manager;
				_previous = manager.IsDesignMode;
				manager.IsDesignMode = false;
			}

			public void Dispose()
			{
				if (_disposed)
					return;

				_disposed = true;
				_manager.IsDesignMode = _previous;
			}
		}
	}
}
