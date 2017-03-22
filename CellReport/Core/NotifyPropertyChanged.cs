using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CellReport.Core
{
	[Serializable]
	public class NotifyPropertyChanged : INotifyPropertyChanged
	{
		[NonSerialized]
		private PropertyChangedEventHandler _propertyChanged;

		public event PropertyChangedEventHandler PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { if (_propertyChanged != null) _propertyChanged -= value; }
		}

		[NonSerialized]
		private Dictionary<string, object> _parameters;

		protected void SetParam(string name, object value)
		{
			object oldValue;

			if (_parameters == null)
				_parameters = new Dictionary<string, object>();

			_parameters.TryGetValue(name, out oldValue);
			_parameters[name] = value;

			if (!(oldValue == null && value == null))
				OnPropertyChanged(name);
			if (oldValue != null && value != null && oldValue.Equals(value))
				OnPropertyChanged(name);
		}

		protected T GetParam<T>(string name)
		{
			object value;

			if (_parameters != null && _parameters.TryGetValue(name, out value))
				return (T)value;

			return default(T);
		}

		protected void SetField<T>(ref T field, T value) where T : NotifyPropertyChanged
		{
			if (field != null)
				field.PropertyChanged -= OnPropertyChanged;
			field = value;
			if (field != null)
				field.PropertyChanged += OnPropertyChanged;
		}

		internal void RaisePropertyChanged(string name)
		{
			OnPropertyChanged(name);
		}

		protected virtual void OnPropertyChanged(string name)
		{
			OnPropertyChanged(new PropertyChangedEventArgs(name));
		}

		protected void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			OnPropertyChanged(this, e);
		}

		protected void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_propertyChanged != null)
				_propertyChanged(sender, e);
		}
	}
}
