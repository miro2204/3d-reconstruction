using System.Collections.Generic;
using System.Drawing;

namespace _3D_reconstruction
{
    public class PointComparer : IComparer<Point>
    {
        public int Compare(Point first, Point second)
        {
            if (first.Y == second.Y)
            {
                return first.X - second.X;
            }
            return first.Y - second.Y;
        }
    }

    public class PointComparerY : IComparer<Point>
    {
        public int Compare(Point first, Point second)
        {
            if (first.Y == second.Y)
            {
                return second.X - first.X;
            }
            return second.Y - first.Y;
        }
    }
}
