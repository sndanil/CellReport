using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CellReport.Core;
using CellReport.Core.Contents;
using System.Globalization;

namespace CellReport.Presenters
{
	public abstract class BaseRenderer
	{
		public BaseRenderer()
		{
		}

		protected void RenderPagerContent(object context, BaseContent content, Rectangle bounds, HorizontalAlignment h, float scale)
		{
			TextBasedContent textContent = null;
			var text = string.Empty;
			if (content is CollectionContent)
			{
				foreach (var item in ((CollectionContent)content).GetContents())
				{
					if (item is TextBasedContent)
					{
						textContent = (TextBasedContent)item;
						text += textContent.GetTextForRender();
					}
				}
			}
			else if (content is TextBasedContent)
			{
				textContent = (TextBasedContent)content;
				text = textContent.GetTextForRender();
			}
			if (textContent != null)
			{
				var style = (TextContentStyle)textContent.GetActualStyle();
				var fontSize = scale * style.Font.Size;
				RenderText(context, bounds, text, textContent.GetColor(), h, VerticalAlignment.Middle,
					style.Angle, style.Font.Family, fontSize, style.Font.Style, style.Direction);
			}
		}

		public void RenderPager(object context, Pager pager, Rectangle bounds, float scale)
		{
			if (pager == null || context == null)
				return;

			RenderPagerContent(context, pager.Left, bounds, HorizontalAlignment.Left, scale);
			RenderPagerContent(context, pager.Center, bounds, HorizontalAlignment.Center, scale);
			RenderPagerContent(context, pager.Right, bounds, HorizontalAlignment.Right, scale);
		}

		public virtual void RenderImage(object context, Rectangle cellRect, byte[] data)
		{
		}

		public virtual void RenderText(object context, Rectangle textRect, string text, string color,
				HorizontalAlignment h, VerticalAlignment v, float angle,
				string fontFamily, float fontSize, CellReport.Core.FontStyle fontStyle, Direction direction)
		{
		}

		public virtual void RenderBarCode(object context, Rectangle cellRect, BarCodeContent content, string color,
			string fontFamily, float fontSize, FontStyle fontStyle, Orientation orientation)
		{
		}

		public virtual void RenderLine(object context, string color, float lineWidth, float x1, float y1, float x2, float y2)
		{
			RenderLines(context, color, lineWidth, LineStyle.Solid,
					new[] { new Point(x1, y1), new Point(x2, y2) });
		}

		public virtual void RenderLines(object context, string color, float lineWidth, LineStyle style, Point[] points)
		{
		}

		public virtual void RenderRect(object context, Rectangle rect, string color, LineStyle style, float lineWidth)
		{
		}

		public virtual void RenderFillRect(object context, Rectangle rect, float angle, params string[] colors)
		{
		}

		public virtual void RenderChart(object context, Rectangle rect, Core.Contents.ChartContent content)
		{
		}

		public virtual void SetClip(object context, Rectangle rect)
		{
		}

		public virtual byte[] CreateImage(int width, int height, Action<object> renderAction)
		{
			return null;
		}
	}
}
