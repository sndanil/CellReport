using System;

namespace CellReport.Core
{
    [Serializable]
	public struct Column
	{
		public float Width;
		public string Marker;

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
				return true;
			if (obj is Column)
			{
				var col = (Column)obj;
				return (Width == col.Width) && (Marker == col.Marker);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Width.GetHashCode() + (!string.IsNullOrEmpty(Marker) ? Marker.GetHashCode() : 0);
		}
	}
}
