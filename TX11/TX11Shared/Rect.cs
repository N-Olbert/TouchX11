using System;
using System.Drawing;

namespace TX11Shared
{
    public class Rect
    {
        private Rectangle r;

        public int Left
        {
            get => r.Left;
            set => r = Rectangle.FromLTRB(value, r.Top, r.Right, r.Bottom);
        }

        public int Top
        {
            get => r.Top;
            set => r = Rectangle.FromLTRB(r.Left, value, r.Right, r.Bottom);
        }

        public int Right
        {
            get => r.Right;
            set => r = Rectangle.FromLTRB(r.Left, r.Top, value, r.Bottom);
        }

        public int Bottom
        {
            get => r.Bottom;
            set => r = Rectangle.FromLTRB(r.Left, r.Top, r.Right, value);
        }

        public int X => r.X;

        public int Y => r.Y;

        /**
     * @return the rectangle's width. This does not check for a valid rectangle
     * (i.e. left <= right) so the result may be negative.
     */
        public int Width()
        {
            return r.Width;
        }

        /**
         * @return the rectangle's height. This does not check for a valid rectangle
         * (i.e. top <= bottom) so the result may be negative.
         */
        public int Height()
        {
            return r.Height;
        }

        /**
         * Create a new empty Rect. All coordinates are initialized to 0.
         */
        public Rect()
        {
        }

        /**
         * Create a new rectangle with the specified coordinates. Note: no range
         * checking is performed, so the caller must ensure that left <= right and
         * top <= bottom.
         *
         * @param left   The X coordinate of the left side of the rectangle
         * @param top    The Y coordinate of the top of the rectangle
         * @param right  The X coordinate of the right side of the rectangle
         * @param bottom The Y coordinate of the bottom of the rectangle
         */
        public Rect(int left, int top, int right, int bottom)
        {
            r = Rectangle.FromLTRB(left, top, right, bottom);
        }

        /**
         * Create a new rectangle, initialized with the values in the specified
         * rectangle (which is left unmodified).
         *
         * @param r The rectangle whose coordinates are copied into the new
         *          rectangle.
         */
        public Rect(Rect r)
        {
            if (r == null)
            {
                throw new ArgumentNullException(nameof(r));
            }

            this.r = r.r;
        }

        public static bool Intersects(Rect rect1, Rect rect2)
        {
            return rect1 != null && rect2 != null && rect1.r.IntersectsWith(rect2.r);
        }

        public bool Contains(int x, int y)
        {
            return r.Contains(x, y);
        }

        public bool Contains(Rect rect)
        {
            return rect != null && this.r.Contains(rect.r);
        }
    }
}