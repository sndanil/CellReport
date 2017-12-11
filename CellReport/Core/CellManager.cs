using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CellReport.Core.Expressions;
using CellReport.Core.BookMarks;
using CellReport.Core.Contents;

namespace CellReport.Core
{
	[Serializable]
	public class CellManager : NotifyPropertyChanged
	{
		public static bool IsDesignMode = false;

		private Column[] _columns;
		private float[] _rows;
		private int _actualRowsCount;
		private int _actualColumnsCount;
		private Cell[][] _cells;
		private PrintSettings _printSettings;
		private DisplaySettings _displaySettings;
		private ExportSettings _exportSettings;
		private Properties _properties;
		private readonly Dictionary<int, bool> _autoFitRows = new Dictionary<int, bool>();
		private readonly Dictionary<BaseExpression, int> _expressions = new Dictionary<BaseExpression, int>();
		private readonly Dictionary<string, TemplateSection> _sections = new Dictionary<string, TemplateSection>();
		private readonly List<BookMark> _bookMarks = new List<BookMark>();
		private readonly List<int> _pageBreakRows = new List<int>();

		private readonly List<BaseStyle> _stylesList = new List<BaseStyle>();
		private readonly List<ExternalStyle> _externalStyles = new List<ExternalStyle>();

		private readonly Dictionary<string, byte[]> _resources = new Dictionary<string, byte[]>();

		public CellManager()
		{
			InitFields();
			FitArraysByProperties();
		}

		public CellManager(int rowsCount, int columnsCount)
		{
			InitFields();
			Properties.DefaultRowsCount = rowsCount;
			Properties.DefaultColumnsCount = columnsCount;
			FitArraysByProperties();
		}

		public CellManager(int rowsCount, int columnsCount, float defaultRowHeight, Column defaultColumn)
		{
			InitFields();
			Properties.DefaultRowHeight = defaultRowHeight;
			Properties.DefaultColumn = defaultColumn;
			Properties.DefaultRowsCount = rowsCount;
			Properties.DefaultColumnsCount = columnsCount;
			FitArraysByProperties();
		}

		public CellManager(Properties properties)
		{
			InitFields(properties);
			FitArraysByProperties();
		}

		private void FitArraysByProperties()
		{
			ArrayHelper.SetNewLength(ref _rows, Properties.DefaultRowsCount, Properties.DefaultRowHeight);
			ArrayHelper.SetNewLength(ref _columns, Properties.DefaultColumnsCount, Properties.DefaultColumn);
			ArrayHelper.SetNewLength(ref _cells, Properties.DefaultRowsCount, Properties.DefaultColumnsCount);
		}

		private void InitFields(Properties properties = null)
		{
			PrintSettings = new PrintSettings();
			DisplaySettings = new DisplaySettings();
			ExportSettings = new ExportSettings();
			Properties = properties ?? new Properties();
		}

		public void AddResource(string name, byte[] data)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");
			if (data == null)
				throw new ArgumentNullException("data");
			_resources[name] = data;
			OnPropertyChanged("Resources");
		}

		public void RemoveResource(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");
			if (_resources.ContainsKey(name))
			{
				_resources.Remove(name);
				OnPropertyChanged("Resources");
			}
		}

		public byte[] GetResource(string name)
		{
			byte[] data;
			if (!_resources.TryGetValue(name, out data))
				data = new byte[0];

			return data;
		}

		public IEnumerable<string> GetResourceNames()
		{
			foreach (var name in _resources.Keys)
				yield return name;
		}

		public void AddExpression(BaseExpression expression)
		{
			_expressions[expression] = 1;
			expression.PropertyChanged += OnPropertyChanged;
			OnPropertyChanged("Expressions");
		}

		public void ReplaceExpression(BaseExpression expression)
		{
			if (expression == null)
				return;

			var oldExpr = GetExpressions().FirstOrDefault(x => x.ID == expression.ID);
			if (oldExpr != null)
			{
				if (oldExpr.GetType() != expression.GetType())
				{
					var i = 0;
					do
					{
						i++;
					} while (GetExpressionByID(expression.ID + i.ToString()) != null);
					expression.ID += i.ToString();
				}
				else
					RemoveExpression(oldExpr);
			}
			AddExpression(expression);
			OnPropertyChanged("Expressions");
		}

		public void RemoveExpression(BaseExpression expression)
		{
			_expressions.Remove(expression);
			expression.PropertyChanged -= OnPropertyChanged;
			OnPropertyChanged("Expressions");
		}

		public IEnumerable<BaseExpression> GetExpressions()
		{
			foreach (var expression in _expressions.Keys)
				yield return expression;
		}

		public BaseExpression GetExpressionByID(string id)
		{
			return (from exp in GetExpressions() where exp.ID == id select exp).FirstOrDefault();
		}

		public void AddPageBreakRow(int row)
		{
			if (!_pageBreakRows.Contains(row))
				_pageBreakRows.Add(row);
		}

		public void RemovePageBreakRow(int row)
		{
			_pageBreakRows.Remove(row);
		}

		public bool IsPageBreakRow(int row)
		{
			return _pageBreakRows.Contains(row);
		}

		public void AddSection(TemplateSection section)
		{
			_sections[section.Name] = section;
			section.PropertyChanged += OnPropertyChanged;
			OnPropertyChanged("Sections");
		}

		public void RemoveSection(TemplateSection section)
		{
			_sections.Remove(section.Name);
			section.PropertyChanged -= OnPropertyChanged;
			OnPropertyChanged("Sections");
		}

		public TemplateSection GetSectionByName(string name)
		{
			if (!_sections.ContainsKey(name))
				throw new ApplicationException("Section not found");

			return _sections[name];
		}

		public IEnumerable<TemplateSection> GetSections()
		{
			foreach (var item in _sections.Values)
				yield return item;
		}

		public void AddBookMark(BookMarks.BookMark bookMark)
		{
			_bookMarks.Add(bookMark);
			OnPropertyChanged("BookMarks");
		}

		public void RemoveBookMark(BookMarks.BookMark bookMark)
		{
			_bookMarks.Remove(bookMark);
			OnPropertyChanged("BookMarks");
		}

		public IEnumerable<BookMarks.BookMark> GetBookMarks()
		{
			foreach (var item in _bookMarks)
				yield return item;
		}

		public byte[] GetDataByUri(string uri)
		{
			try
			{
				if (uri.ToUpper().StartsWith("RESOURCE"))
				{
					var name = uri.Substring(uri.IndexOf("://") + 3);
					return GetResource(name);
				}
				if (uri.ToUpper().StartsWith("FILE"))
				{
					var path = uri.Substring(uri.IndexOf("://") + 3);
					var resolved = FileResolver.Resolve(path);
					if (resolved != null)
						return resolved;
					if (System.IO.File.Exists(path))
						return System.IO.File.ReadAllBytes(path);
					if (System.IO.File.Exists(System.IO.Path.GetFileName(path)))
						return System.IO.File.ReadAllBytes(System.IO.Path.GetFileName(path));
					var localPath = System.IO.Path.Combine(
						System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
						System.IO.Path.GetFileName(path));
					if (System.IO.File.Exists(localPath))
						return System.IO.File.ReadAllBytes(localPath);
				}
				else if (uri.ToUpper().StartsWith("HTTP"))
				{
					using (System.Net.WebClient client = new System.Net.WebClient())
						return client.DownloadData(uri);
				}

				return null;
			}
			catch
			{
				return null;
			}
		}

		public PrintSettings PrintSettings
		{
			get { return _printSettings; }
			set { SetField(ref _printSettings, value); }
		}

		public DisplaySettings DisplaySettings
		{
			get { return _displaySettings; }
			set { SetField(ref _displaySettings, value); }
		}

		public ExportSettings ExportSettings
		{
			get { return _exportSettings; }
			set { SetField(ref _exportSettings, value); }
		}

		public Properties Properties
		{
			get { return _properties; }
			set { SetField(ref _properties, value); }
		}

		public int ColumnsCount
		{
			get { return ArrayHelper.GetLength(_columns); }
			set
			{
				ArrayHelper.SetNewLength(ref _columns, value, Properties.DefaultColumn);
				ArrayHelper.SetNewLength(ref _cells, RowsCount, value);
				OnPropertyChanged("ColumnsCount");
			}
		}

		public int RowsCount
		{
			get { return ArrayHelper.GetLength(_rows); }
			set
			{
				if (_rows.Length > value)
				{
					for (int i = value; i < _rows.Length; i++)
					{
						if (_autoFitRows.ContainsKey(i))
							_autoFitRows.Remove(i);
					}
				}
				ArrayHelper.SetNewLength(ref _rows, value, Properties.DefaultRowHeight);
				ArrayHelper.SetNewLength(ref _cells, value, ColumnsCount);
				OnPropertyChanged("RowsCount");
			}
		}

		public int ActualRowsCount
		{
			get { return _actualRowsCount; }
		}

		public int ActualColumnsCount
		{
			get { return _actualColumnsCount; }
		}

		private void ResizeIfNeed(int row, int column)
		{
			if (row >= RowsCount)
				RowsCount = row + 1;

			if (column >= ColumnsCount)
				ColumnsCount = column + 1;
		}

		public void InsertRows(int startIndex, int count, bool fitSections = false)
		{
			var oldRowsCount = RowsCount;
			RowsCount += count;
			for (int row = oldRowsCount - 1; row >= startIndex; row--)
			{
				for (int col = 0; col < ColumnsCount; col++)
				{
					var cell = GetCell(row, col);
					if (cell != null && cell.Location.Row == row && cell.Location.Column == col)
					{
						CreateCell(row + count, col, cell);
						DeleteCell(row, col);
					}
				}
				SetRowHeight(row + count, GetRowHeight(row));
				SetRowHeight(row, Properties.DefaultRowHeight);
			}
			if (fitSections)
			{
				foreach (var section in GetSections())
				{
					if (section.Top > startIndex)
					{
						var oldTop = section.Top;
						var oldBottom = section.Bottom;
						section.Top = oldTop + count;
						section.Bottom = oldBottom + count;
					}
					else if (section.Top < startIndex && section.Bottom >= startIndex)
					{
						var oldBottom = section.Bottom;
						section.Bottom = oldBottom + count;
					}
				}
			}

			OnPropertyChanged("Rows");
		}

		public void InsertColumns(int startIndex, int count, bool fitSections = false)
		{
			var oldColsCount = ColumnsCount;
			ColumnsCount += count;
			for (int col = oldColsCount - 1; col >= startIndex; col--)
			{
				for (int row = 0; row < RowsCount; row++)
				{
					var cell = GetCell(row, col);
					if (cell != null && cell.Location.Row == row && cell.Location.Column == col)
					{
						CreateCell(row, col + count, cell);
						DeleteCell(row, col);
					}
				}
				SetColumnWidth(col + count, GetColumnWidth(col));
				SetColumnWidth(col, Properties.DefaultColumn.Width);
			}
			if (fitSections)
			{
				foreach (var section in GetSections())
				{
					if (section.Left > startIndex)
					{
						var oldLeft = section.Left;
						var oldRight = section.Right;
						section.Left = oldLeft + count;
						section.Right = oldRight + count;
					}
					else if (section.Left < startIndex && section.Right >= startIndex)
					{
						var oldRight = section.Right;
						section.Right = oldRight + count;
					}
				}
			}

			OnPropertyChanged("Columns");
		}

		public void DeleteRows(int startIndex, int count, bool fitSections = false)
		{
			for (int row = startIndex; row < startIndex + count; row++)
			{
				for (int col = 0; col < ColumnsCount; col++)
				{
					var cell = GetCell(row, col);
					if (cell != null)
					{
						if (cell.Location.Row == row && cell.Location.Column == col)
							DeleteCell(row, col);
						else
							cell.Span = new CellSpan {ColSpan = 0, RowSpan = 0};
					}
				}
			}
			for (int row = startIndex + count; row < RowsCount; row++)
			{
				for (int col = 0; col < ColumnsCount; col++)
				{
					var cell = GetCell(row, col);
					if (cell != null && cell.Location.Row == row && cell.Location.Column == col)
					{
						var newCell = CreateCell(row - count, col, cell);
						if (newCell != GetCell(row, col))
							DeleteCell(row, col);
					}
				}
				SetRowHeight(row - count, GetRowHeight(row));
			}
			if (fitSections)
			{
				foreach (var section in GetSections())
				{
					if (section.Top > startIndex)
					{
						var oldTop = section.Top;
						var oldBottom = section.Bottom;
						section.Top = oldTop - count;
						section.Bottom = oldBottom - count;
					}
					else if (section.Top < startIndex && section.Bottom >= startIndex)
					{
						var oldBottom = section.Bottom;
						section.Bottom = oldBottom - count;
					}
				}
			}
			RowsCount -= count;
			OnPropertyChanged("Rows");
		}

		public void DeleteColumns(int startIndex, int count, bool fitSections = false)
		{
			for (int col = startIndex; col < startIndex + count; col++)
			{
				for (int row = 0; row < RowsCount; row++)
				{
					var cell = GetCell(row, col);
					if (cell != null)
					{
						if (cell.Location.Row == row && cell.Location.Column == col)
							DeleteCell(row, col);
						else
							cell.Span = new CellSpan {ColSpan = 0, RowSpan = 0};
					}
				}
			}
			for (int col = startIndex + count; col < ColumnsCount; col++)
			{
				for (int row = 0; row < RowsCount; row++)
				{
					var cell = GetCell(row, col);
					if (cell != null && cell.Location.Row == row && cell.Location.Column == col)
					{
						var newCell = CreateCell(row, col - count, cell);
						if (newCell != GetCell(row, col))
							DeleteCell(row, col);
					}
				}
				SetColumnWidth(col - count, GetColumnWidth(col));
			}
			if (fitSections)
			{
				foreach (var section in GetSections())
				{
					if (section.Left > startIndex)
					{
						var oldLeft = section.Left;
						var oldRight = section.Right;
						section.Left = oldLeft - count;
						section.Right = oldRight - count;
					}
					else if (section.Left < startIndex && section.Right >= startIndex)
					{
						var oldRight = section.Right;
						section.Right = oldRight - count;
					}
				}
			}
			ColumnsCount -= count;
			OnPropertyChanged("Columns");
		}

		public void DeleteCell(CellLocation location)
		{
			DeleteCell(location.Row, location.Column);
		}

		public void DeleteCell(int row, int column)
		{
			if (_cells[row][column] != null)
			{
				var cell = _cells[row][column];
				_cells[row][column].Span = new CellSpan {ColSpan = 0, RowSpan = 0};
				_cells[cell.Location.Row][cell.Location.Column] = null;

				if (row == _actualRowsCount - 1 || column == _actualColumnsCount - 1)
				{
					var found = false;
					for (var colIndex = _actualColumnsCount - 1; colIndex >= 0; colIndex--)
					{
						for (var rowIndex = 0; rowIndex < _cells.Length; rowIndex++)
						{
							if (colIndex < _cells[rowIndex].Length && _cells[rowIndex][colIndex] != null)
							{
								found = true;
								break;
							}
						}
						if (found)
							break;
						_actualColumnsCount--;
					}
					for (var rowIndex = _actualRowsCount - 1; rowIndex >= 0; rowIndex--)
					{
						for (var colIndex = 0; colIndex < _actualColumnsCount; colIndex++)
						{
							if (rowIndex < _cells.Length && colIndex < _cells[rowIndex].Length && _cells[rowIndex][colIndex] != null)
							{
								found = true;
								break;
							}
						}
						if (found)
							break;
						_actualRowsCount--;
					}
				}
			}
			OnPropertyChanged("Cells");
		}

		public Cell CreateCell(CellLocation location)
		{
			return CreateCell(location.Row, location.Column);
		}

		public Cell CreateCell(int row, int column)
		{
			ResizeIfNeed(row, column);
			DeleteCell(row, column);
			CheckRowAndColumn(row, column);
			_actualRowsCount = Math.Max(_actualRowsCount, row + 1);
			_actualColumnsCount = Math.Max(_actualColumnsCount, column + 1);
			var cell = new Cell(this, row, column);
			cell.PropertyChanged += OnPropertyChanged;
			_cells[row][column] = cell;

			OnPropertyChanged("Cells");
			return cell;
		}

		public Cell CreateCell(int row, int column, Cell from)
		{
			if (from == null)
				return null;

			var span = from.Span;
			var cell = CreateCell(row, column);
			cell.Style = from.Style;
			cell.Span = span;
			if (from.Content != null)
				cell.Content = from.Content.Clone(this);

			OnPropertyChanged("Cells");
			return cell;
		}

		private void CheckRowAndColumn(int row, int column)
		{
			if (row < 0 || column < 0)
				throw new ArgumentException(string.Format("Incorrect value for 'row' ({0}) or 'column' ({1})", row, column));
			if (row >= RowsCount || column >= ColumnsCount)
				ResizeIfNeed(row, column);
		}

		public Cell GetCell(CellLocation location)
		{
			return GetCell(location.Row, location.Column);
		}

		public Cell GetCell(int row, int column)
		{
			CheckRowAndColumn(row, column);
			return _cells[row][column];
		}

		public void SetSpansForCell(CellLocation location, CellSpan span)
		{
			CheckRowAndColumn(location.Row + span.RowSpan, location.Column + span.ColSpan);

			var cell = GetCell(location) ?? CreateCell(location.Row, location.Column);
			var oldSpan = cell.Span;
			for (int row = location.Row; row <= location.Row + oldSpan.RowSpan; row++)
				for (int col = location.Column; col <= location.Column + oldSpan.ColSpan; col++)
				{
					if (row != location.Row || col != location.Column)
						_cells[row][col] = null;
				}
			for (int row = location.Row; row <= location.Row + span.RowSpan; row++)
				for (int col = location.Column; col <= location.Column + span.ColSpan; col++)
				{
					if (row != location.Row || col != location.Column)
					{
						if (_cells[row][col] != null)
							_cells[row][col].Span = new CellSpan {ColSpan = 0, RowSpan = 0};
						_cells[row][col] = cell;
						_actualRowsCount = Math.Max(_actualRowsCount, row + 1);
						_actualColumnsCount = Math.Max(_actualColumnsCount, col + 1);
					}
				}
			cell.SpanInternal = span;
			OnPropertyChanged("Cells");
		}

		public float GetRowHeight(int index)
		{
			return _rows[index];
		}

		public void SetRowHeight(int index, float height)
		{
			ResizeIfNeed(index, 0);
			if (_rows[index] != height)
			{
				_rows[index] = height < 0 ? 0 : height;
				OnPropertyChanged("RowHeight");
			}
		}

		public void SetAutoFitRow(int row, bool autofit)
		{
			if (!autofit && _autoFitRows.ContainsKey(row))
			{
				_autoFitRows.Remove(row);
				OnPropertyChanged("AutoFitRow");
			}
			if (autofit && !_autoFitRows.ContainsKey(row))
			{
				_autoFitRows.Add(row, true);
				OnPropertyChanged("AutoFitRow");
			}
		}

		public bool IsAutoFitRow(int row)
		{
			return _autoFitRows.ContainsKey(row);
		}

		public bool IsCurrectLocation(CellLocation location)
		{
			return location.Row >= 0 && location.Column >= 0 && location.Row < RowsCount && location.Column < ColumnsCount;
		}

		internal bool AutoFitted { get; set; }

		public IEnumerable<int> GetAutoFitRows()
		{
			return from item in _autoFitRows where item.Value select item.Key;
		}

		public Column GetColumn(int index)
		{
			return _columns[index];
		}

		public string GetColumnMarker(int index)
		{
			return _columns[index].Marker;
		}

		public void SetColumnMarker(int index, string marker)
		{
			_columns[index].Marker = marker;
			OnPropertyChanged("ColumnMarker");
		}

		public float GetColumnWidth(int index)
		{
			var col = _columns[index];
			return MarkerResolver.Resolve(col.Marker) ? col.Width : 0;
		}

		public void SetColumnWidth(int index, float width)
		{
			ResizeIfNeed(0, index);
			if (_columns[index].Width != width)
			{
				_columns[index].Width = width < 0 ? 0 : width;
				OnPropertyChanged("ColumnWidth");
			}
		}

		public void AddStyle(BaseStyle style)
		{
			if (style == null)
				return;
			_stylesList.Add(style);
			OnPropertyChanged("Styles");
		}

		public void ReplaceStyle(BaseStyle style)
		{
			if (style == null)
				return;

			var oldStyle = GetStyles().Where(s => s.GetType() == style.GetType() && s.ID == style.ID).FirstOrDefault();
			if (oldStyle != null)
				_stylesList.Remove(oldStyle);
			_stylesList.Add(style);
			OnPropertyChanged("Styles");
		}

		public void RemoveStyle(BaseStyle style)
		{
			if (style == null)
				return;
			_stylesList.Remove(style);
			OnPropertyChanged("Styles");
		}

		public IEnumerable<BaseStyle> GetStyles()
		{
			foreach (var style in _stylesList)
				yield return style;
		}

		public T GetStyleByID<T>(string id) where T : BaseStyle
		{
			var style = (from s in _stylesList where s.ID == id && s.GetType() == typeof (T) select (s as T)).FirstOrDefault();
			return style;
		}

		public void AddExternalStyle(ExternalStyle style)
		{
			_externalStyles.Add(style);
			var data = this.GetDataByUri(style.Uri);
			if (data != null)
			{
				var template = new CellTemplate(this);
				var xDoc = System.Xml.Linq.XDocument.Load(new System.IO.StreamReader(new System.IO.MemoryStream(data)));
				var element = xDoc.Element("CellReportTemplate");
				template.ParseStyles(element, this);
			}
			OnPropertyChanged("ExternalStyles");
		}

		internal IEnumerable<ExternalStyle> GetExtStyles()
		{
			foreach (var style in _externalStyles)
				yield return style;
		}

		public void RemoveUnusedStyles()
		{
			var unusedStyles = new List<BaseStyle>();
			var cells = _cells.SelectMany(x => x).Where(x => x != null);
			foreach (var style in _stylesList)
			{
				if (cells.All(x => !HasStyle(x, style)))
					unusedStyles.Add(style);
			}

			foreach (var style in unusedStyles)
				RemoveStyle(style);
		}

		private bool HasStyle(Cell cell, BaseStyle style)
		{
			return ((cell.Style != null) && (cell.Style.GetType() == style.GetType()) && (cell.Style.ID == style.ID)) ||
			       ((cell.Content != null) && HasStyle(cell.Content, style));
		}

		private bool HasStyle(BaseContent content, BaseStyle style)
		{
			if (content is CollectionContent)
				return ((CollectionContent) content).GetContents().Any(x => HasStyle(x, style));

			var contentStyle = content.GetStyle();
			return (contentStyle != null) && (contentStyle.GetType() == style.GetType()) && (contentStyle.ID == style.ID);
		}

		[OnDeserialized]
		internal void OnDeserializedMethod(StreamingContext context)
		{
			foreach (Cell[] row in _cells)
			{
				foreach (Cell cell in row)
				{
					if (cell != null)
						cell.SetManager(this);
				}
			}
		}

		public void OnDeserializedMethod()
		{
			OnDeserializedMethod(new StreamingContext());
		}
	}
}
