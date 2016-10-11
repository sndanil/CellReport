using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;
using CellReport.Presenters;
using System.ComponentModel;

namespace CellReport.Studio.ViewModel
{
    public class CurrentValues : NotifyPropertyChanged
    {
        private BaseDisplayPresenter _presenter;
        private CellManager _manager;
        private BaseContent _content;
        private TemplateSection _section;

        public CurrentValues()
        {
        }

        protected override void OnPropertyChanged(string name)
        {
            //System.Threading.Thread.Sleep(1000);
            base.OnPropertyChanged(name);
        }

        public CurrentValues(BaseDisplayPresenter presenter)
        {
            _presenter = presenter;
            _manager = presenter.Manager;
            presenter.PropertyChanged += PresenterPropertyChanged;
			_manager.PropertyChanged += ManagerPropertyChanged;
        }

        private void PresenterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CurrentCell":
                    OnPropertyChanged("CurrentCellStyle");
                    OnPropertyChanged("CurrentCellHeight");
                    OnPropertyChanged("CurrentCellWidth");
                    OnPropertyChanged("CurrentRowAutoHeight");
                    OnPropertyChanged("CurrentContents");
					OnPropertyChanged("CurrentColumnMarker");
                    Content = CurrentContents.FirstOrDefault();
                    break;
            }
        }

		private void ManagerPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "ColumnWidth":
					OnPropertyChanged("CurrentCellWidth");
					break;
				case "RowHeight":
					OnPropertyChanged("CurrentCellHeight");
					break;
			}
		}

        public IList<CellStyle> CellStyles
        {
            get
            {
                if (_presenter == null)
                    return new List<CellStyle>();
                var result = _presenter.Manager.GetStyles().Where(s => s.GetType() == typeof(CellStyle)).Select(s => (CellStyle)s).ToList();
                result.Insert(0, Cell.DefaultStyle);
                return result;
            }
            set
            {
                OnPropertyChanged("CellStyles");
            }
        }

        public IList<BaseStyle> AllStyles
        {
            get
            {
                if (_presenter == null)
                    return new List<BaseStyle>();
                return _presenter.Manager.GetStyles().ToList();
            }
            set
            {
                OnPropertyChanged("AllStyles");
            }
        }

        public CellStyle CurrentCellStyle
        {
            get 
            { 
                return (_presenter == null || _presenter.Manager.GetCell(_presenter.CurrentCell) == null) ? Cell.DefaultStyle : 
                    _presenter.Manager.GetCell(_presenter.CurrentCell).Style; 
            }
            set
            {
                if (value == null)
                    return;

                if (_presenter.HaveSelection())
                {
                    var start = _presenter.GetStartSelection();
                    var end = _presenter.GetEndSelection();
                    for (int row = start.Row; row <= end.Row; row++)
                        for (int col = start.Column; col <= end.Column; col++)
                        {
                            if (_presenter.Manager.GetCell(row, col) == null)
                                _presenter.Manager.CreateCell(row, col);
                            _presenter.Manager.GetCell(row, col).Style = value;
                        }
                }
                else
                {
                    if (_presenter.Manager.GetCell(_presenter.CurrentCell) == null)
                        _presenter.Manager.CreateCell(_presenter.CurrentCell);
                    _presenter.Manager.GetCell(_presenter.CurrentCell).Style = value;
                }
                _presenter.Update();
                OnPropertyChanged("CurrentCellStyle");
            }
        }

        public float CurrentCellHeight
        {
            get
            {
                return (_presenter == null) ? 0 : _presenter.Manager.GetRowHeight(_presenter.CurrentCell.Row);
            }
            set
            {
                _presenter.Manager.SetRowHeight(_presenter.CurrentCell.Row, value);
                _presenter.Update();
                OnPropertyChanged("CurrentCellHeight");
            }
        }

        public float CurrentCellWidth
        {
            get
            {
                return (_presenter == null) ? 0 : _presenter.Manager.GetColumnWidth(_presenter.CurrentCell.Column);
            }
            set
            {
                _presenter.Manager.SetColumnWidth(_presenter.CurrentCell.Column, value);
                _presenter.Update();
                OnPropertyChanged("CurrentCellWidth");
            }
        }

        public BaseContent Content
        {
            get { return _content; }
            set
            {
                _content = value;
                OnPropertyChanged("Content");
                OnPropertyChanged("CurrentStyles");
                OnPropertyChanged("CurrentStyle");
            }
        }

		private CellStyle _currCellEditStyle = null;
		public CellStyle CurrCellEditStyle
        {
            get { return _currCellEditStyle; }
            set { _currCellEditStyle = value; OnPropertyChanged("CurrCellEditStyle"); }
        }

        private TextContentStyle _currTextStyle = null;
        public TextContentStyle CurrTextStyle
        {
            get { return _currTextStyle; }
            set { _currTextStyle = value; OnPropertyChanged("CurrTextStyle"); }
        }

        public IList<BaseStyle> TextStyles
        {
            get { return _manager.GetStyles().Where(s => s.GetType() == typeof(TextContentStyle)).ToList(); }
            set { OnPropertyChanged("TextStyles"); }
        }

        private DateTimeContentStyle _currDateTimeStyle = null;
        public DateTimeContentStyle CurrDateTimeStyle
        {
            get { return _currDateTimeStyle; }
            set { _currDateTimeStyle = value; OnPropertyChanged("CurrDateTimeStyle"); }
        }

        public IList<BaseStyle> DateTimeStyles
        {
            get { return _manager.GetStyles().Where(s => s.GetType() == typeof(DateTimeContentStyle)).ToList(); }
            set { OnPropertyChanged("DateTimeStyles"); }
        }

        private DecimalContentStyle _currDecimalStyle = null;
        public DecimalContentStyle CurrDecimalStyle
        {
            get { return _currDecimalStyle; }
            set { _currDecimalStyle = value; OnPropertyChanged("CurrDecimalStyle"); }
        }

        public IList<BaseStyle> DecimalStyles
        {
            get { return _manager.GetStyles().Where(s => s.GetType() == typeof(DecimalContentStyle)).ToList(); }
            set { OnPropertyChanged("DecimalStyles"); }
        }

        public IList<BaseStyle> CurrentStyles
        {
            get 
            {
                if (_content == null)
                    return new List<BaseStyle>();
 
                if (_content.GetType() == typeof(TextContent))
                    return _manager.GetStyles().Where(s => s.GetType() == typeof(TextContentStyle)).ToList();
                if (_content.GetType() == typeof(DateTimeContent))
                    return _manager.GetStyles().Where(s => s.GetType() == typeof(DateTimeContentStyle)).ToList();
                if (_content.GetType() == typeof(DecimalContent))
                    return _manager.GetStyles().Where(s => s.GetType() == typeof(DecimalContentStyle)).ToList();

                return new List<BaseStyle>();
            }
            set
            {
                OnPropertyChanged("CurrentStyles");
            }
        }

        public BaseStyle CurrentStyle
        {
            get
            {
                if (_content == null)
                    return null;

				if ((_content.GetType() == typeof(TextContent)) ||
					(_content.GetType() == typeof(DateTimeContent)) ||
					(_content.GetType() == typeof(DecimalContent)))
                    return _content.GetStyle();

                return null;
            }
            set
            {
                if (_content == null || value == null)
                    return;

                if (_content.GetType() == typeof(TextContent))
                    SetStyle<TextContent>(value);
                if (_content.GetType() == typeof(DateTimeContent))
                    SetStyle<DateTimeContent>(value);
                if (_content.GetType() == typeof(DecimalContent))
                    SetStyle<DecimalContent>(value);
                _presenter.Update();
                OnPropertyChanged("CurrentStyle");
            }
        }

        public bool CurrentRowAutoHeight
        {
            get { return _presenter.Manager.GetAutoFitRows().Count(row => row == _presenter.CurrentCell.Row) > 0; }
            set 
            { 
                _presenter.Manager.SetAutoFitRow(_presenter.CurrentCell.Row, value);
                OnPropertyChanged("CurrentRowAutoHeight");
            }
        }

        private void SetStyle<T>(BaseStyle style)
            where T : BaseContent
        {
            if (_presenter.HaveSelection())
            {
                var start = _presenter.GetStartSelection();
                var end = _presenter.GetEndSelection();
                for (int row = start.Row; row <= end.Row; row++)
                    for (int col = start.Column; col <= end.Column; col++)
                    {
                        if (_presenter.Manager.GetCell(row, col) != null)
                        {
                            foreach (var content in _presenter.Manager.GetCell(row, col).GetContents().Where(c => c.GetType() == typeof(T)))
                                content.StyleID = style.ID;
                        }
                    }
            }
            else
            {
                if (_presenter.Manager.GetCell(_presenter.CurrentCell) != null)
                {
                    foreach (var content in _presenter.Manager.GetCell(_presenter.CurrentCell).GetContents().Where(c => c.GetType() == typeof(T)))
                        content.StyleID = style.ID;
                }
            }
        }

        public IList<BaseContent> CurrentContents
        {
            get
            {
                if (_manager == null || _manager.GetCell(_presenter.CurrentCell) == null
                    || _manager.GetCell(_presenter.CurrentCell).Content == null)
                    return new List<BaseContent>();

                var content = _manager.GetCell(_presenter.CurrentCell).Content;
                if (content is CollectionContent)
                    return ((CollectionContent)content).GetContents().OrderBy(c => c is TextBasedContent ? 0 : 1).ToList();
                else
                {
                    return new List<BaseContent>(new BaseContent[] { content });
                }
            }
            set
            {
                OnPropertyChanged("CurrentContents");
            }
        }

        public IList<TemplateSection> Sections
        {
            get
            {
                return _manager.GetSections().ToList();
            }
            set
            {
                OnPropertyChanged("Sections");
            }
        }

        public TemplateSection CurrentSection
        {
            get { return _section; }
            set
            {
                if (_section != value)
                {
                    if (_section != null)
                        _section.PropertyChanged -= SectionPropertyChanged;
                    _section = value;
                    if (_section != null)
                    {
                        _presenter.BeginUpdate();
                        _presenter.StartSelection = _section.LeftTop;
                        _presenter.EndSelection = _section.RightBottom;
                        _presenter.EndUpdate();
                        _section.PropertyChanged += SectionPropertyChanged;
                    }
                    OnPropertyChanged("CurrentSection");
                }
            }
        }

        void SectionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Sections = null;
        }

        public IList<BaseExpression> Expressions
        {
            get
            {
                return _manager.GetExpressions().ToList();
            }
            set
            {
                OnPropertyChanged("Expressions");
            }
        }

		public string CurrentColumnMarker
		{
			get { return _manager.GetColumnMarker(_presenter.CurrentCell.Column); }
			set
			{
				_manager.SetColumnMarker(_presenter.CurrentCell.Column, value);
				OnPropertyChanged("CurrentColumnMarker");
				_presenter.Update();
			}
		}

		public IList<string> Scales
		{
			get
			{
				return new List<string>()
				{
					"50%", "75%", "85%", "100%", "125%",
					"150%", "175%", "200%", "300%", "500%"
				};
			}
			set
			{
				OnPropertyChanged("Scales");
			}
		}

		public string CurrentScale
		{
			get { return (_presenter.Scale * 100).ToString() + "%"; }
			set
			{
				var strScale = string.IsNullOrEmpty(value) ? "100" : value.Replace("%", string.Empty);
				_presenter.Scale = float.Parse(strScale, CultureInfo.InvariantCulture) / 100f;
			}
		}
    }
}
