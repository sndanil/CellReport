using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CellReport.Avalonia.Services;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Presenters;
using AvaloniaFontStyle = Avalonia.Media.FontStyle;
using CoreFontStyle = CellReport.Core.FontStyle;

namespace CellReport.Avalonia.Editing
{
	/// <summary>
	/// Инлайн-редактор значения ячейки поверх <see cref="PresentControl"/>.
	/// </summary>
	internal sealed class CellInlineEditor
	{
		private readonly PresentControl _owner;
		private readonly TextBox _textBox;

		private BaseDisplayPresenter? _presenter;
		private CellValueEditor? _editor;
		private CellSnapshot? _before;
		private CellLocation _location;
		private bool _active;
		private bool _closing;

		public CellInlineEditor(PresentControl owner, TextBox textBox)
		{
			_owner = owner;
			_textBox = textBox;

			_textBox.MaxLength = 4096;
			_textBox.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
			_textBox.LostFocus += OnLostFocus;
		}

		public bool IsActive => _active;

		public string EditorText
		{
			get => _textBox.Text ?? string.Empty;
			set => _textBox.Text = value ?? string.Empty;
		}

		public bool Begin(CellLocation location, string? initialText = null)
		{
			var presenter = _owner.Presenter;
			if (presenter == null || presenter.Manager.IsDesignMode)
				return false;

			var manager = presenter.Manager;
			if (!manager.IsCurrectLocation(location))
				return false;

			var editor = manager.CreateValueEditor(location);
			if (editor == null)
				return false;

			if (_active)
				Cancel();

			_presenter = presenter;
			_editor = editor;
			_before = CellSnapshot.Capture(manager, location);
			_location = location;
			_active = true;

			presenter.CurrentCell = location;
			presenter.EditingCell = location;
			presenter.Update();

			if (initialText == null)
			{
				string text;
				editor.TryGetText(out text);
				_textBox.Text = text;
			}
			else
				_textBox.Text = initialText;

			ApplyFont(manager, location);
			Reposition();

			_textBox.IsVisible = true;
			_textBox.Focus();
			_textBox.SelectAll();

			presenter.PropertyChanged += OnPresenterPropertyChanged;
			return true;
		}

		public void Commit()
		{
			if (!_active || _editor == null || _presenter == null)
				return;

			var manager = _presenter.Manager;
			var text = _textBox.Text ?? string.Empty;
			CellEditError error;
			if (!_editor.TrySetText(text, out error))
			{
				ShowError(error);
				return;
			}

			var before = _before;
			var after = CellSnapshot.Capture(manager, _location);
			var location = _location;
			EndEdit();

			if (before != null)
				_owner.OnCellEdited(new CellEditEventArgs(location, before, after));
		}

		public void Cancel()
		{
			if (!_active)
				return;

			EndEdit();
		}

		private void EndEdit()
		{
			_closing = true;
			try
			{
				_textBox.IsVisible = false;
				_active = false;
				if (_presenter != null)
				{
					_presenter.PropertyChanged -= OnPresenterPropertyChanged;
					_presenter.EditingCell = CellLocation.None;
					_presenter.Update();
				}
			}
			finally
			{
				_editor = null;
				_before = null;
				_presenter = null;
				_location = CellLocation.None;
				ClearError();
				_closing = false;
			}

			_owner.FocusCells();
		}

		private void OnLostFocus(object? sender, RoutedEventArgs e)
		{
			if (_closing || !_active)
				return;

			Commit();
			if (_active)
				_textBox.Focus();
		}

		private void OnKeyDown(object? sender, KeyEventArgs e)
		{
			if (!_active)
				return;

			if (e.Key == Key.Escape)
			{
				e.Handled = true;
				Cancel();
				return;
			}

			if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
			{
				e.Handled = true;
				Commit();
				return;
			}

			if (e.Key == Key.Tab)
			{
				e.Handled = true;
				var forward = !e.KeyModifiers.HasFlag(KeyModifiers.Shift);
				var manager = _presenter?.Manager;
				var from = _location;

				Commit();
				if (!_active && manager != null)
				{
					var next = manager.FindNextEditable(from, forward);
					if (!next.Equals(CellLocation.None))
						Begin(next);
				}
			}
		}

		private void OnPresenterPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (!_active || _presenter == null)
				return;

			if (e.PropertyName is nameof(BasePresenter.LeftColumn)
				or nameof(BasePresenter.TopRow)
				or nameof(BasePresenter.Scale))
				Reposition();
		}

		private void ApplyFont(CellManager manager, CellLocation location)
		{
			if (_presenter == null)
				return;

			var cell = manager.GetCell(location);
			var textContent = cell?.Content as TextBasedContent;
			var style = textContent?.GetActualStyle() as TextContentStyle;
			var font = style == null ? TextBasedContent.DefaultStyle.Font : style.Font;

			var size = UnitsConverter.ConvertValue(font.Size, GraphicsUnit.Point, GraphicsUnit.Pixel, 96) * _presenter.Scale;
			_textBox.FontSize = Math.Max(8, size);
			_textBox.FontFamily = new FontFamily(font.Family);
			_textBox.FontStyle = (font.Style & CoreFontStyle.Italic) != 0 ? AvaloniaFontStyle.Italic : AvaloniaFontStyle.Normal;
			_textBox.FontWeight = (font.Style & CoreFontStyle.Bold) != 0 ? FontWeight.Bold : FontWeight.Normal;
		}

		private void Reposition()
		{
			if (!_active || _presenter == null)
				return;

			var rect = _presenter.GetCellRect(_location);
			var parent = _textBox.Parent as Control;
			var bounds = parent?.Bounds ?? default;
			if (bounds.Width > 0 && bounds.Height > 0
				&& (rect.Right < 0 || rect.Bottom < 0 || rect.X > bounds.Width || rect.Y > bounds.Height))
			{
				Commit();
				return;
			}

			_textBox.Margin = new Thickness(rect.X, rect.Y, 0, 0);
			_textBox.Width = Math.Max(rect.Width, 48);
			_textBox.Height = Math.Max(rect.Height, 26);
		}

		private void ShowError(CellEditError error)
		{
			var message = ErrorText(error);
			_textBox.Classes.Set("invalid", true);
			ToolTip.SetTip(_textBox, message);
		}

		private void ClearError()
		{
			_textBox.Classes.Set("invalid", false);
			ToolTip.SetTip(_textBox, null);
		}

		private static string ErrorText(CellEditError error)
		{
			switch (error)
			{
				case CellEditError.InvalidNumber:
					return ReportLocalization.Get("Report/Edit/InvalidNumber");
				case CellEditError.InvalidDate:
					return ReportLocalization.Get("Report/Edit/InvalidDate");
				case CellEditError.TooLong:
					return ReportLocalization.Get("Report/Edit/TooLong");
				case CellEditError.Computed:
					return ReportLocalization.Get("Report/Edit/Computed");
				default:
					return ReportLocalization.Get("Report/Edit/NotEditable");
			}
		}
	}
}
