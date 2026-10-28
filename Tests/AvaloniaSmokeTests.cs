#if NET10_0
using Avalonia;
using Avalonia.Headless;
using CellReport.Avalonia;
using CellReport.Avalonia.Services;
using CellReport.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CellReportTests
{
	[TestClass]
	[DoNotParallelize]
	public class AvaloniaSmokeTests
	{
		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			if (Application.Current == null)
			{
				AppBuilder.Configure<TestApplication>()
					.UseHeadless(new AvaloniaHeadlessPlatformOptions())
					.SetupWithoutStarting();
			}
		}

		[TestMethod]
		public void PresentControl_CanAttachManager_InHeadlessHost()
		{
			var control = new PresentControl();
			var manager = new CellManager(1, 1);

			control.AttachManager(manager);

			Assert.IsNotNull(control.Presenter);
		}

		[TestMethod]
		public void DesignModeScope_OnlySuspendsTargetManager()
		{
			var target = new CellManager { IsDesignMode = true };
			var other = new CellManager { IsDesignMode = true };

			using (DesignModeScope.Suspend(target))
			{
				Assert.IsFalse(target.IsDesignMode);
				Assert.IsTrue(other.IsDesignMode);
			}

			Assert.IsTrue(target.IsDesignMode);
		}

		private sealed class TestApplication : Application
		{
		}
	}
}
#endif
