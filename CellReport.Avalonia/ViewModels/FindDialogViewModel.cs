using CellReport.Avalonia.Services;
using CellReport.Core;
using CellReport.Presenters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CellReport.Avalonia.ViewModels
{
	public partial class FindDialogViewModel : ObservableObject
	{
		private static readonly List<string> LastSearchTerms = new();
		private readonly BaseDisplayPresenter _presenter;
		private Cell? _lastCell;

		public FindDialogViewModel(BaseDisplayPresenter presenter)
		{
			_presenter = presenter;
			LookOverOptions = new[]
			{
				ReportLocalization.Get("Report/Search/LookOver/ByRows"),
				ReportLocalization.Get("Report/Search/LookOver/ByColumns")
			};
			DirectionOptions = new[]
			{
				ReportLocalization.Get("Report/Search/Direction/Forward"),
				ReportLocalization.Get("Report/Search/Direction/Backward")
			};
			SearchHistory = new ObservableCollection<string>(LastSearchTerms.AsEnumerable().Reverse());
			if (SearchHistory.Count > 0)
				SearchText = SearchHistory[0];
		}

		public string Title => ReportLocalization.Get("Report/Search/Title");

		public string FindWhatLabel => ReportLocalization.Get("Report/Search/FindWhat");

		public string MatchCaseLabel => ReportLocalization.Get("Report/Search/MatchCase");

		public string LookOverLabel => ReportLocalization.Get("Report/Search/LookOver");

		public string DirectionLabel => ReportLocalization.Get("Report/Search/Direction");

		public string FindNextLabel => ReportLocalization.Get("Report/Search/Next");

		public string FindAllLabel => ReportLocalization.Get("Report/Search/All");

		public string CloseLabel => ReportLocalization.Get("Report/Search/Close");

		public string ResultContentLabel => ReportLocalization.Get("Report/Search/Result/Content");

		public string ResultRowLabel => ReportLocalization.Get("Report/Search/Result/Row");

		public string ResultColumnLabel => ReportLocalization.Get("Report/Search/Result/Column");

		public string[] LookOverOptions { get; }

		public string[] DirectionOptions { get; }

		public ObservableCollection<string> SearchHistory { get; }

		public ObservableCollection<FindResultItem> Results { get; } = new();

		[ObservableProperty]
		private string _searchText = string.Empty;

		[ObservableProperty]
		private bool _matchCase;

		[ObservableProperty]
		private int _lookOverIndex;

		[ObservableProperty]
		private int _directionIndex;

		[ObservableProperty]
		private FindResultItem? _selectedResult;

		[ObservableProperty]
		private string? _statusMessage;

		partial void OnSelectedResultChanged(FindResultItem? value)
		{
			if (value == null)
				return;

			_presenter.CurrentCell = new Core.CellLocation { Row = value.Row, Column = value.Column };
		}

		[RelayCommand]
		private void FindNext()
		{
			AddToHistory();
			_lastCell = null;
			var lookOver = LookOverIndex == 0 ? SearchLookOver.ByRows : SearchLookOver.ByColumns;
			var direction = DirectionIndex == 0 ? SearchDirection.Forward : SearchDirection.Backward;

			if (ReportSearchService.TryFindNext(_presenter, SearchText, MatchCase, lookOver, direction, ref _lastCell, out _))
			{
				StatusMessage = null;
				return;
			}

			StatusMessage = ReportLocalization.Get("Report/Search/NotFound");
		}

		[RelayCommand]
		private void FindAll()
		{
			AddToHistory();
			_lastCell = null;
			Results.Clear();

			var lookOver = LookOverIndex == 0 ? SearchLookOver.ByRows : SearchLookOver.ByColumns;
			var direction = DirectionIndex == 0 ? SearchDirection.Forward : SearchDirection.Backward;
			foreach (var item in ReportSearchService.FindAll(_presenter, SearchText, MatchCase, lookOver, direction))
				Results.Add(item);

			StatusMessage = Results.Count == 0 ? ReportLocalization.Get("Report/Search/NotFound") : null;
		}

		private void AddToHistory()
		{
			if (string.IsNullOrWhiteSpace(SearchText))
				return;

			var index = LastSearchTerms.IndexOf(SearchText);
			if (index >= 0)
				LastSearchTerms.RemoveAt(index);
			LastSearchTerms.Add(SearchText);

			SearchHistory.Clear();
			foreach (var term in LastSearchTerms.AsEnumerable().Reverse())
				SearchHistory.Add(term);
		}
	}
}
