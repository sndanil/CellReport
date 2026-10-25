using CellReport.Core;
using CellReport.Core.Expressions;
using ExpressionOperation = CellReport.Core.Expressions.Operation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CellReport.Editor.Avalonia.ViewModels;

public partial class ExpressionsViewModel : ObservableObject
{
	private readonly MainWindowViewModel _owner;
	private CellManager? _manager;
	private bool _loading;

	public ExpressionsViewModel(MainWindowViewModel owner)
	{
		_owner = owner;
		NewKind = ExpressionKindChoice.All[0];
	}

	public IReadOnlyList<ExpressionKindChoice> Kinds { get; } = ExpressionKindChoice.All;

	public ObservableCollection<ExpressionEdit> Items { get; } = new();

	[ObservableProperty]
	private ExpressionKindChoice _newKind;

	public void Load(CellManager? manager)
	{
		_manager = manager;
		_loading = true;
		try
		{
			Items.Clear();
			if (manager == null)
				return;

			foreach (var expression in manager.GetExpressions().OrderBy(item => item.ID, StringComparer.CurrentCulture))
				Items.Add(new ExpressionEdit(this, expression));
		}
		finally
		{
			_loading = false;
		}
	}

	public void Commit(Action change)
	{
		if (_loading || _manager == null)
			return;

		_owner.CommitDocumentChange(change);
	}

	[RelayCommand]
	private void Add()
	{
		if (_manager == null || NewKind == null)
			return;

		var expression = Create(NewKind.Key, NextId());
		Commit(() => _manager.AddExpression(expression));
		Items.Add(new ExpressionEdit(this, expression));
	}

	public void Remove(ExpressionEdit edit)
	{
		if (_loading || _manager == null || edit.Expression == null)
			return;

		Commit(() => _manager.RemoveExpression(edit.Expression));
		Items.Remove(edit);
	}

	public void ChangeKind(ExpressionEdit edit, string key)
	{
		if (_loading || _manager == null || edit.Expression == null || edit.Expression.GetType().Name == key)
			return;

		var created = Create(key, edit.Expression.ID);
		var previous = edit.Expression;
		Commit(() =>
		{
			_manager.RemoveExpression(previous);
			_manager.AddExpression(created);
		});
		edit.Attach(created);
	}

	public bool ChangeId(ExpressionEdit edit, string value)
	{
		if (_loading || _manager == null || edit.Expression == null)
			return false;

		value = (value ?? string.Empty).Trim();
		var current = edit.Expression.ID ?? string.Empty;
		if (value.Length == 0 || value == current)
			return false;

		if (_manager.GetExpressionByID(value) != null)
			return false;

		var expression = edit.Expression;
		Commit(() =>
		{
			_manager.RemoveExpression(expression);
			expression.ID = value;
			_manager.AddExpression(expression);
			ReplaceReferences(_manager, current, value);
		});

		foreach (var item in Items)
			item.RefreshLinks();

		return true;
	}

	private string NextId()
	{
		var index = Items.Count + 1;
		while (_manager!.GetExpressionByID("expression" + index.ToString(CultureInfo.InvariantCulture)) != null)
			index++;

		return "expression" + index.ToString(CultureInfo.InvariantCulture);
	}

	private static BaseExpression Create(string key, string id)
	{
		BaseExpression expression = key switch
		{
			"NowExpression" => new NowExpression(),
			"PagesCountExpression" => new PagesCountExpression(),
			"DocumentNameExpression" => new DocumentNameExpression(),
			"SumExpression" => new SumExpression { ParamName = string.Empty },
			"DecimalExpression" => new DecimalExpression(),
			_ => new CountExpression(0, 1)
		};
		expression.ID = id;
		return expression;
	}

	private static void ReplaceReferences(CellManager manager, string oldId, string newId)
	{
		for (var row = 0; row < manager.RowsCount; row++)
		{
			for (var column = 0; column < manager.ColumnsCount; column++)
			{
				if (manager.TryGetCell(row, column, out var cell) && cell.Content != null)
					cell.Content.ReplaceExpressionId(oldId, newId);
			}
		}

		ReplacePagers(manager.PrintSettings.Headers, oldId, newId);
		ReplacePagers(manager.PrintSettings.Footers, oldId, newId);

		foreach (var expression in manager.GetExpressions())
		{
			if (expression is DecimalExpression decimalExpression && decimalExpression.PrevExpression == oldId)
				decimalExpression.PrevExpression = newId;
		}
	}

	private static void ReplacePagers(IList<Pager>? pagers, string oldId, string newId)
	{
		if (pagers == null)
			return;

		foreach (var pager in pagers)
		{
			pager.Left?.ReplaceExpressionId(oldId, newId);
			pager.Center?.ReplaceExpressionId(oldId, newId);
			pager.Right?.ReplaceExpressionId(oldId, newId);
		}
	}
}

public partial class ExpressionEdit : ObservableObject
{
	private readonly ExpressionsViewModel _owner;
	private BaseExpression _expression;
	private bool _loading;

	public ExpressionEdit(ExpressionsViewModel owner, BaseExpression expression)
	{
		_owner = owner;
		_expression = expression;
		RemoveCommand = new RelayCommand(() => _owner.Remove(this));
		Attach(expression);
	}

	public BaseExpression Expression => _expression;

	public IReadOnlyList<ExpressionKindChoice> Kinds => ExpressionKindChoice.All;

	public IReadOnlyList<OperationChoice> Operations => OperationChoice.All;

	public IRelayCommand RemoveCommand { get; }

	[ObservableProperty]
	private ExpressionKindChoice _kind;

	[ObservableProperty]
	private string _id = string.Empty;

	[ObservableProperty]
	private string _startText = "0";

	[ObservableProperty]
	private string _incrementText = "1";

	[ObservableProperty]
	private string _paramName = string.Empty;

	[ObservableProperty]
	private bool _perPage;

	[ObservableProperty]
	private bool _freeze;

	[ObservableProperty]
	private OperationChoice _operation;

	[ObservableProperty]
	private string _secondValueText = "0";

	[ObservableProperty]
	private string _prevExpression = string.Empty;

	[ObservableProperty]
	private bool _isCount;

	[ObservableProperty]
	private bool _isSum;

	[ObservableProperty]
	private bool _isNow;

	[ObservableProperty]
	private bool _isDecimal;

	[ObservableProperty]
	private bool _showSecondValue;

	public void Attach(BaseExpression expression)
	{
		_loading = true;
		_expression = expression;
		Id = expression.ID ?? string.Empty;
		Kind = ExpressionKindChoice.Find(expression.GetType().Name);
		if (expression is CountExpression count)
		{
			StartText = count.Start.ToString(CultureInfo.CurrentCulture);
			IncrementText = count.Increment.ToString(CultureInfo.CurrentCulture);
		}

		if (expression is SumExpression sum)
		{
			ParamName = sum.ParamName ?? string.Empty;
			PerPage = sum.PerPage;
		}

		if (expression is NowExpression now)
			Freeze = now.Freeze;

		if (expression is DecimalExpression decimalExpression)
		{
			Operation = OperationChoice.Find(decimalExpression.Operation);
			SecondValueText = decimalExpression.SecondValue.ToString("0.##", CultureInfo.CurrentCulture);
			PrevExpression = decimalExpression.PrevExpression ?? string.Empty;
		}

		PublishFlags();
		_loading = false;
	}

	public void RefreshLinks()
	{
		if (_expression is not DecimalExpression decimalExpression)
			return;

		_loading = true;
		PrevExpression = decimalExpression.PrevExpression ?? string.Empty;
		_loading = false;
	}

	partial void OnKindChanged(ExpressionKindChoice value)
	{
		if (_loading || value == null)
			return;

		_owner.ChangeKind(this, value.Key);
	}

	partial void OnIdChanged(string value)
	{
		if (_loading)
			return;

		if (_owner.ChangeId(this, value))
			return;

		_loading = true;
		Id = _expression.ID ?? string.Empty;
		_loading = false;
	}

	partial void OnStartTextChanged(string value)
	{
		if (_loading || _expression is not CountExpression count || !TryInt(value, out var start) || count.Start == start)
			return;

		_owner.Commit(() => count.Start = start);
	}

	partial void OnIncrementTextChanged(string value)
	{
		if (_loading || _expression is not CountExpression count || !TryInt(value, out var increment) || count.Increment == increment)
			return;

		_owner.Commit(() => count.Increment = increment);
	}

	partial void OnParamNameChanged(string value)
	{
		if (_loading || _expression is not SumExpression sum || (sum.ParamName ?? string.Empty) == (value ?? string.Empty))
			return;

		_owner.Commit(() => sum.ParamName = value ?? string.Empty);
	}

	partial void OnPerPageChanged(bool value)
	{
		if (_loading || _expression is not SumExpression sum || sum.PerPage == value)
			return;

		_owner.Commit(() => sum.PerPage = value);
	}

	partial void OnFreezeChanged(bool value)
	{
		if (_loading || _expression is not NowExpression now || now.Freeze == value)
			return;

		_owner.Commit(() => now.Freeze = value);
	}

	partial void OnOperationChanged(OperationChoice value)
	{
		if (_loading || value == null || _expression is not DecimalExpression decimalExpression || decimalExpression.Operation == value.Operation)
			return;

		_owner.Commit(() => decimalExpression.Operation = value.Operation);
		ShowSecondValue = value.Operation is ExpressionOperation.Multiply or ExpressionOperation.Add;
	}

	partial void OnSecondValueTextChanged(string value)
	{
		if (_loading || _expression is not DecimalExpression decimalExpression || !TryDecimal(value, out var number) || decimalExpression.SecondValue == number)
			return;

		_owner.Commit(() => decimalExpression.SecondValue = number);
	}

	partial void OnPrevExpressionChanged(string value)
	{
		if (_loading || _expression is not DecimalExpression decimalExpression)
			return;

		var next = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		if (decimalExpression.PrevExpression == next)
			return;

		_owner.Commit(() => decimalExpression.PrevExpression = next);
	}

	private void PublishFlags()
	{
		IsCount = _expression is CountExpression;
		IsSum = _expression is SumExpression;
		IsNow = _expression is NowExpression;
		IsDecimal = _expression is DecimalExpression;
		ShowSecondValue = _expression is DecimalExpression decimalExpression
			&& decimalExpression.Operation is ExpressionOperation.Multiply or ExpressionOperation.Add;
	}

	private static bool TryInt(string? text, out int value)
	{
		if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
			return true;

		return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
	}

	private static bool TryDecimal(string? text, out decimal value)
	{
		if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
			return true;

		return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
	}
}

public sealed class ExpressionKindChoice
{
	public static IReadOnlyList<ExpressionKindChoice> All { get; } =
	[
		new("CountExpression", "Счётчик"),
		new("SumExpression", "Сумма"),
		new("DecimalExpression", "Число"),
		new("NowExpression", "Текущие дата и время"),
		new("PagesCountExpression", "Количество страниц"),
		new("DocumentNameExpression", "Название отчёта")
	];

	public ExpressionKindChoice(string key, string title)
	{
		Key = key;
		Title = title;
	}

	public string Key { get; }

	public string Title { get; }

	public static ExpressionKindChoice Find(string key)
	{
		foreach (var kind in All)
		{
			if (kind.Key == key)
				return kind;
		}

		return All[0];
	}

	public override string ToString() => Title;
}

public sealed class OperationChoice
{
	public static IReadOnlyList<OperationChoice> All { get; } =
	[
		new(ExpressionOperation.Truncate, "Целая часть"),
		new(ExpressionOperation.DecimalPart, "Дробная часть"),
		new(ExpressionOperation.DecimalPartAndShift, "Дробная часть со сдвигом"),
		new(ExpressionOperation.Multiply, "Умножение"),
		new(ExpressionOperation.Add, "Сложение")
	];

	public OperationChoice(ExpressionOperation operation, string title)
	{
		Operation = operation;
		Title = title;
	}

	public ExpressionOperation Operation { get; }

	public string Title { get; }

	public static OperationChoice Find(ExpressionOperation operation)
	{
		foreach (var choice in All)
		{
			if (choice.Operation == operation)
				return choice;
		}

		return All[0];
	}

	public override string ToString() => Title;
}
