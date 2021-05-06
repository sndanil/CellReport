using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;

namespace CellReport.Core
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        public static readonly Point Empty = new Point(0, 0);

        private float _x;
        private float _y;
        
        public Point(float x, float y)
        {
            this._x = x;
            this._y = y;
        }

        public Point(Size sz)
        {
            this._x = sz.Width;
            this._y = sz.Height;
        }

        public bool IsEmpty
        {
            get { return (this._x == 0) && (this._y == 0); }
        }

        public float X
        {
            get { return this._x; }
            set { this._x = value; }
        }

        public float Y
        {
            get { return this._y; }
            set { this._y = value; }
        }

        public static explicit operator Size(Point p)
        {
            return new Size(p.X, p.Y);
        }

        public static Point operator +(Point pt, Size sz)
        {
            return Add(pt, sz);
        }

        public static Point operator -(Point pt, Size sz)
        {
            return Subtract(pt, sz);
        }

        public static bool operator ==(Point left, Point right)
        {
            return ((left.X == right.X) && (left.Y == right.Y));
        }

        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }

        public static Point Add(Point pt, Size sz)
        {
            return new Point(pt.X + sz.Width, pt.Y + sz.Height);
        }

        public static Point Subtract(Point pt, Size sz)
        {
            return new Point(pt.X - sz.Width, pt.Y - sz.Height);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point))
                return false;
            Point point = (Point)obj;
            return ((point.X == this.X) && (point.Y == this.Y));
        }

        public override int GetHashCode()
        {
            return (Convert.ToInt32(this._x) ^ Convert.ToInt32(this._y));
        }

        public void Offset(float dx, float dy)
        {
            this.X += dx;
            this.Y += dy;
        }

        public void Offset(Point p)
        {
            this.Offset(p.X, p.Y);
        }

        public override string ToString()
        {
            return ("{X=" + this.X.ToString(CultureInfo.CurrentCulture) + ",Y=" + this.Y.ToString(CultureInfo.CurrentCulture) + "}");
        }
    }
}
