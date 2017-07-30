using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using CellReport.Core;
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
							Filter = "CellReport templates (*.tcr)|*.tcr|CellReport templates package (*.tcrp)|*.tcrp|All files (*.*)|*.*",
							FilterIndex = 2
						};
						var result = dialog.ShowDialog();
						if (result == true)
						{
							EditorModel.DesignerTab.IsSelected = true;

							CellTemplate template = new CellTemplate();
							template.Load(dialog.FileName);
							EditorModel.FileName = dialog.FileName;

							ApplyReport(template.Manager);
						}
					}

					command = new DelegateCommand(Action);
					Commands.Add("open", command);
				}

				return command;
			}
		}

		public static void ApplyReport(CellManager manager)
		{
			EditorModel.ReportManager = null;
			EditorModel.Presenter = null;
			EditorModel.ReportContainer.Focus();

			EditorModel.ReportManager = manager;
			EditorModel.Presenter = new DisplayPresenter(manager, EditorModel.ReportContainer);
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
			var emptyName = string.IsNullOrEmpty(EditorModel.FileName);
			if (emptyName || saveAs)
			{
				var dialog = new Microsoft.Win32.SaveFileDialog
				{
					Filter = "CellReport templates (*.tcr)|*.tcr|CellReport templates package (*.tcrp)|*.tcrp|All files (*.*)|*.*",
					FilterIndex = 2
				};
				if (!emptyName)
					dialog.FileName = Path.GetFileName(EditorModel.FileName);
				if (dialog.ShowDialog() == true)
					EditorModel.FileName = dialog.FileName;
				else
					return;
			}
			CellTemplate template = new CellTemplate(EditorModel.ReportManager);
			template.Save(EditorModel.FileName, Path.GetExtension(EditorModel.FileName)?.ToUpper() == ".TCRP" ? TemplateType.Package : TemplateType.Xml);
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
						EditorModel.Presenter.BeginUpdate();
						if (EditorModel.Presenter.HaveSelection())
						{
							var start = EditorModel.Presenter.GetStartSelection();
							var end = EditorModel.Presenter.GetEndSelection();
							EditorModel.Presenter.ClearSelection();
							EditorModel.Presenter.Manager.DeleteRows(start.Row, end.Row - start.Row + 1);
						}
						else
							EditorModel.Presenter.Manager.DeleteRows(EditorModel.Presenter.CurrentCell.Row, 1);
						if (EditorModel.Presenter.CurrentCell.Row >= EditorModel.Presenter.Manager.RowsCount)
							EditorModel.Presenter.CurrentCell = new CellLocation() {Row = EditorModel.Presenter.CurrentCell.Row - 1, Column = EditorModel.Presenter.CurrentCell.Column};
						EditorModel.Presenter.EndUpdate();
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
						EditorModel.Presenter.BeginUpdate();
						if (EditorModel.Presenter.HaveSelection())
						{
							var start = EditorModel.Presenter.GetStartSelection();
							var end = EditorModel.Presenter.GetEndSelection();
							EditorModel.Presenter.ClearSelection();
							EditorModel.Presenter.Manager.DeleteColumns(start.Column, end.Column - start.Column + 1);
						}
						else
							EditorModel.Presenter.Manager.DeleteColumns(EditorModel.Presenter.CurrentCell.Column, 1);
						if (EditorModel.Presenter.CurrentCell.Column >= EditorModel.Presenter.Manager.ColumnsCount)
							EditorModel.Presenter.CurrentCell = new CellLocation() {Row = EditorModel.Presenter.CurrentCell.Row, Column = EditorModel.Presenter.CurrentCell.Column - 1};
						EditorModel.Presenter.EndUpdate();
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
						EditorModel.Presenter.BeginUpdate();
						EditorModel.Presenter.Manager.InsertRows(EditorModel.Presenter.CurrentCell.Row, 1);
						EditorModel.Presenter.EndUpdate();
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
						EditorModel.Presenter.BeginUpdate();
						EditorModel.Presenter.Manager.InsertRows(EditorModel.Presenter.CurrentCell.Row + 1, 1);
						EditorModel.Presenter.EndUpdate();
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
						EditorModel.Presenter.BeginUpdate();
						EditorModel.Presenter.Manager.InsertColumns(EditorModel.Presenter.CurrentCell.Column, 1);
						EditorModel.Presenter.EndUpdate();
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
						EditorModel.Presenter.BeginUpdate();
						EditorModel.Presenter.Manager.InsertColumns(EditorModel.Presenter.CurrentCell.Column + 1, 1);
						EditorModel.Presenter.EndUpdate();
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
						EditorModel.Presenter.BeginUpdate();
						if (EditorModel.Presenter.HaveSelection())
						{
							var start = EditorModel.Presenter.GetStartSelection();
							var end = EditorModel.Presenter.GetEndSelection();
							var cell = EditorModel.ReportManager.GetCell(EditorModel.Presenter.CurrentCell.Row, EditorModel.Presenter.CurrentCell.Column);
							if (cell != null)
								cell = EditorModel.Presenter.Manager.CreateCell(start.Row, start.Column, cell);
							else
								cell = EditorModel.Presenter.Manager.CreateCell(start);
							cell.Span = new CellSpan {RowSpan = end.Row - start.Row, ColSpan = end.Column - start.Column};
							EditorModel.Presenter.ClearSelection();
							EditorModel.Presenter.CurrentCell = start;
						}
						else if (EditorModel.Presenter.Manager.GetCell(EditorModel.Presenter.CurrentCell) != null)
							EditorModel.Presenter.Manager.GetCell(EditorModel.Presenter.CurrentCell).Span = new CellSpan();
						EditorModel.Presenter.EndUpdate();
						//StateManager.EndUpdate();
					}

					command = new DelegateCommand(Action, () =>
					{
						return IsDefaultEnabled();
						//&& (EditorModel.Presenter?.HaveSelection() == true || EditorModel.Presenter?.HaveSelection() == true)
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
							EditorModel.Presenter.ShowFindDialog();
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
						var lineNumber = 0;
						var currentRow = EditorModel.Presenter.CurrentCell.Row;
						var rowIndex = -1;
						var textLines = EditorModel.Editor.Text.Split('\n');
						foreach (var textLine in textLines)
						{
							var line = textLine.Replace("\r", "");
							if (line.Contains("<Row") && rowIndex != currentRow)
							{
								rowIndex++;

								var rowElement = XElement.Parse(line.EndsWith("/>") ? line : line + "</Row>");
								if (rowElement.Attribute("Index") != null)
								{
									rowIndex = Convert.ToInt32(rowElement.Attribute("Index").Value);
								}
							}

							if (rowIndex == currentRow)
								break;

							lineNumber++;
						}

						double vertOffset = (EditorModel.Editor.TextArea.TextView.DefaultLineHeight) * lineNumber;
						EditorModel.Editor.ScrollToVerticalOffset(vertOffset);
						EditorModel.Editor.TextArea.Focus();
					}

					command = new DelegateCommand(Action, () => IsDefaultEnabled() && EditorModel.IsDesignMode);
					Commands.Add("FindInCode", command);
				}

				return command;
			}
		}

		public static void SwitchEditorToCodeMode()
		{
			if (EditorModel.ReportManager == null || EditorModel.Presenter == null)
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
				var currentCell = EditorModel.Presenter?.CurrentCell;
				EditorModel.Presenter = new DisplayPresenter(template.Manager, EditorModel.ReportContainer);
				foreach (var resourceName in EditorModel.ReportManager.GetResourceNames())
				{
					template.Manager.AddResource(resourceName, EditorModel.ReportManager.GetResource(resourceName));
				}
				EditorModel.ReportManager = template.Manager;
				if (currentCell.HasValue)
					EditorModel.Presenter.CurrentCell = currentCell.Value;
			}
			catch (Exception e)
			{
				EditorModel.Presenter = null;
			}
		}
	}
}
