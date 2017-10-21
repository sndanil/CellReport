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
				OnPropertyChanged(nameof(CurrentText));
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
					OnPropertyChanged(nameof(CurrentText));
					OnPropertyChanged(nameof(CurrentTemplate));
					OnPropertyChanged(nameof(IsTextContent));
					break;
			}
		}

		public string CurrentText
		{
			get
			{
				if (_manager == null || _presenter == null)
					return null;

				var content = _manager.GetCell(_presenter.CurrentCell)?.Content;
				if (content is TextContent)
					return ((TextContent) content).Text;

				return null;
			}
			set
			{
				if (_manager == null || _presenter == null)
					return;

				var content = _manager.GetCell(_presenter.CurrentCell)?.Content;
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

				var content = _manager.GetCell(_presenter.CurrentCell)?.Content;
				if (content is TextContent)
					return ((TextContent)content).Template;

				return null;
			}
			set
			{
				if (_manager == null || _presenter == null)
					return;

				var content = _manager.GetCell(_presenter.CurrentCell)?.Content;
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

				return _manager.GetCell(_presenter.CurrentCell)?.Content is TextContent;
			}
			set
			{
				if (_manager == null || _presenter == null)
					return;

				var content = _manager.GetCell(_presenter.CurrentCell)?.Content;
				if (content is TextContent)
				{
					OnPropertyChanged();
				}
			}
		}

	}
}
