using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CellReport.Presenters.WinForms
{
	public partial class DoubleBufferedControl : UserControl
	{
		public DoubleBufferedControl()
		{
			InitializeComponent();

			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
					ControlStyles.Selectable | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, true);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (ProcessKey != null)
			{
				var result = ProcessKey(this, new KeyEventArgs(keyData));
				if (!result)
					return base.ProcessCmdKey(ref msg, keyData);
				else
					return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
		}

		public delegate bool ProcessKeyHandler(object sender, KeyEventArgs e);
		public event ProcessKeyHandler ProcessKey;
	}
}
