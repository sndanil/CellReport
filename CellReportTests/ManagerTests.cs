using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit;
using NUnit.Framework;
using CellReport.Core;

namespace CellReportTests
{
    [TestFixture]
    public class ManagerTests
    {
        [Test]
        public void Test1()
        {
            CellManager manager = new CellManager(5, 5);

            manager.RowsCount = 30;
            manager.ColumnsCount = 20;

            Assert.AreEqual(30, manager.RowsCount);
            Assert.AreEqual(20, manager.ColumnsCount);

            var cell = manager.CreateCell(29, 19);
            Assert.IsNotNull(cell);
        }

        [Test]
        public void Test2()
        {
            CellManager manager = new CellManager(10, 10);

            var cell = manager.CreateCell(2, 3);

            Assert.AreEqual(2, cell.Location.Row);
            Assert.AreEqual(3, cell.Location.Column);
        }

        [Test]
        public void Test3()
        {
            CellManager manager = new CellManager(10, 10);

            var cell = manager.CreateCell(2, 3);
            cell.Span = new CellSpan() { RowSpan = 2, ColSpan = 3 };

            Assert.AreEqual(2, cell.Location.Row);
            Assert.AreEqual(3, cell.Location.Column);

            Assert.AreEqual(2, cell.Span.RowSpan);
            Assert.AreEqual(3, cell.Span.ColSpan);

            Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(2, 3)));

            cell = manager.CreateCell(0, 0);
            cell.Span = new CellSpan() { RowSpan = 1, ColSpan = 1 };

            Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(0, 0)));
            Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(1, 1)));
            Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(0, 1)));
            Assert.IsTrue(object.ReferenceEquals(cell, manager.GetCell(1, 0)));
        }

        [Test]
        public void Test4()
        {
            CellManager manager = new CellManager(10, 10, 50, new Column());

            var cell = manager.CreateCell(2, 3);
            manager.SetRowHeight(2, 100);
            manager.SetColumnWidth(3, 120);

            Assert.AreEqual(100, cell.Size.Height);
            Assert.AreEqual(120, cell.Size.Width);

            Assert.AreEqual(100, manager.GetRowHeight(2));
            Assert.AreEqual(120, manager.GetColumnWidth(3));

            Assert.AreEqual(50, manager.GetRowHeight(1));
            Assert.AreEqual(50, manager.GetColumnWidth(1));

            cell.Span = new CellSpan { ColSpan = 1, RowSpan = 1 };

            Assert.AreEqual(150, cell.Size.Height);
            Assert.AreEqual(170, cell.Size.Width);
        }

        [Test]
        public void Test5()
        {
            CellManager manager = new CellManager();

            var style = new CellStyle("#FFFFFF");
            var cell = manager.CreateCell(2, 3);
            cell.Style = style;

            var style2 = new CellStyle("#FFFFFF");
            var cell2 = manager.CreateCell(3, 3);
            cell2.Style = style2;

            Assert.IsTrue(object.ReferenceEquals(cell.Style, cell2.Style));
        }

        [Test]
        public void Test6()
        {
            CellManager manager = new CellManager(5, 5);

            manager.RowsCount = 30;

            Assert.AreEqual(30, manager.RowsCount);
            Assert.AreEqual(5, manager.ColumnsCount);

            var cell = manager.CreateCell(29, 1);
            Assert.IsNotNull(cell);
        }

        [Test]
        public void Test7()
        {
            CellManager manager = new CellManager(5, 5);

            manager.ColumnsCount = 30;

            Assert.AreEqual(5, manager.RowsCount);
            Assert.AreEqual(30, manager.ColumnsCount);

            var cell = manager.CreateCell(1, 29);
            Assert.IsNotNull(cell);
        }

        [Test]
        public void Test8()
        {
            CellManager manager = new CellManager(5, 5);

            manager.ColumnsCount = 30;

            Assert.AreEqual(0, manager.ActualColumnsCount);
            Assert.AreEqual(0, manager.ActualRowsCount);

            var cell = manager.CreateCell(1, 29);

            Assert.AreEqual(30, manager.ActualColumnsCount);
            Assert.AreEqual(2, manager.ActualRowsCount);
        }
    }
}
