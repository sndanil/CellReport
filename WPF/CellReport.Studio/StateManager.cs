using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

using CellReport.Core;
using CellReport.Presenters.Wpf;
using CellReport.Studio.ViewModel;

namespace CellReport.Studio
{
	public static class StateManager
	{
		private const int MaxStatesCount = 20;

		private static List<Stream> _states = new List<Stream>();
		private static int _current = -1;
		private static DisplayPresenter _presenter = null;
		private static bool _updateMode = false;
		private static bool _hasChanges = false;

		public static void Init()
		{
			_updateMode = false;
			UpdatePresenter();
			Clear();
			AddCurrentState();
		}

		private static void UpdatePresenter()
		{
			if (_presenter != null)
			{
				_presenter.PropertyChanged -= Presenter_PropertyChanged;
				_presenter.Manager.PropertyChanged -= Manager_PropertyChanged;
			}

			_presenter = StudioCommands.Presenter;
			_presenter.PropertyChanged += Presenter_PropertyChanged;
			_presenter.Manager.PropertyChanged += Manager_PropertyChanged;
		}

		private static void Presenter_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != "State")
				return;
			if (_presenter.State != Presenters.PresenterState.Normal)
				BeginUpdate();
			else
			{
				EndUpdate();
				CommandManager.InvalidateRequerySuggested();
			}
		}

		private static void Manager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (!_updateMode)
				AddCurrentState();
			else
				_hasChanges = true;
		}

		public static void Clear()
		{
			foreach (var state in _states)
				state.Dispose();
			_states.Clear();
			_current = -1;
		}

		public static void AddCurrentState()
		{
			while (_states.Count - 1 > _current)
				RemoveState(_states.Count - 1);
			while (_states.Count >= MaxStatesCount)
				RemoveState(0);

			var template = new CellTemplate(_presenter.Manager);
			var ms = new MemoryStream();
			template.Save(ms, TemplateType.Package);
			_states.Add(ms);
			_current = _states.Count - 1;
		}

		public static bool CanUndo()
		{
			return (_current > 0);
		}

		public static void Undo()
		{
			if (!CanUndo())
				return;
			_current--;
			LoadCurrentState();
		}

		public static bool CanRedo()
		{
			return ((_current >= 0) && (_current < _states.Count - 1));
		}

		public static void Redo()
		{
			if (!CanRedo())
				return;
			_current++;
			LoadCurrentState();
		}

		public static void BeginUpdate()
		{
			_updateMode = true;
			_hasChanges = false;
		}

		public static void EndUpdate()
		{
			_updateMode = false;
			if (_hasChanges)
				AddCurrentState();
		}

		private static void LoadCurrentState()
		{
			var template = new CellTemplate();
			_states[_current].Position = 0;
			template.Load(_states[_current]);
			_presenter.Manager.PropertyChanged -= new PropertyChangedEventHandler(Manager_PropertyChanged);
			_presenter.Manager = template.Manager;
			_presenter.Manager.PropertyChanged += new PropertyChangedEventHandler(Manager_PropertyChanged);
			_presenter.Update();
			StudioCommands.Values = new CurrentValues(_presenter);
			StudioCommands.Updater.Update(false);
		}

		private static void RemoveState(int index)
		{
			_states[index].Dispose();
			_states.RemoveAt(index);
		}
	}
}
