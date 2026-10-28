using CellReport.Core.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	public class DecimalExpressionTests
	{
		[DataTestMethod]
		[DataRow("0.12", "12")]
		[DataRow("1.23", "23")]
		[DataRow("-1.23", "-23")]
		[DataRow("42", "0")]
		public void DecimalPartAndShift_ReturnsShiftedFraction(string input, string expected)
		{
			var expression = new DecimalExpression
			{
				Operation = Operation.DecimalPartAndShift
			};

			var result = expression.GetValue(null, decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture));

			Assert.AreEqual(
				decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
				result);
		}
	}
}
