using System.Windows.Forms;
using CellReport.Localization;

namespace CellReport.WinForms.Presenters.WinForms
{
	public static class LocalHelper
	{
		public static string LocalizeText(string text)
		{
			return text?.StartsWith("#") == true ? GetLocalString(text) : text;
		}

		public static void LocalizeForm(Form form)
		{
			if (form.Text?.StartsWith("#") == true)
			{
				form.Text = GetLocalString(form.Text);
			}

			foreach (Control item in form.Controls)
			{
				LocalizeControl(item);
			}
		}

		public static void LocalizeControl(Control ctrl)
		{
			if (ctrl.ContextMenuStrip != null)
				LocalizeControl(ctrl.ContextMenuStrip);

			if (ctrl.Text?.StartsWith("#") == true)
			{
				ctrl.Text = GetLocalString(ctrl.Text);
			}

			if (ctrl is ListView listView)
			{
				foreach (ColumnHeader column in listView.Columns)
				{
					column.Text = LocalizeText(column.Text);
				}
			}

			if (ctrl is ToolStrip toolStrip)
			{
				LocalazeToolStripItems(toolStrip.Items);
			}

			if (ctrl is ContextMenuStrip contextMenu)
			{
				LocalazeToolStripItems(contextMenu.Items);
			}

			foreach (Control item in ctrl.Controls)
			{
				LocalizeControl(item);
			}
		}

		private static void LocalazeToolStripItems(ToolStripItemCollection items)
		{
			foreach (ToolStripItem stripItem in items)
			{
				if (stripItem.Text?.StartsWith("#") == true)
					stripItem.Text = GetLocalString(stripItem.Text);

				if (stripItem.ToolTipText?.StartsWith("#") == true)
					stripItem.ToolTipText = GetLocalString(stripItem.ToolTipText);

				if (stripItem is ToolStripDropDownItem dropDown)
					LocalazeToolStripItems(dropDown.DropDownItems);
			}
		}

		public static string GetLocalString(string id)
		{
			return LocalizationManager.GetLocalString(id);
		}

	}
}
