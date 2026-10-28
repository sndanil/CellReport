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
				if (_presenter != null)
				{
					TotalUpdate();
					_presenter.PropertyChanged += PresenterOnPropertyChanged;
					_presenter.Manager.PropertyChanged += ManagerOnPropertyChanged;
				}
			}
		}

		private void PresenterOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			switch (propertyChangedEventArgs.PropertyName)
			{
				case "CurrentCell":
					CurrentCell = CurrentCell;
					break;
				case "EndSelection":
					OnPropertyChanged(nameof(SelectionWidth));
					OnPropertyChanged(nameof(SelectionHeight));
					OnPropertyChanged(nameof(HaveSelection));
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

			if (sender is Param)
			{
				Presenter.Update();	
			}

			if (sender is TemplateSection)
			{
				CurrentSection = CurrentSection;
				return;
			}

			if (sender is CellManager)
			{
				switch (propertyChangedEventArgs.PropertyName)
				{
					case "ColumnWidth":
						OnPropertyChanged(nameof(CurrentCellWidth));
						OnPropertyChanged(nameof(SelectionWidth));
						break;
					case "RowHeight":
						OnPropertyChanged(nameof(CurrentCellHeight));
						OnPropertyChanged(nameof(SelectionHeight));
						break;
				}
			}
		}

		private string _fileName;
		public string FileName
		{
			get => _fileName;
			set
			{
				SetProperty(ref _fileName, value);
				ExternalStyleSearch.DocumentDirectory = string.IsNullOrEmpty(value)
					? null
					: Path.GetDirectoryName(Path.GetFullPath(value));
			}
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
				OnPropertyChanged(nameof(CurrentCellWidth));
				OnPropertyChanged(nameof(CurrentCellHeight));
				OnPropertyChanged(nameof(CurrentCellAutoHeight));
				OnPropertyChanged(nameof(CurrentCellMarkerForShow));
				OnPropertyChanged(nameof(CurrentCellMarkerForHide));
				OnPropertyChanged(nameof(CurrentCellStyle));
				OnPropertyChanged(nameof(CurrentTextStyle));
				OnPropertyChanged(nameof(CurrentDecimalStyle));
				OnPropertyChanged(nameof(CurrentDateTimeStyle));
				OnPropertyChanged(nameof(CurrentText));
				OnPropertyChanged(nameof(CurrentAutoFitFont));
				OnPropertyChanged(nameof(CurrentCustomFontSize));
				OnPropertyChanged(nameof(CurrentCustomFontFamily));
				OnPropertyChanged(nameof(CurrentCustomFontBold));
				OnPropertyChanged(nameof(CurrentCustomFontItalic));
				OnPropertyChanged(nameof(CurrentCustomFontUnderline));
				OnPropertyChanged(nameof(CurrentCustomFontStrikeout));
				OnPropertyChanged(nameof(CurrentAlignTop));
				OnPropertyChanged(nameof(CurrentAlignMiddle));
				OnPropertyChanged(nameof(CurrentAlignBottom));
				OnPropertyChanged(nameof(CurrentAlignLeft));
				OnPropertyChanged(nameof(CurrentAlignCenter));
				OnPropertyChanged(nameof(CurrentAlignRight));
				OnPropertyChanged(nameof(HasStyledContent));
				OnPropertyChanged(nameof(IsTextableContent));
				OnPropertyChanged(nameof(IsTextContent));
				OnPropertyChanged(nameof(IsDecimalContent));
				OnPropertyChanged(nameof(IsDateTimeContent));
				OnPropertyChanged(nameof(IsBarCodeContent));
				OnPropertyChanged(nameof(IsImageContent));
				OnPropertyChanged(nameof(IsActionContent));
				OnPropertyChanged(nameof(HasContent));
				OnPropertyChanged(nameof(CurrentContents));

				CurrentParam = null;
				CurrentContent = null;
				if (HasContent)
				{
					CurrentContent = CurrentContents?.Count > 1 ? CurrentContents?.FirstOrDefault(c => !(c is ActionContent)) : CurrentContents?.FirstOrDefault();
					if (ContentParams?.Count  > 0)
						CurrentParam = ContentParams.FirstOrDefault();
				}

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

				OnPropertyChanged(nameof(SelectionWidth));
				Manager.SetColumnWidth(Presenter.CurrentCell.Column, value);
				Presenter.Update();
				OnPropertyChanged();
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

				OnPropertyChanged(nameof(SelectionHeight));
				Manager.SetRowHeight(Presenter.CurrentCell.Row, value);
				Presenter.Update();
				OnPropertyChanged();
			}
		}

		public bool CurrentCellAutoHeight
		{
			get
			{
				if (_presenter == null)
					return false;

				return Manager.GetAutoFitRows().Any(r => r == CurrentCell.Row);
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged(nameof(CurrentCellAutoHeight));
				Manager.SetAutoFitRow(Presenter.CurrentCell.Row, value);
				Presenter.Update();
				OnPropertyChanged();
			}
		}

		public string CurrentCellMarkerForShow
		{
			get
			{
				if (_presenter == null)
					return null;

				return Manager.GetColumnMarkerForShow(CurrentCell.Column);
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged(nameof(CurrentCellMarkerForShow));
				Manager.SetColumnMarkerForShow(Presenter.CurrentCell.Column, string.IsNullOrEmpty(value) ? null : value);
				Presenter.Update();
				OnPropertyChanged();
			}
		}

		public string CurrentCellMarkerForHide
		{
			get
			{
				if (_presenter == null)
					return null;

				return Manager.GetColumnMarkerForHide(CurrentCell.Column);
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged(nameof(CurrentCellMarkerForHide));
				Manager.SetColumnMarkerForHide(Presenter.CurrentCell.Column, string.IsNullOrEmpty(value) ? null : value);
				Presenter.Update();
				OnPropertyChanged();
			}
		}

		public bool HaveSelection
		{
			get
			{
				if (Presenter == null)
					return false;

				if (Presenter.HaveSelection())
					return true;

				return Manager.GetCell(Presenter.CurrentCell)?.Span.ColSpan > 0 || Manager.GetCell(Presenter.CurrentCell)?.Span.RowSpan > 0;
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged();
			}
		}

		public float SelectionWidth
		{
			get
			{
				if (_presenter == null)
					return 0;

				if (Presenter.HaveSelection())
				{
					float width = 0;
					var start = EditorModel.State.Presenter.GetStartSelection();
					var end = EditorModel.State.Presenter.GetEndSelection();
					for (int i = start.Column; i <= end.Column; i++)
					{
						width += Manager.GetColumnWidth(i);
					}
					return width;
				}

				var cell = Manager.GetCell(CurrentCell);
				if (cell != null)
				{
					float width = 0;
					for (int i = cell.Location.Column; i <= cell.Location.Column + cell.Span.ColSpan; i++)
					{
						width += Manager.GetColumnWidth(i);
					}
					return width;
				}

				return CurrentCellWidth;
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged();
			}
		}

		public float SelectionHeight
		{
			get
			{
				if (_presenter == null)
					return 0;

				if (Presenter.HaveSelection())
				{
					float height = 0;
					var start = EditorModel.State.Presenter.GetStartSelection();
					var end = EditorModel.State.Presenter.GetEndSelection();
					for (int i = start.Row; i <= end.Row; i++)
					{
						height += Manager.GetRowHeight(i);
					}
					return height;
				}

				var cell = Manager.GetCell(CurrentCell);
				if (cell != null)
				{
					float height = 0;
					for (int i = cell.Location.Row; i <= cell.Location.Row + cell.Span.RowSpan; i++)
					{
						height += Manager.GetRowHeight(i);
					}
					return height;
				}

				return CurrentCellHeight;
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged();
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

		private T GetBaseContent<T>(Cell cell) where T : BaseContent
		{
			if (cell == null)
				return null;

			var content = cell.Content;
			if (content is CollectionContent)
				content = ((CollectionContent)content).GetContents().FirstOrDefault(c => c.GetType().IsSubclassOf(typeof(T)));

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

		public bool CurrentAutoFitFont
		{
			get
			{
				if (_presenter == null)
					return false;

				return GetContent<TextContent>()?.AutoSizeFont == true;
			}
			set
			{
				if (_presenter == null)
					return;

				var content = GetContent<TextContent>();
				if (content != null)
				{
					content.AutoSizeFont = value;
					OnPropertyChanged();
					Presenter.Update();
				}
			}
		}

		public string CurrentCustomFontSize
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return null;

				var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
				if (content != null)
				{
					var param = content.GetParams().FirstOrDefault(p => p.Target == "Style.Font.Size");
					if (param != null)
						return param.Value as string;

					return Convert.ToInt32(((TextContentStyle)content.GetStyle()).Font.Size).ToString();
				}

				return "";
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
				if (content != null)
				{

					var param = ContentParams?.FirstOrDefault(p => p.Target == "Style.Font.Size");
					if (value == "" || value == Convert.ToInt32(((TextContentStyle)content.GetStyle()).Font.Size).ToString())
					{
						if (param != null)
						{
							content.RemoveParam(param);
							ContentParams = ContentParams;
						}
					}
					else
					{
						if (param != null)
						{
							param.Value = value;
						}
						else
						{
							param = new Param(EditorModel.State.Manager, content) { Name = "Шрифт.Размер", Target = "Style.Font.Size", Value = value };
							content.AddParam(param);
							ContentParams = ContentParams;
						}
					}
				}

				OnPropertyChanged();
				Presenter.Update();
			}
		}

		public string CurrentCustomFontFamily
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return null;

				var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
				if (content != null)
				{
					var param = content.GetParams().FirstOrDefault(p => p.Target == "Style.Font.Family");
					if (param != null)
						return param.Value as string;

					return ((TextContentStyle)content.GetStyle()).Font.Family;
				}

				return "";
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
				if (content != null)
				{

					var param = ContentParams?.FirstOrDefault(p => p.Target == "Style.Font.Family");
					if (value == "" || value == ((TextContentStyle)content.GetStyle()).Font.Family)
					{
						if (param != null)
						{
							content.RemoveParam(param);
							ContentParams = ContentParams;
						}
					}
					else
					{
						if (param != null)
						{
							param.Value = value;
						}
						else
						{
							param = new Param(EditorModel.State.Manager, content) { Name = "Шрифт", Target = "Style.Font.Family", Value = value };
							content.AddParam(param);
							ContentParams = ContentParams;
						}
					}
				}

				OnPropertyChanged();
				Presenter.Update();
			}
		}

		public bool CurrentCustomFontBold
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return GetFontStylePartState(nameof(FontStyle.Bold), FontStyle.Bold);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetFontStylePartState(value, nameof(FontStyle.Bold), FontStyle.Bold);

				OnPropertyChanged();
				Presenter.Update();
			}
		}

		public bool CurrentCustomFontItalic
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return GetFontStylePartState(nameof(FontStyle.Italic), FontStyle.Italic);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetFontStylePartState(value, nameof(FontStyle.Italic), FontStyle.Italic);

				OnPropertyChanged();
				Presenter.Update();
			}
		}

		public bool CurrentCustomFontUnderline
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return GetFontStylePartState(nameof(FontStyle.Underline), FontStyle.Underline);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetFontStylePartState(value, nameof(FontStyle.Underline), FontStyle.Underline);

				OnPropertyChanged();
				Presenter.Update();
			}
		}

		public bool CurrentCustomFontStrikeout
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return GetFontStylePartState(nameof(FontStyle.Strikeout), FontStyle.Strikeout);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetFontStylePartState(value, nameof(FontStyle.Strikeout), FontStyle.Strikeout);

				OnPropertyChanged();
				Presenter.Update();
			}
		}

		private void SetFontStylePartState(bool value, string name, FontStyle part)
		{
			var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
			if (content != null)
			{

				var param = ContentParams?.FirstOrDefault(p => p.Target == "Style.Font.Style");
				if (value == ((TextContentStyle)content.GetStyle()).Font.Style.HasFlag(part))
				{
					if (param != null)
					{
						var paramValue = param.Value as string;
						if (paramValue?.Contains(name) == true)
						{
							paramValue = paramValue?.Replace($",{name}", "").Trim();
							if (Enum.TryParse(paramValue, out FontStyle style) && style == ((TextContentStyle)content.GetStyle()).Font.Style
								|| string.IsNullOrEmpty(paramValue) || paramValue == nameof(FontStyle.Regular))
							{
								content.RemoveParam(param);
								ContentParams = ContentParams;
							}
							else
							{
								param.Value = paramValue;
							}
						}
						else
						{
							param.Value = param.Value + $",{name}";
						}
					}
				}
				else
				{
					if (param != null)
					{
						if (value)
							param.Value = param.Value + $",{name}";
						else
						{
							var paramValue = param.Value as string;
							paramValue = paramValue?.Replace($",{name}", "").Trim();
							param.Value = paramValue;
						}
					}
					else
					{
						param = new Param(EditorModel.State.Manager, content) { Name = "Шрифт.Стиль", Target = "Style.Font.Style" };
						param.Value = ((TextContentStyle)content.GetStyle()).Font.Style == FontStyle.Regular ? 
							((TextContentStyle)content.GetStyle()).Font.Style.ToString()
							: $"{nameof(FontStyle.Regular)},{((TextContentStyle)content.GetStyle()).Font.Style}";
						if (value)
							param.Value = param.Value + $",{name}";
						else
						{
							var paramValue = param.Value as string;
							paramValue = paramValue?.Replace($",{name}", "").Trim();
							param.Value = paramValue;
						}

						content.AddParam(param);
						ContentParams = ContentParams;
					}
				}
			}
		}

		private bool GetFontStylePartState(string name, FontStyle part)
		{
			var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
			if (content != null)
			{
				var param = content.GetParams().FirstOrDefault(p => p.Target == "Style.Font.Style");
				if (param != null)
					return (param.Value as string)?.Contains(name) == true;

				return ((TextContentStyle)content.GetStyle()).Font.Style.HasFlag(part);
			}

			return false;
		}

		public bool CurrentAlignTop
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return HaveVerticalAlignment(VerticalAlignment.Top);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetVerticalAlignment(VerticalAlignment.Top, value);
				OnPropertyChanged();
				OnPropertyChanged(nameof(CurrentAlignMiddle));
				OnPropertyChanged(nameof(CurrentAlignBottom));
				Presenter.Update();
			}
		}

		public bool CurrentAlignMiddle
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return HaveVerticalAlignment(VerticalAlignment.Middle);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetVerticalAlignment(VerticalAlignment.Middle, value);
				OnPropertyChanged();
				OnPropertyChanged(nameof(CurrentAlignTop));
				OnPropertyChanged(nameof(CurrentAlignBottom));
				Presenter.Update();
			}
		}

		public bool CurrentAlignBottom
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return HaveVerticalAlignment(VerticalAlignment.Bottom);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetVerticalAlignment(VerticalAlignment.Bottom, value);
				OnPropertyChanged();
				OnPropertyChanged(nameof(CurrentAlignTop));
				OnPropertyChanged(nameof(CurrentAlignMiddle));
				Presenter.Update();
			}
		}

		public bool CurrentAlignLeft
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return HaveHorizontalAlignment(HorizontalAlignment.Left);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetHorizontalAlignment(HorizontalAlignment.Left, value);
				OnPropertyChanged();
				OnPropertyChanged(nameof(CurrentAlignCenter));
				OnPropertyChanged(nameof(CurrentAlignRight));
				Presenter.Update();
			}
		}

		public bool CurrentAlignCenter
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return HaveHorizontalAlignment(HorizontalAlignment.Center);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetHorizontalAlignment(HorizontalAlignment.Center, value);
				OnPropertyChanged();
				OnPropertyChanged(nameof(CurrentAlignLeft));
				OnPropertyChanged(nameof(CurrentAlignRight));
				Presenter.Update();
			}
		}

		public bool CurrentAlignRight
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return false;

				return HaveHorizontalAlignment(HorizontalAlignment.Right);
			}
			set
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return;

				SetHorizontalAlignment(HorizontalAlignment.Right, value);
				OnPropertyChanged();
				OnPropertyChanged(nameof(CurrentAlignLeft));
				OnPropertyChanged(nameof(CurrentAlignCenter));
				Presenter.Update();
			}
		}

		private bool HaveVerticalAlignment(VerticalAlignment alignment)
		{
			var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
			if (content != null)
			{
				var param = content.GetParams().FirstOrDefault(p => p.Target == "Style.VAlignment");
				if (Enum.TryParse(param?.Value as string, out VerticalAlignment align))
					return align == alignment;

				return ((TextContentStyle)content.GetStyle()).VAlignment == alignment;
			}

			return false;
		}

		private void SetVerticalAlignment(VerticalAlignment alignment, bool value)
		{
			var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
			if (content != null)
			{

				var param = ContentParams?.FirstOrDefault(p => p.Target == "Style.VAlignment");
				if ((((TextContentStyle)content.GetStyle()).VAlignment == alignment) == value)
				{
					if (param != null)
					{
						content.RemoveParam(param);
						ContentParams = ContentParams;
					}
				}
				else
				{
					if (param != null)
					{
						param.Value = alignment.ToString();
					}
					else
					{
						param = new Param(EditorModel.State.Manager, content) { Name = "Верт.Выравнивание", Target = "Style.VAlignment", Value = alignment.ToString() };
						content.AddParam(param);
						ContentParams = ContentParams;
					}
				}
			}
		}

		private bool HaveHorizontalAlignment(HorizontalAlignment alignment)
		{
			var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
			if (content != null)
			{
				var param = content.GetParams().FirstOrDefault(p => p.Target == "Style.HAlignment");
				if (Enum.TryParse(param?.Value as string, out HorizontalAlignment align))
					return align == alignment;

				return ((TextContentStyle)content.GetStyle()).HAlignment == alignment;
			}

			return false;
		}

		private void SetHorizontalAlignment(HorizontalAlignment alignment, bool value)
		{
			var content = GetBaseContent<TextBasedContent>(Manager?.GetCell(CurrentCell));
			if (content != null)
			{

				var param = ContentParams?.FirstOrDefault(p => p.Target == "Style.HAlignment");
				if ((((TextContentStyle)content.GetStyle()).HAlignment == alignment) == value)
				{
					if (param != null)
					{
						content.RemoveParam(param);
						ContentParams = ContentParams;
					}
				}
				else
				{
					if (param != null)
					{
						param.Value = alignment.ToString();
					}
					else
					{
						param = new Param(EditorModel.State.Manager, content) { Name = "Гор.Выравнивание", Target = "Style.HAlignment", Value = alignment.ToString() };
						content.AddParam(param);
						ContentParams = ContentParams;
					}
				}
			}
		}

		private void UpdateFontAndAlignStates()
		{
			OnPropertyChanged(nameof(CurrentCustomFontSize));
			OnPropertyChanged(nameof(CurrentCustomFontFamily));
			OnPropertyChanged(nameof(CurrentCustomFontBold));
			OnPropertyChanged(nameof(CurrentCustomFontItalic));
			OnPropertyChanged(nameof(CurrentCustomFontUnderline));
			OnPropertyChanged(nameof(CurrentCustomFontStrikeout));
			OnPropertyChanged(nameof(CurrentAlignTop));
			OnPropertyChanged(nameof(CurrentAlignMiddle));
			OnPropertyChanged(nameof(CurrentAlignBottom));
			OnPropertyChanged(nameof(CurrentAlignLeft));
			OnPropertyChanged(nameof(CurrentAlignCenter));
			OnPropertyChanged(nameof(CurrentAlignRight));
		}

		public bool HasStyledContent
		{
			get
			{
				if (_presenter == null)
					return false;

				return IsTextContent || IsDecimalContent || IsDateTimeContent;
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged();
			}
		}

		public bool HasContent
		{
			get
			{
				if (_presenter == null)
					return false;

				return Manager?.GetCell(CurrentCell)?.Content != null;
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged();
			}
		}

		public ObservableCollection<BaseContent> CurrentContents
		{
			get
			{
				if (_presenter == null || Manager?.GetCell(CurrentCell)?.Content == null)
					return null;

				var contents = Manager?.GetCell(CurrentCell)?.Content?.GetParams();

				if (Manager?.GetCell(CurrentCell)?.Content is CollectionContent collectionContent)
					return new ObservableCollection<BaseContent>(collectionContent.GetContents());

				return new ObservableCollection<BaseContent>(new [] { Manager?.GetCell(CurrentCell)?.Content });
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged();
			}
		}

		private BaseContent _currentContent;
		public BaseContent CurrentContent
		{
			get => _currentContent;
			set
			{
				SetProperty(ref _currentContent, value);
				OnPropertyChanged(nameof(ContentParams));
				CurrentParam = null;
			}
		}

		public ObservableCollection<Param> ContentParams
		{
			get
			{
				if (_presenter == null)
					return null;

				var @params = CurrentContent?.GetParams();

				if (@params != null)
					return new ObservableCollection<Param>(@params);

				return null;
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged();
			}
		}

		private Param _currentParam;
		public Param CurrentParam
		{
			get => _currentParam;
			set => SetProperty(ref _currentParam, value);
		}

		public bool IsTextableContent
		{
			get
			{
				if (_presenter == null)
					return false;

				return IsTextContent || IsDateTimeContent || IsDecimalContent;
			}
			set
			{
				if (_presenter == null)
					return;

				OnPropertyChanged();
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

		public bool IsDecimalContent
		{
			get
			{
				if (_presenter == null)
					return false;

				return GetContent<DecimalContent>() != null;
			}
			set
			{
				if (_presenter == null)
					return;

				if (GetContent<DecimalContent>() != null)
					OnPropertyChanged();
			}
		}

		public bool IsDateTimeContent
		{
			get
			{
				if (_presenter == null)
					return false;

				return GetContent<DateTimeContent>() != null;
			}
			set
			{
				if (_presenter == null)
					return;

				if (GetContent<DateTimeContent>() != null)
					OnPropertyChanged();
			}
		}

		public bool IsBarCodeContent
		{
			get
			{
				if (_presenter == null)
					return false;

				return GetContent<BarCodeContent>() != null;
			}
			set
			{
				if (_presenter == null)
					return;

				if (GetContent<BarCodeContent>() != null)
					OnPropertyChanged();
			}
		}

		public bool IsImageContent
		{
			get
			{
				if (_presenter == null)
					return false;

				return GetContent<ImageContent>() != null;
			}
			set
			{
				if (_presenter == null)
					return;

				if (GetContent<ImageContent>() != null)
					OnPropertyChanged();
			}
		}

		public bool IsActionContent
		{
			get
			{
				if (_presenter == null)
					return false;

				return GetContent<ActionContent>() != null;
			}
			set
			{
				if (_presenter == null)
					return;

				if (GetContent<ActionContent>() != null)
					OnPropertyChanged();
			}
		}

		private readonly ObservableRangeCollection<TemplateSection> _sections = new ObservableRangeCollection<TemplateSection>();
		public ObservableCollection<TemplateSection> Sections
		{
			get
			{
				_sections.Clear();
				if (Manager != null)
				{
					var result = Manager.GetSections().ToList();
					_sections.AddRange(result);
				}

				return _sections;
			}
			set => OnPropertyChanged();
		}

		private TemplateSection _currentSection;
		public TemplateSection CurrentSection
		{
			get => _currentSection;
			set
			{
				SetProperty(ref _currentSection, value);

				if (value != null)
				{
					Presenter.BeginUpdate();
					Presenter.StartSelection = value.LeftTop;
					Presenter.EndSelection = value.RightBottom;
					Presenter.EndUpdate();
				}
				else
				{
					Presenter.ClearSelection();
				}
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

		private readonly ObservableRangeCollection<DecimalContentStyle> _decimalStyles = new ObservableRangeCollection<DecimalContentStyle>();
		public ObservableCollection<DecimalContentStyle> DecimalStyles
		{
			get
			{
				_decimalStyles.Clear();
				if (Manager != null)
				{
					var result = Manager.GetStyles().Where(s => s.GetType() == typeof(DecimalContentStyle)).Select(s => (DecimalContentStyle)s).ToList();
					result.Insert(0, DecimalContent.DefaultStyle);

					_decimalStyles.AddRange(result);
				}

				return _decimalStyles;
			}
			set => OnPropertyChanged();
		}

		private readonly ObservableRangeCollection<DateTimeContentStyle> _dateTimeStyles = new ObservableRangeCollection<DateTimeContentStyle>();
		public ObservableCollection<DateTimeContentStyle> DateTimeStyles
		{
			get
			{
				_dateTimeStyles.Clear();
				if (Manager != null)
				{
					var result = Manager.GetStyles().Where(s => s.GetType() == typeof(DateTimeContentStyle)).Select(s => (DateTimeContentStyle)s).ToList();
					result.Insert(0, DateTimeContent.DefaultStyle);

					_dateTimeStyles.AddRange(result);
				}

				return _dateTimeStyles;
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
				UpdateFontAndAlignStates();
			}
		}

		public DecimalContentStyle CurrentDecimalStyle
		{
			get => GetContent<DecimalContent>()?.GetStyle() as DecimalContentStyle;
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
						var content = GetContent<DecimalContent>(Presenter.Manager.GetCell(row, col));
						if (content != null)
							content.StyleID = value.ID;
					}
				}
				else
				{
					var content = GetContent<DecimalContent>();
					if (content != null)
						content.StyleID = value.ID;
				}
				Presenter.Update();
				OnPropertyChanged();
				UpdateFontAndAlignStates();
			}
		}

		public DateTimeContentStyle CurrentDateTimeStyle
		{
			get => GetContent<DateTimeContent>()?.GetStyle() as DateTimeContentStyle;
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
						var content = GetContent<DateTimeContent>(Presenter.Manager.GetCell(row, col));
						if (content != null)
							content.StyleID = value.ID;
					}
				}
				else
				{
					var content = GetContent<DateTimeContent>();
					if (content != null)
						content.StyleID = value.ID;
				}
				Presenter.Update();
				OnPropertyChanged();
				UpdateFontAndAlignStates();
			}
		}

		public ObservableCollection<string> Fonts { get; } = new ObservableCollection<string>(new[] { "" }.Concat(System.Windows.Media.Fonts.SystemFontFamilies.Select(fontFamily => fontFamily.ToString())));

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
			template.Manager.IsDesignMode = true;
			_presenter.Manager.PropertyChanged -= ManagerOnPropertyChanged;
			_presenter.Manager = template.Manager;
			Presenter = _presenter;
			_presenter.Update();
		}

		#endregion
	}
}
