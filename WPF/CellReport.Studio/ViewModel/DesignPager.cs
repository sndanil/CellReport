using System;
using System.Collections.Generic;
using System.Linq;

using CellReport.Core;
using CellReport.Core.Contents;
using CellReport.Core.Expressions;

namespace CellReport.Studio.ViewModel
{
	public class DesignPager : NotifyPropertyChanged
	{
		private Pager _pager = null;

		public DesignPager(Pager pager)
		{
			_pager = pager;

			var contents = new List<BaseContent>() { _pager.Left, _pager.Center, _pager.Right };
			Content = string.Join("; ", contents.Where(x => x != null).Select(x => GetContentText(x)));

			FromPage = _pager.FromPage;
			ToPage = _pager.ToPage;
		}

		public string Content
		{
			get { return GetParam<string>("Content"); }
			set { SetParam("Content", value); }
		}

		public int FromPage
		{
			get { return GetParam<int>("FromPage"); }
			set { SetParam("FromPage", value); }
		}

		public int ToPage
		{
			get { return GetParam<int>("ToPage"); }
			set { SetParam("ToPage", value); }
		}

		private string GetContentText(BaseContent content)
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
			return text;
		}

		public Pager ToPager()
		{
			_pager.FromPage = this.FromPage;
			_pager.ToPage = this.ToPage;
			return _pager;
		}

		public DesignPager Clone()
		{
			return new DesignPager(this.ToPager().Clone(StudioCommands.Manager));
		}

		public static IList<Pager> GetPagers(IList<DesignPager> designPagers, List<BaseExpression> expressions)
		{
			var pagers = new List<Pager>();
			foreach (var pager in designPagers.Select(x => x.ToPager()))
			{
				pagers.Add(pager);
				pager.AddExpressions(StudioCommands.Manager, expressions);
			}
			return pagers;
		}
	}
}
