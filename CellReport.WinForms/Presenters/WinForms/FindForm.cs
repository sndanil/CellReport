using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CellReport.Core.Contents;
using CellReport.Localization;
using CellReport.Presenters;

namespace CellReport.WinForms.Presenters.WinForms
{
	public partial class FindForm : Form
	{
		private BaseDisplayPresenter _presenter;
		private static List<string> _lastFind = new List<string>();
		private Core.Cell _lastCell;

		public BaseDisplayPresenter Presenter
		{
			get => _presenter;
			set => _presenter = value;
		}

		public FindForm()
		{
			InitializeComponent();
			BindComboBox();

			LocalizationManager.AddDefaultString("Report/Search/Title", "Поиск");
			LocalizationManager.AddDefaultString("Report/Search/FindWhat", "Найти");
			LocalizationManager.AddDefaultString("Report/Search/MatchCase", "Учитывать регистр");
			LocalizationManager.AddDefaultString("Report/Search/MatchWholeWord", "Только слово целиком");
			LocalizationManager.AddDefaultString("Report/Search/All", "Найти все");
			LocalizationManager.AddDefaultString("Report/Search/Next", "Найти далее");
			LocalizationManager.AddDefaultString("Report/Search/Close", "Закрыть");
			LocalizationManager.AddDefaultString("Report/Search/LookOver", "Просматривать");
			LocalizationManager.AddDefaultString("Report/Search/LookOver/ByRows", "По строкам"); 			
			LocalizationManager.AddDefaultString("Report/Search/LookOver/ByColumns", "По столбцам");
			LocalizationManager.AddDefaultString("Report/Search/Direction", "Направление");
			LocalizationManager.AddDefaultString("Report/Search/Direction/Forward", "Вперед");
			LocalizationManager.AddDefaultString("Report/Search/Direction/Backward", "Назад");
			LocalizationManager.AddDefaultString("Report/Search/Result/Content", "Содержимое");
			LocalizationManager.AddDefaultString("Report/Search/Result/Row", "Строка");
			LocalizationManager.AddDefaultString("Report/Search/Result/Column", "Столбец");
			LocalizationManager.AddDefaultString("Report/Search/NotFound", "Искомая комбинация не найдена.");

			_findByComboBox.Items.AddRange(new object[]
			{
				LocalizationManager.GetLocalString("#Report/Search/LookOver/ByRows"),
				LocalizationManager.GetLocalString("#Report/Search/LookOver/ByColumns"),
			});
			_directionComboBox.Items.AddRange(new object[]
			{
				LocalizationManager.GetLocalString("#Report/Search/Direction/Forward"),
				LocalizationManager.GetLocalString("#Report/Search/Direction/Backward"),
			});

			_directionComboBox.SelectedIndex = 0;
			_findByComboBox.SelectedIndex = 0;

			LocalHelper.LocalizeForm(this);
		}

		private void BindComboBox()
		{
			var arr = _lastFind.ToArray();
			Array.Reverse(arr);
			_findComboBox.Items.Clear();
			_findComboBox.Items.AddRange(arr.Select(a => (object)a).ToArray());
			if (_lastFind.Count > 0)
				_findComboBox.Text = _lastFind[_lastFind.Count - 1];
			_findComboBox.SelectAll();
		}

		private void AddToLast()
		{
			var index = _lastFind.IndexOf(_findComboBox.Text);
			if (index >= 0)
				_lastFind.RemoveAt(index);
			_lastFind.Add(_findComboBox.Text);
			BindComboBox();
		}

		private void CancelButtonClick(object sender, EventArgs e)
		{
			Close();
		}

		private string CheckCell(Core.Cell cell, bool setPosition)
		{
			if (cell != null && _lastCell != cell)
			{
				if (cell.Content is TextBasedContent textContent)
				{
					if (textContent.FindText(_findComboBox.Text, _caseSensCheckBox.Checked))
					{
						_lastCell = cell;
						if (setPosition)
							_presenter.CurrentCell = cell.Location;
						return textContent.GetTextForRender();
					}
				}
				if (cell.Content is CollectionContent collection)
				{
					foreach (var item in collection.GetContents())
					{
						textContent = item as TextBasedContent;
						if (textContent != null)
						{
							if (textContent.FindText(_findComboBox.Text, _caseSensCheckBox.Checked))
							{
								_lastCell = cell;
								if (setPosition)
									_presenter.CurrentCell = cell.Location;
								return textContent.GetTextForRender();
							}
						}
					}
				}
			}
			return null;
		}

		private void FindNextButtonClick(object sender, EventArgs e)
		{
			AddToLast();
			if (_findByComboBox.SelectedIndex == 0)
			{
				if (_directionComboBox.SelectedIndex == 0)
				{
					for (int row = _presenter.CurrentCell.Row; row < _presenter.Manager.RowsCount; row++)
					{
						var startCol = row == _presenter.CurrentCell.Row ? _presenter.CurrentCell.Column : 0;
						for (int col = startCol; col < _presenter.Manager.ColumnsCount; col++)
						{
							if (CheckCell(_presenter.Manager.GetCell(row, col), true) != null)
								return;
						}
					}
				}
				else
				{
					for (int row = _presenter.CurrentCell.Row; row >= 0; row--)
					{
						var startCol = row == _presenter.CurrentCell.Row ? _presenter.CurrentCell.Column : _presenter.Manager.ColumnsCount - 1;
						for (int col = startCol; col >= 0; col--)
						{
							if (CheckCell(_presenter.Manager.GetCell(row, col), true) != null)
								return;
						}
					}
				}
			}
			else
			{
				if (_directionComboBox.SelectedIndex == 0)
				{
					for (int col = _presenter.CurrentCell.Column; col < _presenter.Manager.ColumnsCount; col++)
					{
						var startRow = col == _presenter.CurrentCell.Column ? _presenter.CurrentCell.Row : 0;
						for (int row = startRow; row < _presenter.Manager.RowsCount; row++)
						{
							if (CheckCell(_presenter.Manager.GetCell(row, col), true) != null)
								return;
						}
					}
				}
				else
				{
					for (int col = _presenter.CurrentCell.Column; col >= 0; col--)
					{
						var startRow = col == _presenter.CurrentCell.Column ? _presenter.CurrentCell.Row : _presenter.Manager.RowsCount - 1;
						for (int row = startRow; row >= 0; row--)
						{
							if (CheckCell(_presenter.Manager.GetCell(row, col), true) != null)
								return;
						}
					}
				}
			}
			MessageBox.Show(LocalizationManager.GetLocalString("#Report/Search/NotFound"));
		}

		private void FindAllButtonClick(object sender, EventArgs e)
		{
			AddToLast();
			_findListView.Items.Clear();
			_findListView.BeginUpdate();
			if (_findByComboBox.SelectedIndex == 0)
			{
				if (_directionComboBox.SelectedIndex == 0)
				{
					for (int row = 0; row < _presenter.Manager.RowsCount; row++)
						for (int col = 0; col < _presenter.Manager.ColumnsCount; col++)
							AddItemToList(CheckCell(_presenter.Manager.GetCell(row, col), false), row, col);
				}
				else
				{
					for (int row = _presenter.Manager.RowsCount - 1; row >= 0; row--)
						for (int col = _presenter.Manager.ColumnsCount - 1; col >= 0; col--)
							AddItemToList(CheckCell(_presenter.Manager.GetCell(row, col), false), row, col);
				}
			}
			else
			{
				if (_directionComboBox.SelectedIndex == 0)
				{
					for (int col = 0; col < _presenter.Manager.ColumnsCount; col++)
						for (int row = 0; row < _presenter.Manager.RowsCount; row++)
							AddItemToList(CheckCell(_presenter.Manager.GetCell(row, col), false), row, col);
				}
				else
				{
					for (int col = _presenter.Manager.ColumnsCount - 1; col >= 0; col--)
						for (int row = _presenter.Manager.RowsCount - 1; row >= 0; row--)
							AddItemToList(CheckCell(_presenter.Manager.GetCell(row, col), false), row, col);
				}
			}
			_findListView.EndUpdate();
			if (_findListView.Items.Count == 0)
				MessageBox.Show(LocalizationManager.GetLocalString("#Report/Search/NotFound"));
		}

		private void AddItemToList(string text, int row, int col)
		{
			if (text != null)
			{
				var item = new ListViewItem(text);
				item.SubItems.Add((row + 1).ToString());
				item.SubItems.Add((col + 1).ToString());
				_findListView.Items.Add(item);
			}
		}

		private void FindListViewSelectedIndexChanged(object sender, EventArgs e)
		{
			if (_findListView.SelectedItems.Count == 1)
			{
				var row = int.Parse(_findListView.SelectedItems[0].SubItems[1].Text) - 1;
				var col = int.Parse(_findListView.SelectedItems[0].SubItems[2].Text) - 1;
				_presenter.CurrentCell = new Core.CellLocation() { Row = row, Column = col };
			}
		}
	}
}
