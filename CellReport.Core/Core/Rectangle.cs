using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace CellReport.Core
{
    [Serializable]
    public struct Rectangle
    {
        public static readonly Rectangle Empty = new Rectangle(0, 0, 0, 0);

        private float _x;
        private float _y;
        private float _width;
        private float _height;

        public Rectangle(float x, float y, float width, float height)
        {
            this._x = x;
            this._y = y;
            this._width = width;
            this._height = height;
        }

        public Size Size
        {
            get{ return new Size(this.Width, this.Height); }
            set
            {
                this.Width = value.Width;
                this.Height = value.Height;
            }
        }

        public float X
        {
            get{ return this._x; }
            set{ this._x = value; }
        }

        public float Y
        {
            get{ return this._y; }
            set{ this._y = value; }
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

        public float Left
        {
            get{ return this.X; }
        }

        public float Top
        {
            get{ return this.Y; }
        }

        public float Right
        {
            get{ return (this.X + this.Width); }
        }

        public float Bottom
        {
            get{ return (this.Y + this.Height); }
        }

        public bool IsEmpty
        {
            get
            {
                if (this.Width > 0f)
                    return (this.Height <= 0f);
                return true;
            }
        }
        public override bool Equals(object obj)
        {
            if (!(obj is Rectangle))
            {
                return false;
            }
            Rectangle ef = (Rectangle)obj;
            return ((((ef.X == this.X) && (ef.Y == this.Y)) && (ef.Width == this.Width)) && (ef.Height == this.Height));
        }

        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return ((((left.X == right.X) && (left.Y == right.Y)) && (left.Width == right.Width)) && (left.Height == right.Height));
        }

        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return !(left == right);
        }

        public bool Contains(float x, float y)
        {
            return ((((this.X <= x) && (x < (this.X + this.Width))) && (this.Y <= y)) && (y < (this.Y + this.Height)));
        }

        public bool Contains(Rectangle rect)
        {
            return ((((this.X <= rect.X) && ((rect.X + rect.Width) <= (this.X + this.Width))) && (this.Y <= rect.Y)) && ((rect.Y + rect.Height) <= (this.Y + this.Height)));
        }

        public override int GetHashCode()
        {
            return (int)(((((uint)this.X) ^ ((((uint)this.Y) << 13) | (((uint)this.Y) >> 0x13))) ^ ((((uint)this.Width) << 0x1a) | (((uint)this.Width) >> 6))) ^ ((((uint)this.Height) << 7) | (((uint)this.Height) >> 0x19)));
        }

        public void Inflate(float x, float y)
        {
            this.X -= x;
            this.Y -= y;
            this.Width += 2f * x;
            this.Height += 2f * y;
        }

        public void Inflate(Size size)
        {
            this.Inflate(size.Width, size.Height);
        }

        public static Rectangle Inflate(Rectangle rect, float x, float y)
        {
            Rectangle ef = rect;
            ef.Inflate(x, y);
            return ef;
        }

        public void Intersect(Rectangle rect)
        {
            Rectangle ef = Intersect(rect, this);
            this.X = ef.X;
            this.Y = ef.Y;
            this.Width = ef.Width;
            this.Height = ef.Height;
        }

        public static Rectangle Intersect(Rectangle a, Rectangle b)
        {
            float x = Math.Max(a.X, b.X);
            float num2 = Math.Min((float)(a.X + a.Width), (float)(b.X + b.Width));
            float y = Math.Max(a.Y, b.Y);
            float num4 = Math.Min((float)(a.Y + a.Height), (float)(b.Y + b.Height));
            if ((num2 >= x) && (num4 >= y))
                return new Rectangle(x, y, num2 - x, num4 - y);
            return Empty;
        }

        public bool IntersectsWith(Rectangle rect)
        {
            return ((((rect.X < (this.X + this.Width)) && (this.X < (rect.X + rect.Width))) && (rect.Y < (this.Y + this.Height))) && (this.Y < (rect.Y + rect.Height)));
        }

        public static Rectangle Union(Rectangle a, Rectangle b)
        {
            float x = Math.Min(a.X, b.X);
            float num2 = Math.Max((float)(a.X + a.Width), (float)(b.X + b.Width));
            float y = Math.Min(a.Y, b.Y);
            float num4 = Math.Max((float)(a.Y + a.Height), (float)(b.Y + b.Height));
            return new Rectangle(x, y, num2 - x, num4 - y);
        }

        public void Offset(float x, float y)
        {
            this.X += x;
            this.Y += y;
        }

        public override string ToString()
        {
            return ("{X=" + this.X.ToString(CultureInfo.CurrentCulture) + ",Y=" + this.Y.ToString(CultureInfo.CurrentCulture) + ",Width=" + this.Width.ToString(CultureInfo.CurrentCulture) + ",Height=" + this.Height.ToString(CultureInfo.CurrentCulture) + "}");
        }
    }
}
