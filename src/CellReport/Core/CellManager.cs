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
		public bool IsDesignMode { get; set; }

		private Column[] _columns;
		private float[] _rows;
		private int _actualRowsCount;
		private int _actualColumnsCount;
		private Cell[][] _cells;
		[NonSerialized]
		private readonly List<Cell> _originCells = new List<Cell>();
		[NonSerialized]
		private Dictionary<int, List<Cell>> _spanCellsByRow;
		private PrintSettings _printSettings;
		private DisplaySettings _displaySettings;
		private ExportSettings _exportSettings;
		private BookmarkSettings _bookmarkSettings;
		[NonSerialized]
		private Action _bookMarksCompletion;
		private Properties _properties;
		private readonly Dictionary<int, bool> _autoFitRows = new Dictionary<int, bool>();
		private readonly HashSet<BaseExpression> _expressions = new HashSet<BaseExpression>();
		[NonSerialized]
		private Dictionary<string, BaseExpression> _expressionsById = new Dictionary<string, BaseExpression>();
		private readonly List<TemplateSection> _sections = new List<TemplateSection>();
		[NonSerialized]
		private Dictionary<SectionLookupKey, TemplateSection> _sectionsByKey = new Dictionary<SectionLookupKey, TemplateSection>();
		private readonly List<BookMark> _bookMarks = new List<BookMark>();
		private readonly HashSet<int> _pageBreakRows = new HashSet<int>();

		private readonly List<BaseStyle> _stylesList = new List<BaseStyle>();
		[NonSerialized]
		private readonly HashSet<BaseStyle> _externalStyleObjects = new HashSet<BaseStyle>(ReferenceComparer.Instance);
		private readonly List<ExternalStyle> _externalStyles = new List<ExternalStyle>();

		public string TemplateDirectory { get; set; }

		private readonly Dictionary<string, byte[]> _resources = new Dictionary<string, byte[]>();

		private int _updateDepth;
		[NonSerialized]
		private HashSet<string> _pendingPropertyChanges;
		[NonSerialized]
		private UriDataLoader _uriDataLoader;
		private UriDataLoaderOptions _uriDataLoaderOptions = new UriDataLoaderOptions();

		public UriDataLoaderOptions UriDataLoaderOptions
		{
			get { return _uriDataLoaderOptions ?? (_uriDataLoaderOptions = new UriDataLoaderOptions()); }
			set
			{
				_uriDataLoaderOptions = value ?? new UriDataLoaderOptions();
				_uriDataLoader = null;
			}
		}

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
			BookmarkSettings = new BookmarkSettings();
			Properties = properties ?? new Properties();
		}

		public CellManagerUpdateScope BeginUpdateScope()
		{
			return new CellManagerUpdateScope(this);
		}

		public void BeginUpdate()
		{
			_updateDepth++;
		}

		public void EndUpdate()
		{
			if (_updateDepth <= 0)
				throw new InvalidOperationException("EndUpdate was called without a matching BeginUpdate.");

			_updateDepth--;
			if (_updateDepth == 0)
				FlushPendingPropertyChanges();
		}

		protected override void OnPropertyChanged(string name)
		{
			if (_updateDepth > 0)
			{
				if (_pendingPropertyChanges == null)
					_pendingPropertyChanges = new HashSet<string>();
				_pendingPropertyChanges.Add(name);
				return;
			}

			base.OnPropertyChanged(name);
		}

		private void FlushPendingPropertyChanges()
		{
			if (_pendingPropertyChanges == null || _pendingPropertyChanges.Count == 0)
				return;

			var pending = _pendingPropertyChanges.ToArray();
			_pendingPropertyChanges.Clear();
			foreach (var name in pending)
				base.OnPropertyChanged(name);
		}

		private void RegisterExpression(BaseExpression expression)
		{
			if (_expressionsById == null)
				_expressionsById = new Dictionary<string, BaseExpression>();

			_expressions.Add(expression);
			_expressionsById[expression.ID] = expression;
		}

		private void UnregisterExpression(BaseExpression expression)
		{
			_expressions.Remove(expression);
			if (_expressionsById != null)
			{
				BaseExpression existing;
				if (_expressionsById.TryGetValue(expression.ID, out existing) && ReferenceEquals(existing, expression))
					_expressionsById.Remove(expression.ID);
			}
		}

		private void RebuildExpressionIndex()
		{
			_expressionsById = new Dictionary<string, BaseExpression>();
			foreach (var expression in _expressions)
				_expressionsById[expression.ID] = expression;
		}

		private void EnsureSectionIndex()
		{
			if (_sectionsByKey == null)
			{
				_sectionsByKey = new Dictionary<SectionLookupKey, TemplateSection>();
				SectionIndex.Rebuild(_sectionsByKey, _sections);
			}
		}

		private void RebuildSectionIndex()
		{
			_sectionsByKey = new Dictionary<SectionLookupKey, TemplateSection>();
			SectionIndex.Rebuild(_sectionsByKey, _sections);
		}

		private UriDataLoader GetUriDataLoader()
		{
			if (_uriDataLoader == null)
				_uriDataLoader = new UriDataLoader(GetResource, UriDataLoaderOptions);
			return _uriDataLoader;
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

		/// <summary>
		/// Сбрасывает кэш изображений, ссылающихся на указанный URI.
		/// </summary>
		public int InvalidateImageCaches(string uri)
		{
			if (string.IsNullOrEmpty(uri))
				return 0;

			var count = 0;
			foreach (var image in EnumerateImages())
			{
				if (!string.Equals(image.ActualUri, uri, StringComparison.OrdinalIgnoreCase))
					continue;

				image.InvalidateCache();
				count++;
			}

			return count;
		}

		/// <summary>
		/// URI изображений, используемых в документе, и количество ссылок на каждый.
		/// </summary>
		public IReadOnlyList<KeyValuePair<string, int>> GetUriReferenceCounts()
		{
			var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var image in EnumerateImages())
			{
				var uri = image.ActualUri;
				if (string.IsNullOrEmpty(uri))
					continue;

				int count;
				counts.TryGetValue(uri, out count);
				counts[uri] = count + 1;
			}

			var result = new List<KeyValuePair<string, int>>(counts);
			result.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.CurrentCultureIgnoreCase));
			return result;
		}

		/// <summary>
		/// Заменяет ссылки на изображение старого URI новым.
		/// Возвращает количество изменённых изображений.
		/// </summary>
		public int ReplaceUriReferences(string oldUri, string newUri)
		{
			if (string.IsNullOrEmpty(oldUri) || string.IsNullOrEmpty(newUri)
				|| string.Equals(oldUri, newUri, StringComparison.OrdinalIgnoreCase))
				return 0;

			var replaced = 0;
			foreach (var image in EnumerateImages())
			{
				var changed = false;
				if (string.Equals(image.Uri, oldUri, StringComparison.OrdinalIgnoreCase))
				{
					image.Uri = newUri;
					changed = true;
				}

				foreach (var param in image.GetParams())
				{
					if (!string.Equals(param.Target, "Uri", StringComparison.Ordinal))
						continue;

					var value = param.Value as string;
					if (!string.Equals(value, oldUri, StringComparison.OrdinalIgnoreCase))
						continue;

					param.Value = newUri;
					changed = true;
				}

				if (!changed)
					continue;

				image.InvalidateCache();
				replaced++;
			}

			return replaced;
		}

		private IEnumerable<ImageContent> EnumerateImages()
		{
			for (var row = 0; row < RowsCount; row++)
			{
				for (var column = 0; column < ColumnsCount; column++)
				{
					if (!TryGetCell(row, column, out var cell) || cell == null)
						continue;
					if (cell.Location.Row != row || cell.Location.Column != column)
						continue;

					foreach (var content in cell.GetContents())
					{
						var image = content as ImageContent;
						if (image != null)
							yield return image;
					}
				}
			}

			var print = PrintSettings;
			if (print == null)
				yield break;

			foreach (var image in EnumeratePagerImages(print.Headers))
				yield return image;
			foreach (var image in EnumeratePagerImages(print.Footers))
				yield return image;
		}

		private static IEnumerable<ImageContent> EnumeratePagerImages(IList<Pager> pagers)
		{
			if (pagers == null)
				yield break;

			foreach (var pager in pagers)
			{
				foreach (var image in EnumerateImagesIn(pager.Left))
					yield return image;
				foreach (var image in EnumerateImagesIn(pager.Center))
					yield return image;
				foreach (var image in EnumerateImagesIn(pager.Right))
					yield return image;
			}
		}

		private static IEnumerable<ImageContent> EnumerateImagesIn(BaseContent content)
		{
			if (content == null)
				yield break;

			var image = content as ImageContent;
			if (image != null)
			{
				yield return image;
				yield break;
			}

			var collection = content as CollectionContent;
			if (collection == null)
				yield break;

			foreach (var item in collection.GetContents())
			{
				foreach (var nested in EnumerateImagesIn(item))
					yield return nested;
			}
		}

		public void AddExpression(BaseExpression expression)
		{
			RegisterExpression(expression);
			expression.PropertyChanged += OnPropertyChanged;
			OnPropertyChanged("Expressions");
		}

		public void ReplaceExpression(BaseExpression expression)
		{
			if (expression == null)
				return;

			var oldExpr = GetExpressionByID(expression.ID);
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
			UnregisterExpression(expression);
			expression.PropertyChanged -= OnPropertyChanged;
			OnPropertyChanged("Expressions");
		}

		public IEnumerable<BaseExpression> GetExpressions()
		{
			foreach (var expression in _expressions)
				yield return expression;
		}

		public BaseExpression GetExpressionByID(string id)
		{
			if (string.IsNullOrEmpty(id) || _expressionsById == null)
				return null;

			BaseExpression expression;
			_expressionsById.TryGetValue(id, out expression);
			return expression;
		}

		public void AddPageBreakRow(int row)
		{
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
			EnsureSectionIndex();
			var foundSection = GetSectionInternal(section.Name, section.ObjectName, true);
			if (foundSection != null)
			{
				_sections.Remove(foundSection);
				SectionIndex.Unregister(_sectionsByKey, foundSection);
			}

			_sections.Add(section);
			SectionIndex.Register(_sectionsByKey, section);
			section.PropertyChanged += OnPropertyChanged;
			OnPropertyChanged("Sections");
		}

		public void RemoveSection(TemplateSection section)
		{
			EnsureSectionIndex();
			_sections.Remove(section);
			SectionIndex.Unregister(_sectionsByKey, section);
			section.PropertyChanged -= OnPropertyChanged;
			OnPropertyChanged("Sections");
		}

		private TemplateSection GetSectionInternal(string name, string objectName, bool strict = false)
		{
			EnsureSectionIndex();
			return SectionIndex.Find(_sectionsByKey, name, objectName, strict);
		}

		public TemplateSection GetSection(string name, string objectName)
		{
			var foundSection = GetSectionInternal(name, objectName);
			if (foundSection == null)
				throw new ApplicationException("Section not found");

			return foundSection;
		}

		public IEnumerable<TemplateSection> GetSections()
		{
			return _sections.ToArray();
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
			// сформированные отчёты достраивают закладки последней секции лениво,
			// чтобы потребителю (просмотр, экспорт) не приходилось вызывать что-то ещё
			var completion = _bookMarksCompletion;
			if (completion != null)
				completion();

			foreach (var item in _bookMarks)
				yield return item;
		}

		internal IList<BookMarks.BookMark> BookMarksStorage
		{
			get { return _bookMarks; }
		}

		internal void SetBookMarksCompletion(Action completion)
		{
			_bookMarksCompletion = completion;
		}

		public byte[] GetDataByUri(string uri)
		{
			return GetUriDataLoader().Load(uri);
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

		public BookmarkSettings BookmarkSettings
		{
			get { return _bookmarkSettings; }
			set { SetField(ref _bookmarkSettings, value ?? new BookmarkSettings()); }
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
			var cell = ResolveCell(row, column);
			if (cell != null)
			{
				var originRow = cell.Location.Row;
				var originCol = cell.Location.Column;
				var oldSpan = cell.SpanInternal;
				cell.SpanInternal = new CellSpan { ColSpan = 0, RowSpan = 0 };
				_cells[originRow][originCol] = null;
				_originCells.Remove(cell);
				CellGridHelper.ClearSpanSlotReferences(_cells, cell.Location, oldSpan, cell);
				InvalidateSpanCellsIndex();

				if (row >= _actualRowsCount - 1 || column >= _actualColumnsCount - 1)
					RecalculateActualDimensions();
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
			_originCells.Add(cell);
			InvalidateSpanCellsIndex();

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
			return ResolveCell(row, column);
		}

		public bool TryGetCell(int row, int column, out Cell cell)
		{
			if (row < 0 || column < 0 || row >= RowsCount || column >= ColumnsCount)
			{
				cell = null;
				return false;
			}

			cell = ResolveCell(row, column);
			return cell != null;
		}

		internal Cell GetStoredCell(int row, int column)
		{
			if (row < 0 || column < 0 || row >= RowsCount || column >= ColumnsCount)
				return null;

			return _cells[row][column];
		}

		internal int OriginCellCount
		{
			get { return _originCells.Count; }
		}

		internal void SetStoredCellForTests(int row, int column, Cell cell)
		{
			if (row >= 0 && column >= 0 && row < RowsCount && column < ColumnsCount)
				_cells[row][column] = cell;
		}

		private Cell ResolveCell(int row, int column)
		{
			var stored = _cells[row][column];
			if (CellGridHelper.IsOrigin(stored, row, column))
				return stored;

			if (stored != null && CellGridHelper.Covers(stored, row, column))
				return stored;

			for (int i = 0; i < _originCells.Count; i++)
			{
				var origin = _originCells[i];
				if (CellGridHelper.Covers(origin, row, column))
					return origin;
			}

			return null;
		}

		/// <summary>
		/// Возвращает ячейку, занимающую слот: собственную ячейку слота или merged-ячейку,
		/// покрывающую этот слот.
		/// </summary>
		public bool TryGetOccupiedCell(int row, int column, out Cell cell)
		{
			cell = null;
			if (row < 0 || column < 0 || row >= RowsCount || column >= ColumnsCount)
				return false;

			var stored = _cells[row][column];
			if (stored != null)
			{
				cell = stored;
				return true;
			}

			List<Cell> candidates;
			if (!GetSpanCellsByRow().TryGetValue(row, out candidates))
				return false;

			for (var i = 0; i < candidates.Count; i++)
			{
				var candidate = candidates[i];
				if (CellGridHelper.Covers(candidate, row, column))
				{
					cell = candidate;
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Занят ли слот ячейкой (собственной или покрывающей merged-ячейкой).
		/// </summary>
		public bool IsSlotOccupied(int row, int column)
		{
			Cell cell;
			return TryGetOccupiedCell(row, column, out cell);
		}

		/// <summary>
		/// Индекс merged-ячеек по строкам: нужен, чтобы не сканировать все origin-ячейки
		/// на каждый пустой слот при поиске границ переполнения.
		/// </summary>
		private Dictionary<int, List<Cell>> GetSpanCellsByRow()
		{
			if (_spanCellsByRow != null)
				return _spanCellsByRow;

			_spanCellsByRow = new Dictionary<int, List<Cell>>();
			for (var i = 0; i < _originCells.Count; i++)
			{
				var cell = _originCells[i];
				if (cell == null)
					continue;

				var span = cell.SpanInternal;
				if (span.RowSpan <= 0 && span.ColSpan <= 0)
					continue;

				for (var row = cell.Location.Row; row <= cell.Location.Row + span.RowSpan; row++)
				{
					List<Cell> cells;
					if (!_spanCellsByRow.TryGetValue(row, out cells))
					{
						cells = new List<Cell>();
						_spanCellsByRow[row] = cells;
					}
					cells.Add(cell);
				}
			}

			return _spanCellsByRow;
		}

		private void InvalidateSpanCellsIndex()
		{
			_spanCellsByRow = null;
		}

		public void SetSpansForCell(CellLocation location, CellSpan span)
		{
			CheckRowAndColumn(location.Row + span.RowSpan, location.Column + span.ColSpan);

			var cell = ResolveCell(location.Row, location.Column) ?? CreateCell(location.Row, location.Column);
			var oldSpan = cell.SpanInternal;
			CellGridHelper.ClearSpanSlotReferences(_cells, cell.Location, oldSpan, cell);
			cell.SpanInternal = span;
			InvalidateSpanCellsIndex();

			_actualRowsCount = System.Math.Max(_actualRowsCount, location.Row + span.RowSpan + 1);
			_actualColumnsCount = System.Math.Max(_actualColumnsCount, location.Column + span.ColSpan + 1);
			OnPropertyChanged("Cells");
		}

		private void RecalculateActualDimensions()
		{
			CellGridHelper.RecalculateActualDimensions(_originCells, out _actualRowsCount, out _actualColumnsCount);
		}

		internal void NormalizeSparseGrid()
		{
			_originCells.Clear();
			for (int row = 0; row < RowsCount; row++)
			{
				for (int col = 0; col < _cells[row].Length; col++)
				{
					var cell = _cells[row][col];
					if (cell == null)
						continue;

					if (CellGridHelper.IsOrigin(cell, row, col))
					{
						if (!_originCells.Contains(cell))
							_originCells.Add(cell);
						continue;
					}

					_cells[row][col] = null;
				}
			}

			RecalculateActualDimensions();
			InvalidateSpanCellsIndex();
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

		public string GetColumnMarkerForShow(int index)
		{
			return _columns[index].MarkerForShow;
		}

		public void SetColumnMarkerForShow(int index, string markerForShow)
		{
			_columns[index].MarkerForShow = markerForShow;
			OnPropertyChanged("ColumnMarkerForShow");
		}

		public string GetColumnMarkerForHide(int index)
		{
			return _columns[index].MarkerForHide;
		}

		public void SetColumnMarkerForHide(int index, string markerForHide)
		{
			_columns[index].MarkerForHide = markerForHide;
			OnPropertyChanged("ColumnMarkerForHide");
		}

		public float GetColumnWidth(int index)
		{
			var col = _columns[index];
			if (!string.IsNullOrEmpty(col.MarkerForShow) && !IsDesignMode)
				return MarkerResolver.Resolve(col.MarkerForShow) ? col.Width : 0;
			if (!string.IsNullOrEmpty(col.MarkerForHide) && !IsDesignMode)
				return MarkerResolver.Resolve(col.MarkerForHide) ? 0 : col.Width;

			return col.Width;
		}

		public void SetColumnWidth(int index, float width)
		{
			ResizeIfNeed(0, index);
			if (_columns[index].Width != width)
			{
				if (!string.IsNullOrEmpty(_columns[index].MarkerForShow) && !IsDesignMode && !MarkerResolver.Resolve(_columns[index].MarkerForShow))
					return;
				if (!string.IsNullOrEmpty(_columns[index].MarkerForHide) && !IsDesignMode && !MarkerResolver.Resolve(_columns[index].MarkerForHide))
					return;

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
			var data = ExternalStyleSearch.TryLoad(style.Uri, TemplateDirectory);
			if (data == null)
			{
				// Хостовый резолвер (например, Kayala отдаёт DefaultStyles.xml из рабочего каталога Config).
				// UriDataLoader по умолчанию запрещает file://-URI, поэтому хук вызываем напрямую.
				var fileName = ExternalStyleSearch.BareFileName(style.Uri);
				if (fileName != null)
					data = FileResolver.Resolve(fileName);
			}
			if (data == null)
				data = this.GetDataByUri(style.Uri);
			if (data != null)
			{
				var known = new HashSet<BaseStyle>(_stylesList, ReferenceComparer.Instance);
				var template = new CellTemplate(this);
				var xDoc = System.Xml.Linq.XDocument.Load(new System.IO.StreamReader(new System.IO.MemoryStream(data)));
				var element = xDoc.Element("CellReportTemplate");
				template.ParseStyles(element, this);

				// запоминаем, какие стили пришли из внешнего файла — их нельзя
				// редактировать в шаблоне и их можно вычистить «сжатием»
				foreach (var parsed in _stylesList)
				{
					if (!known.Contains(parsed))
						_externalStyleObjects.Add(parsed);
				}
			}
			OnPropertyChanged("ExternalStyles");
		}

		internal IEnumerable<ExternalStyle> GetExtStyles()
		{
			foreach (var style in _externalStyles)
				yield return style;
		}

		/// <summary>
		/// Стиль загружен из внешнего файла стилей.
		/// </summary>
		public bool IsExternalStyle(BaseStyle style)
		{
			return style != null && _externalStyleObjects.Contains(style);
		}

		/// <summary>
		/// Стили, хранящиеся в самом шаблоне (доступны для изменения).
		/// </summary>
		public IEnumerable<BaseStyle> GetTemplateStyles()
		{
			foreach (var style in _stylesList)
			{
				if (!_externalStyleObjects.Contains(style))
					yield return style;
			}
		}

		/// <summary>
		/// Стили, загруженные из внешних файлов (без дублей по идентификатору и типу).
		/// </summary>
		public IEnumerable<BaseStyle> GetExternalStyles()
		{
			var seen = new HashSet<string>(StringComparer.Ordinal);
			foreach (var style in _stylesList)
			{
				if (!_externalStyleObjects.Contains(style))
					continue;
				if (!seen.Add(style.GetType().Name + "|" + (style.ID ?? string.Empty)))
					continue;

				yield return style;
			}
		}

		/// <summary>
		/// Описания внешних файлов стилей.
		/// </summary>
		public IEnumerable<ExternalStyle> GetExternalStyleSources()
		{
			foreach (var style in _externalStyles)
				yield return style;
		}

		/// <summary>
		/// Перенаправляет ссылки на стиль старого идентификатора к новому.
		/// Возвращает количество изменённых ссылок.
		/// </summary>
		public int ReplaceStyleReferences(Type styleType, string oldId, string newId)
		{
			if (styleType == null || string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId) || oldId == newId)
				return 0;

			var isCellStyle = styleType == typeof(CellStyle);
			var count = 0;

			for (var row = 0; row < RowsCount; row++)
			{
				for (var column = 0; column < ColumnsCount; column++)
				{
					if (!TryGetCell(row, column, out var cell) || cell == null)
						continue;
					if (cell.Location.Row != row || cell.Location.Column != column)
						continue;

					if (isCellStyle && string.Equals(cell.StyleID, oldId, StringComparison.Ordinal))
					{
						cell.StyleID = newId;
						count++;
					}

					count += ReplaceContentStyle(cell.Content, styleType, oldId, newId);
				}
			}

			if (PrintSettings != null)
			{
				count += ReplacePagerStyle(PrintSettings.Headers, styleType, oldId, newId);
				count += ReplacePagerStyle(PrintSettings.Footers, styleType, oldId, newId);
			}

			return count;
		}

		private static int ReplaceContentStyle(BaseContent content, Type styleType, string oldId, string newId)
		{
			if (content == null)
				return 0;

			if (content is CollectionContent collection)
			{
				var nested = 0;
				foreach (var item in collection.GetContents())
					nested += ReplaceContentStyle(item, styleType, oldId, newId);
				return nested;
			}

			if (!string.Equals(content.StyleID, oldId, StringComparison.Ordinal))
				return 0;

			var style = content.GetStyle();
			if (style == null || style.GetType() != styleType)
				return 0;

			content.StyleID = newId;
			return 1;
		}

		private static int ReplacePagerStyle(IList<Pager> pagers, Type styleType, string oldId, string newId)
		{
			if (pagers == null)
				return 0;

			var count = 0;
			foreach (var pager in pagers)
			{
				count += ReplaceContentStyle(pager.Left, styleType, oldId, newId);
				count += ReplaceContentStyle(pager.Center, styleType, oldId, newId);
				count += ReplaceContentStyle(pager.Right, styleType, oldId, newId);
			}

			return count;
		}

		/// <summary>
		/// Удаляет стили шаблона, которые уже есть во внешних файлах. Если идентификаторы
		/// различаются, ссылки в ячейках перенаправляются на внешний стиль.
		/// </summary>
		public StyleCompressionResult CompressStyles()
		{
			var result = new StyleCompressionResult();
			var external = new List<BaseStyle>(GetExternalStyles());
			if (external.Count == 0)
				return result;

			foreach (var style in new List<BaseStyle>(GetTemplateStyles()))
			{
				var source = external.FirstOrDefault(item =>
						item.GetType() == style.GetType()
						&& string.Equals(item.ID, style.ID, StringComparison.Ordinal))
					?? external.FirstOrDefault(item => item.GetType() == style.GetType() && item.Equals(style));

				if (source == null)
					continue;

				if (!string.Equals(style.ID, source.ID, StringComparison.Ordinal))
				{
					result.References += ReplaceStyleReferences(style.GetType(), style.ID ?? string.Empty, source.ID ?? string.Empty);
					result.Remapped++;
				}

				RemoveStyle(style);
				result.Removed++;
			}

			return result;
		}

		public void RemoveUnusedStyles()
		{
			var unusedStyles = new List<BaseStyle>();
			foreach (var style in _stylesList)
			{
				if (!IsStyleUsed(style))
					unusedStyles.Add(style);
			}

			foreach (var style in unusedStyles)
				RemoveStyle(style);
		}

		public bool IsStyleUsed(BaseStyle style)
		{
			for (int i = 0; i < _originCells.Count; i++)
			{
				if (HasStyle(_originCells[i], style))
					return true;
			}

			return false;
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
			RebuildExpressionIndex();
			RebuildSectionIndex();
			foreach (Cell[] row in _cells)
			{
				foreach (Cell cell in row)
				{
					if (cell != null)
						cell.SetManager(this);
				}
			}
			NormalizeSparseGrid();
		}

		public void OnDeserializedMethod()
		{
			OnDeserializedMethod(new StreamingContext());
		}

		/// <summary>
		/// Сравнение стилей по ссылке: стили разных типов могут быть равны по значению.
		/// </summary>
		private sealed class ReferenceComparer : IEqualityComparer<BaseStyle>
		{
			public static readonly ReferenceComparer Instance = new ReferenceComparer();

			public bool Equals(BaseStyle x, BaseStyle y)
			{
				return ReferenceEquals(x, y);
			}

			public int GetHashCode(BaseStyle obj)
			{
				return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
			}
		}
	}
}
