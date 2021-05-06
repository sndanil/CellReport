using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CellReport.Core;
using System.Windows.Input;

namespace CellReport.Presenters.Wpf
{
    public class CellControl : Canvas
    {
        private bool _selectionMode = false;

        public CellControl(): base()
        {
            VisualTextRenderingMode = TextRenderingMode.ClearType;
            this.SnapsToDevicePixels = true;
            this.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
        }

        public DisplayPresenter Presenter
        {
            get;
            internal set;
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(Brushes.White, new Pen(Brushes.White, 1), new Rect(1, 1, this.ActualWidth, this.ActualHeight));
            if (Presenter != null)
                Presenter.Render(drawingContext, new Rectangle(0, 0, (float)this.ActualWidth, (float)this.ActualHeight));
        }

        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (Presenter == null)
                return;
            int numberToGo = e.Delta / 120;
            Presenter.TopRow -= numberToGo;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (Presenter == null)
                return;
            Mouse.Capture(this);
            Keyboard.Focus(this);
            var position = System.Windows.Input.Mouse.GetPosition(this);
            Presenter.RaiseClick(this, e, Convert.ToSingle(position.X), Convert.ToSingle(position.Y));
            Presenter.StartSelection = Presenter.GetCellByDevicePoint(Convert.ToSingle(position.X), Convert.ToSingle(position.Y), false);
            _selectionMode = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_selectionMode)
            {
                var position = System.Windows.Input.Mouse.GetPosition(this);
                Presenter.EndSelection = Presenter.GetCellByDevicePoint(Convert.ToSingle(position.X), Convert.ToSingle(position.Y), false);
                Presenter.Update();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (Presenter == null)
                return;
            if (Presenter.StartSelection.Equals(Presenter.EndSelection))
            {
                Presenter.ClearSelection();
            }
            else
            {
                if (Presenter.EndSelection.Equals(CellLocation.None))
                    Presenter.ClearSelection();
                Presenter.Update();
            }
            _selectionMode = false;
            Mouse.Capture(this, CaptureMode.None);
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (Presenter != null)
                e.Handled = Presenter.RaiseProcessKey(this, e);
            base.OnKeyUp(e);
        }
    }
}
