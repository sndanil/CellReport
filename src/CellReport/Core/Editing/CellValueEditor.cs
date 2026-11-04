using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using CellReport.Core.Contents;

namespace CellReport.Core
{
	/// <summary>
	/// Разрешает «слот значения» редактируемой ячейки и читает/пишет его.
	/// Слот — либо параметр контента (тогда разметка шаблона не меняется),
	/// либо литеральный текст <see cref="TextContent"/>.
	/// </summary>
	public sealed class CellValueEditor
	{
		private const int MaxTextLength = 4096;
		private static readonly Regex PlaceholderRegex = new Regex(@"\[([^\[\]]+)\]", RegexOptions.Compiled);

		private readonly CellManager _manager;
		private readonly CellLocation _location;

		private CellEditValueKind _kind;
		private CellEditError _error;
		private TextContent _textContent;
		private BaseContent _owner;
		private Param _param;
		private string _paramName;
		private bool _literal;

		private CellValueEditor(CellManager manager, CellLocation location)
		{
			_manager = manager;
			_location = location;
		}

		public CellLocation Location
		{
			get { return _location; }
		}

		public CellEditValueKind Kind
		{
			get { return _kind; }
		}

		public CellEditError Error
		{
			get { return _error; }
		}

		public bool IsEditable
		{
			get { return _error == CellEditError.None; }
		}

		public string Hint
		{
			get
			{
				var cell = SafeCell();
				if (cell != null && !string.IsNullOrEmpty(cell.EditPlaceholder))
					return cell.EditPlaceholder;
				if (!string.IsNullOrEmpty(_paramName))
					return _paramName;
				var placeholder = _manager.EditSettings.DefaultPlaceholder;
				return string.IsNullOrEmpty(placeholder) ? null : placeholder;
			}
		}

		public string Tooltip
		{
			get
			{
				var cell = SafeCell();
				if (cell != null && !string.IsNullOrEmpty(cell.EditHint))
					return cell.EditHint;

				var hint = Hint;
				if (string.IsNullOrEmpty(hint))
					return null;

				var format = _manager.EditSettings.TooltipFormat;
				if (string.IsNullOrEmpty(format))
					return hint;

				try
				{
					return string.Format(format, hint);
				}
				catch (FormatException)
				{
					return hint;
				}
			}
		}

		public bool IsEmpty
		{
			get
			{
				if (!IsEditable)
					return true;

				if (_kind != CellEditValueKind.Text)
				{
					if (_param == null)
						return true;
					try
					{
						return _param.Value == null;
					}
					catch
					{
						return true;
					}
				}

				string text;
				TryGetText(out text);
				return string.IsNullOrEmpty(text);
			}
		}

		public static CellValueEditor Create(CellManager manager, CellLocation location)
		{
			var editor = new CellValueEditor(manager, location);
			editor.Resolve();
			return editor;
		}

		public bool TryGetText(out string text)
		{
			text = string.Empty;
			if (!IsEditable)
				return false;

			try
			{
				if (_literal)
				{
					text = _textContent == null ? string.Empty : (_textContent.Text ?? string.Empty);
					return true;
				}

				if (_param == null)
					return true;

				object value;
				try
				{
					value = _param.Value;
				}
				catch
				{
					value = null;
				}

				if (value == null)
					return true;

				if (_kind == CellEditValueKind.Text)
				{
					if ((value is DateTime || value is decimal) && _owner != null && !string.IsNullOrEmpty(_paramName))
					{
						var formatted = _owner.GetFormattedParamValueByName(_paramName);
						text = formatted ?? Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
					}
					else
						text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
					return true;
				}

				var textOwner = _owner as TextBasedContent;
				if (textOwner != null)
					text = textOwner.GetText() ?? string.Empty;
				else
					text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
				return true;
			}
			catch
			{
				text = string.Empty;
				return false;
			}
		}

		public bool TrySetText(string text, out CellEditError error)
		{
			error = CellEditError.None;
			if (!IsEditable)
			{
				error = _error == CellEditError.None ? CellEditError.NotEditable : _error;
				return false;
			}

			text = text ?? string.Empty;
			if (text.Length > MaxTextLength)
			{
				error = CellEditError.TooLong;
				return false;
			}

			try
			{
				switch (_kind)
				{
					case CellEditValueKind.Text:
						return SetTextValue(text, out error);
					case CellEditValueKind.Decimal:
						return SetDecimalValue(text, out error);
					case CellEditValueKind.DateTime:
						return SetDateTimeValue(text, out error);
					default:
						error = CellEditError.UnsupportedContent;
						return false;
				}
			}
			catch
			{
				error = CellEditError.UnsupportedContent;
				return false;
			}
		}

		private void Resolve()
		{
			if (_manager == null || !_manager.IsCurrectLocation(_location))
			{
				_error = CellEditError.NotEditable;
				return;
			}

			var settings = _manager.EditSettings;
			if (settings.Mode == CellEditMode.None)
			{
				_error = CellEditError.NotEditable;
				return;
			}

			var cell = SafeCell();
			if (settings.Mode == CellEditMode.MarkedCells && (cell == null || !cell.Editable))
			{
				_error = CellEditError.NotEditable;
				return;
			}

			if (cell != null && !string.IsNullOrEmpty(cell.EditParam))
			{
				if (ResolveParamByName(cell, cell.EditParam))
					return;
				ResolveMissingEditParam(cell);
				return;
			}

			if (cell == null)
			{
				_kind = CellEditValueKind.Text;
				_literal = true;
				return;
			}

			var primary = FindPrimaryContent(cell);
			if (primary == null)
			{
				if (cell.Content != null)
				{
					_error = CellEditError.UnsupportedContent;
					return;
				}

				_kind = CellEditValueKind.Text;
				_literal = true;
				return;
			}

			var textContent = primary as TextContent;
			if (textContent != null)
			{
				ResolveTextContent(cell, textContent, settings.Mode);
				return;
			}

			var decimalContent = primary as DecimalContent;
			if (decimalContent != null)
			{
				ResolveValueContent(primary, CellEditValueKind.Decimal);
				return;
			}

			ResolveValueContent(primary, CellEditValueKind.DateTime);
		}

		private void ResolveTextContent(Cell cell, TextContent textContent, CellEditMode mode)
		{
			var textParam = FindParamByTarget(textContent, "Text");
			if (textParam != null)
			{
				if (IsComputed(textParam))
				{
					_error = CellEditError.Computed;
					return;
				}

				SetParamSlot(textContent, textParam, CellEditValueKind.Text);
				return;
			}

			var text = textContent.Text;
			if (string.IsNullOrEmpty(text))
			{
				var onlyParam = FindSingleUnboundParam(textContent);
				if (onlyParam != null)
				{
					if (IsComputed(onlyParam))
					{
						_error = CellEditError.Computed;
						return;
					}

					SetParamSlot(textContent, onlyParam, KindOf(textContent, onlyParam));
					return;
				}

				_kind = CellEditValueKind.Text;
				_literal = true;
				_textContent = textContent;
				return;
			}

			var names = GetPlaceholderNames(text);
			if (names.Count == 1)
			{
				var name = names[0];
				var param = FindParamByName(textContent, name);
				if (param != null)
				{
					if (IsComputed(param))
					{
						_error = CellEditError.Computed;
						return;
					}

					SetParamSlot(textContent, param, KindOf(textContent, param));
					return;
				}

				_paramName = name;
				_owner = textContent;
				_kind = CellEditValueKind.Text;
				return;
			}

			if (names.Count > 1 || text.IndexOf("{{", StringComparison.Ordinal) >= 0)
			{
				if (mode != CellEditMode.AllCells)
				{
					_error = CellEditError.NotEditable;
					return;
				}
			}

			_kind = CellEditValueKind.Text;
			_literal = true;
			_textContent = textContent;
		}

		private void ResolveValueContent(BaseContent content, CellEditValueKind kind)
		{
			var param = FindParamByTarget(content, "Value");
			if (param != null)
			{
				if (IsComputed(param))
				{
					_error = CellEditError.Computed;
					return;
				}

				SetParamSlot(content, param, KindOf(content, param));
				return;
			}

			_owner = content;
			_kind = kind;
		}

		private void ResolveMissingEditParam(Cell cell)
		{
			var primary = FindPrimaryContent(cell);
			_paramName = cell.EditParam;

			var decimalContent = primary as DecimalContent;
			if (decimalContent != null)
			{
				_owner = decimalContent;
				_kind = CellEditValueKind.Decimal;
				return;
			}

			var dateTimeContent = primary as DateTimeContent;
			if (dateTimeContent != null)
			{
				_owner = dateTimeContent;
				_kind = CellEditValueKind.DateTime;
				return;
			}

			if (primary != null && !(primary is TextContent))
			{
				_error = CellEditError.UnsupportedContent;
				return;
			}

			var textContent = primary as TextContent;
			if (textContent != null
				&& !string.IsNullOrEmpty(textContent.Text)
				&& GetPlaceholderNames(textContent.Text).Count == 0)
			{
				// EditParam на литеральном тексте не влияет на отображение:
				// правим сам текст, иначе ввод «пропадал» бы с экрана.
				_literal = true;
				_textContent = textContent;
				_paramName = null;
				_kind = CellEditValueKind.Text;
				return;
			}

			_owner = primary;
			_kind = CellEditValueKind.Text;
		}

		private bool ResolveParamByName(Cell cell, string name)
		{
			foreach (var content in cell.GetContents())
			{
				foreach (var param in content.GetParams())
				{
					if (!string.Equals(param.Name, name, StringComparison.Ordinal))
						continue;

					if (IsComputed(param))
					{
						_error = CellEditError.Computed;
						return true;
					}

					if (!(content is TextContent || content is DecimalContent || content is DateTimeContent))
					{
						_error = CellEditError.UnsupportedContent;
						return true;
					}

					SetParamSlot(content, param, KindOf(content, param));
					return true;
				}
			}

			return false;
		}

		private void SetParamSlot(BaseContent owner, Param param, CellEditValueKind kind)
		{
			_owner = owner;
			_param = param;
			_paramName = param.Name;
			_kind = kind;
		}

		private static CellEditValueKind KindOf(BaseContent content, Param param)
		{
			if (content is DecimalContent)
				return CellEditValueKind.Decimal;
			if (content is DateTimeContent)
				return CellEditValueKind.DateTime;
			if (param != null)
			{
				if (string.Equals(param.Type, "Decimal", StringComparison.OrdinalIgnoreCase))
					return CellEditValueKind.Decimal;
				if (string.Equals(param.Type, "DateTime", StringComparison.OrdinalIgnoreCase))
					return CellEditValueKind.DateTime;
			}

			return CellEditValueKind.Text;
		}

		private static bool IsComputed(Param param)
		{
			return param != null && !string.IsNullOrEmpty(param.ExpressionID);
		}

		private static BaseContent FindPrimaryContent(Cell cell)
		{
			if (cell == null)
				return null;

			foreach (var content in cell.GetContents())
			{
				if (content is TextContent || content is DecimalContent || content is DateTimeContent)
					return content;
			}

			return null;
		}

		private static Param FindParamByTarget(BaseContent content, string target)
		{
			foreach (var param in content.GetParams())
			{
				if (string.Equals(param.Target, target, StringComparison.OrdinalIgnoreCase))
					return param;
			}

			return null;
		}

		private static Param FindParamByName(BaseContent content, string name)
		{
			foreach (var param in content.GetParams())
			{
				if (string.Equals(param.Name, name, StringComparison.Ordinal))
					return param;
			}

			return null;
		}

		private static Param FindSingleUnboundParam(BaseContent content)
		{
			Param result = null;
			foreach (var param in content.GetParams())
			{
				if (!string.IsNullOrEmpty(param.Target) || param.IsVirtual)
					continue;
				if (result != null)
					return null;
				result = param;
			}

			return result;
		}

		private static List<string> GetPlaceholderNames(string text)
		{
			var result = new List<string>();
			var matches = PlaceholderRegex.Matches(text);
			foreach (Match match in matches)
			{
				var name = match.Groups[1].Value;
				if (!result.Contains(name))
					result.Add(name);
			}

			return result;
		}

		private bool SetTextValue(string text, out CellEditError error)
		{
			error = CellEditError.None;
			if (_literal)
			{
				var content = EnsureTextContent(out error);
				if (content == null)
					return false;

				content.Text = Normalize(text);
				return true;
			}

			var param = EnsureParam("Text", out error);
			if (param == null)
				return false;

			if (string.IsNullOrEmpty(text))
				param.Value = null;
			else
				param.Value = text;

			if (param.IsVirtual)
				param.IsVirtual = false;

			return true;
		}

		private bool SetDecimalValue(string text, out CellEditError error)
		{
			error = CellEditError.None;
			var param = EnsureParam("Value", out error);
			if (param == null)
				return false;

			if (string.IsNullOrWhiteSpace(text))
			{
				param.Value = null;
				return true;
			}

			decimal value;
			if (!TryParseDecimal(text, param, out value))
			{
				error = CellEditError.InvalidNumber;
				return false;
			}

			param.Value = value;
			param.Type = "Decimal";
			return true;
		}

		private bool SetDateTimeValue(string text, out CellEditError error)
		{
			error = CellEditError.None;
			var param = EnsureParam("Value", out error);
			if (param == null)
				return false;

			if (string.IsNullOrWhiteSpace(text))
			{
				param.Value = null;
				return true;
			}

			DateTime value;
			if (!TryParseDateTime(text, param, out value))
			{
				error = CellEditError.InvalidDate;
				return false;
			}

			param.Value = value;
			param.Type = "DateTime";
			return true;
		}

		private Param EnsureParam(string target, out CellEditError error)
		{
			error = CellEditError.None;
			if (_param != null)
				return _param;

			BaseContent content = _owner;
			if (content == null)
			{
				content = EnsureTextContent(out error);
				if (content == null)
					return null;
			}

			if (content is BarCodeContent || content is ImageContent || content is ActionContent
				|| content is ChartContent || content is LineContent || content is CollectionContent)
			{
				error = CellEditError.UnsupportedContent;
				return null;
			}

			var param = new Param(_manager, content)
			{
				Name = _paramName,
				Target = target
			};
			content.AddParam(param);
			_param = param;
			return param;
		}

		private TextContent EnsureTextContent(out CellEditError error)
		{
			error = CellEditError.None;
			if (_textContent != null)
				return _textContent;

			var cell = SafeCell();
			if (cell == null)
				cell = _manager.CreateCell(_location);
			if (cell == null)
			{
				error = CellEditError.UnsupportedContent;
				return null;
			}

			var existing = cell.Content as TextContent;
			if (existing != null)
			{
				_textContent = existing;
				return existing;
			}

			var collection = cell.Content as CollectionContent;
			if (collection != null)
			{
				foreach (var item in collection.GetContents())
				{
					var text = item as TextContent;
					if (text == null)
						continue;
					_textContent = text;
					return text;
				}
			}
			else if (cell.Content != null)
			{
				error = CellEditError.UnsupportedContent;
				return null;
			}

			var created = new TextContent(_manager);
			if (collection != null)
				collection.Add(created);
			else
				cell.Content = created;

			_textContent = created;
			return created;
		}

		private Cell SafeCell()
		{
			if (_manager == null || !_manager.IsCurrectLocation(_location))
				return null;

			try
			{
				return _manager.GetCell(_location);
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		private static string Normalize(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			return text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
		}

		private static bool TryParseDecimal(string text, Param param, out decimal value)
		{
			const NumberStyles style = NumberStyles.Number;
			if (!string.IsNullOrEmpty(param.Culture))
			{
				try
				{
					if (decimal.TryParse(text, style, CultureInfo.GetCultureInfo(param.Culture), out value))
						return true;
				}
				catch (CultureNotFoundException)
				{
				}
			}

			if (decimal.TryParse(text, style, CultureInfo.CurrentCulture, out value))
				return true;

			return decimal.TryParse(text, style, CultureInfo.InvariantCulture, out value);
		}

		private static bool TryParseDateTime(string text, Param param, out DateTime value)
		{
			if (!string.IsNullOrEmpty(param.Format))
			{
				if (TryParseExact(text, param.Format, param.Culture, out value))
					return true;
			}

			var formats = new[]
			{
				"dd.MM.yyyy HH:mm:ss",
				"dd.MM.yyyy HH:mm",
				"dd.MM.yyyy",
				"d.M.yyyy H:mm:ss",
				"d.M.yyyy H:mm",
				"d.M.yyyy"
			};

			if (TryParseExact(text, formats, param.Culture, out value))
				return true;

			return DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out value);
		}

		private static bool TryParseExact(string text, string format, string culture, out DateTime value)
		{
			if (!string.IsNullOrEmpty(format)
				&& DateTime.TryParseExact(text, format, ResolveCulture(culture), DateTimeStyles.None, out value))
				return true;

			value = DateTime.MinValue;
			return false;
		}

		private static bool TryParseExact(string text, string[] formats, string culture, out DateTime value)
		{
			if (DateTime.TryParseExact(text, formats, ResolveCulture(culture), DateTimeStyles.None, out value))
				return true;

			if (DateTime.TryParseExact(text, formats, CultureInfo.CurrentCulture, DateTimeStyles.None, out value))
				return true;

			if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
				return true;

			value = DateTime.MinValue;
			return false;
		}

		private static CultureInfo ResolveCulture(string culture)
		{
			if (string.IsNullOrEmpty(culture))
				return CultureInfo.CurrentCulture;

			try
			{
				return CultureInfo.GetCultureInfo(culture);
			}
			catch (CultureNotFoundException)
			{
				return CultureInfo.CurrentCulture;
			}
		}
	}
}
