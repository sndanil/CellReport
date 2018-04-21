using System;

namespace CellReport.Core
{
	[Serializable]
	public struct Column
	{
		public float Width;
		public string MarkerForShow;
		public string MarkerForHide;

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
				return true;
			if (obj is Column)
			{
				var col = (Column)obj;
				return (Width == col.Width) && (MarkerForShow == col.MarkerForShow) && (MarkerForHide == col.MarkerForHide);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Width.GetHashCode() + (!string.IsNullOrEmpty(MarkerForShow) ? MarkerForShow.GetHashCode() : 0)
				 + (!string.IsNullOrEmpty(MarkerForHide) ? MarkerForHide.GetHashCode() : 0);
		}
	}
}
