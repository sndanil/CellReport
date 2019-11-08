using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CellReport.Core;
using CellReport.Core.BookMarks;
using CellReport.Core.Contents;

namespace CellReport
{
	[Serializable]
	public class ReportDocument
	{
		[NonSerialized] private readonly CellTemplate _template;
		[NonSerialized] private int _rowNumberForAddSection;
		[NonSerialized] private int _rowNumberForJoinSection;
		[NonSerialized] private int _colNumberForJoinSection;
		[NonSerialized] private readonly List<Cell> _lastAddedCells = new List<Cell>();
		[NonSerialized] private string _lastSectionName;
		[NonSerialized] private BookMark _lastBookMark;

		[field:NonSerialized]
		public event Action<string> SectionAttached;

		[field: NonSerialized]
		public event Action<ISectionObject> SetDefaultSectionObject;

		public CellManager Manager { get; private set; }

		public ReportDocument(CellTemplate template)
		{
			_template = template ?? throw new ArgumentNullException(nameof(template));
			CreateManager();
		}

		public ReportDocument(CellTemplate template, CellManager manager)
		{
			_template = template ?? throw new ArgumentNullException(nameof(template));
			if (manager != null)
				Manager = manager;
			else
				CreateManager();
		}

		public ReportDocument(string templateFileName)
		{
			_template = new CellTemplate();
			_template.Load(templateFileName);
			CreateManager();
		}

		public ReportDocument(System.IO.Stream stream)
		{
			_template = new CellTemplate();
			_template.Load(stream);
			CreateManager();
		}

		private void CreateManager()
		{
			var rowsCount = Math.Max(1, _template.GetMaxRowsCountInSections());
			var colsCount = Math.Max(1, _template.GetMaxColumnsCountInSections());
			Manager = new CellManager(rowsCount, colsCount, _template.Manager.Properties.DefaultRowHeight, _template.Manager.Properties.DefaultColumn)
			{
				DisplaySettings = _template.Manager.DisplaySettings,
				ExportSettings = _template.Manager.ExportSettings
			};
			Manager.PrintSettings = _template.Manager.PrintSettings.Clone(Manager);
			Manager.Properties = _template.Manager.Properties;
			foreach (var resourceName in _template.Manager.GetResourceNames())
				Manager.AddResource(resourceName, _template.Manager.GetResource(resourceName));
			foreach (var expr in _template.Manager.GetExpressions())
				Manager.AddExpression(expr.Clone());
			foreach (var style in _template.Manager.GetStyles())
				Manager.AddStyle(style);
			//foreach (var style in _template.Manager.GetExtStyles())
			//	Manager.AddExternalStyle(style);
		}

		private void CreateBookMarks()
		{
			if (string.IsNullOrEmpty(_lastSectionName))
			{
				foreach (var item in _template.Manager.GetBookMarks())
				{
					if (string.IsNullOrEmpty(item.SectionID))
						Manager.AddBookMark(item.Clone());
				}
			}
			else
			{
				foreach (var item in _template.Manager.GetBookMarks())
					AddBookMark(item, null);
			}
		}

		private void AddBookMark(BookMark item, BookMark parent)
		{
			bool addBookMark = false;
			var bookMark = item.Clone();
			bookMark.Child.Clear();
			if (_lastSectionName.Equals(item.SectionID))
			{
				foreach (var cell in _lastAddedCells)
				{
					var @break = false;
					if (cell.Content != null)
					{
						foreach (var param in cell.Content.GetParams())
						{
							if (item.TextParamName == param.Name)
							{
								if (!string.IsNullOrEmpty(param.Value as string))
									bookMark.Name = param.Value.ToString();
								if (!string.IsNullOrEmpty(item.VisibleParamName))
									addBookMark = param.Value != null;
								else
									addBookMark = true;
								bookMark.Location = cell.Location;
								@break = true;
								break;
							}
						}
					}
					if (@break)
						break;
				}
			}
			if (addBookMark)
			{
				if (parent == null)
					Manager.AddBookMark(bookMark);
				else if (_lastBookMark == null)
					Manager.AddBookMark(bookMark);
				else if (_lastBookMark.SectionID == parent.SectionID)
					_lastBookMark.Child.Add(bookMark);
				else
				{
					var lastInTree = Manager.GetBookMarks().LastOrDefault();
					if (lastInTree == null)
						Manager.AddBookMark(bookMark);
					else if (!AddIfSectionEqual(parent.SectionID, lastInTree, bookMark))
						Manager.AddBookMark(bookMark);
				}

				_lastBookMark = bookMark;
			}
			foreach (var child in item.Child)
				AddBookMark(child, item);
		}

		private bool AddIfSectionEqual(string sectionName, BookMark parent, BookMark child)
		{
			if (parent.SectionID == sectionName)
			{
				parent.Child.Add(child);
				return true;
			}
			foreach (var item in parent.Child)
			{
				if (AddIfSectionEqual(sectionName, item, child))
					return true;
			}

			return false;
		}

		public void AddPageBreak()
		{
			if (_rowNumberForAddSection > 0)
				Manager.AddPageBreakRow(_rowNumberForAddSection - 1);
		}

		public void AddSection(string sectionName)
		{
			CreateBookMarks();
			_lastSectionName = sectionName;
			var section = _template.GetSectionByName(sectionName);
			CreateCells(_rowNumberForAddSection, 0, section);
			_rowNumberForJoinSection = _rowNumberForAddSection;
			_rowNumberForAddSection += section.Height;
			_colNumberForJoinSection = section.Width;
			if (section.IsHeaderFixed)
				Manager.PrintSettings.HeaderFixedRows.Add(new Range(_rowNumberForAddSection - section.Height, _rowNumberForAddSection - 1));
			if (section.IsFooterFixed)
				Manager.PrintSettings.FooterFixedRows.Add(new Range(_rowNumberForAddSection - section.Height, _rowNumberForAddSection - 1));

			SectionAttached?.Invoke(sectionName);
		}

		public ReportDocument AddSection(string sectionName, object obj)
		{
			if (obj is IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					AddSection(sectionName);
					ProcessSection(item);
				}
			}
			else
			{
				AddSection(sectionName);
				ProcessSection(obj);
			}

			return this;
		}

		private void ProcessSection(object obj)
		{
			if (obj != null)
			{
				if (obj is ISectionObject sectionObject)
					SetSectionObject(sectionObject);
				else
				{
					foreach (var property in obj.GetType().GetProperties())
					{
						var value = property.GetValue(obj, null);
						if (value is ISectionObject sectionObject2)
							SetSectionObject(sectionObject2);
						else
							SetParameter(property.Name, value);
					}
				}
			}
		}

		public void JoinSection(string sectionName)
		{
			CreateBookMarks();
			_lastSectionName = sectionName;
			var section = _template.GetSectionByName(sectionName);
			CreateCells(_rowNumberForJoinSection, _colNumberForJoinSection, section);
			_colNumberForJoinSection += section.Width;

			SectionAttached?.Invoke(sectionName);
		}

		public ReportDocument JoinSection(string sectionName, object obj)
		{
			if (obj is IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					JoinSection(sectionName);
					ProcessSection(item);
				}
			}
			else
			{
				JoinSection(sectionName);
				ProcessSection(obj);
			}

			return this;
		}

		public void SetParameter(string paramName, object value)
		{
			foreach (var cell in _lastAddedCells)
			{
				cell.Content?.SetContentParam(paramName, value);
			}
		}

		public void SetSectionObject(ISectionObject obj)
		{
			foreach (var cell in _lastAddedCells)
			{
				foreach (var param in cell.Content?.GetParams() ?? new Param[0])
				{
					var value = obj.GetParamValue(param.Name) ?? obj.GetParamValue(param.Assign);

					if (value != null)
						SetParameter(param.Name, value);
				}
			}

			SetDefaultSectionObject?.Invoke(obj);
		}

		public void SetSectionObject(string prefix, ISectionObject obj)
		{
			foreach (var cell in _lastAddedCells)
			{
				foreach (var param in cell.Content?.GetParams() ?? new Param[0])
				{
					if (param.Name?.StartsWith(prefix) == true)
					{
						var value = obj.GetParamValue(param.Name.Replace(prefix, string.Empty));
						if (value != null)
							SetParameter(param.Name, value);
					}
					if (param.Assign?.StartsWith(prefix) == true)
					{
						var value = obj.GetParamValue(param.Assign.Replace(prefix, string.Empty));
						if (value != null)
							SetParameter(param.Name, value);
					}
				}
			}
		}

		public void FitDocument()
		{
			Manager.RowsCount = Manager.ActualRowsCount;
			Manager.ColumnsCount = Manager.ActualColumnsCount;
		}

		private void CreateCells(int rowOffset, int colOffset, TemplateSection section)
		{
			_lastAddedCells.Clear();
			if (Manager.RowsCount < (rowOffset + section.Height))
			{
				if (Manager.RowsCount < 5000)
					Manager.RowsCount = Manager.RowsCount*2; // rowOffset + section.Height;
				else
					Manager.RowsCount = Manager.RowsCount + section.Height*100;
			}
			if (Manager.ColumnsCount < (colOffset + section.Width))
				Manager.ColumnsCount = colOffset + section.Width;
			foreach (var cell in section.GetCells(_template.Manager))
			{
				var sectionLocation = section.TranslateToSectionOffset(cell);
				var newCell = Manager.CreateCell(rowOffset + sectionLocation.Row, colOffset + sectionLocation.Column, cell);
				newCell.Content?.ExecuteExpressions();
				_lastAddedCells.Add(newCell);
			}
			foreach (var rowIndex in section.GetAutoFitRows(_template.Manager))
			{
				if (rowIndex >= section.Top && rowIndex <= section.Bottom)
					Manager.SetAutoFitRow((rowIndex - section.Top) + rowOffset, true);
			}
			int index = 0;
			foreach (var height in section.GetRowsHeights(_template.Manager))
			{
				Manager.SetRowHeight(index + rowOffset, height);
				index++;
			}
			index = 0;
			foreach (var width in section.GetColumnWidths(_template.Manager))
			{
				Manager.SetColumnWidth(index + colOffset, width);
				index++;
			}
		}
	}
}
