using System;
using System.Linq;
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
using CellReport.Export.Pdf;
using System.Drawing.Printing;

namespace CellReport.Presenters.WinForms
{
	public partial class PresentControl : GestureControl
	{
		private BaseDisplayPresenter _presenter;
		private readonly Dictionary<string, int> _bookMarkImages = new Dictionary<string, int>();
		private bool ShowResizers => Manager.DisplaySettings.ShowResizers && !Manager.IsDesignMode;
		private CellManager Manager => _presenter.Manager;

		private readonly Timer _moveTimer = new Timer();

        private long _zoomDistance = 0;
        private float _startScale = 0f;
        private int _gestureShiftX = 0;
        private int _gestureShiftY = 0;

        public BaseDisplayPresenter Presenter
        {
            get => _presenter;
            set => _presenter = value;
        }

        public string DefaultPrinterName { get; set; }

		public event EventHandler DefaultPrinterNameChanged;

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
			Localization.LocalizationManager.AddDefaultString("Report/ExportToPdf", "Экспорт в PDF");
			Localization.LocalizationManager.AddDefaultString("Report/Search", "Поиск");
			Localization.LocalizationManager.AddDefaultString("Report/GoToLine", "Перейти к строке");
			Localization.LocalizationManager.AddDefaultString("Report/Save", "Сохранить");
			Localization.LocalizationManager.AddDefaultString("Report/BookMarks", "Закладки");

			LocalHelper.LocalizeControl(this);
		}

        public PresentControl(DisplayPresenter presenter)
                : this()
        {
            _presenter = presenter;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (PrinterSettings.InstalledPrinters.Count > 0)
            {
                //PrinterSettings.InstalledPrinters.OfType<string>().Contains(DefaultPrinterName)
                var checkedPrinter = DefaultPrinterName ?? (new PrinterSettings()).PrinterName;
				var printersMenuList = new List<ToolStripMenuItem>();
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    var menuItem = new ToolStripMenuItem(printerName) 
					{ 
						Checked = checkedPrinter == printerName 
					};
                    menuItem.Click += (s, _) => 
					{
						var currentMenu = (ToolStripMenuItem)s;
                        printersMenuList.ForEach(i => i.Checked = false);
                        DefaultPrinterName = currentMenu.Text;
                        currentMenu.Checked = true;
                        DefaultPrinterNameChanged?.Invoke(this, new EventArgs());
                        Print(DefaultPrinterName);
					};

                    _printButton.DropDownItems.Insert(printersMenuList.Count, menuItem);
                    printersMenuList.Add(menuItem);
                }
                _printButton.DropDownItems.Insert(printersMenuList.Count, new ToolStripSeparator());
            }

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

        protected override void OnTwoFingerTap(TwoFingerTapEventArgs e)
		{
			base.OnTwoFingerTap(e);
			_presenter.Scale = 1;
		}

		protected override void OnPan(PanEventArgs e)
		{
			base.OnPan(e);

			_gestureShiftY += e.PanOffset.Y;
			var topRow = _presenter.TopRow;
			while (topRow >= 0 && topRow < _presenter.Manager.RowsCount && Math.Abs(_gestureShiftY) > Presenter.GetRowHeight(topRow))
			{
				var shift = _gestureShiftY > 0 ? 1 : -1;
				_gestureShiftY -= Convert.ToInt32(Presenter.GetRowHeight(topRow)) * shift;
				topRow -= shift;
			}
			_presenter.TopRow = topRow;

			_gestureShiftX += e.PanOffset.X;
			var leftColumn = _presenter.LeftColumn;
			while (leftColumn >= 0 && leftColumn < _presenter.Manager.ColumnsCount && Math.Abs(_gestureShiftX) > Presenter.GetColumnWidth(leftColumn))
			{
				var shift = _gestureShiftX > 0 ? 1 : -1;
				_gestureShiftX -= Convert.ToInt32(Presenter.GetColumnWidth(leftColumn)) * shift;
				leftColumn -= shift;
			}
			_presenter.LeftColumn = leftColumn;
		}

		protected override void OnZoom(ZoomEventArgs e)
		{
			base.OnZoom(e);

			if (e.Begin)
			{
				_zoomDistance = e.Distance;
				_startScale = _presenter.Scale;
			}

			var alfa = e.Distance - _zoomDistance;
			var step = 200f;
			var newScale = _startScale + alfa / step;
			_presenter.Scale = Math.Max(0.25f, Math.Min(300, newScale));
			if (e.End)
				_zoomDistance = 0;
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

		void Application_Idle(object sender, EventArgs e)
		{
			_showGridButton.Checked = _showGridMenuItem.Checked = _presenter.ShowGridLines;
			_showResizersButton.Checked = _showResizersMenuItem.Checked = ShowResizers;
			_autoWidthMenuItem.Checked = _autoWidthCntxMenuItem.Checked = Manager.PrintSettings.AutoFitWidthPages != 0;
			_bookMarksStripButton.Checked = _presenter.ShowBookMarks;
			_bookMarksSplitter.Visible = _presenter.ShowBookMarks;
			_bookMarksTreeView.Visible = _presenter.ShowBookMarks;
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
			Print(DefaultPrinterName);
		}

		private void Print(string printerName =  null)
		{
            try
            {
                var printPresenter = new PrintPresenter(_presenter.Manager);
                printPresenter.Print(printerName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

		private void CustomPrintMenuItemClick(object sender, EventArgs e)
		{
			try
			{
				var printPresenter = new PrintPresenter(_presenter.Manager);
				printPresenter.CustomPrint();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void PageSetupMenuItemClick(object sender, EventArgs e)
		{
			try
			{
				var printPresenter = new PrintPresenter(_presenter.Manager);
				printPresenter.ShowCustomPage();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void PrintPreviewMenuItemClick(object sender, EventArgs e)
		{
			try
			{
				var printPresenter = new PrintPresenter(_presenter.Manager);
				printPresenter.ShowPreviewDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
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

		private void ExportPdfButtonClick(object sender, EventArgs e)
		{
			var exp = PdfExporter.CreateVectorExporter(_presenter.Manager);
			Export(exp, true);
		}

		private void SaveMenuItemClick(object sender, EventArgs e)
		{
			using (var dialog = new SaveFileDialog())
			{
				dialog.Filter = "Excel (*.xlsx)|*.xlsx|HTML (*.html)|*.html|PDF (*.pdf)|*.pdf|CellReport шаблон (*.tcr)|*.tcr|CellReport шаблон пакетный (*.tcrp)|*.tcrp|All files (*.*)|*.*";
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
						else if (dialog.FileName.EndsWith(".pdf"))
							exp = PdfExporter.CreateVectorExporter(_presenter.Manager);
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

		private void BookMarksTreeViewAfterSelect(object sender, TreeViewEventArgs e)
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
