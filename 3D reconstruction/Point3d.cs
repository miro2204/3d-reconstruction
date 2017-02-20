using System;

namespace _3D_reconstruction
{
    public class Point3d
    {
        public float X;
        public float Y;
        public float Z;

        public Point3d()
        {
        }

        public Point3d(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", X, Y, Z);
        }
    }
}
