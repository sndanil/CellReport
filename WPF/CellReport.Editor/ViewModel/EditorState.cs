using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.Helpers;
using CellReport.Presenters.Wpf;

namespace CellReport.Editor.ViewModel
{
	public class EditorState: ObservableObject
	{
		private const int MaxStatesCount = 2000;

		private static readonly List<Stream> States = new List<Stream>();
		private static int _current = -1;
		private static bool _updateMode;
		private static bool _hasChanges;

		public CellManager Manager => _presenter?.Manager;

		private void TotalUpdate()
		{
			ResetAllProperties();

			Task.Delay(500).ContinueWith(_ =>
			{
				var currentCell = CurrentCell;
				CurrentCell = CellLocation.None;
				CurrentCell = currentCell;
			}, TaskScheduler.FromCurrentSynchronizationContext());
		}

		private DisplayPresenter _presenter;
		public DisplayPresenter Presenter
		{
			get => _presenter;
			set
			{
				if (_presenter != null)
				{
					_presenter.PropertyChanged -= PresenterOnPropertyChanged;
					_presenter.Manager.PropertyChanged -= ManagerOnPropertyChanged;
				}

				SetProperty(ref _presenter, value);
				TotalUpdate();
				_presenter.PropertyChanged += PresenterOnPropertyChanged;
				_presenter.Manager.PropertyChanged += ManagerOnPropertyChanged;
			}
		}

		private void PresenterOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			switch (propertyChangedEventArgs.PropertyName)
			{
				case "CurrentCell":
					CurrentCell = CurrentCell;
					break;
				//case "State":
					//TotalUpdate();
				//	break;
			}
		}

		private void ManagerOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (_updateMode)
				_hasChanges = true;
			else
				AddCurrentState();

			switch (propertyChangedEventArgs.PropertyName)
			{
				case "ColumnWidth":
					OnPropertyChanged(nameof(CurrentCellWidth));
					break;
				case "RowHeight":
					OnPropertyChanged(nameof(CurrentCellHeight));
					break;
			}
		}

		private string _fileName;
		public string FileName
		{
			get => _fileName;
			set => SetProperty(ref _fileName, value);
		}

		public int Scale
		{
			get
			{
				if (_presenter == null)
					return 100;

				return Convert.ToInt32(Presenter.Scale * 100);
			}
			set
			{
				if (_presenter == null)
					return;

				Presenter.Scale = value / 100f;
				OnPropertyChanged();
				Presenter.Update();
			}
		}

		public CellLocation CurrentCell
		{
			get
			{
				if (_presenter == null)
					return CellLocation.None;

				return Presenter.CurrentCell;
			}
			set
			{
				if (_presenter == null)
					return;

				Presenter.CurrentCell = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CurrentCellStyle));
				OnPropertyChanged(nameof(CurrentTextStyle));
				OnPropertyChanged(nameof(CurrentTemplate));
				OnPropertyChanged(nameof(CurrentText));
				OnPropertyChanged(nameof(IsTextContent));
				//TotalUpdate();
				Presenter.Update();
			}
		}

		public float CurrentCellWidth
		{
			get
			{
				if (_presenter == null)
					return 0;

				return Manager.GetColumnWidth(Presenter.CurrentCell.Column);
			}
			set
			{
				if (_presenter == null)
					return;

				Presenter.Update();
			}
		}

		public float CurrentCellHeight
		{
			get
			{
				if (_presenter == null)
					return 0;

				return Manager.GetRowHeight(Presenter.CurrentCell.Row);
			}
			set
			{
				if (_presenter == null)
					return;

				Presenter.Update();
			}
		}

		private T GetContent<T>() where T : BaseContent
		{
			if (CurrentCell.Equals(CellLocation.None))
				return null;

			return GetContent<T>(Manager?.GetCell(CurrentCell));
		}

		private T GetContent<T>(Cell cell) where T : BaseContent
		{
			if (cell == null)
				return null;

			var content = cell.Content;
			if (content is CollectionContent)
				content = ((CollectionContent)content).GetContents().FirstOrDefault(c => c.GetType() == typeof(T));

			return content as T;
		}

		public string CurrentText
		{
			get
			{
				if (_presenter == null)
					return null;

				return GetContent<TextContent>()?.Text;
			}
			set
			{
				if (_presenter == null)
					return;

				var content = GetContent<TextContent>();
				if (content != null)
				{
					content.Text = value;
					OnPropertyChanged();
					Presenter.Update();
				}
			}
		}

		public string CurrentTemplate
		{
			get
			{
				if (_presenter == null)
					return null;

				return GetContent<TextContent>()?.Template;
			}
			set
			{
				if (_presenter == null)
					return;

				var content = GetContent<TextContent>();
				if (content != null)
				{
					content.Template = value;
					OnPropertyChanged();
					Presenter.Update();
				}
			}
		}

		public bool IsTextContent
		{
			get
			{
				if (_presenter == null)
					return false;

				return GetContent<TextContent>() != null;
			}
			set
			{
				if (_presenter == null)
					return;

				if (GetContent<TextContent>() != null)
					OnPropertyChanged();
			}
		}

		private readonly ObservableRangeCollection<CellStyle> _cellStyles = new ObservableRangeCollection<CellStyle>();
		public ObservableCollection<CellStyle> CellStyles
		{
			get
			{
				_cellStyles.Clear();
				if (Manager != null)
				{
					var result = Manager.GetStyles().Where(s => s.GetType() == typeof(CellStyle)).Select(s => (CellStyle)s).ToList();
					result.Insert(0, Cell.DefaultStyle);

					_cellStyles.AddRange(result);
				}

				return _cellStyles;
			}
			set => OnPropertyChanged();
		}

		public CellStyle CurrentCellStyle
		{
			get => (CurrentCell.Equals(CellLocation.None) || Manager?.GetCell(CurrentCell) == null) ? Cell.DefaultStyle : Manager.GetCell(CurrentCell).Style;
			set
			{
				if (value == null || _presenter == null || Manager == null)
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

		private readonly ObservableRangeCollection<TextContentStyle> _textStyles = new ObservableRangeCollection<TextContentStyle>();
		public ObservableCollection<TextContentStyle> TextStyles
		{
			get
			{
				_textStyles.Clear();
				if (Manager != null)
				{
					var result = Manager.GetStyles().Where(s => s.GetType() == typeof(TextContentStyle)).Select(s => (TextContentStyle)s).ToList();
					result.Insert(0, TextBasedContent.DefaultStyle);

					_textStyles.AddRange(result);
				}

				return _textStyles;
			}
			set => OnPropertyChanged();
		}

		public TextContentStyle CurrentTextStyle
		{
			get => GetContent<TextContent>()?.GetStyle() as TextContentStyle;
			set
			{
				if (value == null || _presenter == null || Manager == null)
					return;

				if (Presenter.HaveSelection())
				{
					var start = Presenter.GetStartSelection();
					var end = Presenter.GetEndSelection();
					for (int row = start.Row; row <= end.Row; row++)
					for (int col = start.Column; col <= end.Column; col++)
					{
						var content = GetContent<TextContent>(Presenter.Manager.GetCell(row, col));
						if (content != null)
							content.StyleID = value.ID;
					}
				}
				else
				{
					var content = GetContent<TextContent>();
					if (content != null)
						content.StyleID = value.ID;
				}
				Presenter.Update();
				OnPropertyChanged();
			}
		}

		#region Undo Redo
		public void Clear()
		{
			foreach (var state in States)
				state.Dispose();
			States.Clear();
			_current = -1;
		}

		private void RemoveState(int index)
		{
			States[index].Dispose();
			States.RemoveAt(index);
		}

		public void AddCurrentState()
		{
			while (States.Count - 1 > _current)
				RemoveState(States.Count - 1);
			while (States.Count >= MaxStatesCount)
				RemoveState(0);

			var template = new CellTemplate(_presenter.Manager);
			var ms = new MemoryStream();
			template.Save(ms, TemplateType.Package);
			States.Add(ms);
			_current = States.Count - 1;
		}

		public bool CanUndo()
		{
			return (_current > 0);
		}

		public void Undo()
		{
			if (!CanUndo())
				return;
			_current--;
			LoadCurrentState();
		}

		public bool CanRedo()
		{
			return _current >= 0 && (_current < States.Count - 1);
		}

		public void Redo()
		{
			if (!CanRedo())
				return;
			_current++;
			LoadCurrentState();
		}

		public void BeginUpdate()
		{
			_updateMode = true;
			_hasChanges = false;
			_presenter.BeginUpdate();
		}

		public void EndUpdate()
		{
			if (_hasChanges)
				AddCurrentState();
			_updateMode = false;
			_presenter.EndUpdate();
		}

		private void LoadCurrentState()
		{
			var template = new CellTemplate();
			States[_current].Position = 0;
			template.Load(States[_current]);
			_presenter.Manager.PropertyChanged -= ManagerOnPropertyChanged;
			_presenter.Manager = template.Manager;
			Presenter = _presenter;
			_presenter.Update();
		}

		#endregion
	}
}
