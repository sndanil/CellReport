using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CellReport.Core;
using CellReport.WinForms.Presenters.WinForms;

namespace CellReport.Presenters.Wpf
{
	public class DisplayPresenter : WinForms.DisplayPresenter
	{
		private readonly Panel _control;
		private readonly PresentControl _presentControl;

		public DisplayPresenter(CellManager manager, Panel control)
				: base(manager, null)
		{
			if (manager == null)
				throw new ArgumentNullException(nameof(manager));

			ShowToolBar = false;
			_presentControl = new PresentControl();
			_presentControl.SetPresenter(this);
			_control = control;
			Control = _presentControl.GetWinFormsControl();

			Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
			DeviceDpiX = Convert.ToSingle(m.M11);
			DeviceDpiY = Convert.ToSingle(m.M22);
			CurrentUnit = GraphicsUnit.WpfPoint;
			//DeviceOffsetX = 1;

			if (control != null)
				_control.Children.Add(_presentControl);
		}

		public override void Update()
		{
			if (!IsInUpdateMode() && _presentControl != null)
				_presentControl.Update();
		}

		protected override Rectangle GetHostBounds()
		{
			return new Rectangle(0, 0, Convert.ToSingle(_control.ActualWidth), Convert.ToSingle(_control.ActualHeight));
		}

		public void ShowFindDialog()
		{
			using (var findForm = new FindForm())
			{
				findForm.Presenter = this;
				findForm.ShowDialog();
			}
		}
	}
}
