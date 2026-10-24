using CellReport.Core;
using System;

namespace CellReport.Avalonia.Services
{
	public static class DesignModeScope
	{
		public static IDisposable Suspend()
		{
			return new Suspension();
		}

		private sealed class Suspension : IDisposable
		{
			private readonly bool _previous;
			private bool _disposed;

			public Suspension()
			{
				_previous = CellManager.IsDesignMode;
				CellManager.IsDesignMode = false;
			}

			public void Dispose()
			{
				if (_disposed)
					return;

				_disposed = true;
				CellManager.IsDesignMode = _previous;
			}
		}
	}
}
