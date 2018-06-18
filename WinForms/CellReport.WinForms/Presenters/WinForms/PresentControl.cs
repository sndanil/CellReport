using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using CellReport.Core;
using CellReport.WinForms.Presenters.WinForms;
using CellReport.Export.Excel;
using CellReport.Export;
using CellReport.Export.Html;
using Alteridem.WinTouch;

namespace CellReport.Presenters.WinForms
{
	public partial class PresentControl : UserControl
	{
		private GestureListener _gesture;
		private BaseDisplayPresenter _presenter;
		private readonly Dictionary<string, int> _bookMarkImages = new Dictionary<string, int>();
		private bool ShowResizers => Manager.DisplaySettings.ShowResizers && !CellManager.IsDesignMode;
		private CellManager Manager => _presenter.Manager;

		private readonly Timer _moveTimer = new Timer();

		public PresentControl()
		{
			InitializeComponent();

			_paintPanel.MouseWheel += PaintPanelMouseWheel;
			_paintPanel.MouseDown += PaintPanelMouseDown;
			_paintPanel.MouseUp += PaintPanelMouseUp;
			_paintPanel.MouseMove += PaintPanelMouseMove;
			_paintPanel.MouseClick += PaintPanelMouseClick;
			_paintPanel.ProcessKey += PaintPanelProcessKey;
			_paintPanel.DoubleClick += PaintPanelDoubleClick;

			if (!CellManager.IsDesignMode)
			{
				_gesture = new GestureListener(_paintPanel);
				_gesture.Zoom += (sender, e) =>
				{
					_presenter.Scale = Math.Max(0.25f, Math.Min(300, Convert.ToSingle(_presenter.Scale * e.PercentChange)));
				};
				_gesture.TwoFingerTap += (sender, e) =>
				{
					_presenter.Scale = 1;
				};
				_gesture.Pan += (sender, e) =>
				{
					_presenter.TopRow -= e.PanOffset.Y / 3;
					_presenter.LeftColumn -= e.PanOffset.X / 3;
				};
			}

			Application.Idle += Application_Idle;
			_moveTimer.Interval = 100;
			_moveTimer.Tick += MoveTimerOnTick;

			Localization.LocalizationManager.AddDefaultString("Report/Print", "Печать");
			Localization.LocalizationManager.AddDefaultString("Report/CustomPrint", "Выборочная печать");
			Localization.LocalizationManager.AddDefaultString("Report/Preview", "Предварительный просмотр");
			Localization.LocalizationManager.AddDefaultString("Report/PageSettings", "Параметры страницы");
			Localization.LocalizationManager.AddDefaultString("Report/AutoFitPage", "Автоширина по странице");
			Localization.LocalizationManager.AddDefaultString("Report/ShowHideGrid", "Отобразить или скрыть сетку");
			Localization.LocalizationManager.AddDefaultString("Report/ShowHideHeaders", "Отобразить или скрыть заголовки строк и столбцов");
			Localization.LocalizationManager.AddDefaultString("Report/ExportToExcel", "Экспорт в Excel");
			Localization.LocalizationManager.AddDefaultString("Report/Search", "Поиск");
			Localization.LocalizationManager.AddDefaultString("Report/GoToLine", "Перейти к строке");
			Localization.LocalizationManager.AddDefaultString("Report/Save", "Сохранить");
			Localization.LocalizationManager.AddDefaultString("Report/BookMarks", "Закладки");

			LocalHelper.LocalizeControl(this);
		}

		private void MoveTimerOnTick(object sender, EventArgs eventArgs)
		{
			var position = _paintPanel.PointToClient(Cursor.Position);

			if (position.Y > _paintPanel.Height)
			{
				_presenter.TopRow++;
			}
			if (position.Y < 0)
			{
				_presenter.TopRow--;
			}
			if (position.X > _paintPanel.Width)
			{
				_presenter.LeftColumn++;
			}
			if (position.X < 0)
			{
				_presenter.LeftColumn--;
			}
		}

		void PaintPanelMouseMove(object sender, MouseEventArgs e)
		{
			if (Presenter == null || !Presenter.IsSelectionMode)
				return;

			Presenter.EndSelection = Presenter.GetCellByDevicePoint(Convert.ToSingle(e.X), Convert.ToSingle(e.Y), false);
			Presenter.Update();
		}

		void PaintPanelMouseUp(object sender, MouseEventArgs e)
		{
			if (Presenter == null || e.Button != MouseButtons.Left)
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
			Presenter.IsSelectionMode = false;
			_paintPanel.Capture = false;
			_moveTimer.Stop();
		}

		void PaintPanelMouseDown(object sender, MouseEventArgs e)
		{
			if (Presenter == null || e.Button != MouseButtons.Left)
				return;
			_paintPanel.Capture = true;
			Presenter.RaiseClick(this, e, Convert.ToSingle(e.X), Convert.ToSingle(e.Y));
			Presenter.StartSelection = Presenter.GetCellByDevicePoint(Convert.ToSingle(e.X), Convert.ToSingle(e.Y), false);
			Presenter.IsSelectionMode = true;
			_moveTimer.Start();
		}

		private void PaintPanelDoubleClick(object sender, EventArgs e)
		{
			Presenter.IsSelectionMode = false;
			_paintPanel.Capture = false;
			_moveTimer.Stop();
			_presenter.RaiseDblClick(sender, e, _paintPanel.PointToClient(MousePosition).X,
					_paintPanel.PointToClient(MousePosition).Y);
		}

		public BaseDisplayPresenter Presenter
		{
			get => _presenter;
			set => _presenter = value;
		}

		void Application_Idle(object sender, EventArgs e)
		{
			_showGridButton.Checked = _showGridMenuItem.Checked = _presenter.ShowGridLines;
			_showResizersButton.Checked = _showResizersMenuItem.Checked = ShowResizers;
			_autoWidthMenuItem.Checked = _autoWidthCntxMenuItem.Checked = Manager.PrintSettings.AutoFitWidthPages != 0;
			_bookMarksStripButton.Checked = _presenter.ShowBookMarks;
			_bookMarksSplitter.Visible = _presenter.ShowBookMarks;
			_bookMarksTreeView.Visible = _presenter.ShowBookMarks;
		}

		public PresentControl(DisplayPresenter presenter)
				: this()
		{
			_presenter = presenter;
		}

		protected override void OnLoad(EventArgs e)
		{
			_scaleComboBox.SelectedIndex = _scaleComboBox.Items.IndexOf("100%");
			_columnResizer.Presenter = _presenter;
			_rowsResizer.TopOffset = _columnResizer.Height;
			_rowsResizer.Presenter = _presenter;
			_presenter.ShowToolBarChanged += TopRowLeftColumnChanged;
			_paintPanel.Paint += (s, c) => _presenter.Render(s, c);
			_paintPanel.Focus();

			foreach (var bookMark in _presenter.Manager.GetBookMarks())
				AddBookMark(bookMark, null);

			Update(false);
		}

		private void AddBookMark(Core.BookMarks.BookMark bookMark, TreeNode parent)
		{
			TreeNode node;
			if (parent != null)
				node = parent.Nodes.Add(bookMark.GetText() ?? string.Empty);
			else
				node = _bookMarksTreeView.Nodes.Add(bookMark.GetText() ?? string.Empty);
			if (!string.IsNullOrEmpty(bookMark.Image))
			{
				if (_bookMarkImages.Count == 0)
					_bookMarksImageList.Images.Clear();
				int index;
				if (!_bookMarkImages.TryGetValue(bookMark.Image, out index))
				{
					var bytes = _presenter.Manager.GetDataByUri(bookMark.Image);
					if (bytes != null && bytes.Length > 0)
					{
						var img = Image.FromStream(new System.IO.MemoryStream(bytes));
						index = _bookMarksImageList.Images.Count;
						_bookMarkImages.Add(bookMark.Image, index);
						_bookMarksImageList.Images.Add(img);
					}
				}
				node.ImageIndex = index;
				node.StateImageIndex = index;
				node.SelectedImageIndex = index;
			}
			node.Tag = bookMark;
			foreach (var child in bookMark.Child)
				AddBookMark(child, node);
		}

		public void Update(bool invalidate)
		{
			_columnResizer.Visible = ShowResizers;
			_rowsResizer.Visible = ShowResizers;
			_toolBar.Visible = _presenter.ShowToolBar;
			_hScrollBar.Maximum = _presenter.Manager.ColumnsCount;
			_vScrollBar.Maximum = _presenter.Manager.RowsCount;
			_vScrollBar.Value = _presenter.TopRow;
			_hScrollBar.Value = _presenter.LeftColumn;
			if (invalidate)
			{
				_rowsResizer.Invalidate(true);
				_columnResizer.Invalidate(true);
				_paintPanel.Invalidate(true);
				//this.Invalidate(true);
			}
		}

		private void TopRowLeftColumnChanged(object sender, EventArgs e)
		{
			Update(true);
		}

		private void ScrollBar_ValueChanged(object sender, EventArgs e)
		{
			if (_presenter != null)
			{
				_presenter.TopRow = _vScrollBar.Value;
				_presenter.LeftColumn = _hScrollBar.Value;
			}
		}

		private void ShowGridButtonClick(object sender, EventArgs e)
		{
			_presenter.Manager.DisplaySettings.ShowGridLines = !_presenter.Manager.DisplaySettings.ShowGridLines;
		}

		private void ShowResizersButtonClick(object sender, EventArgs e)
		{
			Manager.DisplaySettings.ShowResizers = !ShowResizers;
		}

		private void PrintButtonClick(object sender, EventArgs e)
		{
			try
			{
				var printPresenter = new PrintPresenter(_presenter.Manager);
				printPresenter.Print();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void CustomPrintMenuItemClick(object sender, EventArgs e)
		{
			var printPresenter = new PrintPresenter(_presenter.Manager);
			printPresenter.CustomPrint();
		}

		private void PageSetupMenuItemClick(object sender, EventArgs e)
		{
			var printPresenter = new PrintPresenter(_presenter.Manager);
			printPresenter.ShowCustomPage();
		}

		private void PrintPreviewMenuItemClick(object sender, EventArgs e)
		{
			var printPresenter = new PrintPresenter(_presenter.Manager);
			printPresenter.ShowPreviewDialog();
		}

		private void AutoWidthMenuItemClick(object sender, EventArgs e)
		{
			_presenter.Manager.PrintSettings.AutoFitWidthPages =
					_presenter.Manager.PrintSettings.AutoFitWidthPages == 0 ? 1 : 0;
		}

		private void ScaleComboBoxSelectedIndexChanged(object sender, EventArgs e)
		{
			var scaleStr = string.IsNullOrEmpty(_scaleComboBox.Text) ? "100" : _scaleComboBox.Text.Replace("%", string.Empty);
			_presenter.Scale = float.Parse(scaleStr, CultureInfo.InvariantCulture) / 100f;
			_paintPanel.Focus();
		}

		private void PaintPanelMouseWheel(object sender, MouseEventArgs e)
		{
			if (_presenter != null)
			{
				int numberToGo = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
				_presenter.TopRow = _presenter.TopRow - numberToGo;
			}
		}

		private void PaintPanelMouseClick(object sender, MouseEventArgs e)
		{
			//_presenter.RaiseClick(sender, e, e.X, e.Y);
		}

		bool PaintPanelProcessKey(object sender, EventArgs e)
		{
			return _presenter.RaiseProcessKey(sender, e);
		}

		internal RectangleF GetPaintPanelBounds()
		{
			return _paintPanel.Bounds;
		}

		private void Export(BaseExporter exp, bool openFile)
		{
			using (var dialog = new SaveFileDialog())
			{
				dialog.Filter = string.Format("{0} файлы (*.{1})|*.{1}|Все файлы (*.*)|*.*", exp.Name, exp.Extension);
				dialog.FileName = _presenter.Manager.Properties.DocumentName + "." + exp.Extension;
				dialog.FilterIndex = 1;
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					try
					{
						using (var file = System.IO.File.Create(dialog.FileName))
							exp.ExportTo(file);
						if (openFile)
							System.Diagnostics.Process.Start(dialog.FileName);
					}
					catch (System.IO.IOException)
					{
						MessageBox.Show("Невозможно сохранить файл. Возможно, файл занят другим приложением.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					catch (Win32Exception)
					{
						MessageBox.Show("Не удалось запустить приложения ассоциированное с файлом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void ExportExcelButtonClick(object sender, EventArgs e)
		{
			var exp = new ExcelExporter(_presenter.Manager, _presenter.Renderer);
			Export(exp, true);
		}

		private void SaveMenuItemClick(object sender, EventArgs e)
		{
			using (var dialog = new SaveFileDialog())
			{
				dialog.Filter = "Excel (*.xlsx)|*.xlsx|HTML (*.html)|*.html|CellReport шаблон (*.tcr)|*.tcr|CellReport шаблон пакетный (*.tcrp)|*.tcrp|All files (*.*)|*.*";
				dialog.FileName = _presenter.Manager.Properties.DocumentName + ".xlsx";
				dialog.FilterIndex = 1;
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					try
					{
						BaseExporter exp = null;
						if (dialog.FileName.EndsWith(".xlsx"))
							exp = new ExcelExporter(_presenter.Manager, _presenter.Renderer);
						else if (dialog.FileName.EndsWith(".html"))
							exp = new HtmlExporter(_presenter.Manager, _presenter.Renderer);
						if (exp != null)
						{
							using (var file = System.IO.File.Create(dialog.FileName))
								exp.ExportTo(file);
						}
						else
						{
							(new CellTemplate(_presenter.Manager)).Save(dialog.FileName, dialog.FileName.EndsWith("tcrp") ? TemplateType.Package : TemplateType.Xml);
						}
					}
					catch (System.IO.IOException)
					{
						MessageBox.Show("Невозможно сохранить файл. Возможно, файл занят другим приложением.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void GoToRowMenuItemClick(object sender, EventArgs e)
		{
			using (var goToRowForm = new GoToRowForm())
			{
				goToRowForm.MaxSelectedRow = _presenter.Manager.RowsCount;
				goToRowForm.SelectedRow = _presenter.CurrentCell.Row + 1;
				if (goToRowForm.ShowDialog() == DialogResult.OK)
				{
					_presenter.CurrentCell = new CellLocation
					{
						Column = _presenter.CurrentCell.Column,
						Row = goToRowForm.SelectedRow - 1
					};
				}
			}

		}

		private void FindMenuItemClick(object sender, EventArgs e)
		{
			using (var findForm = new FindForm())
			{
				findForm.Presenter = _presenter;
				findForm.ShowDialog();
			}
		}

		private void BookMarksStripButtonClick(object sender, EventArgs e)
		{
			_presenter.ShowBookMarks = !_presenter.ShowBookMarks;
		}

		private void _bookMarksTreeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node != null && e.Node.Tag != null)
			{
				var bookMark = e.Node.Tag as Core.BookMarks.BookMark;
				if (bookMark != null)
				{
					_presenter.TopRow = bookMark.Location.Row;
					_presenter.CurrentCell = bookMark.Location;
				}
			}
		}
	}
}
