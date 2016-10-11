using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using CellReport.Core;
using System.Globalization;
using System.Windows.Input;

namespace CellReport.Presenters.Wpf
{
    public class DisplayPresenter : CellReport.Presenters.WinForms.DisplayPresenter
    {
        private Panel _control = null;
        private PresentControl _presentControl = null;

        public DisplayPresenter(CellReport.Core.CellManager manager, Panel control)
            : base(manager, null)
        {
            if (manager == null)
                throw new ArgumentNullException("manager");
            ShowToolBar = false;
            _presentControl = new PresentControl();
            _presentControl.SetPresenter(this);
            _control = control;
            this.Control = _presentControl.GetWinFormsControl();

            Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
            DeviceDpiX = Convert.ToSingle(m.M11);
            DeviceDpiY = Convert.ToSingle(m.M22);            
            CurrentUnit = CellReport.Core.GraphicsUnit.WpfPoint;
            //DeviceOffsetX = 1;

            if (control != null)
                _control.Children.Add(_presentControl);
        }

        public override void Update()
        {
            if (!IsInUpdateMode() && _presentControl != null)
                _presentControl.Update();
        }

        protected override CellReport.Core.Rectangle GetHostBounds()
        {
            return new Rectangle(0, 0, Convert.ToSingle(_control.ActualWidth), Convert.ToSingle(_control.ActualHeight));
        }
    }
}
