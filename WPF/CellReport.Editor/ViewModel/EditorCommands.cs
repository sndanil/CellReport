using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using CellReport.Core;
using CellReport.Editor.Import;
using CellReport.Presenters.Wpf;

namespace CellReport.Editor.ViewModel
{
	public static class EditorCommands
	{
		private static readonly Dictionary<string, DelegateCommand> Commands = new Dictionary<string, DelegateCommand>();

		private static bool IsDefaultEnabled()
		{
			return EditorModel.ReportManager != null && !EditorModel.IsCodeMode;
		}

		public static DelegateCommand Create
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("create", out command))
				{
					void Action()
					{
						var manager = new CellManager();
						manager.AddExternalStyle(new ExternalStyle("file://DefaultStyles.xml"));

						ApplyReport(manager);
					}

					command = new DelegateCommand(Action);
					Commands.Add("create", command);
				}

				return command;
			}
		}

		public static DelegateCommand Open
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("open", out command))
				{
					void Action()
					{
						var dialog = new Microsoft.Win32.OpenFileDialog
						{
							Filter = "CellReport шаблон (*.tcr)|*.tcr|CellReport шаблон упакованный (*.tcrp)|*.tcrp|Все файлы (*.*)|*.*",
							FilterIndex = 2
						};
						var result = dialog.ShowDialog();
						if (result == true)
						{
							EditorModel.DesignerTab.IsSelected = true;

							CellTemplate template = new CellTemplate();
							template.Load(dialog.FileName);
							EditorModel.State.FileName = dialog.FileName;

							ApplyReport(template.Manager);
						}
					}

					command = new DelegateCommand(Action);
					Commands.Add("open", command);
				}

				return command;
			}
		}

		public static DelegateCommand Import
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("import", out command))
				{
					void Action()
					{
						var dialog = new Microsoft.Win32.OpenFileDialog
						{
							Filter = "Excel (*.xlsx)|*.xlsx",
						};
						var result = dialog.ShowDialog();
						if (result == true)
						{
							EditorModel.DesignerTab.IsSelected = true;

							CellManager manager = ExcelImporter.Import(dialog.FileName);
							if (manager != null)
							{
								EditorModel.State.FileName = dialog.FileName;
								ApplyReport(manager);
							}
						}
					}

					command = new DelegateCommand(Action);
					Commands.Add("import", command);
				}

				return command;
			}
		}

		public static void ApplyReport(CellManager manager)
		{
			EditorModel.ReportManager = null;
			EditorModel.ReportContainer.Focus();

			EditorModel.ReportManager = manager;
			EditorModel.State.Manager = manager;
			EditorModel.State.Presenter = new DisplayPresenter(manager, EditorModel.ReportContainer);
		}

		public static DelegateCommand Save
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("save", out command))
				{
					void Action()
					{
						SaveReport(false);
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("save", command);
				}

				return command;
			}
		}

		public static DelegateCommand SaveAs
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("SaveAs", out command))
				{
					void Action()
					{
						SaveReport(true);
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("SaveAs", command);
				}

				return command;
			}
		}

		private static void SaveReport(bool saveAs)
		{
			var emptyName = string.IsNullOrEmpty(EditorModel.State.FileName);
			if (emptyName || saveAs)
			{
				var dialog = new Microsoft.Win32.SaveFileDialog
				{
					Filter = "CellReport шаблон (*.tcr)|*.tcr|CellReport шаблон упакованный (*.tcrp)|*.tcrp|Все файлы (*.*)|*.*",
					FilterIndex = 2
				};
				if (!emptyName)
					dialog.FileName = Path.GetFileName(EditorModel.State.FileName);
				if (dialog.ShowDialog() == true)
					EditorModel.State.FileName = dialog.FileName;
				else
					return;
			}
			CellTemplate template = new CellTemplate(EditorModel.ReportManager);
			template.Save(EditorModel.State.FileName, Path.GetExtension(EditorModel.State.FileName)?.ToUpper() == ".TCRP" ? TemplateType.Package : TemplateType.Xml);
		}

		public static DelegateCommand Print
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("Print", out command))
				{
					void Action()
					{
						var printPresenter = new Presenters.WinForms.PrintPresenter(EditorModel.ReportManager);
						printPresenter.CustomPrint();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("Print", command);
				}

				return command;
			}
		}

		public static DelegateCommand PrintPreview
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("PrintPreview", out command))
				{
					void Action()
					{
						var printPresenter = new Presenters.WinForms.PrintPresenter(EditorModel.ReportManager);
						printPresenter.ShowPreviewDialog();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("PrintPreview", command);
				}

				return command;
			}
		}

		public static DelegateCommand Exit
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("Exit", out command))
				{
					void Action()
					{
						Environment.Exit(0);
					}

					command = new DelegateCommand(Action);
					Commands.Add("Exit", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteRows
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("DeleteRows", out command))
				{
					void Action()
					{
						EditorModel.State.Presenter.BeginUpdate();
						if (EditorModel.State.Presenter.HaveSelection())
						{
							var start = EditorModel.State.Presenter.GetStartSelection();
							var end = EditorModel.State.Presenter.GetEndSelection();
							EditorModel.State.Presenter.ClearSelection();
							EditorModel.State.Presenter.Manager.DeleteRows(start.Row, end.Row - start.Row + 1);
						}
						else
							EditorModel.State.Presenter.Manager.DeleteRows(EditorModel.State.Presenter.CurrentCell.Row, 1);
						if (EditorModel.State.Presenter.CurrentCell.Row >= EditorModel.State.Presenter.Manager.RowsCount)
							EditorModel.State.Presenter.CurrentCell = new CellLocation() {Row = EditorModel.State.Presenter.CurrentCell.Row - 1, Column = EditorModel.State.Presenter.CurrentCell.Column};
						EditorModel.State.Presenter.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("DeleteRows", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteColumns
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("DeleteColumns", out command))
				{
					void Action()
					{
						EditorModel.State.Presenter.BeginUpdate();
						if (EditorModel.State.Presenter.HaveSelection())
						{
							var start = EditorModel.State.Presenter.GetStartSelection();
							var end = EditorModel.State.Presenter.GetEndSelection();
							EditorModel.State.Presenter.ClearSelection();
							EditorModel.State.Presenter.Manager.DeleteColumns(start.Column, end.Column - start.Column + 1);
						}
						else
							EditorModel.State.Presenter.Manager.DeleteColumns(EditorModel.State.Presenter.CurrentCell.Column, 1);
						if (EditorModel.State.Presenter.CurrentCell.Column >= EditorModel.State.Presenter.Manager.ColumnsCount)
							EditorModel.State.Presenter.CurrentCell = new CellLocation() {Row = EditorModel.State.Presenter.CurrentCell.Row, Column = EditorModel.State.Presenter.CurrentCell.Column - 1};
						EditorModel.State.Presenter.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("DeleteColumns", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddRows
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("addRows", out command))
				{
					void Action()
					{
						EditorModel.State.Presenter.BeginUpdate();
						EditorModel.State.Presenter.Manager.InsertRows(EditorModel.State.Presenter.CurrentCell.Row, 1);
						EditorModel.State.Presenter.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("addRows", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddRowsAfter
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("addRowsAfter", out command))
				{
					void Action()
					{
						EditorModel.State.Presenter.BeginUpdate();
						EditorModel.State.Presenter.Manager.InsertRows(EditorModel.State.Presenter.CurrentCell.Row + 1, 1);
						EditorModel.State.Presenter.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("addRowsAfter", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddColumns
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("AddColumns", out command))
				{
					void Action()
					{
						EditorModel.State.Presenter.BeginUpdate();
						EditorModel.State.Presenter.Manager.InsertColumns(EditorModel.State.Presenter.CurrentCell.Column, 1);
						EditorModel.State.Presenter.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddColumns", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddColumnsAfter
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("AddColumnsAfter", out command))
				{
					void Action()
					{
						EditorModel.State.Presenter.BeginUpdate();
						EditorModel.State.Presenter.Manager.InsertColumns(EditorModel.State.Presenter.CurrentCell.Column + 1, 1);
						EditorModel.State.Presenter.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddColumnsAfter", command);
				}

				return command;
			}
		}

		public static DelegateCommand MergeCells
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("MergeCells", out command))
				{
					void Action()
					{
						//StateManager.BeginUpdate();
						EditorModel.State.Presenter.BeginUpdate();
						if (EditorModel.State.Presenter.HaveSelection())
						{
							var start = EditorModel.State.Presenter.GetStartSelection();
							var end = EditorModel.State.Presenter.GetEndSelection();
							var cell = EditorModel.ReportManager.GetCell(EditorModel.State.Presenter.CurrentCell.Row, EditorModel.State.Presenter.CurrentCell.Column);
							if (cell != null)
								cell = EditorModel.State.Presenter.Manager.CreateCell(start.Row, start.Column, cell);
							else
								cell = EditorModel.State.Presenter.Manager.CreateCell(start);
							cell.Span = new CellSpan {RowSpan = end.Row - start.Row, ColSpan = end.Column - start.Column};
							EditorModel.State.Presenter.ClearSelection();
							EditorModel.State.Presenter.CurrentCell = start;
						}
						else if (EditorModel.State.Presenter.Manager.GetCell(EditorModel.State.Presenter.CurrentCell) != null)
							EditorModel.State.Presenter.Manager.GetCell(EditorModel.State.Presenter.CurrentCell).Span = new CellSpan();
						EditorModel.State.Presenter.EndUpdate();
					}

					command = new DelegateCommand(Action, () =>
					{
						return IsDefaultEnabled();
					});
					Commands.Add("MergeCells", command);
				}

				return command;
			}
		}

		public static DelegateCommand Find
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("Find", out command))
				{
					void Action()
					{
						if (EditorModel.IsDesignMode)
						{
							EditorModel.State.Presenter.ShowFindDialog();
						}
						if (EditorModel.IsCodeMode)
						{
							FindAndReplaceDialog.ShowForFind(EditorModel.Editor);
						}
					}

					command = new DelegateCommand(Action, () => EditorModel.ReportManager != null && (EditorModel.IsDesignMode || EditorModel.IsCodeMode));
					Commands.Add("Find", command);
				}

				return command;
			}
		}

		public static DelegateCommand FindAndReplace
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("FindAndReplace", out command))
				{
					void Action()
					{
						if (EditorModel.IsCodeMode)
						{
							FindAndReplaceDialog.ShowForReplace(EditorModel.Editor);
						}
					}

					command = new DelegateCommand(Action, () => EditorModel.ReportManager != null && (EditorModel.IsCodeMode));
					Commands.Add("FindAndReplace", command);
				}

				return command;
			}
		}

		public static DelegateCommand FindInCode
		{
			get
			{
				DelegateCommand command = null;
				if (!Commands.TryGetValue("FindInCode", out command))
				{
					void Action()
					{
						EditorModel.CodeTab.IsSelected = true;
						SwitchEditorToCodeMode();
						Task.Delay(500).ContinueWith(_ =>
							{
								var lineNumber = 0;
								var currentRow = EditorModel.State.Presenter.CurrentCell.Row;
								var rowIndex = -1;
								var textLines = EditorModel.Editor.Text.Split('\n');
								var selectStartLine = 0;
								var selectEndLine = 0;
								foreach (var textLine in textLines)
								{
									var line = textLine.Replace("\r", "");
									if (line.Contains("<Row") && rowIndex != currentRow && selectStartLine == 0)
									{
										rowIndex++;

										var rowElement = XElement.Parse(line.EndsWith("/>") ? line : line + "</Row>");
										if (rowElement.Attribute("Index") != null)
										{
											rowIndex = Convert.ToInt32(rowElement.Attribute("Index").Value);
										}
									}

									if (rowIndex == currentRow && selectStartLine == 0)
									{
										selectStartLine = lineNumber + 1;
									}

									if ((line.Contains("</Row") || line.Contains("<Row") && line.EndsWith("/>")) && selectStartLine != 0)
									{
										selectEndLine = lineNumber + 1;
										break;
									}

									lineNumber++;
								}

								var startLine = EditorModel.Editor.Document.GetLineByNumber(selectStartLine);
								var endLine = EditorModel.Editor.Document.GetLineByNumber(selectEndLine);
								EditorModel.Editor.Select(startLine.Offset, endLine.Offset + endLine.Length - startLine.Offset);

								double vertOffset = (EditorModel.Editor.TextArea.TextView.DefaultLineHeight) * (selectStartLine - 1);
								EditorModel.Editor.ScrollToVerticalOffset(vertOffset);
								EditorModel.Editor.TextArea.Focus();
							}, TaskScheduler.FromCurrentSynchronizationContext());
					}

					command = new DelegateCommand(Action, () => IsDefaultEnabled() && EditorModel.IsDesignMode);
					Commands.Add("FindInCode", command);
				}

				return command;
			}
		}

		public static void SwitchEditorToCodeMode()
		{
			if (EditorModel.ReportManager == null || EditorModel.State.Presenter == null)
				return;

			CellTemplate template = new CellTemplate(EditorModel.ReportManager);
			var memoryStream = new MemoryStream();
			template.Save(memoryStream, TemplateType.Xml);
			memoryStream.Position = 0;

			EditorModel.Editor.Text = new StreamReader(memoryStream).ReadToEnd();
		}

		public static void SwitchEditorToDesignMode()
		{
			if (EditorModel.ReportManager == null)
				return;

			try
			{
				CellTemplate template = new CellTemplate();
				template.LoadFromXml(EditorModel.Editor.Text);
				var currentCell = EditorModel.State.Presenter?.CurrentCell;
				foreach (var resourceName in EditorModel.ReportManager.GetResourceNames())
				{
					template.Manager.AddResource(resourceName, EditorModel.ReportManager.GetResource(resourceName));
				}
				ApplyReport(template.Manager);
				if (currentCell.HasValue)
					EditorModel.State.Presenter.CurrentCell = currentCell.Value;
			}
			catch (Exception e)
			{
				EditorModel.State.Presenter = null;
			}
		}
	}
}
