using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Editor.Import;
using CellReport.Presenters.Wpf;

namespace CellReport.Editor.ViewModel
{
	public static class EditorCommands
	{
		private static readonly Dictionary<string, DelegateCommand> Commands = new Dictionary<string, DelegateCommand>();

		private static bool IsDefaultEnabled()
		{
			return EditorModel.State?.Manager != null && !EditorModel.IsCodeMode;
		}

		public static DelegateCommand Create
		{
			get
			{
				if (!Commands.TryGetValue("create", out var command))
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
				if (!Commands.TryGetValue("open", out var command))
				{
					void Action()
					{
						var dialog = new Microsoft.Win32.OpenFileDialog
						{
							Filter = "CellReport шаблон|*.tcr;*.tcrp|Все файлы (*.*)|*.*",
							FilterIndex = 1
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
				if (!Commands.TryGetValue("import", out var command))
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
								EditorModel.State.FileName = Path.Combine(Path.GetDirectoryName(dialog.FileName), Path.GetFileNameWithoutExtension(dialog.FileName) + ".tcr");
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

		public static void ApplyReport(CellManager manager, CellLocation currentCell = new CellLocation())
		{
			EditorModel.ReportContainer.Focus();

			EditorModel.State.Presenter = new DisplayPresenter(manager, EditorModel.ReportContainer) { CurrentCell = currentCell };
			EditorModel.State.Clear();
			EditorModel.State.AddCurrentState();
		}

		public static DelegateCommand Save
		{
			get
			{
				if (!Commands.TryGetValue("save", out var command))
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
				if (!Commands.TryGetValue("SaveAs", out var command))
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
				var filterIndex = 1;
				if (!emptyName)
				{
					filterIndex = EditorModel.State.FileName.ToLower().EndsWith("tcrp") ? 2 : 1;
				}
				var dialog = new Microsoft.Win32.SaveFileDialog
				{
					Filter = "CellReport шаблон (*.tcr)|*.tcr|CellReport шаблон упакованный (*.tcrp)|*.tcrp|Все файлы (*.*)|*.*",
					FilterIndex = filterIndex
				};
				if (!emptyName)
				{
					dialog.InitialDirectory = Path.GetDirectoryName(EditorModel.State.FileName);
					dialog.FileName = Path.GetFileName(EditorModel.State.FileName);
				}
				if (dialog.ShowDialog() == true)
					EditorModel.State.FileName = dialog.FileName;
				else
					return;
			}
			CellTemplate template = new CellTemplate(EditorModel.State.Manager);
			template.Save(EditorModel.State.FileName, Path.GetExtension(EditorModel.State.FileName)?.ToUpper() == ".TCRP" ? TemplateType.Package : TemplateType.Xml);
		}

		public static DelegateCommand Print
		{
			get
			{
				if (!Commands.TryGetValue("Print", out var command))
				{
					void Action()
					{
						var printPresenter = new Presenters.WinForms.PrintPresenter(EditorModel.State.Manager);
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
				if (!Commands.TryGetValue("PrintPreview", out var command))
				{
					void Action()
					{
						var printPresenter = new Presenters.WinForms.PrintPresenter(EditorModel.State.Manager);
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
				if (!Commands.TryGetValue("Exit", out var command))
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

		public static DelegateCommand Undo
		{
			get
			{
				if (!Commands.TryGetValue("Undo", out var command))
				{
					command = new DelegateCommand(() =>
					{
						if (EditorModel.IsDesignMode)
							EditorModel.State?.Undo();
						else
							EditorModel.Editor.Undo();
					}, () => EditorModel.IsDesignMode ? EditorModel.State?.CanUndo() == true : EditorModel.Editor.CanUndo);
					Commands.Add("Undo", command);
				}

				return command;
			}
		}

		public static DelegateCommand Redo
		{
			get
			{
				if (!Commands.TryGetValue("Redo", out var command))
				{
					command = new DelegateCommand(() =>
					{
						if (EditorModel.IsDesignMode)
							EditorModel.State?.Redo();
						else
							EditorModel.Editor.Redo();
					}, () => EditorModel.IsDesignMode ? EditorModel.State?.CanRedo() == true : EditorModel.Editor.CanRedo);
					Commands.Add("Redo", command);
				}

				return command;
			}
		}

		public static DelegateCommand Copy
		{
			get
			{
				if (!Commands.TryGetValue("Copy", out var command))
				{
					void Action()
					{
						CopyManager.Copy();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("Copy", command);
				}

				return command;
			}
		}

		public static DelegateCommand Cut
		{
			get
			{
				if (!Commands.TryGetValue("Cut", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						CopyManager.Cut();
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("Cut", command);
				}

				return command;
			}
		}

		public static DelegateCommand Paste
		{
			get
			{
				if (!Commands.TryGetValue("Paste", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						CopyManager.Paste();
						EditorModel.State.EndUpdate();
					}

					bool Enabled() => IsDefaultEnabled() && CopyManager.HasData;
					command = new DelegateCommand(Action, Enabled);
					Commands.Add("Paste", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteRows
		{
			get
			{
				if (!Commands.TryGetValue("DeleteRows", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						if (EditorModel.State.Presenter.HaveSelection())
						{
							var start = EditorModel.State.Presenter.GetStartSelection();
							var end = EditorModel.State.Presenter.GetEndSelection();
							EditorModel.State.Presenter.ClearSelection();
							EditorModel.State.Presenter.Manager.DeleteRows(start.Row, end.Row - start.Row + 1, AskAboutSections());
						}
						else
							EditorModel.State.Presenter.Manager.DeleteRows(EditorModel.State.Presenter.CurrentCell.Row, 1, AskAboutSections());
						if (EditorModel.State.Presenter.CurrentCell.Row >= EditorModel.State.Presenter.Manager.RowsCount)
							EditorModel.State.Presenter.CurrentCell = new CellLocation() {Row = EditorModel.State.Presenter.CurrentCell.Row - 1, Column = EditorModel.State.Presenter.CurrentCell.Column};
						EditorModel.State.EndUpdate();
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
				if (!Commands.TryGetValue("DeleteColumns", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						if (EditorModel.State.Presenter.HaveSelection())
						{
							var start = EditorModel.State.Presenter.GetStartSelection();
							var end = EditorModel.State.Presenter.GetEndSelection();
							EditorModel.State.Presenter.ClearSelection();
							EditorModel.State.Presenter.Manager.DeleteColumns(start.Column, end.Column - start.Column + 1, AskAboutSections());
						}
						else
							EditorModel.State.Presenter.Manager.DeleteColumns(EditorModel.State.Presenter.CurrentCell.Column, 1, AskAboutSections());
						if (EditorModel.State.Presenter.CurrentCell.Column >= EditorModel.State.Presenter.Manager.ColumnsCount)
							EditorModel.State.Presenter.CurrentCell = new CellLocation() {Row = EditorModel.State.Presenter.CurrentCell.Row, Column = EditorModel.State.Presenter.CurrentCell.Column - 1};
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("DeleteColumns", command);
				}

				return command;
			}
		}

		private static bool AskAboutSections()
		{
			return MessageBox.Show("Изменить размеры или положение секции находящиеся в затронутых ячейках?", "Секции", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
		}

		public static DelegateCommand AddRows
		{
			get
			{
				if (!Commands.TryGetValue("addRows", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						EditorModel.State.Presenter.Manager.InsertRows(EditorModel.State.Presenter.CurrentCell.Row, 1, AskAboutSections());
						EditorModel.State.EndUpdate();
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
				if (!Commands.TryGetValue("addRowsAfter", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						EditorModel.State.Presenter.Manager.InsertRows(EditorModel.State.Presenter.CurrentCell.Row + 1, 1, AskAboutSections());
						EditorModel.State.EndUpdate();
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
				if (!Commands.TryGetValue("AddColumns", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						EditorModel.State.Presenter.Manager.InsertColumns(EditorModel.State.Presenter.CurrentCell.Column, 1, AskAboutSections());
						EditorModel.State.EndUpdate();
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
				if (!Commands.TryGetValue("AddColumnsAfter", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						EditorModel.State.Presenter.Manager.InsertColumns(EditorModel.State.Presenter.CurrentCell.Column + 1, 1, AskAboutSections());
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddColumnsAfter", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddTextContent
		{
			get
			{
				if (!Commands.TryGetValue("AddTextContent", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						ContentHelper.AddContent(new TextContent(EditorModel.State.Manager));
						EditorModel.State.CurrentCell = EditorModel.State.CurrentCell;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddTextContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddDecimalContent
		{
			get
			{
				if (!Commands.TryGetValue("AddDecimalContent", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						ContentHelper.AddContent(new DecimalContent(EditorModel.State.Manager));
						EditorModel.State.CurrentCell = EditorModel.State.CurrentCell;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddDecimalContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddDateTimeContent
		{
			get
			{
				if (!Commands.TryGetValue("AddDateTimeContent", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						ContentHelper.AddContent(new DateTimeContent(EditorModel.State.Manager));
						EditorModel.State.CurrentCell = EditorModel.State.CurrentCell;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddDateTimeContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddBarCodeContent
		{
			get
			{
				if (!Commands.TryGetValue("AddBarCodeContent", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						ContentHelper.AddContent(new BarCodeContent(EditorModel.State.Manager, BarCodeType.EAN13));
						EditorModel.State.CurrentCell = EditorModel.State.CurrentCell;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddBarCodeContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddImageContent
		{
			get
			{
				if (!Commands.TryGetValue("AddImageContent", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						ContentHelper.AddContent(new ImageContent(EditorModel.State.Manager));
						EditorModel.State.CurrentCell = EditorModel.State.CurrentCell;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddImageContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddActionContent
		{
			get
			{
				if (!Commands.TryGetValue("AddActionContent", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						ContentHelper.AddContent(new ActionContent(EditorModel.State.Manager) { Type = ActionType.MouseDblClick });
						EditorModel.State.CurrentCell = EditorModel.State.CurrentCell;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddActionContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteContent
		{
			get
			{
				if (!Commands.TryGetValue("DeleteContent", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();
						var cell = EditorModel.State.Manager.GetCell(EditorModel.State.CurrentCell);

						if (cell?.Content != null)
								cell.Content = null;
						EditorModel.State.CurrentCell = EditorModel.State.CurrentCell;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("DeleteContent", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddParam
		{
			get
			{
				if (!Commands.TryGetValue("AddParam", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();

						if (EditorModel.State.CurrentContent != null)
						{
							var param = new Param(EditorModel.State.Manager, EditorModel.State.CurrentContent) { Name = "Новый" };
							EditorModel.State.CurrentContent.AddParam(param);
						}
						EditorModel.State.CurrentContent = EditorModel.State.CurrentContent;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddParam", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteParam
		{
			get
			{
				if (!Commands.TryGetValue("DeleteParam", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();

						if (EditorModel.State.CurrentContent != null && EditorModel.State.CurrentParam != null)
						{
							EditorModel.State.CurrentContent.RemoveParam(EditorModel.State.CurrentParam);
						}
						EditorModel.State.CurrentContent = EditorModel.State.CurrentContent;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("DeleteParam", command);
				}

				return command;
			}
		}

		public static DelegateCommand AddSection
		{
			get
			{
				if (!Commands.TryGetValue("AddSection", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();

						var newSection = new TemplateSection {Name = "Новая"};
						EditorModel.State.Manager.AddSection(newSection);
						EditorModel.State.Sections = null;
						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("AddSection", command);
				}

				return command;
			}
		}

		public static DelegateCommand DeleteSection
		{
			get
			{
				if (!Commands.TryGetValue("DeleteSection", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();

						if (EditorModel.State.CurrentSection != null)
						{
							EditorModel.State.Manager.RemoveSection(EditorModel.State.CurrentSection);
							EditorModel.State.Sections = null;
							EditorModel.State.CurrentSection = null;
						}

						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("DeleteSection", command);
				}

				return command;
			}
		}

		public static DelegateCommand SetSectionBounds
		{
			get
			{
				if (!Commands.TryGetValue("SetSectionBounds", out var command))
				{
					void Action()
					{
						EditorModel.State.BeginUpdate();

						if (EditorModel.State.CurrentSection != null)
						{
							EditorModel.State.CurrentSection.Left = EditorModel.State.Presenter.StartSelection.Column;
							EditorModel.State.CurrentSection.Top = EditorModel.State.Presenter.StartSelection.Row;
							EditorModel.State.CurrentSection.Right = EditorModel.State.Presenter.EndSelection.Column;
							EditorModel.State.CurrentSection.Bottom = EditorModel.State.Presenter.EndSelection.Row;
						}

						EditorModel.State.EndUpdate();
					}

					command = new DelegateCommand(Action, IsDefaultEnabled);
					Commands.Add("SetSectionBounds", command);
				}

				return command;
			}
		}

		public static DelegateCommand MergeCells
		{
			get
			{
				if (!Commands.TryGetValue("MergeCells", out var command))
				{
					void Action()
					{
						//StateManager.BeginUpdate();
						EditorModel.State.BeginUpdate();
						if (EditorModel.State.Presenter.HaveSelection())
						{
							var start = EditorModel.State.Presenter.GetStartSelection();
							var end = EditorModel.State.Presenter.GetEndSelection();
							var cell = EditorModel.State.Manager.GetCell(EditorModel.State.Presenter.CurrentCell.Row, EditorModel.State.Presenter.CurrentCell.Column);
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
						EditorModel.State.EndUpdate();
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
				if (!Commands.TryGetValue("Find", out var command))
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

					command = new DelegateCommand(Action, () => EditorModel.State?.Manager != null && (EditorModel.IsDesignMode || EditorModel.IsCodeMode));
					Commands.Add("Find", command);
				}

				return command;
			}
		}

		public static DelegateCommand FindAndReplace
		{
			get
			{
				if (!Commands.TryGetValue("FindAndReplace", out var command))
				{
					void Action()
					{
						if (EditorModel.IsCodeMode)
						{
							FindAndReplaceDialog.ShowForReplace(EditorModel.Editor);
						}
					}

					command = new DelegateCommand(Action, () => EditorModel.State?.Manager != null && (EditorModel.IsCodeMode));
					Commands.Add("FindAndReplace", command);
				}

				return command;
			}
		}

		public static DelegateCommand FindInCode
		{
			get
			{
				if (!Commands.TryGetValue("FindInCode", out var command))
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
			if (EditorModel.State.Presenter == null)
				return;

			CellTemplate template = new CellTemplate(EditorModel.State.Manager);
			var memoryStream = new MemoryStream();
			template.Save(memoryStream, TemplateType.Xml);
			memoryStream.Position = 0;

			EditorModel.Editor.Text = new StreamReader(memoryStream).ReadToEnd();
		}

		public static void SwitchEditorToDesignMode()
		{
			if (EditorModel.State.Manager == null)
				return;

			try
			{
				EditorModel.State.AddCurrentState();

				CellTemplate template = new CellTemplate();
				template.LoadFromXml(EditorModel.Editor.Text);
				var currentCell = EditorModel.State.Presenter?.CurrentCell;
				foreach (var resourceName in EditorModel.State.Manager.GetResourceNames())
				{
					template.Manager.AddResource(resourceName, EditorModel.State.Manager.GetResource(resourceName));
				}
				if (currentCell.HasValue)
					ApplyReport(template.Manager, currentCell.Value);
				else
					ApplyReport(template.Manager);
			}
			catch (Exception e)
			{
				EditorModel.State.Presenter = null;
			}
		}
	}
}
