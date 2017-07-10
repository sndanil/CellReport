using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Xml;

using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Export.Excel;
using CellReport.Presenters.Wpf;
using CellReport.Export;
using CellReport.Export.Html;

namespace CellReport.Studio.ViewModel
{
	public interface IViewUpdater
	{
		void Update(bool isNew = true);
	}

	public class StudioCommands
	{
		public static Panel Root = null;
		public static IViewUpdater Updater = null;
		private static DisplayPresenter _presenter = null;
		private static CurrentValues _values = null;
		private static string _fileName = null;
		private static Dictionary<string, DelegateCommand> _commands = new Dictionary<string, DelegateCommand>();

		public StudioCommands()
		{

		}

		private static bool IsDefaultEnabled()
		{
			return _presenter != null;
		}

		public static DisplayPresenter Presenter
		{
			get { return _presenter; }
		}

		public static CellManager Manager
		{
			get { return _presenter.Manager; }
		}

		public static DisplaySettings DisplaySettings
		{
			get { return _presenter == null ? new DisplaySettings() : _presenter.Manager.DisplaySettings; }
		}

		public static PrintSettings PrintSettings
		{
			get { return _presenter == null ? new PrintSettings() : _presenter.Manager.PrintSettings; }
		}

		public static ExportSettings ExportSettings
		{
			get { return _presenter == null ? new ExportSettings() : _presenter.Manager.ExportSettings; }
		}

		public static CellReport.Core.Properties Properties
		{
			get { return _presenter == null ? new CellReport.Core.Properties() : _presenter.Manager.Properties; }
		}

		public static CurrentValues Values
		{
			get { return _values ?? new CurrentValues(); }
			set { _values = value; }
		}

		public static DelegateCommand Open
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("open", out command))
				{
					Action action = () =>
							{
								var dialog = new Microsoft.Win32.OpenFileDialog();
								dialog.Filter = "Xml files (*.xml)|*.xml|CellReport templates (*.tcr)|*.tcr|CellReport templates package (*.tcrp)|*.tcrp|All files (*.*)|*.*";
								dialog.FilterIndex = 3;
								var result = dialog.ShowDialog();
								if (result == true)
								{
									Root.Children.Clear();
									CellTemplate template = new CellTemplate();
									template.Load(dialog.FileName);

									_presenter = new DisplayPresenter(template.Manager, Root);
									_fileName = dialog.FileName;
									_values = new CurrentValues(_presenter);
									Updater.Update();
									_presenter.Update();
								}
							};
					command = new DelegateCommand(action);
					_commands.Add("open", command);
				}

				return command;
			}
		}

		public static DelegateCommand Create
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("create", out command))
				{
					Action action = () =>
					{
						var manager = new CellManager();
						manager.AddExternalStyle(new CellReport.Core.ExternalStyle("file://DefaultStyles.xml"));
						Root.Children.Clear();

						_presenter = new DisplayPresenter(manager, Root);
						_fileName = null;
						_values = new CurrentValues(_presenter);
						Updater.Update();
					};
					command = new DelegateCommand(action);
					_commands.Add("create", command);
				}

				return command;
			}
		}

		public static DelegateCommand ImportFromExcel
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("ImportFromExcel", out command))
				{
					Action action = () =>
					{
						var dialog = new Microsoft.Win32.OpenFileDialog();
						dialog.Filter = "Таблица XML 2003 (*.xml)|*.xml|All files (*.*)|*.*";
						dialog.FilterIndex = 1;
						var result = dialog.ShowDialog();
						if (result == true)
						{
							var defCol = new Column() { Width = 17 };
							var properties = new CellReport.Core.Properties() { DefaultRowHeight = 5 };
							var doc = new XmlDocument();
							var namespaceManager = new XmlNamespaceManager(doc.NameTable);
							try
							{
								doc.Load(dialog.FileName);
							}
							catch
							{
								System.Windows.MessageBox.Show("Невозможно загрузить файл.");
								return;
							}
							namespaceManager.AddNamespace("ss", "urn:schemas-microsoft-com:office:spreadsheet");
							var tableNode = doc.SelectSingleNode("//ss:Table", namespaceManager);
							if (tableNode.Attributes["ss:ExpandedColumnCount"] != null)
								properties.DefaultColumnsCount = Convert.ToInt32(tableNode.Attributes["ss:ExpandedColumnCount"].Value);
							if (tableNode.Attributes["ss:ExpandedRowCount"] != null)
								properties.DefaultRowsCount = Convert.ToInt32(tableNode.Attributes["ss:ExpandedRowCount"].Value);
							if (tableNode.Attributes["ss:DefaultColumnWidth"] != null)
								defCol.Width = UnitsConverter.ConvertValue(Convert.ToSingle(tableNode.Attributes["ss:DefaultColumnWidth"].Value, CultureInfo.InvariantCulture), GraphicsUnit.Point, GraphicsUnit.Millimeter, 0);
							if (tableNode.Attributes["ss:DefaultRowHeight"] != null)
								properties.DefaultRowHeight = UnitsConverter.ConvertValue(Convert.ToSingle(tableNode.Attributes["ss:DefaultRowHeight"].Value, CultureInfo.InvariantCulture), GraphicsUnit.Point, GraphicsUnit.Millimeter, 0);
							properties.DefaultColumn = defCol;

							var manager = new CellManager(properties);
							manager.AddExternalStyle(new CellReport.Core.ExternalStyle("file://DefaultStyles.xml"));

							var index = 0;
							foreach (XmlElement columnNode in tableNode.SelectNodes("ss:Column", namespaceManager))
							{
								var width = properties.DefaultColumn.Width;
								var span = 1;
								if (columnNode.Attributes["ss:Index"] != null)
									index = int.Parse(columnNode.Attributes["ss:Index"].Value) - 1;
								if (columnNode.Attributes["ss:Width"] != null)
									width = UnitsConverter.ConvertValue(Convert.ToSingle(columnNode.Attributes["ss:Width"].Value, CultureInfo.InvariantCulture), GraphicsUnit.Point, GraphicsUnit.Millimeter, 0);
								if (columnNode.Attributes["ss:Span"] != null)
									span = Convert.ToInt32(columnNode.Attributes["ss:Span"].Value) + 1;

								for (int i = 0; i < span; i++)
									manager.SetColumnWidth(i + index, width);

								index += span;
							}
							index = 0;
							foreach (XmlElement rowNode in tableNode.SelectNodes("ss:Row", namespaceManager))
							{
								var height = properties.DefaultRowHeight;
								var span = 1;
								var autoHeight = false;
								if (rowNode.Attributes["ss:Index"] != null)
									index = int.Parse(rowNode.Attributes["ss:Index"].Value) - 1;
								if (rowNode.Attributes["ss:Height"] != null)
									height = UnitsConverter.ConvertValue(Convert.ToSingle(rowNode.Attributes["ss:Height"].Value, CultureInfo.InvariantCulture), GraphicsUnit.Point, GraphicsUnit.Millimeter, 0);
								if (rowNode.Attributes["ss:Span"] != null)
									span = Convert.ToInt32(rowNode.Attributes["ss:Span"].Value) + 1;
								if (rowNode.Attributes["ss:AutoFitHeight"] != null)
									autoHeight = rowNode.Attributes["ss:AutoFitHeight"].Value == "1";

								for (int i = 0; i < span; i++)
								{
									manager.SetRowHeight(i + index, height);
									if (autoHeight)
										manager.SetAutoFitRow(i + index, true);
								}

								var cellIndex = 0;
								foreach (XmlElement cellNode in rowNode.SelectNodes("ss:Cell", namespaceManager))
								{
									var cellSpan = new CellSpan();
									if (cellNode.Attributes["ss:Index"] != null)
										cellIndex = int.Parse(cellNode.Attributes["ss:Index"].Value) - 1;
									if (cellNode.Attributes["ss:MergeAcross"] != null)
										cellSpan.ColSpan = int.Parse(cellNode.Attributes["ss:MergeAcross"].Value);
									if (cellNode.Attributes["ss:MergeDown"] != null)
										cellSpan.RowSpan = int.Parse(cellNode.Attributes["ss:MergeDown"].Value);

									var cell = manager.CreateCell(index, cellIndex);
									cell.Span = cellSpan;

									if (cellNode.Attributes["ss:StyleID"] != null)
									{
										string styleid = cellNode.Attributes["ss:StyleID"].Value;
										var styleNode = doc.SelectSingleNode(string.Format("//ss:Style[@ss:ID=\"{0}\"]", styleid), namespaceManager);
										Borders borders = new Borders(CellReport.Core.Border.Empty);
										if (styleNode["Borders"] != null)
										{
											var bordersArray = new CellReport.Core.Border[5]
																{
																								CellReport.Core.Border.Empty,
																								CellReport.Core.Border.Empty,
																								CellReport.Core.Border.Empty,
																								CellReport.Core.Border.Empty,
																								CellReport.Core.Border.Empty
																	};
											foreach (XmlElement borderNode in styleNode["Borders"].ChildNodes)
											{
												var position = borderNode.Attributes["ss:Position"].Value;
												var lineStyle = LineStyle.Solid;
												if (borderNode.Attributes["ss:LineStyle"] != null)
												{
													switch (borderNode.Attributes["ss:LineStyle"].Value)
													{
														case "Dash":
															lineStyle = LineStyle.Dash;
															break;
													}
												}
												var weight = UnitsConverter.ConvertValue(Convert.ToSingle(borderNode.Attributes["ss:Weight"].Value, CultureInfo.InvariantCulture),
																			GraphicsUnit.Point, GraphicsUnit.Millimeter, 0);
												switch (position)
												{
													case "All":
														bordersArray[0] = new CellReport.Core.Border(weight, "Black", lineStyle);
														break;
													case "Top":
														bordersArray[1] = new CellReport.Core.Border(weight, "Black", lineStyle);
														break;
													case "Left":
														bordersArray[2] = new CellReport.Core.Border(weight, "Black", lineStyle);
														break;
													case "Right":
														bordersArray[3] = new CellReport.Core.Border(weight, "Black", lineStyle);
														break;
													case "Bottom":
														bordersArray[4] = new CellReport.Core.Border(weight, "Black", lineStyle);
														break;
												}
											}
											if (bordersArray[0] == CellReport.Core.Border.Empty)
												borders = new Borders(bordersArray[1], bordersArray[2], bordersArray[3], bordersArray[4]);
											else
												borders = new Borders(bordersArray[0]);
										}
										var cellStyle = new CellStyle("Transparent", borders, new Paddings(0));
										var findStyle = manager.GetStyles().FirstOrDefault(s => s.Equals(cellStyle));
										if (findStyle == null)
										{
											cellStyle.ID = styleid;
											manager.AddStyle(cellStyle);
											cell.StyleID = styleid;
										}
										else
											cell.StyleID = findStyle.ID;
									}

									foreach (XmlElement dataNode in cellNode.SelectNodes("ss:Data", namespaceManager))
									{
										string styleid = null;
										var direction = Direction.LeftToRight;
										var halignment = HorizontalAlignment.Left;
										var valignment = VerticalAlignment.Top;
										var font = new FontInfo("Arial", 11);
										if (cellNode.Attributes["ss:StyleID"] != null)
										{
											styleid = cellNode.Attributes["ss:StyleID"].Value;
											var styleNode = doc.SelectSingleNode(string.Format("//ss:Style[@ss:ID=\"{0}\"]", styleid), namespaceManager);
											if (styleNode["Alignment"] != null)
											{
												if (styleNode["Alignment"].Attributes["ss:Horizontal"] != null)
													halignment = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), styleNode["Alignment"].Attributes["ss:Horizontal"].Value);
												if (styleNode["Alignment"].Attributes["ss:Vertical"] != null)
													valignment = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), styleNode["Alignment"].Attributes["ss:Vertical"].Value);
												if (styleNode["Alignment"].Attributes["ss:VerticalText"] != null &&
																			styleNode["Alignment"].Attributes["ss:VerticalText"].Value == "1")
													direction = Direction.TopToDown;
											}
											if (styleNode["Font"] != null)
											{
												var family = "Arial";
												var size = 11f;
												var style = FontStyle.Regular;
												if (styleNode["Font"].Attributes["ss:FontName"] != null)
													family = styleNode["Font"].Attributes["ss:FontName"].Value;
												if (styleNode["Font"].Attributes["ss:Size"] != null)
													size = Convert.ToSingle(styleNode["Font"].Attributes["ss:Size"].Value, CultureInfo.InvariantCulture) - 0.5f;
												if (styleNode["Font"].Attributes["ss:Bold"] != null
																			&& styleNode["Font"].Attributes["ss:Bold"].Value == "1")
													style |= FontStyle.Bold;
												if (styleNode["Font"].Attributes["ss:Italic"] != null
																			&& styleNode["Font"].Attributes["ss:Italic"].Value == "1")
													style |= FontStyle.Italic;
												font = new FontInfo(family, size, style);
											}
										}
										BaseContent cont = null;
										BaseStyle contstyle = null;
										switch (dataNode.Attributes["ss:Type"].Value)
										{
											case "String":
												cont = new TextContent(manager) { Text = dataNode.InnerText };
												contstyle = new TextContentStyle(halignment, valignment, font, "Black", 0, direction);
												break;
											case "Number":
												cont = new DecimalContent(manager) { Value = Convert.ToDecimal(dataNode.InnerText, CultureInfo.InvariantCulture) };
												contstyle = new DecimalContentStyle(halignment, valignment, font, "Black", "Red", 0, "#,##0.00;-#,##0.00;-", "ru-RU", direction);
												break;
										}
										if (cont != null)
										{
											cell.Content = cont;
											if (!string.IsNullOrEmpty(styleid))
												SetContentStyle(manager, styleid, cont, contstyle);
										}
									}

									cellIndex += cellSpan.ColSpan + 1;
								}

								index += span;
							}
							Root.Children.Clear();
							_presenter = new DisplayPresenter(manager, Root);
							_fileName = null;
							_values = new CurrentValues(_presenter);
							Updater.Update();
						}
					};
					command = new DelegateCommand(action);
					_commands.Add("ImportFromExcel", command);
				}

				return command;
			}
		}

		private static bool SetContentStyle(CellManager manager, string styleid, BaseContent cont, BaseStyle style)
		{
			var findStyle = manager.GetStyles().FirstOrDefault(s => s.Equals(style));
			if (findStyle == null)
			{
				style.ID = styleid;
				manager.AddStyle(style);
				cont.StyleID = style.ID;
				return true;
			}
			else
				cont.StyleID = findStyle.ID;

			return false;
		}

		public static DelegateCommand ImportFromForint
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("ImportFromForint", out command))
				{
					Action action = () =>
					{
						var dialog = new Microsoft.Win32.OpenFileDialog();
						dialog.Filter = "CellReporter files (*.fr)|*.fr|All files (*.*)|*.*";
						dialog.FilterIndex = 1;
						var result = dialog.ShowDialog();
						if (result == true)
						{
							var dataSet = new DataSet();
							dataSet.ReadXml(dialog.FileName);
							var rowsCount = dataSet.Tables["Rows"].Rows.Count;
							var colsCount = dataSet.Tables["Cols"].Rows.Count;

							var manager = new CellManager(rowsCount, colsCount);
							manager.AddExternalStyle(new CellReport.Core.ExternalStyle("file://DefaultStyles.xml"));
							int index = 0;
							foreach (DataRow row in dataSet.Tables["Rows"].Rows)
							{
								manager.SetRowHeight(index, UnitsConverter.ConvertValue(Convert.ToInt32(row["Height"]), GraphicsUnit.Pixel, GraphicsUnit.Millimeter, 96));
								index++;
							}
							index = 0;
							foreach (DataRow row in dataSet.Tables["Cols"].Rows)
							{
								manager.SetColumnWidth(index, UnitsConverter.ConvertValue(Convert.ToInt32(row["Width"]), GraphicsUnit.Pixel, GraphicsUnit.Millimeter, 96));
								index++;
							}
							foreach (DataRow row in dataSet.Tables["Sections"].Rows)
							{
								var section = new TemplateSection();
								section.Name = row["Name"].ToString();
								section.Left = Convert.ToInt32(row["StartCol"]);
								section.Top = Convert.ToInt32(row["StartRow"]);
								section.Right = Convert.ToInt32(row["EndCol"]);
								section.Bottom = Convert.ToInt32(row["EndRow"]);
								manager.AddSection(section);
							}
							var cellStyleCounter = 1;
							var textStyleCounter = 1;
							var decimalStyleCounter = 1;
							foreach (DataRow row in dataSet.Tables["Cells"].Rows)
							{
								var rowIndex = Convert.ToInt32(row["StartRow"]);
								var colIndex = Convert.ToInt32(row["StartCol"]);
								var cellRow = dataSet.Tables["Cell"].Select("Cells_Id='" + row["Cells_Id"] + "'")[0];
								var colSpan = Convert.ToInt32(cellRow["MergeAcross"]);
								var rowSpan = Convert.ToInt32(cellRow["MergeDown"]);
								var cell = manager.CreateCell(rowIndex, colIndex);
								cell.Span = new CellSpan() { RowSpan = rowSpan, ColSpan = colSpan };

								var cellAlignment = Convert.ToInt32(cellRow["AlignmentAsInt"]);
								var halignment = HorizontalAlignment.Center;
								var valignment = VerticalAlignment.Center;
								switch (cellAlignment)
								{
									case 1:
										valignment = VerticalAlignment.Bottom;
										break;
									case 3:
										valignment = VerticalAlignment.Top;
										break;
									case 4:
										halignment = HorizontalAlignment.Left;
										valignment = VerticalAlignment.Bottom;
										break;
									case 5:
										halignment = HorizontalAlignment.Left;
										break;
									case 6:
										halignment = HorizontalAlignment.Left;
										valignment = VerticalAlignment.Top;
										break;
									case 7:
										halignment = HorizontalAlignment.Right;
										valignment = VerticalAlignment.Bottom;
										break;
									case 8:
										halignment = HorizontalAlignment.Right;
										valignment = VerticalAlignment.Center;
										break;
									case 9:
										halignment = HorizontalAlignment.Right;
										valignment = VerticalAlignment.Top;
										break;
								}
								var foreColor = NormalizeColor(BitConverter.GetBytes(Convert.ToInt32(cellRow["ForeColorAsInt"])));
								var backColor = NormalizeColor(BitConverter.GetBytes(Convert.ToInt32(cellRow["BackColorAsInt"])));
								var lineColor = NormalizeColor(BitConverter.GetBytes(Convert.ToInt32(cellRow["LineColorAsInt"])));
								var fontString = cellRow["FontAsString"].ToString();
								var fontStyle = FontStyle.Regular;
								if (fontString.Contains("Bold"))
									fontStyle |= FontStyle.Bold;
								if (fontString.Contains("Italic"))
									fontStyle |= FontStyle.Italic;
								var fontFamily = fontString.Split(';')[0];
								var fontSize = Convert.ToSingle(fontString.Split(';')[1].Replace("pt", "").Trim());
								var topLineWidth = ConvertFromForintPixels(cellRow["TopLine"]);
								var bottomLineWidth = ConvertFromForintPixels(cellRow["BottomLine"]);
								var leftLineWidth = ConvertFromForintPixels(cellRow["LeftLine"]);
								var rightLineWidth = ConvertFromForintPixels(cellRow["RightLine"]);

								var cellStyle = new CellStyle(backColor,
															new Borders(new Core.Border(topLineWidth, topLineWidth == 0 ? "Transparent" : lineColor, topLineWidth == 0 ? LineStyle.None : LineStyle.Solid),
																					new Core.Border(leftLineWidth, leftLineWidth == 0 ? "Transparent" : lineColor, leftLineWidth == 0 ? LineStyle.None : LineStyle.Solid),
																					new Core.Border(rightLineWidth, rightLineWidth == 0 ? "Transparent" : lineColor, rightLineWidth == 0 ? LineStyle.None : LineStyle.Solid),
																					new Core.Border(bottomLineWidth, bottomLineWidth == 0 ? "Transparent" : lineColor, bottomLineWidth == 0 ? LineStyle.None : LineStyle.Solid)),
																					new Paddings(0));
								var findStyle = manager.GetStyles().Where(s => s.Equals(cellStyle)).FirstOrDefault();
								if (findStyle != null)
									cell.Style = (CellStyle)findStyle;
								else
								{
									cellStyle.ID = "Стиль " + cellStyleCounter;
									manager.AddStyle(cellStyle);
									cellStyleCounter++;
									cell.Style = (CellStyle)cellStyle;
								}
								switch (cellRow["Format"].ToString())
								{
									case "String":
										if (cellRow["Text"].ToString() != string.Empty)
										{
											var cont = new TextContent(manager) { Text = cellRow["Text"].ToString() };
											cont.AddParam(new Param(manager, cont) { Name = cellRow["Text"].ToString(), Target = "Text" });
											cell.Content = cont;
											var textStyle = new TextContentStyle(halignment, valignment,
																		new FontInfo(fontFamily, fontSize, fontStyle), foreColor);
											if (SetContentStyle(manager, "Стиль " + textStyleCounter, cont, textStyle))
												textStyleCounter++;
										}
										break;
									case "Number":
										if (cellRow["Text"].ToString() != string.Empty)
										{
											var cont = new DecimalContent(manager);
											cont.AddParam(new Param(manager, cont) { Name = cellRow["Text"].ToString(), Target = "Value" });
											cell.Content = cont;
											var numberScale = Convert.ToInt32(cellRow["NumberScale"]);
											var format = "#,##0.00;-#,##0.00;-";
											switch (numberScale)
											{
												case 0:
													format = "#,##0;-#,##0;-";
													break;
												case 3:
													format = "#,##0.000;-#,##0.000;-";
													break;
											}
											var decimalStyle = new DecimalContentStyle(halignment, valignment,
																		new FontInfo(fontFamily, fontSize, fontStyle), foreColor, "Red", 0, format, "ru-RU", Direction.LeftToRight);
											if (SetContentStyle(manager, "Стиль " + decimalStyleCounter, cont, decimalStyle))
												decimalStyleCounter++;
										}
										break;
								}
							}
							Root.Children.Clear();

							_presenter = new DisplayPresenter(manager, Root);
							_fileName = null;
							_values = new CurrentValues(_presenter);
							Updater.Update();
						}
					};
					command = new DelegateCommand(action);
					_commands.Add("ImportFromForint", command);
				}

				return command;
			}
		}

		private static float ConvertFromForintPixels(object forint)
		{
			return UnitsConverter.ConvertValue(Convert.ToSingle(Math.Round(Convert.ToSingle(forint, CultureInfo.InvariantCulture), 0, MidpointRounding.AwayFromZero))
					, GraphicsUnit.Pixel, GraphicsUnit.Millimeter, 96);
		}
		private static string NormalizeColor(byte[] rgb)
		{
			if (rgb[0] == 255 && rgb[1] == 255 && rgb[2] == 255)
				return "Transparent";
			if (rgb[0] == 0 && rgb[1] == 0 && rgb[2] == 0)
				return "Black";

			return "#" + rgb[2].ToString("X2") + rgb[1].ToString("X2") + rgb[0].ToString("X2");
		}

		private static void SaveReport(bool saveAs)
		{
			var emptyName = string.IsNullOrEmpty(_fileName);
			if (emptyName || saveAs)
			{
				var dialog = new Microsoft.Win32.SaveFileDialog();
				dialog.Filter = "Xml files (*.xml)|*.xml|CellReport templates (*.tcr)|*.tcr|CellReport templates package (*.tcrp)|*.tcrp|All files (*.*)|*.*";
				dialog.FilterIndex = 3;
				if (!emptyName)
					dialog.FileName = System.IO.Path.GetFileName(_fileName);
				if (dialog.ShowDialog() == true)
					_fileName = dialog.FileName;
				else
					return;
			}
			CellTemplate template = new CellTemplate(_presenter.Manager);
			template.Save(_fileName, System.IO.Path.GetExtension(_fileName).ToUpper() == ".TCRP" ? TemplateType.Package : TemplateType.Xml);
		}

		public static DelegateCommand Save
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("save", out command))
				{
					Action action = () =>
					{
						SaveReport(false);
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("save", command);
				}

				return command;
			}
		}

		public static DelegateCommand SaveAs
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("SaveAs", out command))
				{
					Action action = () =>
					{
						SaveReport(true);
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("SaveAs", command);
				}

				return command;
			}
		}

		private static void Export(BaseExporter exp, bool openFile)
		{
			var dialog = new Microsoft.Win32.SaveFileDialog();
			dialog.Filter = string.Format("{0} файлы (*.{1})|*.{1}|Все файлы (*.*)|*.*", exp.Name, exp.Extension);
			dialog.FileName = _presenter.Manager.Properties.DocumentName + "." + exp.Extension;
			dialog.FilterIndex = 1;
			if (dialog.ShowDialog() == true)
			{
				try
				{
					using (var file = System.IO.File.Create(dialog.FileName))
						exp.ExportTo(file);
					if (openFile)
						System.Diagnostics.Process.Start(dialog.FileName);
				}
				catch (System.IO.IOException)
				{
					System.Windows.MessageBox.Show("Невозможно сохранить файл. Возможно, файл занят другим приложением.", "Ошибка",
						System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
				}
			}
		}

		public static DelegateCommand ExportExcel
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("ExportExcel", out command))
				{
					Action action = () =>
					{
						var exp = new ExcelExporter(_presenter.Manager, _presenter.Renderer);
						Export(exp, _presenter.Manager.ExportSettings.RunExcelAfterExport);
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("ExportExcel", command);
				}

				return command;
			}
		}

		public static DelegateCommand ExportHtml
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("ExportHtml", out command))
				{
					Action action = () =>
					{
						var exp = new HtmlExporter(_presenter.Manager, _presenter.Renderer);
						Export(exp, false);
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("ExportHtml", command);
				}

				return command;
			}
		}

		public static DelegateCommand Default
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("default", out command))
				{
					Action action = () =>
					{
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("default", command);
				}

				return command;
			}
		}

		public static DelegateCommand Inactive
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("Inactive", out command))
				{
					Action action = () =>
					{
					};
					Func<bool> inactive = () => false;
					command = new DelegateCommand(action, inactive);
					_commands.Add("Inactive", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddRows
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("addRows", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						_presenter.Manager.InsertRows(_presenter.CurrentCell.Row, 1);
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("addRows", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddRowsAfter
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("addRowsAfter", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						_presenter.Manager.InsertRows(_presenter.CurrentCell.Row + 1, 1);
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("addRowsAfter", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddColumns
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddColumns", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						_presenter.Manager.InsertColumns(_presenter.CurrentCell.Column, 1);
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddColumns", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddColumnsAfter
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddColumnsAfter", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						_presenter.Manager.InsertColumns(_presenter.CurrentCell.Column + 1, 1);
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddColumnsAfter", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteRows
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("DeleteRows", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						if (_presenter.HaveSelection())
						{
							var start = _presenter.GetStartSelection();
							var end = _presenter.GetEndSelection();
							_presenter.ClearSelection();
							_presenter.Manager.DeleteRows(start.Row, end.Row - start.Row + 1);
						}
						else
							_presenter.Manager.DeleteRows(_presenter.CurrentCell.Row, 1);
						if (_presenter.CurrentCell.Row >= _presenter.Manager.RowsCount)
							_presenter.CurrentCell = new CellLocation() { Row = _presenter.CurrentCell.Row - 1, Column = _presenter.CurrentCell.Column };
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("DeleteRows", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteColumns
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("DeleteColumns", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						if (_presenter.HaveSelection())
						{
							var start = _presenter.GetStartSelection();
							var end = _presenter.GetEndSelection();
							_presenter.ClearSelection();
							_presenter.Manager.DeleteColumns(start.Column, end.Column - start.Column + 1);
						}
						else
							_presenter.Manager.DeleteColumns(_presenter.CurrentCell.Column, 1);
						if (_presenter.CurrentCell.Column >= _presenter.Manager.ColumnsCount)
							_presenter.CurrentCell = new CellLocation() { Row = _presenter.CurrentCell.Row, Column = _presenter.CurrentCell.Column - 1 };
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("DeleteColumns", command);
				}

				return command;
			}
		}

		public static DelegateCommand MergeCells
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("MergeCells", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						if (_presenter.HaveSelection())
						{
							var start = _presenter.GetStartSelection();
							var end = _presenter.GetEndSelection();
							var cell = _presenter.Manager.GetCell(_presenter.CurrentCell.Row, _presenter.CurrentCell.Column);
							if (cell != null)
								cell = _presenter.Manager.CreateCell(start.Row, start.Column, cell);
							else
								cell = _presenter.Manager.CreateCell(start);
							cell.Span = new CellSpan() { RowSpan = end.Row - start.Row, ColSpan = end.Column - start.Column };
							_presenter.ClearSelection();
							_presenter.CurrentCell = start;
						}
						else if (_presenter.Manager.GetCell(_presenter.CurrentCell) != null)
							_presenter.Manager.GetCell(_presenter.CurrentCell).Span = new CellSpan();
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("MergeCells", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteCell
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("DeleteCell", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						if (_presenter.HaveSelection())
						{
							var start = _presenter.GetStartSelection();
							var end = _presenter.GetEndSelection();
							for (int row = start.Row; row <= end.Row; row++)
								for (int col = start.Column; col <= end.Column; col++)
									_presenter.Manager.DeleteCell(row, col);
						}
						else
							_presenter.Manager.DeleteCell(_presenter.CurrentCell);
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("DeleteCell", command);
				}

				return command;
			}
		}

		public static void AddContent(CellReport.Core.Contents.BaseContent content)
		{
			StateManager.BeginUpdate();
			var cell = _presenter.Manager.GetCell(_presenter.CurrentCell);

			if (cell == null)
				cell = _presenter.Manager.CreateCell(_presenter.CurrentCell);

			if (cell.Content is CellReport.Core.Contents.CollectionContent)
			{
				var collection = cell.Content as CellReport.Core.Contents.CollectionContent;
				collection.Add(content);
			}
			else if (cell.Content != null)
			{
				var collection = new CellReport.Core.Contents.CollectionContent(_presenter.Manager, 2);
				collection.Add(content);
				collection.Add(cell.Content);
				cell.Content = collection;
			}
			else
				cell.Content = content;
			StateManager.EndUpdate();
		}

		public static DelegateCommand AddTextContent
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddTextContent", out command))
				{
					Action action = () =>
					{
						_presenter.BeginUpdate();
						AddContent(new TextContent(_presenter.Manager));
						Values.Content = Values.CurrentContents.LastOrDefault();
						Values.CurrentContents = null;
						_presenter.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddTextContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddDecimalContent
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddDecimalContent", out command))
				{
					Action action = () =>
					{
						_presenter.BeginUpdate();
						AddContent(new DecimalContent(_presenter.Manager));
						Values.Content = Values.CurrentContents.LastOrDefault();
						Values.CurrentContents = null;
						_presenter.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddDecimalContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddDateTimeContent
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddDateTimeContent", out command))
				{
					Action action = () =>
					{
						_presenter.BeginUpdate();
						AddContent(new DateTimeContent(_presenter.Manager));
						Values.Content = Values.CurrentContents.LastOrDefault();
						Values.CurrentContents = null;
						_presenter.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddDateTimeContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddActionContent
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddActionContent", out command))
				{
					Action action = () =>
					{
						_presenter.BeginUpdate();
						AddContent(new ActionContent(_presenter.Manager) { Type = ActionType.MouseDblClick });
						Values.Content = Values.CurrentContents.LastOrDefault();
						Values.CurrentContents = null;
						_presenter.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddActionContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddBarCodeContent
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddBarCodeContent", out command))
				{
					Action action = () =>
					{
						_presenter.BeginUpdate();
						AddContent(new BarCodeContent(_presenter.Manager, BarCodeType.Auto));
						Values.Content = Values.CurrentContents.LastOrDefault();
						Values.CurrentContents = null;
						_presenter.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddBarCodeContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddImageContent
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddImageContent", out command))
				{
					Action action = () =>
					{
						_presenter.BeginUpdate();
						AddContent(new ImageContent(_presenter.Manager));
						Values.Content = Values.CurrentContents.LastOrDefault();
						Values.CurrentContents = null;
						_presenter.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddImageContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteContent
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("DeleteContent", out command))
				{
					Action action = () =>
					{
						_presenter.BeginUpdate();
						var cell = _presenter.Manager.GetCell(_presenter.CurrentCell);

						if (cell != null)
						{
							if (cell.Content is CellReport.Core.Contents.CollectionContent)
							{
								StateManager.BeginUpdate();
								var collection = cell.Content as CellReport.Core.Contents.CollectionContent;
								var findContent = collection.GetContents().FirstOrDefault(c => c.Equals(Values.Content));
								if (findContent != null)
								{
									collection.Remove(findContent);
									Values.Content = collection.GetContents().First();
								}
								if (collection.Count == 0)
								{
									cell.Content = null;
									Values.Content = null;
								}
								else if (collection.Count == 1)
								{
									cell.Content = collection.GetContents().First();
									Values.Content = cell.Content;
								}
								StateManager.EndUpdate();
							}
							else if (cell.Content != null)
							{
								cell.Content = null;
								Values.Content = null;
							}
							Values.CurrentContents = null; // Update contents
											}
						_presenter.EndUpdate();
					};
					Func<bool> enabled = () => Values.Content != null;
					command = new DelegateCommand(action, enabled);
					_commands.Add("DeleteContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand ShowContentProperties
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("ShowContentProperties", out command))
				{
					Action action = () =>
					{
						if (Values.Content.GetType() == typeof(TextContent))
						{
							StateManager.BeginUpdate();
							_presenter.BeginUpdate();
							var wnd = new PropertiesWindows.TextContentProperty(Values.Content);
							wnd.ShowDialog();
							_presenter.EndUpdate();
							StateManager.EndUpdate();
						}
					};
					Func<bool> enabled = () => Values.Content != null;
					command = new DelegateCommand(action, enabled);
					_commands.Add("ShowContentProperties", command);
				}

				return command;
			}
		}

		public static DelegateCommand ShowContentParams
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("ShowContentParams", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						var paramWnd = new Params(Values.Content, _presenter.Manager);
						paramWnd.ShowDialog();
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					Func<bool> enabled = () => Values.Content != null;
					command = new DelegateCommand(action, enabled);
					_commands.Add("ShowContentParams", command);
				}

				return command;
			}
		}

		public static DelegateCommand Copy
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("Copy", out command))
				{
					Action action = () =>
					{
						CopyManager.Copy();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("Copy", command);
				}

				return command;
			}
		}

		public static DelegateCommand CopyContent
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("CopyContent", out command))
				{
					Action action = () =>
					{
						CopyManager.CopyContent();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("CopyContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand Paste
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("Paste", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						_presenter.BeginUpdate();
						CopyManager.Paste();
						_values = new CurrentValues(_presenter);
						Updater.Update(false);
						_presenter.EndUpdate();
						StateManager.EndUpdate();
					};
					Func<bool> enabled = () => IsDefaultEnabled() && CopyManager.HasData;
					command = new DelegateCommand(action, enabled);
					_commands.Add("Paste", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddSection
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddSection", out command))
				{
					Action action = () =>
					{
						var section = new CellReport.Core.TemplateSection();
						section.Name = "Секция " + Values.Sections.Count;
						if (!_presenter.StartSelection.Equals(CellLocation.None))
						{
							section.LeftTop = _presenter.GetStartSelection();
							section.RightBottom = _presenter.GetEndSelection();
						}
						else
						{
							section.LeftTop = _presenter.CurrentCell;
							section.RightBottom = _presenter.CurrentCell;
						}
						_presenter.Manager.AddSection(section);
						_values.Sections = null; // Refresh
											_values.CurrentSection = section;
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddSection", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteSection
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("DeleteSection", out command))
				{
					Action action = () =>
					{
						_presenter.Manager.RemoveSection(_values.CurrentSection);
						_values.Sections = null;//refresh
											_values.CurrentSection = _values.Sections.FirstOrDefault();
					};
					Func<bool> enabled = () => _values != null && _values.CurrentSection != null;
					command = new DelegateCommand(action, enabled);
					_commands.Add("DeleteSection", command);
				}

				return command;
			}
		}

		public static DelegateCommand ShowExpressions
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("ShowExpressions", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						var expWnd = new Expressions();
						expWnd.ShowDialog();
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("ShowExpressions", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddTextStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddTextStyle", out command))
				{
					Action action = () =>
					{
						var styleWnd = new CellReport.Studio.PropertiesWindows.TextStyle();
						var style = new DesignTextStyle();
						styleWnd.DataContext = style;
						var result = styleWnd.ShowDialog();
						if (result.HasValue && result.Value)
						{
							var newStyle = style.ToTextStyle();
							_presenter.Manager.ReplaceStyle(newStyle);
							Studio.ViewModel.StudioCommands.Values.TextStyles = null;
							Studio.ViewModel.StudioCommands.Values.CurrTextStyle = newStyle;
						}
						_presenter.Update();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddTextStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand EditTextStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("EditTextStyle", out command))
				{
					Action action = () =>
					{
						var styleWnd = new CellReport.Studio.PropertiesWindows.TextStyle();
						var style = new DesignTextStyle(Studio.ViewModel.StudioCommands.Values.CurrTextStyle);
						styleWnd.DataContext = style;
						var result = styleWnd.ShowDialog();
						if (result.HasValue && result.Value)
						{
							var newStyle = style.ToTextStyle();
							_presenter.Manager.ReplaceStyle(newStyle);
							Studio.ViewModel.StudioCommands.Values.TextStyles = null;
							Studio.ViewModel.StudioCommands.Values.CurrTextStyle = newStyle;
						}
						_presenter.Update();
					};
					command = new DelegateCommand(action, () => Studio.ViewModel.StudioCommands.Values.CurrTextStyle != null);
					_commands.Add("EditTextStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand DelTextStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("DelTextStyle", out command))
				{
					Action action = () =>
					{
						_presenter.Manager.RemoveStyle(Studio.ViewModel.StudioCommands.Values.CurrTextStyle);
						Studio.ViewModel.StudioCommands.Values.TextStyles = null;
						Studio.ViewModel.StudioCommands.Values.CurrTextStyle = (TextContentStyle)Studio.ViewModel.StudioCommands.Values.TextStyles.FirstOrDefault();
						_presenter.Update();
					};
					command = new DelegateCommand(action, () => Studio.ViewModel.StudioCommands.Values.CurrTextStyle != null);
					_commands.Add("DelTextStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddDecimalStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddDecimalStyle", out command))
				{
					Action action = () =>
					{
						var styleWnd = new CellReport.Studio.PropertiesWindows.DecimalStyle();
						var style = new DesignDecimalStyle();
						styleWnd.DataContext = style;
						var result = styleWnd.ShowDialog();
						if (result.HasValue && result.Value)
						{
							var newStyle = style.ToDecimalStyle();
							_presenter.Manager.ReplaceStyle(newStyle);
							Studio.ViewModel.StudioCommands.Values.DecimalStyles = null;
							Studio.ViewModel.StudioCommands.Values.CurrDecimalStyle = newStyle;
						}
						_presenter.Update();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddDecimalStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand EditDecimalStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("EditDecimalStyle", out command))
				{
					Action action = () =>
					{
						var styleWnd = new CellReport.Studio.PropertiesWindows.DecimalStyle();
						var style = new DesignDecimalStyle(Studio.ViewModel.StudioCommands.Values.CurrDecimalStyle);
						styleWnd.DataContext = style;
						var result = styleWnd.ShowDialog();
						if (result.HasValue && result.Value)
						{
							var newStyle = style.ToDecimalStyle();
							_presenter.Manager.ReplaceStyle(newStyle);
							Studio.ViewModel.StudioCommands.Values.DecimalStyles = null;
							Studio.ViewModel.StudioCommands.Values.CurrDecimalStyle = newStyle;
						}
						_presenter.Update();
					};
					command = new DelegateCommand(action, () => Studio.ViewModel.StudioCommands.Values.CurrDecimalStyle != null);
					_commands.Add("EditDecimalStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand DelDecimalStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("DelDecimalStyle", out command))
				{
					Action action = () =>
					{
						_presenter.Manager.RemoveStyle(Studio.ViewModel.StudioCommands.Values.CurrDecimalStyle);
						Studio.ViewModel.StudioCommands.Values.DecimalStyles = null;
						Studio.ViewModel.StudioCommands.Values.CurrDecimalStyle = (DecimalContentStyle)Studio.ViewModel.StudioCommands.Values.DecimalStyles.FirstOrDefault();
						_presenter.Update();
					};
					command = new DelegateCommand(action, () => Studio.ViewModel.StudioCommands.Values.CurrDecimalStyle != null);
					_commands.Add("DelDecimalStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddDateTimeStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddDateTimeStyle", out command))
				{
					Action action = () =>
					{
						var styleWnd = new CellReport.Studio.PropertiesWindows.DateTimeStyle();
						var style = new DesignDateTimelStyle();
						styleWnd.DataContext = style;
						var result = styleWnd.ShowDialog();
						if (result.HasValue && result.Value)
						{
							var newStyle = style.ToDateTimeStyle();
							_presenter.Manager.ReplaceStyle(newStyle);
							Studio.ViewModel.StudioCommands.Values.DateTimeStyles = null;
							Studio.ViewModel.StudioCommands.Values.CurrDateTimeStyle = newStyle;
						}
						_presenter.Update();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddDateTimeStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand EditDateTimeStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("EditDateTimeStyle", out command))
				{
					Action action = () =>
					{
						var styleWnd = new CellReport.Studio.PropertiesWindows.DateTimeStyle();
						var style = new DesignDateTimelStyle(Studio.ViewModel.StudioCommands.Values.CurrDateTimeStyle);
						styleWnd.DataContext = style;
						var result = styleWnd.ShowDialog();
						if (result.HasValue && result.Value)
						{
							var newStyle = style.ToDateTimeStyle();
							_presenter.Manager.ReplaceStyle(newStyle);
							Studio.ViewModel.StudioCommands.Values.DateTimeStyles = null;
							Studio.ViewModel.StudioCommands.Values.CurrDateTimeStyle = newStyle;
						}
						_presenter.Update();
					};
					command = new DelegateCommand(action, () => Studio.ViewModel.StudioCommands.Values.CurrDateTimeStyle != null);
					_commands.Add("EditDateTimeStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand DelDateTimeStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("DelDateTimeStyle", out command))
				{
					Action action = () =>
					{
						_presenter.Manager.RemoveStyle(Studio.ViewModel.StudioCommands.Values.CurrDateTimeStyle);
						Studio.ViewModel.StudioCommands.Values.DateTimeStyles = null;
						Studio.ViewModel.StudioCommands.Values.CurrDateTimeStyle = (DateTimeContentStyle)Studio.ViewModel.StudioCommands.Values.DateTimeStyles.FirstOrDefault();
						_presenter.Update();
					};
					command = new DelegateCommand(action, () => Studio.ViewModel.StudioCommands.Values.CurrDateTimeStyle != null);
					_commands.Add("DelDateTimeStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddCellStyle
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("AddCellStyle", out command))
				{
					Action action = () =>
					{
						var styleWnd = new CellReport.Studio.PropertiesWindows.CellStyle();
						var style = new DesignCellStyle();
						styleWnd.DataContext = style;
						var result = styleWnd.ShowDialog();
						if (result.HasValue && result.Value)
						{
							var newStyle = style.ToStyle();
							_presenter.Manager.ReplaceStyle(newStyle);
							StudioCommands.Values.CellStyles = null;
							StudioCommands.Values.CurrCellEditStyle = newStyle;
						}
						_presenter.Update();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("AddCellStyle", command);
				}

				return command;
			}
		}

		public static DelegateCommand EditCellStyle
		{
			get
			{
				DelegateCommand command = null;
				if (_commands.TryGetValue("EditCellStyle", out command))
					return command;

				Action action = () =>
				{
					var styleWnd = new CellReport.Studio.PropertiesWindows.CellStyle();
					var style = new DesignCellStyle(StudioCommands.Values.CurrCellEditStyle);
					styleWnd.DataContext = style;
					var result = styleWnd.ShowDialog();
					if (result.HasValue && result.Value)
					{
						var newStyle = style.ToStyle();
						_presenter.Manager.ReplaceStyle(newStyle);
						StudioCommands.Values.CellStyles = null;
						StudioCommands.Values.CurrCellEditStyle = newStyle;
					}
					_presenter.Update();
				};
				command = new DelegateCommand(action, () => StudioCommands.Values.CurrCellEditStyle != null);
				_commands.Add("EditCellStyle", command);
				return command;
			}
		}

		public static DelegateCommand DelCellStyle
		{
			get
			{
				DelegateCommand command = null;
				if (_commands.TryGetValue("DelCellStyle", out command))
					return command;

				Action action = () =>
				{
					_presenter.Manager.RemoveStyle(StudioCommands.Values.CurrCellEditStyle);
					Studio.ViewModel.StudioCommands.Values.CellStyles = null;
					Studio.ViewModel.StudioCommands.Values.CurrCellEditStyle = StudioCommands.Values.CellStyles.FirstOrDefault();
					_presenter.Update();
				};
				command = new DelegateCommand(action, () => StudioCommands.Values.CurrCellEditStyle != null);
				_commands.Add("DelCellStyle", command);
				return command;
			}
		}

		public static DelegateCommand RemoveUnusedStyles
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("RemoveUnusedStyles", out command))
				{
					Action action = () =>
					{
						StateManager.BeginUpdate();
						Manager.RemoveUnusedStyles();
						Studio.ViewModel.StudioCommands.Values.CellStyles = null;
						Studio.ViewModel.StudioCommands.Values.TextStyles = null;
						Studio.ViewModel.StudioCommands.Values.DecimalStyles = null;
						Studio.ViewModel.StudioCommands.Values.DateTimeStyles = null;
						StateManager.EndUpdate();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("RemoveUnusedStyles", command);
				}

				return command;
			}
		}

		public static DelegateCommand Header
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("Header", out command))
				{
					Action action = () =>
					{
						var pagerProps = new CellReport.Studio.PropertiesWindows.PagerProperties(PrintSettings.Headers);
						if (pagerProps.ShowDialog() == true)
						{
							StateManager.BeginUpdate();
							PrintSettings.Headers = DesignPager.GetPagers(pagerProps.Pagers, pagerProps.Expressions);
							StateManager.EndUpdate();
						}
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("Header", command);
				}

				return command;
			}
		}

		public static DelegateCommand Footer
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("Footer", out command))
				{
					Action action = () =>
					{
						var pagerProps = new CellReport.Studio.PropertiesWindows.PagerProperties(PrintSettings.Footers);
						if (pagerProps.ShowDialog() == true)
						{
							StateManager.BeginUpdate();
							PrintSettings.Footers = DesignPager.GetPagers(pagerProps.Pagers, pagerProps.Expressions);
							StateManager.EndUpdate();
						}
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("Footer", command);
				}

				return command;
			}
		}

		public static DelegateCommand Resources
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("Resources", out command))
				{
					Action action = () =>
					{
						var resEditor = new CellReport.Studio.PropertiesWindows.ResourcesEditor();
						if (resEditor.ShowDialog() == true)
						{
							StateManager.BeginUpdate();
							DesignResource.SaveResources(resEditor.DesignResources);
							StateManager.EndUpdate();
						}
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("Resources", command);
				}

				return command;
			}
		}

		public static DelegateCommand Print
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("Print", out command))
				{
					Action action = () =>
					{
						var printPresenter = new CellReport.Presenters.WinForms.PrintPresenter(_presenter.Manager);
						printPresenter.CustomPrint();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("Print", command);
				}

				return command;
			}
		}

		public static DelegateCommand PrintPreview
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("PrintPreview", out command))
				{
					Action action = () =>
					{
						var printPresenter = new CellReport.Presenters.WinForms.PrintPresenter(_presenter.Manager);
						printPresenter.ShowPreviewDialog();
					};
					command = new DelegateCommand(action, IsDefaultEnabled);
					_commands.Add("PrintPreview", command);
				}

				return command;
			}
		}

		public static DelegateCommand Undo
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("Undo", out command))
				{
					command = new DelegateCommand(StateManager.Undo, StateManager.CanUndo);
					_commands.Add("Undo", command);
				}

				return command;
			}
		}

		public static DelegateCommand Redo
		{
			get
			{
				DelegateCommand command = null;
				if (!_commands.TryGetValue("Redo", out command))
				{
					command = new DelegateCommand(StateManager.Redo, StateManager.CanRedo);
					_commands.Add("Redo", command);
				}

				return command;
			}
		}
	}
}
