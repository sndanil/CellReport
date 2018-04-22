using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CellReport.Core;
using CellReport;
using CellReport.Presenters.WinForms;

namespace Demo
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();

			CellReport.Core.MarkerResolver.OnResolve += MarkerResolver_OnResolve;
		}

		private bool MarkerResolver_OnResolve(string arg)
		{
			switch (arg)
			{
				case "Hide":
					return true;
				//case "Show":
				//	return true;
				default:
					return false;
			}
		}

		private void _groupButton_Click(object sender, EventArgs e)
		{
			var doc = new ReportDocument(@"Templates\GroupedReport.tcrp");
			doc.AddSection("header");
			for (int i = 0; i < 16; i++)
			{
				doc.AddSection("level1");
				doc.SetParameter("Наим", "Название" + i);
				doc.JoinSection("digits");
				for (int i1 = 0; i1 < 2; i1++)
				{
					doc.AddSection("level2");
					doc.SetParameter("Наим", "Название" + i + "_" + i1);
					doc.JoinSection("digits");
					for (int i2 = 0; i2 < 2; i2++)
					{
						doc.AddSection("level3");
						doc.SetParameter("Наим", "Название" + i + "_" + i1 + "_" + i2);
						doc.JoinSection("digits");
					}
				}
			}
			doc.Show();
		}

		private void _tableAndChartButton_Click(object sender, EventArgs e)
		{
			var doc = new ReportDocument(@"Templates\ПродажиПоМагазинам.tcrp");
			doc.AddSection("header");

			doc.AddSection("line");
			doc.SetParameter("Name", "Магазин 1");
			doc.SetParameter("Sum", 25000);

			doc.AddSection("line");
			doc.SetParameter("Name", "Магазин 2");
			doc.SetParameter("Sum", 2500);

			doc.AddSection("line");
			doc.SetParameter("Name", "Магазин 3");
			doc.SetParameter("Sum", 5000);

			doc.AddSection("line");
			doc.SetParameter("Name", "Магазин 4");
			doc.SetParameter("Sum", 20000);

			doc.AddSection("chart");
			doc.SetParameter("xdata", new string[] { "Магазин 1", "Магазин 2", "Магазин 3", "Магазин 4" });
			doc.SetParameter("ydata", new decimal[] { 25000, 2500, 5000, 20000 });

			doc.Show();
		}

		private void _barCodesButton_Click(object sender, EventArgs e)
		{
			var template = new CellReport.Core.CellTemplate();
			template.Load(@"Templates\barcodes.tcr");
			template.Show(null);
		}

		private void _chartsButton_Click(object sender, EventArgs e)
		{
			var template = new CellReport.Core.CellTemplate();
			template.Load(@"Templates\charts.tcrp");
			template.Show(null);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			var template = new CellReport.Core.CellTemplate();
			template.Load(@"Templates\images.tcrp");
			template.Show(null);
		}

		private void _ttnButton_Click(object sender, EventArgs e)
		{
			var template = new CellReport.Core.CellTemplate();
			template.Load(@"Templates\torg12.tcrp");
			template.Show(null);
		}

		private void _op5Button_Click(object sender, EventArgs e)
		{
			var template = new CellReport.Core.CellTemplate();
			template.Load(@"Templates\op05.tcr");
			template.Show(null);
		}

		private void _kalcButton_Click(object sender, EventArgs e)
		{
			var template = new CellReport.Core.CellTemplate();
			template.Load(@"Templates\calc.tcrp");
			template.Show(null);
		}

		private void _pageBreakButton_Click(object sender, EventArgs e)
		{
			var doc = new ReportDocument(@"Templates\ПродажиПоМагазинам.tcrp");
			doc.AddSection("header");
			doc.AddPageBreak();

			doc.AddSection("line");
			doc.SetParameter("Name", "Магазин 1");
			doc.SetParameter("Sum", 25000);
			doc.AddPageBreak();

			doc.AddSection("line");
			doc.SetParameter("Name", "Магазин 2");
			doc.SetParameter("Sum", 2500);
			doc.AddPageBreak();

			doc.AddSection("line");
			doc.SetParameter("Name", "Магазин 3");
			doc.SetParameter("Sum", 5000);
			doc.AddPageBreak();

			doc.AddSection("line");
			doc.SetParameter("Name", "Магазин 4");
			doc.SetParameter("Sum", 20000);
			doc.AddPageBreak();

			doc.Show();
		}

		private void _autoHeightButton_Click(object sender, EventArgs e)
		{
			var rep = new ReportDocument(@"Templates\ПродажиТоваров.tcrp");
			rep.AddSection("Шапка");

			for (int i = 0; i < 100; i++)
			{
				rep.AddSection("1");
				rep.SetParameter("Наим", "Вареники с картофелем 550гр. ООО \"НатурПродукт\" Вареники с картофелем 550гр. ООО \"НатурПродукт\"");

				rep.AddSection("1");
				rep.SetParameter("Наим", "Лук Желтый репчатый 500г чулок Тульская нива");

				rep.AddSection("1");
				rep.SetParameter("Наим", "Мыльные пузыри 20мл УЛЫБКА");

				rep.AddSection("1");
				rep.SetParameter("Наим", "Служба спасения - полиция Игровой набор ВЕЛЛИ");

				rep.AddSection("1");
				rep.SetParameter("Наим", "Морковь");

				rep.AddSection("1");
				rep.SetParameter("Наим", "Вареники с картофелем 550гр. ООО \"НатурПродукт\" Вареники с картофелем 550гр. ООО \"НатурПродукт\"");
			}

			rep.Show();
		}

		private void _autoSizeFontButton_Click(object sender, EventArgs e)
		{
			var rep = new ReportDocument(@"Templates\AutoSizeFont.tcrp");
			
			rep.AddSection("Секция");
			rep.SetParameter("Наим", 
				"Вареники с картофелем 550гр. ООО \"НатурПродукт\" Вареники с картофелем 550гр. ООО \"НатурПродукт\""
				+ "Вареники с картофелем 550гр. ООО \"НатурПродукт\" Вареники с картофелем 550гр. ООО \"НатурПродукт\""
				+ "Вареники с картофелем 550гр. ООО \"НатурПродукт\" Вареники с картофелем 550гр. ООО \"НатурПродукт\""
				);

			rep.AddSection("Секция");
			rep.SetParameter("Наим", "Лук Желтый репчатый 500г чулок Тульская нива");

			rep.AddSection("Секция");
			rep.SetParameter("Наим", "Мыльные пузыри 20мл УЛЫБКА");

			rep.AddSection("Секция");
			rep.SetParameter("Наим", "Служба спасения - полиция Игровой набор ВЕЛЛИ");

			rep.AddSection("Секция");
			rep.SetParameter("Наим", "Морковь");

			rep.AddSection("Секция");
			rep.SetParameter("Наим", "Вареники с картофелем 550гр. ООО \"НатурПродукт\" Вареники с картофелем 550гр. ООО \"НатурПродукт\"");

			rep.Show();
		}

		private void button1_Click_1(object sender, EventArgs e)
		{
			var template = new CellReport.Core.CellTemplate();
			template.Load(@"Templates\ShowHideColumn.tcrp");
			template.Show(null).ShowResizers = true;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			var rep = new ReportDocument(@"Templates\Params.tcr");

			rep.AddSection("Section");
			rep.SetParameter("Virtual", "Лук Желтый");
			rep.SetParameter("Новый2", "Значение со скобками [Скобки]");

			rep.Show();
		}
	}
}
