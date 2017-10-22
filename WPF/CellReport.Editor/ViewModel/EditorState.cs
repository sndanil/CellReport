using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.Helpers;
using CellReport.Presenters.Wpf;

namespace CellReport.Editor.ViewModel
{
	public class EditorState: ObservableObject
	{
		public EditorState()
		{
		}

		private CellManager _manager;
		public CellManager Manager
		{
			get => _manager;
			set
			{
				SetProperty(ref _manager, value);
				OnPropertyChanged(nameof(CurrentCell));
				OnPropertyChanged(nameof(CellStyles));
			}
		}

		private DisplayPresenter _presenter;
		public DisplayPresenter Presenter
		{
			get => _presenter;
			set
			{
				if (_presenter != null)
					_presenter.PropertyChanged -= PresenterOnPropertyChanged;

				SetProperty(ref _presenter, value);
				_presenter.PropertyChanged += PresenterOnPropertyChanged;
			}
		}

		private void PresenterOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			switch (propertyChangedEventArgs.PropertyName)
			{
				case "CurrentCell":
					CurrentCell = CurrentCell;
					break;
			}
		}

		public CellLocation CurrentCell
		{
			get
			{
				if (_manager == null || _presenter == null)
					return CellLocation.None;

				return Presenter.CurrentCell;
			}
			set
			{
				if (_manager == null || _presenter == null)
					return;

				Presenter.CurrentCell = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CurrentCellStyle));
				OnPropertyChanged(nameof(CurrentText));
				OnPropertyChanged(nameof(CurrentTemplate));
				OnPropertyChanged(nameof(IsTextContent));
				Presenter.Update();
			}
		}

		public string CurrentText
		{
			get
			{
				if (_manager == null || _presenter == null)
					return null;

				var content = _manager.GetCell(CurrentCell)?.Content;
				if (content is CollectionContent)
					content = ((CollectionContent) content).GetContents().FirstOrDefault(c => c is TextContent);

				return (content as TextContent)?.Text;
			}
			set
			{
				if (_manager == null || _presenter == null)
					return;

				var content = _manager.GetCell(CurrentCell)?.Content;
				if (content is CollectionContent)
					content = ((CollectionContent)content).GetContents().FirstOrDefault(c => c is TextContent);

				if (content is TextContent)
				{
					((TextContent) content).Text = value;
					OnPropertyChanged();
					Presenter.Update();
				}
			}
		}

		public string CurrentTemplate
		{
			get
			{
				if (_manager == null || _presenter == null)
					return null;

				var content = _manager.GetCell(CurrentCell)?.Content;
				if (content is CollectionContent)
					content = ((CollectionContent)content).GetContents().FirstOrDefault(c => c is TextContent);

				return (content as TextContent)?.Template;
			}
			set
			{
				if (_manager == null || _presenter == null)
					return;

				var content = _manager.GetCell(CurrentCell)?.Content;
				if (content is CollectionContent)
					content = ((CollectionContent)content).GetContents().FirstOrDefault(c => c is TextContent);

				if (content is TextContent)
				{
					((TextContent)content).Template = value;
					OnPropertyChanged();
					Presenter.Update();
				}
			}
		}

		public bool IsTextContent
		{
			get
			{
				if (_manager == null || _presenter == null)
					return false;

				var content = _manager.GetCell(CurrentCell)?.Content;
				if (content is CollectionContent)
					content = ((CollectionContent)content).GetContents().FirstOrDefault(c => c is TextContent);

				return content is TextContent;
			}
			set
			{
				if (_manager == null || _presenter == null)
					return;

				var content = _manager.GetCell(CurrentCell)?.Content;
				if (content is CollectionContent)
					content = ((CollectionContent)content).GetContents().FirstOrDefault(c => c is TextContent);

				if (content is TextContent)
				{
					OnPropertyChanged();
				}
			}
		}

		public List<CellStyle> CellStyles
		{
			get
			{
				if (_manager == null)
					return new List<CellStyle>();

				var result = Manager.GetStyles().Where(s => s.GetType() == typeof(CellStyle)).Select(s => (CellStyle)s).ToList();
				result.Insert(0, Cell.DefaultStyle);

				return result;
			}
			set => OnPropertyChanged();
		}

		public CellStyle CurrentCellStyle
		{
			get => (CurrentCell.Equals(CellLocation.None) || Manager?.GetCell(CurrentCell) == null) ? Cell.DefaultStyle : Manager.GetCell(CurrentCell).Style;
			set
			{
				if (value == null || _presenter == null || _manager == null)
					return;

				if (Presenter.HaveSelection())
				{
					var start = Presenter.GetStartSelection();
					var end = Presenter.GetEndSelection();
					for (int row = start.Row; row <= end.Row; row++)
					for (int col = start.Column; col <= end.Column; col++)
					{
						if (Presenter.Manager.GetCell(row, col) == null)
							Presenter.Manager.CreateCell(row, col);
						Presenter.Manager.GetCell(row, col).Style = value;
					}
				}
				else
				{
					if (Manager.GetCell(CurrentCell) == null)
						Manager.CreateCell(CurrentCell);
					Manager.GetCell(CurrentCell).Style = value;
				}
				Presenter.Update();
				OnPropertyChanged();
			}
		}

	}
}
