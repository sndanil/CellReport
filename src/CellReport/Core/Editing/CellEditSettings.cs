using System;

namespace CellReport.Core
{
	/// <summary>
	/// Runtime-настройки заполнения ячеек. В XML шаблона не сериализуются:
	/// по умолчанию шаблон всегда открывается только для чтения.
	/// </summary>
	[Serializable]
	public class CellEditSettings : NotifyPropertyChanged
	{
		private CellEditMode _mode;
		private bool _showFrame = true;
		private bool _showBackground;
		private bool _showPlaceholder = true;
		private bool _showTooltip = true;
		private string _frameColor = "#FF43A047";
		private string _backgroundColor = "#1A43A047";
		private string _placeholderColor = "#FF9E9E9E";
		private string _defaultPlaceholder = "Введите значение";
		private string _tooltipFormat = "Можно заполнить: {0}";

		public CellEditMode Mode
		{
			get { return _mode; }
			set
			{
				if (_mode == value)
					return;
				_mode = value;
				OnPropertyChanged("Mode");
			}
		}

		public bool ShowFrame
		{
			get { return _showFrame; }
			set
			{
				if (_showFrame == value)
					return;
				_showFrame = value;
				OnPropertyChanged("ShowFrame");
			}
		}

		public bool ShowBackground
		{
			get { return _showBackground; }
			set
			{
				if (_showBackground == value)
					return;
				_showBackground = value;
				OnPropertyChanged("ShowBackground");
			}
		}

		public bool ShowPlaceholder
		{
			get { return _showPlaceholder; }
			set
			{
				if (_showPlaceholder == value)
					return;
				_showPlaceholder = value;
				OnPropertyChanged("ShowPlaceholder");
			}
		}

		public bool ShowTooltip
		{
			get { return _showTooltip; }
			set
			{
				if (_showTooltip == value)
					return;
				_showTooltip = value;
				OnPropertyChanged("ShowTooltip");
			}
		}

		public string FrameColor
		{
			get { return _frameColor; }
			set
			{
				if (_frameColor == value)
					return;
				_frameColor = value;
				OnPropertyChanged("FrameColor");
			}
		}

		public string BackgroundColor
		{
			get { return _backgroundColor; }
			set
			{
				if (_backgroundColor == value)
					return;
				_backgroundColor = value;
				OnPropertyChanged("BackgroundColor");
			}
		}

		public string PlaceholderColor
		{
			get { return _placeholderColor; }
			set
			{
				if (_placeholderColor == value)
					return;
				_placeholderColor = value;
				OnPropertyChanged("PlaceholderColor");
			}
		}

		public string DefaultPlaceholder
		{
			get { return _defaultPlaceholder; }
			set
			{
				if (_defaultPlaceholder == value)
					return;
				_defaultPlaceholder = value;
				OnPropertyChanged("DefaultPlaceholder");
			}
		}

		public string TooltipFormat
		{
			get { return _tooltipFormat; }
			set
			{
				if (_tooltipFormat == value)
					return;
				_tooltipFormat = value;
				OnPropertyChanged("TooltipFormat");
			}
		}
	}
}
