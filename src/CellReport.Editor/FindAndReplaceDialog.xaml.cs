using System;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace CellReport.Editor
{
	/// <summary>
	/// Interaction logic for FindAndReplaceDialog.xaml
	/// </summary>
	public partial class FindAndReplaceDialog
	{
		private static string _textToFind = "";
		private static bool _caseSensitive;
		private static bool _wholeWord;
		private static bool _useRegex;
		private static bool _useWildcards;
		private static bool _searchUp;

		private readonly TextEditor _editor;

		public FindAndReplaceDialog(TextEditor editor)
		{
			InitializeComponent();

			_editor = editor;

			FindText.Text = FindForReplaceText.Text = _textToFind;
			IsCaseSensitive.IsChecked = _caseSensitive;
			IsWholeWord.IsChecked = _wholeWord;
			IsRegex.IsChecked = _useRegex;
			IsWildcards.IsChecked = _useWildcards;
			IsSearchUp.IsChecked = _searchUp;

			Loaded += (sender, args) =>
			{
				if (_dialog.MainTabControl.SelectedIndex == 0)
					FindText.Focus();
				else
					FindForReplaceText.Focus();
			};

			PreviewKeyDown += (sender, args) =>
			{
				if (args.Key == Key.Escape)
					Close();
			};
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			_textToFind = FindForReplaceText.Text;
			_caseSensitive = IsCaseSensitive.IsChecked == true;
			_wholeWord = IsWholeWord.IsChecked == true;
			_useRegex = IsRegex.IsChecked == true;
			_useWildcards = IsWildcards.IsChecked == true;
			_searchUp = IsSearchUp.IsChecked == true;

			_dialog = null;
		}

		private void FindNextClick(object sender, RoutedEventArgs e)
		{
			FindNext(FindText.Text);
		}

		private void FindNext2Click(object sender, RoutedEventArgs e)
		{
			FindNext(FindForReplaceText.Text);
		}

		private void ReplaceClick(object sender, RoutedEventArgs e)
		{
			Regex regex = GetRegEx(FindForReplaceText.Text);
			string input = _editor.Text.Substring(_editor.SelectionStart, _editor.SelectionLength);
			Match match = regex.Match(input);
			if (match.Success && match.Index == 0 && match.Length == input.Length)
			{
				_editor.Document.Replace(_editor.SelectionStart, _editor.SelectionLength, ReplaceText.Text);
			}

			FindNext(FindForReplaceText.Text);
		}

		private void ReplaceAllClick(object sender, RoutedEventArgs e)
		{
			var message = $"Вы хотите заменить все '{FindForReplaceText.Text}' на '{ReplaceText.Text}'?";
			if (MessageBox.Show(message, "Заменить все", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
			{
				Regex regex = GetRegEx(FindForReplaceText.Text, true);
				int offset = 0;
				_editor.BeginChange();
				foreach (Match match in regex.Matches(_editor.Text))
				{
					_editor.Document.Replace(offset + match.Index, match.Length, ReplaceText.Text);
					offset += ReplaceText.Text.Length - match.Length;
				}
				_editor.EndChange();
			}
		}

		private bool FindNext(string textToFind)
		{
			Regex regex = GetRegEx(textToFind);
			int start = regex.Options.HasFlag(RegexOptions.RightToLeft) ?
				_editor.SelectionStart : _editor.SelectionStart + _editor.SelectionLength;
			Match match = regex.Match(_editor.Text, start);

			if (!match.Success)  // start again from beginning or end
			{
				if (regex.Options.HasFlag(RegexOptions.RightToLeft))
					match = regex.Match(_editor.Text, _editor.Text.Length);
				else
					match = regex.Match(_editor.Text, 0);
			}

			if (match.Success)
			{
				_editor.Select(match.Index, match.Length);
				TextLocation loc = _editor.Document.GetLocation(match.Index);
				_editor.ScrollTo(loc.Line, loc.Column);
			}

			return match.Success;
		}

		private Regex GetRegEx(string textToFind, bool leftToRight = false)
		{
			RegexOptions options = RegexOptions.None;
			if (IsSearchUp.IsChecked == true && !leftToRight)
				options |= RegexOptions.RightToLeft;
			if (IsCaseSensitive.IsChecked == false)
				options |= RegexOptions.IgnoreCase;

			if (IsRegex.IsChecked == true)
			{
				return new Regex(textToFind, options);
			}
			else
			{
				string pattern = Regex.Escape(textToFind);
				if (IsWildcards.IsChecked == true)
					pattern = pattern.Replace("\\*", ".*").Replace("\\?", ".");
				if (IsWholeWord.IsChecked == true)
					pattern = "\\b" + pattern + "\\b";
				return new Regex(pattern, options);
			}
		}

		private static FindAndReplaceDialog _dialog;

		public static void ShowForReplace(TextEditor editor)
		{
			if (_dialog == null)
			{
				_dialog = new FindAndReplaceDialog(editor) {MainTabControl = {SelectedIndex = 1}};
				_dialog.Show();
				_dialog.Activate();
			}
			else
			{
				_dialog.MainTabControl.SelectedIndex = 1;
				_dialog.Activate();
			}

			if (!editor.TextArea.Selection.IsMultiline)
			{
				_dialog.FindText.Text = _dialog.FindForReplaceText.Text = editor.TextArea.Selection.GetText();
				_dialog.FindText.SelectAll();
				_dialog.FindForReplaceText.SelectAll();
				_dialog.FindForReplaceText.Focus();
			}
		}

		public static void ShowForFind(TextEditor editor)
		{
			if (_dialog == null)
			{
				_dialog = new FindAndReplaceDialog(editor);
				_dialog.Show();
				_dialog.Activate();
			}
			else
			{
				_dialog.Activate();
			}

			if (!editor.TextArea.Selection.IsMultiline)
			{
				_dialog.FindText.Text = _dialog.FindForReplaceText.Text = editor.TextArea.Selection.GetText();
				_dialog.FindText.SelectAll();
				_dialog.FindForReplaceText.SelectAll();
				_dialog.FindForReplaceText.Focus();
			}
		}
	}
}
