using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;
using CellReport.Studio.ViewModel;

namespace CellReport.Studio
{
	public class CopyManager
	{
		private List<BaseStyle> _styles = null;
		private List<BaseExpression> _expressions = null;
		private Dictionary<string, string> _resources = null;
		private Dictionary<string, string> _exprIds = null;

		#region Serialization

		private string Serialize(object data)
		{
			_styles = new List<BaseStyle>();
			_expressions = new List<BaseExpression>();
			_resources = new Dictionary<string, string>();

			var doc = new XDocument(new XElement("CopyData"));
			var dataElement = new XElement("Data");
			if (data is Cell[][])
				Serialize(dataElement, (Cell[][])data);
			else if (data is Cell)
				Serialize(dataElement, (Cell)data);
			else if (data is BaseContent)
				Serialize(dataElement, (BaseContent)data);

			var stylesElement = new XElement("Styles");
			foreach (var style in _styles)
				stylesElement.Add(style.Serialize());

			var exprElement = new XElement("Expressions");
			foreach (var expr in _expressions)
				exprElement.Add(expr.Serialize());

			var resElement = new XElement("Resources");
			foreach (var resPair in _resources)
				resElement.Add(new XElement("Resource", new XAttribute("Name", resPair.Key), resPair.Value));

			doc.Root.Add(dataElement, stylesElement, exprElement, resElement);
			return doc.ToString();
		}

		private void Serialize(XElement root, Cell[][] cells)
		{
			var table = new XElement("Table");
			for (int rowIndex = 0; rowIndex < cells.Length; rowIndex++)
			{
				var row = new XElement("Row");
				table.Add(row);
				for (int colIndex = 0; colIndex < cells[rowIndex].Length; colIndex++)
					Serialize(row, cells[rowIndex][colIndex]);
			}
			root.Add(table);
		}

		private void Serialize(XElement root, Cell cell)
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
				if (_styles.All(x => (x.GetType() != style.GetType()) || (x.ID != style.ID)))
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
			if (content is CollectionContent)
			{
				foreach (var item in ((CollectionContent)content).GetContents())
					SaveContentInfo(item);
				return;
			}

			if (!string.IsNullOrEmpty(content.StyleID))
			{
				var style = content.GetStyle();
				if (_styles.All(x => (x.GetType() != style.GetType()) || (x.ID != style.ID)))
					_styles.Add(style);
			}

			foreach (var param in content.GetParams())
			{
				if (string.IsNullOrEmpty(param.ExpressionID))
					continue;
				
				var expr = param.Expression;
				if (_expressions.All(x => (x.GetType() != expr.GetType()) || (x.ID != expr.ID)))
					_expressions.Add(expr);
			}

			if (content is ImageContent)
			{
				var uri = content.GetParamValueByTarget("Uri") as string;
				if (string.IsNullOrEmpty(uri) || !uri.ToUpper().StartsWith("RESOURCE"))
					return;

				var name = uri.Substring(uri.IndexOf("://") + 3);
				if (_resources.ContainsKey(name))
					return;

				var data = StudioCommands.Manager.GetResource(name);
				if ((data != null) && (data.Length > 0))
					_resources.Add(name, Convert.ToBase64String(data));
			}
		}

		#endregion

		#region Deserialization

		private Cell[][] DeserializeCells(XElement element)
		{
			var rows = element.Elements("Row").ToList();
			var cells = new Cell[rows.Count][];
			for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
			{
				var cols = rows[rowIndex].Elements().ToList();
				cells[rowIndex] = new Cell[cols.Count];
				for (int colIndex = 0; colIndex < cols.Count; colIndex++)
					cells[rowIndex][colIndex] = DeserializeCell(cols[colIndex]);
			}
			return cells;
		}

		private Cell DeserializeCell(XElement element)
		{
			if (element.Name == "EmptyCell")
				return null;

			var cell = Cell.Parse(StudioCommands.Manager, element);
			cell.Content = BaseContent.Parse(StudioCommands.Manager, element);
			ReplaceContentExpressionIds(cell.Content);
			return cell;
		}

		private BaseContent DeserializeContent(XElement element)
		{
			var content = BaseContent.ParseContent(StudioCommands.Manager, element);
			ReplaceContentExpressionIds(content);
			return content;
		}

		private void LoadContentInfo(XElement root)
		{
			var manager = StudioCommands.Manager;
			var stylesElement = root.Element("Styles");
			foreach (var element in stylesElement.Elements())
			{
				var style = BaseStyle.Parse(element);
				manager.ReplaceStyle(style);
			}

			_exprIds = new Dictionary<string, string>();
			var exprElement = root.Element("Expressions");
			foreach (var element in exprElement.Elements())
			{
				var newExpr = BaseExpression.Parse(element);
				var oldId = newExpr.ID;
				manager.ReplaceExpression(newExpr);
				if (newExpr.ID != oldId)
					_exprIds.Add(oldId, newExpr.ID);
			}

			var resElement = root.Element("Resources");
			foreach (var element in resElement.Elements())
			{
				var name = element.Attribute("Name").Value;
				var data = Convert.FromBase64String(element.Value);
				manager.AddResource(name, data);
			}
		}

		private void ReplaceContentExpressionIds(BaseContent content)
		{
			if (content != null)
			{
				foreach (var pair in _exprIds)
					content.ReplaceExpressionId(pair.Key, pair.Value);
			}
		}

		#endregion

		#region Copy & Paste

		public static void Copy()
		{
			var presenter = StudioCommands.Presenter;
			string data = null;
			var cm = new CopyManager();
			if (presenter.HaveSelection())
			{
				var start = presenter.GetStartSelection();
				var end = presenter.GetEndSelection();
				var arrCell = new Cell[end.Row - start.Row + 1][];
				var processedCells = new Dictionary<Cell, bool>();
				for (int row = start.Row; row <= end.Row; row++)
				{
					arrCell[row - start.Row] = new Cell[end.Column - start.Column + 1];
					for (int col = start.Column; col <= end.Column; col++)
					{
						var cell = presenter.Manager.GetCell(row, col);
						if ((cell == null) || processedCells.ContainsKey(cell))
							continue;

						arrCell[row - start.Row][col - start.Column] = cell;
						processedCells.Add(cell, true);
					}
				}
				data = cm.Serialize(arrCell);
			}
			else
				data = cm.Serialize(presenter.Manager.GetCell(presenter.CurrentCell));
			Save(data);
		}

		public static void CopyContent()
		{
			var presenter = StudioCommands.Presenter;
			if (presenter.Manager.GetCell(presenter.CurrentCell) == null)
				return;

			var cm = new CopyManager();
			var data = cm.Serialize(presenter.Manager.GetCell(presenter.CurrentCell).Content);
			Save(data);
		}

		public static void Paste()
		{
			var presenter = StudioCommands.Presenter;
			var data = Load();
			if (string.IsNullOrEmpty(data))
				return;

			var cm = new CopyManager();
			var doc = XDocument.Parse(data);
			cm.LoadContentInfo(doc.Root);
			
			var dataElement = doc.Root.Element("Data").Elements().FirstOrDefault();
			if (dataElement == null)
				return;

			if (dataElement.Name == "Table")
			{
				var cells = cm.DeserializeCells(dataElement);
				for (int row = 0; row < cells.Length; row++)
					for (int col = 0; col < cells[row].Length; col++)
						presenter.Manager.CreateCell(presenter.CurrentCell.Row + row, presenter.CurrentCell.Column + col, cells[row][col]);
			}
			else if (dataElement.Name == "Cell")
			{
				var cell = cm.DeserializeCell(dataElement);
				if (presenter.HaveSelection())
				{
					var start = presenter.GetStartSelection();
					var end = presenter.GetEndSelection();
					for (int row = start.Row; row <= end.Row; row++)
						for (int col = start.Column; col <= end.Column; col++)
							presenter.Manager.CreateCell(row, col, cell);
				}
				else
					presenter.Manager.CreateCell(presenter.CurrentCell.Row, presenter.CurrentCell.Column, cell);
			}
			else
			{
				var content = cm.DeserializeContent(dataElement);
				if (presenter.Manager.GetCell(presenter.CurrentCell) == null)
					presenter.Manager.CreateCell(presenter.CurrentCell);
				presenter.Manager.GetCell(presenter.CurrentCell).Content = content.Clone(presenter.Manager);
			}
		}

		private static void Save(string data)
		{
			var format = DataFormats.GetDataFormat(typeof(CopyManager).FullName);
			var dataObj = new DataObject();
			dataObj.SetData(format.Name, data, false);
			Clipboard.SetDataObject(dataObj, true);
		}

		private static string Load()
		{
			var dataObj = Clipboard.GetDataObject();
			var format = typeof(CopyManager).FullName;
			if (dataObj.GetDataPresent(format))
				return dataObj.GetData(format) as string;
			return null;
		}

		public static bool HasData
		{
			get { return !string.IsNullOrEmpty(Load()); }
		}

		#endregion
	}
}
