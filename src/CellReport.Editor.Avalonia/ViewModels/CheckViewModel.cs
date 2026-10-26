using System.Collections.ObjectModel;
using System.Linq;
using CellReport.Core;
using CellReport.Editor.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CellReport.Editor.Avalonia.ViewModels;

public partial class CheckViewModel : ObservableObject
{
	private readonly MainWindowViewModel _owner;

	public CheckViewModel(MainWindowViewModel owner)
	{
		_owner = owner;
		Recheck();
	}

	public ObservableCollection<TemplateIssue> Issues { get; } = new();

	[ObservableProperty]
	private TemplateIssue? _selectedIssue;

	[ObservableProperty]
	private string _summary = string.Empty;

	[RelayCommand]
	public void Recheck()
	{
		var selected = SelectedIssue;
		Issues.Clear();
		foreach (var issue in TemplateValidator.Validate(_owner.Manager))
			Issues.Add(issue);

		SelectedIssue = selected != null && Issues.Contains(selected) ? selected : Issues.FirstOrDefault();

		var errors = Issues.Count(item => item.Severity == TemplateIssueSeverity.Error);
		var warnings = Issues.Count - errors;
		Summary = Issues.Count == 0
			? "Проблем не найдено"
			: "Ошибок: " + errors + " · Предупреждений: " + warnings;
	}

	public void Navigate(TemplateIssue? issue)
	{
		if (issue?.Cell is not { } cell || cell.Equals(CellLocation.None))
			return;

		_owner.GoToCell(cell);
	}
}
