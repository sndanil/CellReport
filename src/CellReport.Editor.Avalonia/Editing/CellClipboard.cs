using Avalonia.Controls;
using Avalonia.Input.Platform;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;
using CellReport.Presenters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CellReport.Editor.Avalonia.Editing;

public sealed class CellClipboard
{
	private static string? _payload;

	private List<BaseStyle> _styles = new();
	private List<BaseExpression> _expressions = new();
	private Dictionary<string, string> _resources = new();
	private Dictionary<string, string> _exprIds = new();
	private readonly CellManager _manager;

	private CellClipboard(CellManager manager)
	{
		_manager = manager;
	}

	public static bool HasData => !string.IsNullOrEmpty(_payload);

	public static async Task CopyAsync(BaseDisplayPresenter presenter, Window? window)
	{
		var clipboard = new CellClipboard(presenter.Manager);
		string data;
		if (presenter.HaveSelection())
			data = clipboard.Serialize(CaptureSelection(presenter));
		else
			data = clipboard.Serialize(presenter.Manager.GetCell(presenter.CurrentCell));

		await StoreAsync(data, window);
	}

	public static string Cut(BaseDisplayPresenter presenter)
	{
		var clipboard = new CellClipboard(presenter.Manager);
		string data;
		if (presenter.HaveSelection())
		{
			var start = presenter.GetStartSelection();
			var end = presenter.GetEndSelection();
			data = clipboard.Serialize(CaptureSelection(presenter));
			for (var row = start.Row; row <= end.Row; row++)
				for (var column = start.Column; column <= end.Column; column++)
					presenter.Manager.DeleteCell(row, column);
		}
		else
		{
			data = clipboard.Serialize(presenter.Manager.GetCell(presenter.CurrentCell));
			presenter.Manager.DeleteCell(presenter.CurrentCell);
		}

		_payload = data;
		return data;
	}

	public static Task PublishAsync(string data, Window? window)
	{
		return StoreAsync(data, window);
	}

	public static Task<string?> ReadAsync(Window? window)
	{
		return ReadClipboardAsync(window);
	}

	public static void Paste(BaseDisplayPresenter presenter, string data)
	{
		if (string.IsNullOrEmpty(data))
			return;

		var clipboard = new CellClipboard(presenter.Manager);
		var doc = XDocument.Parse(data);
		if (doc.Root == null || doc.Root.Name != "CopyData")
			return;

		clipboard.LoadContentInfo(doc.Root);
		var dataElement = doc.Root.Element("Data")?.Elements().FirstOrDefault();
		if (dataElement == null)
			return;

		if (dataElement.Name == "Table")
		{
			var cells = clipboard.DeserializeCells(dataElement);
			for (var row = 0; row < cells.Length; row++)
			{
				for (var column = 0; column < cells[row].Length; column++)
				{
					var source = cells[row][column];
					if (source != null)
						presenter.Manager.CreateCell(presenter.CurrentCell.Row + row, presenter.CurrentCell.Column + column, source);
				}
			}
		}
		else if (dataElement.Name == "Cell")
		{
			var cell = clipboard.DeserializeCell(dataElement);
			if (cell == null)
				return;

			if (presenter.HaveSelection())
			{
				var start = presenter.GetStartSelection();
				var end = presenter.GetEndSelection();
				for (var row = start.Row; row <= end.Row; row++)
					for (var column = start.Column; column <= end.Column; column++)
						presenter.Manager.CreateCell(row, column, cell);
			}
			else
				presenter.Manager.CreateCell(presenter.CurrentCell.Row, presenter.CurrentCell.Column, cell);
		}
		else
		{
			var content = clipboard.DeserializeContent(dataElement);
			if (presenter.Manager.GetCell(presenter.CurrentCell) == null)
				presenter.Manager.CreateCell(presenter.CurrentCell);
			var target = presenter.Manager.GetCell(presenter.CurrentCell);
			if (target != null && content != null)
				target.Content = content.Clone(presenter.Manager);
		}
	}

	private static async Task StoreAsync(string data, Window? window)
	{
		_payload = data;
		var clipboard = window == null ? null : TopLevel.GetTopLevel(window)?.Clipboard;
		if (clipboard != null)
			await clipboard.SetTextAsync(data);
	}

	private static async Task<string?> ReadClipboardAsync(Window? window)
	{
		var clipboard = window == null ? null : TopLevel.GetTopLevel(window)?.Clipboard;
		var text = clipboard == null ? null : await clipboard.TryGetTextAsync();
		if (!string.IsNullOrEmpty(text) && text.Contains("<CopyData", StringComparison.Ordinal))
		{
			_payload = text;
			return text;
		}

		if (string.IsNullOrEmpty(text))
			return _payload;

		return null;
	}

	private static Cell?[][] CaptureSelection(BaseDisplayPresenter presenter)
	{
		var start = presenter.GetStartSelection();
		var end = presenter.GetEndSelection();
		var cells = new Cell[end.Row - start.Row + 1][];
		var processed = new Dictionary<Cell, bool>();
		for (var row = start.Row; row <= end.Row; row++)
		{
			cells[row - start.Row] = new Cell[end.Column - start.Column + 1];
			for (var column = start.Column; column <= end.Column; column++)
			{
				var cell = presenter.Manager.GetCell(row, column);
				if (cell == null || processed.ContainsKey(cell))
					continue;

				cells[row - start.Row][column - start.Column] = cell;
				processed.Add(cell, true);
			}
		}

		return cells!;
	}

	private string Serialize(object? data)
	{
		_styles = new List<BaseStyle>();
		_expressions = new List<BaseExpression>();
		_resources = new Dictionary<string, string>();

		var doc = new XDocument(new XElement("CopyData"));
		var dataElement = new XElement("Data");
		if (data is Cell[][] table)
			Serialize(dataElement, table);
		else if (data is Cell cell)
			Serialize(dataElement, cell);
		else if (data is BaseContent content)
			Serialize(dataElement, content);

		var stylesElement = new XElement("Styles");
		foreach (var style in _styles)
			stylesElement.Add(style.Serialize());

		var exprElement = new XElement("Expressions");
		foreach (var expr in _expressions)
			exprElement.Add(expr.Serialize());

		var resElement = new XElement("Resources");
		foreach (var pair in _resources)
			resElement.Add(new XElement("Resource", new XAttribute("Name", pair.Key), pair.Value));

		doc.Root?.Add(dataElement, stylesElement, exprElement, resElement);
		return doc.ToString();
	}

	private void Serialize(XElement root, Cell[][] cells)
	{
		var table = new XElement("Table");
		for (var rowIndex = 0; rowIndex < cells.Length; rowIndex++)
		{
			var row = new XElement("Row");
			table.Add(row);
			for (var columnIndex = 0; columnIndex < cells[rowIndex].Length; columnIndex++)
				Serialize(row, cells[rowIndex][columnIndex]);
		}

		root.Add(table);
	}

	private void Serialize(XElement root, Cell? cell)
	{
		if (cell == null)
		{
			root.Add(new XElement("EmptyCell"));
			return;
		}

		root.Add(cell.Serialize());
		if (!string.IsNullOrEmpty(cell.StyleID))
		{
			var style = cell.Style;
			if (_styles.All(item => item.GetType() != style.GetType() || item.ID != style.ID))
				_styles.Add(style);
		}

		if (cell.Content != null)
			SaveContentInfo(cell.Content);
	}

	private void Serialize(XElement root, BaseContent content)
	{
		root.Add(content.Serialize());
		SaveContentInfo(content);
	}

	private void SaveContentInfo(BaseContent content)
	{
		if (content is CollectionContent collection)
		{
			foreach (var item in collection.GetContents())
				SaveContentInfo(item);
			return;
		}

		if (!string.IsNullOrEmpty(content.StyleID))
		{
			var style = content.GetStyle();
			if (style != null && _styles.All(item => item.GetType() != style.GetType() || item.ID != style.ID))
				_styles.Add(style);
		}

		foreach (var param in content.GetParams())
		{
			if (string.IsNullOrEmpty(param.ExpressionID))
				continue;

			var expr = param.Expression;
			if (expr != null && _expressions.All(item => item.GetType() != expr.GetType() || item.ID != expr.ID))
				_expressions.Add(expr);
		}

		if (content is ImageContent)
		{
			var uri = content.GetParamValueByTarget("Uri") as string;
			if (string.IsNullOrEmpty(uri) || !uri.ToUpper().StartsWith("RESOURCE"))
				return;

			var name = uri.Substring(uri.IndexOf("://", StringComparison.Ordinal) + 3);
			if (_resources.ContainsKey(name))
				return;

			var bytes = _manager.GetResource(name);
			if (bytes != null && bytes.Length > 0)
				_resources.Add(name, Convert.ToBase64String(bytes));
		}
	}

	private Cell?[][] DeserializeCells(XElement element)
	{
		var rows = element.Elements("Row").ToList();
		var cells = new Cell?[rows.Count][];
		for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
		{
			var columns = rows[rowIndex].Elements().ToList();
			cells[rowIndex] = new Cell?[columns.Count];
			for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
				cells[rowIndex][columnIndex] = DeserializeCell(columns[columnIndex]);
		}

		return cells;
	}

	private Cell? DeserializeCell(XElement element)
	{
		if (element.Name == "EmptyCell")
			return null;

		var cell = Cell.Parse(_manager, element);
		cell.Content = BaseContent.Parse(_manager, element);
		ReplaceContentExpressionIds(cell.Content);
		return cell;
	}

	private BaseContent? DeserializeContent(XElement element)
	{
		var content = BaseContent.ParseContent(_manager, element);
		ReplaceContentExpressionIds(content);
		return content;
	}

	private void LoadContentInfo(XElement root)
	{
		var stylesElement = root.Element("Styles");
		if (stylesElement != null)
		{
			foreach (var element in stylesElement.Elements())
				_manager.ReplaceStyle(BaseStyle.Parse(element));
		}

		_exprIds = new Dictionary<string, string>();
		var exprElement = root.Element("Expressions");
		if (exprElement != null)
		{
			foreach (var element in exprElement.Elements())
			{
				var expression = BaseExpression.Parse(element);
				var oldId = expression.ID;
				_manager.ReplaceExpression(expression);
				if (expression.ID != oldId)
					_exprIds.Add(oldId, expression.ID);
			}
		}

		var resElement = root.Element("Resources");
		if (resElement == null)
			return;

		foreach (var element in resElement.Elements())
		{
			var name = element.Attribute("Name")?.Value;
			if (string.IsNullOrEmpty(name))
				continue;

			_manager.AddResource(name, Convert.FromBase64String(element.Value));
		}
	}

	private void ReplaceContentExpressionIds(BaseContent? content)
	{
		if (content == null)
			return;

		foreach (var pair in _exprIds)
			content.ReplaceExpressionId(pair.Key, pair.Value);
	}
}
