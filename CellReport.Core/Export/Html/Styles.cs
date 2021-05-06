using CellReport.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CellReport.Export.Html
{
	internal class Styles
	{
		private Dictionary<BaseStyle, string> _styles = new Dictionary<BaseStyle, string>();
		private Dictionary<string, string> _images = new Dictionary<string, string>();
		private CellManager _manager = null;

		public Styles(CellManager manager)
		{
			_manager = manager;
		}

		public string Add(BaseStyle style)
		{
			if (_styles.ContainsKey(style))
				return _styles[style];

			// "c" is required since css class name cannot start from digit.
			// GetHashCode is needed to get short names.
			var id = "c" + Guid.NewGuid().GetHashCode().ToString("x");
			_styles.Add(style, id);
			return id;
		}

		public string Add(string uri)
		{
			if (_images.ContainsKey(uri))
				return _images[uri];

			var id = "i" + Guid.NewGuid().GetHashCode().ToString("x");
			_images.Add(uri, id);
			return id;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			CssClass.GetBodyClass().AppendTo(sb);
			CssClass.GetTableClass().AppendTo(sb);
			CssClass.GetImgClass().AppendTo(sb);
			foreach (var pair in _styles)
				CssClass.GetFromStyle(pair.Value, pair.Key).AppendTo(sb);
			foreach (var pair in _images)
				CssClass.GetFromUri(pair.Value, pair.Key, _manager).AppendTo(sb);
			return sb.ToString();
		}
	}
}
