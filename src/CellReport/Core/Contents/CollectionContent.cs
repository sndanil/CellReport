using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Globalization;

namespace CellReport.Core.Contents
{
	[Serializable]
	public enum LayoutType
	{
		Layers,
		Sequence
	}

	[Serializable]
	public class CollectionContent : BaseContent
	{
		private List<BaseContent> _contents;

		public int ActualContentIndex { get; set; }

		public CollectionContent(CellManager manager, int capacity) : base(manager)
		{
			_contents = new List<BaseContent>(capacity);
			Layout = LayoutType.Layers;
			ActualContentIndex = -1;
		}

		public int GetActualContentIndex()
		{
			var indexAsOnject = GetParamValueByTarget("ActualContentIndex");
			if (indexAsOnject == null)
				return ActualContentIndex;
			else if (indexAsOnject is string)
				return int.Parse(indexAsOnject.ToString(), CultureInfo.InvariantCulture);
			else
				return Convert.ToInt32(indexAsOnject);
		}

		public void Add(BaseContent content)
		{
			_contents.Add(content);
			content.PropertyChanged += OnPropertyChanged;
			OnPropertyChanged("Contents");
		}

		public void Remove(BaseContent content)
		{
			_contents.Remove(content);
			content.PropertyChanged -= OnPropertyChanged;
			OnPropertyChanged("Contents");
		}

		public int Count => _contents.Count;

		public override IEnumerable<Param> GetParams()
		{
			foreach (BaseContent content in _contents)
				foreach (var param in content.GetParams())
					yield return param;
		}

		public override void SetContentParam(string paramName, object value)
		{
			base.SetContentParam(paramName, value);
			foreach (var content in _contents)
				content.SetContentParam(paramName, value);
		}

		internal override void ResetExpressions()
		{
			foreach (var item in GetContents())
				item.ResetExpressions();
		}

		internal override void ExecuteExpressions()
		{
			foreach (var item in GetContents())
				item.ExecuteExpressions();
		}

		public override void ReplaceExpressionId(string oldId, string newId)
		{
			foreach (var item in GetContents())
				item.ReplaceExpressionId(oldId, newId);
		}

		public LayoutType Layout { get; set; }

		public IEnumerable<BaseContent> GetContents()
		{
			if (GetActualContentIndex() == -1)
			{
				foreach (var content in _contents)
					yield return content;
			}
			else
				yield return _contents[GetActualContentIndex()];
		}

		public override BaseContent Clone(CellManager manager)
		{
			var result = base.Clone(manager);
			((CollectionContent)result)._contents = new List<BaseContent>(_contents.Count);
			for (int i = 0; i < _contents.Count; i++)
				((CollectionContent)result).Add(_contents[i].Clone(manager));

			return result;
		}

		internal new static CollectionContent Parse(CellManager manager, XElement element)
		{
			var content = new CollectionContent(manager, 4);
			if (element.Attribute("Layout") != null)
				content.Layout = (LayoutType)Enum.Parse(typeof(LayoutType), element.Attribute("Layout").Value);
			if (element.Attribute("ActualContentIndex") != null)
				content.ActualContentIndex = int.Parse(element.Attribute("ActualContentIndex").Value, CultureInfo.InvariantCulture);
			foreach (var contentElement in element.Elements())
			{
				var childContent = BaseContent.ParseContent(manager, contentElement);
				if (childContent != null)
					content.Add(childContent);
			}

			return content;
		}

		protected override XElement OnSerialize(string contentName)
		{
			if (_contents.Count == 1)
				return _contents[0].Serialize();
			else if (_contents.Count > 0)
			{

				var result = new XElement("CollectionContent");
				foreach (var item in GetContents())
					result.Add(item.Serialize());

				return result;
			}
			else
				return null;
		}
	}
}
