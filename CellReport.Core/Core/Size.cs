using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace CellReport.Core
{
    [Serializable]
    public struct Size
    {
        public static readonly Size Empty = new Size(0, 0);

        private float _width;
        private float _height;

        public Size(float width, float height)
        {
            _width = width;
            _height = height;
        }

        public static Size operator +(Size sz1, Size sz2)
        {
            return Add(sz1, sz2);
        }

        public static Size operator -(Size sz1, Size sz2)
        {
            return Subtract(sz1, sz2);
        }

        public static bool operator ==(Size sz1, Size sz2)
        {
            return ((sz1.Width == sz2.Width) && (sz1.Height == sz2.Height));
        }

        public static bool operator !=(Size sz1, Size sz2)
        {
            return !(sz1 == sz2);
        }

        public bool IsEmpty
        {
            get{ return ((this._width == 0f) && (this._height == 0f)); }
        }

        public float Width
        {
            get{ return this._width; }
            set{ this._width = value; }
        }

        public float Height
        {
            get{ return this._height; }
            set{ this._height = value; }
        }

        public static Size Add(Size sz1, Size sz2)
        {
            return new Size(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
        }

        public static Size Subtract(Size sz1, Size sz2)
        {
            return new Size(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Size))
            {
                return false;
            }
            Size ef = (Size)obj;
            return (((ef.Width == this.Width) && (ef.Height == this.Height)) && ef.GetType().Equals(base.GetType()));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return ("{Width=" + this._width.ToString(CultureInfo.CurrentCulture) + ", Height=" + this._height.ToString(CultureInfo.CurrentCulture) + "}");
        }
    }
}
